# LocalGPT 4.0.2 source validation

## Validation scope

This release is source/static validated. No `dotnet build`, `dotnet test`, `dotnet publish`, native macOS packaging, signing/notarization, PKG execution or GitHub access was used in this environment.

## Runtime evidence addressed

The supplied macOS log showed Ollama discovery completing successfully while the setup snapshot subsequently failed through an obsolete NVIDIA probe stack after `Interop.Sys.GetCwd()` failed. It also showed attempts to write the application log below `/Applications/LocalGPT.app`. The captured `/install` HTML therefore stayed on the setup refresh failure/loading state instead of rendering the guided provider controls.

The maintained 4.0.1 source no longer contained that old hardware-probe implementation. 4.0.2 therefore treats this as an updater/runtime ownership problem in addition to keeping setup probes fail-soft.

## 4.0.2 source checks

- All maintained LocalGPT release version surfaces are `4.0.2` and preserve the one-digit minor/patch policy.
- Unix publish validates the freshly published managed assembly version before native packaging and stamps `RELEASE-VERSION.txt` plus `SOURCE-SHA256.txt`.
- The macOS launcher validates both stamps and requires a 64-character hexadecimal source fingerprint.
- Runtime `server.json` publishes `Version` and `ExecutablePath`.
- The macOS launcher rejects a responding endpoint whose PID/version/executable identity does not belong to the current installed bundle; older endpoint files without the new identity are stale by definition.
- Stale application processes are terminated before a replacement runtime starts.
- PKG `preinstall` stops the existing application before `/Applications/LocalGPT.app` is replaced and clears the logged-in user's runtime endpoint.
- PKG `postinstall` reopens the newly installed application when the previous version had been running.
- Packaged startup uses the per-user runtime working directory and per-user LocalGPT log path.
- Application startup logs assembly version, executable path, base directory and working directory.
- Optional NVIDIA discovery is fail-soft while cancellation remains propagating.
- Existing setup recovery, fast Windows Ollama updater, provider-model resolution, console progress handling and explicit update confirmation remain present.
- Localization catalogs retain exact key parity.
- Render-mode ownership remains unchanged: intended routed pages retain `InteractiveServer`, the Error page remains static, and `InitialSetupAssistantPanel.razor` owns no nested render mode.
- Source-package hygiene rejects `bin`, `obj`, `__pycache__`, Python bytecode and traversal/symlink artifacts.

## Expected macOS field evidence

A valid 4.0.2 package should log launcher identity similar to `Runtime identity: version=4.0.2 source=<64 hex> app=...`, followed by application identity similar to `LocalGPT runtime identity: assembly=4.0.2.0; executable=/Applications/LocalGPT.app/...; base=...; workingDirectory=...`.

When upgrading while 4.0.1 or older is still running, the PKG should stop the old runtime before replacing the bundle; reopening must never attach to an endpoint whose version/executable identity differs from 4.0.2.

## Static audit results

The maintained Python source audits passed on the final 4.0.2 tree before packaging:

- release audit: 15 routed pages, 14 routed `InteractiveServer` boundaries, 6 localization catalogs and 6 provider profiles;
- application architecture policy: passed;
- cross-platform boundaries: 22 checks passed;
- async continuation policy: 259 source files, 3108 await tokens, 2751 `ConfigureAwait(false)`, 135 renderer-affine `ConfigureAwait(true)`, 217 configured async disposals and 5 configured async streams;
- provider-qualified Council audit: 282 checks passed;
- service resilience: 2252 service methods passed, with 29 yield methods and 3 direct Program/Startup methods intentionally skipped by that audit;
- XML documentation: 10420 C# declarations across 656 maintained source files plus 802 direct Razor `@code` members across 45 component types;
- generated macOS launcher, PKG `preinstall`, and PKG `postinstall` templates all pass POSIX `/bin/sh -n` syntax validation after placeholder substitution.

PowerShell itself is unavailable in this environment, so the PowerShell packaging source was not parser-executed here. The native PKG lifecycle still requires the Mac field test to validate Installer.app behavior, process handoff, signing/notarization and relaunch semantics.
