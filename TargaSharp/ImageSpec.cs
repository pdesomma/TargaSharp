using System;
using System.Drawing;
using TargaSharp.Helpers;

namespace TargaSharp
{
    /// <summary>
    /// Image Specification - Field 5 (10 bytes):
    /// <para>This field and its sub-fields describe the image screen location, size and pixel depth.
    /// These information is always written to the file.</para>
    /// </summary>
    public class ImageSpec : FileField, ICloneable, IEquatable<ImageSpec>
    {
        public ImageSpec() : base(10, 8, 5) { }

        /// <summary>
        /// Make ImageSpec from values.
        /// </summary>
        /// <param name="xOrigin">These specify the absolute horizontal coordinate for the lower
        /// left corner of the image as it is positioned on a display device having an origin at
        /// the lower left of the screen(e.g., the TARGA series).</param>
        /// <param name="yOrigin">These specify the absolute vertical coordinate for the lower
        /// left corner of the image as it is positioned on a display device having an origin at
        /// the lower left of the screen(e.g., the TARGA series).</param>
        /// <param name="imageWidth">This field specifies the width of the image in pixels.</param>
        /// <param name="imageHeight">This field specifies the height of the image in pixels.</param>
        /// <param name="pixelDepth">This field indicates the number of bits per pixel. This number
        /// includes the Attribute or Alpha channel bits. Common values are 8, 16, 24 and 32 but
        /// other pixel depths could be used.</param>
        /// <param name="imageDescriptor">Contains image origin bits and alpha channel bits
        /// (or number of overlay bits).</param>
        public ImageSpec(ushort xOrigin, ushort yOrigin, ushort imageWidth, ushort imageHeight, PixelDepth pixelDepth, ImageDescriptor imageDescriptor) : this()
        {
            XOrigin = xOrigin;
            YOrigin = yOrigin;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
            PixelDepth = pixelDepth;
            ImageDescriptor = imageDescriptor ;
        }

        /// <summary>
        /// Make ImageSpec from bytes.
        /// </summary>
        /// <param name="bytes">Array of bytes(byte[10]).</param>
        public ImageSpec(byte[] bytes) : base(10, 8, 5, bytes) 
        {
            XOrigin = BitConverter.ToUInt16(bytes, 0);
            YOrigin = BitConverter.ToUInt16(bytes, 2);
            ImageWidth = BitConverter.ToUInt16(bytes, 4);
            ImageHeight = BitConverter.ToUInt16(bytes, 6);
            PixelDepth = (PixelDepth)bytes[8];
            ImageDescriptor = new ImageDescriptor(bytes[9]);
        }



        public static bool operator ==(ImageSpec item1, ImageSpec item2) => EqualityHelper.Operator(item1, item2);
        public static bool operator !=(ImageSpec item1, ImageSpec item2) => !(item1 == item2);


        /// <summary>
        /// Contains image origin bits and alpha channel bits(or number of overlay bits).
        /// </summary>
        public ImageDescriptor ImageDescriptor { get; set; } = new ImageDescriptor();

        /// <summary>
        /// This field specifies the height of the image in pixels.
        /// </summary>
        public ushort ImageHeight { get; set; }

        /// <summary>
        /// This field specifies the width of the image in pixels.
        /// </summary>
        public ushort ImageWidth { get; set; }

        /// <summary>
        /// This field indicates the number of bits per pixel. This number includes the Attribute or
        /// Alpha channel bits. Common values are 8, 16, 24 and 32 but other pixel depths could be used.
        /// </summary>
        public PixelDepth PixelDepth { get; set; } = PixelDepth.Other;

        /// <summary>
        /// Size of image.
        /// </summary>
        public Size Size
        {
            get => new Size(ImageWidth, ImageHeight);
            set => (ImageWidth, ImageHeight) = ((ushort)value.Width, (ushort)value.Height);
        }

        /// <summary>
        /// These specify the absolute horizontal coordinate for the lower left corner of the image
        /// as it is positioned on a display device having an origin at the lower left of the
        /// screen(e.g., the TARGA series).
        /// </summary>
        public ushort XOrigin { get; set; }

        /// <summary>
        /// These specify the absolute vertical coordinate for the lower left corner of the image
        /// as it is positioned on a display device having an origin at the lower left of the
        /// screen(e.g., the TARGA series).
        /// </summary>
        public ushort YOrigin { get; set; }

        /// <summary>
        /// Make full copy of <see cref="ImageSpec"/>.
        /// </summary>
        /// <returns></returns>
        public ImageSpec Clone() => new ImageSpec(ToBytes()!);
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is ImageSpec imgSpec ? Equals(imgSpec) : false;
        public bool Equals(ImageSpec? item) =>
            item is not null &&
            XOrigin == item.XOrigin &&
            YOrigin == item.YOrigin &&
            ImageWidth == item.ImageWidth &&
            ImageHeight == item.ImageHeight &&
            PixelDepth == item.PixelDepth &&
            ImageDescriptor == item.ImageDescriptor;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + XOrigin.GetHashCode();
                hash = hash * 23 + YOrigin.GetHashCode();
                hash = hash * 23 + ImageWidth.GetHashCode();
                hash = hash * 23 + ImageHeight.GetHashCode();
                hash = hash * 23 + PixelDepth.GetHashCode();

                return ImageDescriptor is null ? hash : hash * 23 + ImageDescriptor.GetHashCode();
            }
        }

        /// <summary>
        /// Convert <see cref="ImageSpec"/> to byte array.
        /// </summary>
        /// <returns>Byte array with length = 10.</returns>
        public override byte[]? ToBytes() => ByteHelper.ToBytes(XOrigin, YOrigin, ImageWidth, ImageHeight, (byte)PixelDepth, ImageDescriptor?.ToByte() ?? byte.MinValue);

        public override string ToString() => string.Format("{0}={1}, {2}={3}, {4}={5}, {6}={7}, {8}={9}, {10}={11}",
                nameof(XOrigin), XOrigin,
                nameof(YOrigin), YOrigin,
                nameof(ImageWidth), ImageWidth,
                nameof(ImageHeight), ImageHeight,
                nameof(PixelDepth), PixelDepth,
                nameof(ImageDescriptor), ImageDescriptor);


        /// <summary>
        /// How to create this object from a byte array that's been validated.
        /// </summary>
        /// <param name="bytes"></param>
        protected override void FromBytes(byte[] bytes)
        {
            XOrigin = BitConverter.ToUInt16(bytes, 0);
            YOrigin = BitConverter.ToUInt16(bytes, 2);
            ImageWidth = BitConverter.ToUInt16(bytes, 4);
            ImageHeight = BitConverter.ToUInt16(bytes, 6);
            PixelDepth = (PixelDepth)bytes[8];
            ImageDescriptor = new ImageDescriptor(bytes[9]);
        }
    }
}
