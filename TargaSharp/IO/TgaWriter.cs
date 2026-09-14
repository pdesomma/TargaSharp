using TargaSharp.Validation;

namespace TargaSharp.IO
{
    /// <summary>
    /// Default <see cref="ITgaWriter"/> implementation. Validates the <see cref="TgaFile"/> (see
    /// <see cref="ITgaValidator"/>), computes the on-disk layout (see <see cref="TgaLayoutPlanner"/>),
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
        /// Computes section order, sizes and offsets for a <see cref="TgaFile"/>.
        /// </summary>
        private readonly ITgaLayoutPlanner _planner;

        /// <summary>
        /// Make a <see cref="TgaWriter"/> that validates with a new <see cref="TgaValidator"/>.
        /// </summary>
        public TgaWriter() : this(new TgaValidator()) { }

        /// <summary>
        /// Make a <see cref="TgaWriter"/> that validates with <paramref name="validator"/>.
        /// </summary>
        /// <param name="validator">The validator to run against a <see cref="TgaFile"/> before it is written.</param>
        /// <exception cref="ArgumentNullException"><paramref name="validator"/> is <see langword="null"/>.</exception>
        public TgaWriter(ITgaValidator validator) : this(validator, new TgaLayoutPlanner()) { }

        /// <summary>
        /// Make a <see cref="TgaWriter"/> with an explicit validator and layout planner.
        /// </summary>
        /// <param name="validator">The validator to run against a <see cref="TgaFile"/> before it is written.</param>
        /// <param name="planner">Computes the on-disk layout of a validated <see cref="TgaFile"/>.</param>
        /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
        internal TgaWriter(ITgaValidator validator, ITgaLayoutPlanner planner)
        {
            ArgumentNullException.ThrowIfNull(validator);
            ArgumentNullException.ThrowIfNull(planner);
            _validator = validator;
            _planner = planner;
        }

        /// <inheritdoc />
        public void Write(TgaFile file, Stream stream)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(stream);
            if (!stream.CanWrite)
                throw new ArgumentException("Stream must be writable.", nameof(stream));

            IReadOnlyList<TgaValidationError> errors = _validator.Validate(file);
            if (errors.Count > 0)
                throw new TgaValidationException(errors);

            // The plan is the single source of truth for section order and offsets; writing is just a dump.
            TgaLayout layout = _planner.Plan(file);
            foreach (TgaSection section in layout.Sections)
                stream.Write(section.Bytes, 0, section.Bytes.Length);

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

            // Plan + validate against a buffer first so a failure never leaves a truncated file behind.
            byte[] bytes = Write(file);
            File.WriteAllBytes(path, bytes);
        }
    }
}
