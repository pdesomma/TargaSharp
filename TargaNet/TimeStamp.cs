using TargaNet.Helpers;

namespace TargaNet
{
    /// <summary>
    /// Wrapper for working with DateTimes as time stamps for TGA files.
    /// </summary>
    public class TimeStamp : FileField, ICloneable, IEquatable<TimeStamp>
    {
        public const int Length = 12;

        /// <summary>
        /// Make empty <see cref="TimeStamp"/>.
        /// </summary>
        public TimeStamp() : base(Length, 367 ,13) { }

        /// <summary>
        /// Make <see cref="TimeStamp"/> from <see cref="DateTime"/>.
        /// </summary>
        /// <param name="DateAndTime">Some <see cref="DateTime"/> variable.</param>
        public TimeStamp(DateTime DateAndTime) : this()
        {
            Month = (ushort)DateAndTime.Month;
            Day = (ushort)DateAndTime.Day;
            Year = (ushort)DateAndTime.Year;
            Hour = (ushort)DateAndTime.Hour;
            Minute = (ushort)DateAndTime.Minute;
            Second = (ushort)DateAndTime.Second;
        }

        /// <summary>
        /// Make <see cref="TimeStamp"/> from ushort values.
        /// </summary>
        /// <param name="month">Month (1 - 12).</param>
        /// <param name="day">Day (1 - 31).</param>
        /// <param name="year">Year (4 digit, ie. 1989).</param>
        /// <param name="hour">Hour (0 - 23).</param>
        /// <param name="minute">Minute (0 - 59).</param>
        /// <param name="second">Second (0 - 59).</param>
        public TimeStamp(ushort month, ushort day, ushort year, ushort hour, ushort minute, ushort second)
            : this()
        {
            Month = month;
            Day = day;
            Year = year;
            Hour = hour;
            Minute = minute;
            Second = second;
        }

        /// <summary>
        /// Make <see cref="TimeStamp"/> from bytes.
        /// </summary>
        /// <param name="bytes">Array of bytes(byte[12]).</param>
        public TimeStamp(byte[] bytes) : base(Length, 367, 13, bytes) 
        {
            Month = BitConverter.ToUInt16(bytes, 0);
            Day = BitConverter.ToUInt16(bytes, 2);
            Year = BitConverter.ToUInt16(bytes, 4);
            Hour = BitConverter.ToUInt16(bytes, 6);
            Minute = BitConverter.ToUInt16(bytes, 8);
            Second = BitConverter.ToUInt16(bytes, 10);
        }


        public static bool operator ==(TimeStamp item1, TimeStamp item2) => EqualityHelper.Operator(item1, item2);
        public static bool operator !=(TimeStamp item1, TimeStamp item2) => !(item1 == item2);

        /// <summary>
        /// Gets or Sets month (1 - 12).
        /// </summary>
        public ushort Month { get; private set; }

        /// <summary>
        /// Gets or Sets day (1 - 31).
        /// </summary>
        public ushort Day { get; private set; }

        /// <summary>
        /// Gets or Sets year (4 digit, ie. 1989).
        /// </summary>
        public ushort Year { get; private set; }

        /// <summary>
        /// Gets or Sets hour (0 - 23).
        /// </summary>
        public ushort Hour { get; private set; }

        /// <summary>
        /// Gets or Sets minute (0 - 59).
        /// </summary>
        public ushort Minute { get; private set; }

        /// <summary>
        /// Gets or Sets second (0 - 59).
        /// </summary>
        public ushort Second { get; private set; }

        /// <summary>
        /// Make full independent copy of <see cref="TimeStamp"/>.
        /// </summary>
        /// <returns>Copy of <see cref="TimeStamp"/></returns>
        public TimeStamp Clone() => new(Month, Day, Year, Hour, Minute, Second);
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is TimeStamp d && Equals(d);
        public bool Equals(TimeStamp? item) =>
                item is not null &&
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
        /// Convert <see cref="TimeStamp"/> to byte array.
        /// </summary>
        /// <returns>Byte array with length = 12.</returns>
        public override byte[]? ToBytes() => ByteHelper.ToBytes(Month, Day, Year, Hour, Minute, Second);

        /// <summary>
        /// Gets <see cref="TimeStamp"/> like <see cref="DateTime"/>.
        /// </summary>
        /// <returns><see cref="DateTime"/> value of <see cref="TimeStamp"/>.</returns>
        public DateTime ToDateTime() => new(Year, Month, Day, Hour, Minute, Second);

        /// <summary>
        /// Gets <see cref="TimeStamp"/> like string.
        /// </summary>
        /// <returns>String in "1990.01.23 1:02:03" format.</returns>
        public override string ToString() => string.Format("{0:D4}.{1:D2}.{2:D2} {3}:{4:D2}:{5:D2}", Year, Month, Day, Hour, Minute, Second);


        /// <summary>
        /// How to create this object from a byte array that's been validated.
        /// </summary>
        /// <param name="bytes"></param>
        protected override void FromBytes(byte[] bytes)
        {
            Month = BitConverter.ToUInt16(bytes, 0);
            Day = BitConverter.ToUInt16(bytes, 2);
            Year = BitConverter.ToUInt16(bytes, 4);
            Hour = BitConverter.ToUInt16(bytes, 6);
            Minute = BitConverter.ToUInt16(bytes, 8);
            Second = BitConverter.ToUInt16(bytes, 10);
        }
    }
}
