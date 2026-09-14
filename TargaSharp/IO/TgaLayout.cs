using TargaSharp.Validation;

namespace TargaSharp.IO
{
    /// <summary>
    /// One contiguous section of a TGA file on disk: its name, absolute byte offset and the bytes to write.
    /// </summary>
    /// <param name="Name">Human-readable section name, e.g. "Header" or "ExtensionArea.ScanLineTable".</param>
    /// <param name="Offset">Absolute offset of the first byte of this section from the start of the file.</param>
    /// <param name="Bytes">The bytes that make up this section.</param>
    internal sealed record TgaSection(string Name, uint Offset, byte[] Bytes);

    /// <summary>
    /// The complete on-disk layout of a <see cref="TgaFile"/>: every section in file order with its offset.
    /// Produced by <see cref="TgaLayoutPlanner"/>; consumed by <see cref="TgaWriter"/>, which writes sections in order.
    /// </summary>
    internal sealed class TgaLayout
    {
        /// <summary>
        /// Make a <see cref="TgaLayout"/> from sections already in file order.
        /// </summary>
        /// <param name="sections">Sections in file order, offsets contiguous from 0.</param>
        internal TgaLayout(IReadOnlyList<TgaSection> sections)
        {
            Sections = sections;
        }

        /// <summary>
        /// Gets the sections in the order they appear in the file.
        /// </summary>
        internal IReadOnlyList<TgaSection> Sections { get; }

        /// <summary>
        /// Gets the total file size in bytes.
        /// </summary>
        internal uint TotalSize => Sections.Count == 0 ? 0 : Sections[^1].Offset + (uint)Sections[^1].Bytes.Length;
    }

    /// <summary>
    /// Computes a <see cref="TgaFile"/>'s on-disk layout for <see cref="TgaWriter"/>.
    /// </summary>
    internal interface ITgaLayoutPlanner
    {
        /// <summary>
        /// Plans <paramref name="file"/>'s layout, assigning its writer-owned derived fields (ID length,
        /// extension size, offsets) in the process.
        /// </summary>
        /// <param name="file">The file to lay out. Expected to have passed <see cref="ITgaValidator"/> validation.</param>
        /// <returns>The layout, ready to be written section by section.</returns>
        /// <exception cref="TgaValidationException">A structural inconsistency was found that validation did not model.</exception>
        TgaLayout Plan(TgaFile file);
    }

    /// <summary>
    /// Computes a <see cref="TgaFile"/>'s on-disk layout. This is the single source of truth for section order
    /// (spec: header, image ID, color map, image data, developer fields, developer directory, extension area,
    /// scan line table, postage stamp, color correction table, footer). As a side effect it assigns every
    /// writer-owned derived field on the file (ID length, extension size, all offsets) so that the bytes it
    /// returns and the fields the file now carries always agree.
    /// </summary>
    internal sealed class TgaLayoutPlanner : ITgaLayoutPlanner
    {
        /// <summary>
        /// A section whose offset is known but whose bytes are produced later, once every offset has been assigned.
        /// </summary>
        /// <param name="Name">Section name.</param>
        /// <param name="Offset">Absolute offset of the section.</param>
        /// <param name="Size">Size in bytes the section will occupy.</param>
        /// <param name="Materialize">Produces the section's bytes; called after all offsets are assigned.</param>
        private sealed record PendingSection(string Name, uint Offset, uint Size, Func<byte[]> Materialize);

        /// <inheritdoc />
        public TgaLayout Plan(TgaFile file)
        {
            ArgumentNullException.ThrowIfNull(file);

            var pending = new List<PendingSection>();
            uint offset = 0;

            // Adds a fixed-size section at the current offset, advances the cursor and returns where it landed.
            uint Add(string name, uint size, Func<byte[]> materialize)
            {
                uint at = offset;
                pending.Add(new PendingSection(name, at, size, materialize));
                offset += size;
                return at;
            }

            // Header is written last-materialized because IdLength is assigned below.
            Add("Header", TgaHeader.Size, () => file.Header.ToBytes());

            PlanImageId(file, Add);
            PlanColorMap(file, Add);
            PlanImageData(file, Add);

            if (file.Footer is not null)
            {
                PlanDeveloperArea(file, Add);
                PlanExtensionArea(file, Add);
                Add("Footer", TgaFooter.Size, () => file.Footer.ToBytes());
            }

            // Every derived field is now assigned, so sections that embed offsets serialize correctly.
            var sections = new List<TgaSection>(pending.Count);
            foreach (var p in pending)
            {
                byte[] bytes = p.Materialize();
                if (bytes.Length != p.Size)
                    throw Fail(p.Name, $"planned {p.Size} bytes but serialized {bytes.Length}.");
                sections.Add(new TgaSection(p.Name, p.Offset, bytes));
            }

            return new TgaLayout(sections);
        }

        /// <summary>
        /// Plans the image ID field. IdLength is derived from the ID string's <see cref="TgaString.Length"/>
        /// (which already covers the text and optional NUL terminator), never the other way around.
        /// </summary>
        /// <param name="file">File being planned.</param>
        /// <param name="add">Section sink.</param>
        private static void PlanImageId(TgaFile file, Func<string, uint, Func<byte[]>, uint> add)
        {
            TgaString? imageId = file.ImageArea.ImageId;
            if (imageId is null)
            {
                file.Header.IdLength = 0;
                return;
            }

            // The field's own Length (text + padding + optional NUL) is honored as-is so a padded ID read
            // from disk is written back byte-for-byte; it only has to fit the 1-byte IdLength.
            if (imageId.Length > byte.MaxValue)
                throw Fail("ImageArea.ImageId", $"length {imageId.Length} exceeds the {byte.MaxValue} byte maximum.");

            file.Header.IdLength = (byte)imageId.Length;
            if (file.Header.IdLength > 0)
                add("ImageArea.ImageId", file.Header.IdLength, imageId.ToBytes);
        }

        /// <summary>
        /// Plans the color map data, present only when the header declares a color map.
        /// </summary>
        /// <param name="file">File being planned.</param>
        /// <param name="add">Section sink.</param>
        private static void PlanColorMap(TgaFile file, Func<string, uint, Func<byte[]>, uint> add)
        {
            if (file.Header.ColorMapType == TgaColorMapType.NoColorMap)
                return;

            byte[]? data = file.ImageArea.ColorMapData;
            int expected = file.Header.ColorMapDataLength;
            if (data is null || data.Length != expected)
                throw Fail("ImageArea.ColorMapData", $"expected {expected} bytes, found {data?.Length.ToString() ?? "null"}.");

            add("ImageArea.ColorMapData", (uint)data.Length, () => data);
        }

        /// <summary>
        /// Plans the image data, RLE-encoding it up front when the image type calls for it (the encoded size is only known by encoding).
        /// </summary>
        /// <param name="file">File being planned.</param>
        /// <param name="add">Section sink.</param>
        private static void PlanImageData(TgaFile file, Func<string, uint, Func<byte[]>, uint> add)
        {
            if (file.Header.ImageType == TgaImageType.NoImageData)
                return;

            byte[]? data = file.ImageArea.ImageData;
            int bytesPerPixel = file.Header.ImageSpec.PixelDepth.BytesPerPixel();
            long expected = file.Header.ImageDataLength;
            if (file.Width == 0 || file.Height == 0 || data is null || data.Length != expected)
                throw Fail("ImageArea.ImageData", $"expected {expected} bytes for {file.Width}x{file.Height}, found {data?.Length.ToString() ?? "null"}.");

            byte[] bytes = file.Header.ImageType.IsRunLengthEncoded()
                ? RleCodec.Encode(data, bytesPerPixel, file.Width, file.Height)
                : data;
            add("ImageArea.ImageData", (uint)bytes.Length, () => bytes);
        }

        /// <summary>
        /// Plans the developer fields followed by the developer directory, and records the directory offset in the footer.
        /// Entries are written in tag order, as the spec requires a tag-ordered directory - but on a private copy: the
        /// caller's <see cref="TgaDeveloperArea.Entries"/> list is never reordered by writing. A zero-length entry keeps its
        /// directory slot (the spec does not forbid one) so a file round-trips with the entry count it declared.
        /// Only each entry's derived <see cref="TgaDeveloperEntry.Offset"/> is assigned.
        /// </summary>
        /// <param name="file">File being planned.</param>
        /// <param name="add">Section sink.</param>
        private static void PlanDeveloperArea(TgaFile file, Func<string, uint, Func<byte[]>, uint> add)
        {
            List<TgaDeveloperEntry> entries = file.DeveloperArea?.Entries
                .Where(e => e is not null)
                .OrderBy(e => e.Tag)
                .ToList() ?? [];

            for (int i = 0; i < entries.Count - 1; i++)
                if (entries[i].Tag == entries[i + 1].Tag)
                    throw Fail($"DeveloperArea.Entries[{i + 1}].Tag", $"duplicate tag {entries[i].Tag}.");

            if (entries.Count == 0)
            {
                file.Footer!.DeveloperDirectoryOffset = 0;
                return;
            }

            // Field payloads precede the directory; each entry's Offset is where its payload lands.
            for (int i = 0; i < entries.Count; i++)
            {
                TgaDeveloperEntry entry = entries[i];
                entry.Offset = add($"DeveloperArea.Entries[{i}].Data", (uint)entry.FieldSize, () => entry.Data);
            }

            var directory = new TgaDeveloperArea(entries);
            uint directorySize = sizeof(ushort) + (uint)(entries.Count * TgaDeveloperEntry.Size);
            file.Footer!.DeveloperDirectoryOffset = add("DeveloperArea.Directory", directorySize, directory.ToBytes);
        }

        /// <summary>
        /// Plans the extension area and its three optional trailing tables, recording each table's offset in the
        /// extension area and the extension area's own offset in the footer.
        /// </summary>
        /// <param name="file">File being planned.</param>
        /// <param name="add">Section sink.</param>
        private static void PlanExtensionArea(TgaFile file, Func<string, uint, Func<byte[]>, uint> add)
        {
            TgaExtensionArea? ext = file.ExtensionArea;
            if (ext is null)
            {
                file.Footer!.ExtensionAreaOffset = 0;
                return;
            }

            int otherDataLength = ext.OtherDataInExtensionArea?.Length ?? 0;
            if (otherDataLength > ushort.MaxValue - TgaExtensionArea.MinSize)
                throw Fail("ExtensionArea.OtherDataInExtensionArea", $"length {otherDataLength} exceeds the {ushort.MaxValue - TgaExtensionArea.MinSize} bytes the Extension Size field can hold.");

            ext.ExtensionSize = (ushort)(TgaExtensionArea.MinSize + otherDataLength);
            // A caller-supplied timestamp is preserved; only an unset one is stamped with "now".
            if (ext.DateTimeStamp.IsUnset)
                ext.DateTimeStamp = new TgaDateTime(DateTime.UtcNow);
            file.Footer!.ExtensionAreaOffset = add("ExtensionArea", ext.ExtensionSize, ext.ToBytes);

            if (ext.ScanLineTable is null)
                ext.ScanLineOffset = 0;
            else
            {
                if (ext.ScanLineTable.Length != file.Height)
                    throw Fail("ExtensionArea.ScanLineTable", $"length {ext.ScanLineTable.Length} != image height {file.Height}.");

                uint[] table = ext.ScanLineTable;
                ext.ScanLineOffset = add("ExtensionArea.ScanLineTable", (uint)table.Length * sizeof(uint), () => ToBytes(table));
            }

            if (ext.PostageStampImage is null)
                ext.PostageStampOffset = 0;
            else
            {
                TgaPostageStampImage stamp = ext.PostageStampImage;
                int expected = stamp.Width * stamp.Height * file.Header.ImageSpec.PixelDepth.BytesPerPixel();
                if (file.Header.ImageType != TgaImageType.NoImageData && stamp.Data.Length != expected)
                    throw Fail("ExtensionArea.PostageStampImage.Data", $"expected {expected} bytes, found {stamp.Data.Length}.");

                ext.PostageStampOffset = add("ExtensionArea.PostageStampImage", TgaPostageStampImage.HeaderSize + (uint)stamp.Data.Length, stamp.ToBytes);
            }

            if (ext.ColorCorrectionTable is null)
                ext.ColorCorrectionTableOffset = 0;
            else
            {
                if (ext.ColorCorrectionTable.Length != TgaExtensionArea.ColorCorrectionTableLength)
                    throw Fail("ExtensionArea.ColorCorrectionTable", $"length {ext.ColorCorrectionTable.Length} != {TgaExtensionArea.ColorCorrectionTableLength}.");

                ushort[] table = ext.ColorCorrectionTable;
                ext.ColorCorrectionTableOffset = add("ExtensionArea.ColorCorrectionTable", (uint)table.Length * sizeof(ushort), () => ToBytes(table));
            }
        }

        /// <summary>
        /// Serializes a table of 32-bit values little-endian.
        /// </summary>
        /// <param name="table">Values to serialize.</param>
        /// <returns>The bytes.</returns>
        private static byte[] ToBytes(uint[] table)
        {
            var builder = new TgaByteBuilder(table.Length * sizeof(uint));
            foreach (uint v in table) builder.Add(v);
            return builder.ToArray();
        }

        /// <summary>
        /// Serializes a table of 16-bit values little-endian.
        /// </summary>
        /// <param name="table">Values to serialize.</param>
        /// <returns>The bytes.</returns>
        private static byte[] ToBytes(ushort[] table)
        {
            var builder = new TgaByteBuilder(table.Length * sizeof(ushort));
            foreach (ushort v in table) builder.Add(v);
            return builder.ToArray();
        }

        /// <summary>
        /// Builds the exception for a structural layout failure.
        /// </summary>
        /// <param name="path">Path of the offending field.</param>
        /// <param name="message">What was wrong.</param>
        /// <returns>The exception to throw.</returns>
        private static TgaValidationException Fail(string path, string message) =>
            new([new TgaValidationError(path, message)]);
    }
}
