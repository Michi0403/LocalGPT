#!/usr/bin/env python3
"""Validate and package the exact Kawaii DocFX tree shipped by the LocalGPT app.

GitHub Pages intentionally publishes the checked-in app documentation rather than
re-downloading an older release archive. This keeps the embedded help and the public
site identical after a documentation commit.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import sys
from pathlib import Path


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


def copy_tree(source: Path, output: Path) -> None:
    if output.exists():
        shutil.rmtree(output)
    shutil.copytree(source, output, symlinks=False)
    (output / ".nojekyll").write_text("", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    try:
        source = args.source.resolve(strict=True)
        output = args.output.resolve(strict=False)
        metadata = validate_source(source)
        copy_tree(source, output)
        metadata["artifact"] = output.as_posix()
        metadata["deploymentSource"] = "checked-in app help-docs"
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
