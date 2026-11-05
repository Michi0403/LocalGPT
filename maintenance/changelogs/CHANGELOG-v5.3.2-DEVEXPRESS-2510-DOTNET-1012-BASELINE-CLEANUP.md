# LocalGPT 5.3.2 — DevExpress 25.2.10 / .NET 10.0.12 baseline cleanup

## Summary

LocalGPT 5.3.2 keeps the 5.3.1 functionality unchanged while making the repository's dependency contract agree with the maintained forward baseline shared with PublisherStudio.

## Fixed

- Confirmed the active LocalGPT project remains on `DevExpressVersion` **25.2.10**.
- Removed every repository reference to the superseded DevExpress patch, including obsolete historical release-audit retention literals that contradicted the current project baseline.
- Updated the documentation-only `System.Formats.Nrbf` tooling dependency from **an earlier .NET 10 patch** to **10.0.12** and aligned its historical audit assertions.
- Removed two unreferenced compiled installer executables from the source root that still embedded .NET an earlier .NET 10 patch runtime payloads. Source packages no longer carry those stale binaries.
- Preserved the 5.3.1 progressive upload-workspace read compiler repair and all 5.3.0 whole-file/source-evidence/Council behavior.

## Version

`5.3.1 -> 5.3.2`, following the maintained single-digit version-slot rule.
