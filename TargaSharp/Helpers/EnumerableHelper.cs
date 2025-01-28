using System;
using System.Collections.Generic;
using System.Linq;

namespace TargaSharp.Helpers
{
    /// <summary>
    /// Simplify common enumerable operations like concatenation, comparing and other.
    /// </summary>
    internal static class EnumerableHelper
    {
        /// <summary>
        /// Copies a range of elements from an Array starting at the specified source index.
        /// The length and the index are specified as 32-bit integers.
        /// </summary>
        /// <param name="items">The enumerable that contains the data to copy.</param>
        /// <param name="offset">A 32-bit integer that represents the index in
        /// <see cref="items"/> at which copying begins.</param>
        /// <param name="count">A 32-bit integer that represents the number of elements to copy.</param>
        /// <returns></returns>
        public static T[] GetElements<T>(IEnumerable<T> items, uint offset, uint count)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(items);
#else 
            if (items is null) throw new ArgumentNullException(nameof(items));
#endif
            return items.Skip((int)offset).Take((int)count).ToArray();
        }

        /// <summary>
        /// Compare two enumerables.
        /// </summary>
        /// <param name="enumerable1">First enumerable.</param>
        /// <param name="enumerable2">Second enumerable.</param>
        /// <returns>True, if enumerables are both null or sequentially equal.</returns>
        public static bool AreEnumerablesEqual<T>(IEnumerable<T>? enumerable1, IEnumerable<T>? enumerable2)
        {
            if (ReferenceEquals(enumerable1, enumerable2)) return true;
            if (enumerable1 == default || enumerable2 == default) return false;

            return enumerable1.SequenceEqual(enumerable2);
        }

        /// <summary>
        /// Compare elements in one Array with different offsets.
        /// </summary>
        /// <param name="enumerable">Some Array.</param>
        /// <param name="offset1">First offset.</param>
        /// <param name="offset2">Second offset.</param>
        /// <param name="count">Elements count which must be compared.</param>
        /// <returns></returns>
        public static bool AreElementsEqual<T>(IEnumerable<T> enumerable, int offset1, int offset2, int count)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(enumerable);
#else 
            if (enumerable is null) throw new ArgumentNullException(nameof(enumerable));
#endif
            return enumerable.Skip(offset1).Take(count).SequenceEqual(enumerable.Skip(offset2).Take(count));
        }
    }
}
