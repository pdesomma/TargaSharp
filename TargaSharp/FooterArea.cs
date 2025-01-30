using System;
using TargaSharp.Helpers;

namespace TargaSharp
{
    /// <summary>
    /// File Footer Area
    /// </summary>
    public class FooterArea : IByteConvert, ICloneable, IEquatable<FooterArea>
    {
        /// <summary>
        /// Gets TGA Footer Section size in bytes.
        /// </summary>
        public const int Size = 26;

        /// <summary>
        /// Make NewXFile format TGA Footer with <see cref="ExtensionAreaOffset"/> = 0 and
        /// <see cref="DeveloperDirectoryOffset"/> = 0.
        /// </summary>
        public FooterArea() { }

        /// <summary>
        /// Make <see cref="FooterArea"/> from values.
        /// </summary>
        /// <param name="extOff">Extension Area Offset, offset from the beginning of the file.</param>
        /// <param name="devDirOff">Developer Directory Offset, offset from the beginning of the file.</param>
        /// <param name="sign">New TGA format signature.</param>
        /// <param name="reservedChar">Reserved Character - ASCII character “.” (period).</param>
        /// <param name="terminator">Binary Zero Terminator, a binary zero which acts as a final terminator.</param>
        public FooterArea(uint extOff, uint devDirOff, TgaString sign, TgaString reservedChar, TgaString terminator)
        {
            ExtensionAreaOffset = extOff;
            DeveloperDirectoryOffset = devDirOff;
            Signature = sign;
            ReservedCharacter = reservedChar;
            BinaryZeroStringTerminator = terminator;
        }

        /// <summary>
        /// Make <see cref="FooterArea"/> from bytes (if signature is right).
        /// </summary>
        /// <param name="bytes">Bytes array (byte[26]).</param>
        public FooterArea(byte[] bytes)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(bytes);
#else
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));
#endif
            if (bytes.Length != Size) throw new ArgumentOutOfRangeException(nameof(bytes.Length) + " must be equal " + Size + "!");

            ExtensionAreaOffset = BitConverter.ToUInt32(bytes, 0);
            DeveloperDirectoryOffset = BitConverter.ToUInt32(bytes, 4);
            Signature = new TgaString(EnumerableHelper.GetElements(bytes, 8, (uint)TgaString.XFileSignature.Length));
            ReservedCharacter =
#if NET8_0_OR_GREATER
                new TgaString([bytes[24]]);
#else
                new TgaString(new byte[] { bytes[24] });
#endif
            BinaryZeroStringTerminator =

#if NET8_0_OR_GREATER
                new TgaString([bytes[25]]);
#else
                new TgaString(new byte[] { bytes[25] });
#endif
        }

        public static bool operator ==(FooterArea item1, FooterArea item2) => EqualityHelper.Operator(item1, item2);
        public static bool operator !=(FooterArea item1, FooterArea item2) => !(item1 == item2);



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
        public uint DeveloperDirectoryOffset { get; set; }

        /// <summary>
        /// Byte 0-3 - Extension Area Offset - Field 28
        /// The first four bytes (bytes 0-3, the first LONG) of the TGA File Footer contain an
        /// offset from the beginning of the file to the start of the Extension Area. Simply
        /// SEEK to this location to position to the start of the Extension Area. If the
        /// Extension Area Offset is zero, no Extension Area exists in the file.
        /// </summary>
        public uint ExtensionAreaOffset { get; set; }

        /// <summary>
        /// Is footer is real footer of TGA File Format Version 2.0?
        /// Checking by <see cref="TgaString.XFileSignatute"/>.
        /// </summary>
        public bool IsFooterCorrect => Signature == TgaString.XFileSignature;

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
        public TgaString Signature { get; set; } = TgaString.XFileSignature;

        /// <summary>
        /// Make full copy of <see cref="FooterArea"/>.
        /// </summary>
        /// <returns></returns>
        public FooterArea Clone() => new(ExtensionAreaOffset, DeveloperDirectoryOffset, Signature.Clone(), ReservedCharacter.Clone(), BinaryZeroStringTerminator.Clone());
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is FooterArea footer && Equals(footer);
        public bool Equals(FooterArea? item) =>
            item is not null &&
            ExtensionAreaOffset == item.ExtensionAreaOffset &&
            DeveloperDirectoryOffset == item.DeveloperDirectoryOffset &&
            Signature == item.Signature &&
            ReservedCharacter == item.ReservedCharacter &&
            BinaryZeroStringTerminator == item.BinaryZeroStringTerminator;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + ExtensionAreaOffset.GetHashCode();
                hash = hash * 23 + DeveloperDirectoryOffset.GetHashCode();

                if (Signature is not null) hash = hash * 23 + Signature.GetHashCode();
                if (ReservedCharacter is not null) hash = hash * 23 + ReservedCharacter.GetHashCode();
                if (BinaryZeroStringTerminator is not null) hash = hash * 23 + BinaryZeroStringTerminator.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Convert <see cref="FooterArea"/> to byte array.
        /// </summary>
        /// <returns>Byte array with size equal <see cref="Size"/>.</returns>
        public byte[]? ToBytes() => ByteHelper.ToBytes(ExtensionAreaOffset, DeveloperDirectoryOffset,Signature.ToBytes(), ReservedCharacter.ToBytes(), BinaryZeroStringTerminator.ToBytes());        

        public override string ToString() => string.Format("{0}={1}, {2}={3}, FullSignature={4}",
                nameof(ExtensionAreaOffset), ExtensionAreaOffset, nameof(DeveloperDirectoryOffset), DeveloperDirectoryOffset, (Signature + ReservedCharacter + BinaryZeroStringTerminator).ToString());   
    }
}