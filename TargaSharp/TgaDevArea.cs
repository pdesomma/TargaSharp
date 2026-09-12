namespace TargaSharp
{
    /// <summary>
    /// Developer Area
    /// </summary> 
    public class TgaDevArea : ICloneable
    {
        /// <summary>
        /// Create a new instnace of the <see cref="TgaDevArea"/> class.
        /// </summary>
        public TgaDevArea() { }

        /// <summary>
        /// Create a new instance of the <see cref="TgaDevArea"/> class.
        /// </summary>
        /// <param name="entries"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public TgaDevArea(List<TgaDevEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries) + " = null!");
            Entries = entries;
        }


        public static bool operator ==(TgaDevArea item1, TgaDevArea item2)
        {
            if (item1 is null) return item2 is null;
            if (item2 is null) return item1 is null;
            return item1.Equals(item2);
        }
        public static bool operator !=(TgaDevArea item1, TgaDevArea item2) => !(item1 == item2);

        public TgaDevEntry this[int index]
        {
            get { return Entries[index]; }
            set { Entries[index] = value; }
        }


        /// <summary>
        /// Count of entries.
        /// </summary>
        public int Count => Entries.Count;

        /// <summary>
        /// Developer Data - Field 9 (variable):
        /// </summary>
        public List<TgaDevEntry> Entries { get; set; } = new List<TgaDevEntry>();




        /// <summary>
        /// Make full copy of <see cref="TgaDevArea"/>.
        /// </summary>
        /// <returns>Full independent copy of <see cref="TgaDevArea"/>.</returns>
        public TgaDevArea Clone() => new TgaDevArea(new List<TgaDevEntry>(Entries.Select(x => x.Clone())));
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is TgaDevArea ? Equals((TgaDevArea)obj) : false;
        public bool Equals(TgaDevArea item) => BitConverterHelper.IsListsEqual(Entries, item.Entries);
       
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 27;
                if (Entries != null)
                    for (int i = 0; i < Entries.Count; i++)
                        hash = (13 * hash) + Entries[i].GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Convert <see cref="TgaDevArea"/> (without Fields Data, only Directory!) to byte array.
        /// </summary>
        /// <returns>Byte array, Len = (NUMBER_OF_TAGS_IN_THE_DIRECTORY * 10) + 2 bytes in size.
        /// The "+ 2" includes the 2 bytes for the number of tags in the directory.</returns>
        public byte[] ToBytes()
        {
            if (Entries == null) throw new Exception(nameof(Entries) + " = null!");

            ushort numEntries = (ushort)Math.Min(ushort.MaxValue, Entries.Count);
            var devDir = new List<byte>(BitConverter.GetBytes(numEntries));
            for (int i = 0; i < Entries.Count; i++)
            {
                devDir.AddRange(BitConverter.GetBytes(Entries[i].Tag));
                devDir.AddRange(BitConverter.GetBytes(Entries[i].Offset));
                devDir.AddRange(BitConverter.GetBytes(Entries[i].FieldSize));
            }
            return devDir.ToArray();
        }
    }
}