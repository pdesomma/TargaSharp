using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace TargaSharp.Drawing
{
    /// <summary>
    /// Copies pixel rows between a GDI+ <see cref="Bitmap"/> and an unpadded row-major buffer, honoring GDI+'s
    /// own stride (4-byte row padding, or a negative stride for bottom-up surfaces) instead of assuming it.
    /// </summary>
    internal static class TgaBitmapRows
    {
        /// <summary>
        /// Unpadded bytes per row of <paramref name="bmp"/>.
        /// </summary>
        /// <param name="bmp">Bitmap to measure.</param>
        /// <returns>Width * bytes per pixel.</returns>
        internal static int StrideBytes(Bitmap bmp) => bmp.Width * (Image.GetPixelFormatSize(bmp.PixelFormat) / 8);

        /// <summary>
        /// Reads every pixel of <paramref name="bmp"/> into an unpadded row-major buffer.
        /// </summary>
        /// <param name="bmp">Source bitmap; must be at least 8 bits per pixel.</param>
        /// <returns>Width * Height * bytes-per-pixel bytes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bmp"/> is <see langword="null"/>.</exception>
        internal static byte[] Read(Bitmap bmp)
        {
            ArgumentNullException.ThrowIfNull(bmp);

            int strideBytes = StrideBytes(bmp);
            byte[] pixels = new byte[strideBytes * bmp.Height];

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            try
            {
                for (int y = 0; y < bmp.Height; y++)
                    Marshal.Copy(bmpData.Scan0 + (nint)y * bmpData.Stride, pixels, y * strideBytes, strideBytes);
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }

            return pixels;
        }

        /// <summary>
        /// Writes unpadded row-major <paramref name="pixels"/> into <paramref name="bmp"/>'s buffer one row at a time.
        /// </summary>
        /// <param name="bmp">Destination bitmap; must be at least 8 bits per pixel.</param>
        /// <param name="pixels">Exactly Width * Height * bytes-per-pixel bytes.</param>
        /// <exception cref="ArgumentNullException"><paramref name="bmp"/> or <paramref name="pixels"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="pixels"/> is not the bitmap's unpadded size.</exception>
        internal static void Write(Bitmap bmp, byte[] pixels)
        {
            ArgumentNullException.ThrowIfNull(bmp);
            ArgumentNullException.ThrowIfNull(pixels);

            int strideBytes = StrideBytes(bmp);
            long expected = (long)strideBytes * bmp.Height;
            // Marshal.Copy into Scan0 is unchecked: a too-long buffer corrupts the GDI+ heap and kills the process.
            if (pixels.Length != expected)
                throw new ArgumentException($"{pixels.Length} bytes but {bmp.Width}x{bmp.Height} {bmp.PixelFormat} needs {expected}.", nameof(pixels));

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
            try
            {
                for (int y = 0; y < bmp.Height; y++)
                    Marshal.Copy(pixels, y * strideBytes, bmpData.Scan0 + (nint)y * bmpData.Stride, strideBytes);
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }
        }
    }
}
