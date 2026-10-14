$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$script = Join-Path $PSScriptRoot 'audit_cross_platform_boundaries.py'
$python = Get-Command python -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $python) { $python = Get-Command python3 -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1 }
if ($null -eq $python) { throw 'Python 3 is required for the LocalGPT cross-platform boundary audit.' }

& $python.Source $script
if ($LASTEXITCODE -ne 0) { throw "LocalGPT cross-platform boundary audit failed with exit code $LASTEXITCODE." }
