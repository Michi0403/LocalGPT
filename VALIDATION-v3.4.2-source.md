# LocalGPT 3.4.2 source validation

Source-only validation; `dotnet` and PowerShell builds were not executed in the packaging environment.

The macOS 3.4.1 release log proves that DocFX reached 100%, produced the complete PDF, validated 1152 HTML pages / 1119 API pages, and then failed only during the final pinned Pages archive validation because the extracted snapshot reached 4,573,399,330 bytes.

3.4.2 closes that failure path by:
- removing DocFX's nested `pdf/LocalGPT-<version>.pdf` candidate after the canonical root PDF is copied, preventing the same multi-gigabyte PDF from appearing twice;
- validating the full release documentation/PDF before Pages preparation, then creating a tracked HTML-only Pages snapshot with release-PDF metadata and a link to the latest release;
- excluding the standalone PDF from each runtime ZIP while keeping the validated HTML/API documentation and release-PDF metadata, so seven runtime packages do not each duplicate a multi-gigabyte file;
- avoiding a whole-file Python read for known DocFX HTML-accessibility-fallback PDFs merely to probe `/StructTreeRoot`.

Static validation completed:
- LocalGPT 3.4.2 release audit passed.
- Cross-platform boundary audit: 22 checks passed.
- Application architecture audit passed.
- Async continuation audit passed for 259 source files.
- Code-generation/DXFunction source audit passed.
- Python syntax compilation of `.github/scripts/prepare-pages-artifact.py` passed.
- A unit check of the Pages copy path confirmed PDF exclusion, preserved PDF metadata, and release-link rewriting.
