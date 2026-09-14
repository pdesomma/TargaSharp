using System.Drawing.Imaging;

namespace TargaSharp.Drawing
{
    /// <summary>
    /// One row of <see cref="TgaPixelFormatMap"/>: a GDI+ <see cref="PixelFormat"/> and the TGA header values that share its byte layout.
    /// </summary>
    /// <param name="PixelFormat">GDI+ format.</param>
    /// <param name="Depth">TGA pixel depth with the same bytes per pixel.</param>
    /// <param name="AlphaBits">Attribute (alpha) bits per pixel the format carries.</param>
    /// <param name="Grayscale">Whether the format is intensity-only (a black-and-white TGA image type).</param>
    /// <param name="PreMultiplied">Whether the alpha is pre-multiplied into the color channels.</param>
    internal readonly record struct TgaPixelFormatMapping(PixelFormat PixelFormat, TgaPixelDepth Depth, byte AlphaBits, bool Grayscale, bool PreMultiplied)
    {
        /// <summary>
        /// Whether the format carries any alpha bits.
        /// </summary>
        public bool HasAlpha => AlphaBits > 0;
    }

    /// <summary>
    /// The single table of GDI+ <see cref="PixelFormat"/>s this bridge supports, used in both directions:
    /// <see cref="TgaDrawing.FromBitmap(System.Drawing.Bitmap, bool, bool, bool)"/> looks a bitmap's format up, and
    /// <see cref="TgaFileDrawingExtensions"/> resolves a file's header back to a format.
    /// </summary>
    internal static class TgaPixelFormatMap
    {
        /// <summary>
        /// Every supported format. 1/4bpp pack several pixels per byte and 48/64bpp use 16-bit channels;
        /// neither maps onto a TGA pixel depth (8/16/24/32), so they are absent.
        /// </summary>
        internal static readonly IReadOnlyList<TgaPixelFormatMapping> Mappings =
        [
            new(PixelFormat.Format8bppIndexed, TgaPixelDepth.Bpp8, 0, false, false),
            new(PixelFormat.Format16bppGrayScale, TgaPixelDepth.Bpp16, 0, true, false),
            new(PixelFormat.Format16bppRgb555, TgaPixelDepth.Bpp16, 0, false, false),
            new(PixelFormat.Format16bppArgb1555, TgaPixelDepth.Bpp16, 1, false, false),
            new(PixelFormat.Format24bppRgb, TgaPixelDepth.Bpp24, 0, false, false),
            new(PixelFormat.Format32bppRgb, TgaPixelDepth.Bpp32, 0, false, false),
            new(PixelFormat.Format32bppArgb, TgaPixelDepth.Bpp32, 8, false, false),
            new(PixelFormat.Format32bppPArgb, TgaPixelDepth.Bpp32, 8, false, true),
        ];

        /// <summary>
        /// Finds the mapping for a bitmap's <paramref name="pixelFormat"/>.
        /// </summary>
        /// <param name="pixelFormat">GDI+ format to look up.</param>
        /// <param name="mapping">The mapping when supported.</param>
        /// <returns><see langword="true"/> when <paramref name="pixelFormat"/> is supported.</returns>
        internal static bool TryGet(PixelFormat pixelFormat, out TgaPixelFormatMapping mapping)
        {
            foreach (TgaPixelFormatMapping m in Mappings)
            {
                if (m.PixelFormat == pixelFormat)
                {
                    mapping = m;
                    return true;
                }
            }

            mapping = default;
            return false;
        }

        /// <summary>
        /// Resolves the <see cref="PixelFormat"/> holding the same byte layout as a file with <paramref name="depth"/>.
        /// 16bpp grayscale resolves to <see cref="PixelFormat.Format8bppIndexed"/> (the caller keeps each pixel's high byte)
        /// because GDI+ cannot read, draw, clone or save <see cref="PixelFormat.Format16bppGrayScale"/>.
        /// </summary>
        /// <param name="depth">File pixel depth.</param>
        /// <param name="useAlpha">Whether the per-pixel attribute bits are meaningful alpha.</param>
        /// <param name="grayscale">Whether the image type is black-and-white.</param>
        /// <param name="preMultiplied">Whether the alpha is pre-multiplied (only honored with <paramref name="useAlpha"/>).</param>
        /// <returns>The matching <see cref="PixelFormat"/>.</returns>
        /// <exception cref="NotSupportedException"><paramref name="depth"/> has no GDI+ equivalent.</exception>
        internal static PixelFormat Resolve(TgaPixelDepth depth, bool useAlpha, bool grayscale, bool preMultiplied)
        {
            if (grayscale && depth == TgaPixelDepth.Bpp16)
                return PixelFormat.Format8bppIndexed;

            // Grayscale rows are never resolved to (see above); 8bpp indexed serves both palette and gray bytes.
            TgaPixelFormatMapping[] candidates = Mappings.Where(m => m.Depth == depth && !m.Grayscale).ToArray();
            if (candidates.Length == 0)
                throw new NotSupportedException($"{nameof(TgaPixelDepth)} {(byte)depth} has no {nameof(PixelFormat)} equivalent.");

            // Exact alpha/pre-multiplied match, else the depth's plain alpha layout, else its opaque layout.
            bool wantPreMultiplied = useAlpha && preMultiplied;
            TgaPixelFormatMapping? best =
                Find(candidates, m => m.HasAlpha == useAlpha && m.PreMultiplied == wantPreMultiplied)
                ?? Find(candidates, m => m.HasAlpha == useAlpha && !m.PreMultiplied)
                ?? Find(candidates, m => !m.HasAlpha);
            return best!.Value.PixelFormat;
        }

        /// <summary>
        /// First candidate satisfying <paramref name="predicate"/>, or <see langword="null"/>.
        /// </summary>
        /// <param name="candidates">Mappings to search.</param>
        /// <param name="predicate">Match condition.</param>
        /// <returns>The first match or <see langword="null"/>.</returns>
        private static TgaPixelFormatMapping? Find(TgaPixelFormatMapping[] candidates, Func<TgaPixelFormatMapping, bool> predicate)
        {
            foreach (TgaPixelFormatMapping m in candidates)
                if (predicate(m)) return m;
            return null;
        }
    }
}
