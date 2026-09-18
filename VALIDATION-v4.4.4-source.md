# LocalGPT 4.4.4 source validation

This is source-only validation. No `dotnet`, NuGet restore/build/publish, EF CLI, macOS `productbuild` execution, GitHub access, GitHub API operation, or other online repository operation was performed in this environment.

## Reported compiler failure addressed

The supplied Windows build reached the LocalGPT Razor compilation stage after its maintained pre-build checks passed, then failed with three `RZ9986` errors in `Components/Shared/ChatGameConsole.razor` on the Chat, Game and Operator `DxButton.CssClass` attributes. The 4.4.3 markup mixed literal class text with an inline Razor conditional inside a component attribute. All three values are now single explicit Razor expressions and preserve the same base classes plus the conditional `is-active` suffix.

## Release-specific contracts

- `src/LocalGPT/LocalGPT.csproj`, `src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj`, and `src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj` identify the release as `4.4.4`; the maintained single-digit minor/patch version policy is satisfied.
- `build/audit_chat_ascii_console.py` rejects the mixed-content DevExpress `CssClass` form and requires the three mode buttons to use the pure-expression binding.
- Routable Razor pages retain the maintained `@rendermode InteractiveServer` contract, excluding only the intentional static error fallback.
- `MainLayout.razor` and `Drawer.razor` remain byte-for-byte identical to the supplied 4.4.2 source.
- The 4.4.3 ASCII-surface requirement, DevExpress popup lifetime, ordinary DevExpress console-control ownership, macOS Installer title validation, Mermaid diagram/cache synchronization, and shell/menu protections remain covered by the 4.4.4 release audit.

## Source checks executed

- `python3 build/audit_release_4_4_4.py` — passed.
- `python3 build/audit_chat_ascii_console.py --root .` — passed; 18 checks.
- `python3 build/audit_application_architecture.py --root . --product localgpt --mode all` — passed.
- `python3 build/audit_service_resilience.py --root . --product localgpt` — passed; 2470 service methods own try/catch + diagnostics, with 29 iterator and 3 direct Program/Startup exclusions reported by the maintained audit.
- `python3 build/audit_async_continuations.py --source-root src/LocalGPT` — passed for 268 source files / 3332 await tokens; 2971 `ConfigureAwait(false)`, 138 renderer-affine `ConfigureAwait(true)`, 218 explicitly configured await-using disposals, and 5 configured async streams.
- `python3 build/audit_configurable_behavior_policy.py` — passed.
- `python3 build/audit_xround_wiring.py` — passed.
- `python3 build/audit_powershell_variable_interpolation.py` — passed.
- `python3 build/audit_cross_platform_boundaries.py` — passed; 22 checks.
- Final source ZIP CRC test and clean re-extraction/content comparison — passed.
- The same Python source audit set above was rerun from the cleanly re-extracted final ZIP — passed.

## Build limitation

A compiler/runtime success result is not claimed here because this environment does not run `dotnet`. The next authoritative compiler check is the user's normal Windows/DevExpress build. This 4.4.4 release specifically removes the three reported `RZ9986` source patterns and adds a static regression guard against their return.
