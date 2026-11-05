# LocalGPT 3.0.7 source changelog

## Nullable console exit-code compile repair

LocalGPT 3.0.7 is a focused source repair over 3.0.6. It preserves the 3.0.5/3.0.6 AI-guided hardware/provider/model setup and repairs the next compiler blocker reported from the authoritative local .NET build.

### Fixed

- Repaired `ConsoleCommandService.ExecuteAsync` compiler error `CS0173` by giving the nested timeout/process-exit conditional an explicit nullable integer target (`int? exitCode`).
- Preserved the intended three-state exit-code semantics: `-2` for LocalGPT's timeout sentinel, the real process exit code after normal termination, and `null` only when no process exit code is available.
- Kept the existing success/status/log/result flow unchanged; the repair is type-resolution only and does not hardcode platform-specific behavior.
- Updated the maintained application/wrapper/installer versions and opt-in CanIRun.ai runtime identification to 3.0.7.

### Preserved

- The AI-guided initial setup, shared PowerShell/Bash/Cmd/direct console abstraction, provider bootstrap, model installation, HWiNFO/manual hardware evidence, optional attributed CanIRun.ai lookup, benchmark-team setup and Council/DXFunction wiring remain intact.
- Existing fresh-human-confirmation gates for consequential commands and provider/model operations are retained.
- LocalGPT 1-Wire protocol remains `2.1.1`.
- No EF migration or schema change is introduced by 3.0.7.
- InteractiveServer render-mode boundaries are not intentionally changed by this release.

### Version

- LocalGPT: 3.0.7
- LocalGPTWebviewWrapper: 3.0.7
- LocalGPTInstallerConsole: 3.0.7
