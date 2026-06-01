# LocalGPT WebView Wrapper

LocalGPT is a Windows desktop-hosted Blazor/ASP.NET Core application. The UI and AI services live in the `LocalGPT` ASP.NET Core server project, and the `LocalGPTWebviewWrapper` WinUI 3 app starts that server and displays it through WebView2.

The wrapper exists to make the app feel like a native Windows application while keeping the main UI in Blazor, where DevExpress components and browser debugging are easier to work with.

## Projects

- `LocalGPT`: Blazor/ASP.NET Core server, DevExpress UI, AI/Ollama configuration, native command runner services, Minecraft mod workspace services.
- `LocalGPTWebviewWrapper`: WinUI 3/WebView2 executable that launches and hosts the local server.
- `LocalGPTWebviewWrapper (Package)`: MSIX/DesktopBridge package project used for deploy/debug from Visual Studio and loose-layout registration.
- `build`: local build and repair scripts.

## Required tools

Use the current Visual Studio install with these workloads/components:

- .NET desktop development
- ASP.NET and web development
- Windows application development / WinUI tooling
- Windows SDK `10.0.22621.0` or newer
- Windows App SDK runtime
- WebView2 runtime
- DevExpress Blazor packages/feed access for the installed `25.1.x` packages

The projects currently target .NET 10 and the repository pins SDK 10.0.300 through `global.json`.

Check local runtimes:

```powershell
dotnet --list-runtimes
```

The desktop runtime prompt in Edge means the .NET 10 Desktop Runtime is missing. The wrapper project is framework-dependent on .NET 10 so the package uses the installed runtime instead of carrying a malformed local CoreCLR layout. Run the repair script with `-InstallMissingRuntime` to install the runtime through winget before launching from Visual Studio or the registered package.

## One-command local repair

From the repository root:

```powershell
.\LocalGPTWebviewWrapper\build\Repair-LocalGptDevEnvironment.ps1 -Register -Launch
```

To let the script install the .NET 10 Desktop Runtime with winget if it is missing:

```powershell
.\LocalGPTWebviewWrapper\build\Repair-LocalGptDevEnvironment.ps1 -InstallMissingRuntime -Register -Launch
```

The repair script:

- checks for .NET 10 desktop runtime
- creates/trusts a local package certificate in CurrentUser stores
- builds the full solution with Visual Studio MSBuild
- registers the loose AppX layout for debugging when `-Register` is passed
- launches the installed app when `-Launch` is passed

## Manual build

Use Visual Studio MSBuild for the full solution because the package project is a DesktopBridge `.wapproj`.

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" `
  ".\LocalGPTWebviewWrapper\LocalGPTWebviewWrapper.sln" `
  /p:Platform=x64 `
  /p:Configuration=Debug `
  /m `
  /v:minimal
```

The package is emitted under:

```text
%TEMP%\LocalGPTWebviewWrapper\AppPackages\
```

The loose debug layout is emitted under:

```text
LocalGPTWebviewWrapper\LocalGPTWebviewWrapper (Package)\bin\x64\Debug\AppX\
```

## Manual deploy/debug

Register the loose layout:

```powershell
Add-AppxPackage -Register ".\LocalGPTWebviewWrapper\LocalGPTWebviewWrapper (Package)\bin\x64\Debug\AppxManifest.xml" `
  -ForceApplicationShutdown `
  -ForceUpdateFromAnyVersion
```

Launch from Start Menu, Visual Studio, or PowerShell:

```powershell
$app = Get-StartApps | Where-Object { $_.AppID -like 'a6e38587-f17a-4a2e-8022-248694f372b3_*' } | Select-Object -First 1
Start-Process "shell:AppsFolder\$($app.AppID)"
```

## Static web assets and DevExpress

Blazor and DevExpress static assets are served through `LocalGPT.staticwebassets.runtime.json`. In this repository the WinUI package must copy that manifest beside `LocalGPTWebviewWrapper.exe`, otherwise these paths fail:

- `/LocalGPT.styles.css`
- `/_content/DevExpress.Blazor.Resources/js/import-scripts.js`
- `/_content/DevExpress.Blazor.Themes/office-white.bs5.min.css`

The package project now copies the manifest from the RID-specific build output, for example:

```text
LocalGPT\bin\x64\Debug\net10.0\win-x64\LocalGPT.staticwebassets.runtime.json
```

DevExpress 25 uses this module path:

```text
/_content/DevExpress.Blazor/modules/dx-blazor-all.js
```

The older path below is expected to return 404 with the installed package version:

```text
/_content/DevExpress.Blazor/dx-blazor-all.js
```

## Ollama and gpt-oss diagnostics

LocalGPT uses Ollama as the preferred local debug host. The default local model is:

```text
gpt-oss:20b
```

Before testing `DxAIChat`, verify that Ollama itself can produce text:

```powershell
.\LocalGPTWebviewWrapper\build\Test-OllamaGptOss.ps1
```

Useful options:

```powershell
.\LocalGPTWebviewWrapper\build\Test-OllamaGptOss.ps1 -PullIfMissing
.\LocalGPTWebviewWrapper\build\Test-OllamaGptOss.ps1 -StartServerIfDown
```

The script checks:

- `ollama --version`
- `/api/version`
- `/api/tags`
- `/api/ps`
- `/api/chat`
- `/api/generate`

If `/api/chat` returns an empty assistant message or `/api/generate` ends prematurely, the problem is in the Ollama/model host layer, not in `DxAIChat`. Restart or update Ollama, then rerun the script before debugging the Blazor frontend.

`gpt-oss:20b` can spend early generated tokens in Ollama's `thinking` field before visible `content` appears. If the helper shows `done_reason: length` and empty content, rerun with a larger prediction budget:

```powershell
.\LocalGPTWebviewWrapper\build\Test-OllamaGptOss.ps1 -NumPredict 1024 -TimeoutSeconds 300
```

`DxAIChat` uses LocalGPT's configured `IChatClient`. For Ollama/gpt-oss, LocalGPT uses an Ollama-native client so the response can include a visible `Model thinking` markdown details block followed by the final answer.

Known-good local expectation:

```text
Ollama version: 0.13.x or newer
Model: gpt-oss:20b
Family: gptoss
Parameter size: 20.9B
Quantization: MXFP4
```

## Common fixes

If Visual Studio says deployment fails:

1. Build the solution once with the repair script.
2. Register the loose AppX layout with `-Register`.
3. If certificate errors appear, run `build\New-LocalPackageCertificate.ps1`.
4. If the Edge runtime/download prompt appears, run the repair script with `-InstallMissingRuntime`; then rebuild/register the package so the .NET 10 framework-dependent runtime configuration is current.
5. If DevExpress components render blank or throw JavaScript module errors, verify `LocalGPT.staticwebassets.runtime.json` exists beside the packaged executable.
6. If deployment reports `0x80070002`, `0x80073CF9`, or a `DEP1000` resolved-state/copy failure, rebuild the package and run `build\Repair-LocalGptDevEnvironment.ps1 -SkipBuild -Register`. The package project must copy `Images\*.png` into the loose `bin\<platform>\<configuration>\AppX\Images` layout. The repair script removes only the stale LocalGPT dev package registration and retries once when Windows keeps the old layout pinned.

If DevExpress packages do not restore:

1. Confirm the DevExpress NuGet feed is configured in Visual Studio or `NuGet.config`.
2. Confirm the package versions resolve to the installed `25.1.x` feed.
3. Run `dotnet restore .\LocalGPTWebviewWrapper\LocalGPTWebviewWrapper.sln`.

## AI and Ollama direction

The app is intended to support several selectable Ollama-hosted AI models and reuse context smartly. Keep configuration save/load behavior durable because the setup page is the control surface for those AI profiles.

The longer-term Minecraft mod building feature should keep risky OS command execution behind backend services such as `INativeCommandRunner`, and should keep browser/client-only helpers in the frontend layer. Treat command execution as a deliberate capability, not as random UI code.

## Minecraft Java mod toolchain

LocalGPT now treats Minecraft Java Edition as the first-class mod-builder target. Fabric is the lightweight mod path, NeoForge is the modern Forge-style path, Paper is the server-side plugin path, and vanilla datapacks cover command/data systems that should not require Java. Bedrock should be added later as a separate behavior/resource pack exporter.

Install or verify the local modding tools with:

```powershell
.\LocalGPTWebviewWrapper\build\Setup-MinecraftModToolchain.ps1 -Install -InstallGradle -InstallEclipse
```

Generated mod workspaces include:

```powershell
.\build-local.ps1
```

That helper finds JDK 21, uses LocalGPT's local Gradle folder under `%LOCALAPPDATA%\LocalGPT\Tools`, and builds the generated Fabric, NeoForge, or Paper project. Datapack workspaces use their generated helper to validate JSON and create a zip without Java. Eclipse can import generated Java workspaces with `File > Import > Gradle > Existing Gradle Project`.

For release packaging, use:

```powershell
.\LocalGPTWebviewWrapper\build\Publish-LocalGptRelease.ps1 -Configuration Release -Platforms x64,x86,arm64
```

Release zips and SHA256 manifests are written under `artifacts\releases\` and are ignored by git.

The AI Council should help with both mod code and user setup. If JDK, Gradle, Minecraft, Ollama, or a selected model is missing, the council should ask a short technical recovery poll and save the missing feature/setup note to memory instead of pretending the workflow completed.

If the AI reports missing LocalGPT features, blocked workflows, or not-yet-implemented capabilities, LocalGPT writes a text report under:

```text
%LOCALAPPDATA%\LocalGPT\AIReports\
```

## Diagnostics

Prefer LocalGPT diagnostic routes when testing the real configured services:

```powershell
$server = Get-Content "$env:LOCALAPPDATA\LocalGPT\runtime\server.json" -Raw | ConvertFrom-Json
Invoke-RestMethod -Method Post -Uri "$($server.BaseUrl)/__diag/dxaichat-smoke" -ContentType application/json -Body '{"maxOutputTokens":1024}'
Invoke-RestMethod -Method Post -Uri "$($server.BaseUrl)/__diag/council" -ContentType application/json -Body '{...}'
Invoke-RestMethod -Uri "$($server.BaseUrl)/__diag/minecraft/workspace-smoke?loader=datapack"
```

The WinUI wrapper can also test the embedded WebView2 shell. Run this from a registered/package identity or Visual Studio debug launch; direct unpackaged exe launch can fail with WinUI activation error `REGDB_E_CLASSNOTREG`.

For registered/package launches, create the one-shot smoke flag first:

```powershell
$runtime = "$env:LOCALAPPDATA\LocalGPT\runtime"
New-Item -ItemType Directory -Force -Path $runtime | Out-Null
Set-Content -Path "$runtime\webview2-smoke.flag" -Value "exit" -Encoding utf8
```

```powershell
$env:LOCALGPT_WEBVIEW2_SMOKE = "1"
$env:LOCALGPT_WEBVIEW2_SMOKE_EXIT = "1"
.\LocalGPTWebviewWrapper\LocalGPTWebviewWrapper\bin\x64\Debug\net10.0-windows10.0.22621.0\win-x64\LocalGPTWebviewWrapper.exe
```

Snapshots are written under:

```text
%LOCALAPPDATA%\LocalGPT\WebView2Diagnostics\
```
