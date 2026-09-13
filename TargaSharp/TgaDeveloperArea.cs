namespace TargaSharp
{
    /// <summary>
    /// Developer Area
    /// </summary> 
    public sealed record TgaDeveloperArea : ICloneable
    {
        /// <summary>
        /// Create a new instnace of the <see cref="TgaDeveloperArea"/> class.
        /// </summary>
        public TgaDeveloperArea() { }

        /// <summary>
        /// Create a new instance of the <see cref="TgaDeveloperArea"/> class.
        /// </summary>
        /// <param name="entries"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public TgaDeveloperArea(List<TgaDeveloperEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries) + " = null!");
            Entries = entries;
        }


        public TgaDeveloperEntry this[int index]
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
        public List<TgaDeveloperEntry> Entries { get; set; } = new List<TgaDeveloperEntry>();




        /// <summary>
        /// Make full copy of <see cref="TgaDeveloperArea"/>. Named <c>Copy</c> rather than <c>Clone</c>
        /// because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns>Full independent copy of <see cref="TgaDeveloperArea"/>.</returns>
        public TgaDeveloperArea Copy() => this with { Entries = new List<TgaDeveloperEntry>(Entries.Select(x => x.Copy())) };

        /// <inheritdoc />
        object ICloneable.Clone() => Copy();

        /// <inheritdoc />
        public bool Equals(TgaDeveloperArea? other)
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
        /// Convert <see cref="TgaDeveloperArea"/> (without Fields Data, only Directory!) to byte array.
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
            var devDir = new TgaByteBuilder(sizeof(ushort) + Entries.Count * TgaDeveloperEntry.Size).Add(numEntries);
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