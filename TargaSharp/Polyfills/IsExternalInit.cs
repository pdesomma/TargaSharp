#if NETFRAMEWORK
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Marker the compiler requires for <c>init</c> accessors and records; absent from .NET Framework.
    /// </summary>
    internal static class IsExternalInit { }
}
#endif
