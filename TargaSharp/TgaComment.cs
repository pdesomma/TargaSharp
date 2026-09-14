using System.Text;

namespace TargaSharp
{
    /// <summary>
    /// TGA Field 12 (Author Comments): 4 fixed-width lines of up to 80 ASCII characters each,
    /// every line terminated by a NUL byte (81 bytes per line, 324 bytes total).
    /// </summary>
    public sealed record TgaComment : ICloneable
    {
        /// <summary>
        /// Gets TGA Field size in bytes.
        /// </summary>
        public const int Size = 81 * 4;

        /// <summary>
        /// Number of comment lines in the field, per spec.
        /// </summary>
        private const int LineCount = 4;

        /// <summary>
        /// Maximum ASCII characters per line, not counting the mandatory NUL terminator.
        /// </summary>
        private const int LineLength = 80;

        /// <summary>
        /// Bytes occupied by one line including its NUL terminator (80 + 1 = 81, per spec "SrtN").
        /// </summary>
        private const int SlotLength = LineLength + 1;

        /// <summary>
        /// Backing storage for <see cref="Lines"/>, always exactly <see cref="LineCount"/> entries.
        /// </summary>
        private readonly string[] _lines = { string.Empty, string.Empty, string.Empty, string.Empty };

        /// <summary>
        /// Backing field for <see cref="BlankSpaceChar"/>.
        /// </summary>
        private char _blankSpaceChar = TgaString.DefaultBlankSpaceChar;

        /// <summary>
        /// Create a new instance of the <see cref="TgaComment"/> class with up to
        /// <see cref="LineCount"/> lines of text. Missing lines default to empty.
        /// </summary>
        /// <param name="lines">Comment lines (0-4 of them), each ASCII and at most
        /// <see cref="LineLength"/> characters.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="lines"/> or any of
        /// its elements is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="lines"/> has
        /// more than <see cref="LineCount"/> elements, or a line exceeds <see cref="LineLength"/>
        /// characters.</exception>
        /// <exception cref="ArgumentException">Thrown when a line contains a non-ASCII character.</exception>
        public TgaComment(params string[] lines)
        {
            ArgumentNullException.ThrowIfNull(lines);
            if (lines.Length > LineCount)
                throw new ArgumentOutOfRangeException(nameof(lines), lines.Length, $"At most {LineCount} lines are supported (TGA Field 12).");

            for (int i = 0; i < lines.Length; i++)
                SetLine(i, lines[i]);
        }

        /// <summary>
        /// Create a new instance of the <see cref="TgaComment"/> class from raw field bytes.
        /// </summary>
        /// <param name="bytes">Raw field bytes, must be exactly <see cref="Size"/> long.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="bytes"/>.Length != <see cref="Size"/>.</exception>
        /// <remarks>
        /// This is the file-reader path: each 80-character line slot is decoded leniently with
        /// <see cref="Encoding.ASCII"/> (which maps code points >= 128 to '?') instead of
        /// throwing, since malformed non-ASCII bytes are known to appear in the wild. Line-length
        /// and ASCII validation performed by <see cref="SetLine"/> is intentionally skipped here.
        /// </remarks>
        public TgaComment(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            if (bytes.Length != Size)
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length, $"Length must be {Size}.");

            for (int i = 0; i < LineCount; i++)
            {
                string slot = Encoding.ASCII.GetString(bytes, i * SlotLength, LineLength);
                int nulIndex = slot.IndexOf('\0');
                _lines[i] = nulIndex >= 0 ? slot.Substring(0, nulIndex) : slot.TrimEnd(' ');

                // ToBytes fills with BlankSpaceChar then NUL-terminates at byte 81, so a space-filled slot has no
                // NUL in its first 80 bytes; infer ' ' from that so ToBytes reproduces the input (and Equals holds).
                if (nulIndex < 0 && slot.EndsWith(' '))
                    _blankSpaceChar = ' ';
            }
        }

        /// <summary>
        /// Copy constructor backing the compiler-synthesized <c>with</c> expression. Declared
        /// explicitly (rather than relying on the default member-wise copy) so <see cref="_lines"/>
        /// is deep-copied instead of shared by reference between the original and the copy.
        /// </summary>
        /// <param name="original">Instance to copy from.</param>
        private TgaComment(TgaComment original)
        {
            _lines = (string[])original._lines.Clone();
            _blankSpaceChar = original._blankSpaceChar;
        }

        /// <summary>
        /// Gets the 4 comment lines (indices 0-3), per TGA Field 12. Use <see cref="SetLine"/> to
        /// change a line.
        /// </summary>
        public IReadOnlyList<string> Lines => _lines;

        /// <summary>
        /// Gets or sets the char used to fill blank space after a line's text and before its NUL terminator.
        /// </summary>
        public char BlankSpaceChar
        {
            get => _blankSpaceChar;
            set => _blankSpaceChar = value;
        }

        /// <summary>
        /// Sets one comment line.
        /// </summary>
        /// <param name="index">Zero-based line index, must be 0-3.</param>
        /// <param name="text">Line text, ASCII only, at most <see cref="LineLength"/> characters.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is
        /// outside 0-3, or <paramref name="text"/> is longer than <see cref="LineLength"/> characters.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="text"/> contains a
        /// character with code point >= 128 (TGA comment lines are ASCII-only per spec).</exception>
        public void SetLine(int index, string text)
        {
            if (index < 0 || index >= LineCount)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"index must be in range 0-{LineCount - 1}.");

            ArgumentNullException.ThrowIfNull(text);

            if (text.Length > LineLength)
                throw new ArgumentOutOfRangeException(nameof(text), text.Length, $"text must be <= {LineLength} characters (TGA Field 12 line width).");

            foreach (char c in text)
            {
                if (c >= 128)
                    throw new ArgumentException($"text must be ASCII (all chars < 128); TGA comment lines are ASCII-only per spec. Found '{c}' (0x{(int)c:X2}).", nameof(text));
                // The byte ctor reads a line up to its first NUL, so an embedded one would not round-trip.
                if (c == TgaString.DefaultEndingChar)
                    throw new ArgumentException("text must not contain NUL; readers treat it as the end of the line.", nameof(text));
            }

            _lines[index] = text;
        }

        /// <summary>
        /// Make full independent copy of <see cref="TgaComment"/>. Named <c>Copy</c> rather than
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
        /// Get the 4 lines joined with "\n", trimmed of trailing empty lines.
        /// </summary>
        /// <returns>Comment lines joined with "\n".</returns>
        public string GetString() => string.Join("\n", _lines).TrimEnd('\n');

        /// <summary>
        /// Convert <see cref="TgaComment"/> to byte array.
        /// </summary>
        /// <returns>Byte array, every byte is ASCII symbol.</returns>
        public byte[] ToBytes()
        {
            char[] c = new char[Size];

            for (int line = 0; line < LineCount; line++)
            {
                string text = _lines[line];
                int slotStart = line * SlotLength;

                for (int i = 0; i < LineLength; i++)
                    c[slotStart + i] = i < text.Length ? text[i] : _blankSpaceChar;

                c[slotStart + LineLength] = TgaString.DefaultEndingChar;
            }

            return Encoding.ASCII.GetBytes(c);
        }

        /// <inheritdoc />
        public bool Equals(TgaComment? other)
        {
            if (other is null) return false;
            return BlankSpaceChar == other.BlankSpaceChar && _lines.AsSpan().SequenceEqual(other._lines);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 27;
                hash = (13 * hash) + BlankSpaceChar.GetHashCode();
                for (int i = 0; i < _lines.Length; i++)
                    hash = (13 * hash) + _lines[i].GetHashCode();
                return hash;
            }
        }

        /// <inheritdoc />
        object ICloneable.Clone()
        {
            return Copy();
        }
    }

}
