param(
    [Parameter(Mandatory)][Alias('Solution')][string[]]$Target,
    [ValidateSet('Release', 'Debug')][string]$Configuration = 'Release',
    [ValidateSet('None', 'Update', 'UpdateWithSudo')][string]$DotNetWorkloads = 'None'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$pathComparison = if ([Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Windows)) {
    [StringComparison]::OrdinalIgnoreCase
}
else {
    [StringComparison]::Ordinal
}
$targetPaths = foreach ($entry in $Target) {
    $path = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $entry))
    if (-not $path.StartsWith($repositoryRoot + [IO.Path]::DirectorySeparatorChar, $pathComparison)) {
        throw 'The validation target must be inside this repository.'
    }
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Validation target not found: $entry" }
    $path
}
$logRoot = Join-Path $repositoryRoot 'artifacts/validation'
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
& (Join-Path $PSScriptRoot 'Assert-PowerShellCompatibility.ps1')
& (Join-Path $PSScriptRoot 'Assert-ProjectVersionResolution.ps1')
# Compile-only releases must enforce the same platform and sandbox source contracts
# as full releases before compiling or preparing downstream artifacts.
& (Join-Path $PSScriptRoot 'Assert-CrossPlatformBoundaries.ps1')
& (Join-Path $PSScriptRoot 'Assert-SessionSandboxIsolation.ps1')
& (Join-Path $PSScriptRoot 'Assert-CouncilRepositorySandboxAndTrafficFiles.ps1')
& (Join-Path $PSScriptRoot 'Update-JavaScriptDiagnosticsManifest.ps1')
& (Join-Path $PSScriptRoot 'Assert-JavaScriptDiagnostics.ps1')
& (Join-Path $repositoryRoot 'build/Initialize-DotNetBuildTools.ps1') -ToolManifest @('.config/dotnet-tools.json', 'src/LocalGPT/dotnet-tools.json') -DotNetWorkloads $DotNetWorkloads

# Keep the normal build targets and stop before platform packaging, setup or launch.
# The wrapper solution also references shared projects with different global
# properties. Build them serially because they write to the same output paths.
$logPath = Join-Path $logRoot 'release-compile.log'
Clear-Content -LiteralPath $logPath -ErrorAction SilentlyContinue
foreach ($path in $targetPaths) {
    & dotnet build $path --configuration $Configuration --verbosity minimal -maxcpucount:1 2>&1 |
        Tee-Object -FilePath $logPath -Append
    if ($LASTEXITCODE -ne 0) { throw "Release compilation of '$path' failed with exit code $LASTEXITCODE. See $logPath." }
}
Write-Host 'Release compilation and build-time maintenance checks passed. No build output was executed.'
