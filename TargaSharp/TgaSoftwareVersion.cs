using System.Text;

namespace TargaSharp
{
    /// <summary>
    /// Software Version - Field 17 (3 bytes): a version number (times 100) plus an optional
    /// ASCII release letter, describing the software identified by the extension area's
    /// Software ID field.
    /// </summary>
    public sealed record TgaSoftwareVersion : ICloneable
    {

        /// <summary>
        /// Gets TGA Field size in bytes.
        /// </summary>
        public const int Size = 3;


        /// <summary>
        /// Gets Empty <see cref="TgaSoftwareVersion"/>, <see cref="VersionLetter"/> = ' ' (space).
        /// </summary>
        public TgaSoftwareVersion() { }

        /// <summary>
        /// Make <see cref="TgaSoftwareVersion"/> from its <see cref="ToString"/> form: three decimal digits
        /// (the version number x 100, e.g. "117" for 1.17) optionally followed by one version letter, e.g. "123d".
        /// </summary>
        /// <param name="str">Input string, example: "123d".</param>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="str"/> is shorter than 3 characters, or its
        /// digits exceed <see cref="ushort.MaxValue"/>.</exception>
        /// <exception cref="FormatException"><paramref name="str"/> is not 3+ decimal digits followed by at most one letter.</exception>
        public TgaSoftwareVersion(string str)
        {
            ArgumentNullException.ThrowIfNull(str);
            if (str.Length < 3)
                throw new ArgumentOutOfRangeException(nameof(str), str.Length, "Length must be >= 3 (three digits and an optional version letter).");

            // Inverse of ToString: every leading digit is the number (so "1234" is not 123 + '4'), an optional trailing char the letter.
            int digits = 0;
            while (digits < str.Length && str[digits] is >= '0' and <= '9')
                digits++;
            if (digits < 3 || digits < str.Length - 1)
                throw new FormatException($"\"{str}\" must be three or more decimal digits followed by at most one version letter, e.g. \"117\" or \"117b\".");

            if (!ushort.TryParse(str.Substring(0, digits), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out ushort versionNumber))
                throw new ArgumentOutOfRangeException(nameof(str), str, $"Version number must be <= {ushort.MaxValue}.");

            VersionNumber = versionNumber;
            if (digits < str.Length) VersionLetter = str[digits];
        }

        /// <summary>
        /// Make <see cref="TgaSoftwareVersion"/> from bytes.
        /// </summary>
        /// <param name="bytes">Bytes array (byte[3]).</param>
        public TgaSoftwareVersion(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            if (bytes.Length != Size)
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length, $"Length must be {Size}.");

            VersionNumber = TgaBinary.ReadUInt16(bytes, 0);
            VersionLetter = Encoding.ASCII.GetString(bytes, 2, 1)[0];
        }

        /// <summary>
        /// Create a new instance of the <see cref="TgaSoftwareVersion"/> class.
        /// </summary>
        /// <param name="versionNumber">Set 123 for 1.23 version.</param>
        /// <param name="versionLetter">Version letter, example: for 'a' - "1.23a".</param>
        public TgaSoftwareVersion(ushort versionNumber, char versionLetter = ' ')
        {
            VersionNumber = versionNumber;
            VersionLetter = versionLetter;
        }

        /// <summary>
        /// Gets or sets the version number as a binary integer times 100 (e.g. 417 for version 4.17).
        /// </summary>
        public ushort VersionNumber { get; set; } = 0;

        /// <summary>
        /// Gets or sets the ASCII release letter appended to the version (e.g. 'b' for "1.17b"), or a space when unused.
        /// </summary>
        public char VersionLetter { get; set; } = ' ';


        /// <summary>
        /// Make full copy of <see cref="TgaSoftwareVersion"/>. Named <c>Copy</c> rather than
        /// <c>Clone</c> because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns>Full independent copy of <see cref="TgaSoftwareVersion"/>.</returns>
        public TgaSoftwareVersion Copy() => this with { };

        /// <summary>
        /// Convert <see cref="TgaSoftwareVersion"/> to byte array.
        /// </summary>
        /// <returns>Byte array, <see cref="VersionNumber"/> (2 bytes) and
        /// <see cref="VersionLetter"/> (ASCII symbol).</returns>
        public byte[] ToBytes() => ToBytes(VersionNumber, VersionLetter);

        /// <summary>
        /// Convert <see cref="TgaSoftwareVersion"/> to byte array.
        /// </summary>
        /// <param name="versionNumber">Set 123 for 1.23 version.</param>
        /// <param name="versionLetter">Version letter, example: for 'a' - "1.23a".</param>
        /// <returns>Byte array, <see cref="VersionNumber"/> (2 bytes) and <see cref="VersionLetter"/> (ASCII symbol).</returns>
        public static byte[] ToBytes(ushort versionNumber, char versionLetter = ' ') => new TgaByteBuilder(Size).Add(versionNumber).Add(Encoding.ASCII.GetBytes(versionLetter.ToString())).ToArray();

        /// <summary>
        /// Gets <see cref="TgaSoftwareVersion"/> like string.
        /// </summary>
        /// <returns><see cref="VersionNumber"/> formatted as three digits, followed by <see cref="VersionLetter"/>,
        /// with trailing spaces and NUL characters trimmed (e.g. "123d" or "007").</returns>
        public override string ToString() => (VersionNumber.ToString("000") + VersionLetter).TrimEnd(new char[] { ' ', '\0' });

        /// <inheritdoc />
        object ICloneable.Clone() => Copy();
    }
}
