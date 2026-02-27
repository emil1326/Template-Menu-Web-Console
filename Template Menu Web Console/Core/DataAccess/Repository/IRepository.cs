using System.Collections.Generic;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Defines the contract for a domain repository that exposes CRUD operations and persistence sync over a single entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type managed by this repository.</typeparam>
    public interface IRepository<TEntity>
    {
        /// <summary>Gets the current cached list of entities from the underlying service.</summary>
        IReadOnlyList<TEntity> Items { get; }

        /// <summary>
        /// Returns all entities, optionally using the service cache.
        /// </summary>
        /// <param name="useCache">When <c>true</c>, returns the cached list if it is not stale.</param>
        /// <returns>A <see cref="Result{T}"/> containing the full entity list.</returns>
        Result<List<TEntity>> GetAll(bool useCache = true);

        /// <summary>
        /// Retrieves a single entity by its key.
        /// </summary>
        /// <param name="id">The key value. Use a <see cref="CompositeKey"/> for multi-property keys.</param>
        /// <param name="useCache">When <c>true</c>, searches the cache before hitting the data source.</param>
        /// <returns>A <see cref="Result{T}"/> containing the entity, or <c>null</c> if not found.</returns>
        Result<TEntity?> GetById(object id, bool useCache = true);

        /// <summary>
        /// Adds a new entity to the data source.
        /// </summary>
        /// <param name="item">The entity to add.</param>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        Result Add(TEntity item);

        /// <summary>
        /// Replaces an existing entity identified by its <see cref="IsIdAttribute"/> key.
        /// </summary>
        /// <param name="item">The updated entity. Its key must already exist.</param>
        /// <returns>A <see cref="Result"/> indicating success, or <see cref="ErrorCode.NotFound"/> if the key is absent.</returns>
        Result Update(TEntity item);

        /// <summary>
        /// Replaces the entity at the given key with a new item, using delete then add.
        /// </summary>
        /// <param name="id">The key of the entity to replace.</param>
        /// <param name="item">The replacement entity to add after deletion.</param>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        Result UpdateById(object id, TEntity item);

        /// <summary>
        /// Deletes the given entity from the data source using its <see cref="IsIdAttribute"/> key.
        /// </summary>
        /// <param name="item">The entity to delete.</param>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        Result Delete(TEntity item);

        /// <summary>
        /// Deletes the entity with the specified key from the data source.
        /// </summary>
        /// <param name="id">The key of the entity to delete.</param>
        /// <returns>A <see cref="Result"/> indicating success, or <see cref="ErrorCode.NotFound"/> if the key is absent.</returns>
        Result DeleteById(object id);

        /// <summary>
        /// Persists the current service cache to the data source.
        /// </summary>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        Result WriteAllToDataSource();

        /// <summary>
        /// Forces a reload from the data source, discarding the current service cache.
        /// </summary>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        Result ReadAllFromDataSource();
    }
}
