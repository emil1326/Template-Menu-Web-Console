using System;
using System.Linq;
using System.Reflection;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Static helper that discovers and caches the <see cref="IsIdAttribute"/> properties of <typeparamref name="TEntity"/> and extracts or compares key values at runtime.
    /// </summary>
    /// <typeparam name="TEntity">The entity type whose key properties are resolved via reflection.</typeparam>
    /// <remarks>
    /// Key properties are resolved once per type and cached in a static field.
    /// For single-key entities the key is returned as-is; for composite keys a <see cref="CompositeKey"/> is returned.
    /// </remarks>
    public static class EntityKeyResolver<TEntity>
    {
        private static readonly PropertyInfo[] KeyProperties = ResolveKeyProperties();

        /// <summary>
        /// Extracts the key value from an entity instance.
        /// </summary>
        /// <param name="entity">The entity to read the key from. Must not be <c>null</c>.</param>
        /// <returns>The key as an <see cref="object"/> for single-property keys, or a <see cref="CompositeKey"/> for multi-property keys.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the key property value is <c>null</c> at runtime.</exception>
        public static object GetKey(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            if (KeyProperties.Length == 1)
            {
                object? raw = KeyProperties[0].GetValue(entity);
                if (raw == null)
                {
                    throw new InvalidOperationException($"La clé de {typeof(TEntity).Name} est null.");
                }

                return raw;
            }

            object?[] parts = KeyProperties.Select(p => p.GetValue(entity)).ToArray();
            return new CompositeKey(parts);
        }

        /// <summary>
        /// Determines whether the key of an entity equals the given key value.
        /// </summary>
        /// <param name="entity">The entity whose key is compared. Returns <c>false</c> when <c>null</c>.</param>
        /// <param name="key">The key to compare against. Pass a <see cref="CompositeKey"/> for composite keys.</param>
        /// <returns><c>true</c> if the entity's key equals <paramref name="key"/>; otherwise <c>false</c>.</returns>
        public static bool KeysEqual(TEntity entity, object key)
        {
            if (entity == null) return false;

            var entityKey = GetKey(entity);
            return Equals(entityKey, key);
        }

        /// <summary>
        /// Validates that <typeparamref name="TEntity"/> has at least one <see cref="IsIdAttribute"/> property.
        /// </summary>
        /// <returns>A successful <see cref="Result"/> when the configuration is valid; a <see cref="ErrorCode.Configuration"/> failure otherwise.</returns>
        /// <remarks>Call this at construction time to fail fast rather than encountering a runtime exception during entity operations.</remarks>
        public static Result ValidateConfiguration()
        {
            try
            {
                _ = KeyProperties;
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(new AppError(ErrorCode.Configuration, ex.Message));
            }
        }

        private static PropertyInfo[] ResolveKeyProperties()
        {
            PropertyInfo[] properties = typeof(TEntity)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<IsIdAttribute>() != null)
                .OrderBy(p => p.GetCustomAttribute<IsIdAttribute>()?.Order ?? 0)
                .ToArray();

            if (properties.Length == 0)
            {
                throw new InvalidOperationException($"Aucune propriété [IsId] trouvée sur {typeof(TEntity).Name}.");
            }

            return properties;
        }
    }
}
