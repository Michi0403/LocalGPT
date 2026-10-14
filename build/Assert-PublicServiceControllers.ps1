# Maintained cross-platform Windows PowerShell 5.1 build guard for service controllers.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'audit_public_service_controllers_5_6_5.py'
if (-not (Test-Path -LiteralPath $scriptPath)) { throw "Public service controller architecture audit script is missing: $scriptPath" }
$pythonCommand = Get-Command python -ErrorAction SilentlyContinue
if (-not $pythonCommand) { throw 'Python is required for the LocalGPT public service controller audit.' }
& $pythonCommand.Source $scriptPath
if ($LASTEXITCODE -ne 0) { throw "Public service controller architecture validation failed (exit $LASTEXITCODE)." }
