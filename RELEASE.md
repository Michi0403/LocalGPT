# LocalGPT 2.8.5 multilingual localization release

LocalGPT 2.8.5 adds built-in Spanish, French, Japanese, and Ukrainian catalogs alongside the existing German and English catalogs. The existing localization service already discovers built-in JSON catalogs dynamically, so the new cultures integrate into the current language selector and fallback model without changing application architecture or render-mode boundaries.

No GitHub access or .NET/MSBuild invocation was used to prepare this source release.

## Compatibility

- LocalGPT, LocalGPTWebviewWrapper and LocalGPTInstallerConsole are 2.8.5.
- 1-Wire protocol remains 2.1.1.
- Existing 19 InteractiveServer render-mode directives are unchanged.
- No database migration is required.
