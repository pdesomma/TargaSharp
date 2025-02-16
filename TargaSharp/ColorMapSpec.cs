using System;
using TargaSharp.Helpers;

namespace TargaSharp
{
    /// <summary>
    /// This field (5 bytes) and its sub-fields describe the color map (if any) used for the image.
    /// If the Color Map Type field is set to zero, indicating that no color map exists, then
    /// these 5 bytes should be set to zero. These bytes always must be written to the file.
    /// </summary>
    public class ColorMapSpec : FileField, ICloneable, IEquatable<ColorMapSpec>
    {
        /// <summary>
        /// Make new <see cref="ColorMapSpec"/>.
        /// </summary>
        public ColorMapSpec() : base(5, 3, 4) { }

        /// <summary>
        /// Make <see cref="ColorMapSpec"/> from bytes.
        /// </summary>
        /// <param name="bytes">Array of bytes(byte[5]).</param>
        public ColorMapSpec(byte[] bytes) : base(5, 3, 4, bytes) 
        {
            FirstEntryIndex = BitConverter.ToUInt16(bytes, 0);
            ColorMapLength = BitConverter.ToUInt16(bytes, 2);
            EntrySize = (ColorMapEntrySize)bytes[4];
        }


        public static bool operator ==(ColorMapSpec item1, ColorMapSpec item2) => EqualityHelper.Operator(item1, item2);
        public static bool operator !=(ColorMapSpec item1, ColorMapSpec item2) => !(item1 == item2);

        /// <summary>
        /// Field 4.2 (2 bytes):
        /// Total number of color map entries included.
        /// </summary>
        public ushort ColorMapLength { get; set; }

        /// <summary>
        /// Field 4.3 (1 byte):
        /// Establishes the number of bits per entry. Typically 15, 16, 24 or 32-bit values are used.
        /// <para>When working with VDA or VDA/D cards it is preferred that you select 16 bits(5 bits
        /// per primary with 1 bit to select interrupt control) and set the 16th bit to 0 so that the
        /// interrupt bit is disabled. Even if this field is set to 15 bits(5 bits per primary) you
        /// must still parse the color map data 16 bits at a time and ignore the 16th bit.</para>
        /// <para>When working with a TARGA M8 card you would select 24 bits (8 bits per primary)
        /// since the color map is defined as 256 entries of 24 bit color values.</para>
        /// When working with a TrueVista card(ATVista or NuVista) you would select 24-bit(8 bits per
        /// primary) or 32-bit(8 bits per primary including Alpha channel) depending on your
        /// application’s use of look-up tables. It is suggested that when working with 16-bit and
        /// 32-bit color images, you store them as True-Color images and do not use the color map 
        /// field to store look-up tables. Please refer to the TGA Extensions for fields better suited
        /// to storing look-up table information.
        /// </summary>
        public ColorMapEntrySize EntrySize { get; set; } = ColorMapEntrySize.Other;

        /// <summary>
        /// Field 4.1 (2 bytes):
        /// Index of the first color map entry. Index refers to the starting entry in loading
        /// the color map.
        /// <para>Example: If you would have 1024 entries in the entire color map but you only
        /// need to store 72 of those entries, this field allows you to start in the middle of
        /// the color-map (e.g., position 342).</para>
        /// </summary>
        public ushort FirstEntryIndex { get; set; }



        /// <summary>
        /// Make full independent copy of <see cref="ColorMapSpec"/>.
        /// </summary>
        /// <returns>Copy of <see cref="ColorMapSpec"/></returns>
        public ColorMapSpec Clone() => new(ToBytes()!);
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is ColorMapSpec spec && Equals(spec);
        public bool Equals(ColorMapSpec? item) =>
            item is not null && 
            FirstEntryIndex == item.FirstEntryIndex &&
            ColorMapLength == item.ColorMapLength &&
            EntrySize == item.EntrySize;

        public override int GetHashCode()
        {
            unchecked
            {
                return (FirstEntryIndex << 16 | ColorMapLength).GetHashCode() ^ EntrySize.GetHashCode();
            }
        }

        /// <summary>
        /// Convert ColorMapSpec to byte array.
        /// </summary>
        /// <returns>Byte array with length = 5.</returns>
        public override byte[]? ToBytes() => ByteHelper.ToBytes(FirstEntryIndex, ColorMapLength, (byte)EntrySize);

        public override string ToString() => string.Format("{0}={1}, {2}={3}, {4}={5}", 
            nameof(FirstEntryIndex), FirstEntryIndex, nameof(ColorMapLength), ColorMapLength, nameof(EntrySize), EntrySize);


        /// <summary>
        /// How to create this object from a byte array that's been validated.
        /// </summary>
        /// <param name="bytes"></param>
        protected override void FromBytes(byte[] bytes)
        {
            FirstEntryIndex = BitConverter.ToUInt16(bytes, 0);
            ColorMapLength = BitConverter.ToUInt16(bytes, 2);
            EntrySize = (ColorMapEntrySize)bytes[4];
        }
    }
}