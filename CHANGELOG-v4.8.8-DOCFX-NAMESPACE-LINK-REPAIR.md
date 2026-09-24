# LocalGPT 4.8.8 — DocFX namespace link repair

## Fixed

- Added the missing XML documentation entry for the `runtimePlugins` dependency of `DxAiFunctionRegistry`, removing the CS1573 warning introduced with runtime extensions.
- Repaired generated DocFX namespace breadcrumbs without weakening strict local-link validation. With nested namespace layout, DocFX can emit clickable parent namespace segments even when no page exists for a parent that contains no public types (for example `LocalGPT.Runtime` above `LocalGPT.Runtime.Plugins`). The documentation postprocessor now leaves such missing parent segments as plain namespace text while preserving links to namespace pages that actually exist.
- Kept the existing pre-PDF HTML/link/accessibility validator strict; invalid local links outside that generated namespace-breadcrumb case still fail the build.

## Release identity

- Bumped active LocalGPT, installer, wrapper, HTTP user-agent and browser cache-buster identities from 4.8.7 to 4.8.8.

## Validation note

This is a source-only repair based on the real Windows 4.8.7 build log. No .NET/MSBuild/NuGet build, restore, publish or installer execution was performed in the handoff environment.
