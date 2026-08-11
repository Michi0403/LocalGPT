$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$scriptPath = Join-Path $PSScriptRoot 'audit_codegen_dxfunction_wiring.py'
if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) { throw "Code-generation DXFunction audit is missing: $scriptPath" }
$output = & python $scriptPath 2>&1
$exitCode = $LASTEXITCODE
$output | ForEach-Object { Write-Host $_ }
if ($exitCode -ne 0) { throw "Code-generation DXFunction source audit failed with exit code $exitCode." }
