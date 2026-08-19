#!/usr/bin/env python3
"""Adds and validates XML documentation for Razor component types and @code members."""
from __future__ import annotations

import re
import tempfile
from dataclasses import replace
from pathlib import Path
from collections import Counter
import xml.etree.ElementTree as ET

import xml_documentation as xd


def iter_razor(root: Path):
    for path in sorted(root.rglob('*.razor')):
        if 'bin' in path.parts or 'obj' in path.parts or path.name == '_Imports.razor':
            continue
        yield path


def component_namespace(path: Path) -> str:
    text = path.read_text(encoding='utf-8-sig', errors='replace')
    match = re.search(r'(?m)^\s*@namespace\s+([^\s]+)\s*$', text)
    if match:
        return match.group(1).strip()
    parts = list(path.parts)
    try:
        component_index = len(parts) - 1 - parts[::-1].index('Components')
    except ValueError as exc:
        raise RuntimeError(f'Cannot infer Razor namespace because Components is not in the path: {path}') from exc
    if component_index == 0:
        raise RuntimeError(f'Cannot infer Razor project namespace for {path}')
    project_namespace = parts[component_index - 1]
    folder_parts = parts[component_index + 1:-1]
    return '.'.join([project_namespace, 'Components', *folder_parts])


def component_summary(path: Path) -> str:
    name = path.stem
    parts = set(path.parts)
    label = xd.words(name)
    if 'Pages' in parts:
        return f'Renders the {label} LocalGPT Razor page and coordinates the page-local state, commands, and user interactions exposed through its markup.'
    if 'Layout' in parts:
        return f'Renders the {label} LocalGPT layout component and coordinates shell, navigation, or shared presentation behavior used by the application UI.'
    if 'Shared' in parts:
        return f'Renders the reusable {label} Razor component and exposes its parameter-driven UI behavior to LocalGPT pages and layout surfaces.'
    if name == 'App':
        return 'Defines the root LocalGPT Razor application component, establishing the application shell and top-level component composition used at startup.'
    if name == 'Routes':
        return 'Defines the LocalGPT Razor routing component, selecting routable pages and shared route behavior for interactive server navigation.'
    return f'Renders the {label} Razor component and coordinates the component-local state and presentation behavior used by the surrounding LocalGPT interface.'


def ensure_component_companion(path: Path) -> bool:
    companion = path.with_suffix('.razor.cs')
    if companion.exists():
        return False
    namespace = component_namespace(path)
    summary = component_summary(path)
    companion.write_text(
        f'namespace {namespace};\n\n'
        '/// <summary>\n'
        f'/// {summary}\n'
        '/// </summary>\n'
        f'public partial class {path.stem}\n'
        '{\n'
        '}\n',
        encoding='utf-8',
    )
    return True


def _matching_code_block_end(text: str, brace_index: int) -> int:
    sanitized = xd.sanitize(text[brace_index:])
    depth = 0
    char_offset = brace_index
    # sanitize preserves physical line lengths except CR removal; source files in this repository
    # are normalized before packaging. Compute by walking original lines from the opening brace.
    source_lines = text[brace_index:].splitlines(keepends=True)
    for line_index, sanitized_line in enumerate(sanitized):
        for ch in sanitized_line:
            if ch == '{':
                depth += 1
            elif ch == '}':
                depth -= 1
                if depth == 0:
                    # Return the physical line number rather than a character offset. Callers only
                    # need line-safe edit locations for documentation comments.
                    return text[:brace_index].count('\n') + line_index
        if line_index < len(source_lines):
            char_offset += len(source_lines[line_index])
    raise RuntimeError('Unterminated @code block.')


def code_block_ranges(path: Path) -> list[tuple[int, int]]:
    text = path.read_text(encoding='utf-8-sig', errors='replace')
    ranges: list[tuple[int, int]] = []
    search_from = 0
    while True:
        match = re.search(r'(?m)^\s*@code\s*\{', text[search_from:])
        if not match:
            break
        absolute = search_from + match.start()
        brace = text.find('{', absolute, search_from + match.end())
        if brace < 0:
            raise RuntimeError(f'Unable to locate opening brace for @code block in {path}.')
        start_line = text[:brace].count('\n')
        end_line = _matching_code_block_end(text, brace)
        ranges.append((start_line, end_line))
        # Move to the line after the block. Converting back through splitlines avoids depending on
        # UTF-16/editor offsets and is sufficient because Razor files are edited line-wise here.
        lines = text.splitlines(keepends=True)
        search_from = sum(len(line) for line in lines[:end_line + 1])
    return ranges


def scan_razor_members(path: Path) -> tuple[list[str], list[xd.Decl]]:
    text = path.read_text(encoding='utf-8-sig', errors='replace')
    orig = text.splitlines()
    declarations: list[xd.Decl] = []
    component = path.stem
    for block_number, (start_line, end_line) in enumerate(code_block_ranges(path)):
        content = orig[start_line + 1:end_line]
        synthetic_prefix = ['namespace RazorDocumentationAudit', '{', f'partial class {component}', '{']
        synthetic = synthetic_prefix + content + ['}', '}']
        with tempfile.NamedTemporaryFile('w', encoding='utf-8', suffix='.cs', delete=False) as handle:
            handle.write('\n'.join(synthetic) + '\n')
            temp_path = Path(handle.name)
        try:
            _, scanned = xd.scan_file(temp_path)
        finally:
            temp_path.unlink(missing_ok=True)
        content_offset = start_line + 1 - len(synthetic_prefix)
        for declaration in scanned:
            if declaration.kind == 'nested_type' or declaration.start_line < len(synthetic_prefix):
                continue
            # The synthetic wrapper exists only so the C# declaration scanner can apply the same
            # rules used for ordinary source files. Keep every real declaration inside the @code
            # block, including members of nested helper types, but never surface the wrapper class.
            if declaration.kind == 'class' and declaration.name == component and declaration.containing_type is None:
                continue
            mapped = replace(
                declaration,
                start_line=declaration.start_line + content_offset,
                attr_line=declaration.attr_line + content_offset,
                header_end_line=declaration.header_end_line + content_offset,
                member_end_line=declaration.member_end_line + content_offset,
                containing_type=component,
                doc_start=(declaration.doc_start + content_offset if declaration.doc_start is not None else None),
                doc_end=(declaration.doc_end + content_offset if declaration.doc_end is not None else None),
            )
            declarations.append(mapped)
    unique = {(d.start_line, d.kind, d.name): d for d in declarations}
    return orig, sorted(unique.values(), key=lambda d: (d.start_line, d.kind, d.name))


def process_razor_file(path: Path) -> tuple[int, int, int]:
    orig, declarations = scan_razor_members(path)
    edits: list[tuple[int, int, list[str]]] = []
    additions = 0
    changes = 0
    for declaration in declarations:
        existing = orig[declaration.doc_start:declaration.doc_end + 1] if declaration.doc_start is not None else None
        new_block = xd.enrich_block(declaration, path, orig, existing)
        if existing is None:
            edits.append((declaration.attr_line, declaration.attr_line, new_block))
            additions += 1
        elif new_block != existing:
            edits.append((declaration.doc_start, declaration.doc_end + 1, new_block))
            changes += 1
    if edits:
        edits.sort(key=lambda edit: edit[0], reverse=True)
        last = len(orig) + 1
        for start, end, replacement in edits:
            if end > last:
                raise RuntimeError(f'Overlapping Razor XML documentation edits in {path}: {start}:{end}')
            orig[start:end] = replacement
            last = start
        path.write_text('\n'.join(orig) + '\n', encoding='utf-8')
    return additions, changes, len(declarations)


def validate_component_type(path: Path) -> list[str]:
    companion = path.with_suffix('.razor.cs')
    if not companion.exists():
        return [f'{path}: missing .razor.cs partial declaration carrying XML documentation for Razor component {path.stem}']
    _, declarations = xd.scan_file(companion)
    candidates = [d for d in declarations if d.kind == 'class' and d.name == path.stem]
    if not candidates:
        return [f'{companion}: missing partial class declaration for Razor component {path.stem}']
    declaration = candidates[0]
    if declaration.doc_start is None:
        return [f'{companion}:{declaration.start_line + 1}: missing XML documentation for Razor component class {path.stem}']
    lines = companion.read_text(encoding='utf-8-sig', errors='replace').splitlines()
    block = lines[declaration.doc_start:declaration.doc_end + 1]
    summary = xd.extract_summary(block)
    if not summary:
        return [f'{companion}:{declaration.start_line + 1}: empty XML summary for Razor component class {path.stem}']
    if 'component' not in summary.lower() and 'razor' not in summary.lower():
        return [f'{companion}:{declaration.start_line + 1}: Razor component summary does not explain component responsibility for {path.stem}: {summary}']
    return []


def validate_razor_file(path: Path) -> tuple[list[str], Counter]:
    orig, declarations = scan_razor_members(path)
    failures = validate_component_type(path)
    counts = Counter()
    for declaration in declarations:
        counts[declaration.kind] += 1
        if declaration.doc_start is None:
            failures.append(f'{path}:{declaration.start_line + 1}: missing XML documentation for Razor {declaration.kind} {declaration.name}')
            continue
        block = orig[declaration.doc_start:declaration.doc_end + 1]
        summary = xd.extract_summary(block)
        if not summary:
            failures.append(f'{path}:{declaration.start_line + 1}: empty XML summary for Razor {declaration.kind} {declaration.name}')
        elif xd.is_generic(summary):
            failures.append(f'{path}:{declaration.start_line + 1}: generic XML summary remains for Razor {declaration.kind} {declaration.name}: {summary}')
        inherited = any('<inheritdoc' in line.lower() for line in block)
        if not inherited:
            for type_parameter in declaration.typeparams:
                if not xd.has_tag(block, 'typeparam', type_parameter):
                    failures.append(f'{path}:{declaration.start_line + 1}: missing typeparam {type_parameter} for Razor member {declaration.name}')
                elif not xd.tag_text(block, 'typeparam', type_parameter):
                    failures.append(f'{path}:{declaration.start_line + 1}: empty typeparam {type_parameter} explanation for Razor member {declaration.name}')
            for _, parameter_name in declaration.params:
                if not xd.has_tag(block, 'param', parameter_name):
                    failures.append(f'{path}:{declaration.start_line + 1}: missing param {parameter_name} for Razor member {declaration.name}')
                elif not xd.tag_text(block, 'param', parameter_name):
                    failures.append(f'{path}:{declaration.start_line + 1}: empty param {parameter_name} explanation for Razor member {declaration.name}')
            if declaration.kind == 'property':
                if not xd.has_tag(block, 'value'):
                    failures.append(f'{path}:{declaration.start_line + 1}: missing value tag for Razor property {declaration.name}')
                elif not xd.tag_text(block, 'value'):
                    failures.append(f'{path}:{declaration.start_line + 1}: empty value explanation for Razor property {declaration.name}')
            if declaration.kind == 'method' and xd.return_desc(declaration):
                if not xd.has_tag(block, 'returns'):
                    failures.append(f'{path}:{declaration.start_line + 1}: missing returns tag for Razor member {declaration.name}')
                elif not xd.tag_text(block, 'returns'):
                    failures.append(f'{path}:{declaration.start_line + 1}: empty returns explanation for Razor member {declaration.name}')
        xml = '\n'.join(re.sub(r'^\s*///\s?', '', line) for line in block)
        try:
            ET.fromstring('<root>' + xml + '</root>')
        except Exception as exc:
            failures.append(f'{path}:{declaration.start_line + 1}: malformed Razor XML docs for {declaration.name}: {exc}')
    return failures, counts


def run(root: Path, mode: str) -> int:
    razor_files = list(iter_razor(root))
    if mode == 'enhance':
        companions = sum(1 for path in razor_files if ensure_component_companion(path))
        additions = changes = declarations = code_files = 0
        for path in razor_files:
            if not code_block_ranges(path):
                continue
            added, changed, count = process_razor_file(path)
            additions += added
            changes += changed
            declarations += count
            code_files += 1
        print(
            'Razor XML documentation enrichment completed for '
            f'{len(razor_files)} component(s): created {companions} documentation partial class file(s); '
            f'processed {declarations} direct @code member declaration(s) across {code_files} component(s), '
            f'added {additions} missing blocks and enriched {changes} existing blocks.'
        )
        return 0

    failures: list[str] = []
    counts = Counter()
    for path in razor_files:
        file_failures, file_counts = validate_razor_file(path)
        failures.extend(file_failures)
        counts.update(file_counts)
    if failures:
        print(f'Razor XML documentation validation failed with {len(failures)} finding(s):')
        for failure in failures[:500]:
            print(' -', failure)
        return 1
    print(
        f'Razor XML documentation coverage and quality passed for {len(razor_files)} component type(s) and '
        f'{sum(counts.values())} direct @code member declaration(s): '
        + ', '.join(f'{kind}={value}' for kind, value in sorted(counts.items()))
        + '.'
    )
    return 0
