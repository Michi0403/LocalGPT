# LocalGPT v0.1.5 — Organic Runtime Completion

## Build and startup repairs

- Removed interpolation from the Council member system-prompt raw string. The self-assessment JSON is now literal content and model/member values are inserted afterward with explicit placeholder replacement, eliminating CS1733/CS9006/CS8076/CS8089 brace parsing failures.
- Preserved the installer and desktop compatibility contract: default application port 5000, public read-only `Program.Port`, positional installer port input, `--port`, `Program.BaseUrl`, and explicit `System.Threading.Volatile` access.
- The WinUI wrapper now forwards the real command-line arguments into LocalGPT, stays on the UI dispatcher while creating the window, reconnects only to a verified LocalGPT `/health` endpoint when the selected port is already owned, and presents a visible startup error instead of a blank WebView.
- `server.json` is written only after Kestrel has started and is removed only by the process that owns it.

## Council and human collaboration

- Database-backed Council teams are selectable from Chat and editable through the Council Teams page.
- Existing untouched seed teams are upgraded losslessly to seed revision 4; user-modified teams are never overwritten.
- The live Council toolbar is always visible. It can refresh running sessions, join a selected heartbeat, enable the human Council-member role, and submit corrections for the next heartbeat without cancelling the active generation.
- General, OpenSCAD, Spreadsheet and Learning Round presets remain initially seeded for every installation.
- The Learning Round can use chat memory, application logs, Council knowledge, regex definitions and recorded facts through the maintained service/DX-function boundaries.

## Offline-first limits and media

- Chat upload has no application MIME/extension filter.
- Upload count, upload size, multipart request size, Kestrel request size, SignalR message size and one-wire message size use the largest representable framework settings instead of small cloud-oriented defaults.
- Unknown media remains available to capable open-source models or connected organic plugins; LocalGPT does not reject it by format before provider/plugin inspection.

## Regex and knowledge maintenance

- All 49 legacy `[GeneratedRegex]` functions supplied for LocalGPT have corresponding initially seeded database regex definitions; the seed catalog currently contains 59 built-in/general architecture patterns.
- Added a source safeguard that fails when a generated regex no longer has a matching database seed.
- Regex list, read, test and upsert operations remain controller/DX-function-backed and are advertised through the same capability preparation/discovery path used for organic plugins.
- Knowledge self-maintenance can insert regexes and learning information without an approval prompt, while model self-assessments remain untrusted until explicitly approved.

## Shared protocol

- LocalGPT.WireProtocolVersion is version 1.4 and remains the authoritative reusable project inside the LocalGPT repository.
- The exact project and contracts are mirrored into PublisherStudio and checked byte-for-byte.
- Bidirectional human/automated interaction requirements, serialized interaction information, capability/skill/UI state, hardware roads, model token ranges, scheduling and debounce metadata remain part of the shared contracts.

## Persistence and regression safeguards

- Retains lossless migration backup/adoption/repair, partial-schema column repair, Council-team repair and the previous chat-message replacement transaction fix.
- Source-package checks cover project-reference closure, critical implicit C# sources, installer/port wiring, raw-string safety, shared protocol sources, Council controls and regex seed parity.

## Validation performed in the packaging environment

- `python tests/localgpt_hotfix3_source_contracts.py`: passed.
- 59 built-in regex seeds, all 49 generated-regex counterparts, 2 project references and 7 critical compilation sources were verified.
- XML, JSON, source-closure, protocol-mirror and archive hash checks are included with the delivery.
- Native Windows/.NET/WinUI/DevExpress compilation was unavailable here and is therefore not claimed.
