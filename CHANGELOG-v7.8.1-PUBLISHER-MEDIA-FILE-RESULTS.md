# LocalGPT 7.8.1 · Adequate Armadillo

The Undersuperiority Series. Pairs with PublisherStudio 4.9.7 · Tolerable Tapir.
Still not the Squirrel release; the Nuts engine is not here yet either.

## Since 7.7.9
- **7.8.0 · Passable Pangolin:** Council exports use the PublisherStudio capabilities the peer actually advertises (payload schema, input/output contracts taught to the team; `organic.plugin.invoke` + `organic.plugin.work.read` with download URL); Council preflight, run orchestration, Ollama request budget, automatic function policy and Chat provider runtime updates.
- **7.8.1 · Adequate Armadillo:** see below.

## Added: verified files from any PublisherStudio capability
- Results of `publisher.*` capabilities with `documentBytes` (media conversion, publication/website export) are checked against their declared MIME type before they are offered; Chat shows "Download <file name>".
- Large files announced as a Transfer are read automatically through `publisher.file.read` (`OneWire:PeerFileChunkTimeoutSeconds`, `OneWire:MaximumPeerFileBytes`) and must match the announced length, SHA-256 and MIME type.
- The `publisher.document.create` path and its contract gate are unchanged.

## Council
- Teams are taught advertised peer capabilities that share a skill with their preferred capabilities, so new PublisherStudio functions reach the media teams without seed edits.

## Build
- Documentation PDF on Windows: 32-page parts with 12-24 GB, 50 / 75 / 100 above 24 / 32 / 48 GB. macOS keeps small parts; `FUTURE2_DOCUMENTATION_BROWSER_PDF_CHUNK_PAGES` still overrides.

## MCP
- Unchanged. MCP clients reach the new capabilities through `organic.plugin.invoke` / `organic.plugin.work.read` (listed while `McpGateway:ExposeMutatingFunctions` is enabled).

## Checks actually performed
VS Debug build with all repository PowerShell gates (incl. PublisherStudio document contracts). End-to-end chunked transfer and the Council/MCP routes were **not** tested yet.
