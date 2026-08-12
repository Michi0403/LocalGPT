$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$scriptPath = Join-Path $PSScriptRoot 'audit_xround_wiring.py'
if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) { throw "Council X-Round/heartbeat audit is missing: $scriptPath" }
$output = & python $scriptPath 2>&1
$exitCode = $LASTEXITCODE
$output | ForEach-Object { Write-Host $_ }
if ($exitCode -ne 0) { throw "Council X-Round/heartbeat source audit failed with exit code $exitCode." }
