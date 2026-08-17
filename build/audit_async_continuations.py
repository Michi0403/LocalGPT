#!/usr/bin/env python3
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
import argparse
import json
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
    """Return declaration body ranges for one method name, excluding invocation sites."""
    ranges: list[tuple[int, int]] = []
    for index, token in enumerate(tokens):
        if token.value != method_name or index + 1 >= len(tokens) or tokens[index + 1].value != "(":
            continue
        if index > 0 and tokens[index - 1].value in {".", "?.", "new", "nameof"}:
            continue

        parameter_end = matching_end(tokens, index + 1, "(", ")")
        if parameter_end <= index + 2:
            continue
        cursor = parameter_end
        # Skip nullable/constraint tokens until the declaration body or expression body starts.
        while cursor < len(tokens) and tokens[cursor].value not in {"{", "=>", ";"}:
            if tokens[cursor].value in {".", "?."}:
                break
            cursor += 1
        if cursor >= len(tokens):
            continue
        if tokens[cursor].value == "{":
            closing = matching_end(tokens, cursor, "{", "}")
            ranges.append((tokens[cursor].start, tokens[closing - 1].end))
        elif tokens[cursor].value == "=>":
            ending = cursor + 1
            nesting = 0
            while ending < len(tokens):
                value = tokens[ending].value
                if value in {"(", "[", "{"}:
                    nesting += 1
                elif value in {")", "]", "}"}:
                    nesting = max(0, nesting - 1)
                elif value == ";" and nesting == 0:
                    ranges.append((tokens[cursor].start, tokens[ending].end))
                    break
                ending += 1
    return ranges




def find_matching_brace_in_source(text: str, opening_index: int) -> int:
    depth = 0
    index = opening_index
    length = len(text)
    state = "code"
    raw_quote_count = 0
    while index < length:
        current = text[index]
        following = text[index + 1] if index + 1 < length else ""
        if state == "line_comment":
            if current == "\n":
                state = "code"
            index += 1
            continue
        if state == "block_comment":
            if current == "*" and following == "/":
                state = "code"
                index += 2
            else:
                index += 1
            continue
        if state == "string":
            if current == "\\":
                index += 2
                continue
            if current == '"':
                state = "code"
            index += 1
            continue
        if state == "verbatim_string":
            if current == '"':
                if following == '"':
                    index += 2
                    continue
                state = "code"
            index += 1
            continue
        if state == "char":
            if current == "\\":
                index += 2
                continue
            if current == "'":
                state = "code"
            index += 1
            continue
        if state == "raw_string":
            marker = '"' * raw_quote_count
            if text.startswith(marker, index):
                state = "code"
                index += raw_quote_count
            else:
                index += 1
            continue

        if current == "/" and following == "/":
            state = "line_comment"
            index += 2
            continue
        if current == "/" and following == "*":
            state = "block_comment"
            index += 2
            continue
        if current == "@" and following == '"':
            state = "verbatim_string"
            index += 2
            continue
        if current in {"$", "@"}:
            cursor = index
            while cursor < length and text[cursor] in {"$", "@"}:
                cursor += 1
            quote_count = 0
            while cursor + quote_count < length and text[cursor + quote_count] == '"':
                quote_count += 1
            if quote_count >= 3:
                state = "raw_string"
                raw_quote_count = quote_count
                index = cursor + quote_count
                continue
            if quote_count == 1:
                state = "verbatim_string" if "@" in text[index:cursor] else "string"
                index = cursor + 1
                continue
        if current == '"':
            quote_count = 1
            while index + quote_count < length and text[index + quote_count] == '"':
                quote_count += 1
            if quote_count >= 3:
                state = "raw_string"
                raw_quote_count = quote_count
                index += quote_count
            else:
                state = "string"
                index += 1
            continue
        if current == "'":
            state = "char"
            index += 1
            continue
        if current == "{":
            depth += 1
        elif current == "}":
            depth -= 1
            if depth == 0:
                return index + 1
        index += 1
    return length


def csharp_regions(path: Path, text: str) -> list[tuple[str, int]]:
    if path.suffix.lower() != ".razor":
        return [(text, 0)]
    regions: list[tuple[str, int]] = []
    for match in re.finditer(r"(?m)^\s*@(code|functions)\s*\{", text):
        opening = text.find("{", match.start(), match.end())
        if opening < 0:
            continue
        end = find_matching_brace_in_source(text, opening)
        regions.append((text[opening + 1 : max(opening + 1, end - 1)], opening + 1))
    return regions


def source_files(source_root: Path):
    for path in source_root.rglob("*"):
        if not path.is_file() or path.suffix.lower() not in {".cs", ".razor"}:
            continue
        if any(part in {"bin", "obj", ".git", ".vs"} for part in path.parts):
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
        "async_disposals_false": 0,
        "async_disposals_true": 0,
        "async_streams": 0,
    }
    configuration_pattern = re.compile(r"\.ConfigureAwait\s*\(\s*(true|false)\s*\)\s*!?\s*$")
    policy_path = source_root.parents[1] / "build" / "async-continuation-policy.json"
    baseline = json.loads(policy_path.read_text(encoding="utf-8-sig"))
    lifecycle_names = tuple(baseline.get(
        "rendererAffineLifecycleMethods",
        ("OnInitializedAsync", "OnParametersSetAsync", "OnAfterRenderAsync"),
    ))
    helper_methods_by_file = {
        str(relative): tuple(methods)
        for relative, methods in baseline.get("rendererAffineHelperMethods", {}).items()
    }
    discovered_helper_methods: dict[str, set[str]] = {
        relative: set() for relative in helper_methods_by_file
    }

    for relative in helper_methods_by_file:
        if not (source_root / relative).is_file():
            findings.append(Finding(relative, 1, "Renderer-affine helper baseline references a missing component file."))

    for path in source_files(source_root):
        text = path.read_text(encoding="utf-8-sig", errors="replace")
        relative = path.relative_to(source_root).as_posix()
        regions = csharp_regions(path, text)
        if not regions and path.suffix.lower() != ".razor":
            continue
        file_has_await = False
        is_component = relative.startswith("Components/")

        for region_text, region_offset in regions:
            tokens = tokenize(region_text)
            if not any(token.value == "await" for token in tokens):
                continue
            file_has_await = True
            lifecycle_ranges: list[tuple[int, int]] = []
            for lifecycle_name in lifecycle_names:
                lifecycle_ranges.extend(method_ranges(tokens, lifecycle_name))
            renderer_loading_ranges: list[tuple[int, int]] = []
            for helper_name in helper_methods_by_file.get(relative, ()):
                helper_ranges = method_ranges(tokens, helper_name)
                if helper_ranges:
                    discovered_helper_methods.setdefault(relative, set()).add(helper_name)
                    renderer_loading_ranges.extend(helper_ranges)

            for index, token in enumerate(tokens):
                if token.value != "await":
                    continue
                totals["awaits"] += 1
                absolute_start = region_offset + token.start
                current_line = line_number(text, absolute_start)
                if index + 1 >= len(tokens):
                    findings.append(Finding(relative, current_line, "Await token has no following expression."))
                    continue

                following = tokens[index + 1].value
                if following == "using":
                    totals["async_disposals"] += 1
                    line_start = region_text.rfind("\n", 0, token.start) + 1
                    line_end = region_text.find("\n", token.start)
                    if line_end < 0:
                        line_end = len(region_text)
                    using_line = region_text[line_start:line_end]
                    using_configuration = re.search(r"\.ConfigureAwait\s*\(\s*(true|false)\s*\)", using_line)
                    if using_configuration is None:
                        findings.append(Finding(
                            relative,
                            current_line,
                            "Every await using construct must explicitly configure asynchronous disposal with ConfigureAwait(true/false).",
                        ))
                        continue
                    using_true = using_configuration.group(1) == "true"
                    totals["async_disposals_true" if using_true else "async_disposals_false"] += 1
                    if using_true and not is_component:
                        findings.append(Finding(
                            relative,
                            current_line,
                            "ConfigureAwait(true) on async disposal is forbidden outside Components; use ConfigureAwait(false).",
                        ))
                    continue
                if following == "foreach":
                    totals["async_streams"] += 1
                    opening = index + 2
                    while opening < len(tokens) and tokens[opening].value != "(":
                        opening += 1
                    header_end = matching_end(tokens, opening, "(", ")") if opening < len(tokens) else opening
                    header_text = region_text[tokens[index + 1].start : tokens[header_end - 1].end] if header_end > opening else ""
                    if not re.search(r"\.ConfigureAwait\s*\(\s*false\s*\)", header_text):
                        findings.append(Finding(
                            relative,
                            current_line,
                            "Await foreach must configure its async enumerable with ConfigureAwait(false).",
                        ))
                    continue

                expression_end = parse_awaited_expression(tokens, index + 1)
                if expression_end <= index + 1:
                    findings.append(Finding(relative, current_line, "Could not parse awaited expression."))
                    continue
                expression = region_text[tokens[index + 1].start : tokens[expression_end - 1].end]
                configuration = configuration_pattern.search(expression)
                if configuration is None:
                    findings.append(Finding(
                        relative,
                        current_line,
                        "Every ordinary await expression must explicitly use ConfigureAwait(true/false).",
                    ))
                    continue

                uses_true = configuration.group(1) == "true"
                totals["configure_true" if uses_true else "configure_false"] += 1
                if not uses_true:
                    continue

                in_lifecycle = any(start <= token.start <= end for start, end in lifecycle_ranges)
                in_renderer_loading_helper = any(
                    start <= token.start <= end for start, end in renderer_loading_ranges
                )
                if not is_component:
                    findings.append(Finding(
                        relative,
                        current_line,
                        "ConfigureAwait(true) is forbidden outside Components; use ConfigureAwait(false).",
                    ))
                elif not in_lifecycle and not in_renderer_loading_helper:
                    findings.append(Finding(
                        relative,
                        current_line,
                        "ConfigureAwait(true) is allowed only in a Blazor lifecycle method or an exact renderer-affine loading helper listed in async-continuation-policy.json.",
                    ))

        if path.suffix.lower() == ".razor":
            code_starts = [match.start() for match in re.finditer(r"(?m)^\s*@(code|functions)\s*\{", text)]
            markup_end = min(code_starts) if code_starts else len(text)
            for markup_match in re.finditer(r"\bawait\s+[^;\"]+", text[:markup_end]):
                markup_expression = markup_match.group(0)
                markup_line = line_number(text, markup_match.start())
                totals["awaits"] += 1
                markup_configuration = re.search(r"\.ConfigureAwait\s*\(\s*(true|false)\s*\)", markup_expression)
                if markup_configuration is None:
                    findings.append(Finding(
                        relative,
                        markup_line,
                        "Every Razor markup await must explicitly use ConfigureAwait(true/false).",
                    ))
                else:
                    markup_true = markup_configuration.group(1) == "true"
                    totals["configure_true" if markup_true else "configure_false"] += 1
                    if markup_true and "@on" not in text[text.rfind("<", 0, markup_match.start()) : markup_match.start()]:
                        findings.append(Finding(
                            relative,
                            markup_line,
                            "ConfigureAwait(true) in Razor markup is reserved for connected UI event flows.",
                        ))

        if file_has_await:
            totals["files"] += 1

    for relative, expected_methods in helper_methods_by_file.items():
        discovered = discovered_helper_methods.get(relative, set())
        for missing_method in sorted(set(expected_methods) - discovered):
            findings.append(Finding(
                relative,
                1,
                f"Renderer-affine helper baseline method '{missing_method}' was not found as a method declaration.",
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
        f"{totals['async_disposals']} explicitly configured await-using disposals "
        f"({totals['async_disposals_false']} false, {totals['async_disposals_true']} true), "
        f"{totals['async_streams']} configured async streams)."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
