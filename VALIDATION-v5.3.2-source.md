# LocalGPT 5.3.2 source validation

## Scope

Source-only dependency-baseline cleanup. No .NET build, restore, publish or GitHub repository access was performed.

## Dependency baseline

- `src/LocalGPT/LocalGPT.csproj` retains `DevExpressVersion` **25.2.10**.
- Maintained Microsoft/System application package references remain on **10.0.12** where they use the .NET 10 patch line.
- `docs/DocfxDependencies.csproj` now pins its private `System.Formats.Nrbf` tooling dependency to **10.0.12**.
- Repository text/code contains zero superseded DevExpress-version occurrences.
- Unreferenced root installer executables carrying an earlier .NET 10 patch runtime payloads are no longer part of the source package.

## Preserved behavior

The 5.3.1 compile repair and all 5.3.0 source-evidence, archive-extraction, progressive whole-file reading, context-aware DXFunction access and game sandbox behavior are unchanged.

## Source checks performed

- zero superseded DevExpress-version occurrences across repository text/code;
- active DevExpress version is 25.2.10;
- active application/package patch references reviewed are 10.0.12;
- browser cache-buster identity is 5.3.2 without modifying Bootstrap/Bootswatch library version comments; and
- ZIP integrity was verified after packaging.
