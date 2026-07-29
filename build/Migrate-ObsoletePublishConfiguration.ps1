[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$removed = [Collections.Generic.List[string]]::new()

function Remove-ObsoleteProfileRoot([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Container)) { return }

    foreach ($file in @(Get-ChildItem -LiteralPath $path -File -ErrorAction SilentlyContinue | Where-Object {
        $_.Extension -eq '.pubxml' -or $_.Name -like '*.pubxml.user'
    })) {
        Remove-Item -LiteralPath $file.FullName -Force
        $removed.Add($file.FullName.Substring($root.Length).TrimStart([char[]]@([char]'\', [char]'/')))
    }

    if (@(Get-ChildItem -LiteralPath $path -Force -ErrorAction SilentlyContinue).Count -eq 0) {
        Remove-Item -LiteralPath $path -Force
    }
}

foreach ($file in @(Get-ChildItem -LiteralPath (Join-Path $root 'LocalGPTWebviewWrapper') -Recurse -File -Filter '*.pubxml.user' -ErrorAction SilentlyContinue)) {
    Remove-Item -LiteralPath $file.FullName -Force
    $removed.Add($file.FullName.Substring($root.Length).TrimStart([char[]]@([char]'\', [char]'/')))
}

if ($removed.Count -gt 0) {
    Write-Host "Removed $($removed.Count) obsolete LocalGPT publish-profile artifact(s) left by an in-place source upgrade:"
    foreach ($relative in $removed | Sort-Object -Unique) { Write-Host "  - $relative" }
}
else {
    Write-Host 'No obsolete LocalGPT publish-profile artifacts required migration.'
}
