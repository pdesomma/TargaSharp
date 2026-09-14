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
                file.ImageArea.ImageId = new TgaString(ReadExactly(binaryReader, file.Header.IdLength, "Image ID"));

            if (file.Header.ColorMapSpec.ColorMapLength > 0)
            {
                int cmBytesPerPixel = file.Header.ColorMapSpec.ColorMapEntrySize.BytesPerPixel();
                int lenBytes = file.Header.ColorMapSpec.ColorMapLength * cmBytesPerPixel;
                file.ImageArea.ColorMapData = ReadExactly(binaryReader, lenBytes, "Color map");
            }

            // Read Image Data
            int bytesPerPixel = file.Header.ImageSpec.PixelDepth.BytesPerPixel();
            if (file.Header.ImageType != TgaImageType.NoImageData)
            {
                // ushort * ushort * 4 exceeds int.MaxValue (32768 x 32768 x 32bpp wraps to exactly 0), so size in long.
                long imageDataSizeLong = (long)file.Width * file.Height * bytesPerPixel;
                if (imageDataSizeLong > int.MaxValue)
                    throw new EndOfStreamException($"Image data of {imageDataSizeLong} bytes ({file.Width}x{file.Height}x{bytesPerPixel}) exceeds the supported size.");
                int imageDataSize = (int)imageDataSizeLong;

                file.ImageArea.ImageData = file.Header.ImageType.IsRunLengthEncoded()
                    ? RleCodec.Decode(binaryReader, bytesPerPixel, imageDataSize)
                    : ReadExactly(binaryReader, imageDataSize, "Image data");
            }

            // Try parse Footer (a v1.0 file may legitimately be shorter than a footer)
            if (stream.Length < TgaFooter.Size)
                return file;

            stream.Seek(-TgaFooter.Size, SeekOrigin.End);
            if (TgaFooter.TryParse(binaryReader.ReadBytes(TgaFooter.Size), out TgaFooter? mbFooter))
            {
                file.Footer = mbFooter;
                uint devDirOffset = mbFooter.DeveloperDirectoryOffset;
                uint extAreaOffset = mbFooter.ExtensionAreaOffset;

                // If Dev Area exist, read it.
                if (devDirOffset != 0)
                {
                    stream.Seek(devDirOffset, SeekOrigin.Begin);
                    file.DeveloperArea = new TgaDeveloperArea();
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
                        var entry = new TgaDeveloperEntry(tags[i], tagOffsets[i], ReadExactly(binaryReader, tagSizes[i], $"Developer field {tags[i]}"));
                        file.DeveloperArea.Entries.Add(entry);
                    }
                }

                // If Ext Area exist, read it.
                if (extAreaOffset != 0)
                {
                    stream.Seek(extAreaOffset, SeekOrigin.Begin);
                    ushort extAreaSize = binaryReader.ReadUInt16();

                    // Per spec the Extension Area Size field must be 495 for a TGA 2.0 extension area.
                    // A reader should only parse what it understands: a declared size smaller than
                    // TgaExtensionArea.MinSize is not a (valid or forward-compatible) v2.0 ext area, so skip
                    // parsing it instead of forcing a read past what was actually declared/written.
                    if (extAreaSize >= TgaExtensionArea.MinSize)
                    {
                        stream.Seek(extAreaOffset, SeekOrigin.Begin);
                        file.ExtensionArea = new TgaExtensionArea(ReadExactly(binaryReader, extAreaSize, "Extension area"));

                        if (file.ExtensionArea.ScanLineOffset > 0)
                        {
                            stream.Seek(file.ExtensionArea.ScanLineOffset, SeekOrigin.Begin);
                            file.ExtensionArea.ScanLineTable = new uint[file.Height];
                            for (int i = 0; i < file.ExtensionArea.ScanLineTable.Length; i++)
                                file.ExtensionArea.ScanLineTable[i] = binaryReader.ReadUInt32();
                        }

                        if (file.ExtensionArea.PostageStampOffset > 0)
                        {
                            stream.Seek(file.ExtensionArea.PostageStampOffset, SeekOrigin.Begin);
                            byte w = binaryReader.ReadByte();
                            byte h = binaryReader.ReadByte();
                            int imgDataSize = w * h * bytesPerPixel;
                            // Lenient read: a stamp outside the spec's 1..64 range is skipped rather than failing the whole file.
                            if (imgDataSize > 0 && w <= TgaPostageStampImage.MaxSize && h <= TgaPostageStampImage.MaxSize)
                                file.ExtensionArea.PostageStampImage = new TgaPostageStampImage(w, h, ReadExactly(binaryReader, imgDataSize, "Postage stamp"));
                        }

                        if (file.ExtensionArea.ColorCorrectionTableOffset > 0)
                        {
                            stream.Seek(file.ExtensionArea.ColorCorrectionTableOffset, SeekOrigin.Begin);
                            file.ExtensionArea.ColorCorrectionTable = new ushort[TgaExtensionArea.ColorCorrectionTableLength];
                            for (int i = 0; i < file.ExtensionArea.ColorCorrectionTable.Length; i++)
                                file.ExtensionArea.ColorCorrectionTable[i] = binaryReader.ReadUInt16();
                        }
                    }
                }
            }

            return file;
        }

        /// <summary>
        /// Reads exactly <paramref name="count"/> bytes. The count comes from a header field in the file
        /// itself, so it is checked against what the stream can still supply <em>before</em> anything is
        /// allocated: a hostile or corrupt file must not be able to force a multi-GB allocation, and a
        /// truncated one must fail here rather than hand back a silently shortened field.
        /// </summary>
        /// <param name="reader">Reader positioned at the start of the field.</param>
        /// <param name="count">Declared byte length of the field.</param>
        /// <param name="what">Field name for the error message.</param>
        /// <returns>Exactly <paramref name="count"/> bytes.</returns>
        /// <exception cref="EndOfStreamException">Fewer than <paramref name="count"/> bytes remain.</exception>
        private static byte[] ReadExactly(BinaryReader reader, long count, string what)
        {
            long remaining = reader.BaseStream.Length - reader.BaseStream.Position;
            if (count > remaining)
                throw new EndOfStreamException($"{what} declares {count} bytes but only {Math.Max(remaining, 0)} remain in the stream.");

            return reader.ReadBytes((int)count);
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
