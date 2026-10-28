$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$required = @(
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.RunOrchestration.cs'; Tokens = @('LoadContinuationConversationAsync','ContinuedFromConversationId') }
    @{ Path = 'src/LocalGPT/Services/CouncilRuntimeService.PromptAndTextRuntime.cs'; Tokens = @('CreateUpdate') }
    @{ Path = 'src/LocalGPT/Services/CouncilRuntimeService.cs'; Tokens = @('NotRunLane') }
    @{ Path = 'src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs'; Tokens = @('RunUiActionAsync') }
    # Source-read readiness requires the original unrestricted native-function contract,
    # preferred tools plus broker fallback, and exclusive strict lists.
    @{ Path = 'src/LocalGPT/Services/CouncilAutomaticFunctionPolicyService.cs'; Tokens = @('var exposed = preferred', '"dxai.tools.discover", "dxai.tools.preview", "dxai.tools.invoke"', 'strictTeam || strictStep', 'IsStrict: true', '!IsBrokerTool(name)', 'exposed,') }
    @{ Path = 'src/LocalGPT/Services/OllamaThinkingChatClient.Tools.cs'; Tokens = @('automaticFunctionAllowList is null || automaticFunctionAllowList.Contains(function.Name)', 'function.SupportsAutomaticInvocation', 'function.AvailableToAi') }
    @{ Path = 'src/LocalGPT/Services/OllamaThinkingChatClient.ToolExecution.cs'; Tokens = @('var directMatch = functions.FirstOrDefault', 'automaticFunctionAllowList.Contains("dxai.tools.invoke")', 'var transportMatches = eligible', 'return transportMatches.Count == 1') }
    @{ Path = 'src/LocalGPT/Services/ChatUploadWorkspaceDxAiFunctions.cs'; Tokens = @('"chat.upload_workspace_files"', '"chat.upload_workspace_file"', 'Source: "DIHandler"', 'SupportsAutomaticInvocation: true') }
    @{ Path = 'src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.GameProjectTemplates.cs'; Tokens = @('"game-development-baseline"', '"chat.upload_workspace_files"', '"chat.upload_workspace_file"', '"game-development-mechanics"', '"game-development-ai"') }
    @{ Path = 'src/LocalGPT/BusinessObjects/OrganicCouncilModels.cs'; Tokens = @('StrictTeamAllowList = 5', 'StrictStepAllowList = 6', 'bool IsStrict = false') }
    @{ Path = 'src/LocalGPT/Components/Pages/CouncilTeams.razor.cs'; Tokens = @('CouncilAutomaticFunctionPolicyMode.StrictTeamAllowList', 'CouncilAutomaticFunctionPolicyMode.StrictStepAllowList') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.WorkflowPrompting.cs'; Tokens = @('Tool initiative:', 'Repair boundary:', 'automaticFunctionPolicy.IsStrict') }
)
$findings = [Collections.Generic.List[string]]::new()
foreach ($entry in $required) {
    $file = Join-Path $root $entry.Path
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { $findings.Add('Missing source: ' + $entry.Path); continue }
    $content = [IO.File]::ReadAllText($file)
    foreach ($token in $entry.Tokens) {
        if ($content.IndexOf($token, [StringComparison]::Ordinal) -lt 0) { $findings.Add($entry.Path + ': missing contract ' + $token) }
    }
}

# The Council run is server-owned; UI streaming is only an expendable observer.
# Keep the contract in this maintained, cross-platform PowerShell guard rather
# than adding release-specific Python source tests to tests/static.
$clientPath = Join-Path $root 'src/LocalGPT/Services/CouncilChatClient.cs'
$clientSource = [IO.File]::ReadAllText($clientPath)
$streamSignature = 'public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync'
$workerSignature = 'private async Task<MultiModelCouncilResult?> RunCouncilInBackgroundAsync'
$streamStart = $clientSource.IndexOf($streamSignature, [StringComparison]::Ordinal)
if ($streamStart -lt 0) {
    $findings.Add('CouncilChatClient.cs: missing streaming Council observer entry point.')
}
else {
    $streamEnd = $clientSource.IndexOf($workerSignature, $streamStart, [StringComparison]::Ordinal)
    if ($streamEnd -lt 0) {
        $findings.Add('CouncilChatClient.cs: missing independently owned Council worker.')
    }
    else {
        $stream = $clientSource.Substring($streamStart, $streamEnd - $streamStart)
        $launchIndex = $stream.IndexOf('var runTask = RunCouncilInBackgroundAsync(', [StringComparison]::Ordinal)
        $firstYieldIndex = $stream.IndexOf('yield return councilRuntime.CreateUpdate(liveMessageMarker', [StringComparison]::Ordinal)
        if ($launchIndex -lt 0 -or $firstYieldIndex -lt 0 -or $launchIndex -ge $firstYieldIndex) {
            $findings.Add('CouncilChatClient.cs: start the backend Council task before yielding the first browser update.')
        }
        foreach ($contract in @(
            'using var observerCancellationRegistration = cancellationToken.Register(',
            'Interlocked.Exchange(ref attached, 0)',
            'if (Volatile.Read(ref attached) == 1)',
            'RunCouncilInBackgroundAsync(request, liveCancellation,',
            'yield break;',
            'liveSessions.Append(request.RunId, text)',
            'Channel.CreateBounded<string>(new BoundedChannelOptions(512)',
            'FullMode = BoundedChannelFullMode.DropOldest',
            'request.StreamUpdate = Publish'
        )) {
            if ($stream.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
                $findings.Add('CouncilChatClient.cs: observer isolation contract missing: ' + $contract)
            }
        }
        if ($stream -match '(?m)^\s*liveSessions\.Cancel\s*\(') {
            $findings.Add('CouncilChatClient.cs: disconnecting a browser observer must never cancel the backend Council run.')
        }
    }
}
foreach ($contract in @('serviceScopeFactory.CreateAsyncScope()', 'liveSessions.Complete(request.RunId)', 'writer.TryComplete()')) {
    if ($clientSource.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
        $findings.Add('CouncilChatClient.cs: background owner contract missing: ' + $contract)
    }
}
$chatJsPath = Join-Path $root 'src/LocalGPT/wwwroot/js/localgpt-chat-ui.js'
$chatJs = [IO.File]::ReadAllText($chatJsPath)
$nativeStopStart = $chatJs.IndexOf("nativeStop.addEventListener('click'", [StringComparison]::Ordinal)
$nativeStopEnd = if ($nativeStopStart -ge 0) {
    $chatJs.IndexOf('const nativeSend = actionButtons.find(', $nativeStopStart, [StringComparison]::Ordinal)
} else { -1 }
if ($nativeStopStart -lt 0 -or $nativeStopEnd -lt 0) {
    $findings.Add('localgpt-chat-ui.js: missing native Stop click handler or its boundary.')
}
else {
    $nativeStopHandler = $chatJs.Substring($nativeStopStart, $nativeStopEnd - $nativeStopStart)
    foreach ($contract in @(
        'if (!councilComposerActive || !councilComposerDotNet) return;',
        "invokeMethodAsync('StopActiveCouncilRunAsync')"
    )) {
        if ($nativeStopHandler.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
            $findings.Add('localgpt-chat-ui.js: explicit native Stop click contract missing: ' + $contract)
        }
    }
}
if ($chatJs.IndexOf("nativeStop.hasAttribute('data-localgpt-council-stop-bridge')", [StringComparison]::Ordinal) -lt 0) {
    $findings.Add('localgpt-chat-ui.js: native Stop click bridge must prevent duplicate registration.')
}
$liveChatPath = Join-Path $root 'src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs'
$liveChat = [IO.File]::ReadAllText($liveChatPath)
if ($liveChat -notmatch '(?s)\[JSInvokable\]\s*public\s+async\s+Task<bool>\s+StopActiveCouncilRunAsync\s*\(') {
    $findings.Add('Chat.LiveCouncil.razor.cs: missing JS-callable explicit Stop handler.')
}
if ($liveChat.IndexOf('CouncilLiveSessions.Cancel(runId)', [StringComparison]::Ordinal) -lt 0) {
    $findings.Add('Chat.LiveCouncil.razor.cs: explicit Stop must cancel the authoritative live Council session.')
}

# These runtime targets remain supported; version strings are read from source
# by the existing repository boundary audit, never hardcoded into this guard.
$projectFile = Join-Path $root 'src/LocalGPT/LocalGPT.csproj'
$projectXml = [xml]([IO.File]::ReadAllText($projectFile))
$runtimeNode = $projectXml.SelectSingleNode('/Project/PropertyGroup/RuntimeIdentifiers')
if ($null -eq $runtimeNode) {
    $findings.Add('LocalGPT.csproj: RuntimeIdentifiers must preserve the existing Windows, Linux and macOS targets.')
}
else {
    $rids = @($runtimeNode.InnerText.Split(';') | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    foreach ($rid in @('win-x64', 'win-x86', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')) {
        if ($rid -notin $rids) {
            $findings.Add('LocalGPT.csproj: missing previously supported runtime identifier: ' + $rid)
        }
    }
}
foreach ($relative in @(
    'src/LocalGPT/Services/MultiModelCouncilService.KernelTournament.cs',
    'src/LocalGPT/Services/GameSceneEngine.cs',
    'src/LocalGPT/Services/GameStudioService.cs',
    'src/LocalGPT/Services/CouncilGameSessionService.cs'
)) {
    if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) {
        $findings.Add('Council legacy/declarative game implementation missing: ' + $relative)
    }
}

$nativeToolsSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/OllamaThinkingChatClient.Tools.cs'))
if ($nativeToolsSource -match 'automaticFunctionAllowList\s+is\s+null\s*\?\s*function\.Source\s*==\s*"DxFunctionBroker"') {
    $findings.Add('Ollama automatic-tool default regressed to broker-only; 5.5.0 exposed all eligible DXFunctions.')
}
if ($findings.Count -gt 0) { foreach ($finding in $findings) { Write-Error $finding }; throw ('Source contract audit failed: ' + $findings.Count + ' finding(s).') }
Write-Host 'Maintained PowerShell source contract audit passed.'
