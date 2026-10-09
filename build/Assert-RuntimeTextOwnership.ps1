param(
    [string]$ReportPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw $Message }

$root = Split-Path -Parent $PSScriptRoot
$script = Join-Path $PSScriptRoot 'audit_runtime_text_ownership.py'
if (-not (Test-Path -LiteralPath $script -PathType Leaf)) {
    Fail "Runtime text ownership audit is missing: $script"
}

$arguments = @($script, '--root', $root)
if (-not [string]::IsNullOrWhiteSpace($ReportPath)) {
    $arguments += @('--report', $ReportPath)
}

$python = Get-Command python -ErrorAction SilentlyContinue
if ($null -ne $python) {
    $output = @(& $python.Source @arguments 2>&1)
    $exitCode = [int]$LASTEXITCODE
}
else {
    $python3 = Get-Command python3 -ErrorAction SilentlyContinue
    if ($null -ne $python3) {
        $output = @(& $python3.Source @arguments 2>&1)
        $exitCode = [int]$LASTEXITCODE
    }
    else {
        $launcher = Get-Command py -ErrorAction SilentlyContinue
        if ($null -eq $launcher) {
            Fail 'Configurable runtime text/regex ownership validation requires Python 3 so all source findings can be reported deterministically.'
        }
        $output = @(& $launcher.Source -3 @arguments 2>&1)
        $exitCode = [int]$LASTEXITCODE
    }
}

foreach ($line in $output) {
    Write-Host ([string]$line)
}

if ($exitCode -ne 0) {
    Fail "Configurable runtime text/regex ownership validation failed with exit code $exitCode. Fix the reported source ownership findings; do not baseline, suppress, or hardcode around the guard."
}
