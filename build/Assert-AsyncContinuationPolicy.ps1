Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "Async continuation validation failed: $Message" }

$root = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $PSScriptRoot 'async-continuation-baseline.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { Fail 'The async continuation baseline is missing.' }

$utf8 = [System.Text.Encoding]::UTF8
$manifest = [System.IO.File]::ReadAllText($manifestPath, $utf8) | ConvertFrom-Json
if ([int]$manifest.schemaVersion -ne 1) { Fail "Unsupported baseline schema version: $($manifest.schemaVersion)" }
$appRoot = Join-Path $root ([string]$manifest.sourceRoot).Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar)
if (-not (Test-Path -LiteralPath $appRoot -PathType Container)) { Fail "Source root is missing: $appRoot" }

$baseline = @{}
foreach ($property in $manifest.files.PSObject.Properties) {
    $baseline[[string]$property.Name] = $property.Value
}

$failures = New-Object System.Collections.Generic.List[string]
$files = Get-ChildItem -LiteralPath $appRoot -Recurse -File | Where-Object {
    ($_.Extension -eq '.cs' -or $_.Extension -eq '.razor') -and
    $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
}
$checked = 0
$totalAwait = 0
$totalFalse = 0
$totalTrue = 0
foreach ($file in $files) {
    $text = [System.IO.File]::ReadAllText($file.FullName, $utf8)
    $awaitCount = [regex]::Matches($text, '\bawait\b').Count
    if ($awaitCount -eq 0) { continue }
    $checked++
    $falseCount = [regex]::Matches($text, '\.ConfigureAwait\s*\(\s*false\s*\)').Count
    $trueCount = [regex]::Matches($text, '\.ConfigureAwait\s*\(\s*true\s*\)').Count
    $unconfigured = $awaitCount - $falseCount - $trueCount
    if ($unconfigured -lt 0) {
        $failures.Add("$($file.FullName): continuation count exceeds await count; inspect strings/comments and update the guard deliberately.")
        continue
    }
    $relative = $file.FullName.Substring($appRoot.Length).TrimStart([char[]]@('\', '/')).Replace([char]'\', [char]'/')
    $allowedUnconfigured = 0
    $allowedTrue = 0
    if ($baseline.ContainsKey($relative)) {
        $allowedUnconfigured = [int]$baseline[$relative].maxUnconfiguredAwaitCount
        $allowedTrue = [int]$baseline[$relative].maxConfigureAwaitTrueCount
    }
    if ($unconfigured -gt $allowedUnconfigured) {
        $failures.Add("$relative has $unconfigured unconfigured await(s); reviewed maximum is $allowedUnconfigured. Use ConfigureAwait(false), or explicitly review the baseline for renderer-affine code.")
    }
    if ($trueCount -gt $allowedTrue) {
        $failures.Add("$relative has $trueCount ConfigureAwait(true) call(s); reviewed maximum is $allowedTrue. New true continuations require explicit review.")
    }
    $totalAwait += $awaitCount
    $totalFalse += $falseCount
    $totalTrue += $trueCount
}

if ($failures.Count -gt 0) {
    Write-Host 'Async continuation validation failed:'
    foreach ($failure in $failures) { Write-Host "  - $failure" }
    throw "Async continuation validation failed with $($failures.Count) problem(s)."
}

Write-Host "Async continuation validation passed for $checked source files ($totalAwait await tokens, $totalFalse ConfigureAwait(false), $totalTrue ConfigureAwait(true))."
