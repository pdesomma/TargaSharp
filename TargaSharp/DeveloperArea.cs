using System;
using System.Collections.Generic;
using System.Linq;
using TargaSharp.Helpers;

namespace TargaSharp
{
    /// <summary>
    /// Developer Area
    /// </summary>
    public class DeveloperArea : ICloneable, IByteConvert, IEquatable<DeveloperArea>
    {
        /// <summary>
        /// Create a new instance of the <see cref="DeveloperArea"/>.
        /// </summary>
        public DeveloperArea() { }

        /// <summary>
        /// Create a new instance of the <see cref="DeveloperArea"/>.
        /// </summary>
        public DeveloperArea(IEnumerable<DeveloperTag> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(Entries) + " = null!");
            Entries = new(entries);
        }


        public static bool operator ==(DeveloperArea item1, DeveloperArea item2) => EqualityHelper.Operator(item1, item2);
        public static bool operator !=(DeveloperArea item1, DeveloperArea item2) => !(item1 == item2);


        public DeveloperTag this[int index] { get => Entries[index]; set => Entries[index] = value; }

        /// <summary>
        /// Count of entries
        /// </summary>
        public int Count => Entries.Count;

        /// <summary>
        /// Developer Data - Field 9 (variable):
        /// </summary>
        public List<DeveloperTag> Entries { get; set; } =

#if NET8_0_OR_GREATER
                [];
#else
                new();
#endif


        /// <summary>
        /// Make full copy of <see cref="DeveloperArea"/>.
        /// </summary>
        /// <returns>Full independent copy of <see cref="DeveloperArea"/>.</returns>
        public DeveloperArea Clone() => new DeveloperArea(Entries.Select(x => x.Clone()));
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is DeveloperArea dev && Equals(dev);
        public bool Equals(DeveloperArea? item) => item is not null && EnumerableHelper.AreEnumerablesEqual(Entries, item.Entries);

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
        /// Convert <see cref="DeveloperArea"/> (without Fields Data, only Directory!) to byte array.
        /// </summary>
        /// <returns>Byte array, Len = (NUMBER_OF_TAGS_IN_THE_DIRECTORY * 10) + 2 bytes in size.
        /// The "+ 2" includes the 2 bytes for the number of tags in the directory.</returns>
        public byte[]? ToBytes()
        {
            if (Entries == null) throw new Exception(nameof(Entries) + " = null!");

            ushort numberOfEntries = (ushort)Math.Min(ushort.MaxValue, Entries.Count);
            var devDir = BitConverter.GetBytes(numberOfEntries).ToList();

            for (int i = 0; i < Entries.Count; i++)
            {
                devDir.AddRange(BitConverter.GetBytes(Entries[i].Tag));
                devDir.AddRange(BitConverter.GetBytes(Entries[i].Offset));
                devDir.AddRange(BitConverter.GetBytes(Entries[i].FieldSize));
            }
            return
#if NET8_0_OR_GREATER
                [.. devDir];
#else
                devDir.ToArray();
#endif
        }
    } 
}