namespace TargaSharp
{
    /// <summary>
    /// Image Or ColorMap Area
    /// </summary>
    public class TgaImgOrColMap : ICloneable
    {
        /// <summary>
        /// Make empty <see cref="TgaImgOrColMap"/>.
        /// </summary>
        public TgaImgOrColMap() { }

        /// <summary>
        /// Make <see cref="TgaImgOrColMap"/> from arrays.
        /// </summary>
        /// <param name="imageID">This optional field contains identifying information about the image.
        /// The maximum length for this field is 255 bytes. Refer to <see cref="TgaHeader.IDLength"/>
        /// for the length of this field. If field 1 is set to Zero indicating that no Image ID exists
        /// then these bytes are not written to the file.</param>
        /// <param name="colorMapData">Color Map Data, see <see cref="ColorMapData"/> description.</param>
        /// <param name="imageData">Image Data, see <see cref="ImageData"/> description.</param>
        public TgaImgOrColMap(TgaString? imageID, byte[]? colorMapData, byte[]? imageData)
        {
            ImageID = imageID;
            ColorMapData = colorMapData;
            ImageData = imageData;
        }


        public static bool operator ==(TgaImgOrColMap item1, TgaImgOrColMap item2)
        {
            if (item1 is null) return item2 is null;
            if (item2 is null) return item1 is null;
            return item1.Equals(item2);
        }
        public static bool operator !=(TgaImgOrColMap item1, TgaImgOrColMap item2) => !(item1 == item2);


        /// <summary>
        /// Image ID - Field 6 (variable):
        /// <para>This optional field contains identifying information about the image. The maximum length
        /// for this field is 255 bytes. Refer to <see cref="TgaHeader.IDLength"/> for the length of this
        /// field. If field 1 is set to Zero indicating that no Image ID exists then these bytes are not
        /// written to the file. Can have text inside (ASCII).</para>
        /// </summary>
        public TgaString? ImageID { get; set; }

        /// <summary>
        /// Color Map Data - Field 7 (variable):
        /// <para>If the Color Map Type(field 2) field is set to zero indicating that no Color-Map
        /// exists then this field will not be present (i.e., no bytes written to the file).</para>
        /// <para>This variable-length field contains the actual color map information (LUT data).
        /// Field 4.3 specifies the width in bits of each color map entry while Field 4.2 specifies
        /// the number of color map entries in this field. These two fields together are used to
        /// determine the number of bytes contained in field 7.</para>
        /// <para>Each color map entry is stored using an integral number of bytes.The RGB specification
        /// for each color map entry is stored in successive bit-fields in the multi-byte entries.
        /// Each color bit-field is assumed to be MIN(Field4.3/3, 8) bits in length. If Field 4.3
        /// contains 24, then each color specification is 8 bits in length; if Field 4.3 contains 32,
        /// then each color specification is also 8 bits (32/3 gives 10, but 8 is smaller).
        /// Unused bit(s) in the multi-byte entries are assumed to specify attribute bits. The
        /// attribute bit field is often called the Alpha Channel, Overlay Bit(s) or Interrupt Bit(s).</para>
        /// For the TARGA M-8, ATVista and NuVista, the number of bits in a color map specification is
        /// 24 (or 32). The red, green, and blue components are each represented by one byte.
        /// </summary>
        public byte[]? ColorMapData { get; set; }

        /// <summary>
        /// Image Data - Field 8 (variable):
        /// <para>This field contains (Width)x(Height) pixels. Each pixel specifies image data in one
        /// of the following formats:</para>
        /// <para>a single color-map index for Pseudo-Color;
        /// Attribute, Red, Green and Blue ordered data for True-Color;
        /// and independent color-map indices for Direct-Color.</para>
        /// <para>The values for Width and Height are specified in Fields 5.3 and 5.4 respectively.
        /// The number of attribute and color-definition bits for each pixel are defined in Fields 5.6
        /// and 5.5, respectively.Each pixel is stored as an integral number of bytes.</para>
        /// <para>Concretely, this implementation stores each pixel in little-endian BGR(A) byte
        /// order: for 32bpp True-Color, byte 0 = Blue, byte 1 = Green, byte 2 = Red, byte 3 = Alpha
        /// (attribute); for 24bpp, the same without the alpha byte. For 16bpp, each pixel is a
        /// little-endian ushort laid out as ARRRRRGGGGGBBBBB (1 attribute/alpha bit, 5 bits each
        /// for red, green and blue).</para>
        /// </summary>
        public byte[]? ImageData { get; set; }

        /// <summary>
        /// Make full copy of <see cref="TgaImgOrColMap"/>.
        /// </summary>
        /// <returns>Full independed copy of <see cref="TgaImgOrColMap"/>.</returns>
        public TgaImgOrColMap Clone() => new TgaImgOrColMap(ImageID?.Clone(), (byte[]?)ColorMapData?.Clone(), (byte[]?)ImageData?.Clone());
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is TgaImgOrColMap ? Equals((TgaImgOrColMap)obj) : false;
        public bool Equals(TgaImgOrColMap item) => ImageID == item.ImageID && BitConverterHelper.IsArraysEqual(ColorMapData, item.ColorMapData) && BitConverterHelper.IsArraysEqual(ImageData, item.ImageData);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 27;
                if (ImageID is not null) hash = (13 * hash) + ImageID.GetHashCode();
                if (ColorMapData is not null)
                    for (int i = 0; i < ColorMapData.Length; i++)
                        hash = (13 * hash) + ColorMapData[i].GetHashCode();
                if (ImageData is not null)
                    for (int i = 0; i < ImageData.Length; i++)
                        hash = (13 * hash) + ImageData[i].GetHashCode();
                return hash;
            }
        }
    }
}