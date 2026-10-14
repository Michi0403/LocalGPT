Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "Async-boundary component/service architecture validation failed: $Message" }

$root = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $root 'src/LocalGPT'
$pythonScript = Join-Path $PSScriptRoot 'audit_async_only_architecture.py'

function Invoke-PythonAudit {
    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($null -eq $python) { $python = Get-Command python3 -ErrorAction SilentlyContinue }
    if ($python) {
        $output = @(& $python.Source $pythonScript --source-root $sourceRoot --product LocalGPT 2>&1)
        $exitCode = [int]$LASTEXITCODE
        foreach ($line in $output) { Write-Host ([string]$line) }
        return $exitCode
    }

    $launcher = Get-Command py -ErrorAction SilentlyContinue
    if ($launcher) {
        $output = @(& $launcher.Source -3 $pythonScript --source-root $sourceRoot --product LocalGPT 2>&1)
        $exitCode = [int]$LASTEXITCODE
        foreach ($line in $output) { Write-Host ([string]$line) }
        return $exitCode
    }

    return $null
}

if (-not (Test-Path -LiteralPath $pythonScript -PathType Leaf)) {
    Fail "audit script is missing: $pythonScript"
}

$pythonExit = Invoke-PythonAudit
if ($null -eq $pythonExit) {
    Fail 'Python 3 is required for the zero-baseline async-boundary component/service architecture audit.'
}
if ($pythonExit -ne 0) {
    Fail "audit exited with code $pythonExit after reporting every detected violation."
}
