using System;
using TargaSharp.Helpers;

namespace TargaSharp
{
    public class TgaTime : ByteSized, IByteConvert, ICloneable, IEquatable<TgaTime>
    {
        public const int Length = 6;

        /// <summary>
        /// Make empty <see cref="TgaTime"/>.
        /// </summary>
        public TgaTime() : base(Length) { }

        /// <summary>
        /// Make <see cref="TgaTime"/> from <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="Time">Some <see cref="TimeSpan"/> variable.</param>
        public TgaTime(TimeSpan Time) : this() 
        {
            Hours = (ushort)Time.TotalHours;
            Minutes = (ushort)Time.Minutes;
            Seconds = (ushort)Time.Seconds;
        }

        /// <summary>
        /// Make <see cref="TgaTime"/> from ushort values.
        /// </summary>
        /// <param name="Hours">Hour (0 - 65535).</param>
        /// <param name="Minutes">Minute (0 - 59).</param>
        /// <param name="Seconds">Second (0 - 59).</param>
        public TgaTime(ushort hours, ushort minutes, ushort seconds) : this()
        {
            Hours = hours;
            Minutes = minutes;
            Seconds = seconds;
        }

        /// <summary>
        /// Make <see cref="TgaTime"/> from bytes.
        /// </summary>
        /// <param name="bytes">Array of bytes(byte[6]).</param>
        public TgaTime(byte[] bytes) : this()
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(bytes);
#else 
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));
#endif
            if (bytes.Length != ByteSize) throw new ArgumentOutOfRangeException(nameof(bytes) + " must be equal " + ByteSize + "!");

            Hours = BitConverter.ToUInt16(bytes, 0);
            Minutes = BitConverter.ToUInt16(bytes, 2);
            Seconds = BitConverter.ToUInt16(bytes, 4);
        }

        public static bool operator ==(TgaTime item1, TgaTime item2) => EqualityHelper.Operator(item1, item2);
        public static bool operator !=(TgaTime item1, TgaTime item2) => !(item1 == item2);


        /// <summary>
        /// Gets or Sets hour (0 - 65535).
        /// </summary>
        public ushort Hours { get; set; }

        /// <summary>
        /// Gets or Sets minute (0 - 59).
        /// </summary>
        public ushort Minutes { get; set; }

        /// <summary>
        /// Gets or Sets second (0 - 59).
        /// </summary>
        public ushort Seconds { get; set; }


        /// <summary>
        /// Make full independent copy of <see cref="TgaTime"/>.
        /// </summary>
        /// <returns>Copy of <see cref="TgaTime"/></returns>
        public TgaTime Clone() => new TgaTime(Hours, Minutes, Seconds);
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is TgaTime time && Equals(time);
        public bool Equals(TgaTime? item) => item is not null && Hours == item.Hours && Minutes == item.Minutes && Seconds == item.Seconds;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Hours.GetHashCode();
                hash = hash * 23 + (Minutes << 16 | Seconds).GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Convert <see cref="TgaTime"/> to byte array.
        /// </summary>
        /// <returns>Byte array with length = 6.</returns>
        public byte[]? ToBytes() => ByteHelper.ToBytes(Hours, Minutes, Seconds);

        /// <summary>
        /// Gets <see cref="TgaTime"/> like string.
        /// </summary>
        /// <returns>String in "H:M:S" format.</returns>
        public override string ToString() => string.Format("{0}:{1}:{2}", Hours, Minutes, Seconds);

        /// <summary>
        /// Gets <see cref="TgaTime"/> like <see cref="TimeSpan"/>.
        /// </summary>
        /// <returns><see cref="TimeSpan"/> value of <see cref="TgaTime"/>.</returns>
        public TimeSpan ToTimeSpan() => new TimeSpan(Hours, Minutes, Seconds);
    }
}
