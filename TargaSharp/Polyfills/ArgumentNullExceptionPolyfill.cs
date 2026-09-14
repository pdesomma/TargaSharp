#if NETFRAMEWORK
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// Supplies <c>ArgumentNullException.ThrowIfNull</c> (added in .NET 6) to .NET Framework as a
    /// static extension member, so call sites compile unchanged on every target.
    /// </summary>
    internal static class ArgumentNullExceptionPolyfill
    {
        extension(ArgumentNullException)
        {
            /// <summary>
            /// Throws <see cref="ArgumentNullException"/> when <paramref name="argument"/> is <see langword="null"/>.
            /// </summary>
            /// <param name="argument">Value to check.</param>
            /// <param name="paramName">Name of the parameter; filled in by the compiler from the argument expression.</param>
            public static void ThrowIfNull(object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
            {
                if (argument is null)
                    throw new ArgumentNullException(paramName);
            }
        }
    }
}
#endif
