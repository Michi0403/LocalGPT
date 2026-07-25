[CmdletBinding(SupportsShouldProcess)]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $PSScriptRoot 'protected-files.sha256'

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw 'Protected-file hash manifest is missing.'
}

$paths = Get-Content -LiteralPath $manifestPath |
    Where-Object { $_.Trim() -and -not $_.Trim().StartsWith('#') } |
    ForEach-Object {
        if ($_ -notmatch '^[0-9a-fA-F]{64}\s{2}(.+)$') { throw "Invalid manifest line: $_" }
        $Matches[1]
    }
$paths += 'build/protected-files.sha256'
$paths = $paths | Sort-Object -Unique

foreach ($relative in $paths) {
    $path = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Protected file is missing: $relative"
    }

    if ($PSCmdlet.ShouldProcess($relative, 'Mark read-only')) {
        if ($PSVersionTable.PSEdition -eq 'Desktop' -or $env:OS -eq 'Windows_NT') {
            (Get-Item -LiteralPath $path).IsReadOnly = $true
        }
        else {
            & chmod a-w -- $path
            if ($LASTEXITCODE -ne 0) { throw "chmod failed for $relative" }
        }
    }
}

Write-Host 'Governance files are marked read-only for the current checkout.'
Write-Host 'Only the human maintainer should deliberately restore write permission for a reviewed governance update.'
