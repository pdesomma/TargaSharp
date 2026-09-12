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
        /// <param name="sig">New TGA format signature.</param>
        /// <param name="reservChr">Reserved Character - ASCII character “.” (period).</param>
        /// <param name="terminator">Binary Zero Terminator, a binary zero which acts as a final terminator.</param>
        public TgaFooter(uint extOff, uint devDirOff, TgaString sig, TgaString reservChr, TgaString terminator)
        {
            ExtensionAreaOffset = extOff;
            DeveloperDirectoryOffset = devDirOff;
            Signature = sig;
            ReservedCharacter = reservChr;
            BinaryZeroStringTerminator = terminator;
        }

        /// <summary>
        /// Make <see cref="TgaFooter"/> from bytes (if signature is right).
        /// </summary>
        /// <param name="bytes">Bytes array (byte[26]).</param>
        public TgaFooter(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            if (bytes.Length != Size)
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length, $"Length must be {Size}.");

            ExtensionAreaOffset = BitConverter.ToUInt32(bytes, 0);
            DeveloperDirectoryOffset = BitConverter.ToUInt32(bytes, 4);
            Signature = new TgaString(BitConverterHelper.GetElements(bytes, 8, TgaString.XFileSignatuteConst.Length));
            ReservedCharacter = new TgaString(new byte[] { bytes[24] });
            BinaryZeroStringTerminator = new TgaString(new byte[] { bytes[25] });
        }

        /// <summary>
        /// Byte 25 - Binary Zero String Terminator - Field 32
        /// Byte 25 is a binary zero which acts as a final terminator and allows the entire TGA
        /// File Footer to be read and utilized as a “C” string.
        /// </summary>
        public TgaString BinaryZeroStringTerminator { get; set; } = TgaString.ZeroTerminator;

        /// <summary>
        /// Byte 4-7 - Developer Directory Offset - Field 29
        /// The next four bytes(bytes 4-7, the second LONG) contain an offset from the
        /// beginning of the file to the start of the Developer Directory. If the Developer
        /// Directory Offset is zero, then the Developer Area does not exist.
        /// </summary>
        public uint DeveloperDirectoryOffset { get; set; } = 0;

        /// <summary>
        /// Byte 0-3 - Extension Area Offset - Field 28
        /// The first four bytes (bytes 0-3, the first LONG) of the TGA File Footer contain an
        /// offset from the beginning of the file to the start of the Extension Area. Simply
        /// SEEK to this location to position to the start of the Extension Area. If the
        /// Extension Area Offset is zero, no Extension Area exists in the file.
        /// </summary>
        public uint ExtensionAreaOffset { get; set; } = 0;

        /// <summary>
        /// Is footer is real footer of TGA File Format Version 2.0?
        /// Checks <see cref="TgaString.XFileSignatute"/> (Field 30) and also, per spec Fields
        /// 31/32, that <see cref="ReservedCharacter"/> is '.' (byte 24) and
        /// <see cref="BinaryZeroStringTerminator"/> is '\0' (byte 25).
        /// </summary>
        public bool IsFooterCorrect =>
            Signature == TgaString.XFileSignatute &&
            ReservedCharacter.ToBytes() is [(byte)'.'] &&
            BinaryZeroStringTerminator.ToBytes() is [0];

        /// <summary>
        /// Byte 24 - Reserved Character - Field 31
        /// Byte 24 is an ASCII character “.” (period). This character MUST BE a period or
        /// the file is not considered a proper TGA file.
        /// </summary>
        public TgaString ReservedCharacter { get; set; } = TgaString.DotSymbol;

        /// <summary>
        /// Byte 8-23 - Signature - Field 30
        /// This string is exactly 16 bytes long and is formatted exactly as shown below
        /// capital letters), with a hyphen between “TRUEVISION” and “XFILE.” If the
        /// signature is detected, the file is assumed to be of the New TGA format and MAY,
        /// therefore, contain the Developer Area and/or the Extension Area fields.If the
        /// signature is not found, then the file is assumed to be in the Original TGA format.
        /// </summary>
        public TgaString Signature { get; set; } = TgaString.XFileSignatute;

        /// <summary>
        /// Make full copy of <see cref="TgaFooter"/>. Named <c>Copy</c> rather than
        /// <c>Clone</c> because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns></returns>
        public TgaFooter Copy() => this with { Signature = Signature.Copy(), ReservedCharacter = ReservedCharacter.Copy(), BinaryZeroStringTerminator = BinaryZeroStringTerminator.Copy() };

        /// <summary>
        /// Convert <see cref="TgaFooter"/> to byte array.
        /// </summary>
        /// <returns>Byte array with size equal <see cref="Size"/>.</returns>
        public byte[] ToBytes() => BitConverterHelper.ToBytes(ExtensionAreaOffset, DeveloperDirectoryOffset, Signature.ToBytes(), ReservedCharacter.ToBytes(), BinaryZeroStringTerminator.ToBytes());

        public override string ToString() => string.Format("{0}={1}, {2}={3}, FullSignature={4}", nameof(ExtensionAreaOffset), ExtensionAreaOffset, nameof(DeveloperDirectoryOffset), DeveloperDirectoryOffset, (Signature + ReservedCharacter + BinaryZeroStringTerminator).ToString());

        /// <inheritdoc />
        object ICloneable.Clone() => Copy();
    }
}
