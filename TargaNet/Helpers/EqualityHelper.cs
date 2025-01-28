namespace TargaNet.Helpers
{
    /// <summary>
    /// Simplify equality operator
    /// </summary>
    internal static class EqualityHelper
    {
        /// <summary>
        /// Simplify repetitive equality stuff. 
        /// </summary>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        public static bool Operator<T>(T item1, T item2) where T : IEquatable<T>
        {
            if (item1 is null) return item2 is null;
            if (item2 is null) return false;
            return item1.Equals(item2);
        }
    }
}
