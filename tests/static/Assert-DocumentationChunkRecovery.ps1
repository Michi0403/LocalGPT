[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$scriptText = [IO.File]::ReadAllText((Join-Path $root 'build/Build-Documentation.ps1'))

# The actual print-book must preserve an array for a singleton final PDF batch.
if ($scriptText -notmatch '(?s)\$pages\s*=\s*@\(\s*if\s*\(') {
    throw 'The print-book must use an outer array expression for last-page slices.'
}
if ($scriptText -match '\$pages\s*=\s*if\s*\(') {
    throw 'The print-book contains the StrictMode singleton-unwrapping regression.'
}

function Select-TestPages {
    param([int]$Total, [int]$Start, [int]$PageSize)
    $all = @(0..($Total - 1))
    $pages = @($all | Select-Object -Skip $Start -First $PageSize)
    return $pages
}
foreach ($testCase in @(
    @{ Total = 753; Start = 752; Size = 8; Expected = 1 },
    @{ Total = 753; Start = 744; Size = 8; Expected = 8 },
    @{ Total = 16; Start = 0; Size = 8; Expected = 8 }
)) {
    $slice = @(Select-TestPages -Total $testCase.Total -Start $testCase.Start -PageSize $testCase.Size)
    if ($slice.Count -ne $testCase.Expected) {
        throw "Unexpected source slice count $($slice.Count); expected $($testCase.Expected)."
    }
}
if ($scriptText -notmatch 'Discarding incomplete cached documentation PDF chunk') {
    throw 'Missing explicit incomplete-chunk cleanup diagnostics.'
}
if ($scriptText -notmatch 'Only the active failed candidate is disposable') {
    throw 'Missing bounded active-chunk recovery.'
}
if ($scriptText -notmatch 'Documentation PDF rendering progress:') {
    throw 'Missing per-chunk progress logging.'
}
Write-Host 'Documentation chunk singleton and retry source contracts passed.' -ForegroundColor Green
