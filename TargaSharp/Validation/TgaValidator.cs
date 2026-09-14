namespace TargaSharp.Validation
{
    /// <summary>
    /// <inheritdoc cref="ITgaValidator"/>
    /// <para>Each rule group is implemented as its own small private method that appends to a
    /// shared error list, so a single mutation to a valid <see cref="TgaFile"/> is expected to
    /// trip exactly one rule (see <c>TgaValidatorTests</c>).</para>
    /// </summary>
    public sealed class TgaValidator : ITgaValidator
    {
        /// <summary>
        /// Maximum valid <see cref="TgaAttributeType"/> value; 5-127 are reserved and 128-255 are
        /// unassigned (spec Field 24).
        /// </summary>
        private const byte MaxAttributesType = 4;

        /// <summary>
        /// Largest <see cref="TgaExtensionArea.OtherDataInExtensionArea"/> that still fits the
        /// 2-byte Extension Size field together with the fixed <see cref="TgaExtensionArea.MinSize"/> bytes.
        /// </summary>
        private const int MaxOtherDataLength = ushort.MaxValue - TgaExtensionArea.MinSize;

        /// <summary>
        /// Developer Area tag values &gt;= this are reserved for Truevision; 0-32767 are available
        /// for developer use (spec Field 9 / Developer Area Tag description).
        /// </summary>
        private const ushort MinReservedDevTag = 32768;

        /// <inheritdoc />
        public IReadOnlyList<TgaValidationError> Validate(TgaFile file)
        {
            ArgumentNullException.ThrowIfNull(file);

            var errors = new List<TgaValidationError>();

            ValidateImageDimensions(file, errors);
            ValidatePixelDepth(file, errors);
            ValidatePixelDepthMatchesImageType(file, errors);
            ValidateColorMapTypeKnown(file, errors);
            ValidateColorMapSpec(file, errors);
            ValidateColorMappedImageTypeHasColorMap(file, errors);
            ValidateTrueColorImageTypeHasNoColorMap(file, errors);
            ValidateImageTypeKnown(file, errors);
            ValidateAlphaChannelBits(file, errors);

            ValidateColorMapData(file, errors);
            ValidateImageData(file, errors);
            ValidateImageId(file, errors);

            ValidateDeveloperArea(file, errors);

            ValidateDateTimeStamp(file, errors);
            ValidateJobTime(file, errors);
            ValidateGammaValue(file, errors);
            ValidateAttributesType(file, errors);
            ValidateAttributesTypeMatchesAlphaChannelBits(file, errors);
            ValidateOtherDataInExtensionArea(file, errors);
            ValidateScanLineTable(file, errors);
            ValidateColorCorrectionTable(file, errors);
            ValidatePostageStampImage(file, errors);
            ValidateSoftwareVersion(file, errors);
            ValidateNameFields(file, errors);

            return errors;
        }

        /// <summary>
        /// Spec Fields 5.3/5.4: an image that carries pixel data must have non-zero width and height
        /// (<see cref="TgaImageType.NoImageData"/> leaves them unconstrained). Without this rule a
        /// 0xN file validates clean and only fails inside layout.
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateImageDimensions(TgaFile file, List<TgaValidationError> errors)
        {
            if (file.Header.ImageType == TgaImageType.NoImageData) return;

            if (file.Header.ImageSpec.ImageWidth == 0)
                errors.Add(new TgaValidationError("Header.ImageSpec.ImageWidth", "ImageWidth must be > 0 when ImageType is not NoImageData."));
            if (file.Header.ImageSpec.ImageHeight == 0)
                errors.Add(new TgaValidationError("Header.ImageSpec.ImageHeight", "ImageHeight must be > 0 when ImageType is not NoImageData."));
        }

        /// <summary>
        /// Spec Field 2: only 0 (no color map) and 1 (color map) are defined; 2-127 are reserved
        /// for Truevision and 128-255 for developers.
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateColorMapTypeKnown(TgaFile file, List<TgaValidationError> errors)
        {
            var colorMapType = file.Header.ColorMapType;

            if (!colorMapType.IsKnown())
                errors.Add(new TgaValidationError("Header.ColorMapType", $"{(byte)colorMapType} is a reserved/unknown color map type."));
        }

        /// <summary>
        /// Spec Field 5.5: <see cref="TgaImageSpec.PixelDepth"/> must be 8, 16, 24 or 32, unless
        /// <see cref="TgaImageType.NoImageData"/> (no pixel data, so the field is unconstrained).
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidatePixelDepth(TgaFile file, List<TgaValidationError> errors)
        {
            if (file.Header.ImageType == TgaImageType.NoImageData) return;

            var depth = file.Header.ImageSpec.PixelDepth;
            if (depth is not (TgaPixelDepth.Bpp8 or TgaPixelDepth.Bpp16 or TgaPixelDepth.Bpp24 or TgaPixelDepth.Bpp32))
                errors.Add(new TgaValidationError("Header.ImageSpec.PixelDepth", $"PixelDepth must be 8, 16, 24 or 32 when ImageType is not NoImageData (was {(byte)depth})."));
        }

        /// <summary>
        /// Spec Field 8: color-mapped and black-and-white pixels are 8- or 16-bit, true-color pixels 16-, 24- or 32-bit.
        /// Other combinations have no pixel layout the spec defines (and no GDI+ equivalent).
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidatePixelDepthMatchesImageType(TgaFile file, List<TgaValidationError> errors)
        {
            var imageType = file.Header.ImageType;
            var depth = file.Header.ImageSpec.PixelDepth;
            if (!imageType.IsKnown() || imageType == TgaImageType.NoImageData) return;
            // A non-standard depth is already reported by ValidatePixelDepth.
            if (depth is not (TgaPixelDepth.Bpp8 or TgaPixelDepth.Bpp16 or TgaPixelDepth.Bpp24 or TgaPixelDepth.Bpp32)) return;

            bool valid = imageType.IsTrueColor()
                ? depth is TgaPixelDepth.Bpp16 or TgaPixelDepth.Bpp24 or TgaPixelDepth.Bpp32
                : depth is TgaPixelDepth.Bpp8 or TgaPixelDepth.Bpp16;
            if (!valid)
                errors.Add(new TgaValidationError("Header.ImageSpec.PixelDepth", $"PixelDepth {(byte)depth} is not valid for ImageType {imageType} ({(imageType.IsTrueColor() ? "16, 24 or 32" : "8 or 16")})."));
        }

        /// <summary>
        /// Spec Field 4: when <see cref="TgaColorMapType.NoColorMap"/>, the 5 Color Map
        /// Specification bytes should be zero; when <see cref="TgaColorMapType.ColorMap"/>, the
        /// entry size must be one of the spec's typical values and the map must be non-empty.
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateColorMapSpec(TgaFile file, List<TgaValidationError> errors)
        {
            var colorMapType = file.Header.ColorMapType;
            var colorMapSpec = file.Header.ColorMapSpec;

            if (colorMapType == TgaColorMapType.NoColorMap)
            {
                if (colorMapSpec.FirstEntryIndex != 0)
                    errors.Add(new TgaValidationError("Header.ColorMapSpec.FirstEntryIndex", $"FirstEntryIndex must be 0 when ColorMapType is NoColorMap (was {colorMapSpec.FirstEntryIndex})."));
                if (colorMapSpec.ColorMapLength != 0)
                    errors.Add(new TgaValidationError("Header.ColorMapSpec.ColorMapLength", $"ColorMapLength must be 0 when ColorMapType is NoColorMap (was {colorMapSpec.ColorMapLength})."));
                if (colorMapSpec.ColorMapEntrySize != TgaColorMapEntrySize.Other)
                    errors.Add(new TgaValidationError("Header.ColorMapSpec.ColorMapEntrySize", $"ColorMapEntrySize must be Other(0) when ColorMapType is NoColorMap (was {(byte)colorMapSpec.ColorMapEntrySize})."));
            }
            else if (colorMapType == TgaColorMapType.ColorMap)
            {
                if (colorMapSpec.ColorMapEntrySize is not (TgaColorMapEntrySize.X1R5G5B5 or TgaColorMapEntrySize.A1R5G5B5 or TgaColorMapEntrySize.R8G8B8 or TgaColorMapEntrySize.A8R8G8B8))
                    errors.Add(new TgaValidationError("Header.ColorMapSpec.ColorMapEntrySize", $"ColorMapEntrySize must be 15, 16, 24 or 32 when ColorMapType is ColorMap (was {(byte)colorMapSpec.ColorMapEntrySize})."));
                if (colorMapSpec.ColorMapLength == 0)
                    errors.Add(new TgaValidationError("Header.ColorMapSpec.ColorMapLength", "ColorMapLength must be > 0 when ColorMapType is ColorMap."));
            }
        }

        /// <summary>
        /// Color-mapped image types (<see cref="TgaImageType.UncompressedColorMapped"/>,
        /// <see cref="TgaImageType.RleColorMapped"/>) require a color map to be present.
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateColorMappedImageTypeHasColorMap(TgaFile file, List<TgaValidationError> errors)
        {
            var imageType = file.Header.ImageType;

            if (imageType.IsColorMapped() && file.Header.ColorMapType != TgaColorMapType.ColorMap)
                errors.Add(new TgaValidationError("Header.ColorMapType", $"ColorMapType must be ColorMap when ImageType is {imageType}."));
        }

        /// <summary>
        /// True-color and black-and-white image types should not carry a color map (spec: "True-Color
        /// images do not normally make use of the color map field... set it to Zero to ensure compatibility").
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateTrueColorImageTypeHasNoColorMap(TgaFile file, List<TgaValidationError> errors)
        {
            var imageType = file.Header.ImageType;

            if ((imageType.IsTrueColor() || imageType.IsGrayscale()) && file.Header.ColorMapType != TgaColorMapType.NoColorMap)
                errors.Add(new TgaValidationError("Header.ColorMapType", $"ColorMapType must be NoColorMap when ImageType is {imageType}."));
        }

        /// <summary>
        /// Spec Field 3: only image type codes 0, 1, 2, 3, 9, 10 and 11 are currently defined by
        /// Truevision; every other code is reserved or unknown.
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateImageTypeKnown(TgaFile file, List<TgaValidationError> errors)
        {
            var imageType = file.Header.ImageType;

            if (!imageType.IsKnown())
                errors.Add(new TgaValidationError("Header.ImageType", $"{(byte)imageType} is a reserved/unknown image type."));
        }

        /// <summary>
        /// Spec Field 5.6: the number of attribute (alpha) bits per pixel must be consistent with
        /// <see cref="TgaImageSpec.PixelDepth"/> (32bpp: 0 or 8; 24bpp: 0; 16bpp true-color: 0 or 1
        /// (this implementation's 5-5-5-1 layout, see <see cref="TgaImageArea.ImageData"/>);
        /// 16bpp black-and-white: 0 or 8 (a distinct, equally common 8-bit-intensity + 8-bit-alpha
        /// layout - see real-world fixtures <c>monochrome16_top_left*.tga</c>, which are not
        /// representable in the 5-5-5-1 layout); 8bpp: 0).
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateAlphaChannelBits(TgaFile file, List<TgaValidationError> errors)
        {
            byte alphaBits = file.Header.ImageSpec.ImageDescriptor.AlphaChannelBits;
            bool isBlackAndWhite = file.Header.ImageType.IsGrayscale();

            switch (file.Header.ImageSpec.PixelDepth)
            {
                case TgaPixelDepth.Bpp32 when alphaBits is not (0 or 8):
                    errors.Add(new TgaValidationError("Header.ImageSpec.ImageDescriptor.AlphaChannelBits", $"AlphaChannelBits must be 0 or 8 for 32bpp images (was {alphaBits})."));
                    break;
                case TgaPixelDepth.Bpp24 when alphaBits != 0:
                    errors.Add(new TgaValidationError("Header.ImageSpec.ImageDescriptor.AlphaChannelBits", $"AlphaChannelBits must be 0 for 24bpp images (was {alphaBits})."));
                    break;
                case TgaPixelDepth.Bpp16 when isBlackAndWhite && alphaBits is not (0 or 8):
                    errors.Add(new TgaValidationError("Header.ImageSpec.ImageDescriptor.AlphaChannelBits", $"AlphaChannelBits must be 0 or 8 for 16bpp black-and-white images (was {alphaBits})."));
                    break;
                case TgaPixelDepth.Bpp16 when !isBlackAndWhite && alphaBits is not (0 or 1):
                    errors.Add(new TgaValidationError("Header.ImageSpec.ImageDescriptor.AlphaChannelBits", $"AlphaChannelBits must be 0 or 1 for 16bpp true-color images (was {alphaBits})."));
                    break;
                case TgaPixelDepth.Bpp8 when alphaBits != 0:
                    errors.Add(new TgaValidationError("Header.ImageSpec.ImageDescriptor.AlphaChannelBits", $"AlphaChannelBits must be 0 for 8bpp images (was {alphaBits})."));
                    break;
            }
        }

        /// <summary>
        /// Spec Field 7: present (and correctly sized) only when <see cref="TgaColorMapType.ColorMap"/>;
        /// absent/empty otherwise.
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateColorMapData(TgaFile file, List<TgaValidationError> errors)
        {
            var colorMapData = file.ImageArea.ColorMapData;

            if (file.Header.ColorMapType == TgaColorMapType.ColorMap)
            {
                int expected = file.Header.ColorMapDataLength;
                int actual = colorMapData?.Length ?? 0;
                if (actual != expected)
                    errors.Add(new TgaValidationError("ImageArea.ColorMapData", $"ColorMapData.Length must be {expected} (ColorMapLength * bytes-per-entry) but was {actual}."));
            }
            else if (colorMapData is { Length: > 0 })
                errors.Add(new TgaValidationError("ImageArea.ColorMapData", "ColorMapData must be null or empty when ColorMapType is NoColorMap."));
        }

        /// <summary>
        /// Spec Field 8: (Width * Height) pixels of <see cref="TgaImageSpec.PixelDepth"/> size, or
        /// absent entirely for <see cref="TgaImageType.NoImageData"/>. RLE image types are stored
        /// decoded in <see cref="TgaImageArea.ImageData"/> (re-encoded only when written), so the
        /// same raw-size rule applies to every image type.
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateImageData(TgaFile file, List<TgaValidationError> errors)
        {
            long expected = file.Header.ImageDataLength;
            long actual = file.ImageArea.ImageData?.Length ?? 0;

            if (actual != expected)
                errors.Add(new TgaValidationError("ImageArea.ImageData", $"ImageData.Length must be {expected} (Width * Height * bytes-per-pixel, or 0 when ImageType is NoImageData) but was {actual}."));
        }

        /// <summary>
        /// Spec Field 1/6: the Image ID field (its full <see cref="TgaString.Length"/>, padding and
        /// terminator included) is limited to 255 bytes since ID Length is a single byte.
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateImageId(TgaFile file, List<TgaValidationError> errors)
        {
            var imageId = file.ImageArea.ImageId;
            if (imageId is null) return;

            if (imageId.Length > byte.MaxValue)
                errors.Add(new TgaValidationError("ImageArea.ImageId", $"ImageId length ({imageId.Length}) exceeds the {byte.MaxValue} byte maximum."));
        }

        /// <summary>
        /// Spec Field 9 (Developer Area Tag): the directory holds at most 65535 entries, tags &gt;= 32768
        /// are reserved for Truevision, and no tag may appear twice in the directory. <see cref="TgaDeveloperArea.Entries"/> is a plain
        /// mutable list, so a <see langword="null"/> element is reported as an error rather than
        /// dereferenced.
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateDeveloperArea(TgaFile file, List<TgaValidationError> errors)
        {
            if (file.DeveloperArea is null) return;

            if (file.DeveloperArea.Count > ushort.MaxValue)
                errors.Add(new TgaValidationError("DeveloperArea.Entries", $"Entries.Count ({file.DeveloperArea.Count}) exceeds the {ushort.MaxValue} tags the directory's Number of Tags field can hold."));

            var seenTags = new HashSet<ushort>();
            for (int i = 0; i < file.DeveloperArea.Count; i++)
            {
                TgaDeveloperEntry? entry = file.DeveloperArea[i];
                if (entry is null)
                {
                    errors.Add(new TgaValidationError($"DeveloperArea.Entries[{i}]", "Entry must not be null."));
                    continue;
                }

                ushort tag = entry.Tag;

                if (tag >= MinReservedDevTag)
                    errors.Add(new TgaValidationError($"DeveloperArea.Entries[{i}].Tag", $"Tag {tag} is reserved for Truevision (valid developer range is 0-{MinReservedDevTag - 1})."));

                if (!seenTags.Add(tag))
                    errors.Add(new TgaValidationError($"DeveloperArea.Entries[{i}].Tag", $"Duplicate Tag {tag} in DeveloperArea."));
            }
        }

        /// <summary>
        /// Spec Field 13: an all-zero <see cref="TgaDateTime"/> means "not used" and is always
        /// valid; otherwise Month, Day, Hour, Minute and Second must be in their spec ranges.
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateDateTimeStamp(TgaFile file, List<TgaValidationError> errors)
        {
            var dateTime = file.ExtensionArea?.DateTimeStamp;
            if (dateTime is null) return;

            if (dateTime.IsUnset) return;

            if (dateTime.Month is < 1 or > 12)
                errors.Add(new TgaValidationError("ExtensionArea.DateTimeStamp.Month", $"Month must be 1-12 (was {dateTime.Month})."));
            // Day is bounded by the month's length when both are usable; otherwise only by the spec's 1-31.
            int maxDay = dateTime.Month is >= 1 and <= 12 && dateTime.Year >= 1 && dateTime.Year <= 9999
                ? DateTime.DaysInMonth(dateTime.Year, dateTime.Month)
                : 31;
            if (dateTime.Day < 1 || dateTime.Day > maxDay)
                errors.Add(new TgaValidationError("ExtensionArea.DateTimeStamp.Day", $"Day must be 1-{maxDay} (was {dateTime.Day})."));
            if (dateTime.Hour > 23)
                errors.Add(new TgaValidationError("ExtensionArea.DateTimeStamp.Hour", $"Hour must be 0-23 (was {dateTime.Hour})."));
            if (dateTime.Minute > 59)
                errors.Add(new TgaValidationError("ExtensionArea.DateTimeStamp.Minute", $"Minute must be 0-59 (was {dateTime.Minute})."));
            if (dateTime.Second > 59)
                errors.Add(new TgaValidationError("ExtensionArea.DateTimeStamp.Second", $"Second must be 0-59 (was {dateTime.Second})."));
        }

        /// <summary>
        /// Spec Field 15: Minutes and Seconds must be 0-59 (Hours may be any ushort value).
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateJobTime(TgaFile file, List<TgaValidationError> errors)
        {
            var jobTime = file.ExtensionArea?.JobTime;
            if (jobTime is null) return;

            if (jobTime.Minutes > 59)
                errors.Add(new TgaValidationError("ExtensionArea.JobTime.Minutes", $"Minutes must be 0-59 (was {jobTime.Minutes})."));
            if (jobTime.Seconds > 59)
                errors.Add(new TgaValidationError("ExtensionArea.JobTime.Seconds", $"Seconds must be 0-59 (was {jobTime.Seconds})."));
        }

        /// <summary>
        /// Spec Field 20: a zero denominator means "not used" and is always valid; otherwise the
        /// resulting gamma value must be in the spec's 0.0-10.0 range.
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateGammaValue(TgaFile file, List<TgaValidationError> errors)
        {
            var gammaValue = file.ExtensionArea?.GammaValue;
            if (gammaValue is null || gammaValue.IsUnspecified) return;

            float value = gammaValue.Numerator / (float)gammaValue.Denominator;
            if (value > 10f)
                errors.Add(new TgaValidationError("ExtensionArea.GammaValue", $"GammaValue must be 0.0-10.0 when specified (was {value})."));
        }

        /// <summary>
        /// Spec Field 24: only 0-4 are defined; 5-127 are reserved and 128-255 are unassigned.
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateAttributesType(TgaFile file, List<TgaValidationError> errors)
        {
            if (file.ExtensionArea is null) return;

            byte attributesType = (byte)file.ExtensionArea.AttributesType;
            if (attributesType > MaxAttributesType)
                errors.Add(new TgaValidationError("ExtensionArea.AttributesType", $"AttributesType {attributesType} is reserved (5-127) or unassigned (128-255); valid values are 0-{MaxAttributesType}."));
        }

        /// <summary>
        /// Spec Field 24: <see cref="TgaAttributeType.NoAlpha"/> declares that no alpha data is
        /// included, in which case the descriptor's attribute bits (Field 5.6) must also be zero.
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateAttributesTypeMatchesAlphaChannelBits(TgaFile file, List<TgaValidationError> errors)
        {
            if (file.ExtensionArea is null) return;

            byte alphaBits = file.Header.ImageSpec.ImageDescriptor.AlphaChannelBits;
            if (file.ExtensionArea.AttributesType == TgaAttributeType.NoAlpha && alphaBits != 0)
                errors.Add(new TgaValidationError("ExtensionArea.AttributesType", $"AttributesType is NoAlpha but the image descriptor declares {alphaBits} attribute bits per pixel."));
        }

        /// <summary>
        /// Spec Field 10: Extension Size is a 2-byte field covering the fixed 495 bytes plus any
        /// trailing data, so <see cref="TgaExtensionArea.OtherDataInExtensionArea"/> cannot exceed
        /// 65535 - 495 bytes without the size wrapping.
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateOtherDataInExtensionArea(TgaFile file, List<TgaValidationError> errors)
        {
            var otherData = file.ExtensionArea?.OtherDataInExtensionArea;
            if (otherData is null) return;

            if (otherData.Length > MaxOtherDataLength)
                errors.Add(new TgaValidationError("ExtensionArea.OtherDataInExtensionArea", $"OtherDataInExtensionArea.Length ({otherData.Length}) exceeds the {MaxOtherDataLength} bytes the Extension Size field can represent."));
        }

        /// <summary>
        /// Spec Field 25: when present, one 4-byte offset per scan line, so its length must equal
        /// the image height.
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateScanLineTable(TgaFile file, List<TgaValidationError> errors)
        {
            var scanLineTable = file.ExtensionArea?.ScanLineTable;
            if (scanLineTable is null) return;

            if (scanLineTable.Length != file.Height)
                errors.Add(new TgaValidationError("ExtensionArea.ScanLineTable", $"ScanLineTable.Length ({scanLineTable.Length}) must equal Height ({file.Height})."));
        }

        /// <summary>
        /// Spec Field 27: when present, a fixed 256 x 4 SHORT block (1024 <see cref="ushort"/> values).
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateColorCorrectionTable(TgaFile file, List<TgaValidationError> errors)
        {
            var colorCorrectionTable = file.ExtensionArea?.ColorCorrectionTable;
            if (colorCorrectionTable is null) return;

            if (colorCorrectionTable.Length != TgaExtensionArea.ColorCorrectionTableLength)
                errors.Add(new TgaValidationError("ExtensionArea.ColorCorrectionTable", $"ColorCorrectionTable.Length ({colorCorrectionTable.Length}) must be {TgaExtensionArea.ColorCorrectionTableLength} (256 entries x 4 shorts)."));
        }

        /// <summary>
        /// Spec Field 26: when present, a 1-64 pixel uncompressed image stored in the same pixel
        /// depth as the main image, so its data length must equal Width * Height * bytes-per-pixel.
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidatePostageStampImage(TgaFile file, List<TgaValidationError> errors)
        {
            var postageStampImage = file.ExtensionArea?.PostageStampImage;
            if (postageStampImage is null) return;

            if (postageStampImage.Width == 0 || postageStampImage.Height == 0)
            {
                errors.Add(new TgaValidationError("ExtensionArea.PostageStampImage", $"Width and Height must be 1-{TgaPostageStampImage.MaxSize} (was {postageStampImage.Width}x{postageStampImage.Height})."));
                return;
            }

            int expected = postageStampImage.Width * postageStampImage.Height * file.Header.ImageSpec.PixelDepth.BytesPerPixel();
            if (postageStampImage.Data.Length != expected)
                errors.Add(new TgaValidationError("ExtensionArea.PostageStampImage.Data", $"Data.Length must be {expected} (Width * Height * bytes-per-pixel) but was {postageStampImage.Data.Length}."));
        }

        /// <summary>
        /// Spec Fields 11, 14 and 16 as a group; see <see cref="ValidateNameField"/>.
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateNameFields(TgaFile file, List<TgaValidationError> errors)
        {
            var ext = file.ExtensionArea;
            if (ext is null) return;

            ValidateNameField("ExtensionArea.AuthorName", ext.AuthorName, errors);
            ValidateNameField("ExtensionArea.JobNameOrId", ext.JobNameOrId, errors);
            ValidateNameField("ExtensionArea.SoftwareId", ext.SoftwareId, errors);
        }

        /// <summary>
        /// Spec Fields 11, 14 and 16: a 41-byte ASCII field whose last byte is NUL. Any other
        /// <see cref="TgaString.Length"/> would shift every field after it in the extension area.
        /// </summary>
        /// <param name="path">Field path for the error.</param>
        /// <param name="name">Field value; <see langword="null"/> is serialized as the empty field and is valid.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateNameField(string path, TgaString? name, List<TgaValidationError> errors)
        {
            if (name is null) return;

            if (name.Length != TgaExtensionArea.NameFieldLength)
                errors.Add(new TgaValidationError(path, $"Length must be {TgaExtensionArea.NameFieldLength} (was {name.Length})."));
            if (!name.UseEndingChar)
                errors.Add(new TgaValidationError(path, "UseEndingChar must be true; the field's last byte is NUL per spec."));
        }

        /// <summary>
        /// Spec Field 17: the release-letter byte must be a space (unused, per spec) or an ASCII
        /// letter. NUL is also accepted as "unused": several real-world writers (e.g. the
        /// <c>Alpha Premult.tga</c>/<c>Alpha Straight.tga</c> fixtures) zero-fill this byte instead
        /// of writing a space, matching the '\0' blank-fill convention this library itself uses
        /// elsewhere (see <see cref="TgaString.DefaultBlankSpaceChar"/>).
        /// </summary>
        /// <param name="file">File under validation.</param>
        /// <param name="errors">Sink for rule violations.</param>
        private static void ValidateSoftwareVersion(TgaFile file, List<TgaValidationError> errors)
        {
            var softwareVersion = file.ExtensionArea?.SoftwareVersion;
            if (softwareVersion is null) return;

            // char.IsLetter accepts non-ASCII letters that the 1-byte field would serialize as '?'.
            char letter = softwareVersion.VersionLetter;
            if (letter is not (' ' or '\0' or (>= 'A' and <= 'Z') or (>= 'a' and <= 'z')))
                errors.Add(new TgaValidationError("ExtensionArea.SoftwareVersion.VersionLetter", $"VersionLetter must be ' ', NUL or an ASCII letter (was '{letter}')."));
        }
    }
}
