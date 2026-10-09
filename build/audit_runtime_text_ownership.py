#!/usr/bin/env python3
"""Report hardcoded authored runtime text and regex patterns outside LocalGPT's configurable owners.

The guard is intentionally baseline-free. Existing findings are reported so they can be migrated,
and future source cannot silently add another hidden prompt/text/regex policy lane.
"""
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
import argparse
import re
import sys


@dataclass(frozen=True)
class TypeDecl:
    name: str
    kind: str
    open: int
    close: int


@dataclass(frozen=True)
class MethodDecl:
    name: str
    start: int
    body_start: int
    end: int
    type_name: str


@dataclass(frozen=True)
class StringToken:
    start: int
    end: int
    text: str
    interpolated: bool
    raw: bool


TYPE_RE = re.compile(
    r'''(?mx)^[ \t]*(?:\[[^\]\n]+\][ \t]*)*'''
    r'''(?:(?:public|internal|private|protected|file|sealed|abstract|partial|new|readonly|unsafe)\s+)*'''
    r'''(?P<kind>class|record\s+class|record|struct)\s+(?P<name>[A-Za-z_]\w*)(?P<tail>[^;{]*?)\{''')
METHOD_RE = re.compile(
    r'''(?mx)^[ \t]*(?:\[[^\]\n]+\][ \t]*\r?\n[ \t]*)*'''
    r'''(?P<signature>(?P<access>public|private|protected|internal)\s+'''
    r'''(?P<modifiers>(?:(?:static|async|virtual|override|sealed|partial|new|unsafe|extern|required)\s+)*)'''
    r'''(?P<return>[A-Za-z_][\w\.\?<>,\[\]\s:]*(?:\s*\*)?)\s+'''
    r'''(?P<name>[A-Za-z_]\w*)\s*\([^;{}]*?\)\s*'''
    r'''(?:where\s+[^\{=>\r\n]+\s*)?)(?P<body>=>|\{)''')

PROMPT_HINTS = re.compile(
    r'(?i)(prompt|instruction|briefing|systemmessage|system_message|system prompt|role synthesis|peer review instruction|assistant policy)')
SEMANTIC_ASSIGNMENT = re.compile(
    r'(?i)(?:Prompt|Instruction|Briefing|Content|VisibleContent|Role|Title|Description|Message|Warning|Status|Phase|ModelName|Reason|Summary|Label|Tooltip|Placeholder|Question|Progress|Notification|Response|ResultText|DisplayName)\s*=\s*$')
SEMANTIC_CALL = re.compile(
    r'(?i)(?:Warnings?\.Add|ProgressMessage\?*\.Invoke|Notification\w*\s*\(|Notify\w*\s*\(|'
    r'new\s+(?:InvalidOperationException|InvalidDataException|KeyNotFoundException|ArgumentException)\s*\()')
LOG_CONTEXT = re.compile(
    r'(?i)\b(?:logger|_logger|Logger)\s*\.\s*Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(')
REGEX_CALL_LEFT = re.compile(
    r'(?i)(?:new\s+Regex\s*\(|Regex\.(?:Match|Matches|IsMatch|Replace|Split)\s*\(|'
    r'(?:regex|regexCompilation|regexCompiler)\s*\.\s*Compile\s*\(|CompileRegex\s*\()')
PATTERN_ASSIGN_LEFT = re.compile(r'(?i)(?:Regex|Pattern)[A-Za-z0-9_]*\s*(?:=|,)\s*$')
PROMPT_FIELD_LEFT = re.compile(r'(?i)(?:Prompt|Instruction|Briefing|SystemMessage|SystemPrompt)[A-Za-z0-9_]*\s*(?:=|=>)\s*$')
REGEXISH = re.compile(r'\\[AbBdDsSwWZz]|\(\?|\[[^\]]*\]|\^|\$|\.\*|\.\+|\{\d|\(\?:|\(\?<')

# These are the resettable/configurable seed owners. Prompt prose and regex patterns are allowed
# here because they are copied into database-backed BusinessObjects rather than hidden in runtime orchestration.
OWNER_PREFIXES = (
    'Services/Persistence/InitialDataCatalog',
    'Services/Persistence/LocalGptRuntimePolicySeedDataService',
    'Services/OrganicCouncilBlueprintSeedDataService',
)
REGEX_OWNER_PREFIXES = OWNER_PREFIXES + (
    'Services/Persistence/CouncilTextPatternDataService',
    'Services/Persistence/RegexPatternService',
    'Services/RegexCompilationService',
)


def line_of(text: str, pos: int) -> int:
    return text.count('\n', 0, pos) + 1


def mask_csharp(text: str) -> str:
    out = list(text)
    i = 0
    n = len(text)
    state = 'code'
    raw_quotes = 0
    while i < n:
        c = text[i]
        nxt = text[i + 1] if i + 1 < n else ''
        if state == 'code':
            if c == '/' and nxt == '/':
                out[i] = out[i + 1] = ' '
                i += 2
                state = 'line'
                continue
            if c == '/' and nxt == '*':
                out[i] = out[i + 1] = ' '
                i += 2
                state = 'block'
                continue
            if c == '"':
                q = 1
                while i + q < n and text[i + q] == '"':
                    q += 1
                if q >= 3:
                    raw_quotes = q
                    for j in range(i, i + q):
                        out[j] = ' '
                    i += q
                    state = 'raw'
                    continue
                out[i] = ' '
                i += 1
                state = 'string'
                continue
            if c == "'":
                out[i] = ' '
                i += 1
                state = 'char'
                continue
            i += 1
            continue
        if state == 'line':
            if c == '\n':
                state = 'code'
            else:
                out[i] = ' '
            i += 1
            continue
        if state == 'block':
            out[i] = ' '
            if c == '*' and nxt == '/':
                out[i + 1] = ' '
                i += 2
                state = 'code'
            else:
                i += 1
            continue
        if state == 'string':
            out[i] = ' '
            if c == '\\' and i + 1 < n:
                out[i + 1] = ' '
                i += 2
            elif c == '"':
                i += 1
                state = 'code'
            else:
                i += 1
            continue
        if state == 'char':
            out[i] = ' '
            if c == '\\' and i + 1 < n:
                out[i + 1] = ' '
                i += 2
            elif c == "'":
                i += 1
                state = 'code'
            else:
                i += 1
            continue
        if state == 'raw':
            out[i] = ' '
            if c == '"':
                q = 1
                while i + q < n and text[i + q] == '"':
                    q += 1
                if q >= raw_quotes:
                    for j in range(i, min(i + q, n)):
                        out[j] = ' '
                    i += q
                    state = 'code'
                else:
                    i += q
            else:
                i += 1
    return ''.join(out)


def match_brace(masked: str, open_pos: int) -> int:
    depth = 0
    for i in range(open_pos, len(masked)):
        if masked[i] == '{':
            depth += 1
        elif masked[i] == '}':
            depth -= 1
            if depth == 0:
                return i
    return -1


def parse_methods(text: str) -> list[MethodDecl]:
    masked = mask_csharp(text)
    types: list[TypeDecl] = []
    for match in TYPE_RE.finditer(masked):
        open_pos = match.end() - 1
        close_pos = match_brace(masked, open_pos)
        if close_pos >= 0:
            types.append(TypeDecl(match.group('name'), match.group('kind'), open_pos, close_pos))

    raw: list[MethodDecl] = []
    for match in METHOD_RE.finditer(masked):
        candidates = [item for item in types if item.open < match.start() < item.close and not item.kind.startswith('record')]
        if not candidates:
            continue
        owner = min(candidates, key=lambda item: item.close - item.open)
        body_start = match.start('body')
        if match.group('body') == '{':
            end = match_brace(masked, body_start)
        else:
            i = body_start + 2
            par = bracket = brace = 0
            while i < len(masked):
                c = masked[i]
                if c == '(':
                    par += 1
                elif c == ')':
                    par = max(0, par - 1)
                elif c == '[':
                    bracket += 1
                elif c == ']':
                    bracket = max(0, bracket - 1)
                elif c == '{':
                    brace += 1
                elif c == '}' and brace:
                    brace -= 1
                elif c == ';' and par == bracket == brace == 0:
                    break
                i += 1
            end = i
        if end >= 0:
            raw.append(MethodDecl(match.group('name'), match.start(), body_start, end + 1, owner.name))

    return [
        method for method in raw
        if not any(other.start < method.start < other.end and (other.end - other.start) > (method.end - method.start)
                   for other in raw if other is not method)
    ]


def iter_string_tokens(text: str, start: int, end: int):
    i = start
    while i < end:
        if text.startswith('//', i):
            newline = text.find('\n', i + 2)
            i = end if newline < 0 else min(newline + 1, end)
            continue
        if text.startswith('/*', i):
            close = text.find('*/', i + 2)
            i = end if close < 0 else min(close + 2, end)
            continue

        prefix_start = i
        dollars = 0
        while i < end and text[i] == '$':
            dollars += 1
            i += 1
        verbatim = False
        if i < end and text[i] == '@':
            verbatim = True
            i += 1
            if dollars == 0 and i < end and text[i] == '$':
                dollars = 1
                i += 1
        if i >= end or text[i] != '"':
            i = prefix_start + 1
            continue

        quote_count = 1
        while i + quote_count < end and text[i + quote_count] == '"':
            quote_count += 1
        raw = quote_count >= 3
        content_start = i + quote_count
        if raw:
            j = content_start
            while j < end:
                if text[j] == '"':
                    closing = 1
                    while j + closing < end and text[j + closing] == '"':
                        closing += 1
                    if closing >= quote_count:
                        yield StringToken(prefix_start, j + closing, text[content_start:j], dollars > 0, True)
                        i = j + closing
                        break
                    j += closing
                    continue
                j += 1
            else:
                i = end
            continue

        j = content_start
        buffer: list[str] = []
        while j < end:
            c = text[j]
            if verbatim:
                if c == '"':
                    if j + 1 < end and text[j + 1] == '"':
                        buffer.append('"')
                        j += 2
                        continue
                    yield StringToken(prefix_start, j + 1, ''.join(buffer), dollars > 0, False)
                    i = j + 1
                    break
                buffer.append(c)
                j += 1
                continue
            if c == '\\' and j + 1 < end:
                buffer.append(text[j:j + 2])
                j += 2
                continue
            if c == '"':
                yield StringToken(prefix_start, j + 1, ''.join(buffer), dollars > 0, False)
                i = j + 1
                break
            buffer.append(c)
            j += 1
        else:
            i = end


def is_owner(rel: str, prefixes: tuple[str, ...]) -> bool:
    stem = rel[:-3] if rel.endswith('.cs') else rel
    return any(stem.startswith(prefix) for prefix in prefixes)


def meaningful_text(value: str) -> bool:
    if not value or not value.strip():
        return False
    plain = re.sub(r'\{[^{}]*\}', ' ', value)
    return bool(re.search(r'[A-Za-zÀ-ÖØ-öø-ÿ]', plain))


def is_technical_identifier(value: str) -> bool:
    compact = value.strip()
    return bool(compact) and not re.search(r'\s', compact) and bool(re.fullmatch(r'[A-Za-z0-9_.:/+@#-]+', compact))


def authored_word_count(value: str) -> int:
    plain = re.sub(r'\{[^{}]*\}', ' ', value)
    return len(re.findall(r"[A-Za-zÀ-ÖØ-öø-ÿ][A-Za-zÀ-ÖØ-öø-ÿ'-]*", plain))


def candidate_score(token: StringToken) -> tuple[int, int, int, int]:
    return (
        1 if token.raw else 0,
        1 if token.interpolated else 0,
        authored_word_count(token.text),
        len(token.text),
    )


def in_log_call(text: str, token_start: int) -> bool:
    left = text[max(0, token_start - 700):token_start]
    boundary = max(left.rfind(';'), left.rfind('}'), left.rfind('{'))
    return bool(LOG_CONTEXT.search(left[boundary + 1:]))


def likely_runtime_text(value: str, interpolated: bool, raw: bool, left: str, method_name: str) -> bool:
    if not meaningful_text(value):
        return False
    plain = re.sub(r'\{[^{}]*\}', ' ', value)
    word_count = authored_word_count(value)

    # Prompt/instruction builders may use technical keys, but authored prose must be configurable.
    if PROMPT_HINTS.search(method_name) and (raw or word_count >= 2) and not is_technical_identifier(plain):
        return True
    if PROMPT_HINTS.search(left[-140:]) and word_count >= 2 and not is_technical_identifier(plain):
        return True

    # Directly user/model-observable semantic assignments/calls are configuration text.
    if SEMANTIC_ASSIGNMENT.search(left[-160:]) and word_count >= 2 and not is_technical_identifier(plain):
        return True
    if SEMANTIC_CALL.search(left[-260:]) and word_count >= 2 and not is_technical_identifier(plain):
        return True

    # General authored prose, including interpolated operational notices, must not be created ad hoc.
    if raw and ('\n' in value or word_count >= 3):
        return True
    if interpolated and word_count >= 3:
        return True
    if word_count >= 5:
        return True
    if re.search(r'[.!?](?:\s+|$)', plain) and word_count >= 3 and not is_technical_identifier(plain):
        return True
    return False


def snippet(value: str, limit: int = 160) -> str:
    collapsed = re.sub(r'\s+', ' ', value).strip()
    return collapsed if len(collapsed) <= limit else collapsed[:limit - 1] + '…'


def _call_context(text: str, token_start: int, masked: str | None = None):
    """Return (callee_text, open_paren, argument_index) for the nearest enclosing call."""
    if masked is None:
        masked = mask_csharp(text)
    depth = 0
    open_pos = -1
    i = token_start - 1
    while i >= 0:
        c = masked[i]
        if c == ')':
            depth += 1
        elif c == '(':
            if depth == 0:
                open_pos = i
                break
            depth -= 1
        i -= 1
    if open_pos < 0:
        return None

    before = masked[max(0, open_pos - 180):open_pos]
    match = re.search(r'(?is)(new\s+(?:[A-Za-z_]\w*\.)*ChatMessage|new\s+Regex|(?<![A-Za-z0-9_])Regex\.(?:Match|Matches|IsMatch|Replace|Split)|(?:regex|regexCompilation|regexCompiler)\s*\.\s*Compile|CompileRegex)\s*$', before)
    if not match:
        return None

    par = bracket = brace = 0
    argument_index = 0
    for c in masked[open_pos + 1:token_start]:
        if c == '(':
            par += 1
        elif c == ')':
            par = max(0, par - 1)
        elif c == '[':
            bracket += 1
        elif c == ']':
            bracket = max(0, bracket - 1)
        elif c == '{':
            brace += 1
        elif c == '}':
            brace = max(0, brace - 1)
        elif c == ',' and par == bracket == brace == 0:
            argument_index += 1
    return match.group(1), open_pos, argument_index


def _is_direct_regex_pattern(text: str, token: StringToken, masked: str | None = None) -> bool:
    context = _call_context(text, token.start, masked)
    if context is None:
        return False
    callee, _, argument_index = context
    normalized = re.sub(r'\s+', '', callee).lower()
    if normalized.startswith('newregex'):
        return argument_index == 0
    if normalized.startswith('regex.'):
        # Static Regex APIs take input first and the pattern second.
        return argument_index == 1
    if normalized.endswith('.compile') or normalized == 'compileregex':
        return argument_index == 0
    return False


def _is_direct_system_message_literal(text: str, token: StringToken, masked: str | None = None) -> bool:
    context = _call_context(text, token.start, masked)
    if context is None:
        return False
    callee, open_pos, argument_index = context
    if 'chatmessage' not in callee.lower() or argument_index != 1:
        return False
    if masked is None:
        masked = mask_csharp(text)
    first_argument = masked[open_pos + 1:token.start]
    comma = first_argument.find(',')
    if comma >= 0:
        first_argument = first_argument[:comma]
    return 'ChatRole.System' in first_argument


ORCHESTRATION_PROMPT_METHOD = re.compile(
    r'(?i)^(?:Build.*(?:Prompt|Instruction|Briefing)|Append.*Instruction|ContinueAfterDxFunctionResultsAsync)$')


def _method_owns_model_prompt(method: MethodDecl) -> bool:
    if 'SystemPrompt' in method.name:
        return True
    return method.type_name == 'MultiModelCouncilService' and bool(ORCHESTRATION_PROMPT_METHOD.match(method.name))


def scan_code_region(code: str, rel: str, line_base: int, allow_prompt_owner: bool, allow_regex_owner: bool):
    findings: list[tuple[int, str, str, str]] = []
    masked = mask_csharp(code)
    methods = parse_methods(code)

    if not allow_regex_owner:
        for token in iter_string_tokens(code, 0, len(code)):
            if _is_direct_regex_pattern(code, token, masked):
                findings.append((
                    line_base + line_of(code, token.start) - 1,
                    'REGEX001',
                    'hardcoded regular-expression pattern bypasses the database-backed regex catalog/service',
                    snippet(token.text),
                ))

    if not allow_prompt_owner:
        for token in iter_string_tokens(code, 0, len(code)):
            if _is_direct_system_message_literal(code, token, masked) and authored_word_count(token.text) >= 2:
                findings.append((
                    line_base + line_of(code, token.start) - 1,
                    'PROMPT001',
                    'hardcoded ChatRole.System text bypasses the configurable database-backed prompt/template owners',
                    snippet(token.text),
                ))

        for method in methods:
            if not _method_owns_model_prompt(method):
                continue
            candidates: list[StringToken] = []
            for token in iter_string_tokens(code, method.body_start, method.end):
                if in_log_call(code, token.start):
                    continue
                # The orchestration guard is about authored model-policy bodies, not labels,
                # progress/status text, placeholder names, replacement values, or UI prose.
                # Multi-line raw prompt bodies are the regression shape that motivated this gate;
                # direct ChatRole.System literals are handled separately above.
                if not token.raw or authored_word_count(token.text) < 3:
                    continue
                candidates.append(token)
            if candidates:
                token = max(candidates, key=candidate_score)
                findings.append((
                    line_base + line_of(code, token.start) - 1,
                    'PROMPT001',
                    f'{method.type_name}.{method.name} constructs model-facing prompt/instruction prose inside orchestration code instead of resolving a configurable owner',
                    snippet(token.text),
                ))
    return findings


def razor_code_regions(text: str, component_name: str):
    masked = mask_csharp(text)
    for match in re.finditer(r'@code\s*\{', masked):
        open_pos = masked.find('{', match.start())
        if open_pos < 0:
            continue
        close_pos = match_brace(masked, open_pos)
        if close_pos < 0:
            continue
        body_start = open_pos + 1
        body = text[body_start:close_pos]
        safe_name = re.sub(r'[^A-Za-z0-9_]', '_', component_name) or 'RazorComponent'
        if safe_name[0].isdigit():
            safe_name = '_' + safe_name
        prefix = f'public sealed class {safe_name}\n{{\n'
        synthetic = prefix + body + '\n}\n'
        original_line = line_of(text, body_start)
        synthetic_body_line = prefix.count('\n') + 1
        line_base = original_line - synthetic_body_line + 1
        yield synthetic, line_base


def iter_sources(app_root: Path):
    for path in sorted(app_root.rglob('*')):
        if not path.is_file() or path.suffix.lower() not in {'.cs', '.razor'}:
            continue
        if any(part in {'bin', 'obj', 'Migrations', '.git', '.vs'} for part in path.parts):
            continue
        if path.name.endswith('.Designer.cs'):
            continue
        yield path, path.relative_to(app_root).as_posix()


def _console_safe(value: str) -> str:
    # Windows PowerShell may launch Python with a legacy single-byte console encoding.
    # Keep the report UTF-8, but guarantee the build log cannot crash while rendering a finding.
    return value.encode('ascii', errors='backslashreplace').decode('ascii')


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', required=True)
    parser.add_argument('--report')
    args = parser.parse_args()

    root = Path(args.root).resolve()
    app_root = root / 'src' / 'LocalGPT'
    findings: list[tuple[str, int, str, str, str]] = []

    for path, rel in iter_sources(app_root):
        source = path.read_text(encoding='utf-8-sig', errors='replace')
        prompt_owner = is_owner(rel, OWNER_PREFIXES)
        regex_owner = is_owner(rel, REGEX_OWNER_PREFIXES)
        regions = [(source, 1)] if path.suffix.lower() == '.cs' else list(razor_code_regions(source, path.stem))
        for code, line_base in regions:
            for line, code_id, message, example in scan_code_region(code, rel, line_base, prompt_owner, regex_owner):
                findings.append((rel, line, code_id, message, example))

    findings = sorted(set(findings), key=lambda item: (item[0].lower(), item[1], item[2], item[4]))
    output: list[str] = []
    if findings:
        output.append('Configurable model-prompt/regex ownership validation failed:')
        for rel, line, code_id, message, example in findings:
            output.append(f'src/LocalGPT/{rel}({line},1): error {code_id}: {message}. Example: {example!r}')
        output.append(
            f'Configurable model-prompt/regex ownership found {len(findings)} finding(s). '
            'System/model prompt prose must resolve through serializable/configurable services or database-backed templates; '
            'regex patterns must resolve through the database-backed regex catalog/services. '
            'Ordinary UI, status and diagnostic prose remains owned by the existing localization/text-service guards.')
    else:
        output.append(
            'Configurable model-prompt/regex ownership validation passed: operational code contains no literal system/model prompt bodies '
            'or direct regular-expression patterns outside approved configurable database-backed owners.')

    rendered = '\n'.join(output)
    print(_console_safe(rendered))
    if args.report:
        report = Path(args.report)
        if not report.is_absolute():
            report = root / report
        report.parent.mkdir(parents=True, exist_ok=True)
        report.write_text(rendered + '\n', encoding='utf-8')
        print(_console_safe(f'Runtime prompt/regex ownership report: {report}'))

    return 1 if findings else 0


if __name__ == '__main__':
    raise SystemExit(main())
