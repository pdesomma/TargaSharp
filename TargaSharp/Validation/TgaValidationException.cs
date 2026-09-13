namespace TargaSharp.Validation
{
    /// <summary>
    /// Thrown by <see cref="TargaSharp.IO.TgaWriter"/> when a <see cref="TgaFile"/> fails
    /// <see cref="ITgaValidator"/> validation and therefore cannot be written.
    /// </summary>
    public sealed class TgaValidationException : Exception
    {
        /// <summary>
        /// Maximum number of error paths quoted in <see cref="Exception.Message"/> before the
        /// remainder is summarized as a count instead of being listed out in full.
        /// </summary>
        private const int MaxPathsInMessage = 5;

        /// <summary>
        /// Make a <see cref="TgaValidationException"/> from the errors reported by <see cref="ITgaValidator.Validate"/>.
        /// </summary>
        /// <param name="errors">The validation errors that failed the write. Must not be null or empty.</param>
        /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="errors"/> is empty.</exception>
        public TgaValidationException(IReadOnlyList<TgaValidationError> errors) : base(BuildMessage(errors))
        {
            Errors = errors;
        }

        /// <summary>
        /// Gets the validation errors that caused the write to be refused.
        /// </summary>
        public IReadOnlyList<TgaValidationError> Errors { get; }

        /// <summary>
        /// Builds the exception message: the total error count followed by the first
        /// <see cref="MaxPathsInMessage"/> offending paths.
        /// </summary>
        /// <param name="errors">The validation errors to summarize.</param>
        /// <returns>A message of the form <c>"TgaFile failed validation with N error(s): PathA, PathB, ... (and M more)"</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="errors"/> is empty.</exception>
        private static string BuildMessage(IReadOnlyList<TgaValidationError> errors)
        {
            ArgumentNullException.ThrowIfNull(errors);
            if (errors.Count == 0)
                throw new ArgumentException("errors must not be empty.", nameof(errors));

            var shown = errors.Take(MaxPathsInMessage).Select(e => e.Path);
            string paths = string.Join(", ", shown);
            int remaining = errors.Count - MaxPathsInMessage;

            return remaining > 0
                ? $"TgaFile failed validation with {errors.Count} error(s): {paths} (and {remaining} more)."
                : $"TgaFile failed validation with {errors.Count} error(s): {paths}.";
        }
    }
}
