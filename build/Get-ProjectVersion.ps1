param([Parameter(Mandatory)][string]$ProjectPath)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

[xml]$project = Get-Content -LiteralPath $ProjectPath -Raw
# A project can have multiple PropertyGroups; most need not declare Version.
# Select the Version elements themselves so StrictMode never reads a missing property.
foreach ($versionNode in $project.SelectNodes('/Project/PropertyGroup/Version')) {
    $version = [string]$versionNode.InnerText
    if (-not [string]::IsNullOrWhiteSpace($version)) { return $version.Trim() }
}
throw "Project version was not found in $ProjectPath"
