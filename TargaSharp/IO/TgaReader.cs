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

        /// <summary>
        /// Parses a TGA file from a readable, seekable stream positioned anywhere; the stream is left open.
        /// </summary>
        /// <param name="stream">Stream to parse.</param>
        /// <returns>The parsed file.</returns>
        private static TgaFile ReadCore(Stream stream)
        {
            var file = new TgaFile();

            stream.Seek(0, SeekOrigin.Begin);
            var binaryReader = new BinaryReader(stream);

            file.Header = new TgaHeader(binaryReader.ReadBytes(TgaHeader.Size));

            if (file.Header.IdLength > 0)
                file.ImageOrColorMapArea.ImageID = new TgaString(binaryReader.ReadBytes(file.Header.IdLength));

            if (file.Header.ColorMapSpec.ColorMapLength > 0)
            {
                int cmBytesPerPixel = file.Header.ColorMapSpec.ColorMapEntrySize.BytesPerPixel();
                int lenBytes = file.Header.ColorMapSpec.ColorMapLength * cmBytesPerPixel;
                file.ImageOrColorMapArea.ColorMapData = binaryReader.ReadBytes(lenBytes);
            }

            // Read Image Data
            int bytesPerPixel = file.Header.ImageSpec.PixelDepth.BytesPerPixel();
            if (file.Header.ImageType != TgaImageType.NoImageData)
            {
                int imageDataSize = file.Width * file.Height * bytesPerPixel;
                if (file.Header.ImageType.IsRunLengthEncoded())
                {
                    file.ImageOrColorMapArea.ImageData = RleCodec.Decode(binaryReader, bytesPerPixel, imageDataSize);
                }
                else
                {
                    file.ImageOrColorMapArea.ImageData = binaryReader.ReadBytes(imageDataSize);
                    if (file.ImageOrColorMapArea.ImageData.Length != imageDataSize)
                        throw new EndOfStreamException($"Image data truncated: expected {imageDataSize} bytes, got {file.ImageOrColorMapArea.ImageData.Length}.");
                }
            }

            // Try parse Footer (a v1.0 file may legitimately be shorter than a footer)
            if (stream.Length < TgaFooter.Size)
                return file;

            stream.Seek(-TgaFooter.Size, SeekOrigin.End);
            if (TgaFooter.TryParse(binaryReader.ReadBytes(TgaFooter.Size), out TgaFooter? mbFooter))
            {
                file.Footer = mbFooter;
                uint devDirOffset = file.Footer.DeveloperDirectoryOffset;
                uint extAreaOffset = file.Footer.ExtensionAreaOffset;

                // If Dev Area exist, read it.
                if (devDirOffset != 0)
                {
                    stream.Seek(devDirOffset, SeekOrigin.Begin);
                    file.DevArea = new TgaDevArea();
                    uint numberOfTags = binaryReader.ReadUInt16();

                    ushort[] tags = new ushort[numberOfTags];
                    uint[] tagOffsets = new uint[numberOfTags];
                    uint[] tagSizes = new uint[numberOfTags];

                    for (int i = 0; i < numberOfTags; i++)
                    {
                        tags[i] = binaryReader.ReadUInt16();
                        tagOffsets[i] = binaryReader.ReadUInt32();
                        tagSizes[i] = binaryReader.ReadUInt32();
                    }

                    for (int i = 0; i < numberOfTags; i++)
                    {
                        stream.Seek(tagOffsets[i], SeekOrigin.Begin);
                        var entry = new TgaDevEntry(tags[i], tagOffsets[i], binaryReader.ReadBytes((int)tagSizes[i]));
                        file.DevArea.Entries.Add(entry);
                    }
                }

                // If Ext Area exist, read it.
                if (extAreaOffset != 0)
                {
                    stream.Seek(extAreaOffset, SeekOrigin.Begin);
                    ushort extAreaSize = binaryReader.ReadUInt16();

                    // Per spec the Extension Area Size field must be 495 for a TGA 2.0 extension area.
                    // A reader should only parse what it understands: a declared size smaller than
                    // TgaExtArea.MinSize is not a (valid or forward-compatible) v2.0 ext area, so skip
                    // parsing it instead of forcing a read past what was actually declared/written.
                    if (extAreaSize >= TgaExtArea.MinSize)
                    {
                        stream.Seek(extAreaOffset, SeekOrigin.Begin);
                        file.ExtArea = new TgaExtArea(binaryReader.ReadBytes(extAreaSize));

                        if (file.ExtArea.ScanLineOffset > 0)
                        {
                            stream.Seek(file.ExtArea.ScanLineOffset, SeekOrigin.Begin);
                            file.ExtArea.ScanLineTable = new uint[file.Height];
                            for (int i = 0; i < file.ExtArea.ScanLineTable.Length; i++)
                                file.ExtArea.ScanLineTable[i] = binaryReader.ReadUInt32();
                        }

                        if (file.ExtArea.PostageStampOffset > 0)
                        {
                            stream.Seek(file.ExtArea.PostageStampOffset, SeekOrigin.Begin);
                            byte w = binaryReader.ReadByte();
                            byte h = binaryReader.ReadByte();
                            int imgDataSize = w * h * bytesPerPixel;
                            // Lenient read: a stamp outside the spec's 1..64 range is skipped rather than failing the whole file.
                            if (imgDataSize > 0 && w <= TgaPostageStampImage.MaxSize && h <= TgaPostageStampImage.MaxSize)
                                file.ExtArea.PostageStampImage = new TgaPostageStampImage(w, h, binaryReader.ReadBytes(imgDataSize));
                        }

                        if (file.ExtArea.ColorCorrectionTableOffset > 0)
                        {
                            stream.Seek(file.ExtArea.ColorCorrectionTableOffset, SeekOrigin.Begin);
                            file.ExtArea.ColorCorrectionTable = new ushort[256 * 4];
                            for (int i = 0; i < file.ExtArea.ColorCorrectionTable.Length; i++)
                                file.ExtArea.ColorCorrectionTable[i] = binaryReader.ReadUInt16();
                        }
                    }
                }
            }

            return file;
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
                throw new FileNotFoundException("File: \"" + path + "\" not found!");

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Read(stream);
        }
    }
}
