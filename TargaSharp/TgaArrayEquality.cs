namespace TargaSharp
{
    /// <summary>
    /// Value equality and hashing for the optional array fields of the record types, which the
    /// compiler-synthesized record members would otherwise compare by reference.
    /// </summary>
    internal static class TgaArrayEquality
    {
        /// <summary>
        /// Element-wise equality; two <see langword="null"/>s are equal, a <see langword="null"/> and an array are not.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="a">First array.</param>
        /// <param name="b">Second array.</param>
        /// <returns><see langword="true"/> when both are the same reference or hold the same elements in order.</returns>
        internal static bool Equals<T>(T[]? a, T[]? b) where T : IEquatable<T> =>
            ReferenceEquals(a, b) || (a is not null && b is not null && a.AsSpan().SequenceEqual(b));

        /// <summary>
        /// Folds every element into <paramref name="hash"/> with the given multiplier; a <see langword="null"/> array leaves it unchanged.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="hash">Running hash.</param>
        /// <param name="array">Array to fold in.</param>
        /// <param name="multiplier">Per-element multiplier (matches the caller's scalar fields).</param>
        /// <returns>Updated hash.</returns>
        internal static int Hash<T>(int hash, T[]? array, int multiplier = 13) where T : notnull
        {
            if (array is null) return hash;
            unchecked
            {
                for (int i = 0; i < array.Length; i++)
                    hash = (multiplier * hash) + array[i].GetHashCode();
            }
            return hash;
        }
    }
}
