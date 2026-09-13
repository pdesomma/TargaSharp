using System.Text;

namespace TargaSharp
{
    /// <summary>
    /// Use it for working with ASCII strings in TGA files.
    /// </summary>
    public sealed record TgaString : ICloneable
    {
        public const string XFileSignatureText = "TRUEVISION-XFILE";
        public const string DotSymbolConst = ".";
        public const char DefaultEndingChar = '\0';
        public const char DefaultBlankSpaceChar = '\0';

        /// <summary>
        /// Backing field for <see cref="OriginalString"/>.
        /// </summary>
        private string _originalString = string.Empty;

        /// <summary>
        /// Backing field for <see cref="Length"/>.
        /// </summary>
        private int _length;

        /// <summary>
        /// Backing field for <see cref="UseEndingChar"/>.
        /// </summary>
        private bool _useEndingChar;

        /// <summary>
        /// Gets a new Empty <see cref="TgaString"/>. A new instance is returned on every access
        /// so callers cannot mutate a shared default.
        /// </summary>
        public static TgaString Empty => new TgaString();

        /// <summary>
        /// Gets a new <see cref="TgaString"/> with <see cref="DefaultEndingChar"/> = '\0' and
        /// <see cref="UseEndingChar"/> = true. A new instance is returned on every access so
        /// callers cannot mutate a shared default.
        /// </summary>
        public static TgaString ZeroTerminator => new TgaString(1, true);

        /// <summary>
        /// Gets a new "." <see cref="TgaString"/> with dot (period) symbol. A new instance is
        /// returned on every access so callers cannot mutate a shared default.
        /// </summary>
        public static TgaString DotSymbol => new TgaString(DotSymbolConst, DotSymbolConst.Length);

        /// <summary>
        /// Gets a new "TRUEVISION-XFILE" <see cref="TgaString"/> (TGA File Format Version 2.0
        /// signature). A new instance is returned on every access so callers cannot mutate a
        /// shared default.
        /// </summary>
        public static TgaString XFileSignature => new TgaString(XFileSignatureText, XFileSignatureText.Length);

        /// <summary>
        /// Create a new instance of the <see cref="TgaString"/> class.
        /// </summary>
        /// <param name="useEnding"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="useEnding"/>
        /// is <see langword="true"/>, since the default <see cref="Length"/> of 0 cannot hold the
        /// mandatory ending character.</exception>
        public TgaString(bool useEnding = false)
        {
            _useEndingChar = useEnding;
            Validate();
        }

        /// <summary>
        /// Create a new instance of the <see cref="TgaString"/> class.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="useEnding"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <remarks>
        /// This is the file-reader path: bytes read from disk are decoded leniently with
        /// <see cref="Encoding.ASCII"/> (which maps code points >= 128 to '?') instead of
        /// throwing, since malformed non-ASCII bytes are known to appear in the wild. Structural
        /// invariant validation (see <see cref="Validate"/>) is intentionally skipped here.
        /// </remarks>
        public TgaString(byte[] bytes, bool useEnding = false)
        {
            ArgumentNullException.ThrowIfNull(bytes);

            _useEndingChar = useEnding;
            _length = bytes.Length;
            string s = Encoding.ASCII.GetString(bytes, 0, bytes.Length - (useEnding ? 1 : 0));

            if (s.Length > 0)
                switch (s[s.Length - 1])
                {
                    case '\0':
                    case ' ':
                        BlankSpaceChar = s[s.Length - 1];
                        _originalString = s.TrimEnd([s[s.Length - 1]]);
                        break;
                    default:
                        _originalString = s;
                        break;
                }
        }

        /// <summary>
        /// Create a new instance of the <see cref="TgaString"/> class.
        /// </summary>
        /// <param name="length"></param>
        /// <param name="useEnding"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is
        /// less than <c>(useEnding ? 1 : 0)</c>, since it would leave no room for the mandatory
        /// ending character.</exception>
        public TgaString(int length, bool useEnding = false)
        {
            _useEndingChar = useEnding;
            _length = length;
            Validate();
        }

        /// <summary>
        /// Create a new instance of the <see cref="TgaString"/> class.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="length"></param>
        /// <param name="useEnding"></param>
        /// <param name="blankSpaceChar"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="str"/> contains a
        /// non-ASCII character.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is
        /// too small to hold <paramref name="str"/> plus the optional ending character.</exception>
        public TgaString(string str, int length, bool useEnding = false, char blankSpaceChar = DefaultBlankSpaceChar)
        {
            if (str == null) throw new ArgumentNullException(nameof(str) + " = null!");

            _useEndingChar = useEnding;
            _originalString = str;
            _length = length;
            BlankSpaceChar = blankSpaceChar;
            Validate();
        }

        public static TgaString operator +(TgaString item1, TgaString item2)
        {
            if (item1 is null || item2 is null) throw new ArgumentNullException();
            byte[] bytes1 = item1.ToBytes();
            byte[] bytes2 = item2.ToBytes();
            return new TgaString(new TgaByteBuilder(bytes1.Length + bytes2.Length).Add(bytes1).Add(bytes2).ToArray());
        }

        /// <summary>
        /// Gets or sets the decoded ASCII string content, excluding the fixed-width padding and
        /// optional ending character. Per TGA spec, fixed-width string fields are ASCII-only, so
        /// the value must contain no character with code point >= 128, and must fit within
        /// <see cref="Length"/> minus the optional ending character reserved by
        /// <see cref="UseEndingChar"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when set to <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when set to a value containing a non-ASCII
        /// character.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is too long to fit
        /// within <see cref="Length"/> (minus the optional ending character).</exception>
        public string OriginalString
        {
            get => _originalString;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _originalString = value;
                Validate();
            }
        }

        /// <summary>
        /// Gets or sets the total field length in bytes, including the optional ending character
        /// reserved by <see cref="UseEndingChar"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is too small to
        /// hold <see cref="OriginalString"/> plus the optional ending character.</exception>
        public int Length
        {
            get => _length;
            set
            {
                _length = value;
                Validate();
            }
        }

        public char BlankSpaceChar { get; set; } = DefaultBlankSpaceChar;

        /// <summary>
        /// Gets or sets whether a mandatory ending character (see <see cref="DefaultEndingChar"/>)
        /// is reserved as the last byte of the field, per TGA spec fixed-width string field rules.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when set to <see langword="true"/>
        /// while <see cref="Length"/> is 0, since there would be no room for the ending character.</exception>
        public bool UseEndingChar
        {
            get => _useEndingChar;
            set
            {
                _useEndingChar = value;
                Validate();
            }
        }

        /// <summary>
        /// Make full independed copy of <see cref="TgaString"/>. Named <c>Copy</c> rather than
        /// <c>Clone</c> because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns>Copy of <see cref="TgaString"/></returns>
        public TgaString Copy() => this with { };

        /// <summary>
        /// Get ASCII-Like string to first string-terminator, example:
        /// "Some string \0 Some Data \0" - > "Some string".
        /// </summary>
        /// <returns>String to first string-terminator.</returns>
        public string GetString()
        {
            string text = Encoding.ASCII.GetString(ToBytes());
            int endIndex = text.IndexOf('\0');
            if (endIndex != -1)
                text = text.Substring(0, endIndex);
            return text;
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
        /// <param name="length">Length of output ASCII string with Ending char (if used).</param>
        /// <param name="useEnding">Add <see cref="EndingChr"/> to string or not?</param>
        /// <param name="blankSpaceChar">Char for filling blank space in string. If this char is '-' (only for example!),
        /// for string "ABC" with <see cref="Length"/> = 7, with <see cref="UseEnding"/> = true,
        /// <see cref="DefaultEndingChar"/> is '\0', result string is "ABC---\0".</param>
        /// <returns>Byte array, every byte is ASCII symbol.</returns>
        /// <remarks>
        /// This static overload is a general-purpose utility independent of any
        /// <see cref="TgaString"/> instance invariants, so it still pads/truncates defensively
        /// rather than throwing when <paramref name="str"/> is longer than <paramref name="length"/>.
        /// </remarks>
        public static byte[] ToBytes(string str, int length, bool useEnding = true, char blankSpaceChar = '\0')
        {
            char[] chars = new char[Math.Max(length, (useEnding ? 1 : 0))];

            for (int i = 0; i < chars.Length; i++)
                chars[i] = (i < str.Length ? str[i] : blankSpaceChar);

            if (useEnding)
                chars[chars.Length - 1] = DefaultEndingChar;

            return Encoding.ASCII.GetBytes(chars);
        }

        /// <summary>
        /// Validates the structural invariants that span <see cref="OriginalString"/>,
        /// <see cref="Length"/> and <see cref="UseEndingChar"/> together. Since these are
        /// independent mutable properties, this is called after every property setter assignment
        /// and (directly against the backing fields) at the end of every constructor that accepts
        /// caller-supplied values, so the combination is always checked as a whole. The lenient
        /// byte-array (file-reader) constructor intentionally does not call this.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when <see cref="OriginalString"/> contains a
        /// character with code point >= 128 (TGA fixed-width string fields are ASCII-only per spec).</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="Length"/> is
        /// negative, or too small to hold both <see cref="OriginalString"/> and the optional
        /// ending character reserved by <see cref="UseEndingChar"/>.</exception>
        private void Validate()
        {
            foreach (char c in _originalString)
                if (c >= 128)
                    throw new ArgumentException($"OriginalString must be ASCII (all chars < 128); TGA fixed-width string fields are ASCII-only per spec. Found '{c}' (0x{(int)c:X2}).", nameof(OriginalString));

            int reserved = _useEndingChar ? 1 : 0;

            if (_length < reserved)
                throw new ArgumentOutOfRangeException(nameof(Length), _length, $"Length must be >= {reserved} when UseEndingChar = {_useEndingChar} (room is needed for the mandatory ending character), per TGA fixed-width string field rules.");

            if (_originalString.Length > _length - reserved)
                throw new ArgumentOutOfRangeException(nameof(Length), _length, $"OriginalString.Length ({_originalString.Length}) must be <= Length - (UseEndingChar ? 1 : 0) ({_length - reserved}), per TGA fixed-width string field rules.");
        }

        /// <inheritdoc />
        object ICloneable.Clone() => Copy();
    }
}
