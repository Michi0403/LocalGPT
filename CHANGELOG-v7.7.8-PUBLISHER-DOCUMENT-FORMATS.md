# LocalGPT 7.7.8 · Passable Pangolin

The Undersuperiority Series. Pairs with PublisherStudio 4.9.4.

## Added — PublisherStudio documents over 1-Wire
- `publisher.document.create` accepts the kinds `rtf`, `odt`, `html`, `txt` and `epub` in addition to `spreadsheet`, `text` (Word DOCX) and `pdf`.
- Every returned file is checked against its approved format before it is offered: PDF/RTF headers, strict UTF-8 for TXT/HTML (HTML must contain an html element), Open XML parts for XLSX/DOCX, OpenDocument and EPUB `mimetype` plus `content.xml` / `META-INF/container.xml`.
- `/api/onewire/work/{id}/document` serves each kind with its MIME type and file extension; Chat shows a matching download label.

## Checks actually performed
- Diff review and brace balance only. **No .NET build or 1-Wire test has been run.**
