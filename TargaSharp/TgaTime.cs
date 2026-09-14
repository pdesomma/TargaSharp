
namespace TargaSharp
{
    /// <summary>
    /// An elapsed job time stored in the TGA extension area, expressed as separate hours,
    /// minutes and seconds fields.
    /// </summary>
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
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="time"/> is negative or exceeds 65535 hours.</exception>
        public TgaTime(TimeSpan time) : this(CheckedHours((long)CheckedNonNegative(time).TotalHours, nameof(time)), (ushort)time.Minutes, (ushort)time.Seconds) { }
        /// <summary>
        /// Make <see cref="TgaTime"/> from int values.
        /// </summary>
        /// <param name="hours">Hour (0 - 65535).</param>
        /// <param name="minutes">Minute (0 - 59).</param>
        /// <param name="seconds">Second (0 - 59).</param>
        /// <exception cref="ArgumentOutOfRangeException">A value is outside its documented range.</exception>
        public TgaTime(int hours, int minutes, int seconds) : this(CheckedHours(hours, nameof(hours)), CheckedSexagesimal(minutes, nameof(minutes)), CheckedSexagesimal(seconds, nameof(seconds))) { }
        /// <summary>
        /// Make <see cref="TgaTime"/> from ushort values.
        /// </summary>
        /// <param name="hours">Hour (0 - 65535).</param>
        /// <param name="minutes">Minute (0 - 59).</param>
        /// <param name="seconds">Second (0 - 59).</param>
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

            Hours = TgaBinary.ReadUInt16(bytes, 0);
            Minutes = TgaBinary.ReadUInt16(bytes, 2);
            Seconds = TgaBinary.ReadUInt16(bytes, 4);
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
        /// Make full independent copy of <see cref="TgaTime"/>. Named <c>Copy</c> rather than
        /// <c>Clone</c> because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns>Copy of <see cref="TgaTime"/></returns>
        public TgaTime Copy() => this with { };

        /// <summary>
        /// Convert <see cref="TgaTime"/> to byte array.
        /// </summary>
        /// <returns>Byte array with length = 6.</returns>
        public byte[] ToBytes() => new TgaByteBuilder(Size).Add(Hours).Add(Minutes).Add(Seconds).ToArray();
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

        /// <summary>
        /// Rejects a negative span; a sub-hour one would otherwise truncate to 0 hours with wrapped minutes/seconds.
        /// </summary>
        /// <param name="time">Span to check.</param>
        /// <returns><paramref name="time"/> unchanged.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="time"/> is negative.</exception>
        private static TimeSpan CheckedNonNegative(TimeSpan time)
        {
            if (time < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(time), time, "Must not be negative.");
            return time;
        }

        /// <summary>
        /// Narrows an hour count to <see cref="ushort"/>, rejecting values outside 0-65535.
        /// </summary>
        /// <param name="hours">Hour count.</param>
        /// <param name="paramName">Caller's parameter name for the exception.</param>
        /// <returns><paramref name="hours"/> as <see cref="ushort"/>.</returns>
        private static ushort CheckedHours(long hours, string paramName)
        {
            if (hours is < 0 or > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(paramName, hours, $"Hours must be 0-{ushort.MaxValue}.");
            return (ushort)hours;
        }

        /// <summary>
        /// Narrows a minute or second count to <see cref="ushort"/>, rejecting values outside 0-59.
        /// </summary>
        /// <param name="value">Minute or second count.</param>
        /// <param name="paramName">Caller's parameter name for the exception.</param>
        /// <returns><paramref name="value"/> as <see cref="ushort"/>.</returns>
        private static ushort CheckedSexagesimal(int value, string paramName)
        {
            if (value is < 0 or > 59)
                throw new ArgumentOutOfRangeException(paramName, value, "Must be 0-59.");
            return (ushort)value;
        }
    }
}
