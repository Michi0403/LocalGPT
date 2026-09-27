# LocalGPT 4.9.5 source validation

Source-only validation was performed without .NET/MSBuild/NuGet and without GitHub/network access.

- Verified `LocalGPT.csproj` version is `4.9.5`.
- Verified ChatMicrophone uses the repository-owned `oi-microphone` / `oi-media-stop` glyphs and its JavaScript cache-buster is `4.9.5`.
- Verified approval lookup now searches same-operation decisions and applies exact matching to pending/one-use decisions while allowing session/persistent approvals to cover later parameter sets.
- Verified Python `.py` code-generation reviews with a Council run and no project bind to `IProjectArchitectureService.EnsureCouncilRunProjectAsync` before review validation/persistence.
- Verified the existing project-backed execution route still resolves a project workspace, writes reviewed files, registers the revision workspace, scans tracked files, and returns `WorkspacePath` plus `DownloadUrl`.
- Verified compiler-team handoff text explicitly requires Python project/toolchain/regex evidence, direct artifact URL, and manual filesystem fallback path.

No compile claim is made by this document.
