using System;
using System.Collections.Generic;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Represents an ordered set of key values for entities that have multiple <see cref="IsIdAttribute"/> properties.
    /// Implements value equality based on the ordered sequence of parts.
    /// </summary>
    public sealed class CompositeKey : IEquatable<CompositeKey>
    {
        /// <summary>Gets the ordered list of individual key values that make up this composite key.</summary>
        public IReadOnlyList<object?> Parts { get; }

        /// <summary>
        /// Initialises a new <see cref="CompositeKey"/> with the given ordered parts.
        /// </summary>
        /// <param name="parts">The key values in declaration order (matching <see cref="IsIdAttribute.Order"/>). Must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parts"/> is <c>null</c>.</exception>
        public CompositeKey(params object?[] parts)
        {
            Parts = parts ?? throw new ArgumentNullException(nameof(parts));
        }

        public bool Equals(CompositeKey? other)
        {
            if (other is null || Parts.Count != other.Parts.Count)
            {
                return false;
            }

            for (int i = 0; i < Parts.Count; i++)
            {
                if (!object.Equals(Parts[i], other.Parts[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj) => obj is CompositeKey other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var part in Parts)
            {
                hash.Add(part);
            }

            return hash.ToHashCode();
        }
    }
}
