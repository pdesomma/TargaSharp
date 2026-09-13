namespace TargaSharp
{
    /// <summary>
    /// Developer Area
    /// </summary> 
    public sealed record TgaDevArea : ICloneable
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
        /// Make full copy of <see cref="TgaDevArea"/>. Named <c>Copy</c> rather than <c>Clone</c>
        /// because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns>Full independent copy of <see cref="TgaDevArea"/>.</returns>
        public TgaDevArea Copy() => this with { Entries = new List<TgaDevEntry>(Entries.Select(x => x.Copy())) };

        /// <inheritdoc />
        object ICloneable.Clone() => Copy();

        /// <inheritdoc />
        public bool Equals(TgaDevArea? other)
        {
            if (other is null) return false;
            return ReferenceEquals(Entries, other.Entries) || (Entries is not null && other.Entries is not null && Entries.SequenceEqual(other.Entries));
        }

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
        /// <exception cref="InvalidOperationException"><see cref="Entries"/> has more than <see cref="ushort.MaxValue"/>
        /// items, which is more than the TGA spec's Number of Tags field (a USHORT) can represent.</exception>
        public byte[] ToBytes()
        {
            if (Entries == null) throw new Exception(nameof(Entries) + " = null!");
            if (Entries.Count > ushort.MaxValue)
                throw new InvalidOperationException($"{nameof(Entries)}.Count ({Entries.Count}) exceeds the TGA spec limit of {ushort.MaxValue} tags in the Developer Directory.");

            ushort numEntries = (ushort)Entries.Count;
            var devDir = new TgaByteBuilder(sizeof(ushort) + Entries.Count * TgaDevEntry.Size).Add(numEntries);
            for (int i = 0; i < Entries.Count; i++)
            {
                devDir.Add(Entries[i].Tag);
                devDir.Add(Entries[i].Offset);
                devDir.Add(unchecked((uint)Entries[i].FieldSize));
            }
            return devDir.ToArray();
        }
    }
}