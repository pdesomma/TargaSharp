using System.Runtime.Versioning;

// This assembly is a thin GDI+ (System.Drawing) bridge for the cross-platform TargaSharp core, so
// every public API in it is Windows-only. Marking this here (rather than per-member) means new
// public types/members automatically inherit the platform requirement without individual attributes.
[assembly: SupportedOSPlatform("windows")]
