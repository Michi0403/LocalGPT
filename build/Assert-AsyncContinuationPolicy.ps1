Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "Async continuation validation failed: $Message" }

$root = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $root 'src/LocalGPT'
$pythonScript = Join-Path $PSScriptRoot 'audit_async_continuations.py'

function Invoke-PythonAudit {
    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($null -eq $python) { $python = Get-Command python3 -ErrorAction SilentlyContinue }
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

Fail 'Python 3 is required for the syntax-aware zero-tolerance async-continuation audit. No raw-await, await-using, or preconfigured-awaitable fallback is permitted.'
