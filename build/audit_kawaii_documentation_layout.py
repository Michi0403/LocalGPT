#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'docs/templates/localgpt/public/main.css'
SITE = ROOT / 'docs/_site/styles/localgpt-kawaii.css'
APP = ROOT / 'LocalGPTWebviewWrapper/LocalGPT/wwwroot/help-docs/styles/localgpt-kawaii.css'
SNAPSHOT = ROOT / '.github/pages/localgpt-kawaii-docs.zip'


def fail(message: str) -> None:
    raise SystemExit(f'Kawaii documentation layout audit failed: {message}')


def main() -> int:
    source = SOURCE.read_bytes()
    for path in (SITE, APP):
        if path.read_bytes() != source:
            fail(f'{path.relative_to(ROOT)} differs from the maintained theme source')

    text = source.decode('utf-8')
    required = (
        '--kawaii-docs-rail-width: clamp(15rem, 16vw, 18rem)',
        '--kawaii-docs-panel-gap: clamp(1.25rem, 2vw, 2.5rem)',
        '--kawaii-docs-shell-min-height:',
        'column-gap: var(--kawaii-docs-panel-gap)',
        'grid-template-columns:',
        'minmax(0, 1fr)',
        'margin-inline: auto !important',
        'max-width: 112rem !important',
        'width: calc(100% - clamp(2rem, 6vw, 6rem)) !important',
        'min-height: var(--kawaii-docs-shell-min-height) !important',
        'position: static !important',
        'grid-column: 1',
        'grid-column: 2',
        'grid-column: 3',
        'max-width: none !important',
        'overflow: visible !important',
    )
    for marker in required:
        if marker not in text:
            fail(f'missing marker {marker!r}')
    if text.count('var(--kawaii-docs-rail-width)') < 2:
        fail('left and right rails are not driven by the same width variable')

    css_hash = hashlib.sha256(source).hexdigest()[:12]
    for root in (ROOT / 'docs/_site', ROOT / 'LocalGPTWebviewWrapper/LocalGPT/wwwroot/help-docs'):
        for html in root.rglob('*.html'):
            value = html.read_text(encoding='utf-8')
            if 'localgpt-kawaii.css?v=' in value and f'localgpt-kawaii.css?v={css_hash}' not in value:
                fail(f'stale Kawaii CSS cache key in {html.relative_to(ROOT)}')

    with zipfile.ZipFile(SNAPSHOT) as archive:
        snapshot_css = archive.read('styles/localgpt-kawaii.css')
        if snapshot_css != source:
            fail('tracked GitHub Pages snapshot CSS differs from the maintained theme source')
        status = json.loads(archive.read('documentation-status.json'))
        if status.get('version') != '2.3.2':
            fail('tracked GitHub Pages snapshot is not version 2.3.2')
        if 'LocalGPT-2.3.2.pdf' not in archive.namelist():
            fail('tracked GitHub Pages snapshot is missing LocalGPT-2.3.2.pdf')

    print('Kawaii documentation layout audit passed: equal rails, symmetric gaps, full-width articles, and synchronized site assets.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
