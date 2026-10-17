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
$prerequisite = [IO.File]::ReadAllText((Join-Path $root 'build/Initialize-BuildPrerequisites.ps1'))
if ($prerequisite -match '(?i)Python 3 was not found|python preflight') { $violations.Add('Build prerequisite still demands a Python interpreter.') }
$workflowPath = Join-Path $root '.github/workflows/publish-shipped-docs.yml'
if (-not (Test-Path -LiteralPath $workflowPath -PathType Leaf)) { $violations.Add('Missing GitHub Pages workflow.') }
else {
    $workflow = [IO.File]::ReadAllText($workflowPath)
    if ($workflow -match '(?i)python3?\s+|\.py\b') { $violations.Add('GitHub Pages workflow still executes Python.') }
}
if ($violations.Count -gt 0) {
    foreach ($violation in $violations) { Write-Host "Repository PowerShell boundary: $violation" -ForegroundColor Red }
    throw "PowerShell-only build/publish boundary failed with $($violations.Count) issue(s)."
}
Write-Host "PowerShell-only architecture, Debug and Release entrypoints verified ($($releaseScripts.Count) scripts)."
