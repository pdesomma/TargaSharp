namespace TargaSharp
{
    public class TgaDateTime : ICloneable
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
        /// <param name="DateAndTime">Some <see cref="DateTime"/> variable.</param>
        public TgaDateTime(DateTime DateAndTime)
        {
            Month = (ushort)DateAndTime.Month;
            Day = (ushort)DateAndTime.Day;
            Year = (ushort)DateAndTime.Year;
            Hour = (ushort)DateAndTime.Hour;
            Minute = (ushort)DateAndTime.Minute;
            Second = (ushort)DateAndTime.Second;
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



        public static bool operator ==(TgaDateTime item1, TgaDateTime item2)
        {
            if (item1 is null) return item2 is null;
            if (item2 is null) return item1 is null;
            return item1.Equals(item2);
        }
        public static bool operator !=(TgaDateTime item1, TgaDateTime item2) => !(item1 == item2);
        

        /// <summary>
        /// Make <see cref="TgaDateTime"/> from bytes.
        /// </summary>
        /// <param name="bytes">Array of bytes(byte[12]).</param>
        public TgaDateTime(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            if (bytes.Length != Size)
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length, $"Length must be {Size}.");
            Month = BitConverter.ToUInt16(bytes, 0);
            Day = BitConverter.ToUInt16(bytes, 2);
            Year = BitConverter.ToUInt16(bytes, 4);
            Hour = BitConverter.ToUInt16(bytes, 6);
            Minute = BitConverter.ToUInt16(bytes, 8);
            Second = BitConverter.ToUInt16(bytes, 10);
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
        /// Make full independed copy of <see cref="TgaDateTime"/>.
        /// </summary>
        /// <returns>Copy of <see cref="TgaDateTime"/></returns>
        public TgaDateTime Clone() => new TgaDateTime(Month, Day, Year, Hour, Minute, Second);
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is TgaDateTime ? Equals((TgaDateTime)obj) : false;
        public bool Equals(TgaDateTime item) =>
                Month == item.Month &&
                Day == item.Day &&
                Year == item.Year &&
                Hour == item.Hour &&
                Minute == item.Minute &&
                Second == item.Second;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (Month << 16 | Hour).GetHashCode();
                hash = hash * 23 + (Day << 16 | Minute).GetHashCode();
                hash = hash * 23 + (Year << 16 | Second).GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Convert <see cref="TgaDateTime"/> to byte array.
        /// </summary>
        /// <returns>Byte array with length = 12.</returns>
        public byte[] ToBytes() => BitConverterHelper.ToBytes(Month, Day, Year, Hour, Minute, Second)!;

        /// <summary>
        /// Gets <see cref="TgaDateTime"/> like <see cref="DateTime"/>.
        /// </summary>
        /// <returns><see cref="DateTime"/> value of <see cref="TgaDateTime"/>.</returns>
        public DateTime ToDateTime() => new DateTime(Year, Month, Day, Hour, Minute, Second);

        /// <summary>
        /// Gets <see cref="TgaDateTime"/> like string.
        /// </summary>
        /// <returns>String in "1990.01.23 1:02:03" format.</returns>
        public override string ToString() => string.Format("{0:D4}.{1:D2}.{2:D2} {3}:{4:D2}:{5:D2}", Year, Month, Day, Hour, Minute, Second);
    }
}
