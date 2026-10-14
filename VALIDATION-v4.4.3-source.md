# LocalGPT 4.4.3 source validation

This is source-only validation. No `dotnet`, NuGet restore/build/publish, EF CLI, macOS `productbuild` execution, GitHub access, GitHub API operation, or other online repository operation was performed in this environment.

## Release-specific contracts

- `src/LocalGPT/LocalGPT.csproj`, `src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj`, and `src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj` identify the release as `4.4.3`; the maintained single-digit minor/patch version policy is satisfied.
- Routable Razor pages retain the maintained `@rendermode InteractiveServer` contract, excluding only the intentional static error fallback.
- `MainLayout.razor` and `Drawer.razor` are byte-for-byte identical to the supplied 4.4.2 source, so the restored navigation/menu implementation was not reopened by this change.
- The Neon Relay ASCII hot-seat team persists `localgpt.ascii.surface.required`; selection/start paths publish the shared ASCII surface before Council provider work, and both Display Director phases re-check `localgpt.ascii.surface.get` before display mutation.
- `/chat` hosts the shared `ChatGameConsole` in a DevExpress `DxPopup`; hiding the popup republishes presentation state without terminating the underlying chat, Council run, game session, or Operator jobs.
- Ordinary ASCII-console controls use `DxButton`, `DxComboBox`, `DxTextBox`, and `DxMemo`. Native buttons remain only for the established low-level `data-game-action` keyboard/gamepad DOM contract.
- macOS packaging emits Installer Distribution metadata with `<title>LocalGPT <version></title>`, builds through the distribution/package-path path, and validates the expanded final package title before accepting the PKG.
- Mermaid documentation rendering waits for browser font readiness, uses SVG labels, and applies bounded flowchart wrapping; authored, embedded, generated-help, and tracked Pages-snapshot JavaScript/cache references are synchronized.

## Source checks executed

- `python3 build/audit_release_4_4_3.py` — passed.
- `python3 build/audit_chat_ascii_console.py --root .` — passed; 17 checks.
- `python3 build/audit_application_architecture.py --root . --product localgpt --mode all` — passed.
- `python3 build/audit_service_resilience.py --root . --product localgpt` — passed; 2470 service methods own try/catch + diagnostics, with 29 iterator and 3 direct Program/Startup exclusions reported by the maintained audit.
- `python3 build/audit_async_continuations.py --source-root src/LocalGPT` — passed for 268 source files / 3332 await tokens; 2971 `ConfigureAwait(false)`, 138 reviewed renderer-affine `ConfigureAwait(true)`, 218 explicitly configured await-using disposals, and 5 configured async streams.
- `python3 build/audit_configurable_behavior_policy.py` — passed.
- `python3 build/audit_xround_wiring.py` — passed.
- `python3 build/audit_powershell_variable_interpolation.py` — passed; no invalid non-scope `$name:` references found in maintained PowerShell files.
- `python3 build/audit_cross_platform_boundaries.py` — passed; 22 checks with no platform leaks detected.
- Final source ZIP CRC test — passed.
- Clean re-extraction of the final source ZIP followed by reruns of the 4.4.3 release audit, ASCII-console audit, application-architecture audit, service-resilience audit, async-continuation audit, configurable-policy audit, X-Round audit, PowerShell interpolation audit, and cross-platform-boundary audit — passed.

## Build limitation

A compiler/runtime result is intentionally not claimed. The final Windows/macOS build and the macOS Installer UI behavior still need to be exercised in the normal .NET/DevExpress/macOS release environment. The source-level PKG contract is validated statically here, but `productbuild` itself was not run.
