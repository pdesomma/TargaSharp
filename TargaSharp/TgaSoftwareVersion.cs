using System.Text;

namespace TargaSharp
{
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
        /// Make <see cref="TgaSoftwareVersion"/> from string.
        /// </summary>
        /// <param name="str">Input string, example: "123d".</param>
        public TgaSoftwareVersion(string str)
        {
            if (str == null) throw new ArgumentNullException();
            if (str.Length < 3 || str.Length > 4) throw new ArgumentOutOfRangeException(nameof(str.Length) + " must be equal 3 or 4!");

            bool ok = ushort.TryParse(str.Substring(0, 3), out var verNum);
            VersionNumber = verNum;
            if (ok && str.Length == 4) VersionLetter = str[3];
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
        /// <param name="versionNumber"></param>
        /// <param name="versionLetter"></param>
        public TgaSoftwareVersion(ushort versionNumber, char versionLetter = ' ')
        {
            VersionNumber = versionNumber;
            VersionLetter = versionLetter;
        }


        public ushort VersionNumber { get; set; } = 0;
        public char VersionLetter { get; set; } = ' ';


        /// <summary>
        /// Make full copy of <see cref="TgaSoftwareVersion"/>. Named <c>Copy</c> rather than
        /// <c>Clone</c> because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns></returns>
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

        public override string ToString() => (VersionNumber.ToString("000") + VersionLetter).TrimEnd(new char[] { ' ', '\0' });

        /// <inheritdoc />
        object ICloneable.Clone() => Copy();
    }
}
