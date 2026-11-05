# LocalGPT 3.4.5 — DocFX Console Progress Repair

## Fixed

- Keeps the complete LocalGPT documentation and PDF release path unchanged.
- Normalizes redirected DocFX carriage-return output into stable console segments.
- Suppresses only DocFX's redirected in-place `Removed ... files` and `Copied ... files` transfer-counter redraws, which can repaint into impossible totals when passed through PowerShell.
- Preserves each raw DocFX output record in the existing diagnostic capture used by retry/error handling.
- No application, UI, service, documentation-content, PDF, deployment, or packaging behavior was changed.

## Version

- Application, installer console, and webview wrapper: `3.4.5`.
- LocalGPT wire protocol remains `2.1.1`.
- Minor and patch version slots remain single-digit.
