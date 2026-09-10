# LocalGPT 4.0.8 source validation

Source-only validation was performed in the delivery environment. Per the request, no GitHub access and no `dotnet build` / `dotnet publish` were used. PowerShell is not installed in the delivery environment, so PowerShell execution is not claimed.

Validated invariants:

- LocalGPT application, installer, and WebView wrapper versions are 4.0.8; minor and patch slots remain single-digit.
- Runtime browser cache-buster and HTTP User-Agent identities are updated to 4.0.8, including the provider-installation reference documentation.
- `Build-Documentation.ps1` owns a narrow generated-output cleanup helper that selects only top-level `LocalGPT-*.pdf` files whose filename does not equal the current `LocalGPT-$Version.pdf` and removes them from the generated site tree.
- That cleanup runs after HTML generation/cache restoration but before the generated site is saved into the durable HTML cache and before it is copied to runtime/build publication roots.
- Source `docs/` PDFs are not removed by the new cleanup path.
- `Update-GitHubPagesSnapshot.ps1` retains its exact-current-PDF validation and its explicit `pdfAvailable=false` requirement for HTML-only `-AllowMissingPdf` builds; the guard was not weakened.
- The existing 4.0.7 installer compile-surface repair (`System` + `System.IO`) and early installer compilation preflight remain intact.
- Maintained Python architecture, async-continuation, service-resilience, cross-platform, code-generation, X-Round, and release-specific audits pass on the packaged source tree.

A real compiler result is intentionally not claimed because no .NET SDK build was run in this environment.
