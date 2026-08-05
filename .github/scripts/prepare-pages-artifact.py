#!/usr/bin/env python3
"""Validate and package the pinned LocalGPT Kawaii documentation snapshot.

The generated DocFX trees under docs/_site and wwwroot/help-docs are intentionally
ignored by Git. GitHub Actions therefore publishes a single tracked ZIP snapshot
instead of assuming ignored build output exists after checkout.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import stat
import sys
import tempfile
from pathlib import Path, PurePosixPath
from zipfile import BadZipFile, ZipFile


REQUIRED_FILES = (
    "index.html",
    "api/index.html",
    "documentation-status.json",
    "styles/localgpt-kawaii.css",
    "styles/localgpt-kawaii.js",
    "favicon.svg",
    "favicon.ico",
    "logo.svg",
)

INDEX_MARKERS = (
    "localgpt-kawaii-docs",
    "data-localgpt-theme-bootstrap",
    "data-localgpt-favicon",
    "data-localgpt-kawaii-style",
    "data-localgpt-kawaii-script",
)

CSS_MARKERS = (
    "localgpt-theme-control",
    "localgpt-kawaii-sky",
    "localgpt-cursor-paw",
)

JS_MARKERS = (
    "mountThemeControl",
    "localgpt-docs-theme",
    "persistTheme",
    "localgpt-cursor-paw",
)

MAX_UNCOMPRESSED_BYTES = 512 * 1024 * 1024
MAX_FILE_COUNT = 20_000


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def fail(message: str) -> None:
    raise RuntimeError(message)


def validate_source(source: Path) -> dict[str, object]:
    if not source.is_dir():
        fail(f"Documentation source does not exist: {source}")

    for path in source.rglob("*"):
        if path.is_symlink():
            fail(f"Documentation tree must not contain symbolic links: {path}")

    missing = [name for name in REQUIRED_FILES if not (source / name).is_file()]
    if missing:
        fail("Documentation tree is incomplete; missing: " + ", ".join(missing))

    index_text = read_text(source / "index.html")
    missing_index_markers = [marker for marker in INDEX_MARKERS if marker not in index_text]
    if missing_index_markers:
        fail("index.html is not the themed LocalGPT build; missing: " + ", ".join(missing_index_markers))

    css_text = read_text(source / "styles/localgpt-kawaii.css")
    missing_css_markers = [marker for marker in CSS_MARKERS if marker not in css_text]
    if missing_css_markers:
        fail("Kawaii CSS is incomplete; missing: " + ", ".join(missing_css_markers))

    js_text = read_text(source / "styles/localgpt-kawaii.js")
    missing_js_markers = [marker for marker in JS_MARKERS if marker not in js_text]
    if missing_js_markers:
        fail("Kawaii JavaScript is incomplete; missing: " + ", ".join(missing_js_markers))

    favicon_text = read_text(source / "favicon.svg")
    if "LocalGPT cat paw" not in favicon_text or "<svg" not in favicon_text:
        fail("favicon.svg is not the LocalGPT cat-paw icon")

    try:
        status = json.loads(read_text(source / "documentation-status.json"))
    except (UnicodeDecodeError, json.JSONDecodeError) as error:
        fail(f"documentation-status.json is invalid: {error}")
    if not isinstance(status, dict):
        fail("documentation-status.json must contain an object")

    html_files = list(source.rglob("*.html"))
    api_html_files = list((source / "api").rglob("*.html"))
    if len(html_files) < 20 or len(api_html_files) < 1:
        fail(f"Documentation output looks incomplete ({len(html_files)} HTML, {len(api_html_files)} API HTML)")

    return {
        "source": source.as_posix(),
        "version": status.get("version") or status.get("Version") or "unknown",
        "htmlFiles": len(html_files),
        "apiHtmlFiles": len(api_html_files),
        "themePersistence": True,
        "catPawFavicon": True,
        "kawaiiStyleSha256": sha256(source / "styles/localgpt-kawaii.css"),
        "kawaiiScriptSha256": sha256(source / "styles/localgpt-kawaii.js"),
        "faviconSvgSha256": sha256(source / "favicon.svg"),
    }


def safe_extract_zip(archive: Path, destination: Path) -> Path:
    if not archive.is_file():
        fail(f"Pinned Pages archive does not exist: {archive}")

    try:
        with ZipFile(archive) as bundle:
            entries = bundle.infolist()
            if not entries:
                fail(f"Pinned Pages archive is empty: {archive}")
            if len(entries) > MAX_FILE_COUNT:
                fail(f"Pinned Pages archive contains too many entries: {len(entries)}")

            total_size = sum(entry.file_size for entry in entries)
            if total_size > MAX_UNCOMPRESSED_BYTES:
                fail(f"Pinned Pages archive is too large after extraction: {total_size} bytes")

            for entry in entries:
                raw_name = entry.filename.replace("\\", "/")
                path = PurePosixPath(raw_name)
                if path.is_absolute() or ".." in path.parts:
                    fail(f"Unsafe path in pinned Pages archive: {entry.filename}")

                unix_mode = entry.external_attr >> 16
                if stat.S_ISLNK(unix_mode):
                    fail(f"Symbolic links are not allowed in pinned Pages archive: {entry.filename}")

            bundle.extractall(destination)
    except BadZipFile as error:
        fail(f"Pinned Pages archive is not a valid ZIP: {error}")

    if (destination / "index.html").is_file():
        return destination

    candidates = [path for path in destination.iterdir() if path.is_dir() and (path / "index.html").is_file()]
    if len(candidates) == 1:
        return candidates[0]

    fail("Pinned Pages archive must contain index.html at its root or in one top-level directory")


def copy_tree(source: Path, output: Path) -> None:
    if output.exists():
        shutil.rmtree(output)
    shutil.copytree(source, output, symlinks=False)
    (output / ".nojekyll").write_text("", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    source_group = parser.add_mutually_exclusive_group(required=True)
    source_group.add_argument("--archive", type=Path)
    source_group.add_argument("--source", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    try:
        output = args.output.resolve(strict=False)

        if args.archive is not None:
            archive = args.archive.resolve(strict=True)
            with tempfile.TemporaryDirectory(prefix="localgpt-pages-") as temp_dir:
                extracted_source = safe_extract_zip(archive, Path(temp_dir))
                metadata = validate_source(extracted_source)
                copy_tree(extracted_source, output)
            metadata["deploymentSource"] = "tracked Kawaii documentation snapshot"
            metadata["sourceArchive"] = archive.as_posix()
            metadata["sourceArchiveSha256"] = sha256(archive)
        else:
            source = args.source.resolve(strict=True)
            metadata = validate_source(source)
            copy_tree(source, output)
            metadata["deploymentSource"] = "explicit documentation directory"

        metadata["artifact"] = output.as_posix()
        (output / "github-pages-deployment.json").write_text(
            json.dumps(metadata, indent=2, ensure_ascii=False) + "\n",
            encoding="utf-8",
        )
        print(json.dumps(metadata, indent=2, ensure_ascii=False))
        return 0
    except (OSError, RuntimeError) as error:
        print(f"Pages artifact preparation failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
