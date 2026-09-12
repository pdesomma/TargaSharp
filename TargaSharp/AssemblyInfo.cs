using System.Runtime.CompilerServices;

// Exposes internal types (e.g. TargaSharp.IO.RleCodec) to the test assembly so they can be
// unit-tested directly instead of only through TgaFile's public surface.
[assembly: InternalsVisibleTo("TargaSharp.Tests")]
