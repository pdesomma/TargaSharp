using System.Text;

namespace TargaSharp
{
    /// <summary>
    /// Use it for working with ASCII strings in TGA files.
    /// </summary>
    public class TgaString : ICloneable
    {
        public const string XFileSignatuteConst = "TRUEVISION-XFILE";
        public const string DotSymbolConst = ".";
        public const char DefaultEndingChar = '\0';
        public const char DefaultBlankSpaceChar = '\0';

        /// <summary>
        /// Create a new instance of the <see cref="TgaString"/> class.
        /// </summary>
        /// <param name="useEnding"></param>
        public TgaString(bool useEnding = false) => UseEndingChar = useEnding;

        /// <summary>
        /// Create a new instance of the <see cref="TgaString"/> class.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="useEnding"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public TgaString(byte[] bytes, bool useEnding = false) : this(useEnding)
        {
            ArgumentNullException.ThrowIfNull(bytes);

            Length = bytes.Length;
            string s = Encoding.ASCII.GetString(bytes, 0, bytes.Length - (useEnding ? 1 : 0));

            if (s.Length > 0)
                switch (s[s.Length - 1])
                {
                    case '\0':
                    case ' ':
                        BlankSpaceChar = s[s.Length - 1];
                        OriginalString = s.TrimEnd([s[s.Length - 1]]);
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
        /// <param name="useEnding"></param>
        public TgaString(int length, bool useEnding = false) : this(useEnding)
        {
            Length = length;
        }

        /// <summary>
        /// Create a new instance of the <see cref="TgaString"/> class.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="length"></param>
        /// <param name="useEnding"></param>
        /// <param name="blankSpaceChar"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public TgaString(string str, int length, bool useEnding = false, char blankSpaceChar = DefaultBlankSpaceChar) : this(useEnding)
        {
            if (str == null) throw new ArgumentNullException(nameof(str) + " = null!");

            OriginalString = str;
            Length = length;
            BlankSpaceChar = blankSpaceChar;
        }



        public static bool operator ==(TgaString item1, TgaString item2)
        {
            if (item1 is null) return item2 is null;
            if (item2 is null) return item1 is null;
            return item1.Equals(item2);
        }

        public static bool operator !=(TgaString item1, TgaString item2) => !(item1 == item2);

        public static TgaString operator +(TgaString item1, TgaString item2)
        {
            if (item1 is null || item2 is null) throw new ArgumentNullException();
            return new TgaString(BitConverterHelper.ToBytes(item1.ToBytes(), item2.ToBytes())!);
        }



        public string OriginalString { get; set; } = string.Empty;

        public int Length { get; set; }

        public char BlankSpaceChar { get; set; } = DefaultBlankSpaceChar;

        public bool UseEndingChar { get; set; }

        /// <summary>
        /// Gets Empty <see cref="TgaString"/>.
        /// </summary>
        public static readonly TgaString Empty = new TgaString();

        /// <summary>
        /// Gets <see cref="TgaString"/> with <see cref="DefaultEndingChar"/> = '\0' and <see cref="UseEndingChar"/> = true.
        /// </summary>
        public static readonly TgaString ZeroTerminator = new TgaString(true);

        /// <summary>
        /// Gets "." <see cref="TgaString"/> with dot (period) symbol.
        /// </summary>
        public static readonly TgaString DotSymbol = new TgaString(DotSymbolConst, DotSymbolConst.Length);

        /// <summary>
        /// Gets "TRUEVISION-XFILE" <see cref="TgaString"/> (TGA File Format Version 2.0 signatute).
        /// </summary>
        public static readonly TgaString XFileSignatute = new TgaString(XFileSignatuteConst, XFileSignatuteConst.Length);

        /// <summary>
        /// Make full independed copy of <see cref="TgaString"/>.
        /// </summary>
        /// <returns>Copy of <see cref="TgaString"/></returns>
        public TgaString Clone() => new TgaString(OriginalString, Length, UseEndingChar, BlankSpaceChar);

        /// <summary>
        /// Make full independed copy of <see cref="TgaString"/>.
        /// </summary>
        /// <returns>Copy of <see cref="TgaString"/></returns>
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => (obj is not null && obj is TgaString) ? Equals((TgaString)obj) : false;

        public bool Equals(TgaString item) => OriginalString == item.OriginalString && Length == item.Length && BlankSpaceChar == item.BlankSpaceChar && UseEndingChar == item.UseEndingChar;

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
            String Str = Encoding.ASCII.GetString(ToBytes());
            int EndIndex = Str.IndexOf('\0');
            if (EndIndex != -1)
                Str = Str.Substring(0, EndIndex);
            return Str;
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
            char[] C = new char[Math.Max(Length, (UseEnding ? 1 : 0))];

            for (int i = 0; i < C.Length; i++)
                C[i] = (i < str.Length ? str[i] : BlankSpaceChar);

            if (UseEnding)
                C[C.Length - 1] = DefaultEndingChar;

            return Encoding.ASCII.GetBytes(C);
        }
    }
}