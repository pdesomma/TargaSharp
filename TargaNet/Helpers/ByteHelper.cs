namespace TargaNet.Helpers
{
    /// <summary>
    /// Simplify ByteConversion operations, like concatenation of byte arrays, comparing and other.
    /// </summary>
    internal static class ByteHelper
    {
        /// <summary>
        /// Combine byte, byte[], (u)short, (u)int, (u)long values to byte[] array.
        /// </summary>
        /// <param name="objArr">Array of byte, byte[], (u)short, (u)int, (u)long values.</param>
        /// <returns>Array of bytes, null when some object is null.</returns>
        public static byte[]? ToBytes(params object?[] objArr)
        {
            if (objArr is null) return null;

            var bytesList = new List<byte>();
            foreach (var item in objArr)
            {
                if (item is null) continue;
                else if (item is byte b) bytesList.Add(b);
                else if (item is byte[] subBytes) bytesList.AddRange(subBytes);
                else if (item is short s) bytesList.AddRange(BitConverter.GetBytes(s));
                else if (item is ushort us) bytesList.AddRange(BitConverter.GetBytes(us));
                else if (item is int i) bytesList.AddRange(BitConverter.GetBytes(i));
                else if (item is uint ui) bytesList.AddRange(BitConverter.GetBytes(ui));
                else if (item is long l) bytesList.AddRange(BitConverter.GetBytes(l));
                else if (item is ulong ul) bytesList.AddRange(BitConverter.GetBytes(ul));
            }
            return [.. bytesList];
        }
    }
}
