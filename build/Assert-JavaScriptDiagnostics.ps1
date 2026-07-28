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
$appRoot = Join-Path $root 'src\PublisherStudio.Web'
$jsRoot = Join-Path $appRoot 'wwwroot\js'
$manifestPath = Join-Path $PSScriptRoot 'javascript-diagnostics-files.sha256'
$appPath = Join-Path $appRoot 'Components\App.razor'
$bridgePath = Join-Path $appRoot 'Components\Layout\JavaScriptDiagnosticsBridge.razor'
foreach ($requiredPath in @($manifestPath, $appPath, $bridgePath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) { Fail "Required diagnostics source is missing: $requiredPath" }
}

$maintained = @(Get-ChildItem -LiteralPath $jsRoot -File -Filter '*.js')
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
    elseif ($text -notmatch '(?:publisherStudioJavaScriptDiagnostics|publisherStudioDiagnostics|\bpublisherDiagnostics)\.report\s*\(') {
        $errors.Add("Maintained JavaScript file does not report failures through PublisherStudio diagnostics: $relative")
    }
}
foreach ($relative in $manifest.Keys | Where-Object { $_ -notin $relativeFiles }) { $errors.Add("Unexpected JavaScript diagnostics manifest entry: $relative") }

$app = [IO.File]::ReadAllText($appPath)
$diagnosticsIndex = $app.IndexOf('<script src="js/javascript-diagnostics.js"></script>', [StringComparison]::Ordinal)
$jqueryIndex = $app.IndexOf('<script src="vendor/jquery/jquery.min.js"></script>', [StringComparison]::Ordinal)
$blazorIndex = $app.IndexOf('<script src="_framework/blazor.web.js"></script>', [StringComparison]::Ordinal)
if ($diagnosticsIndex -lt 0 -or $jqueryIndex -lt 0 -or $blazorIndex -lt 0 -or $diagnosticsIndex -gt $jqueryIndex -or $diagnosticsIndex -gt $blazorIndex) {
    $errors.Add('App.razor must load JavaScript diagnostics before vendor and Blazor browser scripts.')
}
if (-not $app.Contains('<JavaScriptDiagnosticsBridge />')) { $errors.Add('App.razor no longer renders the interactive JavaScript diagnostics bridge.') }

$bridge = [IO.File]::ReadAllText($bridgePath)
foreach ($required in @('@rendermode @(new InteractiveServerRenderMode(prerender: false))', 'publisherStudioJavaScriptDiagnostics.bindDotNet', '[JSInvokable]', 'ReportJavaScriptErrorAsync', 'Logger.LogError')) {
    if (-not $bridge.Contains($required)) { $errors.Add("Interactive JavaScript logger bridge is missing '$required'.") }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    Fail "$($errors.Count) problem(s) found."
}
Write-Host "JavaScript diagnostics validation passed for $($relativeFiles.Count) maintained PublisherStudio browser files; errors are console-logged and mirrored to ILogger."
