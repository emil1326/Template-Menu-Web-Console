using System;
using System.Collections.Generic;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Base class for all domain repositories. Delegates every CRUD operation to an <see cref="IService{TEntity}"/> and exposes the service cache as <see cref="Items"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type managed by this repository. Must have at least one property annotated with <see cref="IsIdAttribute"/>.</typeparam>
    /// <remarks>
    /// All virtual methods can be overridden in derived repositories to add domain-specific behaviour.
    /// The repository holds no local list; <see cref="Items"/> reads directly from the service cache.
    /// </remarks>
    public abstract class RepositoryBase<TEntity> : IRepository<TEntity>
    {
        /// <summary>The underlying data source service used for all persistence and cache operations.</summary>
        protected readonly IService<TEntity> service;

        /// <summary>
        /// Initialises the repository with the given service and validates the entity key configuration.
        /// </summary>
        /// <param name="service">The data source service. Must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="TEntity"/> has no <see cref="IsIdAttribute"/> property.</exception>
        protected RepositoryBase(IService<TEntity> service)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));

            var configValidation = EntityKeyResolver<TEntity>.ValidateConfiguration();
            if (!configValidation.IsSuccess)
            {
                // configuration check failure – raise as AppError
                throw new AppError(ErrorCode.Configuration,
                    configValidation.Error?.Message ?? "Invalid entity key configuration.",
                    configValidation.Error);
            }
        }

        /// <inheritdoc/>
        /// <remarks>Reads directly from the service cache on every access; no local copy is maintained.</remarks>
        public IReadOnlyList<TEntity> Items => service.ReadAll(useCache: true).Value ?? [];

        /// <inheritdoc/>
        public virtual Result<List<TEntity>> GetAll(bool useCache = true)
        {
            return service.ReadAll(useCache);
        }

        /// <inheritdoc/>
        public virtual Result<TEntity?> GetById(object id, bool useCache = true)
        {
            return service.GetById(id, useCache);
        }

        /// <inheritdoc/>
        public virtual Result Add(TEntity item)
        {
            return service.Add(item);
        }

        /// <inheritdoc/>
        public virtual Result Update(TEntity item)
        {
            return service.Update(item);
        }

        /// <inheritdoc/>
        /// <remarks>Implemented as <see cref="DeleteById"/> followed by <see cref="Add"/>; not atomic.</remarks>
        public virtual Result UpdateById(object id, TEntity item)
        {
            var deleteResult = DeleteById(id);
            if (!deleteResult.IsSuccess)
            {
                return deleteResult;
            }

            return Add(item);
        }

        /// <inheritdoc/>
        public virtual Result Delete(TEntity item)
        {
            return service.Delete(item);
        }

        /// <inheritdoc/>
        /// <remarks>Resolves the entity from the cache via <see cref="IService{TEntity}.GetById"/> before delegating to <see cref="Delete"/>.</remarks>
        public virtual Result DeleteById(object id)
        {
            var found = service.GetById(id, useCache: true);
            if (!found.IsSuccess)
            {
                return Result.Failure(found.Error!);
            }

            if (found.Value is null)
            {
                return Result.Failure(new AppError(ErrorCode.NotFound, $"Entity with key '{id}' not found."));
            }

            return Delete(found.Value);
        }

        /// <inheritdoc/>
        public virtual Result WriteAllToDataSource()
        {
            var current = service.ReadAll(useCache: true);
            if (!current.IsSuccess)
            {
                return Result.Failure(current.Error!);
            }

            return service.WriteAll(current.Value ?? []);
        }

        /// <inheritdoc/>
        public virtual Result ReadAllFromDataSource()
        {
            return service.RefreshCache();
        }
    }
}
