using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Service MongoDB générique basé sur IService<T>
    /// </summary>
    internal class MongoDBService<T> : IService<T> where T : class
    {
        private readonly IMongoCollection<T> _collection;
        private readonly Func<string, FilterDefinition<T>> _idFilter;

        public MongoDBService(
            MongoDBServiceSettings settings,
            Action? configureClassMaps = null,
            Func<string, FilterDefinition<T>>? idFilter = null
            )
        {
            ConfigureBsonMaps(configureClassMaps);

            var connectionString = $"mongodb+srv://emiliendevauchelle_db_user:{settings.Password}@nocrudwa.tyhfbfz.mongodb.net/?appName={settings.AppName}";
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _collection = database.GetCollection<T>(settings.CollectionName);
            _idFilter = idFilter ?? DefaultIdFilter;
        }

        private static void ConfigureBsonMaps(Action? configureClassMaps)
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(T)))
            {
                BsonClassMap.RegisterClassMap<T>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIsRootClass(true);
                });
            }

            configureClassMaps?.Invoke();
        }

        private static FilterDefinition<T> DefaultIdFilter(string id)
        {
            var prop = typeof(T).GetProperty("Id");
            if (prop == null)
                return Builders<T>.Filter.Where(_ => false);

            var targetType = prop.PropertyType;

            if (targetType == typeof(int) || targetType == typeof(int?))
            {
                if (int.TryParse(id, out var parsed))
                    return Builders<T>.Filter.Eq("Id", parsed);

                return Builders<T>.Filter.Where(_ => false);
            }

            return Builders<T>.Filter.Eq("Id", id);
        }

        public List<T> LoadAllAsync() => GetAll();

        public void SaveAll(List<T> items)
        {
            var docs = items ?? [];

            _collection.DeleteMany(Builders<T>.Filter.Empty);

            if (docs.Count > 0)
            {
                _collection.InsertMany(docs);
            }
        }

        public void Save(T item)
        {
            if (item == null)
                return;

            _collection.InsertOne(item);
        }

        public void Delete(string id)
        {
            _collection.DeleteOne(_idFilter(id));
        }

        public void Delete(T item)
        {
            if (item == null)
                return;

            var idValue = GetIdValue(item);
            if (idValue != null)
                Delete(idValue);
        }

        public void Update(string id)
        {
            var item = GetById(id);
            if (item != null)
            {
                Update(item);
            }
        }

        public void Update(T item)
        {
            if (item == null)
                return;

            var idValue = GetIdValue(item);
            if (idValue == null)
                return;

            _collection.ReplaceOne(_idFilter(idValue), item, new ReplaceOptions { IsUpsert = true });
        }

        public T? GetById(string id)
        {
            return _collection.Find(_idFilter(id)).FirstOrDefault();
        }

        public List<T> GetAll()
        {
            return _collection.Find(Builders<T>.Filter.Empty).ToList();
        }

        public List<T> GetByQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return GetAll();

            // Le filtrage avancé est géré par RepositoryOuvrages ou par l'appelant
            return GetAll();
        }

        public List<T> GetByType(string type)
        {
            var all = GetAll();

            if (string.IsNullOrWhiteSpace(type))
                return all;

            return [.. all.Where(o => o?.GetType().Name.Contains(type, StringComparison.OrdinalIgnoreCase) == true)];
        }

        private static string? GetIdValue(T item)
        {
            var prop = typeof(T).GetProperty("Id");
            var value = prop?.GetValue(item);
            return value?.ToString();
        }
    }

    internal class MongoDBServiceSettings
    {
        public string DatabaseName { get; set; } = "BibliothequeDB";
        public string CollectionName { get; set; } = "Ouvrages";
        public string AppName { get; set; } = "noCrudWA";
        public string Password { get; set; } = "Empty";
    }
}
