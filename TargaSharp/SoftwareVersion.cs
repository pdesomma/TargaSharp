using System;
using System.Text;
using TargaSharp.Helpers;

namespace TargaSharp
{
    /// <summary>
    /// The purpose of this field is to define the version of software defined by the “Software ID” field.
    /// </summary>
    public class SoftwareVersion : FileField, ICloneable, IEquatable<SoftwareVersion>
    {
        public const int Length = 3;


        /// <summary>
        /// Gets Empty <see cref="SoftwareVersion"/>, <see cref="VersionLetter"/> = ' ' (space).
        /// </summary>
        public SoftwareVersion() : base(Length, 467, 17) { }

        /// <summary>
        /// Make <see cref="SoftwareVersion"/> from string.
        /// </summary>
        /// <param name="str">Input string, example: "123d".</param>
        public SoftwareVersion(string str) : this()
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(str);
#else 
            if (str is null) throw new ArgumentNullException("str");
#endif
            if (str.Length < 3 || str.Length > 4) throw new ArgumentOutOfRangeException(nameof(str.Length) + " must be 3 or 4!");            
            if (ushort.TryParse(str.Substring(0, 3), out _ ) && str.Length == 4) VersionLetter = str[3];
        }

        /// <summary>
        /// Make <see cref="SoftwareVersion"/> from bytes.
        /// </summary>
        /// <param name="Bytes">Bytes array (byte[3]).</param>
        public SoftwareVersion(byte[] bytes) : base(Length, 467, 17, bytes) 
        {
            VersionNumber = BitConverter.ToUInt16(bytes, 0);
            VersionLetter = Encoding.ASCII.GetString(bytes, 2, 1)[0];
        }

        public SoftwareVersion(ushort versionNumber, char versionLetter = ' ') : this()
        { 
            VersionNumber = versionNumber;
            VersionLetter = versionLetter;
        }

        public static bool operator ==(SoftwareVersion item1, SoftwareVersion item2) => EqualityHelper.Operator(item1, item2);
        public static bool operator !=(SoftwareVersion item1, SoftwareVersion item2) => !(item1 == item2);

        public ushort VersionNumber { get; set; }
        public char VersionLetter { get; set; } = ' ';


        /// <summary>
        /// Make full copy of <see cref="SoftwareVersion"/>.
        /// </summary>
        /// <returns></returns>
        public SoftwareVersion Clone() => new SoftwareVersion(VersionNumber, VersionLetter);
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is SoftwareVersion version && Equals(version);
        public bool Equals(SoftwareVersion item) => VersionNumber == item.VersionNumber && VersionLetter == item.VersionLetter;

        public override int GetHashCode() => VersionNumber.GetHashCode() ^ VersionLetter.GetHashCode();

        /// <summary>
        /// Convert <see cref="SoftwareVersion"/> to byte array.
        /// </summary>
        /// <returns>Byte array, <see cref="VersionNumber"/> (2 bytes) and
        /// <see cref="VersionLetter"/> (ASCII symbol).</returns>
        public override byte[]? ToBytes() => ToBytes(VersionNumber, VersionLetter);

        /// <summary>
        /// Convert <see cref="SoftwareVersion"/> to byte array.
        /// </summary>
        /// <param name="versionNumber">Set 123 for 1.23 version.</param>
        /// <param name="versionLetter">Version letter, example: for 'a' - "1.23a".</param>
        /// <returns>Byte array, <see cref="VersionNumber"/> (2 bytes) and <see cref="VersionLetter"/> (ASCII symbol).</returns>
        public static byte[]? ToBytes(ushort versionNumber, char versionLetter = ' ') => ByteHelper.ToBytes(versionNumber, Encoding.ASCII.GetBytes(versionLetter.ToString()));

        public override string ToString() => (VersionNumber.ToString("000") + VersionLetter).TrimEnd(
#if NET8_0_OR_GREATER
            [' ', '\0']
#else
            new char[] { ' ', '\0' }
#endif  
        );


        /// <summary>
        /// How to create this object from a byte array that's been validated.
        /// </summary>
        /// <param name="bytes"></param>
        protected override void FromBytes(byte[] bytes)
        {
            VersionNumber = BitConverter.ToUInt16(bytes, 0);
            VersionLetter = Encoding.ASCII.GetString(bytes, 2, 1)[0];
        }
    }
}
