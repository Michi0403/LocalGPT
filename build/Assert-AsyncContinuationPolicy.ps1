Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "Async continuation validation failed: $Message" }

$root = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT'
$pythonScript = Join-Path $PSScriptRoot 'audit_async_continuations.py'

function Invoke-PythonAudit {
    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($python) {
        $output = @(& $python.Source $pythonScript --source-root $sourceRoot 2>&1)
        $exitCode = [int]$LASTEXITCODE
        foreach ($line in $output) { Write-Host ([string]$line) }
        return $exitCode
    }

    $launcher = Get-Command py -ErrorAction SilentlyContinue
    if ($launcher) {
        $output = @(& $launcher.Source -3 $pythonScript --source-root $sourceRoot 2>&1)
        $exitCode = [int]$LASTEXITCODE
        foreach ($line in $output) { Write-Host ([string]$line) }
        return $exitCode
    }

    return $null
}

$pythonExit = Invoke-PythonAudit
if ($null -ne $pythonExit) {
    if ($pythonExit -ne 0) { Fail "Python async-continuation audit exited with code $pythonExit." }
    exit 0
}

Write-Warning 'Python was not found; running the reduced PowerShell continuation guard.'
$failures = [Collections.Generic.List[string]]::new()
$files = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File | Where-Object {
    ($_.Extension -eq '.cs' -or $_.Extension -eq '.razor') -and
    $_.FullName -notmatch '[\/](bin|obj|Migrations)[\/]'
}
foreach ($file in $files) {
    $text = [IO.File]::ReadAllText($file.FullName, [Text.Encoding]::UTF8)
    $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart([char[]]@('\', '/')).Replace('\', '/')

    $trueMatches = [regex]::Matches($text, '\.ConfigureAwait\s*\(\s*true\s*\)')
    foreach ($trueMatch in $trueMatches) {
        $isComponent = $relative.StartsWith('Components/', [StringComparison]::OrdinalIgnoreCase)
        $lineStart = $text.LastIndexOf("`n", [Math]::Max(0, $trueMatch.Index - 1)) + 1
        $lineEnd = $text.IndexOf("`n", $trueMatch.Index)
        if ($lineEnd -lt 0) { $lineEnd = $text.Length }
        $lineText = $text.Substring($lineStart, $lineEnd - $lineStart)
        $hasReviewedMarker = $lineText -match 'renderer-affine (?:lifecycle|loading) continuation'
        if (-not $isComponent) {
            $failures.Add("${relative}: ConfigureAwait(true) appears outside Components; context-free application code must use ConfigureAwait(false).")
        }
        elseif (-not $hasReviewedMarker) {
            $line = 1 + ([regex]::Matches($text.Substring(0, $trueMatch.Index), "`n")).Count
            $failures.Add("${relative}:$line contains ConfigureAwait(true) without an exact renderer-affine lifecycle/loading review marker.")
        }
    }

    $candidate = [regex]::Matches(
        $text,
        '(?ms)\bawait\s+(?!using\b|foreach\b)(?<expression>.*?)(?=;|,\s*(?:await|return|throw|[A-Za-z_][A-Za-z0-9_]*\s*=)|\r?\n\s*[}\)])')
    foreach ($match in $candidate) {
        $expression = [string]$match.Groups['expression'].Value
        if ($expression -match 'configuredTaskAwaitable' -or
            $expression -match '\.ConfigureAwait\s*\(\s*(?:true|false)\s*\)') {
            continue
        }
        $line = 1 + ([regex]::Matches($text.Substring(0, $match.Index), "`n")).Count
        $failures.Add("${relative}:$line contains an await expression without explicit ConfigureAwait(true/false).")
    }
}
if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "  - $failure" }
    Fail "$($failures.Count) reduced-fallback problem(s). Install Python 3 to run the complete syntax-aware continuation audit."
}
Write-Host 'Reduced PowerShell async continuation validation passed; Python 3 enables exact Razor code-block, per-await, lifecycle and renderer-affinity validation.'
