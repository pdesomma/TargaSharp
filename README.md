# TargaSharp

## Description
TargaSharp is a free, open source .NET library for reading and writing .tga files based on the excellent TGASharpLib by Alex Green. (https://gitlab.com/Alex_Green/TGASharpLib)

## Packages

### TargaSharp (core, cross-platform)
Reads and writes `.tga` files as plain bytes — no OS-specific dependencies.
```csharp
using TargaSharp;

var tga = new TgaFile(@"C:\images\example.tga");
tga.Save(@"C:\images\example_copy.tga");
```

### TargaSharp.Drawing (Windows, Bitmap bridge)
Converts between `TgaFile` and GDI+ `System.Drawing.Bitmap`. Requires Windows (`System.Drawing.Common`).
```csharp
using TargaSharp.Drawing;

var tga = TgaDrawing.FromBitmap(myBitmap);
using System.Drawing.Bitmap roundTripped = tga.ToBitmap();
```

## License:
**MIT**
