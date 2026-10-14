# LocalGPT 3.2.2 source validation

Status: **SOURCE-NOT-COMPILED** in the preparation environment.

## Passed static checks

- LocalGPT 3.2.2 dedicated recovery/repetition/rejoin-copy audit: 45 checks.
- Application architecture policy audit.
- Provider-qualified Council audit: 282 checks.
- Configurable Council behavior-policy audit.
- Chat quick-preset row regression audit: 34 checks.
- Async continuation audit: 254 files, 2871 await tokens, 2594 `ConfigureAwait(false)`, 64 renderer-affine `ConfigureAwait(true)`, 208 configured await-using disposals, 5 configured async streams.
- Service resilience audit: 2107 service methods with owned try/catch + diagnostics; 29 yield methods and 3 direct Program/Startup methods skipped by policy.
- C# XML documentation audit: 9937 direct declarations across 632 maintained C# files.
- Razor XML documentation audit: 45 components / 752 direct `@code` declarations.
- `node --check` for `localgpt-chat-ui.js`.
- JavaScript diagnostics manifest refreshed for the deliberate `localgpt-chat-ui.js` change.
- Equivalent execution of `Assert-IteratorExceptionPolicy.ps1` method/brace scanning: zero non-baselined findings.
- Synthetic repetition-policy checks: historical short cycle detected, 80-token paragraph cycle detected, non-periodic long prose not classified.

## Protected-source checks

- `Chat.razor` SHA-256: `0d9ab6ed72f41eebbbf8839c54b5fda9a409d424a1fa11c87d2994352c837569`.
- `Chat.razor.css` SHA-256: `2a620187aa41712f53dddab92ee2ab834c4f46fe512925dce94efb387f28b0e4`.
- `DatabaseMigrationCompatibilityService.cs` SHA-256: `50bb2f62df4b6cfe5846063d5e4f20c2ab930a57cb95efa580ad6617f3a748ba`.
- EF migration directory has no delta against the source baseline.
- 1-Wire package/project version remains 2.1.1.

## Real-build errors addressed

The user-provided build output identified two release-source defects:

1. Iterator policy scanner classified `FormatUserVisibleCodePayload` as an iterator because the guard's simple brace scanner counted the literal `{` in `StartsWith('{')`. The equivalent source expression now uses `(char)123`, preserving behavior and the guard itself.
2. C# compiler error CS0136 in `Chat.PresetsAndCouncilConfiguration.razor.cs` was resolved by renaming the earlier pattern variable to `activeRunId`.

No .NET/DevExpress compile was available in this environment after those corrections, so compile success is intentionally not asserted.
