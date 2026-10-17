Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$violations = [Collections.Generic.List[string]]::new()
# Executable architecture/build/publish scripts must not depend on a Python interpreter.
$releaseScripts = @(
    Get-ChildItem -LiteralPath (Join-Path $root 'build') -Recurse -File -Filter '*.ps1'
    Get-ChildItem -LiteralPath $root -File -Filter '*.ps1'
)
foreach ($file in $releaseScripts) {
    $source = [IO.File]::ReadAllText($file.FullName)
    # Visual Studio invokes this boundary directly, bypassing the standalone
    # Assert-PowerShellCompatibility.ps1 used by Build-LocalDevelopment.ps1.
    # Parse the repository's build/publish scripts with the current Windows
    # PowerShell parser BEFORE compiling or launching the expensive PDF render.
    # This detects invalid variable-colon interpolation, among other syntax
    # errors, without executing a script or requiring Python / new tooling.
    $tokens = $null
    $parseErrors = $null
    [void][System.Management.Automation.Language.Parser]::ParseInput(
        $source, [ref]$tokens, [ref]$parseErrors)
    foreach ($parseError in $parseErrors) {
        $violations.Add("$($file.FullName):$($parseError.Extent.StartLineNumber): PowerShell syntax error: $($parseError.Message)")
    }
    foreach ($match in [regex]::Matches($source, '(?im)^\s*(?!#)(?:&\s*)?(?:python(?:3)?|py\s+-3)\s+(?:["\x27]?[^\s"\x27]+\.py\b|\$)|Get-Command\s+(?:python3?|py)\b|\.py["\x27]?\s+(?:--(?:root|source|archive|output|product)|2>&1)')) {
        $violations.Add("$($file.FullName): Python architecture/build invocation: $($match.Value.Trim())")
    }
}
$targets = Join-Path $root 'Directory.Build.targets'
if (-not (Test-Path -LiteralPath $targets -PathType Leaf)) { throw 'Directory.Build.targets is missing.' }
$xml = [xml]([IO.File]::ReadAllText($targets))
foreach ($group in @($xml.Project.PropertyGroup)) {
    foreach ($property in @($group.ChildNodes)) {
        $value = [string]$property.InnerText
        if ($value -match '^\$\(MSBuildThisFileDirectory\)(build/[^<>]+\.ps1)$') {
            $relative = $Matches[1]
            if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) {
                $violations.Add("MSBuild references missing PowerShell contract: $relative")
            }
        }
    }
}
# Release and local-development entrypoints must not invoke scripts that have
# disappeared from the source archive. This would otherwise fail only after
# lengthy release prerequisites (or when a Windows developer never exercises
# the macOS/Linux publish path).
foreach ($entry in @('Build-Release.ps1', 'Build-LocalDevelopment.ps1')) {
    $entryPath = Join-Path $root $entry
    if (-not (Test-Path -LiteralPath $entryPath -PathType Leaf)) {
        $violations.Add("Missing repository entrypoint: $entry")
        continue
    }
    $entrySource = [IO.File]::ReadAllText($entryPath)
    $directCalls = [regex]::Matches(
        $entrySource,
        '(?im)&\s*\(\s*Join-Path\s+\$root\s+[''"](?<script>build/[^''"]+\.ps1)[''"]\s*\)')
    foreach ($call in $directCalls) {
        $relative = $call.Groups['script'].Value
        if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) {
            $violations.Add("$entry directly invokes missing PowerShell script: $relative")
        }
    }
}
$prerequisite = [IO.File]::ReadAllText((Join-Path $root 'build/Initialize-BuildPrerequisites.ps1'))
if ($prerequisite -match '(?i)Python 3 was not found|python preflight') { $violations.Add('Build prerequisite still demands a Python interpreter.') }
$workflowPath = Join-Path $root '.github/workflows/publish-shipped-docs.yml'
if (-not (Test-Path -LiteralPath $workflowPath -PathType Leaf)) { $violations.Add('Missing GitHub Pages workflow.') }
else {
    $workflow = [IO.File]::ReadAllText($workflowPath)
    if ($workflow -match '(?i)python3?\s+|\.py\b') { $violations.Add('GitHub Pages workflow still executes Python.') }
}
# A top-level build must own its tool preparation. A repaired helper/package is
# useless if developers must invoke a separate publish command first.
$oneCommandBuildContracts = @{
    'Build-Release.ps1' = @('build/Ensure-ReleasePackagingPackage.ps1')
    'Build-LocalDevelopment.ps1' = @('build/Ensure-ReleasePackagingPackage.ps1', '-PackagingTool $packagingTool')
    'build/Ensure-ReleasePackagingPackage.ps1' = @('build/Publish-ReleasePackagingPackage.ps1', '$packageNeedsRebuild', 'catch {', 'dotnet publish', 'UseAppHost=true', 'pdf-merge-input-list')
    'build/Publish-ReleasePackagingPackage.ps1' = @('CreateEntry($entryName', 'build/Assert-ReleasePackagingPackageNotices.ps1')
}
foreach ($entry in $oneCommandBuildContracts.Keys) {
    $entryPath = Join-Path $root $entry
    if (-not (Test-Path -LiteralPath $entryPath -PathType Leaf)) {
        $violations.Add("Missing one-command build dependency: $entry")
        continue
    }
    $entryText = [IO.File]::ReadAllText($entryPath)
    foreach ($contract in $oneCommandBuildContracts[$entry]) {
        if ($entryText.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
            $violations.Add("One-command build contract missing from ${entry}: $contract")
        }
    }
}
# The build helper is compiled from this repository; installing its own local
# NuGet package is not a prerequisite and can fail during .store/.stage
# extraction on external macOS volumes. Both entrypoints must run the
# published apphost, while retaining distributable-package notice checks.
$packagePreparation = [IO.File]::ReadAllText((Join-Path $root 'build/Ensure-ReleasePackagingPackage.ps1'))
if ($packagePreparation -match '(?im)^\s*(?:&\s*)?dotnet\s+tool\s+install\b') {
    $violations.Add('The release-packaging preparation must not require dotnet tool install; publish its repository-owned apphost instead.')
}
if ($violations.Count -gt 0) {
    foreach ($violation in $violations) { Write-Host "Repository PowerShell boundary: $violation" -ForegroundColor Red }
    throw "PowerShell-only build/publish boundary failed with $($violations.Count) issue(s)."
}
Write-Host "PowerShell-only architecture, Debug and Release entrypoints verified ($($releaseScripts.Count) scripts)."
