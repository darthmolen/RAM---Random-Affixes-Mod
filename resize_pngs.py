#!/usr/bin/env python3
"""Recursively resize PNG files in a folder, in place."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, UnidentifiedImageError


TARGET_SIZE = (160, 160)


def resize_pngs(folder: Path) -> tuple[int, int, int]:
    """Return counts for resized, already-correct, and unreadable PNG files."""
    resized = unchanged = failed = 0

    for path in folder.rglob("*"):
        if not path.is_file() or path.suffix.lower() != ".png":
            continue

        try:
            with Image.open(path) as image:
                if image.size == TARGET_SIZE:
                    unchanged += 1
                    continue

                # Copy before closing the source file, then overwrite it in place.
                output = image.resize(TARGET_SIZE, Image.Resampling.LANCZOS)
                output.save(path, format="PNG")
                resized += 1
                print(f"Resized: {path}")
        except (UnidentifiedImageError, OSError) as error:
            failed += 1
            print(f"Skipped (could not read): {path} ({error})")

    return resized, unchanged, failed


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Resize every PNG in a folder and all its subfolders to 160x160 pixels."
    )
    parser.add_argument("folder", type=Path, help="Root folder containing PNG files")
    args = parser.parse_args()

    folder = args.folder.resolve()
    if not folder.is_dir():
        parser.error(f"Not a folder: {folder}")

    resized, unchanged, failed = resize_pngs(folder)
    print(f"\nDone. Resized: {resized}; already 160x160: {unchanged}; skipped: {failed}.")


if __name__ == "__main__":
    main()
