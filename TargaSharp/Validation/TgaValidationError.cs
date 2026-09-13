namespace TargaSharp.Validation
{
    /// <summary>
    /// One semantic validation failure reported by <see cref="ITgaValidator"/>.
    /// </summary>
    /// <param name="Path">Dotted member path to the offending field, e.g.
    /// <c>"Header.ImageSpec.PixelDepth"</c> or <c>"ExtensionArea.DateTimeStamp.Month"</c>.</param>
    /// <param name="Message">Human-readable description of why the field is invalid.</param>
    public sealed record TgaValidationError(string Path, string Message);
}
