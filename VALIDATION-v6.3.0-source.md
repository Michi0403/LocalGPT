# LocalGPT 6.3.0 — source verification and release hold

## Verified in this environment

- Original source baseline: `LocalGPT-6.2.9-pdf-chunk-recovery-source.zip`.
- Static regression suite, including added MFA/Stop/source-review contracts: see recorded test result in release response.
- Changed C# / Razor paths reviewed and archived intact; all old files retained.
- ZIP CRC verification and exact member inventory comparison performed before delivery.
- Existing build architecture gates and `AGENTS.md` untouched.
- Customer log `Eingefügter Text(4).txt`: current 6.2.8 run curation passed for two ZIPs (2,236+997 safely extracted entries), yet `learning-inventory` failed with 0/1 complete semantic files and 0/2 covered archive roots.
- Exported Council run snapshot: a tournament run remained `Running`, `CompletedAtUtc = null` at the time of export; this is not proof the process never ended later.

## Not verified / release-blocking acceptance checks

- `dotnet` / `pwsh` are unavailable in this execution environment. **No full .NET compilation or Windows PowerShell architecture build can be claimed.**
- Native DevExpress `DxPopup` and TOTP verification require an interactive 1-Wire enrollment test.
- Native DevExpress Stop, explicit stop and navigation cancellation must each be tested against Ollama and the persisted Council run ledger. In particular, verify provider resource reclamation.
- A two-ZIP Learning Round must prove service-origin source receipts are accepted, the selected files truly reach EOF, `localgpt.learning.maintain` is invoked only as authorized, projects are synchronized, and noncompleted file checklists remain pending.
- Acceptance on a clean installed Windows release is essential before any public release.

This archive is provided for test/repair and is **not designated production-verified**.
