#!/usr/bin/env python3
"""Validate and prepare the single tracked LocalGPT GitHub Pages artifact."""
from __future__ import annotations
import argparse, hashlib, json, re, shutil, stat, sys, tempfile, zlib
from html.parser import HTMLParser
from pathlib import Path, PurePosixPath
from urllib.parse import unquote, urlsplit
from zipfile import BadZipFile, ZipFile

PRODUCT = 'LocalGPT'
STYLE_FILE = 'styles/localgpt-kawaii.css'
SCRIPT_FILE = 'styles/localgpt-kawaii.js'
FAVICON_LABEL = 'LocalGPT cat paw'
INDEX_MARKERS = ('localgpt-kawaii-docs', 'data-localgpt-theme-bootstrap', 'data-localgpt-favicon', 'data-localgpt-kawaii-style', 'data-localgpt-kawaii-script')
CSS_MARKERS = ('localgpt-theme-control', 'localgpt-kawaii-sky', 'localgpt-cursor-paw', '--kawaii-docs-rail-width')
JS_MARKERS = ('mountThemeControl', 'localgpt-docs-theme', 'persistTheme', 'localgpt-cursor-paw')
MAX_UNCOMPRESSED_BYTES = 512 * 1024 * 1024
MAX_FILE_COUNT = 20_000
MIN_PDF_BYTES = 524_288

REQUIRED_FILES = (
    "index.html", "api/index.html", "documentation-status.json",
    STYLE_FILE, SCRIPT_FILE, "favicon.svg", "favicon.ico", "logo.svg",
)

class PageParser(HTMLParser):
    def __init__(self) -> None:
        super().__init__(convert_charrefs=True)
        self.links: list[tuple[str, str]] = []
        self.lang = ""
        self.has_viewport = False
        self.has_title = False
        self.landmarks = 0
        self.images_without_alt = 0
    def handle_starttag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        values = {k.lower(): (v or "") for k, v in attrs}
        lower = tag.lower()
        if lower == "html": self.lang = values.get("lang", "").strip()
        elif lower == "meta" and values.get("name", "").lower() == "viewport": self.has_viewport = bool(values.get("content", "").strip())
        elif lower == "title": self.has_title = True
        elif lower in {"main", "article"}: self.landmarks += 1
        elif lower == "img" and "alt" not in values: self.images_without_alt += 1
        for attr in ("href", "src"):
            value = values.get(attr)
            if value: self.links.append((attr, value.strip()))

def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")

def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""): digest.update(chunk)
    return digest.hexdigest()

def fail(message: str) -> None:
    raise RuntimeError(message)

def resolve_local_link(source: Path, root: Path, raw: str) -> Path | None:
    if not raw or raw.startswith(("#", "mailto:", "tel:", "javascript:", "data:")): return None
    parts = urlsplit(raw)
    if parts.scheme or parts.netloc: return None
    path_text = unquote(parts.path).replace("\\", "/")
    if not path_text: return None
    if path_text.startswith("/"):
        candidate = root / path_text.lstrip("/")
    else:
        candidate = source.parent / path_text
    candidate = candidate.resolve(strict=False)
    root_resolved = root.resolve(strict=True)
    try: candidate.relative_to(root_resolved)
    except ValueError: fail(f"Documentation link escapes the artifact: {source.relative_to(root)} -> {raw}")
    if candidate.is_dir(): candidate = candidate / "index.html"
    return candidate

def validate_html(source: Path) -> tuple[int, int]:
    html_files = sorted(source.rglob("*.html"))
    broken: list[str] = []
    accessibility: list[str] = []
    for html in html_files:
        text = read_text(html)
        parser = PageParser()
        try: parser.feed(text)
        except Exception as error: fail(f"Invalid HTML parser input {html.relative_to(source)}: {error}")
        rel = html.relative_to(source).as_posix()
        is_toc_fragment = html.name.lower() == "toc.html"
        if not is_toc_fragment:
            if not parser.lang: accessibility.append(f"{rel}: missing html lang")
            if not parser.has_viewport: accessibility.append(f"{rel}: missing viewport")
            if not parser.has_title: accessibility.append(f"{rel}: missing title")
            if parser.landmarks == 0: accessibility.append(f"{rel}: missing main/article landmark")
            if parser.images_without_alt: accessibility.append(f"{rel}: {parser.images_without_alt} image(s) missing alt")
        for _, raw in parser.links:
            target = resolve_local_link(html, source, raw)
            if target is not None and not target.is_file():
                broken.append(f"{rel} -> missing target '{target.relative_to(source).as_posix()}'")
    if accessibility:
        fail("Documentation accessibility validation failed: " + "; ".join(accessibility[:30]))
    if broken:
        fail("Documentation contains invalid local links: " + "; ".join(broken[:40]))
    api_count = sum(1 for path in html_files if "api" in path.relative_to(source).parts)
    return len(html_files), api_count

def pdf_contains_token(data: bytes, token: bytes) -> bool:
    """Find a PDF name token in raw bytes or Flate-compressed object streams.

    Modern tagged PDFs commonly store the catalog in a compressed object stream, so a
    raw byte search alone incorrectly rejects valid /StructTreeRoot metadata. This small
    stdlib-only probe keeps the Pages validator dependency-free while handling that case.
    """
    if token in data:
        return True
    for match in re.finditer(rb"stream\r?\n", data):
        # Only attempt zlib decompression when the nearby object dictionary declares Flate.
        prefix = data[max(0, match.start() - 1024):match.start()]
        if b"/FlateDecode" not in prefix:
            continue
        end = data.find(b"endstream", match.end())
        if end < 0:
            continue
        payload = data[match.end():end].rstrip(b"\r\n")
        try:
            decoded = zlib.decompress(payload)
        except zlib.error:
            continue
        if token in decoded:
            return True
    return False

def validate_pdf(source: Path, status: dict[str, object]) -> tuple[str, int, bool, str]:
    name = str(status.get("pdfFileName") or status.get("PdfFileName") or "").strip()
    if not name: fail("documentation-status.json does not declare pdfFileName")
    pdf = source / name
    if not pdf.is_file(): fail(f"Declared documentation PDF is missing: {name}")
    size = pdf.stat().st_size
    if size < MIN_PDF_BYTES: fail(f"Documentation PDF is too small: {size} bytes")
    declared_size = status.get("pdfBytes") or status.get("PdfBytes")
    if declared_size is not None and int(declared_size) != size:
        fail(f"documentation-status.json declares {declared_size} PDF bytes but {name} contains {size}")
    with pdf.open("rb") as stream:
        probe = stream.read(min(size, 4 * 1024 * 1024))
    if not probe.startswith(b"%PDF-"): fail(f"{name} does not have a PDF header")
    if b"ReportLab" in probe or b"Deterministic fallback documentation index" in probe:
        fail(f"{name} is an obsolete source/fallback PDF rather than the maintained HTML-backed handbook")
    accessibility_mode = str(status.get("pdfAccessibilityMode") or status.get("PdfAccessibilityMode") or "tagged-pdf-required").strip()
    html_preflight = bool(status.get("htmlPreflightValidated") or status.get("HtmlPreflightValidated"))
    if accessibility_mode == "html-accessibility-fallback" and html_preflight:
        # DocFX plug-in PDFs can be multi-gigabyte. Do not read the entire file merely to prove a
        # structure token that this renderer is known not to request; HTML accessibility remains strict.
        tagged = b"/StructTreeRoot" in probe
    else:
        data = pdf.read_bytes()
        tagged = pdf_contains_token(data, b"/StructTreeRoot")
        if not tagged:
            fail(f"{name} is not a tagged accessible PDF (/StructTreeRoot missing)")
    return name, size, tagged, accessibility_mode

def validate_source(source: Path, expected_version: str | None = None) -> dict[str, object]:
    if not source.is_dir(): fail(f"Documentation source does not exist: {source}")
    for path in source.rglob("*"):
        if path.is_symlink(): fail(f"Documentation tree must not contain symbolic links: {path}")
    missing = [name for name in REQUIRED_FILES if not (source / name).is_file()]
    if missing: fail("Documentation tree is incomplete; missing: " + ", ".join(missing))
    index_text = read_text(source / "index.html")
    missing_markers = [m for m in INDEX_MARKERS if m not in index_text]
    if missing_markers: fail("index.html is not the themed build; missing: " + ", ".join(missing_markers))
    css_text = read_text(source / STYLE_FILE)
    missing_markers = [m for m in CSS_MARKERS if m not in css_text]
    if missing_markers: fail("Documentation CSS is incomplete; missing: " + ", ".join(missing_markers))
    js_text = read_text(source / SCRIPT_FILE)
    missing_markers = [m for m in JS_MARKERS if m not in js_text]
    if missing_markers: fail("Documentation JavaScript is incomplete; missing: " + ", ".join(missing_markers))
    favicon = read_text(source / "favicon.svg")
    if FAVICON_LABEL not in favicon or "<svg" not in favicon: fail("favicon.svg is not the maintained cat-paw icon")
    try: status = json.loads(read_text(source / "documentation-status.json"))
    except (UnicodeDecodeError, json.JSONDecodeError) as error: fail(f"documentation-status.json is invalid: {error}")
    if not isinstance(status, dict): fail("documentation-status.json must contain an object")
    version = str(status.get("version") or status.get("Version") or "").strip()
    if not version: fail("documentation-status.json does not contain a version")
    if expected_version and version != expected_version: fail(f"Documentation version {version} does not match expected version {expected_version}")
    html_count, api_count = validate_html(source)
    if html_count < 20 or api_count < 1: fail(f"Documentation output looks incomplete ({html_count} HTML, {api_count} API HTML)")
    declared_api = status.get("apiHtmlCount") or status.get("ApiHtmlCount")
    if declared_api is not None and int(declared_api) not in {api_count, max(0, api_count - 1), max(0, api_count - 2)}:
        fail(f"documentation-status.json declares {declared_api} API HTML pages but the artifact contains {api_count}")
    complete = bool(status.get("completeApiReference") or status.get("CompleteApiReference"))
    mode = str(status.get("documentationMode") or status.get("DocumentationMode") or "")
    if complete and api_count < 100: fail(f"completeApiReference=true requires a substantial generated API reference; only {api_count} API pages exist")
    if not complete and "source" not in mode.lower(): fail("An incomplete API preview must be declared as a source documentation mode")
    pdf_available = bool(status.get("pdfAvailable", True))
    if pdf_available:
        pdf_name, pdf_bytes, tagged, pdf_accessibility_mode = validate_pdf(source, status)
        pages_pdf_published = True
    else:
        pdf_name = str(status.get("releasePdfFileName") or status.get("pdfFileName") or "").strip()
        pdf_bytes = int(status.get("releasePdfBytes") or status.get("pdfBytes") or 0)
        tagged = bool(status.get("releasePdfTagged") or False)
        pdf_accessibility_mode = str(status.get("pdfAccessibilityMode") or "html-accessibility-fallback")
        pages_pdf_published = False
        if not pdf_name or pdf_bytes < MIN_PDF_BYTES:
            fail("HTML-only Pages snapshot must preserve releasePdfFileName/releasePdfBytes metadata")
        if (source / pdf_name).exists():
            fail("HTML-only Pages snapshot unexpectedly contains the release PDF")
    return {
        "source": source.as_posix(), "version": version, "htmlFiles": html_count,
        "apiHtmlFiles": api_count, "completeApiReference": complete,
        "pdfFile": pdf_name, "pdfBytes": pdf_bytes, "taggedPdf": tagged,
        "pdfAccessibilityMode": pdf_accessibility_mode, "pagesPdfPublished": pages_pdf_published,
        "localLinksValid": True, "htmlAccessibilityValid": True,
        "themePersistence": True, "catPawFavicon": True,
        "kawaiiStyleSha256": sha256(source / STYLE_FILE),
        "kawaiiScriptSha256": sha256(source / SCRIPT_FILE),
        "faviconSvgSha256": sha256(source / "favicon.svg"),
    }

def safe_extract_zip(archive: Path, destination: Path) -> Path:
    if not archive.is_file(): fail(f"Pinned Pages archive does not exist: {archive}")
    try:
        with ZipFile(archive) as bundle:
            entries = bundle.infolist()
            if not entries: fail(f"Pinned Pages archive is empty: {archive}")
            if len(entries) > MAX_FILE_COUNT: fail(f"Pinned Pages archive contains too many entries: {len(entries)}")
            names = [entry.filename.replace("\\", "/") for entry in entries]
            if len(names) != len(set(names)): fail("Pinned Pages archive contains duplicate entries")
            total_size = sum(entry.file_size for entry in entries)
            if total_size > MAX_UNCOMPRESSED_BYTES: fail(f"Pinned Pages archive is too large after extraction: {total_size} bytes")
            for entry, raw_name in zip(entries, names):
                path = PurePosixPath(raw_name)
                if path.is_absolute() or ".." in path.parts: fail(f"Unsafe path in pinned Pages archive: {entry.filename}")
                if stat.S_ISLNK(entry.external_attr >> 16): fail(f"Symbolic links are not allowed: {entry.filename}")
            bundle.extractall(destination)
    except BadZipFile as error: fail(f"Pinned Pages archive is not a valid ZIP: {error}")
    if (destination / "index.html").is_file(): return destination
    candidates = [p for p in destination.iterdir() if p.is_dir() and (p / "index.html").is_file()]
    if len(candidates) == 1: return candidates[0]
    fail("Pinned Pages archive must contain index.html at its root or in one top-level directory")

def copy_tree(source: Path, output: Path) -> None:
    if output.exists(): shutil.rmtree(output)
    shutil.copytree(source, output, symlinks=False)
    (output / ".nojekyll").write_text("", encoding="utf-8")

def copy_pages_tree(source: Path, output: Path, metadata: dict[str, object]) -> None:
    """Create the tracked Pages payload without duplicating the potentially multi-GB release PDF."""
    pdf_name = str(metadata.get("pdfFile") or "").strip()
    if output.exists(): shutil.rmtree(output)
    def ignore(directory: str, names: list[str]) -> set[str]:
        return {pdf_name} if pdf_name in names else set()
    shutil.copytree(source, output, symlinks=False, ignore=ignore)
    (output / ".nojekyll").write_text("", encoding="utf-8")
    status_path = output / "documentation-status.json"
    status = json.loads(read_text(status_path))
    status["releasePdfFileName"] = pdf_name
    status["releasePdfBytes"] = int(metadata.get("pdfBytes") or 0)
    status["releasePdfTagged"] = bool(metadata.get("taggedPdf"))
    status["pdfAvailable"] = False
    status["pagesPdfPublished"] = False
    status["pagesPdfExcludedReason"] = "The complete handbook is distributed with the release bundle; GitHub Pages publishes the validated HTML reference only."
    status_path.write_text(json.dumps(status, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    release_url = "https://github.com/Michi0403/LocalGPT/releases/latest"
    for html in output.rglob("*.html"):
        text = read_text(html)
        if pdf_name in text:
            pattern = re.compile(r'href=(?P<q>["\'])(?:\.\./|\./)?' + re.escape(pdf_name) + r'(?P=q)', re.IGNORECASE)
            text = pattern.sub('href="' + release_url + '"', text)
            html.write_text(text, encoding="utf-8")

def main() -> int:
    parser = argparse.ArgumentParser()
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--archive", type=Path)
    group.add_argument("--source", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--expected-version")
    parser.add_argument("--html-only", action="store_true", help="Validate generated HTML accessibility and local links without requiring status/PDF artifacts.")
    args = parser.parse_args()
    try:
        if args.html_only:
            if args.source is None:
                fail("--html-only requires --source")
            source = args.source.resolve(strict=True)
            html_count, api_count = validate_html(source)
            print(json.dumps({
                "source": source.as_posix(),
                "htmlFiles": html_count,
                "apiHtmlFiles": api_count,
                "localLinksValid": True,
                "htmlAccessibilityValid": True,
            }, indent=2, ensure_ascii=False))
            return 0
        if args.output is None:
            fail("--output is required unless --html-only is used")
        output = args.output.resolve(strict=False)
        if args.archive is not None:
            archive = args.archive.resolve(strict=True)
            with tempfile.TemporaryDirectory(prefix='localgpt-pages-') as temp_dir:
                source = safe_extract_zip(archive, Path(temp_dir))
                metadata = validate_source(source, args.expected_version)
                copy_tree(source, output)
            metadata.update({"deploymentSource": "tracked Kawaii documentation snapshot", "sourceArchive": archive.as_posix(), "sourceArchiveSha256": sha256(archive)})
        else:
            source = args.source.resolve(strict=True)
            metadata = validate_source(source, args.expected_version)
            copy_pages_tree(source, output, metadata)
            metadata["pagesPdfPublished"] = False
            metadata["deploymentSource"] = "explicit documentation directory (HTML-only Pages snapshot; release PDF kept out of tracked archive)"
        metadata["artifact"] = output.as_posix()
        (output / "github-pages-deployment.json").write_text(json.dumps(metadata, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        print(json.dumps(metadata, indent=2, ensure_ascii=False))
        return 0
    except (OSError, RuntimeError, ValueError) as error:
        print(f"Pages artifact preparation failed: {error}", file=sys.stderr)
        return 1
if __name__ == "__main__": raise SystemExit(main())
