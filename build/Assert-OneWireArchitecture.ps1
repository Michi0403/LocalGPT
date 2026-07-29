[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$findings = [System.Collections.Generic.List[string]]::new()

function Add-Finding([string]$Message) { $findings.Add($Message) }
function Read-OptionalText([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Add-Finding "Missing file: $RelativePath"
        return ''
    }
    return [IO.File]::ReadAllText($path)
}

$protocolProject = Read-OptionalText 'LocalGPTWebviewWrapper\LocalGPT.WireProtocolVersion\LocalGPT.WireProtocolVersion.csproj'
$appProject = Read-OptionalText 'LocalGPTWebviewWrapper\LocalGPT\LocalGPT.csproj'
$globalUsing = Read-OptionalText 'LocalGPTWebviewWrapper\LocalGPT\GlobalUsings.OneWire.cs'
$interfaces = Read-OptionalText 'LocalGPTWebviewWrapper\LocalGPT\Interfaces\IOneWireServices.cs'
$dispatcher = Read-OptionalText 'LocalGPTWebviewWrapper\LocalGPT\Services\OneWire\OneWireExecutionServices.cs'
$state = Read-OptionalText 'LocalGPTWebviewWrapper\LocalGPT\Services\OneWire\OneWireStateServices.cs'
$transport = Read-OptionalText 'LocalGPTWebviewWrapper\LocalGPT\Services\OneWire\OneWireTransportHostedServices.cs'
$settingsPath = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\appsettings.json'

if ($protocolProject -and $protocolProject -notmatch '<Platforms>AnyCPU</Platforms>') { Add-Finding 'The protocol package project is not explicitly AnyCPU.' }
if ($protocolProject -match '<RuntimeIdentifiers?>') { Add-Finding 'The protocol package project declares a runtime identifier and is no longer RID-neutral.' }
if ($appProject -and $appProject -notmatch 'UseLocalWireProtocolProject') { Add-Finding 'LocalGPT no longer exposes explicit source/package protocol modes.' }
if ($globalUsing -and $globalUsing -notmatch 'global using LocalGPT\.WireProtocol;') { Add-Finding 'The application-wide protocol namespace import is missing.' }
if ($interfaces -and $interfaces -notmatch 'RegisterOwned') { Add-Finding 'Connection-generation ownership is missing from the 1-Wire registry contract.' }
if ($interfaces -and $interfaces -notmatch 'IOneWireReplayGuard') { Add-Finding 'The replay-guard contract is missing.' }
if ($dispatcher -and $dispatcher -notmatch 'OneWireDispatchContext') { Add-Finding 'The dispatcher no longer receives transport-owned peer context.' }
if ($dispatcher -match 'SourcePeerId\s*,\s*"localgpt"[\s\S]{0,180}IsConnected') { Add-Finding 'Review dispatcher identity handling; source envelope data may be participating in internal-call authorization.' }
if ($state -and $state -notmatch 'class OneWireReplayGuard') { Add-Finding 'The replay guard implementation is missing.' }
if ($transport -and ($transport -notmatch 'EnableLanTransport' -or $transport -notmatch 'IPAddress\.Loopback')) { Add-Finding 'The TCP listener no longer defaults to loopback with explicit LAN opt-in.' }

if (-not (Test-Path -LiteralPath $settingsPath -PathType Leaf)) {
    Add-Finding 'Missing LocalGPT appsettings.json.'
}
else {
    try { $settings = Get-Content -Raw -LiteralPath $settingsPath | ConvertFrom-Json }
    catch { Add-Finding "appsettings.json is invalid JSON: $($_.Exception.Message)"; $settings = $null }
    if ($settings) {
        if ([int]$settings.OneWire.ServicePort -ne 51140 -or [int]$settings.OneWire.DiscoveryPort -ne 51141) { Add-Finding '1-Wire ports no longer match TCP 51140 / UDP 51141.' }
        if ([bool]$settings.OneWire.EnableLanTransport) { Add-Finding 'LAN transport is enabled by default; reviewed default is loopback-only.' }
        if ([string]$settings.OneWire.ListenAddress -ne '127.0.0.1') { Add-Finding 'The reviewed default listen address is not 127.0.0.1.' }
    }
}

if ($findings.Count -eq 0) {
    Write-Host 'LocalGPT 1-Wire static audit completed with no findings.' -ForegroundColor Green
}
else {
    foreach ($finding in $findings) { Write-Warning $finding }
    Write-Host "LocalGPT 1-Wire static audit completed with $($findings.Count) finding(s). This audit reports only and does not block the build." -ForegroundColor Yellow
}
