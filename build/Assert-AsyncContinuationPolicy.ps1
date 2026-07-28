Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "Async continuation validation failed: $Message" }

$root = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $PSScriptRoot 'async-continuation-baseline.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { Fail 'The async continuation baseline is missing.' }

$utf8 = [System.Text.Encoding]::UTF8
$manifest = [System.IO.File]::ReadAllText($manifestPath, $utf8) | ConvertFrom-Json
if ([int]$manifest.schemaVersion -ne 2) { Fail "Unsupported baseline schema version: $($manifest.schemaVersion)" }
$appRoot = Join-Path $root ([string]$manifest.sourceRoot).Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar)
if (-not (Test-Path -LiteralPath $appRoot -PathType Container)) { Fail "Source root is missing: $appRoot" }

$baseline = @{}
foreach ($property in $manifest.files.PSObject.Properties) {
    $baseline[[string]$property.Name] = $property.Value
}

$failures = New-Object System.Collections.Generic.List[string]
$files = Get-ChildItem -LiteralPath $appRoot -Recurse -File | Where-Object {
    ($_.Extension -eq '.cs' -or $_.Extension -eq '.razor') -and
    $_.FullName -notmatch '[\/](bin|obj)[\/]'
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
    $relative = $file.FullName.Substring($appRoot.Length).TrimStart([char[]]@('\', '/')).Replace([char]'\', [char]'/')
    $isRendererSource = $relative.StartsWith('Components/', [System.StringComparison]::OrdinalIgnoreCase)

    if ($unconfigured -lt 0) {
        $failures.Add("$relative: continuation count exceeds await count; inspect strings/comments and update the guard deliberately.")
        continue
    }

    $allowedUnconfigured = 0
    $allowedTrue = 0
    $minimumFalse = 0
    if ($baseline.ContainsKey($relative)) {
        $allowedUnconfigured = [int]$baseline[$relative].maxUnconfiguredAwaitCount
        $allowedTrue = [int]$baseline[$relative].maxConfigureAwaitTrueCount
        $minimumFalse = [int]$baseline[$relative].minConfigureAwaitFalseCount
    }

    if ($isRendererSource -and $falseCount -gt 0) {
        $failures.Add("$relative contains $falseCount ConfigureAwait(false) call(s). Razor/component continuations must stay on the Blazor renderer context; use ConfigureAwait(true).")
    }
    if (-not $isRendererSource -and $falseCount -lt $minimumFalse) {
        $failures.Add("$relative has only $falseCount ConfigureAwait(false) call(s); the reviewed minimum is $minimumFalse. A service/controller continuation was removed without baseline review.")
    }
    if ($unconfigured -gt $allowedUnconfigured) {
        $expected = if ($isRendererSource) { 'ConfigureAwait(true)' } else { 'ConfigureAwait(false)' }
        $failures.Add("$relative has $unconfigured unconfigured await(s); reviewed maximum is $allowedUnconfigured. Use $expected or deliberately review the baseline.")
    }
    if ($trueCount -gt $allowedTrue) {
        $failures.Add("$relative has $trueCount ConfigureAwait(true) call(s); reviewed maximum is $allowedTrue. New renderer-affine continuations require explicit baseline review.")
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

Write-Host "Async continuation validation passed for $checked source files ($totalAwait await tokens, $totalFalse service/controller ConfigureAwait(false), $totalTrue renderer/reviewed ConfigureAwait(true))."
