using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// An <see cref="IService{TEntity}"/> implementation that persists entities as a JSON array on disk and maintains an in-memory cache with a configurable stale duration.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to persist. Must have at least one <see cref="IsIdAttribute"/> property.</typeparam>
    /// <remarks>
    /// All settings are exposed via the mutable <see cref="Settings"/> property; changes take effect on the next operation.
    /// The JSON file is written with <see cref="JsonFileServiceSettings.JsonFormatting"/> and is created automatically when
    /// <see cref="JsonFileServiceSettings.CreateFileIfMissing"/> is <c>true</c>.
    /// </remarks>
    public class JsonFileService<TEntity> : IService<TEntity>
    {
        private List<TEntity> cache = [];
        private DateTime lastRefreshUtc = DateTime.MinValue;

        /// <summary>Gets or sets the configuration for this service. Changes take effect on the next read or write operation.</summary>
        public JsonFileServiceSettings Settings { get; set; }

        /// <summary>
        /// Initialises the service with the given settings and validates the entity key configuration.
        /// </summary>
        /// <param name="settings">The file and cache settings. Must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="TEntity"/> has no <see cref="IsIdAttribute"/> property.</exception>
        public JsonFileService(JsonFileServiceSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));

            var configValidation = EntityKeyResolver<TEntity>.ValidateConfiguration();
            if (!configValidation.IsSuccess)
            {
                // propagate configuration failure as AppError
                throw new AppError(ErrorCode.Configuration,
                    configValidation.Error?.Message ?? "Invalid entity key configuration.",
                    configValidation.Error);
            }
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
                var loaded = EnsureCache(useCache: true);
                if (!loaded.IsSuccess)
                {
                    return loaded;
                }

                cache.Add(item);
                return PersistCache();
            }
            catch (Exception ex)
            {
                return Result.Failure(new AppError(ErrorCode.DataSource, ex.Message, ex));
            }
        }

        public Result Update(TEntity item)
        {
            try
            {
                var loaded = EnsureCache(useCache: true);
                if (!loaded.IsSuccess)
                {
                    return loaded;
                }

                var key = EntityKeyResolver<TEntity>.GetKey(item);
                int index = cache.FindIndex(existing => EntityKeyResolver<TEntity>.KeysEqual(existing, key));
                if (index < 0)
                {
                    return Result.Failure(new AppError(ErrorCode.NotFound, $"Entity with key '{key}' not found."));
                }

                cache[index] = item;
                return PersistCache();
            }
            catch (Exception ex)
            {
                return Result.Failure(new AppError(ErrorCode.DataSource, ex.Message, ex));
            }
        }

        public Result DeleteById(object id)
        {
            try
            {
                var loaded = EnsureCache(useCache: true);
                if (!loaded.IsSuccess)
                {
                    return loaded;
                }

                int removedCount = cache.RemoveAll(existing => EntityKeyResolver<TEntity>.KeysEqual(existing, id));
                if (removedCount == 0)
                {
                    return Result.Failure(new AppError(ErrorCode.NotFound, $"Entity with key '{id}' not found."));
                }

                return PersistCache();
            }
            catch (Exception ex)
            {
                return Result.Failure(new AppError(ErrorCode.DataSource, ex.Message, ex));
            }
        }

        public Result Delete(TEntity item)
        {
            var key = EntityKeyResolver<TEntity>.GetKey(item);
            return DeleteById(key);
        }

        public Result WriteAll(List<TEntity> items)
        {
            cache = items ?? [];
            return PersistCache();
        }

        public Result RefreshCache()
        {
            return EnsureCache(useCache: false);
        }

        private Result PersistCache()
        {
            try
            {
                string json = JsonConvert.SerializeObject(cache, Settings.JsonFormatting);
                File.WriteAllText(Settings.FilePath, json);
                lastRefreshUtc = DateTime.UtcNow;
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(new AppError(ErrorCode.DataSource, ex.Message, ex));
            }
        }

        private Result<List<TEntity>> EnsureCache(bool useCache)
        {
            try
            {
                if (useCache && cache.Count > 0 && !IsCacheStale)
                {
                    return Result<List<TEntity>>.Success(new List<TEntity>(cache));
                }

                if (!File.Exists(Settings.FilePath))
                {
                    if (Settings.CreateFileIfMissing)
                    {
                        File.WriteAllText(Settings.FilePath, "[]");
                    }

                    cache = [];
                    lastRefreshUtc = DateTime.UtcNow;
                    return Result<List<TEntity>>.Success([]);
                }

                string json = File.ReadAllText(Settings.FilePath);
                cache = JsonConvert.DeserializeObject<List<TEntity>>(json) ?? [];
                lastRefreshUtc = DateTime.UtcNow;
                return Result<List<TEntity>>.Success(new List<TEntity>(cache));
            }
            catch (Exception ex)
            {
                return Result<List<TEntity>>.Failure(new AppError(ErrorCode.DataSource, ex.Message, ex));
            }
        }
    }

    /// <summary>
    /// Configuration settings for a <see cref="JsonFileService{TEntity}"/>.
    /// </summary>
    public class JsonFileServiceSettings
    {
        /// <summary>Gets or sets the path of the JSON file used for persistence. Required; must not be empty.</summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>Gets or sets how long the in-memory cache is considered fresh before a re-read is triggered. Defaults to 5 minutes.</summary>
        public TimeSpan CacheStaleAfter { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>Gets or sets the JSON formatting applied when writing the file. Defaults to <see cref="Formatting.Indented"/>.</summary>
        public Formatting JsonFormatting { get; set; } = Formatting.Indented;

        /// <summary>Gets or sets a value indicating whether the JSON file should be created automatically when it does not exist. Defaults to <c>true</c>.</summary>
        public bool CreateFileIfMissing { get; set; } = true;
    }
}
