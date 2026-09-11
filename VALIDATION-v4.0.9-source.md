# LocalGPT 4.0.9 source validation

Validation is source/static because the assistant environment does not provide the user's macOS signing identities, Apple notarization account, Microsoft Edge macOS process behavior, PowerShell, or the .NET SDK. No `dotnet build`, `codesign`, notarization submission, GitHub access, or native package execution is claimed.

Validated statically:

- all LocalGPT application projects report 4.0.9 and obey the one-digit minor/patch policy;
- `build/assets/mac-apphost-entitlements.plist` parses as a property list and contains only `com.apple.security.cs.allow-jit = true`;
- macOS trust preflight requires `plutil` and lints that exact asset before expensive release work;
- native Developer ID signing normalizes the checked-in asset to XML1, lints the normalized temporary copy, and applies it only to the .NET apphost;
- the previous inline PowerShell entitlement here-string is absent from native packaging;
- browser PDF rendering watches a live output file for stable length and a complete PDF trailer before terminating a lingering renderer; direct `WaitForExit($browserPdfTimeoutMilliseconds)` is absent, so the 480-second value is a failure ceiling rather than the normal successful-part delay;
- durable PDF chunk reuse, final PDF merge, strict documentation completeness checks, generated-PDF version hygiene, and Pages validation remain present;
- the 4.0.7 installer compile-surface imports and early installer compile preflight remain present;
- maintained architecture, async-continuation, service-resilience, cross-platform, code-generation, Council X-Round/provider qualification, and configurable-behavior Python audits pass.

The user's macOS `pwsh Build-Release.ps1` run remains the authoritative execution test for `plutil`, browser-process completion, Developer ID signing, notarization, and package production.
