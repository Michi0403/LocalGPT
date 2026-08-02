#!/usr/bin/env python3
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
import argparse
import re
import sys


@dataclass(frozen=True)
class Finding:
    relative_path: str
    line: int
    message: str


def mask_csharp(text: str) -> str:
    """Preserve source offsets while masking comments, normal strings, chars and raw strings."""
    output = list(text)
    index = 0
    length = len(text)
    state = "code"
    raw_quotes = 0
    while index < length:
        current = text[index]
        next_character = text[index + 1] if index + 1 < length else ""
        if state == "code":
            if current == "/" and next_character == "/":
                output[index] = output[index + 1] = " "
                index += 2
                state = "line-comment"
                continue
            if current == "/" and next_character == "*":
                output[index] = output[index + 1] = " "
                index += 2
                state = "block-comment"
                continue
            if current == '"':
                quote_count = 1
                while index + quote_count < length and text[index + quote_count] == '"':
                    quote_count += 1
                if quote_count >= 3:
                    raw_quotes = quote_count
                    for offset in range(index, index + quote_count):
                        output[offset] = " "
                    index += quote_count
                    state = "raw-string"
                    continue
                output[index] = " "
                index += 1
                state = "string"
                continue
            if current == "'":
                output[index] = " "
                index += 1
                state = "char"
                continue
            index += 1
            continue
        if state == "line-comment":
            if current == "\n":
                state = "code"
            else:
                output[index] = " "
            index += 1
            continue
        if state == "block-comment":
            output[index] = " "
            if current == "*" and next_character == "/":
                output[index + 1] = " "
                index += 2
                state = "code"
            else:
                index += 1
            continue
        if state == "string":
            output[index] = " "
            if current == "\\" and index + 1 < length:
                output[index + 1] = " "
                index += 2
            elif current == '"':
                index += 1
                state = "code"
            else:
                index += 1
            continue
        if state == "char":
            output[index] = " "
            if current == "\\" and index + 1 < length:
                output[index + 1] = " "
                index += 2
            elif current == "'":
                index += 1
                state = "code"
            else:
                index += 1
            continue
        if state == "raw-string":
            output[index] = " "
            if current == '"':
                quote_count = 1
                while index + quote_count < length and text[index + quote_count] == '"':
                    quote_count += 1
                if quote_count >= raw_quotes:
                    for offset in range(index, min(index + quote_count, length)):
                        output[offset] = " "
                    index += quote_count
                    state = "code"
                else:
                    index += quote_count
            else:
                index += 1
    return "".join(output)


def line_number(text: str, offset: int) -> int:
    return text.count("\n", 0, offset) + 1


def next_word(masked: str, offset: int) -> tuple[str, int]:
    index = offset
    while index < len(masked) and masked[index].isspace():
        index += 1
    match = re.match(r"[A-Za-z_]\w*", masked[index:])
    return (match.group(0) if match else "", index)


def scan_await_expression(masked: str, offset: int) -> str:
    """Return one awaited expression, stopping at a top-level expression boundary."""
    index = offset
    parentheses = 0
    brackets = 0
    braces = 0
    while index < len(masked):
        character = masked[index]
        if character == "(":
            parentheses += 1
        elif character == ")":
            if parentheses == 0:
                break
            parentheses -= 1
        elif character == "[":
            brackets += 1
        elif character == "]":
            if brackets == 0:
                break
            brackets -= 1
        elif character == "{":
            braces += 1
        elif character == "}":
            if braces == 0:
                break
            braces -= 1
        elif character in ";," and parentheses == 0 and brackets == 0 and braces == 0:
            break
        index += 1
    return masked[offset:index]


def scan_foreach_header(masked: str, offset: int) -> str:
    opening = masked.find("(", offset)
    if opening < 0:
        return masked[offset : min(len(masked), offset + 2000)]
    depth = 0
    index = opening
    while index < len(masked):
        character = masked[index]
        if character == "(":
            depth += 1
        elif character == ")":
            depth -= 1
            if depth == 0:
                return masked[offset : index + 1]
        index += 1
    return masked[offset : min(len(masked), offset + 2000)]


def iter_source_files(source_root: Path):
    for path in source_root.rglob("*"):
        if not path.is_file() or path.suffix.lower() not in {".cs", ".razor"}:
            continue
        if any(part in {"bin", "obj", "Migrations", ".git", ".vs"} for part in path.parts):
            continue
        if path.name.endswith(".Designer.cs"):
            continue
        yield path


def audit(source_root: Path) -> tuple[list[Finding], dict[str, int]]:
    findings: list[Finding] = []
    totals = {
        "files": 0,
        "awaits": 0,
        "configure_false": 0,
        "configure_true": 0,
        "async_disposals": 0,
        "async_streams": 0,
    }
    configure_false_pattern = re.compile(r"\.ConfigureAwait\s*\(\s*false\s*\)")
    configure_true_pattern = re.compile(r"\.ConfigureAwait\s*\(\s*true\s*\)")

    for path in iter_source_files(source_root):
        text = path.read_text(encoding="utf-8-sig", errors="replace")
        relative = path.relative_to(source_root).as_posix()
        is_renderer_source = relative.startswith("Components/")
        masked = mask_csharp(text) if path.suffix.lower() == ".cs" else text
        false_matches = list(configure_false_pattern.finditer(masked))
        true_matches = list(configure_true_pattern.finditer(masked))
        await_matches = list(re.finditer(r"\bawait\b", masked))
        if not await_matches and not false_matches and not true_matches:
            continue

        totals["files"] += 1
        totals["awaits"] += len(await_matches)
        totals["configure_false"] += len(false_matches)
        totals["configure_true"] += len(true_matches)

        for match in true_matches:
            findings.append(Finding(
                relative,
                line_number(text, match.start()),
                "ConfigureAwait(true) is prohibited. Components use ordinary await; context-free code uses ConfigureAwait(false).",
            ))

        if is_renderer_source:
            for match in false_matches:
                findings.append(Finding(
                    relative,
                    line_number(text, match.start()),
                    "Renderer-owned component code must use ordinary await instead of ConfigureAwait(false).",
                ))
            continue

        for match in await_matches:
            word, expression_start = next_word(masked, match.end())
            if word == "using":
                # The async-disposal continuation is compiler-generated. Rewriting `await using var`
                # to ConfiguredAsyncDisposable changes the local variable type and can hide members
                # such as IServiceScope.ServiceProvider. It is an explicit, reviewed exception.
                totals["async_disposals"] += 1
                continue
            if word == "foreach":
                totals["async_streams"] += 1
                header = scan_foreach_header(masked, expression_start)
                if not configure_false_pattern.search(header):
                    findings.append(Finding(
                        relative,
                        line_number(text, match.start()),
                        "Context-free await foreach must configure the async enumerable with ConfigureAwait(false).",
                    ))
                continue

            expression = scan_await_expression(masked, expression_start)
            if configure_false_pattern.search(expression):
                continue
            # A ConfiguredTaskAwaitable is already configured by the caller and has no second
            # ConfigureAwait method. This legacy extension boundary is semantically compliant.
            if "configuredTaskAwaitable" in expression:
                continue
            findings.append(Finding(
                relative,
                line_number(text, match.start()),
                "Context-free await must use ConfigureAwait(false).",
            ))

    return findings, totals


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-root", required=True)
    arguments = parser.parse_args()
    source_root = Path(arguments.source_root).resolve()
    if not source_root.is_dir():
        print(f"Async continuation source root is missing: {source_root}", file=sys.stderr)
        return 2

    findings, totals = audit(source_root)
    if findings:
        print("Async continuation validation failed:")
        for finding in findings:
            print(f"  - {finding.relative_path}:{finding.line}: {finding.message}")
        print(f"Async continuation validation failed with {len(findings)} problem(s).")
        return 1

    print(
        "Async continuation validation passed for "
        f"{totals['files']} source files ({totals['awaits']} await tokens, "
        f"{totals['configure_false']} ConfigureAwait(false), "
        f"{totals['configure_true']} ConfigureAwait(true), "
        f"{totals['async_disposals']} reviewed await-using disposals, "
        f"{totals['async_streams']} configured async streams)."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
