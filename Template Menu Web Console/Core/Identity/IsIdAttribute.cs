using System;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Marks a property as the entity key used by <see cref="EntityKeyResolver{TEntity}"/> and data source services.
    /// </summary>
    /// <remarks>
    /// Apply to one property for a single key, or to multiple properties with distinct <see cref="Order"/> values for a composite key.
    /// Entities without at least one <see cref="IsIdAttribute"/> property will cause an <see cref="System.InvalidOperationException"/> at repository or service construction.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class IsIdAttribute : Attribute
    {
        /// <summary>Gets or sets the zero-based position of this property within a composite key. Ignored for single-property keys.</summary>
        public int Order { get; set; }
    }
}
