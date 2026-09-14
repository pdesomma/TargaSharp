# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Breaking Changes
- `TgaDateTime.ToDateTime()` now returns `DateTime?` and yields `null` for the spec's all-zero "not set" value instead of throwing `ArgumentOutOfRangeException`.
- `new TgaFile(width, height, ...)` throws `ArgumentOutOfRangeException` for a zero dimension or `TgaPixelDepth.Other` instead of silently producing a 0x0 `NoImageData` file; use `new TgaFile()` + `ToNewFormat()` for an empty v2.0 file.
- `new TgaTime(int, int, int)` and `new TgaTime(TimeSpan)` throw `ArgumentOutOfRangeException` for out-of-range values instead of silently narrowing (`new TgaTime(70000, 0, 0).Hours` was 4464).
- `TgaImageType.IsRunLengthEncoded()` is true only for spec values 9-11; reserved values with bit 3 set (25, 27, 41, ...) no longer decode as RLE.

### Added
- `TgaValidator` rules: non-zero width/height for image types with pixel data (previously passed validation and failed inside `Save`), unknown `ColorMapType` values, `AttributesType = NoAlpha` with non-zero descriptor attribute bits (spec Field 24), `OtherDataInExtensionArea` too large for the 2-byte Extension Size, more than 65535 developer entries, and a 0x0 postage stamp.

### Fixed
- `TgaReader` read color-map bytes whenever `ColorMapLength > 0`, even with `ColorMapType = NoColorMap`, shifting the image data of files with a stale color-map spec; the color map is now gated on `ColorMapType` as the spec requires (and as the writer already did).
- `TgaReader` on an RLE image with a zero dimension always consumed one packet, eating the first footer/extension byte or failing at end of stream; a zero-length decode now reads nothing.
- `Save`/`ToBytes` shortened a padded image ID (e.g. `IdLength` 6 for `"abc"`) to its text length, so files did not round-trip byte-for-byte; the field's `Length` is now written as-is.
- `TgaLayoutPlanner` sized image data in `int`, so a header of 32768x32768x32bpp with empty `ImageData` planned as a valid file when validation was bypassed; now sized in `long`.
- `new TgaFile(65535, 65535, Bpp32)` overflowed the `int` buffer size and threw `OverflowException`; the size is now computed in `long` and rejected with `ArgumentOutOfRangeException`.
- `new TgaDeveloperEntry(byte[])` with a field size above `int.MaxValue` threw `OverflowException` from the placeholder allocation; now `ArgumentOutOfRangeException`.
- `new TgaComment(byte[])` lost a space `BlankSpaceChar`, so a space-padded comment re-serialized NUL-padded and compared unequal to its source.
- `TgaExtensionArea.ToBytes()` stamped `DateTime.UtcNow` into a null `DateTimeStamp`; it now serializes the spec's "not set" value (the writer still stamps an unset timestamp at save time).
- `TgaDeveloperArea.ToBytes()` threw a bare `Exception` for a null `Entries` list; now `InvalidOperationException`. `TgaDeveloperArea`, `TgaPostageStampImage` and `TgaString` null-argument exceptions carried a bogus `ParamName`.
- `new TgaString(Array.Empty<byte>(), useEnding: true)` threw an `ArgumentOutOfRangeException` naming `count`; it now names `bytes`.
- `ToBitmap()` copied `ImageData` into the GDI+ buffer unchecked: an oversized array corrupted the heap and killed the process, an undersized one left rows uninitialized; the length is now verified up front (`InvalidOperationException`) and rows are copied by GDI+'s own stride.
- `TgaDrawing.FromBitmap` on an 8bpp bitmap with an all-gray palette produced `ImageType` Grayscale *plus* a color map, which `Save` rejects; the identity gray ramp now yields a grayscale image with no color map and any other palette a color-mapped image.
- `ToBitmap()` ignored `ColorMapSpec.FirstEntryIndex`, so palettes not starting at 0 mapped every pixel to the wrong color.
- `ToBitmap(forceUseAlpha: true)` read bit 15 of `X1R5G5B5` palette entries (always written as 0) as alpha, making every entry transparent; 15-bit entries are now always opaque.
- Palette alpha (`A1R5G5B5`/`A8R8G8B8` entries) was dropped by a plain `ToBitmap()` because only the descriptor's attribute bits were consulted; `FromBitmap` now marks such files `UsefulAlpha` and `ToBitmap` keys palette alpha on the entry size.
- `GetPostageStampBitmap()` returned a zero-filled bitmap for a stamp whose `Data` was shorter than declared; it now returns `null`.
- `TgaDrawing.FromBitmap` silently truncated bitmaps wider or taller than 65535 (now `ArgumentOutOfRangeException`) and assumed GDI+'s stride equals the padded row width (rows are now copied by the reported stride).
- `ToBitmap()` on an unsupported pixel depth or color-map entry size failed with GDI+'s "Parameter is not valid" or silently kept the default halftone palette; both now throw `NotSupportedException`. A key color on a 16bpp grayscale image no longer throws from `MakeTransparent`.
- 5-bit palette channels are expanded by bit replication (`v << 3 | v >> 2`) instead of float scaling.
- `TgaDrawing.FromBitmap` crashed the process (fatal CLR error) on 1bpp/4bpp indexed bitmaps and produced invalid files for 48/64bpp; those formats now throw `NotSupportedException` up front.
- `Width * Height * bytes-per-pixel` overflowed `int` in `TgaReader` and `TgaValidator` (32768x32768x32bpp wrapped to 0, so a header with no pixel bytes loaded and validated clean); now sized in `long`.
- `TgaReader` allocated header-declared sizes (image data, RLE output, developer fields) before checking them against the stream, so a few hundred bytes could force a multi-GB allocation and `OutOfMemoryException`; declared sizes are now checked against the remaining stream first and rejected with `TgaFormatException`.
- An RLE stream that ended inside a run packet leaked `IndexOutOfRangeException` from the reader; truncated and overrunning RLE packets now raise `TgaFormatException`.
- `TgaValidator` limited the image ID by text length; it now checks the field's full `Length` (padding included), matching what is written.
- `TgaValidator` threw `NullReferenceException` on a `null` element in `DeveloperArea.Entries`; it is now reported as a `TgaValidationError`.
- `Save`/`ToBytes` removed empty entries from and re-sorted the caller's `DeveloperArea.Entries` list; the file is still written tag-ordered without empty entries, but the in-memory list is no longer modified.
- `ToBitmap()` on a file with `NoImageData`, a zero dimension or `null` `ImageData` failed with GDI+'s opaque "Parameter is not valid" (or a `NullReferenceException`); it now throws `InvalidOperationException` saying why.
- `TgaReader` silently accepted a truncated image ID, color map, extension area, developer field or postage stamp (only image data was length-checked); every variable-length section now fails with `TgaFormatException` when the file is shorter than its declared size.
- `new TgaSoftwareVersion(string)` silently produced version `000` (and dropped the letter) when the first three characters were not digits; it now throws `FormatException`.
- `TargaSharp.Drawing` palette codec: 15/16-bit color-map entries were written with R and B in each other's bit fields (palettes round-tripped with red and blue swapped), 32-bit entries returned alpha 255 exactly when alpha was requested, and both paths used host-endian `BitConverter` where the core uses explicit little-endian.

## [0.2.0]

### Breaking Changes
- `TGA` renamed to `TgaFile`.
- Component `Clone()` methods renamed to `Copy()` (records reserve `Clone` for the compiler-synthesized copy constructor); `ICloneable.Clone()` remains as an explicit interface implementation.
- `GetInfo` removed.
- `TgaFile.Size` removed.
- `Save` now returns `void` and throws on failure instead of returning a success flag; `ToBytes` no longer returns `null`.
- `CheckAndUpdateOffsets` removed; replaced by `TgaFile.Validate()` (via `ITgaValidator`/`TgaValidator`), which reports every rule violation instead of failing on the first one.
- Bitmap interop moved out of the core library into the new `TargaSharp.Drawing` package: `TgaDrawing.FromBitmap` (was a `TgaFile` constructor/factory) and `ToBitmap()` (extension method).
- `TgaComment.OriginalString` renamed to `Lines` (now an `IReadOnlyList<string>` of the 4 comment lines instead of a single joined string).
- `TgaFooter` constants and `IsFooterCorrect` removed; replaced by `TgaFooter.TryParse`.
- Derived/computed fields (e.g. offsets and lengths written by `TgaWriter`) are now read-only from consumer code.
- Placeholder `_Reserved_N`-style members removed from `TgaImageType`, `TgaColorMapType` and `TgaAttributeType`; use the `IsKnown()`/`IsTruevisionReserved()`/`IsRunLengthEncoded()` extensions instead.
- `BitConverterHelper` removed (was public); serialization is now internal.
- Public naming sweep (types, members, parameters and locals):
  - `TgaImgOrColMap` → `TgaImageArea`; `TgaFile.ImageOrColorMapArea` → `ImageArea`.
  - `TgaExtArea` → `TgaExtensionArea`; `TgaFile.ExtArea` → `ExtensionArea`.
  - `TgaDevArea` → `TgaDeveloperArea`; `TgaFile.DevArea` → `DeveloperArea`.
  - `TgaDevEntry` → `TgaDeveloperEntry`.
  - `TgaSoftVersion` → `TgaSoftwareVersion`; `TgaExtensionArea.SoftVersion` → `SoftwareVersion`.
  - `ImageID` → `ImageId`; `JobNameOrID` → `JobNameOrId`; `SoftwareID` → `SoftwareId`.
  - `TgaString.XFileSignatute` → `XFileSignature`; `XFileSignatuteConst` → `XFileSignatureText`.
  - `TgaFraction.AspectRatio` → `Value`.
  - `TgaImageType` members: `Uncompressed_ColorMapped` → `UncompressedColorMapped`, `Uncompressed_TrueColor` → `UncompressedTrueColor`, `Uncompressed_BlackWhite` → `UncompressedGrayscale`, `RLE_ColorMapped` → `RleColorMapped`, `RLE_TrueColor` → `RleTrueColor`, `RLE_BlackWhite` → `RleGrayscale`.
  - Assorted parameter and local variable renames to camelCase throughout `TargaSharp` and `TargaSharp.Drawing` (no public API impact beyond named-argument call sites).

### Added
- `TgaValidator` (`ITgaValidator`) for spec value-range and cross-field validation, surfaced via `TgaFile.Validate()`.
- Typed exceptions (e.g. `TgaFormatException`, `TgaValidationException`) instead of silently swallowed errors.
- `TargaSharp.Drawing` package, split out of the core library, for `System.Drawing` (GDI+) interop.

### Changed
- Core model types are now records with enforced spec invariants; several fields that used to be freely settable are now derived and read-only.

## [0.1.0]

First release.
