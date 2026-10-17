# LocalGPT 6.0.1 static source validation

Base archive: `LocalGPT-6.0.0-source.zip`.
Original retained files: 2145 (100%). Existing source files changed: 9. New files (excluding this report): 1.

## Checks performed

- Parsed application and release-packaging project XML: versions 6.0.1 / 1.1.2.
- Both top-level PowerShell build entrypoints resolve the automatic packaging helper; Debug also supplies the helper to documentation.
- Cached invalid NuGet package recovery rebuilds and re-runs the unchanged source-license SHA-256 guard.
- Packing emits the **original byte streams** for both required `licenses/` NuGet root entries, followed by independent verification.
- All directly invoked literal build scripts referenced by both entrypoints are present.
- Native Ollama retry is available only when an unrestricted/normal broker policy is in force and the catalog is large; strict policies and DXFunction approval checks are retained.
- All four upload-workspace handler names remain registered in source.
- DocFX architecture chapter contains the automatic-tool reaction state machine and one-command release process flow.
- Archive retention and CRC to be checked after packaging.

## Limitations / not tested

No .NET SDK, PowerShell 5.1/pwsh, local Ollama, macOS signing tools, NuGet pack/tool-install, Edge or Windows Visual Studio build was executed. Checks are structural and **do not prove** the release succeeds, all original features work, or local single-model/Council provider file access works end to end. The observed Council team-key hardcoding in `MultiModelCouncilService` remains for backward compatibility and needs a separately designed configuration migration; it was not silently removed.

## Modified existing files

- `Build-LocalDevelopment.ps1`
- `build/Assert-RepositoryPowerShellBoundary.ps1`
- `build/Ensure-ReleasePackagingPackage.ps1`
- `build/Publish-ReleasePackagingPackage.ps1`
- `docs/architecture/council-runtime.md`
- `src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj`
- `src/LocalGPT/LocalGPT.csproj`
- `src/LocalGPT/Services/OllamaThinkingChatClient.ToolExecution.cs`
- `src/LocalGPT/Services/OllamaThinkingChatClient.Transport.cs`
