# LocalGPT 3.5.8 source validation

This source release is statically validated in an environment without the .NET SDK, PowerShell runtime, or macOS browser stack; no local compile or native macOS PDF-build claim is made.

The 3.5.8 release audit verifies version consistency, preservation of the 3.5.7 WSL2/Linux release contract, the two-profile browser PDF renderer, validation-before-acceptance of browser PDF candidates, explicit `html-browser-print-compatibility` accessibility metadata, bounded macOS DocFX fallback timeout, operator `DOCFX_PDF_TIMEOUT` override support, LocalGPT.ReleasePackaging 1.0.1, prior compiler-repair markers, and explicit InteractiveServer boundaries.

Maintained architecture, async, service-resilience, cross-platform, configurable-policy, provider-repetition, C#/Razor XML-documentation, Bash syntax, and structured source checks are run before final packaging. The final ZIP is additionally extracted and compared byte-for-byte with the prepared source tree, checked for unsafe/duplicate ZIP entries and CRC errors, and the critical audits are rerun from that exact extraction.
