#!/usr/bin/env python3
from __future__ import annotations
from pathlib import Path
import argparse, importlib.util, re, sys

BOOT_EXCLUSIONS = {
    'localgpt': {
        ('ServiceMethodDiagnosticsRegistration', 'Apply'),
        ('LoggingConfigurationService', 'Configure'),
        ('ChatClientFactory', 'Build'),
    },
    'publisherstudio': {
        ('SystemVariableStoreService', 'AttachLogger'),
        ('SystemVariableStoreService', 'GetAvailableCultures'),
        ('FileLocalizationService', 'GetAvailableCultures'),
        ('ApplicationPortResolver', 'Resolve'),
        ('ApplicationPathService', 'EnsureDirectories'),
    },
}

def load_parser(path: Path):
    spec = importlib.util.spec_from_file_location('service_resilience_arch_parser', path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module

def parse_methods_including_records(text: str, parser):
    masked = parser.mask_csharp(text)
    types = parser.parse_types(text)
    raw = []
    type_words = {'class', 'record', 'struct', 'interface', 'enum', 'delegate'}
    for match in parser.METHOD_RE.finditer(masked):
        return_type = match.group('return').strip()
        signature = re.sub(r'\s+', ' ', match.group('signature')).strip()
        if return_type in type_words or re.search(r'\b(?:class|record|struct|interface|enum|delegate)\b', signature):
            continue
        typ = parser.enclosing_type(types, match.start())
        if typ is None:
            continue
        body = match.group('body')
        body_start = match.start('body')
        if body == '{':
            end = parser.match_brace(masked, body_start)
        else:
            i = body_start + 2
            par = br = cur = 0
            while i < len(masked):
                char = masked[i]
                if char == '(': par += 1
                elif char == ')': par = max(0, par - 1)
                elif char == '[': br += 1
                elif char == ']': br = max(0, br - 1)
                elif char == '{': cur += 1
                elif char == '}' and cur: cur -= 1
                elif char == ';' and par == br == cur == 0: break
                i += 1
            end = i
        if end < 0:
            continue
        raw.append(parser.MethodDecl(match.group('name'), signature, return_type, match.group('modifiers').strip(),
                                     match.group('access'), match.start(), body_start, end + 1, body == '=>', typ.name))
    return [m for m in raw if not any(
        other.start < m.start < other.end and (other.end - other.start) > (m.end - m.start)
        for other in raw if other is not m)]

def line_of(text: str, offset: int) -> int:
    return text.count('\n', 0, offset) + 1

def has_logging(body: str) -> bool:
    return bool(
        re.search(r'\b[A-Za-z_]\w*\s*\.\s*Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(', body)
        or re.search(r'\b(?:System\.Diagnostics\.)?Trace\s*\.\s*Trace(?:Information|Warning|Error)\s*\(', body)
    )

def main() -> int:
    ap = argparse.ArgumentParser(description='Require service-method error boundaries and diagnostics.')
    ap.add_argument('--root', required=True, type=Path, help='Repository root')
    ap.add_argument('--product', required=True, choices=['localgpt', 'publisherstudio'])
    args = ap.parse_args()

    root = args.root.resolve()
    app = root / ('src/LocalGPT' if args.product == 'localgpt' else 'src/PublisherStudio.Web')
    services = app / 'Services'
    parser = load_parser(root / 'build/audit_application_architecture.py')
    failures: list[str] = []
    checked = skipped_yield = skipped_boot = 0

    for path in sorted(services.rglob('*.cs')):
        if any(part in {'bin', 'obj', 'Migrations'} for part in path.parts):
            continue
        text = path.read_text(encoding='utf-8-sig', errors='replace')
        for method in parse_methods_including_records(text, parser):
            body = text[method.body_start:method.end]
            masked = parser.mask_csharp(body)
            if re.search(r'\byield\b', masked):
                skipped_yield += 1
                continue
            if (method.type_name, method.name) in BOOT_EXCLUSIONS[args.product]:
                skipped_boot += 1
                continue
            checked += 1
            ident = f'{path.relative_to(app).as_posix()}:{line_of(text, method.start)} {method.type_name}.{method.name}'
            if not re.search(r'\btry\b', masked) or not re.search(r'\bcatch\b', masked):
                failures.append(f'{ident}: missing try/catch boundary')
                continue
            if not has_logging(body):
                failures.append(f'{ident}: missing ILogger/Trace diagnostics')

    if failures:
        print('Service resilience audit failed:')
        for failure in failures:
            print(f'  - {failure}')
        print(f'Checked {checked} service methods; skipped {skipped_yield} yield methods and {skipped_boot} Program/Startup boot methods.')
        return 1

    print(f'Service resilience audit passed: {checked} service methods own try/catch + diagnostics; skipped {skipped_yield} yield methods and {skipped_boot} direct Program/Startup methods.')
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
