# Source-contract gate for interactive checkbox validation, pairing and install navigation.
param([string]$RepositoryRoot = '')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) { $RepositoryRoot = Split-Path -Parent $PSScriptRoot }
function Require-Source([string]$Path, [string]$Fragment, [string]$Code) {
    $absolute = Join-Path $RepositoryRoot $Path
    if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) { throw "${Code}: Missing $Path" }
    $content = [System.IO.File]::ReadAllText($absolute)
    if (-not $content.Contains($Fragment)) { throw "${Code}: $Path lost required source contract '$Fragment'." }
}
$layout = Join-Path $RepositoryRoot 'src/LocalGPT/Components/Layout/MainLayout.razor'
$layoutSource = [System.IO.File]::ReadAllText($layout)
if ($layoutSource.Contains('localgpt-page-scroll-assist') -or $layoutSource.Contains('#install-scroll-top') -or $layoutSource.Contains('#install-scroll-bottom')) {
    throw 'INSTALLUI0001: Remove the two unwanted floating Install scroll buttons from MainLayout.razor.'
}
Require-Source 'src/LocalGPT/Components/Shared/NetworkCertificateEditor.razor' 'CheckedExpression="@(() => CertificateRequest.IsCertificateAuthority)"' 'INSTALLUI0002'
Require-Source 'src/LocalGPT/Components/Pages/OneWireSecurity.razor' 'Security.CreatePairingTicketAsync(TimeSpan.FromMinutes(15), lifetimeCts.Token)' 'ONEWIREUI0001'
Require-Source 'src/LocalGPT/Components/Pages/Install.razor.cs' 'HTTPS requested; certificate must pass startup validation' 'INSTALLUI0003'
Require-Source 'src/LocalGPT/Components/Pages/Install.razor' 'This is a configuration preview, not the running listener.' 'INSTALLUI0004'
$componentRoot = Join-Path $RepositoryRoot 'src/LocalGPT/Components'
$checkedCount = 0
foreach ($component in @(Get-ChildItem -LiteralPath $componentRoot -Recurse -File -Filter '*.razor')) {
    $content = [System.IO.File]::ReadAllText($component.FullName)
    if ($content.Contains('= ValidationEnabled="false">')) { throw "INSTALLUI0005: Invalid Razor lambda in $($component.FullName)" }
    foreach ($match in [regex]::Matches($content, '(?s)<DxCheckBox\b.*?/>')) {
        $checkbox = [string]$match.Value
        if ($checkbox.Contains('Checked=') -and -not $checkbox.Contains('@bind-Checked') -and
            -not $checkbox.Contains('CheckedExpression=') -and -not $checkbox.Contains('ValidationEnabled="false"')) {
            throw "INSTALLUI0006: $($component.FullName) contains Checked/CheckedChanged without a field expression or explicit calculated-editor validation setting."
        }
        $checkedCount++
    }
}
foreach ($project in @(Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'src') -Recurse -File -Filter '*.csproj')) {
    $projectSource = [System.IO.File]::ReadAllText($project.FullName)
    $declaredVersion = [regex]::Match($projectSource, '<Version>([^<]+)</Version>')
    if (-not $declaredVersion.Success) { continue }
    $versionMatch = [regex]::Match($declaredVersion.Groups[1].Value, '^(\d+)\.(\d+)\.(\d+)$')
    if (-not $versionMatch.Success) { throw "VERSIONUI0001: $($project.FullName) must have three numeric version segments." }
    if ($versionMatch.Groups[2].Length -gt 1 -or $versionMatch.Groups[3].Length -gt 1) {
        throw "VERSIONUI0002: $($project.FullName) must not have two-digit minor or patch version segments."
    }
}
Write-Host "INSTALLUI/ONEWIREUI: checkbox expression, public pairing ticket, honest TLS preview, and removed scroll buttons verified ($checkedCount DX checkboxes)."
