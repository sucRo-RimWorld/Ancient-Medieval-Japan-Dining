from __future__ import annotations

import argparse
from pathlib import Path
import struct
import sys
import zlib

ROOT = Path(__file__).resolve().parents[1]
PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"


class PngValidationError(ValueError):
    pass


def _error(path: Path, message: str) -> PngValidationError:
    return PngValidationError(f"{message}: {path}")


def validate_png(path: Path) -> None:
    """Fully validate an AMJ production PNG, including compressed image bytes."""
    png = path.read_bytes()
    if len(png) < 33 or png[:8] != PNG_SIGNATURE:
        raise _error(path, "invalid PNG signature/header")

    offset = 8
    compressed = bytearray()
    ended = False
    width = height = depth = color = compression = filtering = interlace = None
    chunk_index = 0

    while offset < len(png):
        if offset + 12 > len(png):
            raise _error(path, f"truncated PNG chunk header at byte {offset}")

        length = int.from_bytes(png[offset:offset + 4], "big")
        kind = png[offset + 4:offset + 8]
        end = offset + 12 + length
        if end > len(png):
            name = kind.decode("ascii", errors="replace")
            raise _error(path, f"truncated {name} chunk at byte {offset} ({length} declared data bytes)")

        data = png[offset + 8:end - 4]
        crc = int.from_bytes(png[end - 4:end], "big")
        if zlib.crc32(kind + data) & 0xFFFFFFFF != crc:
            raise _error(path, f"invalid PNG CRC for {kind!r}")

        if chunk_index == 0 and kind != b"IHDR":
            raise _error(path, "IHDR is not the first PNG chunk")

        if kind == b"IHDR":
            if chunk_index != 0 or length != 13:
                raise _error(path, "invalid or duplicate IHDR")
            width, height, depth, color, compression, filtering, interlace = struct.unpack(">IIBBBBB", data)
            if width <= 0 or height <= 0:
                raise _error(path, "invalid PNG dimensions")
            if depth != 8 or color not in (3, 6) or (compression, filtering, interlace) != (0, 0, 0):
                raise _error(path, "unexpected AMJ PNG encoding")
        elif kind == b"IDAT":
            compressed.extend(data)
        elif kind == b"IEND":
            if length != 0 or end != len(png):
                raise _error(path, "invalid PNG end or trailing bytes after IEND")
            ended = True

        offset = end
        chunk_index += 1

    if not ended or not compressed:
        raise _error(path, "missing PNG image data or IEND")

    try:
        decoder = zlib.decompressobj()
        pixels = decoder.decompress(compressed) + decoder.flush()
    except zlib.error as exc:
        raise _error(path, f"PNG IDAT decompression failed ({exc})") from exc

    if not decoder.eof or decoder.unused_data or decoder.unconsumed_tail:
        raise _error(path, "incomplete or extra PNG compressed stream")

    channels = 4 if color == 6 else 1
    stride = width * channels + 1
    if len(pixels) != height * stride:
        raise _error(path, f"invalid PNG scanline size ({len(pixels)} decoded bytes)")
    if any(pixels[y * stride] > 4 for y in range(height)):
        raise _error(path, "invalid PNG scanline filter")


def iter_pngs(inputs: list[Path]) -> list[Path]:
    found: set[Path] = set()
    for item in inputs:
        if item.is_dir():
            found.update(p.resolve() for p in item.rglob("*.png") if p.is_file())
        elif item.is_file() and item.suffix.lower() == ".png":
            found.add(item.resolve())
        else:
            raise PngValidationError(f"PNG input does not exist or is not a PNG: {item}")
    return sorted(found)


def validate_inputs(inputs: list[Path]) -> tuple[int, list[str]]:
    textures = iter_pngs(inputs)
    if not textures:
        return 0, ["no PNG files found"]

    errors: list[str] = []
    for texture in textures:
        try:
            validate_png(texture)
        except (OSError, PngValidationError) as exc:
            errors.append(str(exc))
    return len(textures), errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Validate complete AMJ PNG structure and image data.")
    parser.add_argument(
        "paths",
        nargs="*",
        type=Path,
        default=[ROOT / "Textures"],
        help="PNG files or directories to scan; defaults to the repository Textures directory.",
    )
    args = parser.parse_args(argv)

    try:
        count, errors = validate_inputs(args.paths)
    except PngValidationError as exc:
        print(f"[FAIL] {exc}")
        return 1

    if errors:
        print(f"[FAIL] PNG integrity validation found {len(errors)} problem(s) across {count} file(s):")
        for error in errors:
            print(f"  - {error}")
        return 1

    print(f"[OK] PNG integrity validation passed: {count} file(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
