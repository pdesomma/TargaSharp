using System;
using TargaSharp.Helpers;

namespace TargaSharp
{

    /// <summary>
    /// Image Or ColorMap Area
    /// </summary>
    public class ImageOrColorMapArea : ICloneable, IEquatable<ImageOrColorMapArea>
    {
        /// <summary>
        /// Make empty <see cref="ImageOrColorMapArea"/>.
        /// </summary>
        public ImageOrColorMapArea() { }

        /// <summary>
        /// Make <see cref="ImageOrColorMapArea"/> from arrays.
        /// </summary>
        /// <param name="imageId">This optional field contains identifying information about the image.
        /// The maximum length for this field is 255 bytes. Refer to <see cref="TgaHeader.IDLength"/>
        /// for the length of this field. If field 1 is set to Zero indicating that no Image ID exists
        /// then these bytes are not written to the file.</param>
        /// <param name="colorMapData">Color Map Data, see <see cref="ColorMapData"/> description.</param>
        /// <param name="imageData">Image Data, see <see cref="ImageData"/> description.</param>
        public ImageOrColorMapArea(TgaString? imageId, byte[]? colorMapData, byte[]? imageData)
        {
            ImageId = imageId;
            ColorMapData = colorMapData;
            ImageData = imageData;
        }

        public static bool operator ==(ImageOrColorMapArea item1, ImageOrColorMapArea item2) => EqualityHelper.Operator(item1, item2);
        public static bool operator !=(ImageOrColorMapArea item1, ImageOrColorMapArea item2) => !(item1 == item2);


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
        /// </summary>
        public byte[]? ImageData { get; set; }

        /// <summary>
        /// Image ID - Field 6 (variable):
        /// <para>This optional field contains identifying information about the image. The maximum length
        /// for this field is 255 bytes. Refer to <see cref="TgaHeader.IDLength"/> for the length of this
        /// field. If field 1 is set to Zero indicating that no Image ID exists then these bytes are not
        /// written to the file. Can have text inside (ASCII).</para>
        /// </summary>
        public TgaString? ImageId { get; set; }


        /// <summary>
        /// Make full copy of <see cref="ImageOrColorMapArea"/>.
        /// </summary>
        /// <returns>Full independent copy of <see cref="ImageOrColorMapArea"/>.</returns>
        public ImageOrColorMapArea Clone() => new(ImageId?.Clone(), (byte[]?)ColorMapData?.Clone(), (byte[]?)ImageData?.Clone());
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is ImageOrColorMapArea map && Equals(map);
        public bool Equals(ImageOrColorMapArea? item) => item is not null && ImageId == item.ImageId &&
                EnumerableHelper.AreEnumerablesEqual(ColorMapData, item.ColorMapData) &&
                EnumerableHelper.AreEnumerablesEqual(ImageData, item.ImageData);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 27;
                if (ImageId is not null)
                    hash = (13 * hash) + ImageId.GetHashCode();
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
