using System.Text;

namespace TargaSharp
{
    public class TgaComment : ICloneable
    {
        const int StrNLen = 80; //80 ASCII chars + 1 '\0' = 81 per SrtN!
        string origString = String.Empty;
        char blankSpaceChar = TgaString.DefaultBlankSpaceChar;

        public TgaComment() { }

        public TgaComment(string Str, char BlankSpaceChar = '\0')
        {
            if (Str == null)
                throw new ArgumentNullException(nameof(Str) + " = null!");

            origString = Str;
            blankSpaceChar = BlankSpaceChar;
        }

        public TgaComment(byte[] Bytes)
        {
            ArgumentNullException.ThrowIfNull(Bytes);
            if (Bytes.Length != Size)
                throw new ArgumentOutOfRangeException(nameof(Bytes), Bytes.Length, $"Length must be {Size}.");

            string s = Encoding.ASCII.GetString(Bytes, 0, StrNLen);
            s += Encoding.ASCII.GetString(Bytes, 81, StrNLen);
            s += Encoding.ASCII.GetString(Bytes, 162, StrNLen);
            s += Encoding.ASCII.GetString(Bytes, 243, StrNLen);

            switch (s[s.Length - 1])
            {
                case '\0':
                case ' ':
                    blankSpaceChar = s[s.Length - 1];
                    origString = s.TrimEnd(new char[] { s[s.Length - 1] });
                    break;
                default:
                    origString = s;
                    break;
            }
        }

        /// <summary>
        /// Gets TGA Field size in bytes.
        /// </summary>
        public const int Size = 81 * 4;

        public string OriginalString
        {
            get { return origString; }
            set { origString = value; }
        }

        public char BlankSpaceChar
        {
            get { return blankSpaceChar; }
            set { blankSpaceChar = value; }
        }

        /// <summary>
        /// Make full independed copy of <see cref="TgaComment"/>.
        /// </summary>
        /// <returns>Copy of <see cref="TgaComment"/></returns>
        public TgaComment Clone()
        {
            return new TgaComment(origString, blankSpaceChar);
        }

        /// <summary>
        /// Make full independed copy of <see cref="TgaComment"/>.
        /// </summary>
        /// <returns>Copy of <see cref="TgaComment"/></returns>
        object ICloneable.Clone()
        {
            return Clone();
        }

        public override bool Equals(object obj)
        {
            return ((obj is TgaComment) ? Equals((TgaComment)obj) : false);
        }

        public bool Equals(TgaComment item)
        {
            return (origString == item.origString && blankSpaceChar == item.blankSpaceChar);
        }

        public static bool operator ==(TgaComment item1, TgaComment item2)
        {
            if (ReferenceEquals(item1, null))
                return ReferenceEquals(item2, null);

            if (ReferenceEquals(item2, null))
                return ReferenceEquals(item1, null);

            return item1.Equals(item2);
        }

        public static bool operator !=(TgaComment item1, TgaComment item2)
        {
            return !(item1 == item2);
        }

        public override int GetHashCode()
        {
            return origString.GetHashCode() ^ blankSpaceChar.GetHashCode();
        }

        /// <summary>
        /// Get ASCII-Like string with string-terminators, example: "Line1 \0\0 Line2 \0\0\0".
        /// </summary>
        /// <returns>String with replaced string-terminators to "\0".</returns>
        public override string ToString()
        {
            return Encoding.ASCII.GetString(ToBytes()).Replace("\0", @"\0");
        }

        /// <summary>
        /// Get ASCII-Like string to first string-terminator, example:
        /// "Some string \0 Some Data \0" - > "Some string".
        /// </summary>
        /// <returns>String to first string-terminator.</returns>
        public string GetString()
        {
            String Str = Encoding.ASCII.GetString(ToBytes());
            for (int i = 1; i < 4; i++)
                Str = Str.Insert((StrNLen + 1) * i + i - 1, "\n");
            return Str.Replace("\0", String.Empty).TrimEnd(new char[] { '\n' });
        }

        /// <summary>
        /// Convert <see cref="TgaComment"/> to byte array.
        /// </summary>
        /// <returns>Byte array, every byte is ASCII symbol.</returns>
        public byte[] ToBytes()
        {
            return ToBytes(origString, blankSpaceChar);
        }

        /// <summary>
        /// Convert <see cref="TgaComment"/> to byte array.
        /// </summary>
        /// <param name="Str">Input string.</param>
        /// <param name="BlankSpaceChar">Char for filling blank space in string.</param>
        /// <returns>Byte array, every byte is ASCII symbol.</returns>
        public static byte[] ToBytes(string Str, char BlankSpaceChar = '\0')
        {
            char[] C = new char[81 * 4];

            for (int i = 0; i < C.Length; i++)
            {
                if ((i + 82) % 81 == 0)
                    C[i] = TgaString.DefaultEndingChar;
                else
                {
                    int Index = i - i / 81;
                    C[i] = (Index < Str.Length ? Str[Index] : BlankSpaceChar);
                }
            }
            return Encoding.ASCII.GetBytes(C);
        }
    }

}
