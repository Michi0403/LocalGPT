$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$scriptPath = Join-Path $PSScriptRoot 'Assert-XmlDocumentationCoverage.py'
if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) { throw "XML documentation coverage audit is missing: $scriptPath" }
$output = & python $scriptPath (Join-Path $repositoryRoot 'src') 2>&1
$exitCode = $LASTEXITCODE
$output | ForEach-Object { Write-Host $_ }
if ($exitCode -ne 0) { throw "XML documentation coverage validation failed with exit code $exitCode." }
