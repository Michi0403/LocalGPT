[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$unsupportedContainsPattern = '\.Contains\([^\r\n]*,\s*\[(?:System\.)?StringComparison\]::'
$failures = [System.Collections.Generic.List[string]]::new()

function Get-RepositoryRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return $Path.Substring($root.Length).TrimStart([char[]]'\/').Replace('\', '/')
}

function Read-RepositoryScriptText {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo]$File
    )

    $relative = Get-RepositoryRelativePath -Path $File.FullName
    for ($attempt = 1; $attempt -le 2; $attempt++) {
        try {
            return [System.IO.File]::ReadAllText($File.FullName)
        }
        catch [System.IO.FileNotFoundException] {
            if ($attempt -lt 2) {
                Start-Sleep -Milliseconds 50
                continue
            }

            throw "PowerShell compatibility validation could not read source script '$relative' because it disappeared during validation. Re-run the build after checking for a concurrent checkout, cleanup, or generator process."
        }
        catch [System.IO.DirectoryNotFoundException] {
            if ($attempt -lt 2) {
                Start-Sleep -Milliseconds 50
                continue
            }

            throw "PowerShell compatibility validation could not read source script '$relative' because its directory disappeared during validation. Re-run the build after checking for a concurrent checkout, cleanup, or generator process."
        }
    }
}

# Windows PowerShell 5.1 can apply Get-ChildItem -Include inconsistently when
# -LiteralPath and -Recurse are combined. Filter extensions explicitly so the
# validator never attempts to read generated assets such as DocFX SVG files.
$scriptFiles = Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
    $isPowerShellScript =
        [string]::Equals($_.Extension, '.ps1', [System.StringComparison]::OrdinalIgnoreCase) -or
        [string]::Equals($_.Extension, '.psm1', [System.StringComparison]::OrdinalIgnoreCase)

    if (-not $isPowerShellScript) {
        return $false
    }

    $relative = Get-RepositoryRelativePath -Path $_.FullName
    return $relative -notmatch '(^|/)(\.git|\.vs|artifacts|bin|obj|packages|node_modules)(/|$)' -and
        $relative -notmatch '^docs/_site(/|$)'
}

foreach ($file in $scriptFiles) {
    $content = Read-RepositoryScriptText -File $file
    foreach ($match in [regex]::Matches($content, $unsupportedContainsPattern)) {
        $line = [regex]::Matches($content.Substring(0, $match.Index), "`r`n|`r|`n").Count + 1
        $relative = Get-RepositoryRelativePath -Path $file.FullName
        $failures.Add("${relative}:$line uses String.Contains(value, StringComparison), which is unavailable in Windows PowerShell 5.1. Use String.IndexOf(value, comparison) instead.")
    }

    $sourceLines = $content -split "`r`n|`r|`n"
    for ($lineIndex = 0; $lineIndex -lt $sourceLines.Length; $lineIndex++) {
        $sourceLine = $sourceLines[$lineIndex]
        if ($sourceLine.IndexOf('Join-Path', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) { continue }
        foreach ($quoted in [regex]::Matches($sourceLine, '(["''])(?<value>[^"'']*\\[^"'']*)\1')) {
            $value = $quoted.Groups['value'].Value
            # A Join-Path call can share a line with a regex literal. Only path-like literals are rejected here.
            if ($value.StartsWith('[', [System.StringComparison]::Ordinal) -or $value.IndexOf('(?:', [System.StringComparison]::Ordinal) -ge 0) { continue }
            $relative = Get-RepositoryRelativePath -Path $file.FullName
            $failures.Add("${relative}:$($lineIndex + 1) passes a backslash-delimited path literal to Join-Path. Use '/' or nested Join-Path calls so pwsh on macOS/Linux resolves the same path.")
        }
    }
}

if ($failures.Count -gt 0) {
    throw "PowerShell compatibility validation failed:`n - $($failures -join "`n - ")"
}

Write-Host 'PowerShell compatibility validation passed for Windows PowerShell 5.1 and cross-platform pwsh path literals.'
