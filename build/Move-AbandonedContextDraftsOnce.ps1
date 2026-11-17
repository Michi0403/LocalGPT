#Requires -Version 5.1
<#
.SYNOPSIS
    Quarantines the ten abandoned ContextManagement drafts supplied separately from LocalGPT.
.DESCRIPTION
    Preview is the default. -Apply moves byte-identical drafts to a unique sibling
    directory outside the repository. Locally edited drafts require explicit
    -IncludeModifiedDrafts after reviewing the preview. No source is deleted;
    no database or migration-history data is accessed. Never invoked by a build.
.PARAMETER RepositoryRoot
    LocalGPT source root containing Directory.Build.targets and src/LocalGPT.
.PARAMETER Apply
    Moves the reviewed draft files out of the compilation tree.
.PARAMETER IncludeModifiedDrafts
    Includes names from the supplied draft archive whose contents were locally changed.
.EXAMPLE
    powershell -NoProfile -File build/Move-AbandonedContextDraftsOnce.ps1
.EXAMPLE
    powershell -NoProfile -File build/Move-AbandonedContextDraftsOnce.ps1 -Apply -IncludeModifiedDrafts
#>
[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [switch]$Apply,
    [switch]$IncludeModifiedDrafts
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Fingerprints of the supplied changesbyqwen.zip, not release versions or exemptions.
$drafts = @{
    '20261117123456_AddContextCheckpointsTable.cs' = '616403b7c59fd095be35c0c4ad56a97057c4991c389f366516526ab8cf649e74'
    'ContextBudgetInfo.cs' = '17146a3ee11438abe97094db12f4659ab5a4321e579815ce75dceacb327a99cb'
    'ContextBudgetService.cs' = 'a235fd3940460f3e61c40d31c87eee5fe699e7fea218319fd90c3620ac35edd0'
    'ContextCheckpoint.cs' = '5a5574036c702bdd1c136c2cc41b4b8de41c6b08f15656bd0b2b0e36f33a16b4'
    'ContextCheckpointService.cs' = '0a5c85fc50f8d4bca7dc3795c029a994860d7a63d4fca5632859572f352af56d'
    'ContextCompactionService.cs' = '3044560be9f3b48dc1ad195791a2b0b3bb141591d4088bb08a32c8421387571d'
    'ContextCompactionSummary.cs' = '480777f79bcd570d632bc4ce2236a5c0a17f5ba6478037ff2b9266356cde9628'
    'IContextBudgetService.cs' = '70ed2e75f36cce2dca8522d8aac3825130d74323c5b5bbfa24ae65abe9ceedfb'
    'IContextCheckpointService.cs' = '116e05dd969d157963a8f624aa1e917ef1ee2038e670a16e71b078d2333ac4c4'
    'IContextCompactionService.cs' = 'f16c6f137b2bc4dff203a6968da8f4e4c315d3c4d29b22afc6b3a39429540bc1'
}
$root = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
$sourceRoot = Join-Path $root 'src/LocalGPT'
if (-not (Test-Path -LiteralPath (Join-Path $root 'Directory.Build.targets') -PathType Leaf) -or
    -not (Test-Path -LiteralPath (Join-Path $sourceRoot 'LocalGPT.csproj') -PathType Leaf)) {
    throw "Not a LocalGPT source checkout: $root"
}
# Do not traverse junctions or symbolic links when inspecting or moving files.
foreach ($directoryPath in @($root, (Join-Path $root 'src'), $sourceRoot)) {
    $directory = Get-Item -LiteralPath $directoryPath -Force
    if ($directory.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "Refusing linked source directory: $directoryPath"
    }
}
$pending = New-Object 'System.Collections.Generic.Stack[string]'
$pending.Push($sourceRoot)
$found = New-Object 'System.Collections.Generic.List[object]'
while ($pending.Count -gt 0) {
    $directoryPath = $pending.Pop()
    foreach ($entry in @(Get-ChildItem -LiteralPath $directoryPath -Force)) {
        if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            if ($entry.PSIsContainer -or $drafts.ContainsKey($entry.Name)) {
                Write-Warning "Skipped linked path: $($entry.FullName)"
            }
            continue
        }
        if ($entry.PSIsContainer) {
            if ($entry.Name -notin @('bin', 'obj', '.git', '.vs', 'node_modules')) {
                $pending.Push($entry.FullName)
            }
            continue
        }
        if (-not $drafts.ContainsKey($entry.Name)) { continue }
        $hash = (Get-FileHash -LiteralPath $entry.FullName -Algorithm SHA256).Hash
        $found.Add([pscustomobject]@{
            Full = $entry.FullName
            Relative = $entry.FullName.Substring($root.Length + 1)
            Hash = $hash
            Original = ($hash -eq $drafts[$entry.Name])
        })
    }
}
if ($found.Count -eq 0) {
    Write-Host 'No abandoned ContextManagement draft names found in the source tree.'
    return
}
foreach ($item in $found) {
    $status = if ($item.Original) { 'exact archive match' } else { 'locally changed or different file; review before moving' }
    Write-Host "  $($item.Relative) [$status]"
}
if (-not $Apply) {
    Write-Host 'PREVIEW ONLY. Stop the application/build, review the paths, then rerun with -Apply. Use -IncludeModifiedDrafts only for reviewed local draft edits.'
    return
}
$modified = @($found | Where-Object { -not $_.Original })
if ($modified.Count -gt 0 -and -not $IncludeModifiedDrafts) {
    throw 'Some draft names have different contents. Nothing moved. Review the preview, then use -Apply -IncludeModifiedDrafts only if those files are abandoned drafts.'
}
# Back up outside the repository, preserving the complete relative paths.
$parent = Split-Path -Parent $root
if ([string]::IsNullOrWhiteSpace($parent)) { throw 'The repository must have a parent directory for the backup.' }
$backup = Join-Path $parent ('LocalGPT-context-draft-backup-' + [Guid]::NewGuid().ToString('N'))
if (Test-Path -LiteralPath $backup) { throw "Backup path already exists: $backup" }
[void][IO.Directory]::CreateDirectory($backup)
Write-Host "Backup directory: $backup"
foreach ($item in $found) {
    $file = Get-Item -LiteralPath $item.Full -Force
    if (($file.Attributes -band [IO.FileAttributes]::ReparsePoint) -or
        (Get-FileHash -LiteralPath $item.Full -Algorithm SHA256).Hash -ne $item.Hash) {
        throw "Source changed since inspection: $($item.Full). Already moved files remain in $backup."
    }
    $destination = Join-Path $backup $item.Relative
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $destination))
    Move-Item -LiteralPath $item.Full -Destination $destination
    Write-Host "Quarantined: $($item.Relative)"
}
Write-Host "Moved $($found.Count) draft file(s). All moved files are retained in $backup. No database files were touched."
