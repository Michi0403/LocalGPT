#!/usr/bin/env python3
"""Extract the complete DocFX site already shipped inside a LocalGPT release ZIP."""

from __future__ import annotations

import argparse
import json
import shutil
import sys
import zipfile
from dataclasses import dataclass
from pathlib import Path, PurePosixPath


@dataclass(frozen=True)
class Candidate:
    archive: Path
    prefix: PurePosixPath
    status: dict[str, object]
    score: tuple[int, int, int, int, str]


def normalized_version(value: str) -> str:
    return value.strip().lower().removeprefix("refs/tags/").removeprefix("v")


def is_safe_member(name: str) -> bool:
    path = PurePosixPath(name.replace("\\", "/"))
    return not path.is_absolute() and ".." not in path.parts


def read_json(archive: zipfile.ZipFile, name: str) -> dict[str, object]:
    with archive.open(name) as stream:
        value = json.load(stream)
    if not isinstance(value, dict):
        raise ValueError(f"{name} did not contain a JSON object")
    return value


def find_candidates(archive_path: Path, expected_version: str) -> list[Candidate]:
    candidates: list[Candidate] = []
    with zipfile.ZipFile(archive_path) as archive:
        names = {name.replace("\\", "/") for name in archive.namelist() if is_safe_member(name)}
        for name in sorted(names):
            if not name.endswith("documentation-status.json"):
                continue
            prefix = PurePosixPath(name).parent
            index_name = (prefix / "index.html").as_posix()
            api_index_name = (prefix / "api" / "index.html").as_posix()
            if index_name not in names or api_index_name not in names:
                continue
            try:
                status = read_json(archive, name)
            except (OSError, ValueError, json.JSONDecodeError) as error:
                print(f"Ignoring invalid status file {archive_path.name}:{name}: {error}", file=sys.stderr)
                continue
            version = normalized_version(str(status.get("version", "")))
            score = (
                int(version == expected_version),
                int(bool(status.get("completeApiReference"))),
                int(bool(status.get("pdfAvailable"))),
                int(status.get("apiHtmlCount", 0) or 0),
                str(status.get("generatedAtUtc", "")),
            )
            candidates.append(Candidate(archive_path, prefix, status, score))
    return candidates


def extract_candidate(candidate: Candidate, output: Path) -> None:
    if output.exists():
        shutil.rmtree(output)
    output.mkdir(parents=True)
    prefix_text = candidate.prefix.as_posix().rstrip("/") + "/"
    with zipfile.ZipFile(candidate.archive) as archive:
        for info in archive.infolist():
            name = info.filename.replace("\\", "/")
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


def validate_output(output: Path, status: dict[str, object]) -> None:
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

    custom_style = output / "public" / "main.css"
    resource_style = output / "styles" / "main.css"
    if not custom_style.is_file() and not resource_style.is_file():
        raise RuntimeError("The Kawaii DocFX stylesheet was not included in the shipped site")

    (output / ".nojekyll").write_text("", encoding="utf-8")
    summary = {
        "sourceVersion": status.get("version"),
        "htmlFiles": html_count,
        "apiHtmlCount": status.get("apiHtmlCount"),
        "pdfAvailable": status.get("pdfAvailable"),
        "documentationMode": status.get("documentationMode"),
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
            "No release ZIP contained a complete wwwroot/help-docs tree with index.html, api/index.html, and documentation-status.json"
        )

    selected = max(candidates, key=lambda item: item.score)
    print(
        f"Selected {selected.archive.name}:{selected.prefix.as_posix()} "
        f"for LocalGPT {selected.status.get('version')} (score={selected.score})"
    )
    extract_candidate(selected, arguments.output)
    validate_output(arguments.output, selected.status)
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:  # noqa: BLE001 - workflow boundary must report a concise failure.
        print(f"Documentation extraction failed: {error}", file=sys.stderr)
        raise SystemExit(1)
