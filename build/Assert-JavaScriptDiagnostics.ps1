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
$jsRoot = Join-Path $root 'src/LocalGPT/wwwroot/js'
$manifestPath = Join-Path $PSScriptRoot 'javascript-diagnostics-files.sha256'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { Fail "Required diagnostics manifest is missing: $manifestPath" }

$maintained = @(Get-ChildItem -LiteralPath $jsRoot -File -Filter '*.js' | Where-Object { -not $_.Name.EndsWith('.example.js', [StringComparison]::OrdinalIgnoreCase) })
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
    if ($text -notmatch 'javascript-diagnostics:\s*guarded') { $errors.Add("Maintained JavaScript file lacks the diagnostics marker: $relative") }
    if ($text -notmatch '\btry\s*\{' -or $text -notmatch '\bcatch\s*(?:\([^)]*\))?\s*\{') { $errors.Add("Maintained JavaScript file lacks try/catch protection: $relative") }
    if ($text -match 'catch\s*(?:\([^)]*\))?\s*\{\s*\}') { $errors.Add("Maintained JavaScript file contains an empty catch block: $relative") }
    if ($relative.EndsWith('/javascript-diagnostics.js')) {
        foreach ($required in @('console.error', 'window.addEventListener("error"', 'unhandledrejection', 'ReportJavaScriptErrorAsync')) {
            if (-not $text.Contains($required)) { $errors.Add("JavaScript diagnostics runtime is missing '$required': $relative") }
        }
    }
}
foreach ($relative in $manifest.Keys | Where-Object { $_ -notin $relativeFiles }) { $errors.Add("Unexpected JavaScript diagnostics manifest entry: $relative") }
if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    Fail "$($errors.Count) problem(s) found. Run build/Update-JavaScriptDiagnosticsManifest.ps1 only after reviewing the frontend change."
}
Write-Host "JavaScript diagnostics validation passed for $($relativeFiles.Count) maintained LocalGPT browser files." -ForegroundColor Green
