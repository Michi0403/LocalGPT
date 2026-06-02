# Frontend Test Automation

LocalGPT should test local behavior in three layers:

1. Deterministic HTTP diagnostics through the Test Lab page.
2. Browser-level checks against the Blazor server route.
3. Real WinUI/WebView2 shell checks with Microsoft Edge WebDriver or package smoke diagnostics.

## Test Lab

The `/test-lab` page lets a frontend user run safe local routes without loading a heavy
Ollama model. It supports:

- `/health`
- `/__diag`
- `/__diag/dxaichat-functions`
- `/__diag/minecraft/datapack-version?minecraftVersion=26.1`
- `/__diag/council/artifact-smoke?target=datapack`
- `/__diag/council/artifact-smoke?target=ai-host`
- `/__diag/minecraft/datapack-benchmark?minecraftVersion=26.1`
- `/__diag/learn-base/import`

The page shows raw JSON and extracts `/__artifacts/...` links so generated source,
DLL, solution, and datapack zips are downloaded through HTTP instead of displayed as
text.

## WebView2 And Selenium

Microsoft Edge WebDriver supports automating Microsoft Edge and WebView2 apps through
the W3C WebDriver protocol. Microsoft documents two useful modes for LocalGPT:

- Launch mode: create `EdgeOptions`, set `UseWebView = true`, set `BinaryLocation`
  to the WebView2 wrapper executable, and create an `EdgeDriver`.
- Attach mode: start the app with a WebView2 remote debugging port, then set
  `EdgeOptions.UseWebView = true` and `EdgeOptions.DebuggerAddress` to the matching
  host and port.

Use launch mode for simple one-WebView startup tests. Use attach mode when the app is
already running, when native shell steps are needed first, or when testing a packaged
Visual Studio/MSIX launch.

## Python Workbench Direction

Python browser automation examples, including the local AutomatedDiscordLogin source
folder, are useful as architecture fingerprints:

- isolate automation behind backend services
- require explicit user permission before executing external scripts
- configure Python paths and script roots through typed options
- use safe working directories
- log every run and result
- expose progress and artifacts through the frontend

Do not silently self-expand LocalGPT by running generated Python or browser scripts.
For future Python.NET integration, follow the same permission-gated interop pattern
already documented from the user's legacy multi-project sources.

## Council Guidance

When a model claims frontend behavior was tested, require one of these evidence types:

- Test Lab route result
- browser or WebView2 screenshot/snapshot
- WebView2 diagnostics JSON
- generated artifact download URL with HTTP status
- build/test output

If the evidence is missing, mark the claim as `Needs verification`.
