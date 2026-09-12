# LocalGPT 4.1.5 source validation

This release is a documentation-build resilience change. It does not claim a `dotnet`, Visual Studio, native signing, notarization, or PowerShell runtime build in the validation environment.

Static validation requires the maintained release, architecture, async-continuation, service-resilience, cross-platform, Matrix console, provider/Council, codegen/X-round, configurable-policy, XML-documentation, iterator-policy, system-variable, and `ConfigurationRoot` qualification guards to remain green. The 4.1.5 release audit additionally verifies bounded low-memory browser timeouts, isolated profile cache/crash state, profile-owned child cleanup, fresh-profile chunk retries, durable chunk reuse, and refusal of monolithic DocFX/Playwright fallback for chunked or low-memory builds unless an operator explicitly overrides it.

The source ZIP must be extracted again after packaging and the high-value static guards rerun against the delivered bytes. The user's macOS `pwsh Build-Release.ps1` run remains the authoritative end-to-end compiler and release-pipeline test.
