# LocalGPT 6.7.2 — Council session recovery and image routing

This additive source release responds to the 30 October 2026 incident evidence in `recent-runs.json`, two exported Council transcripts, three Visual Studio logs, and application logs. Two separate saved `learning-round` runs (`02dadbb0` and `7bd7715c`) failed after curation because the custom workflow demanded extracted source excerpts from a workspace containing a single PNG and no source files. A separate LAN Ollama endpoint timed out. A Blazor circuit disconnected; the logs do not establish the ultimate circuit failure trigger.

- Preserve the recorded Council run ID across an initially null render, retain Chat URL parameters on reload, and route rejected/unavailable reconnect attempts into full reload with exact-run rejoin. Explicit new-chat state clears the bridge.
- Session tools list active and retained historical runs. Lightweight Council spooler headers avoid cloning huge model thinking buffers on every render. Selecting and opening a saved run displays its persisted steps, thinking, errors and warnings inline in Chat; completed or failed runs remain inspectable within spooler retention.
- On interactive Chat initialization, if the live Council instance cannot be attached, recover persisted evidence for the exact requested run from the spooler. Never claim that a process-restarted Council continues running.
- An image-only upload cannot select a team whose workflow requires source-file verification or extracted-source excerpts. Only a properly image-capable team can be recommended; otherwise use the solo review path. No existing team or selection policy is rewritten.
- Image advisor offers explicit local OCR capability preflight and separately user-initiated bounded OCR over the quarantined image. If the provider is unavailable, show the actual unavailable reason. OCR evidence remains review-required; it is not image generation or automatic knowledge promotion.
- Maintain PowerShell source regression checks and JavaScript SHA-256 manifest. All existing build targets, sessions, projects, Council logic, sandbox providers, UI controls and migration files are preserved.

## Runtime acceptance still required

The sandbox lacks `dotnet` and `powershell`/`pwsh`; this release has **not** been built or executed. Validate the complete Windows build, Razor/DevExpress compilation, live Ollama image/OCR model availability, Blazor reconnect, UI transcript recovery, and operation against an existing database on a supported host. Cross-platform builds and VM launches are not verified here. Do not infer that a `10060` Ollama LAN socket timeout caused the Blazor circuit disconnect without additional server/browser transport evidence.
