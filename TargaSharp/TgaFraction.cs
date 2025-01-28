using System;
using TargaSharp.Helpers;

namespace TargaSharp
{
    public class TgaFraction : ByteSized, IByteConvert, ICloneable, IEquatable<TgaFraction>
    {
        public const int Length = 4;
        /// <summary>
        /// Make <see cref="TgaFraction"/> from <see cref="Numerator"/> and <see cref="Denominator"/>.
        /// </summary>
        /// <param name="Numerator">Numerator value.</param>
        /// <param name="Denominator">Denominator value.</param>
        public TgaFraction(ushort numerator = 0, ushort denominator = 0) : base(Length)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        /// <summary>
        /// Make <see cref="TgaFraction"/> from bytes.
        /// </summary>
        /// <param name="bytes">Array of bytes(byte[4]).</param>
        public TgaFraction(byte[] bytes) : this()
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(bytes);
#else 
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));
#endif
            if (bytes.Length != ByteSize) throw new ArgumentOutOfRangeException(nameof(bytes.Length) + " must be equal " + ByteSize + "!");

            Numerator = BitConverter.ToUInt16(bytes, 0);
            Denominator = BitConverter.ToUInt16(bytes, 2);
        }


        public static bool operator ==(TgaFraction item1, TgaFraction item2) => EqualityHelper.Operator(item1, item2);
        public static bool operator !=(TgaFraction item1, TgaFraction item2) => !(item1 == item2);

        /// <summary>
        /// Get aspect ratio = <see cref="Numerator"/> / <see cref="Denominator"/>.
        /// </summary>
        public float AspectRatio => Numerator == Denominator ? 1f : Numerator / (float)Denominator;
        /// <summary>
        /// Gets or sets denominator value.
        /// </summary>
        public ushort Denominator { get; set; } 
        /// <summary>
        /// Gets or sets numerator value.
        /// </summary>
        public ushort Numerator { get; set; }


        /// <summary>
        /// Gets Empty <see cref="TgaFraction"/>, all values are 0.
        /// </summary>
        public static readonly TgaFraction Empty = new TgaFraction();

        /// <summary>
        /// Gets One <see cref="TgaFraction"/>, all values are 1 (ones, 1 / 1 = 1).
        /// </summary>
        public static readonly TgaFraction One = new TgaFraction(1, 1);

        /// <summary>
        /// Make full independent copy of <see cref="TgaFraction"/>.
        /// </summary>
        /// <returns>Copy of <see cref="TgaFraction"/></returns>
        public TgaFraction Clone() => new TgaFraction(Numerator, Denominator);
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is TgaFraction f ? Equals(f) : false;
        public bool Equals(TgaFraction? item) => item is not null && Numerator == item.Numerator && Denominator == item.Denominator;

        public override int GetHashCode() => (Numerator << 16 | Denominator).GetHashCode();

        /// <summary>
        /// Convert <see cref="TgaFraction"/> to byte array.
        /// </summary>
        /// <returns>Byte array with length = 4.</returns>
        public byte[]? ToBytes() => ByteHelper.ToBytes(Numerator, Denominator);

        /// <summary>
        /// Gets <see cref="TgaFraction"/> like string.
        /// </summary>
        /// <returns>String in "Numerator=1, Denominator=2" format.</returns>
        public override string ToString() => string.Format("{0}={1}, {2}={3}", nameof(Numerator), Numerator, nameof(Denominator), Denominator);
    }
}
