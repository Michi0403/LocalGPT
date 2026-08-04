# GitHub Pages Kawaii publishing fix

This package updates `.github/scripts/extract-shipped-docs.py` so the Pages workflow can publish the exact themed `wwwroot/help-docs` tree already shipped in LocalGPT release ZIPs.

Changes:

- Decode JSON, HTML, CSS, and JavaScript as `utf-8-sig`, accepting the UTF-8 BOM emitted by the Windows documentation build.
- Validate the current shipped Kawaii theme markers instead of obsolete marker names.
- Preserve relative asset URLs, the pink Kawaii styling, pointer paw trail, click scratch effect, preferred light/dark theme selection, and hidden DocFX theme toggle.
- Keep the existing archive traversal and duplicate-member safeguards.

Static verification performed without .NET:

- Created a Windows-style ZIP with backslash member paths from the supplied `wwwroot/help-docs` tree.
- Confirmed BOM-prefixed `documentation-status.json` is accepted.
- Confirmed 913 HTML files and 853 API HTML pages were extracted.
- Confirmed the Kawaii CSS and JavaScript hashes matched after extraction.
