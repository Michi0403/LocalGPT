# LocalGPT 3.3.3 source validation

Static validation for the DocFX assembly-reference closure repair.

- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper are all version **3.3.3**.
- Documentation-specific builds materialize lock-file assemblies with `CopyLocalLockFileAssemblies=true`.
- DocFX metadata diagnostics are scanned for unresolved assembly references.
- Missing framework references can be copied from installed .NET shared-runtime directories into the temporary documentation input directory.
- Metadata is retried after dependency repair and is not considered successful while unresolved references remain.
- Release and local-development documentation assertions require an unresolved-reference count of zero.
- `documentation-status.json` exposes the unresolved reference list/count and dependency-repair count.
- No direct `System.Formats.Nrbf` application package reference was introduced.
- Existing `InteractiveServer` render-mode declarations remain untouched by this release.
- No .NET build/restore/test was run during preparation of this archive.
