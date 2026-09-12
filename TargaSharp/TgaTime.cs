
namespace TargaSharp
{
    public sealed record TgaTime : ICloneable
    {
        /// <summary>
        /// Gets TGA Field size in bytes.
        /// </summary>
        public const int Size = 6;

        /// <summary>
        /// Make empty <see cref="TgaTime"/>.
        /// </summary>
        public TgaTime() { }
        /// <summary>
        /// Make <see cref="TgaTime"/> from <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="time">Some <see cref="TimeSpan"/> variable.</param>
        public TgaTime(TimeSpan time) : this((ushort)time.TotalHours, (ushort)time.Minutes, (ushort)time.Seconds) { }
        /// <summary>
        /// Make <see cref="TgaTime"/> from <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="time">Some <see cref="TimeSpan"/> variable.</param>
        public TgaTime(int hours, int minutes, int seconds) : this((ushort)hours, (ushort)minutes, (ushort)seconds) { }
        /// <summary>
        /// Make <see cref="TgaTime"/> from ushort values.
        /// </summary>
        /// <param name="Hours">Hour (0 - 65535).</param>
        /// <param name="Minutes">Minute (0 - 59).</param>
        /// <param name="Seconds">Second (0 - 59).</param>
        public TgaTime(ushort hours, ushort minutes, ushort seconds)
        {
            Hours = hours;
            Minutes = minutes;
            Seconds = seconds;
        }
        /// <summary>
        /// Make <see cref="TgaTime"/> from bytes.
        /// </summary>
        /// <param name="bytes">Array of bytes(byte[6]).</param>
        public TgaTime(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            if (bytes.Length != Size) throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length, $"Length must be {Size}.");

            Hours = BitConverter.ToUInt16(bytes, 0);
            Minutes = BitConverter.ToUInt16(bytes, 2);
            Seconds = BitConverter.ToUInt16(bytes, 4);
        }


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
        /// Make full independed copy of <see cref="TgaTime"/>. Named <c>Copy</c> rather than
        /// <c>Clone</c> because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns>Copy of <see cref="TgaTime"/></returns>
        public TgaTime Copy() => this with { };

        /// <summary>
        /// Convert <see cref="TgaTime"/> to byte array.
        /// </summary>
        /// <returns>Byte array with length = 6.</returns>
        public byte[] ToBytes() => BitConverterHelper.ToBytes(Hours, Minutes, Seconds);
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

        /// <inheritdoc />
        object ICloneable.Clone() => Copy();
    }
}
