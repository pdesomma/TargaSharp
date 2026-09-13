using TargaSharp.Validation;

namespace TargaSharp.IO
{
    /// <summary>
    /// Default <see cref="ITgaWriter"/> implementation. Validates the <see cref="TgaFile"/> (see
    /// <see cref="ITgaValidator"/>), computes field offsets/lengths (see <see cref="TryComputeLayout"/>),
    /// then serializes the header, ID field, color map, image data (raw or RLE via <see cref="RleCodec"/>),
    /// and - when present - the developer directory and extension area (including its scan-line,
    /// postage-stamp and color-correction tables), followed by the v2.0 footer.
    /// </summary>
    public sealed class TgaWriter : ITgaWriter
    {
        /// <summary>
        /// Validator run against a <see cref="TgaFile"/> before it is written.
        /// </summary>
        private readonly ITgaValidator _validator;

        /// <summary>
        /// Make a <see cref="TgaWriter"/> that validates with a new <see cref="TgaValidator"/>.
        /// </summary>
        public TgaWriter() : this(new TgaValidator()) { }

        /// <summary>
        /// Make a <see cref="TgaWriter"/> that validates with <paramref name="validator"/>.
        /// </summary>
        /// <param name="validator">The validator to run against a <see cref="TgaFile"/> before it is written.</param>
        /// <exception cref="ArgumentNullException"><paramref name="validator"/> is <see langword="null"/>.</exception>
        public TgaWriter(ITgaValidator validator)
        {
            ArgumentNullException.ThrowIfNull(validator);
            _validator = validator;
        }

        /// <inheritdoc />
        public void Write(TgaFile file, Stream stream)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(stream);
            if (!(stream.CanWrite && stream.CanSeek))
                throw new ArgumentException("Stream must be writable and seekable.", nameof(stream));

            IReadOnlyList<TgaValidationError> errors = _validator.Validate(file);
            if (errors.Count > 0)
                throw new TgaValidationException(errors);

            // Layout failures are structural problems the validator didn't model; surface them the same way.
            if (!TryComputeLayout(file, out string checkResult))
                throw new TgaValidationException([new TgaValidationError("Layout", checkResult)]);

            BinaryWriter bw = new BinaryWriter(stream);
            bw.Write(file.Header.ToBytes());

            if (file.ImageArea.ImageId != null)
                bw.Write(file.ImageArea.ImageId.ToBytes());

            if (file.Header.ColorMapType != TgaColorMapType.NoColorMap)
                bw.Write(file.ImageArea.ColorMapData);

            // ImageData
            if (file.Header.ImageType != TgaImageType.NoImageData)
            {
                if (file.Header.ImageType.IsRunLengthEncoded())
                {
                    int bytesPerPixel = file.Header.ImageSpec.PixelDepth.BytesPerPixel();
                    bw.Write(RleCodec.Encode(file.ImageArea.ImageData, bytesPerPixel, file.Width, file.Height));
                }
                else
                    bw.Write(file.ImageArea.ImageData);
            }

            // Footer
            if (file.Footer != null)
            {
                // DeveloperArea
                if (file.DeveloperArea != null)
                {
                    for (int i = 0; i < file.DeveloperArea.Count; i++)
                        bw.Write(file.DeveloperArea[i].Data);

                    bw.Write((ushort)file.DeveloperArea.Count);

                    for (int i = 0; i < file.DeveloperArea.Count; i++)
                    {
                        bw.Write(file.DeveloperArea[i].Tag);
                        bw.Write(file.DeveloperArea[i].Offset);
                        bw.Write(file.DeveloperArea[i].FieldSize);
                    }
                }

                // ExtensionArea
                if (file.ExtensionArea != null)
                {
                    bw.Write(file.ExtensionArea.ToBytes());

                    if (file.ExtensionArea.ScanLineTable != null)
                        for (int i = 0; i < file.ExtensionArea.ScanLineTable.Length; i++)
                            bw.Write(file.ExtensionArea.ScanLineTable[i]);

                    if (file.ExtensionArea.PostageStampImage != null)
                        bw.Write(file.ExtensionArea.PostageStampImage.ToBytes());

                    if (file.ExtensionArea.ColorCorrectionTable != null)
                        for (int i = 0; i < file.ExtensionArea.ColorCorrectionTable.Length; i++)
                            bw.Write(file.ExtensionArea.ColorCorrectionTable[i]);
                }

                bw.Write(file.Footer.ToBytes());
            }

            bw.Flush();
            stream.Flush();
        }

        /// <inheritdoc />
        public byte[] Write(TgaFile file)
        {
            using var ms = new MemoryStream();
            Write(file, ms);
            return ms.ToArray();
        }

        /// <inheritdoc />
        public void Write(TgaFile file, string path)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(path);

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var ms = new MemoryStream();
            Write(file, ms);
            ms.WriteTo(fs);
            fs.Flush();
        }

        /// <summary>
        /// Check and update all of <paramref name="file"/>'s fields with data length and offsets.
        /// </summary>
        /// <param name="file">The <see cref="TgaFile"/> whose layout is being computed.</param>
        /// <param name="errorStr">Description of the failure, or <see cref="string.Empty"/> on success.</param>
        /// <returns>Return "true", if all OK or "false", if checking failed.</returns>
        internal bool TryComputeLayout(TgaFile file, out string errorStr)
        {
            errorStr = string.Empty;
            if (file.Header is null)
            {
                errorStr = "Header = null";
                return false;
            }

            if (file.ImageArea is null)
            {
                errorStr = "ImageArea = null";
                return false;
            }

            uint offset = TgaHeader.Size; // Virtual Offset

            // IdLength is derived from the ImageId string, not the other way around: the ImageId's
            // own Length (clamped to what a 1-byte IdLength field can hold) drives Header.IdLength,
            // so the header can never disagree with the actual ID field that gets written.
            if (file.ImageArea?.ImageId is not null)
            {
                // Length counts the optional NUL terminator, so the text itself gets one byte less of the 255 max.
                int ending = file.ImageArea.ImageId.UseEndingChar ? 1 : 0;
                int textLength = file.ImageArea.ImageId.OriginalString.Length;
                if (textLength > byte.MaxValue - ending)
                {
                    errorStr = $"ImageId text length {textLength} exceeds the {byte.MaxValue - ending} byte maximum.";
                    return false;
                }

                file.ImageArea.ImageId.Length = textLength + ending;
                file.Header.IdLength = (byte)file.ImageArea.ImageId.Length;
                offset += file.Header.IdLength;
            }
            else
                file.Header.IdLength = 0;



            if (file.Header.ColorMapType != TgaColorMapType.NoColorMap)
            {
                if (file.Header.ColorMapSpec is null)
                {
                    errorStr = "Header.ColorMapSpec = null";
                    return false;
                }

                if (file.Header.ColorMapSpec.ColorMapLength == 0)
                {
                    errorStr = "Header.ColorMapSpec.ColorMapLength = 0";
                    return false;
                }

                if (file.ImageArea?.ColorMapData == null)
                {
                    errorStr = "ImageArea.ColorMapData = null";
                    return false;
                }

                int cmBytesPerPixel = file.Header.ColorMapSpec.ColorMapEntrySize.BytesPerPixel();
                int lenBytes = file.Header.ColorMapSpec.ColorMapLength * cmBytesPerPixel;

                if (lenBytes != file.ImageArea.ColorMapData.Length)
                {
                    errorStr = "ImageArea.ColorMapData.Length has wrong size!";
                    return false;
                }

                offset += (uint)file.ImageArea.ColorMapData.Length;
            }

            int bytesPerPixel = 0;
            if (file.Header.ImageType != TgaImageType.NoImageData)
            {
                if (file.Header.ImageSpec is null)
                {
                    errorStr = "Header.ImageSpec = null";
                    return false;
                }

                if (file.Header.ImageSpec.ImageWidth == 0 || file.Header.ImageSpec.ImageHeight == 0)
                {
                    errorStr = "Header.ImageSpec.ImageWidth = 0 or Header.ImageSpec.ImageHeight = 0";
                    return false;
                }

                if (file.ImageArea?.ImageData == null)
                {
                    errorStr = "ImageArea.ImageData = null";
                    return false;
                }

                bytesPerPixel = file.Header.ImageSpec.PixelDepth.BytesPerPixel();
                if (file.Width * file.Height * bytesPerPixel != file.ImageArea.ImageData.Length)
                {
                    errorStr = "ImageArea.ImageData.Length has wrong size!";
                    return false;
                }

                if (file.Header.ImageType.IsRunLengthEncoded())
                {
                    // Encoded size is only known by encoding; the validator has already checked ImageData length.
                    offset += (uint)RleCodec.Encode(file.ImageArea.ImageData, bytesPerPixel, file.Width, file.Height).Length;
                }
                else
                    offset += (uint)file.ImageArea.ImageData.Length;
            }


            if (file.Footer is not null)
            {
                if (file.DeveloperArea is not null)
                {
                    int devAreaCount = file.DeveloperArea.Count;
                    for (int i = 0; i < devAreaCount; i++)
                        if (file.DeveloperArea[i] is null || file.DeveloperArea[i].FieldSize <= 0) //Del Empty Entries
                        {
                            file.DeveloperArea.Entries.RemoveAt(i);
                            devAreaCount--;
                            i--;
                        }

                    if (file.DeveloperArea.Count <= 0) file.Footer.DeveloperDirectoryOffset = 0;

                    // Need at least 2 entries for a duplicate Tag to even be possible; the loop below compares
                    // each adjacent (sorted) pair, so it also correctly catches a duplicate in a 2-entry directory.
                    if (file.DeveloperArea.Count > 1)
                    {
                        file.DeveloperArea.Entries.Sort((a, b) => { return a.Tag.CompareTo(b.Tag); });
                        for (int i = 0; i < file.DeveloperArea.Count - 1; i++)
                            if (file.DeveloperArea[i].Tag == file.DeveloperArea[i + 1].Tag)
                            {
                                errorStr = "DeveloperArea Enties has same Tags!";
                                return false;
                            }
                    }

                    for (int i = 0; i < file.DeveloperArea.Count; i++)
                    {
                        file.DeveloperArea[i].Offset = offset;
                        offset += (uint)file.DeveloperArea[i].FieldSize;
                    }

                    file.Footer.DeveloperDirectoryOffset = offset;
                    offset += (uint)(file.DeveloperArea.Count * 10 + 2);
                }
                else
                    file.Footer.DeveloperDirectoryOffset = 0;



                if (file.ExtensionArea is not null)
                {
                    file.ExtensionArea.ExtensionSize = TgaExtensionArea.MinSize;
                    if (file.ExtensionArea.OtherDataInExtensionArea != null)
                        file.ExtensionArea.ExtensionSize += (ushort)file.ExtensionArea.OtherDataInExtensionArea.Length;

                    file.ExtensionArea.DateTimeStamp = new TgaDateTime(DateTime.UtcNow);

                    file.Footer.ExtensionAreaOffset = offset;
                    offset += file.ExtensionArea.ExtensionSize;

                    // ScanLineTable
                    if (file.ExtensionArea.ScanLineTable == null)
                        file.ExtensionArea.ScanLineOffset = 0;
                    else
                    {
                        if (file.ExtensionArea.ScanLineTable.Length != file.Height)
                        {
                            errorStr = "ExtensionArea.ScanLineTable.Length != Height";
                            return false;
                        }

                        file.ExtensionArea.ScanLineOffset = offset;
                        offset += (uint)(file.ExtensionArea.ScanLineTable.Length * 4);
                    }


                    if (file.ExtensionArea.PostageStampImage is null)
                        file.ExtensionArea.PostageStampOffset = 0;
                    else
                    {
                        if (file.ExtensionArea.PostageStampImage.Width == 0 || file.ExtensionArea.PostageStampImage.Height == 0)
                        {
                            errorStr = "ExtensionArea.PostageStampImage Width or Height is equal 0!";
                            return false;
                        }

                        if (file.ExtensionArea.PostageStampImage.Data == null)
                        {
                            errorStr = "ExtensionArea.PostageStampImage.Data == null";
                            return false;
                        }

                        int postageStampSizeInBytes = file.ExtensionArea.PostageStampImage.Width * file.ExtensionArea.PostageStampImage.Height * bytesPerPixel;
                        if (file.Header.ImageType != TgaImageType.NoImageData &&
                            file.ExtensionArea.PostageStampImage.Data.Length != postageStampSizeInBytes)
                        {
                            errorStr = "ExtensionArea.PostageStampImage.Data.Length is wrong!";
                            return false;
                        }


                        file.ExtensionArea.PostageStampOffset = offset;
                        offset += (uint)(file.ExtensionArea.PostageStampImage.Data.Length);
                    }


                    if (file.ExtensionArea.ColorCorrectionTable == null)
                        file.ExtensionArea.ColorCorrectionTableOffset = 0;
                    else
                    {
                        if (file.ExtensionArea.ColorCorrectionTable.Length != 1024)
                        {
                            errorStr = "ExtensionArea.ColorCorrectionTable.Length != 256 * 4";
                            return false;
                        }

                        file.ExtensionArea.ColorCorrectionTableOffset = offset;
                        offset += (uint)(file.ExtensionArea.ColorCorrectionTable.Length * 2);
                    }
                }
                else
                    file.Footer.ExtensionAreaOffset = 0;



                if (file.Footer.ToBytes().Length != TgaFooter.Size)
                {
                    errorStr = "Footer.Length is wrong!";
                    return false;
                }
                offset += TgaFooter.Size;
            }
            return true;
        }
    }
}
