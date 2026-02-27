using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// An <see cref="IService{TEntity}"/> implementation backed by a MongoDB collection with an in-memory cache and a configurable stale duration.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to persist. Must be a class and have at least one <see cref="IsIdAttribute"/> property.</typeparam>
    /// <remarks>
    /// The MongoDB client and collection are created once at construction using the values in <see cref="Settings"/> at that point.
    /// The <see cref="Settings"/> property is mutable after construction; changes to cache TTL take effect immediately,
    /// but connection details (host, credentials, database) are fixed after the constructor runs.
    /// BSON class maps are registered once per type via <see cref="BsonClassMap"/>.
    /// </remarks>
    internal class MongoDBService<TEntity> : IService<TEntity> where TEntity : class
    {
        private readonly IMongoCollection<TEntity> collection;
        private List<TEntity> cache = [];
        private DateTime lastRefreshUtc = DateTime.MinValue;

        /// <summary>Gets or sets the configuration for this service. Changes to <see cref="MongoDBServiceSettings.CacheStaleAfter"/> take effect immediately; connection parameters are read only at construction.</summary>
        public MongoDBServiceSettings Settings { get; set; }

        /// <summary>
        /// Initialises the service, registers BSON class maps, validates the entity key configuration, and opens the MongoDB collection.
        /// </summary>
        /// <param name="settings">The connection and cache settings. Must not be <c>null</c>.</param>
        /// <param name="configureClassMaps">Optional callback invoked after the default auto-map to register subclass or custom BSON maps.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="TEntity"/> has no <see cref="IsIdAttribute"/> property.</exception>
        public MongoDBService(
            MongoDBServiceSettings settings,
            Action? configureClassMaps = null)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            ConfigureBsonMaps(configureClassMaps);

            var configValidation = EntityKeyResolver<TEntity>.ValidateConfiguration();
            if (!configValidation.IsSuccess)
            {
                throw new InvalidOperationException(configValidation.Error?.TechnicalMessage);
            }

            var connectionString = $"mongodb+srv://{settings.User}:{settings.Password}@{settings.Host}/?appName={settings.AppName}";
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            collection = database.GetCollection<TEntity>(settings.CollectionName);
        }

        private static void ConfigureBsonMaps(Action? configureClassMaps)
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(TEntity)))
            {
                BsonClassMap.RegisterClassMap<TEntity>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIsRootClass(true);
                });
            }

            configureClassMaps?.Invoke();
        }

        public TimeSpan CacheStaleAfter => Settings.CacheStaleAfter;
        public bool IsCacheStale => DateTime.UtcNow - lastRefreshUtc > Settings.CacheStaleAfter;

        public Result<List<TEntity>> ReadAll(bool useCache = true)
        {
            return EnsureCache(useCache);
        }

        public Result<TEntity?> GetById(object id, bool useCache = true)
        {
            var loaded = EnsureCache(useCache);
            if (!loaded.IsSuccess)
            {
                return Result<TEntity?>.Failure(loaded.Error!);
            }

            var found = cache.FirstOrDefault(x => EntityKeyResolver<TEntity>.KeysEqual(x, id));
            return Result<TEntity?>.Success(found);
        }

        public Result Add(TEntity item)
        {
            try
            {
                collection.InsertOne(item);
                cache.Add(item);
                lastRefreshUtc = DateTime.UtcNow;
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(new AppError(ErrorCode.DataSource, ex.Message));
            }
        }

        public Result Update(TEntity item)
        {
            try
            {
                var key = EntityKeyResolver<TEntity>.GetKey(item);
                var filter = BuildKeyFilter(key);

                var updateResult = collection.ReplaceOne(filter, item, new ReplaceOptions { IsUpsert = false });
                if (updateResult.MatchedCount == 0)
                {
                    return Result.Failure(new AppError(ErrorCode.NotFound, $"Entity with key '{key}' not found."));
                }

                int index = cache.FindIndex(existing => EntityKeyResolver<TEntity>.KeysEqual(existing, key));
                if (index >= 0)
                    cache[index] = item;
                else
                    cache.Add(item);

                lastRefreshUtc = DateTime.UtcNow;
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(new AppError(ErrorCode.DataSource, ex.Message));
            }
        }

        public Result DeleteById(object id)
        {
            try
            {
                var delete = collection.DeleteOne(BuildKeyFilter(id));
                if (delete.DeletedCount == 0)
                {
                    return Result.Failure(new AppError(ErrorCode.NotFound, $"Entity with key '{id}' not found."));
                }

                cache = cache.Where(existing => !EntityKeyResolver<TEntity>.KeysEqual(existing, id)).ToList();
                lastRefreshUtc = DateTime.UtcNow;
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(new AppError(ErrorCode.DataSource, ex.Message));
            }
        }

        public Result Delete(TEntity item)
        {
            var key = EntityKeyResolver<TEntity>.GetKey(item);
            return DeleteById(key);
        }

        public Result WriteAll(List<TEntity> items)
        {
            try
            {
                var docs = items ?? [];
                collection.DeleteMany(Builders<TEntity>.Filter.Empty);

                if (docs.Count > 0)
                {
                    collection.InsertMany(docs);
                }

                cache = new List<TEntity>(docs);
                lastRefreshUtc = DateTime.UtcNow;
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(new AppError(ErrorCode.DataSource, ex.Message));
            }
        }

        public Result RefreshCache()
        {
            var refreshed = EnsureCache(useCache: false);
            if (!refreshed.IsSuccess)
            {
                return Result.Failure(refreshed.Error!);
            }

            return Result.Success();
        }

        private Result<List<TEntity>> EnsureCache(bool useCache)
        {
            try
            {
                if (useCache && cache.Count > 0 && !IsCacheStale)
                {
                    return Result<List<TEntity>>.Success(new List<TEntity>(cache));
                }

                cache = collection.Find(Builders<TEntity>.Filter.Empty).ToList();
                lastRefreshUtc = DateTime.UtcNow;
                return Result<List<TEntity>>.Success(new List<TEntity>(cache));
            }
            catch (Exception ex)
            {
                return Result<List<TEntity>>.Failure(new AppError(ErrorCode.DataSource, ex.Message));
            }
        }

        private static FilterDefinition<TEntity> BuildKeyFilter(object id)
        {
            var idProperties = typeof(TEntity)
                .GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(IsIdAttribute)))
                .OrderBy(p => p.GetCustomAttributes(typeof(IsIdAttribute), true)
                    .Cast<IsIdAttribute>()
                    .FirstOrDefault()?.Order ?? 0)
                .ToArray();

            if (idProperties.Length == 0)
            {
                throw new InvalidOperationException($"Aucune propriété [IsId] trouvée sur {typeof(TEntity).Name}.");
            }

            if (idProperties.Length == 1)
            {
                object converted = ConvertKeyValue(idProperties[0].PropertyType, id);
                return Builders<TEntity>.Filter.Eq(idProperties[0].Name, converted);
            }

            if (id is not CompositeKey composite || composite.Parts.Count != idProperties.Length)
            {
                throw new InvalidOperationException("La clé composite fournie est invalide.");
            }

            var filters = new List<FilterDefinition<TEntity>>();
            for (int i = 0; i < idProperties.Length; i++)
            {
                object converted = ConvertKeyValue(idProperties[i].PropertyType, composite.Parts[i]);
                filters.Add(Builders<TEntity>.Filter.Eq(idProperties[i].Name, converted));
            }

            return Builders<TEntity>.Filter.And(filters);
        }

        private static object ConvertKeyValue(Type targetType, object? value)
        {
            if (value == null)
            {
                throw new InvalidOperationException("La valeur de clé est null.");
            }

            var nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (nonNullableType.IsInstanceOfType(value))
            {
                return value;
            }

            return Convert.ChangeType(value, nonNullableType);
        }
    }

    /// <summary>
    /// Configuration settings for a <see cref="MongoDBService{TEntity}"/>.
    /// </summary>
    internal class MongoDBServiceSettings
    {
        /// <summary>Gets or sets the name of the MongoDB database. Defaults to <c>"BibliothequeDB"</c>.</summary>
        public string DatabaseName { get; set; } = "BibliothequeDB";

        /// <summary>Gets or sets the name of the MongoDB collection. Defaults to <c>"Ouvrages"</c>.</summary>
        public string CollectionName { get; set; } = "Ouvrages";

        /// <summary>Gets or sets the application name sent in the MongoDB connection string. Defaults to <c>"noCrudWA"</c>.</summary>
        public string AppName { get; set; } = "noCrudWA";

        /// <summary>Gets or sets the password used to authenticate with MongoDB.</summary>
        public string Password { get; set; } = "Empty";

        /// <summary>Gets or sets the MongoDB Atlas cluster host (without scheme or trailing slash).</summary>
        public string Host { get; set; } = "";

        /// <summary>Gets or sets the username used to authenticate with MongoDB.</summary>
        public string User { get; set; } = "";

        /// <summary>Gets or sets how long the in-memory cache is considered fresh before a re-read is triggered. Defaults to 30 seconds.</summary>
        public TimeSpan CacheStaleAfter { get; set; } = TimeSpan.FromSeconds(30);
    }
}
