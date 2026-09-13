namespace TargaSharp.IO
{
    /// <summary>
    /// Default <see cref="ITgaWriter"/> implementation. Computes field offsets/lengths (see
    /// <see cref="TryComputeLayout"/>), then serializes the header, ID field, color map, image data
    /// (raw or RLE via <see cref="RleCodec"/>), and - when present - the developer directory and
    /// extension area (including its scan-line, postage-stamp and color-correction tables), followed
    /// by the v2.0 footer.
    /// </summary>
    public sealed class TgaWriter : ITgaWriter
    {
        /// <inheritdoc />
        public void Write(TgaFile file, Stream stream)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(stream);
            if (!(stream.CanWrite && stream.CanSeek))
                throw new FileLoadException("Stream writing or seeking is not avaiable!");

            if (!TryComputeLayout(file, out string checkResult))
                throw new InvalidOperationException(checkResult);

            BinaryWriter bw = new BinaryWriter(stream);
            bw.Write(file.Header.ToBytes());

            if (file.ImageOrColorMapArea.ImageID != null)
                bw.Write(file.ImageOrColorMapArea.ImageID.ToBytes());

            if (file.Header.ColorMapType != TgaColorMapType.NoColorMap)
                bw.Write(file.ImageOrColorMapArea.ColorMapData);

            // ImageData
            if (file.Header.ImageType != TgaImageType.NoImageData)
            {
                if (file.Header.ImageType >= TgaImageType.RLE_ColorMapped &&
                    file.Header.ImageType <= TgaImageType.RLE_BlackWhite)
                {
                    int bytesPerPixel = file.Header.ImageSpec.PixelDepth.BytesPerPixel();
                    bw.Write(RleCodec.Encode(file.ImageOrColorMapArea.ImageData, bytesPerPixel, file.Width, file.Height));
                }
                else
                    bw.Write(file.ImageOrColorMapArea.ImageData);
            }

            // Footer
            if (file.Footer != null)
            {
                // DevArea
                if (file.DevArea != null)
                {
                    for (int i = 0; i < file.DevArea.Count; i++)
                        bw.Write(file.DevArea[i].Data);

                    bw.Write((ushort)file.DevArea.Count);

                    for (int i = 0; i < file.DevArea.Count; i++)
                    {
                        bw.Write(file.DevArea[i].Tag);
                        bw.Write(file.DevArea[i].Offset);
                        bw.Write(file.DevArea[i].FieldSize);
                    }
                }

                // ExtArea
                if (file.ExtArea != null)
                {
                    bw.Write(file.ExtArea.ToBytes());

                    if (file.ExtArea.ScanLineTable != null)
                        for (int i = 0; i < file.ExtArea.ScanLineTable.Length; i++)
                            bw.Write(file.ExtArea.ScanLineTable[i]);

                    if (file.ExtArea.PostageStampImage != null)
                        bw.Write(file.ExtArea.PostageStampImage.ToBytes());

                    if (file.ExtArea.ColorCorrectionTable != null)
                        for (int i = 0; i < file.ExtArea.ColorCorrectionTable.Length; i++)
                            bw.Write(file.ExtArea.ColorCorrectionTable[i]);
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

            if (file.ImageOrColorMapArea is null)
            {
                errorStr = "ImageOrColorMapArea = null";
                return false;
            }

            uint offset = TgaHeader.Size; // Virtual Offset

            // IdLength is derived from the ImageID string, not the other way around: the ImageID's
            // own Length (clamped to what a 1-byte IdLength field can hold) drives Header.IdLength,
            // so the header can never disagree with the actual ID field that gets written.
            if (file.ImageOrColorMapArea?.ImageID is not null)
            {
                // Length counts the optional NUL terminator, so the text itself gets one byte less of the 255 max.
                int ending = file.ImageOrColorMapArea.ImageID.UseEndingChar ? 1 : 0;
                int textLength = file.ImageOrColorMapArea.ImageID.OriginalString.Length;
                if (textLength > byte.MaxValue - ending)
                {
                    errorStr = $"ImageID text length {textLength} exceeds the {byte.MaxValue - ending} byte maximum.";
                    return false;
                }

                file.ImageOrColorMapArea.ImageID.Length = textLength + ending;
                file.Header.IdLength = (byte)file.ImageOrColorMapArea.ImageID.Length;
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

                if (file.ImageOrColorMapArea?.ColorMapData == null)
                {
                    errorStr = "ImageOrColorMapArea.ColorMapData = null";
                    return false;
                }

                int cmBytesPerPixel = file.Header.ColorMapSpec.ColorMapEntrySize.BytesPerPixel();
                int lenBytes = file.Header.ColorMapSpec.ColorMapLength * cmBytesPerPixel;

                if (lenBytes != file.ImageOrColorMapArea.ColorMapData.Length)
                {
                    errorStr = "ImageOrColorMapArea.ColorMapData.Length has wrong size!";
                    return false;
                }

                offset += (uint)file.ImageOrColorMapArea.ColorMapData.Length;
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

                if (file.ImageOrColorMapArea?.ImageData == null)
                {
                    errorStr = "ImageOrColorMapArea.ImageData = null";
                    return false;
                }

                bytesPerPixel = file.Header.ImageSpec.PixelDepth.BytesPerPixel();
                if (file.Width * file.Height * bytesPerPixel != file.ImageOrColorMapArea.ImageData.Length)
                {
                    errorStr = "ImageOrColorMapArea.ImageData.Length has wrong size!";
                    return false;
                }

                if (file.Header.ImageType >= TgaImageType.RLE_ColorMapped &&
                    file.Header.ImageType <= TgaImageType.RLE_BlackWhite)
                {
                    byte[]? rle = RleCodec.Encode(file.ImageOrColorMapArea.ImageData, bytesPerPixel, file.Width, file.Height);
                    if (rle == null)
                    {
                        errorStr = "RLE Compressing error! Check Image Data size.";
                        return false;
                    }

                    offset += (uint)rle.Length;
                    rle = null;
                }
                else
                    offset += (uint)file.ImageOrColorMapArea.ImageData.Length;
            }


            if (file.Footer is not null)
            {
                if (file.DevArea is not null)
                {
                    int devAreaCount = file.DevArea.Count;
                    for (int i = 0; i < devAreaCount; i++)
                        if (file.DevArea[i] is null || file.DevArea[i].FieldSize <= 0) //Del Empty Entries
                        {
                            file.DevArea.Entries.RemoveAt(i);
                            devAreaCount--;
                            i--;
                        }

                    if (file.DevArea.Count <= 0) file.Footer.DeveloperDirectoryOffset = 0;

                    // Need at least 2 entries for a duplicate Tag to even be possible; the loop below compares
                    // each adjacent (sorted) pair, so it also correctly catches a duplicate in a 2-entry directory.
                    if (file.DevArea.Count > 1)
                    {
                        file.DevArea.Entries.Sort((a, b) => { return a.Tag.CompareTo(b.Tag); });
                        for (int i = 0; i < file.DevArea.Count - 1; i++)
                            if (file.DevArea[i].Tag == file.DevArea[i + 1].Tag)
                            {
                                errorStr = "DevArea Enties has same Tags!";
                                return false;
                            }
                    }

                    for (int i = 0; i < file.DevArea.Count; i++)
                    {
                        file.DevArea[i].Offset = offset;
                        offset += (uint)file.DevArea[i].FieldSize;
                    }

                    file.Footer.DeveloperDirectoryOffset = offset;
                    offset += (uint)(file.DevArea.Count * 10 + 2);
                }
                else
                    file.Footer.DeveloperDirectoryOffset = 0;



                if (file.ExtArea is not null)
                {
                    file.ExtArea.ExtensionSize = TgaExtArea.MinSize;
                    if (file.ExtArea.OtherDataInExtensionArea != null)
                        file.ExtArea.ExtensionSize += (ushort)file.ExtArea.OtherDataInExtensionArea.Length;

                    file.ExtArea.DateTimeStamp = new TgaDateTime(DateTime.UtcNow);

                    file.Footer.ExtensionAreaOffset = offset;
                    offset += file.ExtArea.ExtensionSize;

                    // ScanLineTable
                    if (file.ExtArea.ScanLineTable == null)
                        file.ExtArea.ScanLineOffset = 0;
                    else
                    {
                        if (file.ExtArea.ScanLineTable.Length != file.Height)
                        {
                            errorStr = "ExtArea.ScanLineTable.Length != Height";
                            return false;
                        }

                        file.ExtArea.ScanLineOffset = offset;
                        offset += (uint)(file.ExtArea.ScanLineTable.Length * 4);
                    }


                    if (file.ExtArea.PostageStampImage is null)
                        file.ExtArea.PostageStampOffset = 0;
                    else
                    {
                        if (file.ExtArea.PostageStampImage.Width == 0 || file.ExtArea.PostageStampImage.Height == 0)
                        {
                            errorStr = "ExtArea.PostageStampImage Width or Height is equal 0!";
                            return false;
                        }

                        if (file.ExtArea.PostageStampImage.Data == null)
                        {
                            errorStr = "ExtArea.PostageStampImage.Data == null";
                            return false;
                        }

                        int postageStampSizeInBytes = file.ExtArea.PostageStampImage.Width * file.ExtArea.PostageStampImage.Height * bytesPerPixel;
                        if (file.Header.ImageType != TgaImageType.NoImageData &&
                            file.ExtArea.PostageStampImage.Data.Length != postageStampSizeInBytes)
                        {
                            errorStr = "ExtArea.PostageStampImage.Data.Length is wrong!";
                            return false;
                        }


                        file.ExtArea.PostageStampOffset = offset;
                        offset += (uint)(file.ExtArea.PostageStampImage.Data.Length);
                    }


                    if (file.ExtArea.ColorCorrectionTable == null)
                        file.ExtArea.ColorCorrectionTableOffset = 0;
                    else
                    {
                        if (file.ExtArea.ColorCorrectionTable.Length != 1024)
                        {
                            errorStr = "ExtArea.ColorCorrectionTable.Length != 256 * 4";
                            return false;
                        }

                        file.ExtArea.ColorCorrectionTableOffset = offset;
                        offset += (uint)(file.ExtArea.ColorCorrectionTable.Length * 2);
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
