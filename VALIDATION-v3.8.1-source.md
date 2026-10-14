# LocalGPT 3.8.1 source validation

Source-only validation verifies the 3.8.1 version policy, PowerShell 5.1/modern pwsh compatibility guards, one startup trust preflight, non-blocking notary credential recovery, durable notary state, StrictMode-safe notary JSON/state property access, absence of direct `$submit.status` / `$info.status` / `$state.submissionId` assumptions, common `notarytool info` polling for fresh and resumed submissions, PDF chunk-resume invariants, and InteractiveServer architecture.

The validation environment does not provide `pwsh`, `dotnet`, macOS signing tools, or Apple notarization access. Therefore no PowerShell parser execution, .NET build, codesign, stapling, or live Apple submission is claimed here.
