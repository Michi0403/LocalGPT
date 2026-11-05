# LocalGPT 5.7.7 — release license-notice propagation

- Added one consolidated `THIRD-PARTY-NOTICES.txt` that preserves the original PDFsharp 6.2.4 MIT terms and provenance, Open Iconic MIT/OFL license text, and identified Bootstrap, Bootswatch, and QR Code Generator notices, plus the complete standard Apache-2.0 text for app binaries. Repository originals remain untouched.
- `LocalGPT.csproj` and the setup console publish the text as one file beside their executables; the setup console also embeds the notice as an assembly resource for standalone executable distribution. Each Windows application/setup ZIP and Linux/macOS publish payload includes it.
- `LocalGPT.ReleasePackaging` NuGet tool embeds the notice and the original PDFsharp MIT license in `licenses/`. The NuGet tool metadata describes project-owned code as Apache-2.0, rather than misidentifying PDFsharp's MIT as the entire package's license.
- Release bundling copies one `THIRD-PARTY-NOTICES.txt` into `artifacts/release/<version>/`, adds it to SHA256SUMS, and treats a missing or modified notice as an incomplete release.
- PowerShell source notice validation and byte-for-byte NuGet content validation (SHA-256) prevent inadvertent omissions or stale notices.
- Version increments: LocalGPT 5.7.6 -> 5.7.7; packaging tool 1.0.6 -> 1.0.7; setup console 4.9.5 -> 4.9.6.
- No PDFsharp source modification, runtime PDF dependency addition, Python build dependency, documentation behavior change, or PublisherStudio modification.

## Limits and release checks

No .NET build, NuGet pack, native installer generation, or publication was performed. The new build gates must be exercised by the Windows/WSL release flow. All transitive NuGet and dynamically shipped proprietary/asset licenses still need an inventory against the exact final release payload before distribution; this update is not an unconditional legal-compliance certification.
