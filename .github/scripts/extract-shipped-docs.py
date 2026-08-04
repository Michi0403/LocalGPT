#!/usr/bin/env python3
"""Extract the complete themed DocFX site already shipped inside a LocalGPT release ZIP."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import shutil
import sys
import zipfile
from dataclasses import dataclass
from pathlib import Path, PurePosixPath


CSS_MARKERS = (
    "localgpt-kawaii-docs",
    "localgpt-api-neko-note",
    "localgpt-theme-control",
    "localgpt-cursor-paw",
)
JS_MARKERS = (
    "localgpt-cursor-paw",
    "localgpt-cat-scratch",
    "mountThemeControl",
    "localgpt-docs-theme",
)
HTML_MARKERS = (
    "localgpt-kawaii-docs",
    "data-localgpt-kawaii-style",
    "data-localgpt-kawaii-script",
)


@dataclass(frozen=True)
class Candidate:
    archive: Path
    prefix: PurePosixPath
    status: dict[str, object]
    style_sha256: str
    script_sha256: str
    score: tuple[int, int, int, int, int, str]


def normalized_version(value: str) -> str:
    return value.strip().lower().removeprefix("refs/tags/").removeprefix("v")


def normalize_member_name(name: str) -> str:
    return PurePosixPath(name.replace("\\", "/")).as_posix()


def is_safe_member(name: str) -> bool:
    path = PurePosixPath(normalize_member_name(name))
    return not path.is_absolute() and ".." not in path.parts


def build_member_index(archive: zipfile.ZipFile) -> dict[str, zipfile.ZipInfo]:
    """Map portable slash paths to their exact stored ZIP entries.

    PowerShell Compress-Archive can store Windows release members with backslashes.
    zipfile.getinfo() requires the original stored spelling, so candidate discovery must
    not throw that spelling away after normalizing paths for comparison.
    """
    members: dict[str, zipfile.ZipInfo] = {}
    for info in archive.infolist():
        if not is_safe_member(info.filename):
            continue
        normalized = normalize_member_name(info.filename)
        existing = members.get(normalized)
        if existing is not None:
            raise ValueError(
                f"archive contains duplicate or ambiguous members {existing.filename!r} and {info.filename!r} "
                f"for normalized path {normalized!r}"
            )
        members[normalized] = info
    return members


def read_bytes(
    archive: zipfile.ZipFile, info: zipfile.ZipInfo, maximum_bytes: int = 8_000_000
) -> bytes:
    if info.file_size > maximum_bytes:
        raise ValueError(f"{info.filename} is unexpectedly large ({info.file_size} bytes)")
    with archive.open(info) as stream:
        value = stream.read(maximum_bytes + 1)
    if len(value) > maximum_bytes:
        raise ValueError(f"{info.filename} exceeded the bounded read size")
    return value


def read_text(archive: zipfile.ZipFile, info: zipfile.ZipInfo) -> str:
    return read_bytes(archive, info).decode("utf-8-sig")


def read_json(archive: zipfile.ZipFile, info: zipfile.ZipInfo) -> dict[str, object]:
    value = json.loads(read_text(archive, info))
    if not isinstance(value, dict):
        raise ValueError(f"{info.filename} did not contain a JSON object")
    return value


def contains_all(value: str, markers: tuple[str, ...]) -> bool:
    return all(marker in value for marker in markers)


def find_candidates(archive_path: Path, expected_version: str) -> list[Candidate]:
    candidates: list[Candidate] = []
    with zipfile.ZipFile(archive_path) as archive:
        try:
            members = build_member_index(archive)
        except ValueError as error:
            print(f"Ignoring unsafe or ambiguous release archive {archive_path.name}: {error}", file=sys.stderr)
            return candidates

        names = set(members)
        for name in sorted(names):
            if not name.endswith("documentation-status.json"):
                continue

            prefix = PurePosixPath(name).parent
            index_name = (prefix / "index.html").as_posix()
            api_index_name = (prefix / "api" / "index.html").as_posix()
            style_name = (prefix / "styles" / "localgpt-kawaii.css").as_posix()
            script_name = (prefix / "styles" / "localgpt-kawaii.js").as_posix()
            required_names = (index_name, api_index_name, style_name, script_name)
            if any(required_name not in members for required_name in required_names):
                continue

            try:
                status = read_json(archive, members[name])
                index_html = read_text(archive, members[index_name])
                style_bytes = read_bytes(archive, members[style_name])
                script_bytes = read_bytes(archive, members[script_name])
                style_text = style_bytes.decode("utf-8-sig")
                script_text = script_bytes.decode("utf-8-sig")
            except (KeyError, OSError, UnicodeDecodeError, ValueError, json.JSONDecodeError) as error:
                stored_name = members[name].filename if name in members else name
                print(
                    f"Ignoring invalid documentation candidate {archive_path.name}:{stored_name}: {error}",
                    file=sys.stderr,
                )
                continue

            missing_groups: list[str] = []
            if not contains_all(index_html, HTML_MARKERS):
                missing_groups.append("HTML activation markers")
            if not contains_all(style_text, CSS_MARKERS):
                missing_groups.append("Kawaii CSS markers")
            if not contains_all(script_text, JS_MARKERS):
                missing_groups.append("Kawaii JavaScript markers")
            if missing_groups:
                print(
                    f"Ignoring unthemed documentation candidate {archive_path.name}:{prefix.as_posix()} "
                    f"({', '.join(missing_groups)})",
                    file=sys.stderr,
                )
                continue

            # Project Pages must keep asset URLs relative so /OWNER/REPOSITORY/ works.
            if re.search(r'''(?:href|src)=["']/+styles/localgpt-kawaii\.(?:css|js)''', index_html, re.IGNORECASE):
                print(
                    f"Ignoring documentation candidate with root-absolute theme assets "
                    f"{archive_path.name}:{prefix.as_posix()}",
                    file=sys.stderr,
                )
                continue

            version = normalized_version(str(status.get("version", "")))
            score = (
                int(version == expected_version),
                1,  # Reaching this point means the complete current Kawaii theme is present.
                int(bool(status.get("completeApiReference"))),
                int(bool(status.get("pdfAvailable"))),
                int(status.get("apiHtmlCount", 0) or 0),
                str(status.get("generatedAtUtc", "")),
            )
            candidates.append(
                Candidate(
                    archive=archive_path,
                    prefix=prefix,
                    status=status,
                    style_sha256=hashlib.sha256(style_bytes).hexdigest(),
                    script_sha256=hashlib.sha256(script_bytes).hexdigest(),
                    score=score,
                )
            )
    return candidates


def extract_candidate(candidate: Candidate, output: Path) -> None:
    if output.exists():
        shutil.rmtree(output)
    output.mkdir(parents=True)
    prefix_text = candidate.prefix.as_posix().rstrip("/") + "/"
    with zipfile.ZipFile(candidate.archive) as archive:
        for info in archive.infolist():
            name = normalize_member_name(info.filename)
            if not is_safe_member(name) or not name.startswith(prefix_text):
                continue
            relative_text = name[len(prefix_text) :]
            if not relative_text:
                continue
            relative = PurePosixPath(relative_text)
            destination = output.joinpath(*relative.parts)
            if info.is_dir():
                destination.mkdir(parents=True, exist_ok=True)
                continue
            destination.parent.mkdir(parents=True, exist_ok=True)
            with archive.open(info) as source, destination.open("wb") as target:
                shutil.copyfileobj(source, target)


def file_sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def validate_output(output: Path, candidate: Candidate) -> None:
    status = candidate.status
    required = [output / "index.html", output / "api" / "index.html", output / "documentation-status.json"]
    if bool(status.get("pdfAvailable")):
        pdf_name = str(status.get("pdfFileName", "")).strip()
        if pdf_name:
            required.append(output / pdf_name)
    missing = [str(path.relative_to(output)) for path in required if not path.is_file()]
    if missing:
        raise RuntimeError("The shipped documentation tree is incomplete: " + ", ".join(missing))

    html_count = len(list(output.rglob("*.html")))
    if html_count < 10:
        raise RuntimeError(f"Only {html_count} HTML files were extracted; expected a complete DocFX site")

    theme_style = output / "styles" / "localgpt-kawaii.css"
    theme_script = output / "styles" / "localgpt-kawaii.js"
    if not theme_style.is_file() or not theme_script.is_file():
        raise RuntimeError("The cache-busted Kawaii DocFX website assets were not included in the shipped site")

    style_text = theme_style.read_text(encoding="utf-8-sig")
    script_text = theme_script.read_text(encoding="utf-8-sig")
    index_html = (output / "index.html").read_text(encoding="utf-8-sig")
    if not contains_all(index_html, HTML_MARKERS):
        raise RuntimeError("The shipped DocFX index does not activate the LocalGPT Kawaii website theme")
    if not contains_all(style_text, CSS_MARKERS):
        raise RuntimeError("The shipped Kawaii stylesheet is stale or incomplete")
    if not contains_all(script_text, JS_MARKERS):
        raise RuntimeError("The shipped Kawaii JavaScript is stale or incomplete")

    extracted_style_hash = file_sha256(theme_style)
    extracted_script_hash = file_sha256(theme_script)
    if extracted_style_hash != candidate.style_sha256 or extracted_script_hash != candidate.script_sha256:
        raise RuntimeError("The extracted Kawaii assets do not match the selected release archive")

    (output / ".nojekyll").write_text("", encoding="utf-8")
    summary = {
        "sourceArchive": candidate.archive.name,
        "sourcePrefix": candidate.prefix.as_posix(),
        "sourceVersion": status.get("version"),
        "htmlFiles": html_count,
        "apiHtmlCount": status.get("apiHtmlCount"),
        "pdfAvailable": status.get("pdfAvailable"),
        "pdfFileName": status.get("pdfFileName"),
        "documentationMode": status.get("documentationMode"),
        "kawaiiStyleSha256": extracted_style_hash,
        "kawaiiScriptSha256": extracted_script_hash,
        "themeMarkersVerified": True,
        "projectRelativeAssetsVerified": True,
    }
    (output / "github-pages-deployment.json").write_text(
        json.dumps(summary, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--assets", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--version", required=True)
    arguments = parser.parse_args()

    expected_version = normalized_version(arguments.version)
    archives = sorted(arguments.assets.glob("*.zip"))
    if not archives:
        raise RuntimeError("The release did not contain any ZIP assets")

    candidates = [candidate for archive in archives for candidate in find_candidates(archive, expected_version)]
    if not candidates:
        raise RuntimeError(
            "No release ZIP contained a complete themed wwwroot/help-docs tree with the current Kawaii CSS, JavaScript, "
            "index.html, api/index.html and documentation-status.json"
        )

    selected = max(candidates, key=lambda item: item.score)
    print(
        f"Selected {selected.archive.name}:{selected.prefix.as_posix()} "
        f"for LocalGPT {selected.status.get('version')} (score={selected.score})"
    )
    extract_candidate(selected, arguments.output)
    validate_output(arguments.output, selected)
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:  # noqa: BLE001 - workflow boundary must report a concise failure.
        print(f"Documentation extraction failed: {error}", file=sys.stderr)
        raise SystemExit(1)
