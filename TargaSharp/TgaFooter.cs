using System.Diagnostics.CodeAnalysis;

namespace TargaSharp
{
    /// <summary>
    /// File Footer Area
    /// </summary>
    public sealed record TgaFooter : ICloneable
    {
        /// <summary>
        /// Gets TGA Footer Section size in bytes.
        /// </summary>
        public const int Size = 26;

        /// <summary>
        /// Make NewXFile format TGA Footer with <see cref="ExtensionAreaOffset"/> = 0 and
        /// <see cref="DeveloperDirectoryOffset"/> = 0.
        /// </summary>
        public TgaFooter() { }

        /// <summary>
        /// Make <see cref="TgaFooter"/> from values.
        /// </summary>
        /// <param name="extOff">Extension Area Offset, offset from the beginning of the file.</param>
        /// <param name="devDirOff">Developer Directory Offset, offset from the beginning of the file.</param>
        public TgaFooter(uint extOff, uint devDirOff)
        {
            ExtensionAreaOffset = extOff;
            DeveloperDirectoryOffset = devDirOff;
        }

        /// <summary>
        /// Make <see cref="TgaFooter"/> from bytes (if the signature is right). Delegates to
        /// <see cref="TryParse"/>; prefer that method when a non-throwing check is wanted (e.g. while
        /// probing a stream for a v2.0 footer).
        /// </summary>
        /// <param name="bytes">Bytes array (byte[26]).</param>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException"><paramref name="bytes"/> is not a valid TGA v2.0 footer
        /// (wrong length, signature, reserved character or binary-zero terminator).</exception>
        public TgaFooter(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);

            if (!TryParse(bytes, out TgaFooter? footer))
                throw new FormatException("Bytes do not represent a valid TGA v2.0 footer.");

            ExtensionAreaOffset = footer!.ExtensionAreaOffset;
            DeveloperDirectoryOffset = footer.DeveloperDirectoryOffset;
        }

        /// <summary>
        /// Byte 4-7 - Developer Directory Offset - Field 29
        /// The next four bytes(bytes 4-7, the second LONG) contain an offset from the
        /// beginning of the file to the start of the Developer Directory. If the Developer
        /// Directory Offset is zero, then the Developer Area does not exist. This is a derived
        /// field: it is computed by <see cref="TargaSharp.IO.TgaWriter"/> during layout (or read
        /// from the file by <see cref="TargaSharp.IO.TgaReader"/>), so consumers cannot set it directly.
        /// </summary>
        public uint DeveloperDirectoryOffset { get; internal set; } = 0;

        /// <summary>
        /// Byte 0-3 - Extension Area Offset - Field 28
        /// The first four bytes (bytes 0-3, the first LONG) of the TGA File Footer contain an
        /// offset from the beginning of the file to the start of the Extension Area. Simply
        /// SEEK to this location to position to the start of the Extension Area. If the
        /// Extension Area Offset is zero, no Extension Area exists in the file. This is a derived
        /// field: it is computed by <see cref="TargaSharp.IO.TgaWriter"/> during layout (or read
        /// from the file by <see cref="TargaSharp.IO.TgaReader"/>), so consumers cannot set it directly.
        /// </summary>
        public uint ExtensionAreaOffset { get; internal set; } = 0;

        /// <summary>
        /// Make full copy of <see cref="TgaFooter"/>. Named <c>Copy</c> rather than
        /// <c>Clone</c> because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns>Full independent copy of <see cref="TgaFooter"/>.</returns>
        public TgaFooter Copy() => this with { };

        /// <summary>
        /// Attempts to parse <paramref name="bytes"/> as a TGA v2.0 File Footer. Validates the fixed
        /// spec constants that make up the footer alongside the two offsets - the
        /// <see cref="TgaString.XFileSignatureText"/> signature (Field 30), the '.' reserved
        /// character (Field 31) and the binary-zero string terminator (Field 32) - since a file is
        /// only assumed to be in the New TGA format when all three match.
        /// </summary>
        /// <param name="bytes">Bytes array to parse (must be exactly <see cref="Size"/> (26) bytes long).</param>
        /// <param name="footer">The parsed <see cref="TgaFooter"/> on success; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="bytes"/> is a valid TGA v2.0 footer; otherwise <see langword="false"/>.</returns>
        public static bool TryParse(byte[]? bytes, [NotNullWhen(true)] out TgaFooter? footer)
        {
            footer = null;
            if (bytes is null || bytes.Length != Size)
                return false;

            for (int i = 0; i < TgaString.XFileSignatureText.Length; i++)
                if (bytes[8 + i] != (byte)TgaString.XFileSignatureText[i])
                    return false;

            if (bytes[24] != (byte)'.')
                return false;

            if (bytes[25] != 0)
                return false;

            footer = new TgaFooter
            {
                ExtensionAreaOffset = TgaBinary.ReadUInt32(bytes, 0),
                DeveloperDirectoryOffset = TgaBinary.ReadUInt32(bytes, 4),
            };
            return true;
        }

        /// <summary>
        /// Convert <see cref="TgaFooter"/> to byte array. The signature, reserved character and
        /// binary-zero terminator are fixed TGA v2.0 spec constants (not data on this instance) and
        /// are always written as "TRUEVISION-XFILE", '.' and '\0' respectively.
        /// </summary>
        /// <returns>Byte array with size equal <see cref="Size"/>.</returns>
        public byte[] ToBytes() => new TgaByteBuilder(Size)
            .Add(ExtensionAreaOffset)
            .Add(DeveloperDirectoryOffset)
            .Add(TgaString.XFileSignature.ToBytes())
            .Add(TgaString.DotSymbol.ToBytes())
            .Add(TgaString.ZeroTerminator.ToBytes())
            .ToArray();

        /// <summary>
        /// Gets <see cref="TgaFooter"/> like string.
        /// </summary>
        /// <returns>String in "ExtensionAreaOffset={0}, DeveloperDirectoryOffset={1}" format.</returns>
        public override string ToString() => string.Format("{0}={1}, {2}={3}", nameof(ExtensionAreaOffset), ExtensionAreaOffset, nameof(DeveloperDirectoryOffset), DeveloperDirectoryOffset);

        /// <inheritdoc />
        object ICloneable.Clone() => Copy();
    }
}
