using System;
using TargaSharp.Helpers;

namespace TargaSharp
{
    /// <summary>
    /// Entry in the developer area.
    /// </summary>
    public class DeveloperTag : FileField, ICloneable, IEquatable<DeveloperTag>
    {
        /// <summary>
        /// Make empty <see cref="DeveloperTag"/>.
        /// </summary>
        public DeveloperTag() : base(10) { }

        /// <summary>
        /// Make <see cref="DeveloperTag"/> from other <see cref="DeveloperTag"/>.
        /// </summary>
        /// <param name="entry">Some <see cref="DeveloperTag"/> variable.</param>
        public DeveloperTag(DeveloperTag entry) : base(10,entry?.Offset ?? 0, 0)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(entry);
#else 
            if(entry is null) throw new ArgumentNullException(nameof(entry));
#endif

            Tag = entry.Tag;
            Data = entry.Data;
        }

        /// <summary>
        /// Make <see cref="DeveloperTag"/> from <see cref="Tag"/>, <see cref="Offset"/> and <see cref="FieldSize"/>.
        /// </summary>
        /// <param name="tag">TAG ID (0 - 65535). See <see cref="Tag"/>.</param>
        /// <param name="offset">TAG file offset in bytes. See <see cref="Offset"/>.</param>
        /// <param name="data">This is DevEntry Field Data. See <see cref="Data"/>.</param>
        public DeveloperTag(ushort tag, uint offset, byte[]? data = null) : base(10, offset, 0)
        {
            Tag = tag;
            Data = data;
        }

        /// <summary>
        /// Make <see cref="DeveloperTag"/> from bytes.
        /// </summary>
        /// <param name="bytes">Array of bytes(byte[6] or bigger, if <see cref="Data"/> exist).</param>
        public DeveloperTag(byte[] bytes) : base(10, 0, 0, bytes) 
        {
            Tag = BitConverter.ToUInt16(bytes, 0);
            Offset = BitConverter.ToUInt32(bytes, 2);

            if (bytes.Length > 6) Data = EnumerableHelper.GetElements<byte>(bytes, 6, (uint)bytes.Length - 6);
        }

        public static bool operator ==(DeveloperTag item1, DeveloperTag item2) => EqualityHelper.Operator(item1, item2);
        public static bool operator !=(DeveloperTag item1, DeveloperTag item2) => !(item1 == item2);

        /// <summary>
        /// Field DATA.
        /// Although the size and format of the actual Developer Area fields are totally up to the developer,
        /// please define your formats to address future considerations you might have concerning your fields.
        /// This means that if you anticipate changing a field, build flexibility into the format to make these
        /// changes easy on other developers.Major changes to an existing TAG’s definition should never happen.
        /// </summary>
        public byte[]? Data { get; private set; }

        /// <summary>
        /// The FIELD SIZE is a number of bytes in the field. Same like: <see cref="Data.Length"/>,
        /// if <see cref="Data"/> is null, return -1.
        /// </summary>
        public int FieldSize => Data?.Length ?? -1;

        /// <summary>
        /// Each TAG is a value in the range of 0 to 65535. Values from 0 - 32767 are available for developer use,
        /// while values from 32768 - 65535 are reserved for Truevision.
        /// </summary>
        public ushort Tag { get; private set; }

        /// <summary>
        /// Make full independent copy of <see cref="DeveloperTag"/>.
        /// </summary>
        /// <returns>Copy of <see cref="DeveloperTag"/></returns>
        public DeveloperTag Clone() => new DeveloperTag(this);
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is DeveloperTag de ? Equals(de) : false;
        public bool Equals(DeveloperTag? item) =>
            item is not null &&
            Tag == item.Tag &&
            Offset == item.Offset &&
            EnumerableHelper.AreEnumerablesEqual(Data, item.Data);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Tag.GetHashCode();
                hash = hash * 23 + Offset.GetHashCode();

                if (Data != null)
                    for (int i = 0; i < Data.Length; i++)
                        hash = hash * 23 + Data[i].GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Convert <see cref="DeveloperTag"/> to byte array. (Not include <see cref="Data"/>!).
        /// </summary>
        /// <returns>Byte array with length = 10.</returns>
        public override byte[]? ToBytes() => ByteHelper.ToBytes(Tag, Offset, Data?.Length ?? 0);

        /// <summary>
        /// Gets <see cref="DeveloperTag"/> like string.
        /// </summary>
        /// <returns>String in "Tag={0}, Offset={1}, FieldSize={2}" format.</returns>
        public override string ToString() => string.Format("{0}={1}, {1}={2}, {3}={4}", nameof(Tag), Tag, nameof(Offset), Offset, nameof(FieldSize), FieldSize);


        /// <summary>
        /// How to create this object from a byte array that's been validated.
        /// </summary>
        /// <param name="bytes"></param>
        protected override void FromBytes(byte[] bytes)
        {
            Tag = BitConverter.ToUInt16(bytes, 0);
            Offset = BitConverter.ToUInt32(bytes, 2);
            if (bytes.Length > 6) Data = EnumerableHelper.GetElements(bytes, 6, (uint)bytes.Length - 6);
        }

        /// <summary>
        /// Byte arrays in the constructor should match the size exactly.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        protected override bool ValidateByteLength(byte[] bytes) => bytes.Length >= 6;
    }
}
