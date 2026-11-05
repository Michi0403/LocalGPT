# LocalGPT 6.5.1 — focused runtime regression repairs (source candidate)

## Repaired

- `LocalAiAcquisitionService.DownloadSourceAsync` explicitly disposes the HTTP input and exclusive FileStream output before moving the downloaded archive. Publishes a content-addressed, immutable path so an installer reading a previous archive is not overwritten.
- OneWireSecurity Razor peer/capability collections are grouped by case-insensitive identifiers before keyed rendering to prevent duplicate-key circuit crashes. Persistent trust is not modified.
- Local-AI Hugging Face results now use DevExpress DxGrid with full-text search **within loaded results**, column filters, sortable properties, grouping and pagination. The server-side query remains the Hugging Face provider API's metadata search, not an exhaustive full-catalog index.
- Opening the Local AI section supervises a Python.NET status probe; opening Toolchains supervises a refresh. Existing manual refresh controls remain available.
- Adds mandatory PowerShell `Assert-LocalAiDownloadAndPeerUI.ps1` gate and source-level tests.

## Still open / not asserted

- No Windows .NET/DevExpress compilation or live source-download and Whisper installation has been run here. Actual runtime acceptance remains required.
- The backend image/video generation services/DXFunctions already exist. Their end-to-end package/model installation, GPU runtime, artifact rendering and PublisherStudio workflow were not demonstrated or completed by this patch.
- Remote-model-provider full catalog search is limited by provider search API; the grid searches the locally loaded result set.
