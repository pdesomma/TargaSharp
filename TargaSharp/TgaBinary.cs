using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace TargaSharp
{
    /// <summary>
    /// Low-level little-endian binary helpers for TGA's fixed-width binary fields. Per spec, TGA
    /// integers are stored least-significant byte first, regardless of host endianness, so this
    /// type always reads/writes little-endian explicitly instead of relying on
    /// <see cref="System.BitConverter"/>'s host-endianness-dependent behavior.
    /// </summary>
    internal static class TgaBinary
    {
        /// <summary>
        /// Shared guard for fixed-size field constructors: <paramref name="bytes"/> must be non-null and exactly <paramref name="size"/> long.
        /// </summary>
        /// <param name="bytes">Field bytes to check.</param>
        /// <param name="size">Required length.</param>
        /// <param name="paramName">Caller's argument name for the exception; captured automatically.</param>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bytes"/>.Length != <paramref name="size"/>.</exception>
        internal static void RequireLength(byte[] bytes, int size, [CallerArgumentExpression(nameof(bytes))] string? paramName = null)
        {
            ArgumentNullException.ThrowIfNull(bytes, paramName);
            if (bytes.Length != size)
                throw new ArgumentOutOfRangeException(paramName, bytes.Length, $"Length must be {size}.");
        }

        /// <summary>
        /// Reads a little-endian <see cref="ushort"/> from <paramref name="source"/> at <paramref name="offset"/>.
        /// </summary>
        /// <param name="source">Source bytes.</param>
        /// <param name="offset">Zero-based byte offset to read from.</param>
        /// <returns>The decoded <see cref="ushort"/> value.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or fewer
        /// than 2 bytes are available in <paramref name="source"/> starting at <paramref name="offset"/>.</exception>
        internal static ushort ReadUInt16(ReadOnlySpan<byte> source, int offset)
        {
            if (offset < 0 || offset + sizeof(ushort) > source.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, $"{nameof(offset)} has wrong value!");

            return BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(offset));
        }

        /// <summary>
        /// Reads a little-endian <see cref="uint"/> from <paramref name="source"/> at <paramref name="offset"/>.
        /// </summary>
        /// <param name="source">Source bytes.</param>
        /// <param name="offset">Zero-based byte offset to read from.</param>
        /// <returns>The decoded <see cref="uint"/> value.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or fewer
        /// than 4 bytes are available in <paramref name="source"/> starting at <paramref name="offset"/>.</exception>
        internal static uint ReadUInt32(ReadOnlySpan<byte> source, int offset)
        {
            if (offset < 0 || offset + sizeof(uint) > source.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, $"{nameof(offset)} has wrong value!");

            return BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(offset));
        }

        /// <summary>
        /// Writes <paramref name="value"/> as 2 little-endian bytes into <paramref name="destination"/>
        /// at <paramref name="offset"/>.
        /// </summary>
        /// <param name="destination">Destination bytes.</param>
        /// <param name="offset">Zero-based byte offset to write to.</param>
        /// <param name="value">Value to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or there
        /// is not room for 2 bytes in <paramref name="destination"/> starting at <paramref name="offset"/>.</exception>
        internal static void WriteUInt16(Span<byte> destination, int offset, ushort value)
        {
            if (offset < 0 || offset + sizeof(ushort) > destination.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, $"{nameof(offset)} has wrong value!");

            BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset), value);
        }

        /// <summary>
        /// Writes <paramref name="value"/> as 4 little-endian bytes into <paramref name="destination"/>
        /// at <paramref name="offset"/>.
        /// </summary>
        /// <param name="destination">Destination bytes.</param>
        /// <param name="offset">Zero-based byte offset to write to.</param>
        /// <param name="value">Value to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or there
        /// is not room for 4 bytes in <paramref name="destination"/> starting at <paramref name="offset"/>.</exception>
        internal static void WriteUInt32(Span<byte> destination, int offset, uint value)
        {
            if (offset < 0 || offset + sizeof(uint) > destination.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, $"{nameof(offset)} has wrong value!");

            BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(offset), value);
        }
    }

    /// <summary>
    /// Allocation-light builder that concatenates <see cref="byte"/>, <see cref="ushort"/>,
    /// <see cref="uint"/> and raw byte array values (little-endian for the integer overloads, per
    /// TGA spec) into a single byte array. Replaces <c>BitConverterHelper.ToBytes(object[])</c>'s
    /// boxing/<c>object[]</c> surface with an explicit, strongly-typed sequence, so a wrong-typed
    /// argument is now a compile error instead of a runtime <see cref="ArgumentException"/>.
    /// </summary>
    internal sealed class TgaByteBuilder
    {
        /// <summary>
        /// Backing buffer.
        /// </summary>
        private readonly List<byte> _bytes;

        /// <summary>
        /// Creates a new <see cref="TgaByteBuilder"/>.
        /// </summary>
        /// <param name="capacityHint">Optional expected total byte count, used to size the internal
        /// buffer up front and avoid reallocation while appending.</param>
        internal TgaByteBuilder(int capacityHint = 0)
        {
            _bytes = new List<byte>(capacityHint);
        }

        /// <summary>
        /// Appends a single byte.
        /// </summary>
        /// <param name="value">Byte to append.</param>
        /// <returns>This builder, for chaining.</returns>
        internal TgaByteBuilder Add(byte value)
        {
            _bytes.Add(value);
            return this;
        }

        /// <summary>
        /// Appends a <see cref="ushort"/> as 2 little-endian bytes.
        /// </summary>
        /// <param name="value">Value to append.</param>
        /// <returns>This builder, for chaining.</returns>
        internal TgaByteBuilder Add(ushort value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            AddSpan(buffer);
            return this;
        }

        /// <summary>
        /// Appends a <see cref="uint"/> as 4 little-endian bytes.
        /// </summary>
        /// <param name="value">Value to append.</param>
        /// <returns>This builder, for chaining.</returns>
        internal TgaByteBuilder Add(uint value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            AddSpan(buffer);
            return this;
        }

        /// <summary>
        /// Appends raw bytes, or nothing when <paramref name="value"/> is <see langword="null"/>
        /// (mirrors <c>BitConverterHelper.ToBytes</c>'s null-element skip, relied on for optional
        /// trailing fields such as <see cref="TgaExtensionArea.OtherDataInExtensionArea"/>).
        /// </summary>
        /// <param name="value">Bytes to append, or <see langword="null"/> to add nothing.</param>
        /// <returns>This builder, for chaining.</returns>
        internal TgaByteBuilder Add(byte[]? value)
        {
            if (value is not null)
                _bytes.AddRange(value);
            return this;
        }

        /// <summary>
        /// Returns the concatenated bytes as a new array.
        /// </summary>
        /// <returns>The accumulated bytes.</returns>
        internal byte[] ToArray()
        {
            return _bytes.ToArray();
        }

        /// <summary>
        /// Appends the bytes of <paramref name="value"/> to <see cref="_bytes"/>. Extracted so the
        /// <see cref="ushort"/>/<see cref="uint"/> overloads (which stackalloc a small buffer) share
        /// one copy path instead of each calling <see cref="List{T}.AddRange"/> against a freshly
        /// materialized array.
        /// </summary>
        /// <param name="value">Bytes to append.</param>
        private void AddSpan(ReadOnlySpan<byte> value)
        {
            foreach (byte b in value)
                _bytes.Add(b);
        }
    }
}
