[CmdletBinding()]
param(
    [string]$Solution = 'src/LocalGPTWebviewWrapper.sln',
    [string]$OutputDirectory = 'artifacts/security'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $root $Solution
$outputPath = Join-Path $root $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

# This script only audits the checked-out solution. It does not scan hosts or reproduce exploits.
dotnet restore $solutionPath
$report = Join-Path $outputPath 'nuget-vulnerabilities.json'
dotnet package list $solutionPath --include-transitive --vulnerable --format json | Set-Content -Encoding utf8 $report
Write-Host "Dependency audit written to $report"
