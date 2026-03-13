using System;
using System.Collections.Generic;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Defines the contract for a data source service that provides CRUD operations and an in-memory cache over a single entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type managed by this service.</typeparam>
    public interface IService<TEntity>
    {
        /// <summary>Gets the duration after which the in-memory cache is considered stale.</summary>
        TimeSpan CacheStaleAfter { get; }

        /// <summary>Gets a value indicating whether the in-memory cache has exceeded <see cref="CacheStaleAfter"/>.</summary>
        bool IsCacheStale { get; }

        /// <summary>Gets a snapshot of the current in-memory cache without triggering any data source operation.</summary>
        IReadOnlyList<TEntity> CachedItems { get; }

        /// <summary>
        /// Reads all entities, optionally returning the cached copy.
        /// </summary>
        /// <param name="useCache">When <c>true</c>, returns the cached list if it is not stale; otherwise reloads from the data source.</param>
        /// <returns>A <see cref="Result{T}"/> containing the full entity list, or a failure with an <see cref="AppError"/>.</returns>
        Result<List<TEntity>> ReadAll(bool useCache = true);

        /// <summary>
        /// Retrieves a single entity by its key.
        /// </summary>
        /// <param name="id">The key value, matching the property annotated with <see cref="IsIdAttribute"/>. Use a <see cref="CompositeKey"/> for multi-property keys.</param>
        /// <param name="useCache">When <c>true</c>, searches the cache before hitting the data source.</param>
        /// <returns>A <see cref="Result{T}"/> containing the entity, or <c>null</c> if no match was found.</returns>
        Result<TEntity?> GetById(object id, bool useCache = true);

        /// <summary>
        /// Adds a new entity to the data source and updates the cache.
        /// </summary>
        /// <param name="item">The entity to add. Must not be <c>null</c>.</param>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        Result Add(TEntity item);

        /// <summary>
        /// Replaces an existing entity in the data source identified by its <see cref="IsIdAttribute"/> key.
        /// </summary>
        /// <param name="item">The updated entity. Its key must already exist in the data source.</param>
        /// <returns>A <see cref="Result"/> indicating success, or <see cref="ErrorCode.NotFound"/> if the key is absent.</returns>
        Result Update(TEntity item);

        /// <summary>
        /// Deletes the entity with the specified key from the data source and removes it from the cache.
        /// </summary>
        /// <param name="id">The key of the entity to delete.</param>
        /// <returns>A <see cref="Result"/> indicating success, or <see cref="ErrorCode.NotFound"/> if the key is absent.</returns>
        Result DeleteById(object id);

        /// <summary>
        /// Deletes the given entity from the data source using its <see cref="IsIdAttribute"/> key.
        /// </summary>
        /// <param name="item">The entity to delete.</param>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        Result Delete(TEntity item);

        /// <summary>
        /// Replaces all entities in the data source with the provided list and refreshes the cache.
        /// </summary>
        /// <param name="items">The complete list to persist. Pass an empty list to clear the data source.</param>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        Result WriteAll(List<TEntity> items);

        /// <summary>
        /// Forces an immediate reload from the data source, discarding the current cache.
        /// </summary>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        Result RefreshCache();
    }
}
