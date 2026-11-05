# LocalGPT 6.9.0 — Council Rejoin Static/Compile Repair

## Reported Windows Debug build blockers

- `Assert-ApplicationStaticPolicy.ps1` failed on `Chat.LiveCouncil.razor.cs`: the Council marker and spool projection helpers were mistakenly declared `static` despite the application policy forbidding static application methods.
- `CS0120` in `HasCouncilRunMarker`: the incorrectly static helper called the injected, instance-scoped `CouncilText` service.

## Fix

- `HasCouncilRunMarker` is now an instance method. The precise saved-Council-run marker check and injected text service are unchanged.
- `BuildCouncilSpoolChatProjection` is now an instance method. The bounded Council evidence fallback and its contents are unchanged.
- Application version advanced to `6.9.0`.

No controller, Council execution, game engine, installer, SQLite recovery, Blazor lifetime, renderer, or rejoin workflow code was changed beyond those declaration modifiers.

## Verification limits

Offline checks reproduce the architecture guard's static signature detection, fail on the original 6.8.9 helpers, and pass after both modifiers are removed. The exact method bodies/call sites are preserved. No PowerShell/.NET/Windows build or interactive rejoin was run. This is a source-level build repair candidate, not a claim of a successful full build or stable Rejoin.
