# Fixture Manifest

Every `.tga` fixture under `.tests\Fixtures\`, as loaded by `TgaFile`. Generated for TASK M12 by
loading each file and dumping its header fields (see the task's throwaway `ZZZManifestDump`
test, since deleted) rather than guessed from filenames. **Legacy** fixtures (no v2.0 footer /
extension area) are marked in the Footer column.

| File | Size | Width x Height | ImageType | PixelDepth | ColorMapType (entry size / length) | Origin | Alpha bits | Footer | Exercises |
|---|---|---|---|---|---|---|---|---|---|
| `AAA.tga` | 822 B | 130x2 | RleTrueColor | Bpp24 | NoColorMap | BottomLeft | 0 | Yes | Tiny (130x2) RLE true-color smoke fixture. |
| `AAA_UC.tga` | 824 B | 130x2 | UncompressedTrueColor | Bpp24 | NoColorMap | BottomLeft | 0 | Yes | Uncompressed counterpart of AAA.tga - same image, no RLE. |
| `Alpha Premult.tga` | 624.3 KB | 695x464 | RleTrueColor | Bpp32 | NoColorMap | BottomLeft | 8 | Yes | Large real-world RLE 32bpp image with pre-multiplied alpha (ExtensionArea.AttributesType). |
| `Alpha Straight.tga` | 651.8 KB | 695x464 | RleTrueColor | Bpp32 | NoColorMap | BottomLeft | 8 | Yes | Large real-world RLE 32bpp image with straight (non-premultiplied) alpha. |
| `CBW8.TGA` | 8.6 KB | 128x128 | RleGrayscale | Bpp8 | NoColorMap | BottomLeft | 0 | Yes | RLE 8bpp grayscale with extension area (Truevision conformance suite "C" = compressed). |
| `CCM8.TGA` | 9.1 KB | 128x128 | RleColorMapped | Bpp8 | ColorMap (A1R5G5B5, 256 entries) | BottomLeft | 0 | Yes | RLE 8bpp color-mapped, 256-entry 16-bit (A1R5G5B5) palette, with extension area. |
| `CTC16.TGA` | 14.6 KB | 128x128 | RleTrueColor | Bpp16 | NoColorMap | BottomLeft | 1 | Yes | RLE 16bpp true-color (1 alpha bit) with extension area. |
| `CTC24.TGA` | 20.6 KB | 128x128 | RleTrueColor | Bpp24 | NoColorMap | BottomLeft | 0 | Yes | RLE 24bpp true-color with extension area. |
| `CTC32.TGA` | 26.6 KB | 128x128 | RleTrueColor | Bpp32 | NoColorMap | BottomLeft | 8 | Yes | RLE 32bpp true-color (8 alpha bits) with extension area. |
| `DSCN1910_24bpp_uncompressed_00.tga` | 45.6 MB | 4607x3456 | UncompressedTrueColor | Bpp24 | NoColorMap | BottomLeft | 0 | Yes | Large real-world photo, uncompressed 24bpp, BottomLeft origin. |
| `DSCN1910_24bpp_uncompressed_01.tga` | 45.6 MB | 4607x3456 | UncompressedTrueColor | Bpp24 | NoColorMap | BottomRight | 0 | Yes | Same photo as _00, BottomRight origin (horizontal flip on load). |
| `DSCN1910_24bpp_uncompressed_10.tga` | 45.6 MB | 4607x3456 | UncompressedTrueColor | Bpp24 | NoColorMap | TopLeft | 0 | Yes | Same photo as _00, TopLeft origin (vertical flip on load). |
| `DSCN1910_24bpp_uncompressed_11.tga` | 45.6 MB | 4607x3456 | UncompressedTrueColor | Bpp24 | NoColorMap | TopRight | 0 | Yes | Same photo as _00, TopRight origin (both axes flipped on load). |
| `earth.tga` | 768.0 KB | 512x512 | UncompressedTrueColor | Bpp24 | NoColorMap | BottomLeft | 0 | Yes | Real-world uncompressed 24bpp photo, no alpha. |
| `earth_UTP32.tga` | 1.0 MB | 512x512 | UncompressedTrueColor | Bpp32 | NoColorMap | BottomLeft | 8 | Yes | Same subject as earth.tga, uncompressed 32bpp with 8 alpha bits. |
| `example.tga` | 3.6 MB | 1200x800 | RleTrueColor | Bpp32 | NoColorMap | BottomLeft | 0 | **No (legacy)** | RLE 32bpp image; legacy file (no v2.0 footer/extension area). |
| `ffc.tga` | 732.5 KB | 500x500 | UncompressedTrueColor | Bpp24 | NoColorMap | BottomLeft | 0 | Yes | Real-world uncompressed 24bpp photo. |
| `flag_b16.tga` | 30.0 KB | 124x124 | UncompressedTrueColor | Bpp16 | NoColorMap | BottomLeft | 0 | **No (legacy)** | Legacy (no footer) uncompressed 16bpp, BottomLeft origin. |
| `flag_b24.TGA` | 45.1 KB | 124x124 | UncompressedTrueColor | Bpp24 | NoColorMap | BottomLeft | 0 | **No (legacy)** | Legacy (no footer) uncompressed 24bpp, BottomLeft origin. |
| `flag_b32.TGA` | 60.1 KB | 124x124 | UncompressedTrueColor | Bpp32 | NoColorMap | BottomLeft | 0 | **No (legacy)** | Legacy (no footer) uncompressed 32bpp, BottomLeft origin. |
| `flag_t16.TGA` | 30.0 KB | 124x124 | UncompressedTrueColor | Bpp16 | NoColorMap | TopLeft | 0 | **No (legacy)** | Legacy (no footer) uncompressed 16bpp, TopLeft origin (pairs with flag_b16.tga). |
| `flag_t32.tga` | 60.1 KB | 124x124 | UncompressedTrueColor | Bpp32 | NoColorMap | TopLeft | 0 | **No (legacy)** | Legacy (no footer) uncompressed 32bpp, TopLeft origin (pairs with flag_b32.TGA). |
| `foliage_6.tga` | 68.3 MB | 5184x3456 | UncompressedTrueColor | Bpp32 | NoColorMap | BottomLeft | 8 | Yes | Very large real-world uncompressed 32bpp photo (paired with the RLE version below). |
| `foliage_6_RLE.tga` | 12.1 MB | 5184x3456 | RleTrueColor | Bpp32 | NoColorMap | BottomLeft | 8 | Yes | RLE-compressed version of foliage_6.tga - same pixels, ~6x smaller file. |
| `GIMP Example.tga` | 518.1 KB | 640x400 | RleTrueColor | Bpp32 | NoColorMap | BottomLeft | 8 | Yes | RLE 32bpp image exported by GIMP, with extension area (author/software fields). |
| `MARBLES.TGA` | 4.1 MB | 1419x1001 | UncompressedTrueColor | Bpp24 | NoColorMap | BottomLeft | 0 | Yes | Large real-world uncompressed 24bpp photo (Truevision sample image). |
| `monochrome16_top_left.tga` | 8.0 KB | 64x64 | UncompressedGrayscale | Bpp16 | NoColorMap | TopLeft | 8 | Yes | Uncompressed 16bpp grayscale, TopLeft origin, 8 alpha bits. |
| `monochrome16_top_left_rle.tga` | 5.2 KB | 64x64 | RleGrayscale | Bpp16 | NoColorMap | TopLeft | 8 | Yes | RLE counterpart of monochrome16_top_left.tga. |
| `monochrome8_bottom_left.tga` | 4.0 KB | 64x64 | UncompressedGrayscale | Bpp8 | NoColorMap | BottomLeft | 0 | Yes | Uncompressed 8bpp grayscale, BottomLeft origin. |
| `monochrome8_bottom_left_rle.tga` | 2.9 KB | 64x64 | RleGrayscale | Bpp8 | NoColorMap | BottomLeft | 0 | Yes | RLE counterpart of monochrome8_bottom_left.tga. |
| `PreAlpha_Disabled.tga` | 1.8 MB | 1280x1024 | RleTrueColor | Bpp32 | NoColorMap | BottomLeft | 8 | Yes | Large RLE 32bpp photo, alpha channel present but attributes type marks it not pre-multiplied. |
| `PreAlpha_Enabled.tga` | 1.8 MB | 1280x1024 | RleTrueColor | Bpp32 | NoColorMap | BottomLeft | 8 | Yes | Same photo as PreAlpha_Disabled.tga with pre-multiplied alpha attributes type. |
| `rgb24_bottom_left_rle.tga` | 915 B | 64x64 | RleTrueColor | Bpp24 | NoColorMap | BottomLeft | 0 | Yes | Small RLE 24bpp, BottomLeft origin. |
| `rgb24_top_left.tga` | 12.0 KB | 64x64 | UncompressedTrueColor | Bpp24 | NoColorMap | TopLeft | 0 | Yes | Small uncompressed 24bpp, TopLeft origin. |
| `rgb24_top_left_colormap.tga` | 4.1 KB | 64x64 | UncompressedColorMapped | Bpp8 | ColorMap (R8G8B8, 29 entries) | BottomLeft | 0 | **No (legacy)** | Uncompressed 8bpp color-mapped, 29-entry 24-bit (R8G8B8) palette; legacy (no footer). |
| `rgb32_bottom_left.tga` | 16.0 KB | 64x64 | UncompressedTrueColor | Bpp32 | NoColorMap | BottomLeft | 8 | Yes | Small uncompressed 32bpp, BottomLeft origin, 8 alpha bits. |
| `rgb32_top_left_rle.tga` | 1.6 KB | 64x64 | RleTrueColor | Bpp32 | NoColorMap | TopLeft | 8 | Yes | Small RLE 32bpp, TopLeft origin, 8 alpha bits. |
| `rgb32_top_left_rle_colormap.tga` | 861 B | 64x64 | RleColorMapped | Bpp8 | ColorMap (A8R8G8B8, 59 entries) | TopLeft | 0 | Yes | RLE 8bpp color-mapped, 59-entry 32-bit (A8R8G8B8) palette, TopLeft origin. |
| `shuttle.tga` | 106.0 KB | 640x480 | RleTrueColor | Bpp24 | NoColorMap | BottomLeft | 0 | **No (legacy)** | Real-world RLE 24bpp photo; legacy (no footer). |
| `Test_24.tga` | 92 B | 4x4 | UncompressedTrueColor | Bpp24 | NoColorMap | BottomLeft | 0 | Yes | Minimal 4x4 uncompressed 24bpp smoke fixture. |
| `Test_24_RLE.tga` | 100 B | 4x4 | RleTrueColor | Bpp24 | NoColorMap | BottomLeft | 0 | Yes | RLE counterpart of Test_24.tga. |
| `Tree_NoAlphaBits.tga` | 181.1 KB | 253x253 | RleTrueColor | Bpp32 | NoColorMap | TopLeft | 0 | **No (legacy)** | RLE 32bpp with 0 declared alpha bits despite the 32bpp depth (real-world quirk); legacy (no footer). |
| `UBW8.TGA` | 20.6 KB | 128x128 | UncompressedGrayscale | Bpp8 | NoColorMap | BottomLeft | 0 | Yes | Uncompressed 8bpp grayscale with extension area (Truevision conformance suite "U" = uncompressed). |
| `UCM8.TGA` | 21.1 KB | 128x128 | UncompressedColorMapped | Bpp8 | ColorMap (A1R5G5B5, 256 entries) | BottomLeft | 0 | Yes | Uncompressed 8bpp color-mapped, 256-entry 16-bit (A1R5G5B5) palette, with extension area. |
| `UTC16.TGA` | 40.6 KB | 128x128 | UncompressedTrueColor | Bpp16 | NoColorMap | BottomLeft | 1 | Yes | Uncompressed 16bpp true-color (1 alpha bit) with extension area. |
| `UTC24.TGA` | 60.6 KB | 128x128 | UncompressedTrueColor | Bpp24 | NoColorMap | BottomLeft | 0 | Yes | Uncompressed 24bpp true-color with extension area. |
| `UTC32.TGA` | 80.6 KB | 128x128 | UncompressedTrueColor | Bpp32 | NoColorMap | BottomLeft | 8 | Yes | Uncompressed 32bpp true-color (8 alpha bits) with extension area. |
| `xing_b16.tga` | 76.9 KB | 240x164 | UncompressedTrueColor | Bpp16 | NoColorMap | BottomLeft | 0 | **No (legacy)** | Legacy (no footer) uncompressed 16bpp, BottomLeft origin. |
| `xing_b24.tga` | 115.3 KB | 240x164 | UncompressedTrueColor | Bpp24 | NoColorMap | BottomLeft | 0 | **No (legacy)** | Legacy (no footer) uncompressed 24bpp, BottomLeft origin. |
| `xing_b32.TGA` | 153.8 KB | 240x164 | UncompressedTrueColor | Bpp32 | NoColorMap | BottomLeft | 0 | **No (legacy)** | Legacy (no footer) uncompressed 32bpp, BottomLeft origin. |
| `xing_t16.tga` | 76.9 KB | 240x164 | UncompressedTrueColor | Bpp16 | NoColorMap | TopLeft | 0 | **No (legacy)** | Legacy (no footer) uncompressed 16bpp, TopLeft origin (pairs with xing_b16.tga). |
| `xing_t24.tga` | 115.3 KB | 240x164 | UncompressedTrueColor | Bpp24 | NoColorMap | TopLeft | 0 | **No (legacy)** | Legacy (no footer) uncompressed 24bpp, TopLeft origin (pairs with xing_b24.tga). |
| `xing_t32.TGA` | 153.8 KB | 240x164 | UncompressedTrueColor | Bpp32 | NoColorMap | TopLeft | 0 | **No (legacy)** | Legacy (no footer) uncompressed 32bpp, TopLeft origin (pairs with xing_b32.TGA). |
| `generated_devarea_2entries.tga` | 580 B | 2x2 | UncompressedTrueColor | Bpp24 | NoColorMap | BottomLeft | 0 | Yes | Generated (TASK M12): 2x2 24bpp with a DeveloperArea of 2 entries (tags 100, 200; distinct payloads) - the only fixture exercising the developer directory round trip end-to-end. |
| `generated_rle_15bit_bottomright.tga` | 566 B | 8x2 | RleTrueColor | Bpp16 | NoColorMap | BottomRight | 0 | Yes | Generated (TASK M12): 8x2 16bpp RleTrueColor, 0 alpha bits, BottomRight origin, with both RLE run-length and raw packets in one image - no prior fixture combined 15/16-bit RLE with a non-default origin. |

## Legacy (no footer) fixtures (15)

These predate the TGA 2.0 footer/extension area and are the ones exercising the reader's
footer-less fallback path:

- `example.tga`
- `flag_b16.tga`
- `flag_b24.TGA`
- `flag_b32.TGA`
- `flag_t16.TGA`
- `flag_t32.tga`
- `rgb24_top_left_colormap.tga`
- `shuttle.tga`
- `Tree_NoAlphaBits.tga`
- `xing_b16.tga`
- `xing_b24.tga`
- `xing_b32.TGA`
- `xing_t16.tga`
- `xing_t24.tga`
- `xing_t32.TGA`
