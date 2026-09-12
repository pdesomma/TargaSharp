using System.Drawing;

namespace TargaSharp.Drawing
{
    /// <summary>
    /// GDI+ <see cref="Color"/> bridge for <see cref="TgaColorKey"/>.
    /// </summary>
    public static class TgaColorKeyDrawingExtensions
    {
        /// <summary>
        /// Gets <paramref name="colorKey"/> as a GDI+ <see cref="Color"/>.
        /// </summary>
        /// <param name="colorKey">Source <see cref="TgaColorKey"/>.</param>
        /// <returns><see cref="Color"/> value of <paramref name="colorKey"/>.</returns>
        public static Color ToColor(this TgaColorKey colorKey) => Color.FromArgb(colorKey.A, colorKey.R, colorKey.G, colorKey.B);

        /// <summary>
        /// Makes a <see cref="TgaColorKey"/> from a GDI+ <see cref="Color"/>.
        /// </summary>
        /// <param name="color">GDI+ <see cref="Color"/> value.</param>
        /// <returns>New <see cref="TgaColorKey"/> matching <paramref name="color"/>.</returns>
        public static TgaColorKey FromColor(Color color) => new TgaColorKey(color.A, color.R, color.G, color.B);
    }
}
