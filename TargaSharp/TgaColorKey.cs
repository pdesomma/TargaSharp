using System.Drawing;

namespace TargaSharp
{
    public class TgaColorKey : ICloneable
    {
        /// <summary>
        /// Empty constructor (for serialization?)
        /// </summary>
        public TgaColorKey() { }

        /// <summary>
        /// Make <see cref="TgaColorKey"/> from ARGB bytes.
        /// </summary>
        /// <param name="A">Alpha value.</param>
        /// <param name="R">Red value.</param>
        /// <param name="G">Green value.</param>
        /// <param name="B">Blue value.</param>
        public TgaColorKey(byte A, byte R, byte G, byte B)
        {
            this.A = A;
            this.R = R;
            this.G = G;
            this.B = B;
        }

        /// <summary>
        /// Make <see cref="TgaColorKey"/> from ARGB bytes.
        /// </summary>
        /// <param name="bytes">Array of bytes(byte[4]).</param>
        /// <exception cref="ArgumentNullException"></exception>
        public TgaColorKey(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            if (bytes.Length != Size)
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length, $"Length must be {Size}.");
            Color color = Color.FromArgb(System.BitConverter.ToInt32(bytes, 0));
            A = color.A;
            R = color.R;
            G = color.G;
            B = color.B;
        }

        /// <summary>
        /// Make <see cref="TgaColorKey"/> from <see cref="int"/>.
        /// </summary>
        /// <param name="ARGB">32bit ARGB integer color value.</param>
        public TgaColorKey(int ARGB) : this(Color.FromArgb(ARGB)) { }

        /// <summary>
        /// Make <see cref="TgaColorKey"/> from <see cref="Color"/>.
        /// </summary>
        /// <param name="color">GDI+ <see cref="Color"/> value.</param>
        public TgaColorKey(Color color) : this(color.A, color.R, color.G, color.B) { }

        public static bool operator == (TgaColorKey item1, TgaColorKey item2)
        {
            if (item1 is null) return item2 is null;
            if (item2 is null) return item1 is null;
            return item1.Equals(item2);
        }

        public static bool operator != (TgaColorKey item1, TgaColorKey item2) => !(item1 == item2);

        /// <summary>
        /// Gets TGA Field size in bytes.
        /// </summary>
        public const int Size = 4;

        /// <summary>
        /// Gets or sets alpha color value.
        /// </summary>
        public byte A { get; set; }

        /// <summary>
        /// Gets or sets red color value.
        /// </summary>
        public byte R { get; set; }

        /// <summary>
        /// Gets or sets green color value.
        /// </summary>
        public byte G { get; set; }

        /// <summary>
        /// Gets or sets blue color value.
        /// </summary>
        public byte B { get; set; }

        /// <summary>
        /// Make full independed copy of <see cref="TgaColorKey"/>.
        /// </summary>
        /// <returns>Copy of <see cref="TgaColorKey"/></returns>
        public TgaColorKey Clone() => new TgaColorKey(A, R, G, B);

        /// <summary>
        /// Make full independed copy of <see cref="TgaColorKey"/>.
        /// </summary>
        /// <returns>Copy of <see cref="TgaColorKey"/></returns>
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is not null && ((obj is TgaColorKey) ? Equals((TgaColorKey)obj) : false);

        public bool Equals(TgaColorKey item) => A == item.A && R == item.R && G == item.G && B == item.B;

        public override int GetHashCode() => ToInt().GetHashCode();


        /// <summary>
        /// Convert <see cref="TgaColorKey"/> to byte array.
        /// </summary>
        /// <returns>Byte array with length = 4.</returns>
        public byte[] ToBytes() => System.BitConverter.GetBytes(ToInt());

        /// <summary>
        /// Gets <see cref="TgaColorKey"/> like GDI+ <see cref="Color"/>.
        /// </summary>
        /// <returns><see cref="Color"/> value of <see cref="TgaColorKey"/>.</returns>
        public Color ToColor() => Color.FromArgb(A, R, G, B);

        /// <summary>
        /// Gets <see cref="TgaColorKey"/> like ARGB <see cref="int"/>.
        /// </summary>
        /// <returns>ARGB <see cref="int"/> value of <see cref="TgaColorKey"/>.</returns>
        public int ToInt() => ToColor().ToArgb();

        /// <summary>
        /// Gets <see cref="TgaColorKey"/> like string.
        /// </summary>
        /// <returns>String in ARGB format.</returns>
        public override string ToString() => string.Format("{0}={1}, {2}={3}, {4}={5}, {6}={7}", nameof(A), A, nameof(R), R, nameof(G), G, nameof(B), B);
    }
}
