# LocalGPT 3.6.5 source validation

Static/source validation only; no .NET restore, build, publish, DocFX render, GitHub access, or macOS native packaging tools were executed in this environment.

- Confirmed LocalGPT application, installer-console, and WebView wrapper versions are 3.6.5.
- Confirmed the macOS launcher uses `sysctl -n hw.optional.arm64` for physical Apple-Silicon detection, logs process architecture and `sysctl.proc_translated`, and has a native ARM64 re-exec path for translated launches.
- Confirmed runtime architecture failures include the exact `file` description and package architecture-manifest location.
- Confirmed package-time macOS architecture validation writes `Contents/Resources/native-architecture-manifest.txt` and reports exact incompatible Mach-O relative paths.
- Confirmed macOS Info.plist generation includes explicit `LSArchitecturePriority`; the ARM64 package additionally requests native execution.
- Confirmed the 3.6.4 syntax-aware async-continuation audit still passes and the three startup workers remain policy-compliant BackgroundServices.
- Confirmed README/LICENSE/THIRD-PARTY-NOTICES describe Future2, Apache-2.0 project ownership, and the separate DevExpress licensing boundary without claiming the repository grants a DevExpress license.
- Confirmed version-bearing XML/JSON files parse and the version-specific 3.6.5 source audit passes.
- The supplied source ZIP does not contain a repository `tests/` tree, so no omitted test suite is claimed as executed; validation used the maintained Python audits that are present in the supplied source.
- Confirmed no repository-local `bin` or `obj` directory is included in the delivered source ZIP.
