# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Fixed
- `TgaDrawing.FromBitmap` crashed the process (fatal CLR error) on 1bpp/4bpp indexed bitmaps and produced invalid files for 48/64bpp; those formats now throw `NotSupportedException` up front.
- `Width * Height * bytes-per-pixel` overflowed `int` in `TgaReader` and `TgaValidator` (32768x32768x32bpp wrapped to 0, so a header with no pixel bytes loaded and validated clean); now sized in `long`.
- `TgaReader` allocated header-declared sizes (image data, RLE output, developer fields) before checking them against the stream, so a few hundred bytes could force a multi-GB allocation and `OutOfMemoryException`; declared sizes are now checked against the remaining stream first and rejected with `TgaFormatException`.
- An RLE stream that ended inside a run packet leaked `IndexOutOfRangeException` from the reader; truncated and overrunning RLE packets now raise `TgaFormatException`.
- `TgaValidator` threw `NullReferenceException` on a `null` element in `DeveloperArea.Entries`; it is now reported as a `TgaValidationError`.

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
