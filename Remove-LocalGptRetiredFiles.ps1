#Requires -Version 5.1
<#
.SYNOPSIS
    Removes the 15 source files that LocalGPT 7.0.0 retired.

.DESCRIPTION
    Copying the 7.0.0 sources over a 6.9.x checkout adds and replaces files, but it cannot delete the files that
    7.0.0 removed. Those leftovers still declare the old game types and runtime enums, so the build fails with
    duplicate or missing types until they are gone.

    The script only touches the fixed list below, only inside -RepositoryRoot, and only after checking that the
    checkout is already on 7.0.0. A 6.9.x checkout, which still needs these files, is refused.

.PARAMETER RepositoryRoot
    Root of the LocalGPT checkout (the folder that contains src\LocalGPT). Defaults to the current folder.

.PARAMETER UseGit
    Removes tracked files with 'git rm', so the deletions are staged. Untracked leftovers are removed normally.
    A tracked file with local changes is not forced; it is reported and left in place.

.EXAMPLE
    .\Remove-LocalGptRetiredFiles.ps1 -WhatIf
    Lists the retired files that are still present, without changing anything.

.EXAMPLE
    .\Remove-LocalGptRetiredFiles.ps1 -RepositoryRoot D:\Source\LocalGPT -UseGit
    Removes the leftovers and stages the deletions. Add -Confirm:$false to skip the confirmation prompt.
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string] $RepositoryRoot = (Get-Location).Path,
    [switch] $UseGit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Files present in 6.9.9 and deleted in 7.0.0 (paths relative to the repository root).
$retiredFiles = @(
    'src/LocalGPT/BusinessObjects/CouncilAsciiColorModels.cs',
    'src/LocalGPT/BusinessObjects/CouncilGameDirectorModels.cs',
    'src/LocalGPT/BusinessObjects/CouncilGameDisplayModels.cs',
    'src/LocalGPT/BusinessObjects/CouncilGameModels.cs',
    'src/LocalGPT/BusinessObjects/CouncilServiceStateModels.cs',
    'src/LocalGPT/BusinessObjects/GameProjectModels.cs',
    'src/LocalGPT/BusinessObjects/LocalGptRuntimePolicyModels.cs',
    'src/LocalGPT/Services/CouncilGameSessionService.Campaign.cs',
    'src/LocalGPT/Services/CouncilGameSessionService.Combat.cs',
    'src/LocalGPT/Services/CouncilGameSessionService.KernelTournament.cs',
    'src/LocalGPT/Services/CouncilGameSessionService.KernelTournamentBattleView.cs',
    'src/LocalGPT/Services/CouncilGameSessionService.KernelTournamentRules.cs',
    'src/LocalGPT/Services/CouncilGameSessionService.Rendering.cs',
    'src/LocalGPT/Services/CouncilGameSessionService.Rules.cs',
    'src/LocalGPT/Services/MultiModelCouncilService.KernelTournament.cs'
)

# Runs git in the repository without letting its stderr turn into a terminating error (Windows PowerShell 5.1).
function Invoke-Git {
    param([string[]] $Arguments)
    $ErrorActionPreference = 'Continue'
    $output = & git -C $root @Arguments 2>&1
    [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = ($output | Out-String).Trim() }
}

$root = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
$projectFile = Join-Path $root 'src/LocalGPT/LocalGPT.csproj'
if (-not (Test-Path -LiteralPath $projectFile -PathType Leaf)) {
    throw "No LocalGPT checkout at '$root' (src/LocalGPT/LocalGPT.csproj is missing). Pass -RepositoryRoot."
}

$versionMatch = Select-String -LiteralPath $projectFile -Pattern '<Version>(\d+)\.(\d+)\.(\d+)</Version>' | Select-Object -First 1
if ($null -eq $versionMatch) {
    throw "Could not read <Version> from '$projectFile'."
}
$groups = $versionMatch.Matches[0].Groups
$version = '{0}.{1}.{2}' -f $groups[1].Value, $groups[2].Value, $groups[3].Value
if ([int]$groups[1].Value -lt 7) {
    throw "The checkout at '$root' is on LocalGPT $version. Copy the 7.0.0 sources over it first; 6.9.x still needs these files."
}

$engineFile = Join-Path $root 'src/LocalGPT/Services/GameEngine/GameEngineRuntimeService.cs'
if (-not (Test-Path -LiteralPath $engineFile -PathType Leaf)) {
    throw "The project says $version, but src/LocalGPT/Services/GameEngine/GameEngineRuntimeService.cs is missing. The 7.0.0 sources look incomplete; nothing was removed."
}

if ($UseGit) {
    if ($null -eq (Get-Command git -ErrorAction SilentlyContinue)) {
        throw '-UseGit was given, but git is not on PATH.'
    }
    if ((Invoke-Git @('rev-parse', '--is-inside-work-tree')).ExitCode -ne 0) {
        throw "-UseGit was given, but '$root' is not a git working tree."
    }
}

$present = New-Object System.Collections.Generic.List[object]
foreach ($relative in $retiredFiles) {
    $full = Join-Path $root $relative
    if (Test-Path -LiteralPath $full -PathType Leaf) {
        $present.Add([pscustomobject]@{ Relative = $relative; Full = $full })
    }
    else {
        Write-Verbose "Already gone: $relative"
    }
}

Write-Host "LocalGPT $version at ${root}: $($present.Count) of $($retiredFiles.Count) retired files are still present."
if ($present.Count -eq 0) {
    Write-Host 'Nothing to remove.'
    return
}
foreach ($item in $present) {
    Write-Host "  $($item.Relative)"
}

if (-not $PSCmdlet.ShouldProcess($root, "Remove $($present.Count) retired LocalGPT 6.9.x source files")) {
    return
}

$removed = 0
$failed = 0
foreach ($item in $present) {
    try {
        $tracked = $false
        if ($UseGit) {
            $tracked = (Invoke-Git @('ls-files', '--error-unmatch', '--', $item.Relative)).ExitCode -eq 0
        }

        if ($tracked) {
            $result = Invoke-Git @('rm', '--quiet', '--', $item.Relative)
            if ($result.ExitCode -ne 0) {
                throw "git rm exited with code $($result.ExitCode): $($result.Output)"
            }
            Write-Host "Removed (git rm): $($item.Relative)"
        }
        else {
            Remove-Item -LiteralPath $item.Full -Force
            Write-Host "Removed: $($item.Relative)"
        }
        $removed++
    }
    catch {
        $failed++
        Write-Warning "Could not remove $($item.Relative): $($_.Exception.Message)"
    }
}

Write-Host "Done: $removed removed, $failed failed."
if ($failed -gt 0) {
    exit 1
}
