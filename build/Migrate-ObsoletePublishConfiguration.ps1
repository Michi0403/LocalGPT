[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$removed = [Collections.Generic.List[string]]::new()

# Developer publish profiles are supported release-lane entry points and are never removed.
# Only per-machine Visual Studio overlays are cleaned from distributable source trees.
foreach ($file in @(Get-ChildItem -LiteralPath (Join-Path $root 'LocalGPTWebviewWrapper') -Recurse -File -Filter '*.pubxml.user' -ErrorAction SilentlyContinue)) {
    Remove-Item -LiteralPath $file.FullName -Force
    $removed.Add($file.FullName.Substring($root.Length).TrimStart([char[]]@([char]'\', [char]'/')))
}

if ($removed.Count -gt 0) {
    Write-Host "Removed $($removed.Count) machine-specific LocalGPT publish-profile overlay(s):"
    foreach ($relative in $removed | Sort-Object -Unique) { Write-Host "  - $relative" }
}
else {
    Write-Host 'No machine-specific LocalGPT publish-profile overlays required migration.'
}
