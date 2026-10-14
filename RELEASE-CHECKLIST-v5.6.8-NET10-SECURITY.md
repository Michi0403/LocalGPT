# LocalGPT 5.6.8 source release checklist

Baseline: `LocalGPT-5.6.7-source`. New source: `LocalGPT-5.6.8-source`.

## Modified source files (6)

- `docs/DocfxDependencies.csproj`
- `global.json`
- `src/LocalGPT/Components/App.razor`
- `src/LocalGPT/LocalGPT.csproj`
- `src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj`
- `src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj`

## New release documentation

- `CHANGELOG-v5.6.8-NET10-SECURITY.md`
- `RELEASE-CHECKLIST-v5.6.8-NET10-SECURITY.md`
- `VALIDATION-v5.6.8-NET10-SECURITY.md`

## Maintained invariants

- All original source files preserved byte-for-byte except the listed modified files.
- No 10.0.12 active `PackageReference` survives in either application.
- Both repository SDK pins are `10.0.402` and relevant NuGet pins are `10.0.13`.
- DevExpress stays at 25.2.10.
- Publish/restore must be confirmed by owner on a build machine; no .NET build, NuGet restore, VS design-time build or runtime smoke test performed here.
