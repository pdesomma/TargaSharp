using System;
using System.Drawing;
using TargaSharp.Helpers;

namespace TargaSharp
{
    /// <summary>
    /// Key Color Field: Bytes 470-473    
    /// <para>This field contains a long value which is the key color in effect at the time the image is saved.
    /// The format is in A:R:G:B where ‘A’ (most significant byte) is the
    /// alpha channel key color(if you don’t have an alpha channel in your application,
    /// keep this byte zero [0]).</para>
    /// </summary>
    public class KeyColor : FileField, ICloneable, IEquatable<KeyColor>
    {
        /// <summary>
        /// Create a new instance of the <see cref="KeyColor"/>
        /// </summary>
        public KeyColor() : base(4, 470, 18)  { }

        /// <summary>
        /// Make <see cref="KeyColor"/> from ARGB bytes.
        /// </summary>
        /// <param name="a">Alpha value.</param>
        /// <param name="r">Red value.</param>
        /// <param name="g">Green value.</param>
        /// <param name="b">Blue value.</param>
        public KeyColor(byte a, byte r, byte g, byte b) : this()
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        /// Make <see cref="KeyColor"/> from ARGB bytes.
        /// </summary>
        /// <param name="bytes">Array of bytes(byte[4]).</param>
        public KeyColor(byte[] bytes) : base(4, 470, 18, bytes) 
        {
            Color color = Color.FromArgb(BitConverter.ToInt32(bytes, 0));
            A = color.A;
            R = color.R;
            G = color.G;
            B = color.B;
        }

        /// <summary>
        /// Make <see cref="KeyColor"/> from <see cref="int"/>.
        /// </summary>
        /// <param name="argb">32bit ARGB integer color value.</param>
        public KeyColor(int argb) : this()
        {
            Color ColorARGB = Color.FromArgb(argb);
            A = ColorARGB.A;
            R = ColorARGB.R;
            G = ColorARGB.G;
            B = ColorARGB.B;
        }

        /// <summary>
        /// Make <see cref="KeyColor"/> from <see cref="Color"/>.
        /// </summary>
        /// <param name="color">GDI+ <see cref="Color"/> value.</param>
        public KeyColor(Color color) : this()
        {
            A = color.A;
            R = color.R;
            G = color.G;
            B = color.B;
        }

        public static bool operator ==(KeyColor item1, KeyColor item2) => EqualityHelper.Operator(item1, item2);
        public static bool operator !=(KeyColor item1, KeyColor item2) => !(item1 == item2);

        /// <summary>
        /// Gets or sets alpha color value.
        /// </summary>
        public byte A { get; private set; }

        /// <summary>
        /// Gets or sets blue color value.
        /// </summary>
        public byte B { get; private set; }

        /// <summary>
        /// Gets or sets green color value.
        /// </summary>
        public byte G { get; private set; }

        /// <summary>
        /// Gets or sets red color value.
        /// </summary>
        public byte R { get; private set; }

        /// <summary>
        /// Make full independent copy of <see cref="KeyColor"/>.
        /// </summary>
        /// <returns>Copy of <see cref="KeyColor"/></returns>
        public KeyColor Clone() => new KeyColor(A, R, G, B);
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is KeyColor cKey  && Equals(cKey);

        public bool Equals(KeyColor? item) => item is not null && A == item.A && R == item.R && G == item.G && B == item.B;

        public override int GetHashCode() => ToInt().GetHashCode();

        /// <summary>
        /// Convert <see cref="KeyColor"/> to byte array.
        /// </summary>
        /// <returns>Byte array with length = 4.</returns>
        public override byte[]? ToBytes() => BitConverter.GetBytes(ToInt());

        /// <summary>
        /// Gets <see cref="KeyColor"/> like GDI+ <see cref="Color"/>.
        /// </summary>
        /// <returns><see cref="Color"/> value of <see cref="KeyColor"/>.</returns>
        public Color ToColor() => Color.FromArgb(A, R, G, B);

        /// <summary>
        /// Gets <see cref="KeyColor"/> like ARGB <see cref="int"/>.
        /// </summary>
        /// <returns>ARGB <see cref="int"/> value of <see cref="KeyColor"/>.</returns>
        public int ToInt() => ToColor().ToArgb();

        /// <summary>
        /// Gets <see cref="KeyColor"/> like string.
        /// </summary>
        /// <returns>String in ARGB format.</returns>
        public override string ToString() => string.Format("{0}={1}, {2}={3}, {4}={5}, {6}={7}", nameof(A), A, nameof(R), R, nameof(G), G, nameof(B), B);


        /// <summary>
        /// How to create this object from a byte array that's been validated.
        /// </summary>
        /// <param name="bytes"></param>
        protected override void FromBytes(byte[] bytes)
        {
            Color color = Color.FromArgb(BitConverter.ToInt32(bytes, 0));
            A = color.A;
            R = color.R;
            G = color.G;
            B = color.B;
        }
    }
}
