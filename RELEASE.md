# LocalGPT 2.8.4 source compile repair

LocalGPT 2.8.4 repairs the two malformed async-disposal statement chains reported by the authoritative Windows .NET build. Stream lifetimes remain bounded exactly around the copy operations, while the strict explicit `ConfigureAwait(false)` disposal policy is preserved. No GitHub access or .NET build was used to prepare this source release.

## Compatibility

- LocalGPT, LocalGPTWebviewWrapper and LocalGPTInstallerConsole are 2.8.4.
- 1-Wire protocol remains 2.1.1.
- Existing InteractiveServer render-mode directives are unchanged.
- No database migration is required.
