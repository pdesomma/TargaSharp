using System.Text;

namespace TargaSharp
{
    public sealed record TgaComment : ICloneable
    {
        /// <summary>
        /// Gets TGA Field size in bytes.
        /// </summary>
        public const int Size = 81 * 4;

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
        /// Make full independed copy of <see cref="TgaComment"/>. Named <c>Copy</c> rather than
        /// <c>Clone</c> because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns>Copy of <see cref="TgaComment"/></returns>
        public TgaComment Copy()
        {
            return this with { };
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

        /// <inheritdoc />
        object ICloneable.Clone()
        {
            return Copy();
        }
    }

}
