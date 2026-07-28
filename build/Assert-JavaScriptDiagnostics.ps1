[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "JavaScript diagnostics validation failed: $Message" }

function Get-NormalizedTextSha256([string]$Path) {
    $text = [IO.File]::ReadAllText($Path)
    $normalized = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $encoding = New-Object Text.UTF8Encoding($false)
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha256.ComputeHash($encoding.GetBytes($normalized)))).Replace('-', '').ToLowerInvariant() }
    finally { $sha256.Dispose() }
}

$root = Split-Path -Parent $PSScriptRoot
$appRoot = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT'
$wwwroot = Join-Path $appRoot 'wwwroot'
$manifestPath = Join-Path $PSScriptRoot 'javascript-diagnostics-files.sha256'
$appPath = Join-Path $appRoot 'Components\App.razor'
$bridgePath = Join-Path $appRoot 'Components\InteractiveStartupMarker.razor'
$themeDispatcherPath = Join-Path $appRoot 'Components\Layout\ThemeJsChangeDispatcher.cs'
$themeModulePath = Join-Path $wwwroot 'switcher-resources\theme-controller.js'
$chatCssPath = Join-Path $wwwroot 'css\localgpt-theme-contract.css'
$chatPagePath = Join-Path $appRoot 'Components\Pages\Chat.razor'
$chatPageCssPath = Join-Path $appRoot 'Components\Pages\Chat.razor.css'
$contextMenuPath = Join-Path $wwwroot 'js\localgpt-context-menu.js'

foreach ($requiredPath in @($manifestPath, $appPath, $bridgePath, $themeDispatcherPath, $themeModulePath, $chatCssPath, $chatPagePath, $chatPageCssPath, $contextMenuPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) { Fail "Required diagnostics source is missing: $requiredPath" }
}

$maintained = @(
    Get-ChildItem -LiteralPath (Join-Path $wwwroot 'js') -File -Filter '*.js' |
        Where-Object { $_.DirectoryName -notmatch '[\\/]vendor$' -and $_.Name -ne 'devextreme-license.example.js' }
)
$maintained += Get-Item -LiteralPath $themeModulePath
$relativeFiles = @($maintained | ForEach-Object { $_.FullName.Substring($root.Length + 1).Replace([char]'\', [char]'/') } | Sort-Object -Unique)

$manifest = @{}
foreach ($line in Get-Content -LiteralPath $manifestPath) {
    $trimmed = $line.Trim()
    if (-not $trimmed -or $trimmed.StartsWith('#')) { continue }
    if ($trimmed -notmatch '^([0-9a-fA-F]{64})\s{2}(.+)$') { Fail "Invalid JavaScript diagnostics manifest line: $line" }
    $relative = $Matches[2].Replace([char]'\', [char]'/')
    if ($manifest.ContainsKey($relative)) { Fail "Duplicate JavaScript diagnostics manifest entry: $relative" }
    $manifest[$relative] = $Matches[1].ToLowerInvariant()
}

$errors = New-Object 'System.Collections.Generic.List[string]'
foreach ($relative in $relativeFiles) {
    if (-not $manifest.ContainsKey($relative)) { $errors.Add("Maintained JavaScript file is not reviewed in the diagnostics manifest: $relative"); continue }
    $path = Join-Path $root ($relative.Replace([char]'/', [IO.Path]::DirectorySeparatorChar))
    $text = [IO.File]::ReadAllText($path)
    if ((Get-NormalizedTextSha256 $path) -ne $manifest[$relative]) { $errors.Add("Reviewed JavaScript diagnostics file changed without refreshing its manifest: $relative") }
    if ($text -notmatch 'javascript-diagnostics:\s*guarded') { $errors.Add("Maintained JavaScript file lacks the function-level diagnostics marker: $relative") }
    if ($text -notmatch '\btry\s*\{' -or $text -notmatch '\bcatch\s*(?:\([^)]*\))?\s*\{') { $errors.Add("Maintained JavaScript file lacks try/catch protection: $relative") }
    if ($text -match 'catch\s*(?:\([^)]*\))?\s*\{\s*\}') { $errors.Add("Maintained JavaScript file contains an empty catch block: $relative") }
    if ($relative.EndsWith('/javascript-diagnostics.js')) {
        foreach ($required in @('console.error', 'window.addEventListener("error"', 'unhandledrejection', 'ReportJavaScriptErrorAsync', 'pendingReports', 'guardObject', 'guardClass')) {
            if (-not $text.Contains($required)) { $errors.Add("JavaScript diagnostics runtime is missing '$required': $relative") }
        }
    }
    elseif ($relative.EndsWith('/push-sw.js')) {
        foreach ($required in @('reportServiceWorkerError', 'console.error', 'unhandledrejection')) {
            if (-not $text.Contains($required)) { $errors.Add("Service-worker diagnostics are missing '$required': $relative") }
        }
    }
    elseif ($text -notmatch '(?:localGptJavaScriptDiagnostics|localGptDiagnostics|\bdiagnostics)\.report\s*\(') {
        $errors.Add("Maintained JavaScript file does not report failures through LocalGPT diagnostics: $relative")
    }
}
foreach ($relative in $manifest.Keys | Where-Object { $_ -notin $relativeFiles }) { $errors.Add("Unexpected JavaScript diagnostics manifest entry: $relative") }

$app = [IO.File]::ReadAllText($appPath)
$diagnosticsIndex = $app.IndexOf('<script src="js/javascript-diagnostics.js"></script>', [StringComparison]::Ordinal)
$devExpressIndex = $app.IndexOf('@DxResourceManager.RegisterScripts()', [StringComparison]::Ordinal)
$blazorIndex = $app.IndexOf('<script src="_framework/blazor.web.js"', [StringComparison]::Ordinal)
if ($diagnosticsIndex -lt 0 -or $devExpressIndex -lt 0 -or $blazorIndex -lt 0 -or $diagnosticsIndex -gt $devExpressIndex -or $diagnosticsIndex -gt $blazorIndex) {
    $errors.Add('App.razor must load JavaScript diagnostics before DevExpress and Blazor browser scripts.')
}

$bridge = [IO.File]::ReadAllText($bridgePath)
foreach ($required in @('@rendermode @(new InteractiveServerRenderMode(prerender: false))', 'localGptJavaScriptDiagnostics.bindDotNet', '[JSInvokable]', 'ReportJavaScriptErrorAsync', 'Logger.LogError')) {
    if (-not $bridge.Contains($required)) { $errors.Add("Interactive JavaScript logger bridge is missing '$required'.") }
}
$themeDispatcher = [IO.File]::ReadAllText($themeDispatcherPath)
if (-not $themeDispatcher.Contains('"applyThemeState"') -or $themeDispatcher.Contains('"ThemeController.applyThemeState"')) { $errors.Add('Theme dispatcher must invoke the direct exported module function applyThemeState.') }
foreach ($required in @('IFileVersionProvider', 'AddFileVersionToPath(', '"switcher-resources/theme-controller.js"', 'InvokeAsync<IJSObjectReference>("import", themeModulePath)')) {
    if (-not $themeDispatcher.Contains($required)) { $errors.Add("Theme dispatcher cache-safe import contract is missing '$required'.") }
}
foreach ($required in @('var themeControllerModulePath = AppendVersion("switcher-resources/theme-controller.js");', '<script type="module" src="@themeControllerModulePath"></script>')) {
    if (-not $app.Contains($required)) { $errors.Add("App.razor cache-safe theme module contract is missing '$required'.") }
}
$themeModule = [IO.File]::ReadAllText($themeModulePath)
if (-not $themeModule.Contains('export async function applyThemeState')) { $errors.Add('Theme module must directly export applyThemeState.') }

foreach ($relative in @('Components/Layout/HumanCollaborationInbox.razor', 'Components/Layout/CouncilSpoolerPanel.razor')) {
    $path = Join-Path $appRoot ($relative.Replace([char]'/', [IO.Path]::DirectorySeparatorChar))
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { $errors.Add("Required interactive collaboration component is missing: $relative"); continue }
    $text = [IO.File]::ReadAllText($path)
    if (-not $text.StartsWith('@rendermode @(new InteractiveServerRenderMode(prerender: false))')) { $errors.Add("Collaboration component must be an interactive island: $relative") }
    if (-not $text.Contains('@onclick')) { $errors.Add("Collaboration component no longer exposes Blazor click handlers: $relative") }
}

$chatCss = [IO.File]::ReadAllText($chatCssPath)
foreach ($required in @('[data-testid="dxaichat-host"] > *', '.localgpt-chat-root', 'flex: 1 1 100% !important', 'max-width: none !important')) {
    if (-not $chatCss.Contains($required)) { $errors.Add("Chat-width contract is missing '$required'.") }
}


$chatPage = [IO.File]::ReadAllText($chatPagePath)
if (($chatPage.Split('data-localgpt-copyable="true"').Count - 1) -lt 2) {
    $errors.Add('Chat output and former thoughts must both expose the stable copyable marker.')
}
$chatPageCss = [IO.File]::ReadAllText($chatPageCssPath)
foreach ($required in @('min-height: calc(100dvh - 5.25rem)', '#localgpt-chat-host', 'flex: 1 1 32rem', 'max-height: none', 'user-select: text !important')) {
    if (-not $chatPageCss.Contains($required)) { $errors.Add("Chat streaming usability contract is missing '$required'.") }
}
$contextMenu = [IO.File]::ReadAllText($contextMenuPath)
foreach ($required in @('copyableSelector', 'function shouldUseNativeContextMenu', 'if (shouldUseNativeContextMenu(target)) { close(); return; }')) {
    if (-not $contextMenu.Contains($required)) { $errors.Add("Native copy context-menu contract is missing '$required'.") }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    Fail "$($errors.Count) problem(s) found."
}
Write-Host "JavaScript diagnostics validation passed for $($relativeFiles.Count) maintained LocalGPT browser files; errors are console-logged and mirrored to ILogger."
