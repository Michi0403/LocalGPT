# LocalGPT 3.5.0 source validation

LocalGPT 3.5.0 is a source-maintenance handoff. This environment intentionally does not use GitHub and has no .NET SDK or PowerShell runtime, so it does not claim a compiler or native-package build result.

The exact delivery ZIP is validated after fresh extraction. The maintained static gate requires these checks to pass on that extracted copy:

- `python3 build/audit_release_3_5_0.py`
- `python3 build/audit_cross_platform_boundaries.py`
- `python3 build/audit_application_architecture.py --root <root> --product localgpt --mode all`
- `python3 build/audit_async_continuations.py --source-root <root>/src/LocalGPT`
- `python3 build/audit_service_resilience.py --root <root> --product localgpt`
- `python3 build/audit_provider_stream_repetition_policy.py`
- `python3 build/Assert-XmlDocumentationCoverage.py <root>`
- the repository system-variable-initialization rule mirrored byte-for-byte from `build/Assert-SystemVariableInitialization.ps1`
- XML/JSON source metadata parsing and Python build-script syntax compilation
- ZIP CRC, duplicate-entry, traversal-entry, and version-identity checks

The release audit specifically protects the reported `EmbeddedTelemetryIngressService.cs` compiler-corruption class, requires the persisted `EmbeddedTelemetryMaximumSnapshots` policy instead of the old source-owned retention ceiling, keeps the 3.4.9 operator-policy fixes, verifies Debug HTML-only documentation does not require a Release PDF, and checks the seven-runtime cross-platform release matrix and native package paths.

The supplied LocalGPT 3.3.0 baseline is also compared for explicit Blazor render-mode boundaries. All 20 explicit `@rendermode` declarations present in that baseline remain present in 3.5.0, including the 15 maintained `InteractiveServer` page/layout boundaries checked by the release audit.

A real Windows/macOS/Linux build remains the authoritative compiler and native-package test. Any compiler finding from that run should be treated as a release blocker and repaired in the next version.
