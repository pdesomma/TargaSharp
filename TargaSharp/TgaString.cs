using System;
using System.Text;
using TargaSharp.Helpers;

namespace TargaSharp
{
    /// <summary>
    /// Wrapper for working with ASCII strings in TGA files.
    /// </summary>
    public class TgaString : ICloneable, IEquatable<TgaString>, IByteConvert
    {
        public const string XFileSignatureConst = "TRUEVISION-XFILE";
        public const string DotSymbolConst = ".";

        /// <summary>
        /// Create a new instance of the <see cref="TgaString"/> class.
        /// </summary>
        /// <param name="useEndingChar"></param>
        public TgaString(bool useEndingChar = false)
        {
            UseEndingChar = useEndingChar;
        }

        /// <summary>
        /// Create a new instance of the <see cref="TgaString"/> class.
        /// </summary>  
        /// <param name="bytes"></param>
        /// <param name="useEndingChar"></param>
        public TgaString(byte[] bytes, bool useEndingChar = false)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(bytes);
#else 
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));
#endif
            Length = bytes.Length;
            UseEndingChar = useEndingChar;
            string s = Encoding.ASCII.GetString(bytes, 0, bytes.Length - (UseEndingChar ? 1 : 0));
            if (s.Length > 0)
                switch (s[^1])
                {
                    case '\0':
                    case ' ':
                        BlankSpaceChar = s[^1];
#if NET8_0_OR_GREATER
                        OriginalString = s.TrimEnd([BlankSpaceChar]);
#else
                        OriginalString = s.TrimEnd(new char[] { BlankSpaceChar } );
#endif
                        break;
                    default:
                        OriginalString = s;
                        break;
                }
        }

        /// <summary>
        /// Create a new instance of the <see cref="TgaString"/> class.
        /// </summary>  
        /// <param name="length"></param>
        /// <param name="useEndingChar"></param>
        public TgaString(int length, bool useEndingChar = false)
        {
            Length = length;
            UseEndingChar = useEndingChar;
        }

        /// <summary>
        /// Create a new instance of the <see cref="TgaString"/> class.
        /// </summary>  
        /// <param name="str"></param>
        /// <param name="length"></param>
        /// <param name="useEndingChar"></param>
        /// <param name="blankSpaceChar"></param>
        public TgaString(string str, int length, bool useEndingChar = false, char blankSpaceChar = '\0')
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(str);
#else 
            if (str is null) throw new ArgumentNullException(nameof(str));
#endif
            OriginalString = str;
            Length = length;
            BlankSpaceChar = blankSpaceChar;
            UseEndingChar = useEndingChar;
        }

        public static bool operator ==(TgaString item1, TgaString item2) => EqualityHelper.Operator(item1, item2);
        public static bool operator !=(TgaString item1, TgaString item2) => !(item1 == item2);
        public static TgaString? operator +(TgaString item1, TgaString item2)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(item1);
            ArgumentNullException.ThrowIfNull(item2);
#else 
            if (item1 is null) throw new ArgumentNullException(nameof(item1));
            if (item2 is null) throw new ArgumentNullException(nameof(item2));
#endif
            return new TgaString(ByteHelper.ToBytes(item1.ToBytes(), item2?.ToBytes()));
        }

        /// <summary>
        /// Gets ending char, default '\0'.
        /// </summary>
        public static readonly char DefaultEndingChar = '\0';

        /// <summary>
        /// Gets blank space char, value = '\0'.
        /// </summary>
        public static readonly char DefaultBlankSpaceChar = '\0';

        /// <summary>
        /// Gets <see cref="TgaString"/> with <see cref="DefaultEndingChar"/> = '\0' and <see cref="UseEndingChar"/> = true.
        /// </summary>
        public static readonly TgaString ZeroTerminator = new(true);

        /// <summary>
        /// Gets "." <see cref="TgaString"/> with dot (period) symbol.
        /// </summary>
        public static readonly TgaString DotSymbol = new(DotSymbolConst, DotSymbolConst.Length);

        /// <summary>
        /// Gets "TRUEVISION-XFILE" <see cref="TgaString"/> (TGA File Format Version 2.0 signature).
        /// </summary>
        public static readonly TgaString XFileSignature = new(XFileSignatureConst, XFileSignatureConst.Length);

        /// <summary>
        /// Gets Empty <see cref="TgaString"/>.
        /// </summary>
        public static TgaString Empty => new();

        /// <summary>
        /// Convert <see cref="TgaString"/> to byte array.
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <param name="Length">Length of output ASCII string with Ending char (if used).</param>
        /// <param name="UseEnding">Add <see cref="EndingChr"/> to string or not?</param>
        /// <param name="BlankSpaceChar">Char for filling blank space in string. If this char is '-' (only for example!),
        /// for string "ABC" with <see cref="Length"/> = 7, with <see cref="UseEnding"/> = true,
        /// <see cref="DefaultEndingChar"/> is '\0', result string is "ABC---\0".</param>
        /// <returns>Byte array, every byte is ASCII symbol.</returns>
        public static byte[] ToBytes(string str, int Length, bool UseEnding = true, char BlankSpaceChar = '\0')
        {
            char[] C = new char[Math.Max(Length, UseEnding ? 1 : 0)];

            for (int i = 0; i < C.Length; i++)
                C[i] = (i < str.Length ? str[i] : BlankSpaceChar);

            if (UseEnding) C[^1] = DefaultEndingChar;

            return Encoding.ASCII.GetBytes(C);
        }

        public char BlankSpaceChar { get; private set; } = DefaultBlankSpaceChar;
        public int Length { get; set; }
        public string OriginalString { get; private set; } = string.Empty;
        public bool UseEndingChar { get; private set; } = false;


        /// <summary>
        /// Make full independent copy of <see cref="TgaString"/>.
        /// </summary>
        /// <returns>Copy of <see cref="TgaString"/></returns>
        public TgaString Clone() => new(OriginalString, Length, UseEndingChar, BlankSpaceChar);

        /// <summary>
        /// Make full independent copy of <see cref="TgaString"/>.
        /// </summary>
        /// <returns>Copy of <see cref="TgaString"/></returns>
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is TgaString str && Equals(str);

        public bool Equals(TgaString? item) =>
                item is not null &&
                OriginalString == item.OriginalString &&
                Length == item.Length &&
                BlankSpaceChar == item.BlankSpaceChar &&
                UseEndingChar == item.UseEndingChar;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + OriginalString.GetHashCode();
                hash = hash * 23 + Length.GetHashCode();
                hash = hash * 23 + BlankSpaceChar.GetHashCode();
                hash = hash * 23 + UseEndingChar.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Get ASCII-Like string to first string-terminator, example:
        /// "Some string \0 Some Data \0" - > "Some string".
        /// </summary>
        /// <returns>String to first string-terminator.</returns>
        public string GetString()
        {
            String str = Encoding.ASCII.GetString(ToBytes());
            int endIndex = str.IndexOf('\0');
            if (endIndex != -1) str = str[..endIndex];
            return str;
        }

        /// <summary>
        /// Convert <see cref="TgaString"/> to byte array.
        /// </summary>
        /// <returns>Byte array, every byte is ASCII symbol.</returns>
        public byte[] ToBytes() => ToBytes(OriginalString, Length, UseEndingChar, BlankSpaceChar);

        /// <summary>
        /// Get ASCII-Like string with string-terminators, example: "Some string\0\0\0\0\0".
        /// </summary>
        /// <returns>String with replaced string-terminators to "\0".</returns>
        public override string ToString() => Encoding.ASCII.GetString(ToBytes()).Replace("\0", @"\0");
    }
}
