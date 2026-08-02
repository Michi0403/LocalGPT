#!/usr/bin/env python3
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
import argparse
import re
import sys


@dataclass(frozen=True)
class Token:
    kind: str
    value: str
    start: int
    end: int


@dataclass(frozen=True)
class Finding:
    relative_path: str
    line: int
    message: str


_MULTI = sorted(
    [
        "??=", "<<=", ">>=", "?.", "??", "=>", "::", "==", "!=", "<=", ">=",
        "&&", "||", "++", "--", "<<", ">>", "..", "+=", "-=", "*=", "/=",
        "%=", "&=", "|=", "^=", "->",
    ],
    key=len,
    reverse=True,
)


def tokenize(text: str) -> list[Token]:
    tokens: list[Token] = []
    index = 0
    length = len(text)
    while index < length:
        current = text[index]
        if current.isspace():
            index += 1
            continue
        if current == "/" and index + 1 < length and text[index + 1] == "/":
            newline = text.find("\n", index + 2)
            index = length if newline < 0 else newline + 1
            continue
        if current == "/" and index + 1 < length and text[index + 1] == "*":
            closing = text.find("*/", index + 2)
            index = length if closing < 0 else closing + 2
            continue
        if current in "@$" and index + 1 < length and text[index + 1] in '@$"':
            prefix_end = index
            while prefix_end < length and text[prefix_end] in "@$":
                prefix_end += 1
            if prefix_end < length and text[prefix_end] == '"':
                quote_count = 1
                while prefix_end + quote_count < length and text[prefix_end + quote_count] == '"':
                    quote_count += 1
                if quote_count >= 3:
                    cursor = prefix_end + quote_count
                    marker = '"' * quote_count
                    while cursor < length and not text.startswith(marker, cursor):
                        cursor += 1
                    cursor = min(length, cursor + quote_count)
                    tokens.append(Token("string", text[index:cursor], index, cursor))
                    index = cursor
                    continue
                verbatim = "@" in text[index:prefix_end]
                cursor = prefix_end + 1
                while cursor < length:
                    if verbatim:
                        if text[cursor] == '"':
                            if cursor + 1 < length and text[cursor + 1] == '"':
                                cursor += 2
                                continue
                            cursor += 1
                            break
                        cursor += 1
                    else:
                        if text[cursor] == "\\":
                            cursor += 2
                            continue
                        if text[cursor] == '"':
                            cursor += 1
                            break
                        cursor += 1
                tokens.append(Token("string", text[index:cursor], index, cursor))
                index = cursor
                continue
        if current == '"':
            quote_count = 1
            while index + quote_count < length and text[index + quote_count] == '"':
                quote_count += 1
            if quote_count >= 3:
                cursor = index + quote_count
                marker = '"' * quote_count
                while cursor < length and not text.startswith(marker, cursor):
                    cursor += 1
                cursor = min(length, cursor + quote_count)
            else:
                cursor = index + 1
                while cursor < length:
                    if text[cursor] == "\\":
                        cursor += 2
                        continue
                    if text[cursor] == '"':
                        cursor += 1
                        break
                    cursor += 1
            tokens.append(Token("string", text[index:cursor], index, cursor))
            index = cursor
            continue
        if current == "'":
            cursor = index + 1
            while cursor < length:
                if text[cursor] == "\\":
                    cursor += 2
                    continue
                if text[cursor] == "'":
                    cursor += 1
                    break
                cursor += 1
            tokens.append(Token("char", text[index:cursor], index, cursor))
            index = cursor
            continue
        if current.isalpha() or current == "_":
            cursor = index + 1
            while cursor < length and (text[cursor].isalnum() or text[cursor] == "_"):
                cursor += 1
            tokens.append(Token("identifier", text[index:cursor], index, cursor))
            index = cursor
            continue
        if current.isdigit():
            cursor = index + 1
            while cursor < length and (text[cursor].isalnum() or text[cursor] in "._"):
                cursor += 1
            tokens.append(Token("number", text[index:cursor], index, cursor))
            index = cursor
            continue
        operator = next((candidate for candidate in _MULTI if text.startswith(candidate, index)), None)
        if operator is not None:
            tokens.append(Token("operator", operator, index, index + len(operator)))
            index += len(operator)
            continue
        tokens.append(Token("punctuation", current, index, index + 1))
        index += 1
    return tokens


def line_number(text: str, offset: int) -> int:
    return text.count("\n", 0, offset) + 1


def matching_end(tokens: list[Token], index: int, opening: str, closing: str) -> int:
    depth = 0
    for cursor in range(index, len(tokens)):
        value = tokens[cursor].value
        if value == opening:
            depth += 1
        elif value == closing:
            depth -= 1
            if depth == 0:
                return cursor + 1
    return index + 1


def maybe_generic_end(tokens: list[Token], index: int) -> int:
    if index >= len(tokens) or tokens[index].value != "<":
        return index
    depth = 0
    for cursor in range(index, len(tokens)):
        value = tokens[cursor].value
        if value == "<":
            depth += 1
        elif value == ">":
            depth -= 1
            if depth == 0:
                following = tokens[cursor + 1].value if cursor + 1 < len(tokens) else ""
                return cursor + 1 if following in {"(", ".", "?.", "[", "!"} else index
        elif value in {";", "{", "}"} and depth == 1:
            return index
    return index


def parse_postfix(tokens: list[Token], index: int) -> int:
    while index < len(tokens):
        value = tokens[index].value
        if value in {".", "?.", "::"}:
            if index + 1 >= len(tokens):
                return index
            index += 2
            index = maybe_generic_end(tokens, index)
            continue
        if value == "(":
            index = matching_end(tokens, index, "(", ")")
            continue
        if value == "[":
            index = matching_end(tokens, index, "[", "]")
            continue
        if value == "!":
            index += 1
            continue
        break
    return index


def parse_awaited_expression(tokens: list[Token], index: int) -> int:
    if index >= len(tokens):
        return index
    value = tokens[index].value
    if value in {"+", "-", "!", "~", "^", "*", "&"}:
        return parse_postfix(tokens, parse_awaited_expression(tokens, index + 1))
    if value == "await":
        return parse_postfix(tokens, parse_awaited_expression(tokens, index + 1))
    if value == "(":
        closing = matching_end(tokens, index, "(", ")")
        if closing < len(tokens) and tokens[closing].value not in {
            ".", "?.", "(", "[", "!", ";", ",", ")", "]", "}", "?", ":", "??"
        }:
            type_tokens = [token.value for token in tokens[index + 1 : closing - 1]]
            if type_tokens and all(
                re.match(r"^[A-Za-z_]\w*$", token) or token in {".", "?", "[", "]", "<", ">", ",", "::"}
                for token in type_tokens
            ):
                return parse_postfix(tokens, parse_awaited_expression(tokens, closing))
        return parse_postfix(tokens, closing)
    if value == "new":
        cursor = index + 1
        angle_depth = 0
        while cursor < len(tokens):
            token = tokens[cursor].value
            if token == "<":
                angle_depth += 1
            elif token == ">":
                angle_depth = max(0, angle_depth - 1)
            if angle_depth == 0 and token in {"(", "[", "{"}:
                break
            if angle_depth == 0 and token in {";", ",", ")", "]"}:
                break
            cursor += 1
        if cursor < len(tokens) and tokens[cursor].value == "(":
            cursor = matching_end(tokens, cursor, "(", ")")
        while cursor < len(tokens) and tokens[cursor].value == "[":
            cursor = matching_end(tokens, cursor, "[", "]")
        if cursor < len(tokens) and tokens[cursor].value == "{":
            cursor = matching_end(tokens, cursor, "{", "}")
        return parse_postfix(tokens, cursor)
    cursor = maybe_generic_end(tokens, index + 1)
    return parse_postfix(tokens, cursor)


def method_ranges(tokens: list[Token], method_name: str) -> list[tuple[int, int]]:
    ranges: list[tuple[int, int]] = []
    for index, token in enumerate(tokens):
        if token.value != method_name:
            continue
        cursor = index + 1
        while cursor < len(tokens) and tokens[cursor].value not in {"{", ";"}:
            cursor += 1
        if cursor < len(tokens) and tokens[cursor].value == "{":
            closing = matching_end(tokens, cursor, "{", "}")
            ranges.append((tokens[cursor].start, tokens[closing - 1].end))
    return ranges


def source_files(source_root: Path):
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
        "configured_awaitables": 0,
        "async_disposals": 0,
        "async_streams": 0,
    }
    configuration_pattern = re.compile(r"\.ConfigureAwait\s*\(\s*(true|false)\s*\)\s*!?\s*$")

    for path in source_files(source_root):
        text = path.read_text(encoding="utf-8-sig", errors="replace")
        tokens = tokenize(text)
        if not any(token.value == "await" for token in tokens):
            continue
        relative = path.relative_to(source_root).as_posix()
        on_after_render_ranges = method_ranges(tokens, "OnAfterRenderAsync")
        totals["files"] += 1

        for index, token in enumerate(tokens):
            if token.value != "await":
                continue
            totals["awaits"] += 1
            if index + 1 >= len(tokens):
                findings.append(Finding(relative, line_number(text, token.start), "Await token has no following expression."))
                continue
            following = tokens[index + 1].value
            if following == "using":
                totals["async_disposals"] += 1
                continue
            if following == "foreach":
                totals["async_streams"] += 1
                opening = index + 2
                while opening < len(tokens) and tokens[opening].value != "(":
                    opening += 1
                header_end = matching_end(tokens, opening, "(", ")") if opening < len(tokens) else opening
                header_text = text[tokens[index + 1].start : tokens[header_end - 1].end] if header_end > opening else ""
                if not re.search(r"\.ConfigureAwait\s*\(\s*false\s*\)", header_text):
                    findings.append(Finding(
                        relative,
                        line_number(text, token.start),
                        "Await foreach must configure its async enumerable with ConfigureAwait(false).",
                    ))
                continue

            expression_end = parse_awaited_expression(tokens, index + 1)
            if expression_end <= index + 1:
                findings.append(Finding(relative, line_number(text, token.start), "Could not parse awaited expression."))
                continue
            expression = text[tokens[index + 1].start : tokens[expression_end - 1].end]
            configuration = configuration_pattern.search(expression)
            in_on_after_render = any(start <= token.start <= end for start, end in on_after_render_ranges)
            if configuration is None:
                if "configuredTaskAwaitable" in expression:
                    totals["configured_awaitables"] += 1
                    continue
                findings.append(Finding(
                    relative,
                    line_number(text, token.start),
                    "Every await expression must explicitly use ConfigureAwait(false), except renderer-affine OnAfterRenderAsync continuations which must use ConfigureAwait(true).",
                ))
                continue

            uses_true = configuration.group(1) == "true"
            totals["configure_true" if uses_true else "configure_false"] += 1
            if in_on_after_render and not uses_true:
                findings.append(Finding(
                    relative,
                    line_number(text, token.start),
                    "OnAfterRenderAsync continuation must explicitly retain the renderer context with ConfigureAwait(true).",
                ))
            elif not in_on_after_render and uses_true:
                findings.append(Finding(
                    relative,
                    line_number(text, token.start),
                    "ConfigureAwait(true) is allowed only inside OnAfterRenderAsync; use ConfigureAwait(false) here.",
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
        f"{totals['configure_true']} renderer-affine ConfigureAwait(true), "
        f"{totals['configured_awaitables']} preconfigured awaitables, "
        f"{totals['async_disposals']} reviewed await-using disposals, "
        f"{totals['async_streams']} configured async streams)."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
