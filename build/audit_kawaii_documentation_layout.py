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
SCRIPT = ROOT / 'docs/templates/localgpt/public/main.js'
SITE = ROOT / 'docs/_site/styles/localgpt-kawaii.css'
APP = ROOT / 'src/LocalGPT/wwwroot/help-docs/styles/localgpt-kawaii.css'
SNAPSHOT = ROOT / '.github/pages/localgpt-kawaii-docs.zip'


def fail(message: str) -> None:
    raise SystemExit(f'Kawaii documentation layout audit failed: {message}')


def main() -> int:
    source = SOURCE.read_bytes()
    script = SCRIPT.read_bytes()
    script_text = script.decode('utf-8')
    for marker in ('ensureRootDocumentationRail', 'data-localgpt-root-toc', 'docfx:tocrel', 'docfx:navrel'):
        if marker not in script_text:
            fail(f'missing landing-page navigation marker {marker!r}')
    maintained_copies = [APP]
    if SITE.exists():
        maintained_copies.append(SITE)
    for path in maintained_copies:
        if path.read_bytes() != source:
            fail(f'{path.relative_to(ROOT)} differs from the maintained theme source')

    text = source.decode('utf-8')
    required = (
        '--kawaii-docs-rail-width: clamp(15rem, 13vw, 20rem)',
        '--kawaii-docs-panel-gap: clamp(1rem, 1.35vw, 2.4rem)',
        '--kawaii-docs-shell-min-height:',
        'column-gap: var(--kawaii-docs-panel-gap)',
        'grid-template-columns:',
        'minmax(0, 1fr)',
        'margin-inline: auto !important',
        '--kawaii-docs-shell-max-width: 220rem',
        'max-width: var(--kawaii-docs-shell-max-width) !important',
        '--kawaii-docs-shell-width: calc(100% - clamp(1.5rem, 3vw, 6rem))',
        'width: var(--kawaii-docs-shell-width) !important',
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
    for root in (ROOT / 'docs/_site', ROOT / 'src/LocalGPT/wwwroot/help-docs'):
        if not root.exists():
            continue
        for html in root.rglob('*.html'):
            value = html.read_text(encoding='utf-8')
            if 'localgpt-kawaii.css?v=' in value and f'localgpt-kawaii.css?v={css_hash}' not in value:
                fail(f'stale Kawaii CSS cache key in {html.relative_to(ROOT)}')

    with zipfile.ZipFile(SNAPSHOT) as archive:
        snapshot_css = archive.read('styles/localgpt-kawaii.css')
        snapshot_script = archive.read('styles/localgpt-kawaii.js')
        if snapshot_css != source:
            fail('tracked GitHub Pages snapshot CSS differs from the maintained theme source')
        if snapshot_script != script:
            fail('tracked GitHub Pages snapshot JavaScript differs from the maintained theme source')
        status = json.loads(archive.read('documentation-status.json'))
        if status.get('version') != '2.3.5':
            fail('tracked GitHub Pages snapshot is not version 2.3.5')
        if 'LocalGPT-2.3.5.pdf' not in archive.namelist():
            fail('tracked GitHub Pages snapshot is missing LocalGPT-2.3.5.pdf')

    print('Kawaii documentation layout audit passed: equal rails, symmetric gaps, full-width articles, and synchronized site assets.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
