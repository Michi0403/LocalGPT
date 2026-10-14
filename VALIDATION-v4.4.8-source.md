# LocalGPT 4.4.8 source validation

## Scope

Source-only validation for the Kernel Creature Tournament ASCII animation release. No `dotnet` build/restore/publish command, NuGet operation, GitHub access, GitHub API call, or other online repository access was used.

## Behavioral checks

- Maintained Kernel Creature Tournament seed requires the shared ASCII surface.
- A bounded lineup `ascii-sequence` is generated after creature introductions.
- A bounded replay `ascii-sequence` runs after every judge ruling in the tournament loop.
- The ASCII terminal extracts the newest complete sequence from live Council participant lanes before falling back to committed chat messages, so tournament animation is visible during the active run.
- Replay prompts explicitly forbid inventing new attacks, damage, HP/status changes, eliminations, winners, or future rulings.
- Tournament workflow remains tool-free and transcript-owned; no deterministic game session is created for this surface-only preset.
- Council game bootstrap treats a required ASCII surface with zero game runtime classes as a valid transcript-presentation case.
- Council seed version 31 refreshes untouched system presets while preserving user-owned/copied definitions under the existing seed lifecycle.

## Static validation

The working source passed:

- `build/audit_release_4_4_8.py`
- `build/audit_kernel_creature_tournament_ascii.py`
- `build/audit_chat_ascii_console.py --root .` — 24 checks
- `build/audit_ascii_doom_campaign.py --root .` — 32 checks
- `build/audit_application_architecture.py --root . --product localgpt --mode all`
- `build/audit_service_resilience.py --root . --product localgpt` — 2,483 service methods checked, with 29 yield-method and 3 Program/Startup skips
- `build/audit_async_continuations.py --source-root src/LocalGPT` — 270 source files, 3,346 await tokens, 2,930 `ConfigureAwait(false)`, 193 renderer-affine `ConfigureAwait(true)`, 218 explicitly configured async disposals and 5 configured async streams
- `build/audit_configurable_behavior_policy.py`
- `build/audit_xround_wiring.py`
- `build/audit_codegen_dxfunction_wiring.py`
- `build/audit_powershell_variable_interpolation.py`
- `build/audit_cross_platform_boundaries.py` — 22 checks
- `build/audit_kawaii_documentation_layout.py`

The same maintained audit set is rerun from the cleanly extracted release ZIP before handoff.

## Limitation

Runtime/model rendering quality and Windows compilation remain authoritative in the user's normal .NET/DevExpress environment because no .NET toolchain is invoked here.
