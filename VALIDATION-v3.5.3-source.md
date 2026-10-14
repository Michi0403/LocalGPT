# LocalGPT 3.5.3 source validation

## Scope

This maintenance pass addresses the Windows `System.IO.IOException` reported by the authoritative release build after `linux-x64` Full publishing succeeded and `LocalGPT.ReleasePackaging` entered TAR.GZ materialization. The supplied log shows the RID-neutral LocalGPT build, full DocFX/PDF generation, all three Windows application/setup publishes, and the `linux-x64` application publish succeeding before the packaging helper fails in `FileSystem.MoveFile` from `CreateTarGz`.

No .NET SDK or PowerShell runtime is available in this validation environment, so no local compiler, PowerShell parser, native package materialization, or installer execution is claimed here.

## Root cause and repair

`CreateTarGz` used C# `using var` declarations for the temporary `FileStream`, `GZipStream`, and `TarWriter`, then called `File.Move` in the same scope. C# using declarations dispose at the end of the enclosing scope, so on Windows the temporary archive was still open with `FileShare.None` when `File.Move` ran. That is a deterministic sharing violation.

- TAR.GZ creation now uses nested `using (...)` scopes that end before the final move.
- DEB creation had the same latent pattern and now closes its exclusive AR output stream before the final move.
- The final commit has a bounded retry for transient destination sharing after all LocalGPT-owned handles are closed.
- `LocalGPT.ReleasePackaging` is bumped from 1.0.0 to 1.0.1; its project, package publisher, tool installer, LocalGPT release build, shared cache, and upload bundle use 1.0.1.
- Windows/Linux/macOS RID and package lanes remain unchanged.

## Static source validation

The maintained source tree passes:

- LocalGPT 3.5.3 release audit.
- Architecture policy audit.
- Async continuation audit: 259 files, 2,979 await tokens, 2,624 `ConfigureAwait(false)`, 135 renderer-affine `ConfigureAwait(true)`, 215 explicitly configured await-using disposals, and 5 configured async streams.
- Service resilience audit: 2,188 service methods; 29 iterator/yield methods and 3 direct Program/Startup methods are handled by their maintained policy.
- Configurable Council behavior-policy audit.
- Provider stream repetition-policy audit.
- Cross-platform boundary audit: 22 checks.
- XML documentation audit: 10,251 direct C# declarations across 651 maintained source files, plus 45 Razor component types and 776 direct `@code` members.
- Structured metadata parsing: 10 XML/MSBuild files and 38 JSON files parse cleanly with duplicate JSON keys rejected.
- Publish-profile matrix still resolves all seven RIDs: win-x64, win-x86, win-arm64, linux-x64, linux-arm64, osx-x64, and osx-arm64.

The final delivery ZIP is CRC/path/duplicate checked, freshly extracted, and the critical release/architecture/resilience/cross-platform/XML checks are rerun against that exact extraction before handoff.
