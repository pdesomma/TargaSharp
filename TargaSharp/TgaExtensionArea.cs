namespace TargaSharp
{
    /// <summary>
    /// Extension Area
    /// </summary>
    public sealed record TgaExtensionArea : ICloneable
    {
        /// <summary>
        /// Size in bytes of the fixed part of the extension area (spec field 10, TGA 2.0).
        /// </summary>
        public const int MinSize = 495;

        /// <summary>
        /// Longest <see cref="OtherDataInExtensionArea"/> that still fits the 2-byte Extension Size field together with <see cref="MinSize"/>.
        /// </summary>
        public const int MaxOtherDataLength = ushort.MaxValue - MinSize;

        /// <summary>
        /// Number of <see cref="ushort"/> values in a color correction table: 256 entries x 4 channels (spec field 27).
        /// </summary>
        public const int ColorCorrectionTableLength = 256 * 4;

        /// <summary>
        /// Byte length of the Author Name, Job Name/ID and Software ID fields: 40 characters plus a NUL terminator (spec fields 11, 15, 17).
        /// </summary>
        public const int NameFieldLength = 41;

        /// <summary>
        /// Backing field for <see cref="OtherDataInExtensionArea"/>.
        /// </summary>
        private byte[]? _otherDataInExtensionArea;

        /// <summary>
        /// Backing field for <see cref="AuthorName"/>.
        /// </summary>
        private TgaString _authorName = new TgaString(NameFieldLength, true);

        /// <summary>
        /// Backing field for <see cref="AuthorComments"/>.
        /// </summary>
        private TgaComment _authorComments = new TgaComment();

        /// <summary>
        /// Backing field for <see cref="DateTimeStamp"/>.
        /// </summary>
        private TgaDateTime _dateTimeStamp = new TgaDateTime();

        /// <summary>
        /// Backing field for <see cref="JobNameOrId"/>.
        /// </summary>
        private TgaString _jobNameOrId = new TgaString(NameFieldLength, true);

        /// <summary>
        /// Backing field for <see cref="JobTime"/>.
        /// </summary>
        private TgaTime _jobTime = new TgaTime();

        /// <summary>
        /// Backing field for <see cref="SoftwareId"/>.
        /// </summary>
        private TgaString _softwareId = new TgaString(NameFieldLength, true);

        /// <summary>
        /// Backing field for <see cref="SoftwareVersion"/>.
        /// </summary>
        private TgaSoftwareVersion _softwareVersion = new TgaSoftwareVersion();

        /// <summary>
        /// Backing field for <see cref="KeyColor"/>.
        /// </summary>
        private TgaColorKey _keyColor = new TgaColorKey();

        /// <summary>
        /// Backing field for <see cref="PixelAspectRatio"/>.
        /// </summary>
        private TgaFraction _pixelAspectRatio = TgaFraction.Empty;

        /// <summary>
        /// Backing field for <see cref="GammaValue"/>.
        /// </summary>
        private TgaFraction _gammaValue = TgaFraction.Empty;

        /// <summary>
        /// Make <see cref="TgaExtensionArea"/> from bytes. Warning: <see cref="ScanLineTable"/>,
        /// <see cref="PostageStampImage"/>, <see cref="ColorCorrectionTable"/> not included,
        /// because they are optional and may not be present in the Extension Area of the TGA file.
        /// </summary>
        /// <param name="bytes">Bytes of <see cref="TgaExtensionArea"/>.</param>
        /// <param name="slt">Scan Line Table.</param>
        /// <param name="postageStampImage">Postage Stamp Image.</param>
        /// <param name="cct">Color Correction Table.</param>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bytes"/> is shorter than <see cref="MinSize"/> or longer than <see cref="ushort.MaxValue"/>.</exception>
        public TgaExtensionArea(byte[] bytes, uint[]? slt = null, TgaPostageStampImage? postageStampImage = null, ushort[]? cct = null)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            if (bytes.Length < MinSize)
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length, $"Length must be >= {MinSize}.");
            if (bytes.Length > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length, $"Length must be <= {ushort.MaxValue} (ExtensionSize is a ushort).");

            AuthorName = new TgaString(bytes.AsSpan(2, NameFieldLength).ToArray(), true);
            AuthorComments = new TgaComment(bytes.AsSpan(43, TgaComment.Size).ToArray());
            DateTimeStamp = new TgaDateTime(bytes.AsSpan(367, TgaDateTime.Size).ToArray());
            JobNameOrId = new TgaString(bytes.AsSpan(379, NameFieldLength).ToArray(), true);
            JobTime = new TgaTime(bytes.AsSpan(420, TgaTime.Size).ToArray());
            SoftwareId = new TgaString(bytes.AsSpan(426, NameFieldLength).ToArray(), true);
            SoftwareVersion = new TgaSoftwareVersion(bytes.AsSpan(467, TgaSoftwareVersion.Size).ToArray());
            KeyColor = new TgaColorKey(bytes.AsSpan(470, TgaColorKey.Size).ToArray());
            PixelAspectRatio = new TgaFraction(bytes.AsSpan(474, TgaFraction.Size).ToArray());
            GammaValue = new TgaFraction(bytes.AsSpan(478, TgaFraction.Size).ToArray());
            ColorCorrectionTableOffset = TgaBinary.ReadUInt32(bytes, 482);
            PostageStampOffset = TgaBinary.ReadUInt32(bytes, 486);
            ScanLineOffset = TgaBinary.ReadUInt32(bytes, 490);
            AttributesType = (TgaAttributeType)bytes[494];

            // Everything past the fixed body is kept regardless of the size field, so a stale size never drops data.
            if (bytes.Length > MinSize) OtherDataInExtensionArea = bytes.AsSpan(MinSize, bytes.Length - MinSize).ToArray();

            ScanLineTable = slt;
            this.PostageStampImage = postageStampImage;
            ColorCorrectionTable = cct;
        }

        /// <summary>
        /// Make a new <see cref="TgaExtensionArea"/> with all fields defaulted.
        /// </summary>
        public TgaExtensionArea() { }



        #region Properties
        /// <summary>
        /// Gets the extension area size in bytes: <see cref="MinSize"/> (495) plus the length of
        /// <see cref="OtherDataInExtensionArea"/>. Per the TGA 2.0 spec the value is 495 for a standard
        /// extension area; Truevision reserves any other value for future revisions. Derived, so it
        /// always matches what <see cref="ToBytes"/> writes.
        /// </summary>
        public ushort ExtensionSize => (ushort)(MinSize + (OtherDataInExtensionArea?.Length ?? 0));

        /// <summary>
        /// Author Name - Field 11 (41 Bytes):
        /// Bytes 2-42 - This field is an ASCII field of 41 bytes where the last byte must be a null
        /// (binary zero). This gives a total of 40 ASCII characters for the name. If the field is used,
        /// it should contain the name of the person who created the image (author). If the field is not
        /// used, you may fill it with nulls or a series of blanks(spaces) terminated by a null.
        /// The 41st byte must always be a null.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when set to <see langword="null"/>.</exception>
        public TgaString AuthorName
        {
            get => _authorName;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _authorName = value;
            }
        }

        /// <summary>
        /// Author Comments - Field 12 (324 Bytes):
        /// Bytes 43-366 - This is an ASCII field consisting of 324 bytes which are organized as four lines
        /// of 80 characters, each followed by a null terminator.This field is provided, in addition to the
        /// original IMAGE ID field(in the original TGA format), because it was determined that a few
        /// developers had used the IMAGE ID field for their own purposes.This field gives the developer
        /// four lines of 80 characters each, to use as an Author Comment area. Each line is fixed to 81
        /// bytes which makes access to the four lines easy.Each line must be terminated by a null.
        /// If you do not use all 80 available characters in the line, place the null after the last
        /// character and blank or null fill the rest of the line. The 81st byte of each of the four
        /// lines must be null.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when set to <see langword="null"/>.</exception>
        public TgaComment AuthorComments
        {
            get => _authorComments;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _authorComments = value;
            }
        }

        /// <summary>
        /// Date/Time Stamp - Field 13 (12 Bytes):
        /// Bytes 367-378 - This field contains a series of 6 SHORT values which define the integer
        /// value for the date and time that the image was saved. This data is formatted as follows:
        /// <para>SHORT 0: Month(1 - 12)</para>
        /// <para>SHORT 1: Day(1 - 31)</para>
        /// <para>SHORT 2: Year(4 digit, ie. 1989)</para>
        /// <para>SHORT 3: Hour(0 - 23)</para>
        /// <para>SHORT 4: Minute(0 - 59)</para>
        /// <para>SHORT 5: Second(0 - 59)</para>
        /// Even though operating systems typically time- and date-stamp files, this feature is
        /// provided because the operating system may change the time and date stamp if the file is
        /// copied. By using this area, you are guaranteed an unmodified region for date and time
        /// recording. If the fields are not used, you should fill them with binary zeros (0).
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when set to <see langword="null"/>.</exception>
        public TgaDateTime DateTimeStamp
        {
            get => _dateTimeStamp;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _dateTimeStamp = value;
            }
        }

        /// <summary>
        /// Job Name/ID - Field 14 (41 Bytes):
        /// Bytes 379-419 - This field is an ASCII field of 41 bytes where the last byte must be 
        /// a binary zero. This gives a total of 40 ASCII characters for the job name or the ID.
        /// If the field is used, it should contain a name or id tag which refers to the job with
        /// which the image was associated.This allows production companies (and others) to tie
        /// images with jobs by using this field as a job name (i.e., CITY BANK) or job id number
        /// (i.e., CITY023). If the field is not used, you may fill it with a null terminated series
        /// of blanks (spaces) or nulls. In any case, the 41st byte must be a null.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when set to <see langword="null"/>.</exception>
        public TgaString JobNameOrId
        {
            get => _jobNameOrId;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _jobNameOrId = value;
            }
        }

        /// <summary>
        /// Job Time - Field 15 (6 Bytes):
        /// Bytes 420-425 - This field contains a series of 3 SHORT values which define the integer
        /// value for the job elapsed time when the image was saved.This data is formatted as follows:
        /// <para>SHORT 0: Hours(0 - 65535)</para>
        /// <para>SHORT 1: Minutes(0 - 59)</para>
        /// <para>SHORT 2: Seconds(0 - 59)</para>
        /// The purpose of this field is to allow production houses (and others) to keep a running total
        /// of the amount of time invested in a particular image. This may be useful for billing, costing,
        /// and time estimating. If the fields are not used, you should fill them with binary zeros (0).
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when set to <see langword="null"/>.</exception>
        public TgaTime JobTime
        {
            get => _jobTime;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _jobTime = value;
            }
        }

        /// <summary>
        /// Software ID - Field 16 (41 Bytes):
        /// Bytes 426-466 - This field is an ASCII field of 41 bytes where the last byte must be
        /// a binary zero (null). This gives a total of 40 ASCII characters for the Software ID.
        /// The purpose of this field is to allow software to determine and record with what program
        /// a particular image was created.If the field is not used, you may fill it with a
        /// null terminated series of blanks (spaces) or nulls. The 41st byte must always be a null.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when set to <see langword="null"/>.</exception>
        public TgaString SoftwareId
        {
            get => _softwareId;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _softwareId = value;
            }
        }

        /// <summary>
        /// Software Version - Field 17 (3 Bytes):
        /// Bytes 467-469 - This field consists of two sub-fields, a SHORT and an ASCII BYTE.
        /// The purpose of this field is to define the version of software defined by the
        /// “Software ID” field above. The SHORT contains the version number as a binary
        /// integer times 100.
        /// <para>Therefore, software version 4.17 would be the integer value 417.This allows for
        /// two decimal positions of sub-version.The ASCII BYTE supports developers who also
        /// tag a release letter to the end. For example, if the version number is 1.17b, then
        /// the SHORT would contain 117. and the ASCII BYTE would contain “b”.
        /// The organization is as follows:</para>
        /// <para>SHORT (Bytes 0 - 1): Version Number * 100</para>
        /// <para>BYTE(Byte 2): Version Letter</para>
        /// If you do not use this field, set the SHORT to binary zero, and the BYTE to a space(“ “)
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when set to <see langword="null"/>.</exception>
        public TgaSoftwareVersion SoftwareVersion
        {
            get => _softwareVersion;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _softwareVersion = value;
            }
        }

        /// <summary>
        /// Key Color - Field 18 (4 Bytes):
        /// Bytes 470-473 - This field contains a long value which is the key color in effect at
        /// the time the image is saved. The format is in A:R:G:B where ‘A’ (most significant byte)
        /// is the alpha channel key color(if you don’t have an alpha channel in your application,
        /// keep this byte zero [0]).
        /// <para>The Key Color can be thought of as the ‘background color’ or ‘transparent color’.
        /// This is the color of the ‘non image’ area of the screen, and the same color that the
        /// screen would be cleared to if erased in the application. If you don’t use this field,
        /// set it to all zeros (0). Setting the field to all zeros is the same as selecting a key
        /// color of black.</para>
        /// A good example of a key color is the ‘transparent color’ used in TIPS™ for WINDOW loading/saving.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when set to <see langword="null"/>.</exception>
        public TgaColorKey KeyColor
        {
            get => _keyColor;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _keyColor = value;
            }
        }

        /// <summary>
        /// Pixel Aspect Ratio - Field 19 (4 Bytes):
        /// Bytes 474-477 - This field contains two SHORT sub-fields, which when taken together
        /// specify a pixel size ratio.The format is as follows:
        /// <para>SHORT 0: Pixel Ratio Numerator(pixel width)</para>
        /// <para>SHORT 1: Pixel Ratio Denominator(pixel height)</para>
        /// These sub-fields may be used to determine the aspect ratio of a pixel. This is useful when
        /// it is important to preserve the proper aspect ratio of the saved image. If the two values
        /// are set to the same non-zero value, then the image is composed of square pixels. A zero
        /// in the second sub-field (denominator) indicates that no pixel aspect ratio is specified.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when set to <see langword="null"/>.</exception>
        public TgaFraction PixelAspectRatio
        {
            get => _pixelAspectRatio;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _pixelAspectRatio = value;
            }
        }

        /// <summary>
        /// Gamma Value - Field 20 (4 Bytes):
        /// Bytes 478-481 - This field contains two SHORT sub-fields, which when taken together in a ratio,
        /// provide a fractional gamma value.The format is as follows:
        /// <para>SHORT 0: Gamma Numerator</para>
        /// <para>SHORT 1: Gamma Denominator</para>
        /// The resulting value should be in the range of 0.0 to 10.0, with only one decimal place of
        /// precision necessary. An uncorrected image (an image with no gamma) should have the value 1.0 as
        /// the result.This may be accomplished by placing thesame, non-zero values in both positions
        /// (i.e., 1/1). If you decide to totally ignore this field, please set the denominator (the second
        /// SHORT) to the value zero. This will indicate that the Gamma Value field is not being used.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when set to <see langword="null"/>.</exception>
        public TgaFraction GammaValue
        {
            get => _gammaValue;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _gammaValue = value;
            }
        }

        /// <summary>
        /// Color Correction Offset - Field 21 (4 Bytes):
        /// Bytes 482-485 - This field is a 4-byte field containing a single offset value. This is an offset
        /// from the beginning of the file to the start of the Color Correction table. This table may be
        /// written anywhere between the end of the Image Data field (field 8) and the start of the TGA
        /// File Footer. If the image has no Color Correction Table or if the Gamma Value setting is
        /// sufficient, set this value to zero and do not write a Correction Table anywhere. This is a
        /// derived field: it is computed by <see cref="TargaSharp.IO.TgaWriter"/> during layout (or
        /// read from the file by <see cref="TargaSharp.IO.TgaReader"/>), so consumers cannot set it directly.
        /// </summary>
        public uint ColorCorrectionTableOffset { get; internal set; }

        /// <summary>
        /// Postage Stamp Offset - Field 22 (4 Bytes):
        /// Bytes 486-489 - This field is a 4-byte field containing a single offset value. This is an offset
        /// from the beginning of the file to the start of the Postage Stamp Image. The Postage Stamp Image
        /// must be written after Field 25 (Scan Line Table) but before the start of the TGA File Footer.
        /// If no postage stamp is stored, set this field to the value zero (0). This is a derived field:
        /// it is computed by <see cref="TargaSharp.IO.TgaWriter"/> during layout (or read from the file
        /// by <see cref="TargaSharp.IO.TgaReader"/>), so consumers cannot set it directly.
        /// </summary>
        public uint PostageStampOffset { get; internal set; }

        /// <summary>
        /// Scan Line Offset - Field 23 (4 Bytes):
        /// Bytes 490-493 - This field is a 4-byte field containing a single offset value. This is an
        /// offset from the beginning of the file to the start of the Scan Line Table. This is a derived
        /// field: it is computed by <see cref="TargaSharp.IO.TgaWriter"/> during layout (or read from
        /// the file by <see cref="TargaSharp.IO.TgaReader"/>), so consumers cannot set it directly.
        /// </summary>
        public uint ScanLineOffset { get; internal set; }

        /// <summary>
        /// Attributes Type - Field 24 (1 Byte):
        /// Byte 494 - This single byte field contains a value which specifies the type of Alpha channel
        /// data contained in the file. Value Meaning:
        /// <para>0: no Alpha data included (bits 3-0 of field 5.6 should also be set to zero)</para>
        /// <para>1: undefined data in the Alpha field, can be ignored</para>
        /// <para>2: undefined data in the Alpha field, but should be retained</para>
        /// <para>3: useful Alpha channel data is present</para>
        /// <para>4: pre-multiplied Alpha(see description below)</para>
        /// <para>5 -127: RESERVED</para>
        /// <para>128-255: Un-assigned</para>
        /// <para>Pre-multiplied Alpha Example: Suppose the Alpha channel data is being used to specify the
        /// opacity of each pixel(for use when the image is overlayed on another image), where 0 indicates
        /// that the pixel is completely transparent and a value of 1 indicates that the pixel is
        /// completely opaque(assume all component values have been normalized).</para>
        /// <para>A quadruple(a, r, g, b) of( 0.5, 1, 0, 0) would indicate that the pixel is pure red with a
        /// transparency of one-half. For numerous reasons(including image compositing) is is better to
        /// pre-multiply the individual color components with the value in the Alpha channel.</para>
        /// A pre-multiplication of the above would produce a quadruple(0.5, 0.5, 0, 0).
        /// A value of 3 in the Attributes Type Field(field 23) would indicate that the color components
        /// of the pixel have already been scaled by the value in the Alpha channel.
        /// </summary>
        public TgaAttributeType AttributesType { get; set; }

        /// <summary>
        /// Scan Line Table - Field 25 (Variable):
        /// This information is provided, at the developers’ request, for two purposes:
        /// <para>1) To make random access of compressed images easy.</para>
        /// <para>2) To allow “giant picture” access in smaller “chunks”.</para>
        /// This table should contain a series of 4-byte offsets.Each offset you write should point to the
        /// start of the next scan line, in the order that the image was saved (i.e., top down or bottom up).
        /// The offset should be from the start of the file.Therefore, you will have a four byte value for
        /// each scan line in your image. This means that if your image is 768 pixels tall, you will have 768,
        /// 4-byte offset pointers (for a total of 3072 bytes). This size is not extreme, and thus this table
        /// can be built and maintained in memory, and then written out at the proper time.
        /// </summary>
        public uint[]? ScanLineTable { get; set; }

        /// <summary>
        /// Postage Stamp Image - Field 26 (Variable):
        /// The Postage Stamp area is a smaller representation of the original image. This is useful for
        /// “browsing” a collection of image files. If your application can deal with a postage stamp image,
        /// it is recommended that you create one using sub-sampling techniques to create the best
        /// representation possible. The postage stamp image must be stored in the same format as the normal
        /// image specified in the file, but without any compression. The first byte of the postage stamp
        /// image specifies the X size of the stamp in pixels, the second byte of the stamp image specifies the
        /// Y size of the stamp in pixels. Truevision does not recommend stamps larger than 64x64 pixels, and
        /// suggests that any stamps stored larger be clipped. Obviously, the storage of the postage stamp
        /// relies heavily on the storage of the image. The two images are stored using the same format under
        /// the assumption that if you can read the image, then you can read the postage stamp. If the original
        /// image is color mapped, DO NOT average the postage stamp, as you will create new colors not in your map.
        /// </summary>
        public TgaPostageStampImage? PostageStampImage { get; set; }

        /// <summary>
        /// Color Correction Table - Field 27 (2K Bytes):
        /// The Color Correction Table is a block of 256 x 4 SHORT values, where each set of
        /// four contiguous values are the desired A:R:G:B correction for that entry. This
        /// allows the user to store a correction table for image remapping or LUT driving.
        /// Since each color in the block is a SHORT, the maximum value for a color gun (i.e.,
        /// A, R, G, B) is 65535, and the minimum value is zero.Therefore, BLACK maps to
        /// 0, 0, 0, 0 and WHITE maps to 65535, 65535, 65535, 65535.
        /// </summary>
        public ushort[]? ColorCorrectionTable { get; set; }

        /// <summary>
        /// Other Data In Extension Area (if <see cref="ExtensionSize"/> > 495); at most
        /// <see cref="MaxOtherDataLength"/> bytes so the 2-byte size field stays exact.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when set to more than <see cref="MaxOtherDataLength"/> bytes.</exception>
        public byte[]? OtherDataInExtensionArea
        {
            get => _otherDataInExtensionArea;
            set
            {
                if (value is not null && value.Length > MaxOtherDataLength)
                    throw new ArgumentOutOfRangeException(nameof(value), value.Length, $"OtherDataInExtensionArea must be <= {MaxOtherDataLength} bytes (ExtensionSize is a ushort).");
                _otherDataInExtensionArea = value;
            }
        }

        #endregion

        /// <summary>
        /// Make full copy of <see cref="TgaExtensionArea"/>. Named <c>Copy</c> rather than <c>Clone</c>
        /// because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns>Full independent copy of <see cref="TgaExtensionArea"/>.</returns>
        public TgaExtensionArea Copy() => this with
        {
            AuthorName = AuthorName.Copy(),
            AuthorComments = AuthorComments.Copy(),
            DateTimeStamp = DateTimeStamp.Copy(),
            JobNameOrId = JobNameOrId.Copy(),
            JobTime = JobTime.Copy(),
            SoftwareId = SoftwareId.Copy(),
            SoftwareVersion = SoftwareVersion.Copy(),
            KeyColor = KeyColor.Copy(),
            PixelAspectRatio = PixelAspectRatio.Copy(),
            GammaValue = GammaValue.Copy(),
            ScanLineTable = (uint[]?)ScanLineTable?.Clone(),
            PostageStampImage = PostageStampImage?.Copy(),
            ColorCorrectionTable = (ushort[]?)ColorCorrectionTable?.Clone(),
            OtherDataInExtensionArea = (byte[]?)OtherDataInExtensionArea?.Clone(),
        };

        /// <inheritdoc />
        object ICloneable.Clone() => Copy();

        /// <inheritdoc />
        public bool Equals(TgaExtensionArea? other)
        {
            if (other is null) return false;
            return AuthorName == other.AuthorName &&
                AuthorComments == other.AuthorComments &&
                DateTimeStamp == other.DateTimeStamp &&
                JobNameOrId == other.JobNameOrId &&
                JobTime == other.JobTime &&
                SoftwareId == other.SoftwareId &&
                SoftwareVersion == other.SoftwareVersion &&
                KeyColor == other.KeyColor &&
                PixelAspectRatio == other.PixelAspectRatio &&
                GammaValue == other.GammaValue &&
                ColorCorrectionTableOffset == other.ColorCorrectionTableOffset &&
                PostageStampOffset == other.PostageStampOffset &&
                ScanLineOffset == other.ScanLineOffset &&
                AttributesType == other.AttributesType &&

                TgaArrayEquality.Equals(ScanLineTable, other.ScanLineTable) &&
                PostageStampImage == other.PostageStampImage &&
                TgaArrayEquality.Equals(ColorCorrectionTable, other.ColorCorrectionTable) &&

                TgaArrayEquality.Equals(OtherDataInExtensionArea, other.OtherDataInExtensionArea);
        }


        /// <summary>
        /// Gets a hash code derived from every field of this <see cref="TgaExtensionArea"/>.
        /// </summary>
        /// <returns>A hash code for this <see cref="TgaExtensionArea"/>.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 27;
                hash = (13 * hash) + AuthorName.GetHashCode();
                hash = (13 * hash) + AuthorComments.GetHashCode();
                hash = (13 * hash) + DateTimeStamp.GetHashCode();
                hash = (13 * hash) + JobNameOrId.GetHashCode();
                hash = (13 * hash) + JobTime.GetHashCode();
                hash = (13 * hash) + SoftwareId.GetHashCode();
                hash = (13 * hash) + SoftwareVersion.GetHashCode();
                hash = (13 * hash) + KeyColor.GetHashCode();
                hash = (13 * hash) + PixelAspectRatio.GetHashCode();
                hash = (13 * hash) + GammaValue.GetHashCode();
                hash = (13 * hash) + ColorCorrectionTableOffset.GetHashCode();
                hash = (13 * hash) + PostageStampOffset.GetHashCode();
                hash = (13 * hash) + ScanLineOffset.GetHashCode();
                hash = (13 * hash) + AttributesType.GetHashCode();

                hash = TgaArrayEquality.Hash(hash, ScanLineTable);

                if (PostageStampImage != null)
                    hash = (13 * hash) + PostageStampImage.GetHashCode();

                hash = TgaArrayEquality.Hash(hash, ColorCorrectionTable);

                hash = TgaArrayEquality.Hash(hash, OtherDataInExtensionArea);

                return hash;
            }
        }

        /// <summary>
        /// Convert <see cref="TgaExtensionArea"/> to byte array. Warning: <see cref="ScanLineTable"/>,
        /// <see cref="PostageStampImage"/>, <see cref="ColorCorrectionTable"/> not included,
        /// because they are optional and may not be present in the Extension Area of the TGA file.
        /// </summary>
        /// <returns>Byte array.</returns>
        public byte[] ToBytes()
        {
            return new TgaByteBuilder(MinSize + (OtherDataInExtensionArea?.Length ?? 0))
                .Add(ExtensionSize)
                .Add(AuthorName.ToBytes())
                .Add(AuthorComments.ToBytes())
                .Add(DateTimeStamp.ToBytes())
                .Add(JobNameOrId.ToBytes())
                .Add(JobTime.ToBytes())
                .Add(SoftwareId.ToBytes())
                .Add(SoftwareVersion.ToBytes())
                .Add(KeyColor.ToBytes())
                .Add(PixelAspectRatio.ToBytes())
                .Add(GammaValue.ToBytes())
                .Add(ColorCorrectionTableOffset)
                .Add(PostageStampOffset)
                .Add(ScanLineOffset)
                .Add((byte)AttributesType)
                .Add(OtherDataInExtensionArea)
                .ToArray();
        }
    }
}
