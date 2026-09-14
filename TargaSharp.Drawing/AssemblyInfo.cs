using System.Runtime.CompilerServices;
// .NET Framework is Windows-only and has no SupportedOSPlatformAttribute.
#if !NETFRAMEWORK
using System.Runtime.Versioning;
#endif

[assembly: InternalsVisibleTo("TargaSharp.Drawing.Tests")]

#if !NETFRAMEWORK
// This assembly is a thin GDI+ (System.Drawing) bridge for the cross-platform TargaSharp core, so
// every public API in it is Windows-only. Marking this here (rather than per-member) means new
// public types/members automatically inherit the platform requirement without individual attributes.
[assembly: SupportedOSPlatform("windows")]
#endif
