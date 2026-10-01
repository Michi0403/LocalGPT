#!/usr/bin/env python3
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
import argparse
import importlib.util
import re
import sys

ALLOWED_LAYOUTS = (
    'DxGridLayout',
    'DxCarousel',
    'DxDrawer',
    'DxFormLayout',
    'DxSplitter',
    'DxStackLayout',
    'DxTabs',
)
FORBIDDEN_TAGS = {'section': 'RAZORUI0001', 'dialog': 'RAZORUI0002'}
NATIVE_ROOTS = {'div', 'main', 'article', 'aside', 'nav', 'form', 'fieldset', 'section', 'dialog'}
HOST_LAYOUT_EXEMPT = {'App.razor'}
COMPONENT_BOUNDARY_CLASS = 'razor-component-boundary'
LAYOUT_COMPATIBILITY_CSS_MARKER = 'DEVEXPRESS_RAZOR_LAYOUT_COMPATIBILITY'
LAYOUT_COMPATIBILITY_REQUIRED_TOKENS = (
    '.razor-component-boundary',
    '.razor-component-layout-owner',
    '.razor-section-layout-owner',
    '.dxbl-row',
    'dxbl-form-layout-item',
    '.dxbl-fl-ctrl',
    'display: contents !important',
)
POPUP_VIEWPORT_CSS_MARKER = 'DEVEXPRESS_POPUP_VIEWPORT_CONTRACT'
POPUP_VIEWPORT_REQUIRED_TOKENS = (
    'dxbl-popup-root > dxbl-popup-cell > dxbl-modal',
    'dxbl-modal-dialog.dxbl-popup',
    '.dxbl-modal-content',
    '.dxbl-modal-body',
    'max-width: calc(100vw - 1rem) !important',
    'max-height: calc(100dvh - 1rem) !important',
    'overflow: auto !important',
    'overscroll-behavior: contain',
)
PRIMARY_LAYOUT_MARKER_RE = re.compile(r'@\*\s*razor-primary-layout:\s*(?P<layout>Dx(?:GridLayout|Carousel|Drawer|FormLayout|Splitter|StackLayout|Tabs))\s*\|\s*reason:\s*(?P<reason>.*?)\s*\*@', re.I | re.S)
STACK_CHILD_INTENT_RE = re.compile(r'@\*\s*razor-layout-intent:\s*stack-child\s*\|\s*reason:\s*(?P<reason>.*?)\s*\*@', re.I | re.S)
FORM_EDITOR_RE = re.compile(r'<\s*(?:DxTextBox|DxMemo|DxSpinEdit|DxComboBox|DxTagBox|DxCheckBox|DxDateEdit|DxDateRangePicker|DxTimeEdit|DxMaskedInput|DxRadioGroup)\b', re.I)

METHOD_RE = re.compile(
    r'(?m)^[ \t]*(?P<attrs>(?:\[[^\]\n]+\][ \t]*(?:\r?\n[ \t]*)?)*)'
    r'(?:(?P<access>public|private|protected|internal)\s+)?'
    r'(?P<mods>(?:(?:static|async|override|virtual|sealed|new|partial|unsafe|extern|required)\s+)*)'
    r'(?P<return>[A-Za-z_][\w<>,\.\[\]\? \t:]*(?:\s*\*)?)\s+'
    r'(?P<name>[A-Za-z_]\w*)\s*\((?P<args>[^;{}]*?)\)\s*'
    r'(?:where\s+[^\{=>\r\n]+\s*)?(?P<body>\{|=>)'
)

EXPRESSION_PROPERTY_RE = re.compile(
    r'(?ms)^[ \t]*(?P<attrs>(?:\[[^\]\n]+\][ \t]*(?:\r?\n[ \t]*)?)*)'
    r'(?:(?P<access>public|private|protected|internal)\s+)?'
    r'(?P<mods>(?:(?:static|override|virtual|sealed|new|required)\s+)*)'
    r'(?P<type>[A-Za-z_][\w<>,\.\[\]\? \t:]*)\s+'
    r'(?P<name>[A-Za-z_]\w*)\s*=>\s*(?P<expr>.*?);'
)

SIMPLE_LOCAL_METHOD_CALL_RE = re.compile(
    r'^\s*[A-Za-z_]\w*\s*\((?:[^()]|\([^()]*\))*\)\s*$',
    re.S,
)

TAG_RE = re.compile(r'<\s*(?!/|!|\?)(?P<tag>[A-Za-z][A-Za-z0-9_.:]*)\b', re.I)
FORBIDDEN_TAG_RE = re.compile(r'<\s*/?\s*(?P<tag>section|dialog)\b', re.I)
MANUAL_POPUP_RE = re.compile(
    r'<\s*(?P<tag>div|main|article|aside|nav|form|fieldset)\b(?P<attrs>[^>]*)>',
    re.I | re.S,
)
MANUAL_POPUP_CLASS_RE = re.compile(
    r'class\s*=\s*["\'][^"\']*(?:modal-backdrop|dialog-backdrop)[^"\']*["\']',
    re.I,
)
NATIVE_DIALOG_ROLE_RE = re.compile(r'role\s*=\s*["\']dialog["\']', re.I)
RAZOR_COMMENT_RE = re.compile(r'@\*.*?\*@', re.S)
HTML_COMMENT_RE = re.compile(r'<!--.*?-->', re.S)

@dataclass
class Finding:
    file: str
    line: int
    column: int
    code: str
    message: str
    choices: str


def load_parser(path: Path):
    spec = importlib.util.spec_from_file_location('razor_maintenance_parser', path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f'Could not load maintained C# parser: {path}')
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def line_col(text: str, position: int) -> tuple[int, int]:
    line = text.count('\n', 0, position) + 1
    last_nl = text.rfind('\n', 0, position)
    column = position + 1 if last_nl < 0 else position - last_nl
    return line, column


def depth_at(masked: str, position: int) -> int:
    depth = 0
    for char in masked[:position]:
        if char == '{':
            depth += 1
        elif char == '}':
            depth = max(0, depth - 1)
    return depth


def expression_end(masked: str, start: int) -> int:
    paren = bracket = brace = 0
    for index in range(start, len(masked)):
        char = masked[index]
        if char == '(':
            paren += 1
        elif char == ')':
            paren = max(0, paren - 1)
        elif char == '[':
            bracket += 1
        elif char == ']':
            bracket = max(0, bracket - 1)
        elif char == '{':
            brace += 1
        elif char == '}' and brace:
            brace -= 1
        elif char == ';' and paren == 0 and bracket == 0 and brace == 0:
            return index + 1
    return -1


def iter_code_blocks(text: str, parser):
    for marker in re.finditer(r'(?m)^\s*@(code|functions)\s*\{', text):
        opening = text.find('{', marker.start(), marker.end())
        if opening < 0:
            continue
        tail = text[opening:]
        masked_tail = parser.mask_csharp(tail)
        closing_relative = parser.match_brace(masked_tail, 0)
        if closing_relative > 0:
            closing = opening + closing_relative
            yield opening + 1, text[opening + 1:closing], masked_tail[1:closing_relative], opening, closing + 1


def markup_without_code(text: str, parser) -> str:
    chars = list(text)
    for _, _, _, start, end in iter_code_blocks(text, parser):
        for index in range(start, end):
            if chars[index] not in ('\n', '\r'):
                chars[index] = ' '
    cleaned = ''.join(chars)
    cleaned = RAZOR_COMMENT_RE.sub(lambda m: ''.join('\n' if c == '\n' else '\r' if c == '\r' else ' ' for c in m.group(0)), cleaned)
    cleaned = HTML_COMMENT_RE.sub(lambda m: ''.join('\n' if c == '\n' else '\r' if c == '\r' else ' ' for c in m.group(0)), cleaned)
    directive_prefixes = (
        '@page', '@using', '@inject', '@inherits', '@implements', '@namespace', '@attribute',
        '@typeparam', '@layout', '@rendermode', '@preservewhitespace'
    )
    normalized_lines: list[str] = []
    for line in cleaned.splitlines(keepends=True):
        stripped = line.lstrip()
        if stripped.startswith(directive_prefixes):
            normalized_lines.append(''.join('\n' if c == '\n' else '\r' if c == '\r' else ' ' for c in line))
        else:
            normalized_lines.append(line)
    return ''.join(normalized_lines)


def iter_razor_methods(text: str, parser):
    for block_start, block, masked, _, _ in iter_code_blocks(text, parser):
        for match in METHOD_RE.finditer(masked):
            if depth_at(masked, match.start()) != 0:
                continue
            body_start = match.start('body')
            if match.group('body') == '{':
                end = parser.match_brace(masked, body_start)
                if end < 0:
                    continue
                end += 1
            else:
                end = expression_end(masked, body_start + 2)
                if end < 0:
                    continue
            yield match, block[match.start():end], block_start + match.start()


def has_logging(body: str) -> bool:
    return bool(
        re.search(r'\b(?:Logger|logger|_logger|[A-Za-z_]\w*Logger)\s*\.\s*Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(', body)
        or re.search(r'\bOperationalLoggerFactory\s*\.\s*CreateLogger\s*\(', body)
        or re.search(r'\b(?:System\.Diagnostics\.)?Trace\s*\.\s*Trace(?:Information|Warning|Error)\s*\(', body)
    )


def normalize_signature(match: re.Match[str]) -> str:
    args = re.sub(r'\s+', ' ', match.group('args')).strip()
    return_type = re.sub(r'\s+', ' ', match.group('return')).strip()
    access = (match.group('access') or 'private').strip()
    mods = re.sub(r'\s+', ' ', match.group('mods') or '').strip()
    prefix = f'{access} {mods} {return_type}' if mods else f'{access} {return_type}'
    return f'{prefix} {match.group("name")}({args})'


def add_method_findings(findings: list[Finding], relative: str, text: str, parser) -> None:
    for match, body, start in iter_razor_methods(text, parser):
        signature = normalize_signature(match)
        masked_body = parser.mask_csharp(body)
        is_iterator = bool(re.search(r'\byield\s+(?:return|break)\b', masked_body))
        missing: list[str] = []
        if is_iterator:
            if not re.search(r'\btry\b', masked_body) or not re.search(r'\bfinally\b', masked_body):
                missing.append('try/finally')
            if re.search(r'\bcatch\b', masked_body):
                missing.append('iterator catch is forbidden')
        else:
            if not re.search(r'\btry\b', masked_body) or not re.search(r'\bcatch\b', masked_body):
                missing.append('try/catch')
        if not has_logging(body):
            missing.append('structured ILogger diagnostics')
        if missing:
            line, col = line_col(text, start)
            findings.append(Finding(
                relative, line, col, 'RAZORLOG0002',
                f"Component method '{signature}' is missing {', '.join(missing)}.",
                'Keep the operation. Add a method-local diagnostics boundary and structured Logger.Log* call; expected cancellation/circuit disconnects may be handled at Debug level. Do not delete the feature, move it into markup, or swallow the failure merely to satisfy the guard.'
            ))


def add_expression_property_findings(findings: list[Finding], relative: str, text: str, parser) -> None:
    # Procedural/computed getter calls are owned by the zero-baseline async-only architecture audit.
    # Keep this Razor-specific guard focused on layout/diagnostics so the two rules cannot prescribe
    # contradictory synchronous helper patterns.
    return


def add_codebehind_method_findings(findings: list[Finding], relative: str, text: str, parser) -> None:
    for method in parser.parse_methods(text):
        body = text[method.start:method.end]
        masked_body = parser.mask_csharp(body)
        is_iterator = bool(re.search(r'\byield\s+(?:return|break)\b', masked_body))
        missing: list[str] = []
        if is_iterator:
            if not re.search(r'\btry\b', masked_body) or not re.search(r'\bfinally\b', masked_body):
                missing.append('try/finally')
            if re.search(r'\bcatch\b', masked_body):
                missing.append('iterator catch is forbidden')
        else:
            if not re.search(r'\btry\b', masked_body) or not re.search(r'\bcatch\b', masked_body):
                missing.append('try/catch')
        if not has_logging(body):
            missing.append('structured ILogger diagnostics')
        if missing:
            line, col = line_col(text, method.start)
            findings.append(Finding(
                relative, line, col, 'RAZORLOG0002',
                f"Component code-behind method '{method.type_name}.{method.name}' is missing {', '.join(missing)}.",
                'Keep the operation. Add a method-local diagnostics boundary and structured Logger.Log* call; expected cancellation/circuit disconnects may be handled at Debug level. Do not delete or bypass the operation to satisfy the guard.'
            ))


def main() -> int:
    ap = argparse.ArgumentParser(description='Audit Razor layout ownership, forbidden HTML, computed properties, and component logging.')
    ap.add_argument('--root', required=True, type=Path)
    ap.add_argument('--product', required=True, choices=('localgpt', 'publisherstudio'))
    args = ap.parse_args()

    root = args.root.resolve()
    if args.product == 'localgpt':
        app = root / 'src/LocalGPT'
    else:
        app = root / 'src/PublisherStudio.Web'
    components = app / 'Components'
    parser_path = root / 'build/audit_application_architecture.py'
    if not parser_path.is_file():
        print(f'{parser_path}(1,1): error RAZORARCH0000: Maintained C# parser is missing; Razor maintenance architecture cannot be audited.')
        return 1
    parser = load_parser(parser_path)

    findings: list[Finding] = []

    # The maintenance FormLayout wrappers were introduced to retain DevExpress ownership, not to
    # change the product's established grid/flex/height/overflow geometry.  Keep their generated
    # host/row/item/control boxes transparent so the original component roots remain the effective
    # layout participants.  This is a source-level contract because removing one of these selectors
    # recreates the deep visual regression without removing a single DevExpress component.
    compatibility_css = app / 'wwwroot/css/site.css'
    compatibility_relative = compatibility_css.relative_to(app).as_posix()
    shell_path = components / 'App.razor'
    shell_text = shell_path.read_text(encoding='utf-8-sig', errors='replace') if shell_path.is_file() else ''
    if 'css/site.css' not in shell_text:
        findings.append(Finding(
            'Components/App.razor', 1, 1, 'RAZORUI0012',
            'The maintained css/site.css stylesheet is not referenced by the application shell.',
            'Restore the css/site.css link in App.razor. The DevExpress wrapper compatibility contract must live in a stylesheet that the running application actually loads; do not park it in an unused legacy stylesheet.'
        ))
    if not compatibility_css.is_file():
        findings.append(Finding(
            compatibility_relative, 1, 1, 'RAZORUI0012',
            'DevExpress Razor layout compatibility stylesheet is missing.',
            'Restore the maintained stylesheet and its DEVEXPRESS_RAZOR_LAYOUT_COMPATIBILITY rules. Keep maintenance-only component/section FormLayout owners box-neutral instead of deleting DevExpress ownership or restyling every component around the wrapper regression.'
        ))
    else:
        compatibility_text = compatibility_css.read_text(encoding='utf-8-sig', errors='replace')
        missing_compatibility_tokens = [
            token for token in (LAYOUT_COMPATIBILITY_CSS_MARKER, *LAYOUT_COMPATIBILITY_REQUIRED_TOKENS)
            if token not in compatibility_text
        ]
        if missing_compatibility_tokens:
            marker_position = compatibility_text.find(LAYOUT_COMPATIBILITY_CSS_MARKER)
            line, col = line_col(compatibility_text, marker_position if marker_position >= 0 else 0)
            findings.append(Finding(
                compatibility_relative, line, col, 'RAZORUI0012',
                'DevExpress maintenance layout compatibility rules are incomplete: ' + ', '.join(missing_compatibility_tokens),
                'Restore the box-neutral compatibility selector chain for the component boundary, DevExpress layout owner, generated row/item/control shells, and migrated section owner. Do not remove DevExpress controls to recover layout geometry.'
            ))

        missing_popup_tokens = [
            token for token in (POPUP_VIEWPORT_CSS_MARKER, *POPUP_VIEWPORT_REQUIRED_TOKENS)
            if token not in compatibility_text
        ]
        if missing_popup_tokens:
            marker_position = compatibility_text.find(POPUP_VIEWPORT_CSS_MARKER)
            line, col = line_col(compatibility_text, marker_position if marker_position >= 0 else 0)
            findings.append(Finding(
                compatibility_relative, line, col, 'RAZORUI0013',
                'DevExpress modal viewport/scroll containment rules are incomplete: ' + ', '.join(missing_popup_tokens),
                'Restore the shared DxPopup viewport contract: bound modal dialogs to the browser viewport, keep modal content shrinkable, and let the modal body own overflow/scroll. Do not replace DxPopup with manual backdrops or allow studio content to bleed outside the modal border.'
            ))

    razor_files = sorted(p for p in components.rglob('*.razor') if p.name != '_Imports.razor')

    for path in razor_files:
        text = path.read_text(encoding='utf-8-sig', errors='replace')
        relative = path.relative_to(app).as_posix()
        markup = markup_without_code(text, parser)

        for popup_match in MANUAL_POPUP_RE.finditer(markup):
            attrs = popup_match.group('attrs')
            if MANUAL_POPUP_CLASS_RE.search(attrs) or NATIVE_DIALOG_ROLE_RE.search(attrs):
                line, col = line_col(markup, popup_match.start())
                findings.append(Finding(
                    relative, line, col, 'RAZORUI0005',
                    'Native/manual modal or popup ownership is forbidden in maintained Razor UI.',
                    'Use DxPopup or another reviewed DevExpress window/prompt as the actual modal owner. Native elements may remain only as body content inside the DevExpress popup and must not own role=dialog, modal backdrops, focus trapping, or overlay positioning. Preserve the close/cancel workflow and diagnostics instead of deleting the feature.'
                ))

        for match in FORBIDDEN_TAG_RE.finditer(markup):
            tag = match.group('tag').lower()
            line, col = line_col(markup, match.start())
            if tag == 'section':
                choices = 'Replace the semantic HTML section with one of the approved DevExpress layout owners: DxGridLayout, DxCarousel, DxDrawer, DxFormLayout, DxSplitter, DxStackLayout, or DxTabs. Preserve the existing content and behavior inside that layout; never replace a DevExpress component with simpler native HTML.'
            else:
                choices = 'Replace native <dialog> with DxPopup or another reviewed DevExpress window/prompt. When only conditional visibility is needed, toggle an approved DevExpress layout with component state. Preserve the dialog workflow and diagnostics; do not delete it.'
            findings.append(Finding(relative, line, col, FORBIDDEN_TAGS[tag], f'Native <{tag}> is forbidden in maintained Razor UI.', choices))

        tags = list(TAG_RE.finditer(markup))
        tag_names = [m.group('tag') for m in tags]
        has_layout = any(name in ALLOWED_LAYOUTS for name in tag_names)
        if tags and path.name not in HOST_LAYOUT_EXEMPT:
            if not has_layout:
                first = tags[0]
                line, col = line_col(markup, first.start())
                findings.append(Finding(
                    relative, line, col, 'RAZORUI0003',
                    'Rendered Razor UI has no approved DevExpress layout owner.',
                    'Keep one containment-only root <div class="razor-component-boundary">, then place an approved DevExpress layout inside it. Prefer DxFormLayout for editors, scanners, settings and field-oriented UI; use Stack/Grid/Splitter/Tabs/Drawer/Carousel only when their layout semantics are actually required.'
                ))
            else:
                first = tags[0]
                first_name = first.group('tag')
                opening_end = markup.find('>', first.start())
                first_opening = markup[first.start():opening_end + 1] if opening_end >= 0 else markup[first.start():first.end()]
                has_boundary = first_name.lower() == 'div' and re.search(r'class\s*=\s*["\'][^"\']*\b' + re.escape(COMPONENT_BOUNDARY_CLASS) + r'\b', first_opening, re.I)
                if not has_boundary:
                    line, col = line_col(markup, first.start())
                    findings.append(Finding(
                        relative, line, col, 'RAZORUI0004',
                        'Rendered component does not start with the containment-only Razor component boundary.',
                        'Use exactly one outer <div class="razor-component-boundary"> for CSS/bleed containment. It must not replace DevExpress layout ownership: place the semantic DevExpress layout immediately inside it, normally DxFormLayout for editor/form/scanner UI.'
                    ))
                # The first DevExpress layout after the containment div is the semantic primary owner.
                primary = next((m for m in tags[1:] if m.group('tag') in ALLOWED_LAYOUTS), None) if has_boundary else next((m for m in tags if m.group('tag') in ALLOWED_LAYOUTS), None)
                if primary is not None:
                    primary_name = primary.group('tag')
                    marker = PRIMARY_LAYOUT_MARKER_RE.search(text[:max(primary.start() + 800, 1600)])
                    if primary_name != 'DxFormLayout':
                        valid_marker = bool(marker and marker.group('layout').lower() == primary_name.lower() and len(marker.group('reason').strip()) >= 12)
                        if not valid_marker:
                            line, col = line_col(markup, primary.start())
                            findings.append(Finding(
                                relative, line, col, 'RAZORUI0006',
                                f"Primary DevExpress layout is {primary_name}; DxFormLayout is the maintained default.",
                                'Editors, scanners, settings and ordinary component surfaces should use DxFormLayout. Keep another primary layout only when its semantics are genuinely needed and document that architectural choice beside the component root with `@* razor-primary-layout: DxGridLayout | reason: concrete reason *@` (using the actual layout name). Do not use StackLayout as a universal wrapper.'
                            ))
                if FORM_EDITOR_RE.search(markup) and 'DxFormLayout' not in tag_names:
                    first_editor = FORM_EDITOR_RE.search(markup)
                    line, col = line_col(markup, first_editor.start() if first_editor else first.start())
                    findings.append(Finding(
                        relative, line, col, 'RAZORUI0008',
                        'Field/editor controls are rendered without DxFormLayout ownership.',
                        'Wrap the field/editor group in DxFormLayout (and DxFormLayoutItem as appropriate). A Grid/Stack/Splitter may organize larger regions, but it does not replace FormLayout ownership for form-like editor content.'
                    ))

        # DevExpress FormLayout item templates are templated child content.  Leaving their
        # context implicit makes nested DevExpress templates inherit the default `context`
        # name and can produce Razor RZ9999 at compile time.  Require an explicit unique
        # name at the owning item boundary so nested buttons, popups and item templates
        # remain parser-safe without flattening the component.
        form_template_re = re.compile(
            r'<\s*DxFormLayoutItem\b[^>]*>\s*<\s*Template(?P<attrs>[^>]*)>',
            re.I | re.S
        )
        seen_form_template_contexts: set[str] = set()
        for form_template in form_template_re.finditer(markup):
            attrs = form_template.group('attrs') or ''
            context_match = re.search(r'\bContext\s*=\s*["\'](?P<name>[^"\']+)["\']', attrs, re.I)
            line, col = line_col(markup, form_template.start())
            if not context_match:
                findings.append(Finding(
                    relative, line, col, 'RAZORUI0010',
                    'DxFormLayoutItem Template uses the implicit Razor child-content context name.',
                    'Give the Template an explicit locally unique context name, for example `<Template Context="formItemContext01">`. Keep the DevExpress FormLayout/Template structure; do not remove the layout, button, popup, combo-box template, or other nested control to silence RZ9999.'
                ))
                continue
            context_name = context_match.group('name')
            if context_name in seen_form_template_contexts:
                findings.append(Finding(
                    relative, line, col, 'RAZORUI0011',
                    f"DxFormLayoutItem Template context '{context_name}' is reused in the same Razor component.",
                    'Use a locally unique Template Context name for each DxFormLayoutItem. Unique names prevent nested templated child-content scopes from becoming ambiguous as the component evolves.'
                ))
            else:
                seen_form_template_contexts.add(context_name)

        # Prevent the previous mechanical migration pattern from returning.
        for stack_owner in re.finditer(r'<\s*DxStackLayout\b[^>]*class\s*=\s*["\'][^"\']*razor-(?:component|section)-layout-owner[^"\']*["\']', markup, re.I | re.S):
            line, col = line_col(markup, stack_owner.start())
            findings.append(Finding(
                relative, line, col, 'RAZORUI0007',
                'Generic component/section ownership may not be implemented with DxStackLayout.',
                'Use DxFormLayout for the normal component/form surface. Keep DxStackLayout only for a real one-dimensional arrangement whose semantics are distinct from the component owner; do not mechanically replace sections/divs with StackLayout.'
            ))

        # Grid -> Stack is valid only for a genuine one-dimensional subgroup, never as accidental double layout.
        layout_tag_re = re.compile(r'<\s*(?P<close>/?)\s*(?P<name>DxGridLayout|DxStackLayout)\b[^>]*>', re.I | re.S)
        layout_stack: list[tuple[str, int]] = []
        for layout_match in layout_tag_re.finditer(markup):
            name = layout_match.group('name')
            if layout_match.group('close'):
                for idx in range(len(layout_stack) - 1, -1, -1):
                    if layout_stack[idx][0].lower() == name.lower():
                        del layout_stack[idx:]
                        break
                continue
            if name.lower() == 'dxstacklayout' and any(parent[0].lower() == 'dxgridlayout' for parent in layout_stack):
                original_window = text[max(0, layout_match.start() - 900):layout_match.start() + 300]
                intent = STACK_CHILD_INTENT_RE.search(original_window)
                if not intent or len(intent.group('reason').strip()) < 12:
                    line, col = line_col(markup, layout_match.start())
                    findings.append(Finding(
                        relative, line, col, 'RAZORUI0009',
                        'DxStackLayout is nested inside DxGridLayout without a documented one-dimensional subgroup reason.',
                        'Flatten the StackLayout into the Grid when it only simulates rows/columns. Keep it only for a true one-dimensional child group and document the reason immediately nearby with `@* razor-layout-intent: stack-child | reason: concrete reason *@`.'
                    ))
            layout_stack.append((name, layout_match.start()))

        component_name = path.stem
        expected_logger = f'@inject ILogger<{component_name}> Logger'
        logger_matches = [m for m in re.finditer(re.escape(expected_logger), text)]
        if len(logger_matches) != 1:
            line = 1
            col = 1
            if logger_matches:
                line, col = line_col(text, logger_matches[0].start())
            findings.append(Finding(
                relative, line, col, 'RAZORLOG0001',
                f"Every Razor component must own exactly one typed logger injection '{expected_logger}'; found {len(logger_matches)}.",
                'Add the typed ILogger injection in the top directive block and keep it. Do not rely only on a global logger factory for component-local failures and do not remove logging to shorten the component.'
            ))

        add_expression_property_findings(findings, relative, text, parser)
        add_method_findings(findings, relative, text, parser)

    for path in sorted(components.rglob('*.razor.cs')):
        text = path.read_text(encoding='utf-8-sig', errors='replace')
        relative = path.relative_to(app).as_posix()
        add_codebehind_method_findings(findings, relative, text, parser)

    if findings:
        print('Razor maintenance architecture audit failed. All findings from this audit are listed below:')
        for finding in findings:
            print(f'{finding.file}({finding.line},{finding.column}): error {finding.code}: {finding.message}')
            print(f'  Architectural choices: {finding.choices}')
        print(f'Razor maintenance architecture audit found {len(findings)} violation(s) across {len(razor_files)} Razor component(s). No legacy exemption list is permitted; App.razor is the sole document-host layout exception.')
        return 1

    print(
        f'Razor maintenance architecture validation passed for {len(razor_files)} Razor component(s): '
        'containment-only root divs protect component boundaries, DxFormLayout is the default semantic owner for forms/editors, '
        'generic StackLayout wrappers and unexplained Grid-to-Stack nesting are rejected, FormLayout template contexts are explicit and unique, <section>/<dialog> are absent, '
        'maintenance-only DevExpress wrapper shells remain box-neutral, typed component loggers are present, manual/native modal owners are absent, '
        'and component methods own diagnostics.'
    )
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
