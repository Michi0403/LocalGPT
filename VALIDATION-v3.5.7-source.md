# LocalGPT 3.5.7 source validation

This source release is statically validated in an environment without the .NET SDK or PowerShell runtime; no local compile claim is made.

Maintained source audits passed for the working tree: architecture policy; 22 cross-platform boundary checks; configurable Council behavior policy; provider stream repetition policy; async continuation policy across 259 source files (2,979 await tokens); service resilience across 2,188 service methods; C# XML documentation across 10,251 declarations in 651 files; Razor XML documentation across 45 component types and 776 direct `@code` members; and the dedicated 3.5.7 release audit.

The 3.5.7 audit checks version consistency, optional WSL2 routing/fallback, delegated documentation reuse, WSL setup/helper presence, correct Windows-to-WSL DevExpress license bridging, target-architecture AppImage finishing, the stable LocalGPT.ReleasePackaging 1.0.1 contract, prior compiler-repair markers, and explicit InteractiveServer boundaries. Bash syntax and source delimiter/here-string lexical validation also pass for the modified release helpers.

The final source ZIP is additionally checked for duplicate/unsafe entries, CRC integrity, exact extraction byte equality, structured XML/JSON parsing, and the critical maintained audits rerun from the exact extracted archive. A Windows/WSL2, native Linux, or macOS machine remains the authoritative runtime build test.
