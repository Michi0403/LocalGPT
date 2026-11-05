# LocalGPT 6.7.8 — Council PowerShell schema guard repair

## Context

On Windows PowerShell 5.1, the mandatory Debug `Assert-CouncilXRoundWiring.ps1` guard failed under `Set-StrictMode -Version Latest` while auditing `LocalAiRuntimeDxAiFunctions.cs`:

```
PropertyNotFoundStrict: Die Eigenschaft "negativePrompt" wurde für dieses Objekt nicht gefunden.
build/Assert-CouncilXRoundWiring.ps1:124
Directory.Build.targets: MSB3073 (exit code 1)
```

**Root cause:** The guard directly accessed `$schema.properties.negativePrompt` while looking for optional image-generation exclusions in *all* DX function schemas. A schema without that optional property is valid; strict-mode PowerShell throws on the missing member before the `-eq $null` expression can safely check it. This is a guard failure, not a required parameter in the production image tools.

## Repairs

- Use `PSObject.Properties['properties']`, `PSObject.Properties['negativePrompt']`, `PSObject.Properties['required']`, and `PSObject.Properties['default']` to inspect optional JSON fields without triggering `PropertyNotFoundStrict`.
- Continue to enforce that exactly the two maintained image generation/edit schemas expose optional `negativePrompt` with empty-string default, and that the field is not in `required`.
- Convert malformed JSON function-schema input into a named source-contract finding instead of silently skipping it.
- Keep the same mandatory Council guard in normal Debug and Release MSBuild targets. No warning suppression, test bypass, or downgraded policy.
- Advance the application version to 6.7.8.

## Scope

There are no changes to image inference behavior, database schema, Council execution, user chat history, persistent settings, UI recovery, or approvals. This repair specifically addresses the build-blocking PowerShell guard introduced in 6.7.7.

The underlying Blazor `TryClaimPendingStream` disconnect root cause remains unverified; the 6.7.7 recovery work is preserved but not reclassified as proven.
