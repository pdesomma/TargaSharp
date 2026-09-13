namespace TargaSharp.Validation
{
    /// <summary>
    /// Validates a <see cref="TgaFile"/> against the TGA spec's semantic rules (value ranges and
    /// cross-field consistency) beyond what <see cref="TargaSharp.IO.TgaReader"/> and
    /// <see cref="TargaSharp.IO.TgaWriter"/> already enforce structurally.
    /// </summary>
    public interface ITgaValidator
    {
        /// <summary>
        /// Validates <paramref name="file"/> and returns every rule violation found.
        /// </summary>
        /// <param name="file">The <see cref="TgaFile"/> to validate.</param>
        /// <returns>The violations found, or an empty list when <paramref name="file"/> is valid.</returns>
        IReadOnlyList<TgaValidationError> Validate(TgaFile file);
    }
}
