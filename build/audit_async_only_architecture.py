#!/usr/bin/env python3
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
import argparse
import bisect
from functools import lru_cache
import re
import sys


@dataclass(frozen=True)
class Finding:
    relative_path: str
    line: int
    column: int
    code: str
    message: str


ASYNC_RETURN_NAMES = {
    "Task",
    "ValueTask",
    "IAsyncEnumerable",
    "IAsyncEnumerator",
    "ConfiguredTaskAwaitable",
    "ConfiguredValueTaskAwaitable",
}

LANGUAGE_INVOCATIONS = {"nameof", "typeof", "sizeof", "default", "checked", "unchecked"}

TYPE_PATTERN = r"(?:ref\s+(?:readonly\s+)?)?(?:\([^\r\n()]+\)|[A-Za-z_]\w*(?:(?:\.|::)[A-Za-z_]\w*)*(?:\s*<[^;{}()\r\n=]+>)?(?:\s*\[\s*\])?\??)"
MODIFIER_PATTERN = r"(?:(?:public|private|protected|internal|static|virtual|override|sealed|partial|new|unsafe|extern|async|readonly)\s+)*"
METHOD_PATTERN = re.compile(
    rf"^\s*(?P<mods>{MODIFIER_PATTERN})(?P<return>{TYPE_PATTERN})\s+(?P<name>(?:[A-Za-z_]\w*\.)*[A-Za-z_]\w*)\s*(?:<[^;{{}}()\r\n=]+>)?\s*\("
)
PROPERTY_EXPRESSION_PATTERN = re.compile(
    rf"^\s*(?P<mods>(?:(?:public|private|protected|internal|static|virtual|override|sealed|new|readonly|required)\s+)+)(?P<type>{TYPE_PATTERN})\s+(?P<name>[A-Za-z_]\w*)\s*=>\s*(?P<expr>[^;]+);"
)
INVOCATION_PATTERN = re.compile(r"(?P<name>[A-Za-z_]\w*)\s*(?:<[^;{}()\r\n=]+>)?\s*\(")


def mask_non_code(text: str) -> str:
    """Replace comments/string/char contents with spaces while preserving offsets/newlines."""
    chars = list(text)
    i = 0
    n = len(chars)
    while i < n:
        if i + 1 < n and chars[i] == "/" and chars[i + 1] == "/":
            j = i
            while j < n and chars[j] != "\n":
                chars[j] = " "
                j += 1
            i = j
            continue
        if i + 1 < n and chars[i] == "/" and chars[i + 1] == "*":
            j = i
            while j + 1 < n and not (chars[j] == "*" and chars[j + 1] == "/"):
                if chars[j] != "\n":
                    chars[j] = " "
                j += 1
            if j + 1 < n:
                if chars[j] != "\n": chars[j] = " "
                if chars[j + 1] != "\n": chars[j + 1] = " "
                j += 2
            i = j
            continue

        prefix_start = i
        if chars[i] in {"@", "$"}:
            j = i
            while j < n and chars[j] in {"@", "$"}:
                j += 1
            if j < n and chars[j] == '"':
                i = j
            else:
                i = prefix_start
        if chars[i] == '"':
            quote_count = 1
            while i + quote_count < n and chars[i + quote_count] == '"':
                quote_count += 1
            raw = quote_count >= 3
            verbatim = "@" in text[prefix_start:i]
            j = i + quote_count
            if raw:
                marker = '"' * quote_count
                end = text.find(marker, j)
                j = n if end < 0 else end + quote_count
            else:
                while j < n:
                    if not verbatim and chars[j] == "\\":
                        if chars[j] != "\n": chars[j] = " "
                        if j + 1 < n and chars[j + 1] != "\n": chars[j + 1] = " "
                        j += 2
                        continue
                    if chars[j] == '"':
                        if verbatim and j + 1 < n and chars[j + 1] == '"':
                            if chars[j] != "\n": chars[j] = " "
                            if chars[j + 1] != "\n": chars[j + 1] = " "
                            j += 2
                            continue
                        j += 1
                        break
                    j += 1
            for k in range(prefix_start, min(j, n)):
                if chars[k] != "\n": chars[k] = " "
            i = j
            continue
        if chars[i] == "'":
            j = i + 1
            while j < n:
                if chars[j] == "\\":
                    j += 2
                    continue
                if chars[j] == "'":
                    j += 1
                    break
                j += 1
            for k in range(i, min(j, n)):
                if chars[k] != "\n": chars[k] = " "
            i = j
            continue
        i += 1
    return "".join(chars)


@lru_cache(maxsize=8)
def line_starts(text: str) -> tuple[int, ...]:
    starts = [0]
    starts.extend(match.end() for match in re.finditer("\n", text))
    return tuple(starts)


def location(text: str, offset: int) -> tuple[int, int]:
    starts = line_starts(text)
    index = bisect.bisect_right(starts, offset) - 1
    index = max(0, index)
    return index + 1, offset - starts[index] + 1


def razor_declaration_code(masked_text: str) -> str:
    """Keep only Razor @code/@functions blocks while preserving source offsets/newlines."""
    output = ["\n" if char == "\n" else " " for char in masked_text]
    for match in re.finditer(r"@(?:code|functions)\s*\{", masked_text):
        opening = masked_text.find("{", match.start(), match.end())
        if opening < 0:
            continue
        depth = 0
        end = len(masked_text)
        for index in range(opening, len(masked_text)):
            char = masked_text[index]
            if char == "{": depth += 1
            elif char == "}":
                depth -= 1
                if depth == 0:
                    end = index + 1
                    break
        for index in range(match.start(), end):
            output[index] = masked_text[index]
    return "".join(output)


def normalize_return_type(return_type: str) -> str:
    value = re.sub(r"\s+", " ", return_type.strip())
    value = re.sub(r"^ref\s+(?:readonly\s+)?", "", value)
    return value.rstrip("?")


def is_async_return_type(return_type: str) -> bool:
    value = normalize_return_type(return_type)
    base = value.split("<", 1)[0].strip().replace("global::", "")
    short = re.split(r"\.|::", base)[-1]
    return short in ASYNC_RETURN_NAMES


def first_async_invocation(expression: str) -> re.Match[str] | None:
    for match in INVOCATION_PATTERN.finditer(expression):
        name = match.group("name")
        if name in LANGUAGE_INVOCATIONS:
            continue
        if name.endswith("Async"):
            return match
    return None


def add_finding(findings: list[Finding], relative: str, text: str, offset: int, code: str, message: str) -> None:
    line, column = location(text, offset)
    findings.append(Finding(relative, line, column, code, message))


def find_async_method_ranges(code: str) -> list[tuple[int, int]]:
    """Return source ranges for methods whose contract is already awaitable/async.

    This is intentionally conservative: it is used only to decide whether a blocking/discard
    pattern occurs inside an asynchronous orchestration path. Missing a range leaves the
    synchronous boundary to the existing service/component ownership audits instead of
    inventing an invalid async rewrite.
    """
    ranges: list[tuple[int, int]] = []
    lines = code.splitlines(keepends=True)
    offsets: list[int] = []
    cursor = 0
    for line_text in lines:
        offsets.append(cursor)
        cursor += len(line_text)
    for index, line_text in enumerate(lines):
        stripped = line_text.lstrip()
        if not stripped or stripped[0] in {"<", "@", "#", "}"}:
            continue
        window = "".join(lines[index:index + 8])[:6000]
        match = METHOD_PATTERN.match(window)
        if match is None:
            continue
        name = match.group("name").split(".")[-1]
        return_type = match.group("return")
        modifiers = match.group("mods")
        if not (is_async_return_type(return_type) or name.endswith("Async") or re.search(r"\basync\b", modifiers)):
            continue
        absolute_start = offsets[index] + match.start()
        search_start = offsets[index] + match.end()
        search_end = min(len(code), search_start + 4000)
        expression = code.find("=>", search_start, search_end)
        opening = code.find("{", search_start, search_end)
        semicolon = code.find(";", search_start, search_end)
        if expression >= 0 and (opening < 0 or expression < opening):
            end = code.find(";", expression, search_end)
            if end >= 0:
                ranges.append((absolute_start, end + 1))
            continue
        if opening < 0 or (semicolon >= 0 and semicolon < opening):
            continue
        depth = 0
        for body_index in range(opening, len(code)):
            char = code[body_index]
            if char == "{":
                depth += 1
            elif char == "}":
                depth -= 1
                if depth == 0:
                    ranges.append((absolute_start, body_index + 1))
                    break
    return ranges


def is_inside_ranges(offset: int, ranges: list[tuple[int, int]]) -> bool:
    return any(start <= offset < end for start, end in ranges)


def scan_file(path: Path, source_root: Path) -> list[Finding]:
    text = path.read_text(encoding="utf-8-sig")
    full_code = mask_non_code(text)
    code = razor_declaration_code(full_code) if path.suffix.lower() == ".razor" else full_code
    relative = path.relative_to(source_root).as_posix()
    findings: list[Finding] = []
    async_method_ranges = find_async_method_ranges(code)
    is_component_source = relative.startswith("Components/")

    lines = code.splitlines(keepends=True)
    offsets: list[int] = []
    cursor = 0
    for line_text in lines:
        offsets.append(cursor)
        cursor += len(line_text)

    seen_methods: set[int] = set()
    seen_properties: set[int] = set()
    for index, line_text in enumerate(lines):
        stripped = line_text.lstrip()
        if not stripped or stripped[0] in {"<", "@", "#", "}"}:
            continue
        window = "".join(lines[index:index + 8])[:6000]
        match = METHOD_PATTERN.match(window)
        if match is not None:
            absolute_name = offsets[index] + match.start("name")
            if absolute_name not in seen_methods:
                seen_methods.add(absolute_name)
                return_type = match.group("return")
                name = match.group("name")
                modifiers = match.group("mods")
                normalized_return = normalize_return_type(return_type)
                declaration_keyword = normalized_return.split("<", 1)[0].split()[0]
                if declaration_keyword not in {"return", "await", "throw", "new", "if", "for", "foreach", "while", "switch", "catch", "using", "lock", "else", "case", "when", "where", "from", "select", "var", "class", "record", "struct", "interface", "delegate", "enum", "yield"}:
                    simple_name = name.split(".")[-1]
                    if normalized_return == "void" and re.search(r"\basync\b", modifiers):
                        add_finding(findings, relative, text, absolute_name, "ASYNC002", f"method '{name}' is async void. Return Task so callers can await and observe completion/failure.")
                    elif simple_name.endswith("Async") and not is_async_return_type(return_type):
                        add_finding(
                            findings, relative, text, absolute_name, "ASYNC001",
                            f"method '{name}' advertises an asynchronous contract but returns '{normalized_return}'. Methods ending in Async must return Task/ValueTask or an async-enumerable contract."
                        )

        prop = PROPERTY_EXPRESSION_PATTERN.match(window)
        if prop is not None:
            absolute_prop = offsets[index] + prop.start("name")
            if absolute_prop not in seen_properties:
                seen_properties.add(absolute_prop)
                invocation = first_async_invocation(prop.group("expr"))
                if invocation is not None:
                    absolute = offsets[index] + prop.start("expr") + invocation.start()
                    add_finding(
                        findings, relative, text, absolute, "ASYNC003",
                        f"computed member '{prop.group('name')}' invokes asynchronous method '{invocation.group('name')}(...)' synchronously. Materialize the value from an awaited lifecycle/service operation before exposing it through a property."
                    )

    for match in re.finditer(r"\bget\s*=>\s*(?P<expr>[^;\r\n]+);", code):
        invocation = first_async_invocation(match.group("expr"))
        if invocation is not None:
            absolute = match.start("expr") + invocation.start()
            add_finding(findings, relative, text, absolute, "ASYNC003", f"expression-bodied getter invokes asynchronous method '{invocation.group('name')}(...)' synchronously. Compute awaited state before exposing it through the getter.")
    for match in re.finditer(r"\bget\s*\{(?P<body>[^{}\r\n]{0,1500})\}", code):
        invocation = first_async_invocation(match.group("body"))
        if invocation is not None:
            absolute = match.start("body") + invocation.start()
            add_finding(findings, relative, text, absolute, "ASYNC003", f"property getter invokes asynchronous method '{invocation.group('name')}(...)' synchronously. Compute awaited state before exposing it through the getter.")

    # Only diagnose .Result when the receiver is syntactically task-like. A blanket '.Result' search
    # misclassifies normal domain members such as functionCall.Result.Result.
    task_result_patterns = [
        re.compile(r"\b[A-Za-z_]\w*(?:Task|ValueTask)\s*\.\s*Result\b"),
        re.compile(r"\b[A-Za-z_]\w*Async\s*\([^;\r\n]*?\)\s*\.\s*Result\b"),
        re.compile(r"\.\s*AsTask\s*\(\s*\)\s*\.\s*Result\b"),
    ]
    for pattern in task_result_patterns:
        for match in pattern.finditer(full_code):
            if is_component_source or is_inside_ranges(match.start(), async_method_ranges):
                add_finding(findings, relative, text, match.start(), "ASYNC004", "synchronous Task/ValueTask '.Result' is forbidden in renderer/asynchronous orchestration paths; await the operation.")

    blocking_patterns: list[tuple[str, re.Pattern[str], str]] = [
        ("ASYNC005", re.compile(r"\.\s*GetAwaiter\s*\(\s*\)\s*\.\s*GetResult\s*\(\s*\)"), "GetAwaiter().GetResult() is forbidden in renderer/asynchronous orchestration paths; await the operation."),
        ("ASYNC006", re.compile(r"\bTask\s*\.\s*Wait(?:All|Any)\s*\("), "Task.WaitAll/WaitAny is forbidden; use Task.WhenAll/WhenAny and await it."),
        ("ASYNC006", re.compile(r"\bThread\s*\.\s*(?:Sleep|Join)\s*\("), "Thread.Sleep/Join is forbidden in asynchronous application ownership paths; use awaitable/cancellation-aware coordination."),
        ("ASYNC006", re.compile(r"\.\s*WaitOne\s*\("), "WaitOne(...) is forbidden in asynchronous application ownership paths; use awaitable/cancellation-aware coordination."),
        ("ASYNC009", re.compile(r"\bMonitor\s*\.\s*(?:Enter|Wait|Pulse|PulseAll)\s*\("), "Monitor blocking coordination is forbidden in asynchronous application ownership paths; use awaitable ownership or a short language-level lock for purely synchronous state."),
    ]
    for rule, pattern, message in blocking_patterns:
        for match in pattern.finditer(full_code):
            if rule == "ASYNC005" and not (is_component_source or is_inside_ranges(match.start(), async_method_ranges)):
                # A genuinely synchronous service/native adapter may need to bridge a synchronous
                # framework/OS contract. Existing service-architecture guards own that boundary.
                continue
            add_finding(findings, relative, text, match.start(), rule, message)

    # Distinguish discard assignment from lambda syntax (_ => ...), and do not flag `_ = await ...`:
    # the awaited operation is observed and only its result value is intentionally discarded.
    discard_pattern = re.compile(r"(?m)(?<![A-Za-z0-9_])_\s*=(?!\s*>|=)\s*(?P<expr>[^;\r\n]+);")
    for match in discard_pattern.finditer(full_code):
        expr = match.group("expr").strip()
        if expr.startswith("await "):
            continue
        if re.search(r"\b[A-Za-z_]\w*Async\s*(?:<[^;(){}]+>)?\s*\(", expr) or re.search(r"\bTask\s*\.\s*Run\s*\(", expr) or re.search(r"\bInvokeAsync\s*\(", expr):
            add_finding(
                findings, relative, text, match.start(), "ASYNC011",
                "discarded asynchronous invocation/fire-and-forget is forbidden. Await it or transfer explicit ownership to the repository's supervised task owner."
            )

    return findings


def source_files(source_root: Path) -> list[Path]:
    roots = [source_root / "Components", source_root / "Services", source_root / "HostedServices", source_root / "Interfaces"]
    files: list[Path] = []
    for root in roots:
        if not root.is_dir():
            continue
        for path in root.rglob("*"):
            if not path.is_file() or any(part in {"bin", "obj"} for part in path.parts):
                continue
            if path.name.endswith(".Designer.cs"):
                continue
            if path.suffix in {".cs", ".razor"}:
                files.append(path)
    return sorted(files)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-root", required=True)
    parser.add_argument("--product", required=True)
    args = parser.parse_args()

    source_root = Path(args.source_root).resolve()
    if not source_root.is_dir():
        print(f"Async-boundary architecture source root does not exist: {source_root}", file=sys.stderr)
        return 2

    findings: list[Finding] = []
    files = source_files(source_root)
    for path in files:
        try:
            findings.extend(scan_file(path, source_root))
        except UnicodeDecodeError as exc:
            relative = path.relative_to(source_root).as_posix()
            findings.append(Finding(relative, 1, 1, "ASYNC900", f"source could not be decoded as UTF-8: {exc}"))

    unique = sorted(set(findings), key=lambda f: (f.relative_path.lower(), f.line, f.column, f.code, f.message))
    if unique:
        for finding in unique:
            print(f"{source_root / finding.relative_path}({finding.line},{finding.column}): error {finding.code}: {finding.message}")
        print(f"Async-boundary component/service architecture validation found {len(unique)} violation(s) in {len(files)} reviewed {args.product} source file(s). The guard is zero-baseline for the contracts it can determine reliably from source.")
        return 1

    print(f"Async-boundary component/service architecture validation passed for {len(files)} {args.product} source file(s): async naming/contracts, renderer/async sync-over-async, discarded asynchronous work, and blocking coordination primitives are consistent.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
