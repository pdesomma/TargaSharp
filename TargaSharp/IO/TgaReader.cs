namespace TargaSharp.IO
{
    /// <summary>
    /// Default <see cref="ITgaReader"/> implementation. Parses the TGA header, ID field, color map,
    /// image data (raw or RLE via <see cref="RleCodec"/>), and - when a valid v2.0 footer is present -
    /// the developer directory and extension area (including its scan-line, postage-stamp and
    /// color-correction tables).
    /// </summary>
    public sealed class TgaReader : ITgaReader
    {
        /// <inheritdoc />
        public TgaFile Read(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            if (!(stream.CanRead && stream.CanSeek))
                throw new ArgumentException("Stream must be readable and seekable.", nameof(stream));

            try
            {
                return ReadCore(stream);
            }
            // Short reads surface as EndOfStream from BinaryReader or as length errors from the byte[] ctors.
            catch (Exception e) when (e is EndOfStreamException or ArgumentException)
            {
                throw new TgaFormatException("Stream is not a well-formed TGA file: " + e.Message, e);
            }
        }

        /// <inheritdoc />
        public TgaFile Read(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);

            using var stream = new MemoryStream(bytes, false);
            return Read(stream);
        }

        /// <inheritdoc />
        public TgaFile Read(string path)
        {
            ArgumentNullException.ThrowIfNull(path);
            if (!File.Exists(path))
                throw new FileNotFoundException("File: \"" + path + "\" not found!", path);

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Read(stream);
        }

        /// <summary>
        /// Parses a TGA file from a readable, seekable stream positioned anywhere; the stream is left open.
        /// </summary>
        /// <param name="stream">Stream to parse.</param>
        /// <returns>The parsed file.</returns>
        private static TgaFile ReadCore(Stream stream)
        {
            var file = new TgaFile();

            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

            file.Header = new TgaHeader(reader.ReadExactly(TgaHeader.Size, "Header"));

            if (file.Header.IdLength > 0)
                file.ImageArea.ImageId = new TgaString(reader.ReadExactly(file.Header.IdLength, "Image ID"));

            // Spec Field 7: the color map is present only when Field 2 says so; a stale non-zero
            // ColorMapLength on a NoColorMap file must not shift the image data.
            int colorMapDataLength = file.Header.ColorMapDataLength;
            if (colorMapDataLength > 0)
                file.ImageArea.ColorMapData = reader.ReadExactly(colorMapDataLength, "Color map");

            ReadImageData(file, reader);

            // Everything a footer points at must lie between the image data and the footer itself.
            long imageEnd = stream.Position;
            long footerStart = stream.Length - TgaFooter.Size;

            // A v1.0 file may legitimately be shorter than a footer.
            if (footerStart < 0)
                return file;

            stream.Seek(footerStart, SeekOrigin.Begin);
            if (!TgaFooter.TryParse(reader.ReadExactly(TgaFooter.Size, "Footer"), out TgaFooter? footer))
                return file;

            file.Footer = footer;
            ReadDeveloperArea(file, reader, imageEnd, footerStart);
            ReadExtensionArea(file, reader, imageEnd, footerStart);

            return file;
        }

        /// <summary>
        /// Reads the pixel data (Field 8), RLE-decoding it when the image type calls for it.
        /// </summary>
        /// <param name="file">File being populated; its header is already parsed.</param>
        /// <param name="reader">Reader positioned at the first image byte.</param>
        /// <exception cref="TgaFormatException">The header declares more image bytes than a single array can hold.</exception>
        private static void ReadImageData(TgaFile file, BinaryReader reader)
        {
            if (file.Header.ImageType == TgaImageType.NoImageData)
                return;

            int bytesPerPixel = file.Header.ImageSpec.PixelDepth.BytesPerPixel();
            long imageDataSizeLong = file.Header.ImageDataLength;
            if (imageDataSizeLong > int.MaxValue)
                throw new TgaFormatException($"Image data of {imageDataSizeLong} bytes ({file.Width}x{file.Height}x{bytesPerPixel}) exceeds the supported size.");
            int imageDataSize = (int)imageDataSizeLong;

            // A 0-byte image (zero dimension or an unknown pixel depth) is left for the validator to report.
            file.ImageArea.ImageData = imageDataSize == 0 ? []
                : file.Header.ImageType.IsRunLengthEncoded()
                    ? RleCodec.Decode(reader, bytesPerPixel, imageDataSize)
                    : reader.ReadExactly(imageDataSize, "Image data");
        }

        /// <summary>
        /// Reads the developer directory the footer points at (if any) and every field it lists.
        /// </summary>
        /// <param name="file">File being populated; its footer is already parsed.</param>
        /// <param name="reader">Reader over the whole file.</param>
        /// <param name="imageEnd">Offset just past the image data; nothing may be placed before it.</param>
        /// <param name="footerStart">Offset of the footer; nothing may be placed after it.</param>
        /// <exception cref="TgaFormatException">The directory or a field offset points outside the developer/extension region, or the directory runs into the footer.</exception>
        private static void ReadDeveloperArea(TgaFile file, BinaryReader reader, long imageEnd, long footerStart)
        {
            uint directoryOffset = file.Footer!.DeveloperDirectoryOffset;
            if (directoryOffset == 0)
                return;

            CheckOffset(directoryOffset, imageEnd, footerStart, "Footer.DeveloperDirectoryOffset");
            reader.BaseStream.Seek(directoryOffset, SeekOrigin.Begin);
            file.DeveloperArea = new TgaDeveloperArea();
            ushort numberOfTags = reader.ReadUInt16();

            // The directory sits before the footer; reading past it would parse footer bytes as entries.
            long directoryEnd = directoryOffset + sizeof(ushort) + (long)numberOfTags * TgaDeveloperEntry.Size;
            if (directoryEnd > footerStart)
                throw new TgaFormatException($"Developer directory declares {numberOfTags} tags and would end at {directoryEnd}, past the footer at {footerStart}.");

            var tags = new ushort[numberOfTags];
            var tagOffsets = new uint[numberOfTags];
            var tagSizes = new uint[numberOfTags];

            for (int i = 0; i < numberOfTags; i++)
            {
                tags[i] = reader.ReadUInt16();
                tagOffsets[i] = reader.ReadUInt32();
                tagSizes[i] = reader.ReadUInt32();
            }

            for (int i = 0; i < numberOfTags; i++)
            {
                // An empty field reads nothing, so its offset is irrelevant.
                if (tagSizes[i] > 0)
                    CheckOffset(tagOffsets[i], imageEnd, footerStart, $"Developer field {tags[i]} offset");
                reader.BaseStream.Seek(tagOffsets[i], SeekOrigin.Begin);
                file.DeveloperArea.Entries.Add(new TgaDeveloperEntry(tags[i], tagOffsets[i], reader.ReadExactly(tagSizes[i], $"Developer field {tags[i]}")));
            }
        }

        /// <summary>
        /// Reads the extension area the footer points at (if any) plus its scan-line, postage-stamp and
        /// color-correction tables.
        /// </summary>
        /// <param name="file">File being populated; its footer is already parsed.</param>
        /// <param name="reader">Reader over the whole file.</param>
        /// <param name="imageEnd">Offset just past the image data; nothing may be placed before it.</param>
        /// <param name="footerStart">Offset of the footer; nothing may be placed after it.</param>
        /// <exception cref="TgaFormatException">The extension area or one of its table offsets points outside the developer/extension region.</exception>
        private static void ReadExtensionArea(TgaFile file, BinaryReader reader, long imageEnd, long footerStart)
        {
            uint extAreaOffset = file.Footer!.ExtensionAreaOffset;
            if (extAreaOffset == 0)
                return;

            CheckOffset(extAreaOffset, imageEnd, footerStart, "Footer.ExtensionAreaOffset");
            Stream stream = reader.BaseStream;
            stream.Seek(extAreaOffset, SeekOrigin.Begin);
            ushort extAreaSize = reader.ReadUInt16();

            // Per spec the Extension Area Size field must be 495 for a TGA 2.0 extension area.
            // A reader should only parse what it understands: a declared size smaller than
            // TgaExtensionArea.MinSize is not a (valid or forward-compatible) v2.0 ext area, so skip
            // parsing it instead of forcing a read past what was actually declared/written.
            if (extAreaSize < TgaExtensionArea.MinSize)
                return;

            stream.Seek(extAreaOffset, SeekOrigin.Begin);
            var ext = new TgaExtensionArea(reader.ReadExactly(extAreaSize, "Extension area"));
            file.ExtensionArea = ext;

            if (ext.ScanLineOffset > 0)
            {
                CheckOffset(ext.ScanLineOffset, imageEnd, footerStart, "ExtensionArea.ScanLineOffset");
                stream.Seek(ext.ScanLineOffset, SeekOrigin.Begin);
                ext.ScanLineTable = new uint[file.Height];
                for (int i = 0; i < ext.ScanLineTable.Length; i++)
                    ext.ScanLineTable[i] = reader.ReadUInt32();
            }

            if (ext.PostageStampOffset > 0)
            {
                CheckOffset(ext.PostageStampOffset, imageEnd, footerStart, "ExtensionArea.PostageStampOffset");
                stream.Seek(ext.PostageStampOffset, SeekOrigin.Begin);
                byte w = reader.ReadByte();
                byte h = reader.ReadByte();
                int imgDataSize = w * h * file.Header.ImageSpec.PixelDepth.BytesPerPixel();
                // Lenient read: a stamp outside the spec's 1..64 range is skipped rather than failing the whole file.
                if (imgDataSize > 0 && w <= TgaPostageStampImage.MaxSize && h <= TgaPostageStampImage.MaxSize)
                    ext.PostageStampImage = new TgaPostageStampImage(w, h, reader.ReadExactly(imgDataSize, "Postage stamp"));
            }

            if (ext.ColorCorrectionTableOffset > 0)
            {
                CheckOffset(ext.ColorCorrectionTableOffset, imageEnd, footerStart, "ExtensionArea.ColorCorrectionTableOffset");
                stream.Seek(ext.ColorCorrectionTableOffset, SeekOrigin.Begin);
                ext.ColorCorrectionTable = new ushort[TgaExtensionArea.ColorCorrectionTableLength];
                for (int i = 0; i < ext.ColorCorrectionTable.Length; i++)
                    ext.ColorCorrectionTable[i] = reader.ReadUInt16();
            }
        }

        /// <summary>
        /// Rejects an absolute file offset that points into the header/image region or past the footer:
        /// following it would parse unrelated bytes as a developer field, extension area or table.
        /// </summary>
        /// <param name="offset">Offset read from the file.</param>
        /// <param name="imageEnd">Lowest acceptable offset (just past the image data).</param>
        /// <param name="footerStart">Highest acceptable offset (the footer's own).</param>
        /// <param name="field">Field name for the error message.</param>
        /// <exception cref="TgaFormatException"><paramref name="offset"/> is out of range.</exception>
        private static void CheckOffset(uint offset, long imageEnd, long footerStart, string field)
        {
            if (offset < imageEnd || offset > footerStart)
                throw new TgaFormatException($"{field} ({offset}) points outside the {imageEnd}-{footerStart} byte range between the image data and the footer.");
        }
    }
}
