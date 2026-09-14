# TargaSharp

[![CI](https://github.com/pdesomma/TargaSharp/actions/workflows/ci.yml/badge.svg)](https://github.com/pdesomma/TargaSharp/actions/workflows/ci.yml)

A .NET library for reading, editing and writing Truevision TGA (`.tga`) image files, covering the full [TGA 2.0 specification](.doc/tga_specs.pdf): header, color map, raw and run-length-encoded image data, developer area, extension area (scan-line table, postage stamp, color-correction table) and footer.

Derived from [TGASharpLib](https://gitlab.com/Alex_Green/TGASharpLib) by Alex Green.

## Packages

| Package | Targets | Purpose |
|---|---|---|
| `TargaSharp` | net48, net6.0, net8.0, net10.0 | Core: file model, reader, writer, validation. No native or OS-specific dependencies. |
| `TargaSharp.Drawing` | net48, net6.0, net8.0, net10.0 (Windows) | `System.Drawing.Bitmap` ↔ `TgaFile` conversion via GDI+. |

```
dotnet add package TargaSharp
dotnet add package TargaSharp.Drawing   # optional, Windows only
```

## Usage

### Read, edit, write

```csharp
using TargaSharp;

var tga = new TgaFile("input.tga");          // or new TgaFile(byte[]) / new TgaFile(Stream)

ushort width  = tga.Width;
byte[] pixels = tga.ImageArea.ImageData;      // decoded, BGR(A) byte order, row-major
var origin    = tga.Header.ImageSpec.ImageDescriptor.ImageOrigin;

tga.ExtensionArea!.AuthorName = new TgaString("me", 41, true);
tga.Flip(vertical: true);                     // toggles the origin bit; pixel data is untouched
tga.ToOldFormat();                            // drop footer/extension/developer areas -> plain TGA 1.0; ToNewFormat() is the inverse

tga.Save("output.tga");                       // or tga.Save(Stream) / byte[] bytes = tga.ToBytes()
```

### Create from scratch

```csharp
var tga = new TgaFile(256, 256, TgaPixelDepth.Bpp32, TgaImageType.RleTrueColor, attrBits: 8);
FillPixels(tga.ImageArea.ImageData);          // 256 * 256 * 4 bytes, B G R A per pixel
tga.Save("new.tga");
```

### Validate before writing

```csharp
foreach (TgaValidationError e in tga.Validate())
    Console.WriteLine($"{e.Path}: {e.Message}");
```

`Save`/`ToBytes` run the same validation and throw `TgaValidationException` (with the full error list) rather than writing an inconsistent file. Reading is lenient: files with out-of-spec metadata still load; only malformed structure raises `TgaFormatException`.

### Reader / writer interfaces

```csharp
using TargaSharp.IO;

ITgaReader reader = new TgaReader();
TgaFile tga = reader.Read(stream);

ITgaWriter writer = new TgaWriter(new TgaValidator());   // validator is injectable
writer.Write(tga, stream);                               // any writable stream, seeking not required
```

### Bitmaps (TargaSharp.Drawing)

```csharp
using TargaSharp.Drawing;

TgaFile tga = TgaDrawing.FromBitmap(bitmap, useRle: true);
using Bitmap bmp = tga.ToBitmap();
using Bitmap? thumb = tga.GetPostageStampBitmap();
```

## Supported formats

| Image type | Uncompressed | RLE |
|---|---|---|
| Color-mapped (8-bit index, 15/16/24/32-bit palette) | ✓ | ✓ |
| True-color 16 (A1R5G5B5), 24, 32 bpp | ✓ | ✓ |
| Grayscale 8 bpp, 16 bpp (8 + 8 alpha) | ✓ | ✓ |

All four image origins (bottom-left, bottom-right, top-left, top-right), legacy TGA 1.0 files without a footer, and every TGA 2.0 extension field are read and written.

## Errors

| Exception | When |
|---|---|
| `TgaFormatException` | Input is not a well-formed TGA (truncated, sizes don't add up) |
| `TgaValidationException` | The in-memory file violates the spec and cannot be written; `.Errors` lists every violation |
| `ArgumentException` / `ArgumentOutOfRangeException` | A field setter was given an out-of-range value (ASCII-only strings, 4×80 comment lines, alpha bits ≤ 15, …) |
| `NotSupportedException` | `TgaDrawing.FromBitmap` given a `PixelFormat` it cannot convert |

Both TGA exceptions derive from `TgaException`.

## Building

```
dotnet build TargaSharp.sln
dotnet test TargaSharp.sln
```

Requires the .NET 10 SDK (`global.json`). `dotnet build -c Release` also produces the NuGet packages.

## License

MIT — see [LICENSE](LICENSE).
