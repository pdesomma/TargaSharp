namespace TargaSharp
{
    /// <summary>
    /// A date/time stamp stored in the TGA extension area, expressed as separate month, day,
    /// year, hour, minute and second fields.
    /// </summary>
    public sealed record TgaDateTime : ICloneable
    {
        /// <summary>
        /// Gets TGA Field size in bytes.
        /// </summary>
        public const int Size = 12;



        /// <summary>
        /// Make empty <see cref="TgaDateTime"/>.
        /// </summary>
        public TgaDateTime() { }

        /// <summary>
        /// Make <see cref="TgaDateTime"/> from <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dateTime">Some <see cref="DateTime"/> variable.</param>
        public TgaDateTime(DateTime dateTime)
        {
            Month = (ushort)dateTime.Month;
            Day = (ushort)dateTime.Day;
            Year = (ushort)dateTime.Year;
            Hour = (ushort)dateTime.Hour;
            Minute = (ushort)dateTime.Minute;
            Second = (ushort)dateTime.Second;
        }

        /// <summary>
        /// Make <see cref="TgaDateTime"/> from ushort values.
        /// </summary>
        /// <param name="month">Month (1 - 12).</param>
        /// <param name="day">Day (1 - 31).</param>
        /// <param name="year">Year (4 digit, ie. 1989).</param>
        /// <param name="hour">Hour (0 - 23).</param>
        /// <param name="minute">Minute (0 - 59).</param>
        /// <param name="second">Second (0 - 59).</param>
        public TgaDateTime(ushort month, ushort day, ushort year, ushort hour, ushort minute, ushort second)
        {
            Month = month;
            Day = day;
            Year = year;
            Hour = hour;
            Minute = minute;
            Second = second;
        }

        /// <summary>
        /// Make <see cref="TgaDateTime"/> from bytes.
        /// </summary>
        /// <param name="bytes">Array of bytes(byte[12]).</param>
        public TgaDateTime(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            if (bytes.Length != Size)
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length, $"Length must be {Size}.");
            Month = TgaBinary.ReadUInt16(bytes, 0);
            Day = TgaBinary.ReadUInt16(bytes, 2);
            Year = TgaBinary.ReadUInt16(bytes, 4);
            Hour = TgaBinary.ReadUInt16(bytes, 6);
            Minute = TgaBinary.ReadUInt16(bytes, 8);
            Second = TgaBinary.ReadUInt16(bytes, 10);
        }

        /// <summary>
        /// Gets or Sets month (1 - 12).
        /// </summary>
        public ushort Month { get; set; }

        /// <summary>
        /// Gets or Sets day (1 - 31).
        /// </summary>
        public ushort Day { get; set; }

        /// <summary>
        /// Gets or Sets year (4 digit, ie. 1989).
        /// </summary>
        public ushort Year { get; set; }

        /// <summary>
        /// Gets or Sets hour (0 - 23).
        /// </summary>
        public ushort Hour { get; set; }

        /// <summary>
        /// Gets or Sets minute (0 - 59).
        /// </summary>
        public ushort Minute { get; set; }

        /// <summary>
        /// Gets or Sets second (0 - 59).
        /// </summary>
        public ushort Second { get; set; }


        /// <summary>
        /// Make full independent copy of <see cref="TgaDateTime"/>. Named <c>Copy</c> rather than
        /// <c>Clone</c> because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns>Copy of <see cref="TgaDateTime"/></returns>
        public TgaDateTime Copy() => this with { };

        /// <summary>
        /// Gets whether every field is zero, which the spec defines as "date/time not set".
        /// </summary>
        public bool IsUnset => Month == 0 && Day == 0 && Year == 0 && Hour == 0 && Minute == 0 && Second == 0;

        /// <summary>
        /// Convert <see cref="TgaDateTime"/> to byte array.
        /// </summary>
        /// <returns>Byte array with length = 12.</returns>
        public byte[] ToBytes() => new TgaByteBuilder(Size).Add(Month).Add(Day).Add(Year).Add(Hour).Add(Minute).Add(Second).ToArray();

        /// <summary>
        /// Gets <see cref="TgaDateTime"/> as a <see cref="DateTime"/>, or <see langword="null"/> when
        /// <see cref="IsUnset"/> (all fields zero, the spec's "not set" value - which has no
        /// <see cref="DateTime"/> equivalent, since year, month and day 0 are not representable).
        /// </summary>
        /// <returns><see cref="DateTime"/> value of <see cref="TgaDateTime"/>, or <see langword="null"/> when unset.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The fields are set but out of range for a
        /// <see cref="DateTime"/> (e.g. month 13); see <see cref="Validation.TgaValidator"/>.</exception>
        public DateTime? ToDateTime() => IsUnset ? null : new DateTime(Year, Month, Day, Hour, Minute, Second);

        /// <summary>
        /// Gets <see cref="TgaDateTime"/> like string.
        /// </summary>
        /// <returns>String in "1990.01.23 1:02:03" format.</returns>
        public override string ToString() => string.Format("{0:D4}.{1:D2}.{2:D2} {3}:{4:D2}:{5:D2}", Year, Month, Day, Hour, Minute, Second);

        /// <inheritdoc />
        object ICloneable.Clone() => Copy();
    }
}
