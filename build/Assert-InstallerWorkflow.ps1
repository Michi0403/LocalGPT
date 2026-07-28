[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$installerRoot = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPTInstallerConsole'

function Fail([string]$Message) {
    throw "LocalGPT installer workflow validation failed: $Message"
}

$programPath = Join-Path $installerRoot 'Program.cs'
$program = Get-Content -LiteralPath $programPath -Raw
$parseStart = $program.IndexOf('public static CliOptions Parse(string[] args)', [StringComparison]::Ordinal)
if ($parseStart -lt 0) { Fail 'CliOptions.Parse was not found.' }
$parseText = $program.Substring($parseStart)
$defaultStart = $parseText.IndexOf('if (argsList.Count == 0)', [StringComparison]::Ordinal)
$returnIndex = $parseText.IndexOf('return options;', $defaultStart, [StringComparison]::Ordinal)
if ($defaultStart -lt 0 -or $returnIndex -lt 0) { Fail 'The no-command workflow was not found.' }
$defaultBlock = $parseText.Substring($defaultStart, ($returnIndex - $defaultStart) + 'return options;'.Length)
foreach ($required in @(
    'InstallLocalGptWin = true',
    'StartLocalGpt = true',
    'InstallOllama = true',
    'PullOllamaModels = true',
    'Range = ModelRange.Slim',
    'DesktopShortcuts = true',
    'StartMenuShortcuts = true'
)) {
    if (-not $defaultBlock.Contains($required)) { Fail "The default workflow is missing $required." }
}
if ($defaultBlock.Contains('ForceDelete') -or $defaultBlock.Contains('ShowHelp')) {
    Fail 'The no-command workflow may neither delete LocalAppData nor fall back to help.'
}

$launchers = @(
    'Default.cmd',
    'Install.cmd',
    'Update.cmd',
    'Start.cmd',
    'Start-NoBrowser.cmd',
    'Install-Ollama.cmd',
    'Pull-Models-Slim.cmd',
    'Pull-Models-RTX3060.cmd',
    'Pull-Models-Full.cmd',
    'Setup-Learning-Base.cmd',
    'Import-Recommended.cmd',
    'Uninstall.cmd'
)
foreach ($launcher in $launchers) {
    $path = Join-Path $installerRoot $launcher
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Fail "Missing launcher $launcher." }
    $content = Get-Content -LiteralPath $path -Raw
    if ($launcher -ne 'Uninstall.cmd' -and $content.Contains('--force-delete')) {
        Fail "$launcher may not delete the LocalAppData installation."
    }
    if (-not $program.Contains('"' + $launcher + '"')) {
        Fail "Shortcut provisioning is missing $launcher."
    }
}

$defaultLauncher = Get-Content -LiteralPath (Join-Path $installerRoot 'Default.cmd') -Raw
if (-not $defaultLauncher.Contains('call "%~dp0LocalGPTInstallerConsole.exe"')) {
    Fail 'Default.cmd must invoke the setup executable without command-line arguments.'
}

$launchSettingsPath = Join-Path $installerRoot 'Properties\launchSettings.json'
$launchSettings = Get-Content -LiteralPath $launchSettingsPath -Raw | ConvertFrom-Json
#if ($launchSettings.profiles.PSObject.Properties.Count -lt $launchers.Count) {
#    Fail 'Visual Studio launch profiles do not cover the restored startup workflows.'
#}
if (-not $launchSettings.profiles.'LocalGPT Default Install and Update') {
    Fail 'The no-command Visual Studio profile is missing.'
}

$project = Get-Content -LiteralPath (Join-Path $installerRoot 'LocalGPTInstallerConsole.csproj') -Raw
if (-not $project.Contains('<None Update="*.cmd" CopyToOutputDirectory="Always" CopyToPublishDirectory="Always" />')) {
    Fail 'The installer project must deploy every maintained command launcher.'
}


$installerReadme = Get-Content -LiteralPath (Join-Path $installerRoot 'README.md') -Raw
$rootReadme = Get-Content -LiteralPath (Join-Path $root 'README.md') -Raw
foreach ($documentation in @($installerReadme, $rootReadme)) {
    if ($documentation -match 'without arguments (prints|shows) help|performs no installation|No model is pulled by default') {
        Fail 'Installer documentation still describes the removed help-only no-command behavior.'
    }
    if (-not $documentation.Contains('preservation-first')) {
        Fail 'Installer documentation must describe the preservation-first default routine.'
    }
}
if (-not $installerReadme.Contains('Slim minimal model set')) {
    Fail 'Installer documentation must describe the default Slim model check.'
}

Write-Host 'LocalGPT installer workflow validation passed. Double-click runs the preservation-first default install/update routine and all restored launchers are deployed.'
