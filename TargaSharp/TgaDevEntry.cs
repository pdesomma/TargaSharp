namespace TargaSharp
{
    public sealed record TgaDevEntry : ICloneable
    {
        /// <summary>
        /// Make empty <see cref="TgaDevEntry"/>.
        /// </summary>
        public TgaDevEntry() { }

        /// <summary>
        /// Make <see cref="TgaDevEntry"/> from other <see cref="TgaDevEntry"/>.
        /// </summary>
        /// <param name="entry">Some <see cref="TgaDevEntry"/> variable.</param>
        public TgaDevEntry(TgaDevEntry entry)
        {
            if (entry is null) throw new ArgumentNullException(nameof(entry));
            Tag = entry.Tag;
            Offset = entry.Offset;
            Data = BitConverterHelper.ToBytes(entry.Data ?? Array.Empty<byte>());
        }

        /// <summary>
        /// Make <see cref="TgaDevEntry"/> from <see cref="Tag"/>, <see cref="Offset"/> and <see cref="FieldSize"/>.
        /// </summary>
        /// <param name="tag">TAG ID (0 - 65535). See <see cref="Tag"/>.</param>
        /// <param name="offset">TAG file offset in bytes. See <see cref="Offset"/>.</param>
        /// <param name="data">This is DevEntry Field Data. See <see cref="Data"/>.</param>
        public TgaDevEntry(ushort tag, uint offset, byte[] data = null)
        {
            Tag = tag;
            Offset = offset;
            Data = data;
        }

        /// <summary>
        /// Make <see cref="TgaDevEntry"/> from bytes. This mirrors the layout written by
        /// <see cref="ToBytes"/> - [Tag:2][Offset:4][FieldSize:4] - and NOT the field's actual
        /// payload, which is not part of the directory entry itself and is loaded separately
        /// (e.g. by <see cref="TgaFile"/>, which reads <see cref="FieldSize"/> bytes from
        /// <see cref="Offset"/> in the file). <see cref="Data"/> is therefore initialized to a
        /// zero-filled placeholder of length <see cref="FieldSize"/> so the byte layout
        /// round-trips through <see cref="ToBytes"/>.
        /// </summary>
        /// <param name="Bytes">Array of bytes, must be exactly <see cref="Size"/> (10) bytes long.</param>
        public TgaDevEntry(byte[] Bytes)
        {
            ArgumentNullException.ThrowIfNull(Bytes);
            if (Bytes.Length != Size)
                throw new ArgumentOutOfRangeException(nameof(Bytes), Bytes.Length, $"Length must be {Size}.");

            Tag = BitConverter.ToUInt16(Bytes, 0);
            Offset = BitConverter.ToUInt32(Bytes, 2);
            int fieldSize = BitConverter.ToInt32(Bytes, 6);
            Data = new byte[fieldSize];
        }



        /// <summary>
        /// Field DATA.
        /// Although the size and format of the actual Developer Area fields are totally up to the developer,
        /// please define your formats to address future considerations you might have concerning your fields.
        /// This means that if you anticipate changing a field, build flexibility into the format to make these
        /// changes easy on other developers.Major changes to an existing TAG’s definition should never happen.
        /// </summary>
        public byte[] Data { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// The FIELD SIZE is a number of bytes in the field. Same as <see cref="Data"/>.Length.
        /// </summary>
        public int FieldSize => Data.Length;

        /// <summary>
        /// This OFFSET is a number of bytes from the beginning of the file to the start of the field
        /// referenced by the tag. This is a derived field: it is computed by <see cref="TargaSharp.IO.TgaWriter"/>
        /// during layout (or read from the file by <see cref="TargaSharp.IO.TgaReader"/>, which also
        /// passes it through the <see cref="TgaDevEntry(ushort, uint, byte[])"/> constructor), so
        /// consumers cannot set it directly.
        /// </summary>
        public uint Offset { get; internal set; }

        /// <summary>
        /// Each TAG is a value in the range of 0 to 65535. Values from 0 - 32767 are available for developer use,
        /// while values from 32768 - 65535 are reserved for Truevision.
        /// </summary>
        public ushort Tag { get; set; }

        /// <summary>
        /// Gets TGA <see cref="TgaDevEntry"/> size in bytes (Always constant and equal 10!).
        /// It is not <see cref="FieldSize"/>! It is just size of entry sizeof(ushort + uint + uint).
        /// </summary>
        public const int Size = 10;



        /// <summary>
        /// Make full independed copy of <see cref="TgaDevEntry"/>. Named <c>Copy</c> rather than <c>Clone</c>
        /// because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns>Copy of <see cref="TgaDevEntry"/></returns>
        public TgaDevEntry Copy() => this with { Data = (byte[])Data.Clone() };

        /// <inheritdoc />
        object ICloneable.Clone() => Copy();

        /// <inheritdoc />
        public bool Equals(TgaDevEntry? other)
        {
            if (other is null) return false;
            return Tag == other.Tag && Offset == other.Offset &&
                (ReferenceEquals(Data, other.Data) || (Data is not null && other.Data is not null && Data.AsSpan().SequenceEqual(other.Data)));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Tag.GetHashCode();
                hash = hash * 23 + Offset.GetHashCode();
                if (Data is not null)
                    for (int i = 0; i < Data.Length; i++)
                        hash = hash * 23 + Data[i].GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Convert <see cref="TgaDevEntry"/> to byte array. (Not include <see cref="Data"/>!).
        /// </summary>
        /// <returns>Byte array with length = 10.</returns>
        public byte[] ToBytes() => BitConverterHelper.ToBytes(Tag, Offset, Data?.Length ?? 0);

        /// <summary>
        /// Gets <see cref="TgaDevEntry"/> like string.
        /// </summary>
        /// <returns>String in "Tag={0}, Offset={1}, FieldSize={2}" format.</returns>
        public override string ToString() => $"{nameof(Tag)}={Tag}, {nameof(Offset)}={Offset}, {nameof(FieldSize)}={FieldSize}";
    } 
}