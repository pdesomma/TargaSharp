using System.Diagnostics.CodeAnalysis;

namespace TargaSharp
{
    /// <summary>
    /// Simplify ByteConversion operations, like concatination of byte arrays, comparing and other.
    /// </summary>
    public static class BitConverterHelper
    {
        /// <summary>
        /// Combine byte, byte[], (u)short, (u)int, (u)long values to byte[] array. Null elements
        /// are skipped (not written), so optional trailing fields can be passed through as-is.
        /// </summary>
        /// <param name="obj">Array of byte, byte[], (u)short, (u)int, (u)long values. Null elements are skipped.</param>
        /// <returns>Array of bytes, null when <paramref name="obj"/> itself is null.</returns>
        /// <exception cref="ArgumentException">An element of <paramref name="obj"/> is a non-null value
        /// of a type that is not one of byte, <see cref="IEnumerable{T}"/> of byte, short, ushort, int, uint, long or ulong.</exception>
        [return: NotNullIfNotNull(nameof(obj))]
        public static byte[]? ToBytes(params object[] obj)
        {
            if (obj is null) return null;

            var bytesList = new List<byte>();
            for (int index = 0; index < obj.Length; index++)
            {
                switch (obj[index])
                {
                    case byte b:
                        bytesList.Add(b);
                        break;
                    case IEnumerable<byte> bytes:
                        bytesList.AddRange(bytes);
                        break;
                    case short s:
                        bytesList.AddRange(BitConverter.GetBytes(s));
                        break;
                    case ushort us:
                        bytesList.AddRange(BitConverter.GetBytes(us));
                        break;
                    case int i:
                        bytesList.AddRange(BitConverter.GetBytes(i));
                        break;
                    case uint ui:
                        bytesList.AddRange(BitConverter.GetBytes(ui));
                        break;
                    case long l:
                        bytesList.AddRange(BitConverter.GetBytes(l));
                        break;
                    case ulong ul:
                        bytesList.AddRange(BitConverter.GetBytes(ul));
                        break;
                    case null:
                        // Null elements are intentionally skipped (e.g. optional trailing
                        // fields such as TgaExtArea.OtherDataInExtensionArea rely on this).
                        break;
                    default:
                        throw new ArgumentException($"Unsupported type {obj[index].GetType()} at index {index}.", nameof(obj));
                }
            }
            return bytesList.ToArray();
        }

        /// <summary>
        /// Copies a range of elements from an Array starting at the specified source index.
        /// The length and the index are specified as 32-bit integers.
        /// </summary>
        /// <param name="arr">The <see cref="Array"/> that contains the data to copy.</param>
        /// <param name="offset">A 32-bit integer that represents the index in <paramref name="arr"/> at which copying begins.</param>
        /// <param name="count">A 32-bit integer that represents the number of elements to copy.</param>
        /// <returns>A new array containing exactly <paramref name="count"/> elements copied from <paramref name="arr"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="arr"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative or greater than or equal to the length of <paramref name="arr"/>, or <paramref name="count"/> is not positive, or <paramref name="offset"/> plus <paramref name="count"/> exceeds the length of <paramref name="arr"/>.</exception>
        public static T[] GetElements<T>(T[] arr, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(arr);

            if (offset < 0 || offset >= arr.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, $"{nameof(offset)} has wrong value!");

            if (count <= 0 || offset + count > arr.Length)
                throw new ArgumentOutOfRangeException(nameof(count), count, $"{nameof(count)} has wrong value!");

            return arr.AsSpan(offset, count).ToArray();
        }

        /// <summary>
        /// Compare N-dimensional Arrays.
        /// </summary>
        /// <typeparam name="T">Arrays Type.</typeparam>
        /// <param name="array1">First Array.</param>
        /// <param name="array2">Second Array.</param>
        /// <returns>True, if Arrays are equal.</returns>
        public static bool IsArraysEqual<T>(T[] array1, T[] array2)
        {
            if (ReferenceEquals(array1, array2)) return true;
            if (array1 is null || array2 is null) return false;
            if (array1.Length != array2.Length) return false;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < array1.Length; i++)
                if (!comparer.Equals(array1[i], array2[i]))
                    return false;
            return true;
        }

        /// <summary>
        /// Compare Lists.
        /// </summary>
        /// <typeparam name="T">List Type.</typeparam>
        /// <param name="list1">First List.</param>
        /// <param name="list2">Second List.</param>
        /// <returns>True, if Lists are equal.</returns>
        public static bool IsListsEqual<T>(List<T> list1, List<T> list2)
        {
            if (ReferenceEquals(list1, list2)) return true;
            if (list1 is null || list2 is null) return false;
            if (list1.Count != list2.Count) return false;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < list1.Count; i++)
                if (!comparer.Equals(list1[i], list2[i]))
                    return false;
            return true;
        }

        /// <summary>
        /// Compare elements in one Array with different offsets.
        /// </summary>
        /// <typeparam name="T">Array type.</typeparam>
        /// <param name="arr">Some Array.</param>
        /// <param name="offset1">First offset.</param>
        /// <param name="offset2">Second offset.</param>
        /// <param name="count">Elements count which must be compared.</param>
        /// <returns>True, if the specified ranges of elements are equal.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="arr"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset1"/> or <paramref name="offset2"/> is negative or greater than or equal to the length of <paramref name="arr"/>, or <paramref name="count"/> is not positive, or either offset plus <paramref name="count"/> exceeds the length of <paramref name="arr"/>.</exception>
        public static bool IsElementsEqual<T>(T[] arr, int offset1, int offset2, int count)
        {
            ArgumentNullException.ThrowIfNull(arr);

            if (offset1 < 0 || offset1 >= arr.Length)
                throw new ArgumentOutOfRangeException(nameof(offset1), offset1, $"{nameof(offset1)} has wrong value!");

            if (offset2 < 0 || offset2 >= arr.Length)
                throw new ArgumentOutOfRangeException(nameof(offset2), offset2, $"{nameof(offset2)} has wrong value!");

            if (count <= 0 || offset1 + count > arr.Length || offset2 + count > arr.Length)
                throw new ArgumentOutOfRangeException(nameof(count), count, $"{nameof(count)} has wrong value!");

            if (offset1 == offset2) return true;

            return IsArraysEqual(GetElements(arr, offset1, count), GetElements(arr, offset2, count));
        }
    }
}
