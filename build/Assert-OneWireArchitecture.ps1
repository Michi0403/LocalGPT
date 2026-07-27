param()
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

function Fail([string]$Message) { throw "1-Wire architecture validation failed: $Message" }
function ReadText([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { Fail "Required file is missing: $RelativePath" }
    return [System.IO.File]::ReadAllText($path)
}

$protocolProject = ReadText "LocalGPTWebviewWrapper\LocalGPT.WireProtocolVersion\LocalGPT.WireProtocolVersion.csproj"
$appProject = ReadText "LocalGPTWebviewWrapper\LocalGPT\LocalGPT.csproj"
$wrapperProject = ReadText "LocalGPTWebviewWrapper\LocalGPTWebviewWrapper\LocalGPTWebviewWrapper.csproj"
$buildScript = ReadText "Build-LocalDevelopment.ps1"
$releaseScript = ReadText "Build-Release.ps1"
$installerProject = ReadText "LocalGPTWebviewWrapper\LocalGPTInstallerConsole\LocalGPTInstallerConsole.csproj"
$startLauncher = ReadText "LocalGPTWebviewWrapper\LocalGPTInstallerConsole\Start.cmd"
$installLauncher = ReadText "LocalGPTWebviewWrapper\LocalGPTInstallerConsole\Install.cmd"
$updateLauncher = ReadText "LocalGPTWebviewWrapper\LocalGPTInstallerConsole\Update.cmd"
$uninstallLauncher = ReadText "LocalGPTWebviewWrapper\LocalGPTInstallerConsole\Uninstall.cmd"
$globalUsing = ReadText "LocalGPTWebviewWrapper\LocalGPT\GlobalUsings.OneWire.cs"
$httpController = ReadText "LocalGPTWebviewWrapper\LocalGPT\Controller\OneWireHttpController.cs"
$installer = ReadText "LocalGPTWebviewWrapper\LocalGPTInstallerConsole\Program.cs"
$chat = ReadText "LocalGPTWebviewWrapper\LocalGPT\Components\Pages\Chat.razor"
$modelCouncil = ReadText "LocalGPTWebviewWrapper\LocalGPT\Components\Pages\ModelCouncil.razor"
$chatCss = ReadText "LocalGPTWebviewWrapper\LocalGPT\Components\Pages\Chat.razor.css"
$council = ReadText "LocalGPTWebviewWrapper\LocalGPT\Services\MultiModelCouncilService.cs"
$initialData = ReadText "LocalGPTWebviewWrapper\LocalGPT\Services\Persistence\InitialDataCatalog.cs"
$compositeChat = ReadText "LocalGPTWebviewWrapper\LocalGPT\Services\CompositeChatClient.cs"
$settings = Get-Content -Raw -LiteralPath (Join-Path $root "LocalGPTWebviewWrapper\LocalGPT\appsettings.json") | ConvertFrom-Json

if ($protocolProject -notmatch '<Platforms>AnyCPU</Platforms>') { Fail "The protocol project must remain AnyCPU." }
if ($protocolProject -notmatch '<AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>') { Fail "The protocol output must remain RID-neutral." }
try { [xml]$protocolProjectXml = $protocolProject }
catch { Fail "The protocol project XML could not be parsed: $($_.Exception.Message)" }
$runtimeNodes = @($protocolProjectXml.SelectNodes("/Project/PropertyGroup/RuntimeIdentifier | /Project/PropertyGroup/RuntimeIdentifiers"))
if ($runtimeNodes.Count -gt 0) { Fail "The protocol project must not declare RuntimeIdentifier or RuntimeIdentifiers properties." }
if ($appProject -notmatch 'UseLocalWireProtocolProject') { Fail "LocalGPT must retain explicit project/package protocol modes." }
if ($wrapperProject -match 'LocalGPT\.WireProtocolVersion') { Fail "The WinUI wrapper must not reference the protocol project directly." }
if ($wrapperProject -notmatch '<Nullable>enable</Nullable>') { Fail "The WinUI wrapper must compile with nullable annotations enabled." }
if ($buildScript -notmatch 'UseLocalWireProtocolProject=false') { Fail "The WinUI script graph must consume the RID-neutral protocol package." }
if ($buildScript -notmatch 'Packing the RID-neutral protocol for WinUI/package graph isolation') { Fail "The development script must refresh the protocol package before wrapper restore." }
if ($buildScript -notmatch 'Rebinding LocalGPT to the RID-neutral package graph for WinUI metadata resolution' -or $buildScript -notmatch '-t:Rebuild') { Fail "The application must be rebuilt in package mode before WinUI metadata resolution." }
if ($buildScript -match 'SetPlatform' -or $releaseScript -match 'SetPlatform') { Fail "SetPlatform must never be forwarded through protocol project references." }
if ($releaseScript -notmatch 'UseLocalWireProtocolProject=false') { Fail "Release WinUI publication must consume the RID-neutral protocol package." }
if ($releaseScript -match 'UseLocalWireProtocolProject=true') { Fail "Release WinUI publication must not reintroduce an x64/x86/ARM64 protocol source graph." }
if ($installerProject -notmatch '<None Update="Start\.cmd">' -or $installerProject -notmatch '<CopyToPublishDirectory>Always</CopyToPublishDirectory>') { Fail "The published setup must contain the reviewed LocalGPT launchers." }
if ($startLauncher -notmatch '--start-localgpt --port 5000') { Fail "Desktop and Start Menu startup must use the canonical LocalGPT loopback port." }
if ($installLauncher -notmatch '--install-localgpt --force-delete --shortcuts --start-localgpt --port 5000') { Fail "LocalGPT fresh install launcher no longer uses the canonical install/start/shortcut path." }
if ($updateLauncher -notmatch '--install-localgpt --shortcuts --start-localgpt --port 5000' -or $updateLauncher -match '--force-delete') { Fail "LocalGPT update must preserve runtime identity, MFA trust, local databases and user data." }
if ($uninstallLauncher -notmatch '--uninstall --force-delete') { Fail "LocalGPT Uninstall launcher no longer performs the reviewed application removal path." }
if ($globalUsing -notmatch 'global using LocalGPT\.WireProtocol;') { Fail "The application-wide protocol namespace import is missing." }
if ($buildScript -notmatch '-p:BuildProjectReferences=false') { Fail "The wrapper script build must use already-built application output instead of rebuilding the graph with an x64 protocol path." }
if ($installer -notmatch 'WaitForRuntimeEndpoint' -or $installer -notmatch 'TryGetRunningEndpoint') { Fail "The installer/start command must wait for the real runtime URL instead of guessing after a fixed sleep." }
if ($installer -match 'Thread\.Sleep\(TimeSpan\.FromSeconds\(2\)\)') { Fail "The old two-second guessed browser launch returned." }
if ($chat -notmatch 'council-command-center') { Fail "Advanced Council controls must remain collapsible so they cannot hide the chat." }
if ($chat -notmatch 'IsUnavailableConfiguredOllamaSession' -or $chat -notmatch 'SelectReachableFallbackSession') { Fail "Configured offline Ollama sessions must not remain the automatic active model after health discovery." }
if ($chatCss -notmatch 'prompt-suggestion' -or $chatCss -notmatch 'send-button') { Fail "Prompt-suggestion contrast and touch-sized Send safeguards are missing." }
if ($council -notmatch 'RetryParticipantWithSafeLimitsAsync' -or $council -notmatch 'ApplyApprovedOneRunModelExclusionsAsync') { Fail "Council model recovery/exclusion safeguards are missing." }
if ($council -notmatch 'AppendRuntimeBenchmarkSummary' -or $council -notmatch 'OrderParticipantsByObservedHealth') { Fail "Council runtime benchmarking and health-based phase ordering safeguards are missing." }
if ($compositeChat -notmatch 'var retryUpdates = session\.Client' -or $compositeChat -notmatch 'retryUpdates\.MoveNextAsync') { Fail "Streaming retry must use the compiler-safe manual async enumerator pattern." }
if ($compositeChat -match 'await foreach \(var update in session\.Client') { Fail "Streaming retry reintroduced yield-return inside a try/catch async-iterator pattern that cannot compile." }
if ($initialData -notmatch 'DefaultCouncilResourceLoadPercent", "100"') { Fail "New installations must default Council hardware power to 100 percent." }
if ($modelCouncil -notmatch 'MaxOutputTokens = 262144' -or $modelCouncil -notmatch 'ResourceLoadPercent = 100' -or $modelCouncil -notmatch 'MaxContextTokens = 262144') { Fail "The dedicated AI Council page must use the same full-capability default as DXAIChat." }
if ($initialData -notmatch 'DefaultCouncilCritiqueRounds", "1"' -or $chat -notmatch 'MaxRounds = Math.Clamp\(CouncilCritiqueRounds, 0, 3\)') { Fail "Council review depth must keep a balanced one-round default and bounded 0-3 range." }
if ($httpController -notmatch 'using LocalGPT\.Services\.OneWire;') { Fail "OneWireHttpController cannot resolve OneWireMessageDispatcher." }
if ([int]$settings.LocalGPT.Port -ne 5000) { Fail "LocalGPT default web port must remain 5000." }
if ([int]$settings.OneWire.ServicePort -ne 51140 -or [int]$settings.OneWire.DiscoveryPort -ne 51141) { Fail "1-Wire defaults must remain TCP 51140 / UDP 51141." }
if ([string]$settings.OneWire.BroadcastAddress -ne '255.255.255.255') { Fail "1-Wire broadcast address must remain 255.255.255.255." }

Write-Host "1-Wire architecture validation passed for LocalGPT." -ForegroundColor Green
