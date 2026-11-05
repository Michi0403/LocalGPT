# LocalGPT 6.5.8 — macOS Setup Console, Model Inventory, MCP Certificate UX

## Corrected

- **Setup Guide installed models:** `Install.RefreshOnboardingAsync` now requests live connectivity/model inventory (`refreshConnectivity: true`), rather than returning an empty `InstalledModels` collection by design. A completed local console installation/pull triggers another asynchronous inventory read. User-initiated Refresh also re-probes providers; no fixed model names or version assumptions were added.
- **Operational console:** `ConsoleCommandService.GetRecentDisplayText` treats repeat TTY progress frames as updates to a logical progress row, retaining only its latest version in the display snapshot. It does **not** remove events or truncate command receipts, and it preserves the order and times of surviving entries. This is designed to address ANSI cursor-repaint output such as Ollama pulling manifests; it is not a complete VT terminal emulator.
- **MCP TLS certificate:** When the MCP certificate path is unconfigured, Install suggests an existing configured remote-endpoint PFX (with its known password) or the existing certificate-editor default PFX path. It does not change active listeners or persist settings until Save. Added the existing `LocalPathExplorer` as a reusable file browser for both MCP and optional remote web endpoint PFX inputs, with existing-file and `.pfx` validation. Newly generated server-auth certificates also populate an empty MCP certificate draft.
- **Lifecycle:** Installation page unsubscribes from command notifications on disposal.

## Unchanged

- PowerShell-only build/architecture test policy, single-digit version components and dynamically discovered versions.
- 1-Wire trusted peers, Council, explicit user approval, and existing certificate-generation/security services.
- MFA enrollment popup; its QR issue is being left alone at the user's request.

## Validation limits

Source structure and csproj XML can be checked in the current environment. A real .NET 10/Razor/DevExpress compile, Windows PowerShell 5.1/modern pwsh guard run, and a macOS Ollama download + browser test are **not** available here. The remedies are source-level fixes, not an end-to-end runtime certification.
