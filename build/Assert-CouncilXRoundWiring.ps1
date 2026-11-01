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
    @{ Path = 'src/LocalGPT/Services/CouncilRoleAssignmentService.cs'; Tokens = @('AssignAsync', 'UserConfirmed', 'teams.SaveAsync', 'RestrictSelectedRolesToAssignedPool', 'item.IsInstalled') }
    @{ Path = 'src/LocalGPT/Services/CouncilRoleAssignmentIndexService.cs'; Tokens = @('SynchronizeAsync', 'CouncilRoleIndexes', 'CouncilRoleModelBindings') }
    @{ Path = 'src/LocalGPT/Components/Shared/ProviderModelRoleEditor.razor'; Tokens = @('ShowSearchBox="true"', 'Save role assignments', 'ListRenderMode.Virtual', 'DxComboBox TData="TeamOption" TValue="string"', 'ValueChanged="@(async (string key) => await TeamChangedAsync(key))"', 'ValuesChanged="@(async (IEnumerable<string> values) => await SelectedChangedAsync(values))"') }
    @{ Path = 'src/LocalGPT/Migrations/20261029123000_AddCouncilRoleAssignments.cs'; Tokens = @('CouncilRoleIndexes', 'CouncilRoleModelBindings') }
    @{ Path = 'src/LocalGPT/Services/Council/CouncilSpoolerService.cs'; Tokens = @('GetRecentSummaries(int take = 50)', 'CouncilSpoolerRunSummary') }
    @{ Path = 'src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs'; Tokens = @('OpenSelectedCouncilHistoryAsync', 'SelectCouncilSessionAsync', 'CouncilSpooler.GetSnapshot(runId)') }
    @{ Path = 'src/LocalGPT/Components/Pages/Chat.Lifecycle.razor.cs'; Tokens = @('openedCouncilArchive = CouncilSpooler.GetSnapshot(rejoinRunId)', 'localGptReconnect.clearCouncilRun') }
    @{ Path = 'src/LocalGPT/Services/UploadProcessingRecommendationService.cs'; Tokens = @('imageOnly', 'RequiresVerifiedWorkspaceFileRead', 'localgpt.vision.ocr.capability') }
    @{ Path = 'src/LocalGPT/Components/Shared/UploadProcessingAdvisor.razor'; Tokens = @('Check local OCR availability', 'Run OCR on quarantined image', 'RecognizeWorkspaceImageAsync') }
    @{ Path = 'src/LocalGPT/Services/WorkspaceVisionOcrService.cs'; Tokens = @('normalizedRelative = "original/" + normalizedRelative;', 'segment is "." or ".."', 'FileAttributes.ReparsePoint', 'ResolveWorkspaceFile(workspaceRoot, normalizedRelative, logger)') }
    @{ Path = 'src/LocalGPT/Services/UploadProcessingRecommendationService.cs'; Tokens = @('if (imageOnly)', 'return await PersistAsync(fallback, cancellationToken).ConfigureAwait(false);', 'capability.StartsWith("localgpt.vision."', 'capability.EndsWith(".ocr"') }


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

# Keep the Chat terminal worker from racing a disposed circuit and from
# monopolizing a busy multi-model renderer. This is a source boundary check,
# not a substitute for host-side verification of Blazor stream timeouts.
$chatLifetimeSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Pages/Chat.razor.cs'))
foreach ($contract in @(
    'if (isDisposed || componentLifetimeCts.IsCancellationRequested || !showGameConsole)',
    'catch (ObjectDisposedException) when (isDisposed)',
    'Interlocked.Exchange(ref asciiMirrorLoopRunning, 0);',
    'if (!isDisposed && !cancellationToken.IsCancellationRequested && Volatile.Read(ref showGameConsole))'
)) {
    if ($chatLifetimeSource.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
        $findings.Add('Chat.razor.cs: missing disposal-safe ASCII mirror contract: ' + $contract)
    }
}
$chatSessionRenderingSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs'))
if ($chatSessionRenderingSource.IndexOf('await Task.Delay(750, cancellationToken).ConfigureAwait(false);', [StringComparison]::Ordinal) -lt 0) {
    $findings.Add('Chat.LiveCouncil.razor.cs: session list refresh must remain debounced during Council streaming.')
}

# Server-owned Council runs must recover automatically when Blazor rejects a
# dead circuit. Keep the existing manual recovery path and prevent reload loops.
$reconnectSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/wwwroot/js/localgpt-reconnect.js'))
foreach ($contract in @(
    'scheduleAutomaticRecovery(600)',
    'scheduleAutomaticRecovery(1200)',
    'scheduleAutomaticRecovery(15000)',
    'claimAutomaticReload()',
    'reloadAndRejoin();',
    'confirmedRunId = String(runId)',
    'attempts >= 2',
    'components-reconnect-rejected',
    'components-reconnect-hide'
)) {
    if ($reconnectSource.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
        $findings.Add('localgpt-reconnect.js: missing bounded automatic Council recovery: ' + $contract)
    }
}

# A dead Blazor circuit must restore both the Chat conversation and the running
# Council. Recovering a Council marker alone must never split one chat in two.
foreach ($contract in @(
    'conversationStorageKey',
    'readConversationId()',
    "url.searchParams.set('rejoinConversationId', conversationId)",
    'setConversation(conversationId)',
    'clearConversation()',
    "url.searchParams.set('rejoinCouncilRunId', String(runId))"
)) {
    if ($reconnectSource.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
        $findings.Add('localgpt-reconnect.js: exact Chat/Council recovery identity contract missing: ' + $contract)
    }
}
$chatRazorSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Pages/Chat.razor.cs'))
$chatPersistSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Pages/Chat.PersistenceAndMemory.razor.cs'))
$chatLifecycleSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Pages/Chat.Lifecycle.razor.cs'))
$chatLiveSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs'))
foreach ($entry in @(
    @{ Name = 'Chat.razor.cs'; Content = $chatRazorSource; Token = 'RequestedRejoinConversationId' },
    @{ Name = 'Chat.razor.cs'; Content = $chatRazorSource; Token = 'LoadSavedConversationAsync(matching, saveCurrentFirst: false, loadIntoDxChat: false)' },
    @{ Name = 'Chat.Lifecycle.razor.cs'; Content = $chatLifecycleSource; Token = 'localGptReconnect.setConversation' },
    @{ Name = 'Chat.LiveCouncil.razor.cs'; Content = $chatLiveSource; Token = 'firstAttachmentToRun && ActiveConversationId is null' },
    @{ Name = 'Chat.PersistenceAndMemory.razor.cs'; Content = $chatPersistSource; Token = 'chatConversationSaveGate.WaitAsync()' },
    @{ Name = 'Chat.PersistenceAndMemory.razor.cs'; Content = $chatPersistSource; Token = 'chatConversationSaveGate.Release()' }
)) {
    if ($entry.Content.IndexOf($entry.Token, [StringComparison]::Ordinal) -lt 0) {
        $findings.Add($entry.Name + ': recovery/persistence contract missing: ' + $entry.Token)
    }
}

# Recovery must preserve the visible DevExpress Council session, not only the DB id.
# The current implementation merges an ordered overlap so reconnecting does not
# duplicate already-visible messages or discard Council messages. Check the whole
# preservation contract, not one older implementation-specific AddRange expression.
foreach ($contract in @(
    'var restoredMessages = restoredChatSession.Messages.ToList();',
    'var visibleMessages = councilSession.Messages.ToList();',
    'var overlap = 0;',
    'councilSession.Messages.AddRange(restoredMessages);',
    'councilSession.Messages.AddRange(visibleMessages.Skip(overlap));',
    'canonicalConversationMessages.AddRange(councilSession.Messages);',
    'DxAiChat.LoadMessages(councilSession.Messages);'
)) {
    if ($chatLiveSource.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
        $findings.Add('Chat.LiveCouncil.razor.cs: restored Chat/Council overlap and visible-session synchronization missing: ' + $contract)
    }
}

# Optional image exclusions must not block a tool call. Validate the published
# JSON schemas in addition to the invocation and runtime defaults.
$imageFunctionSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/LocalAiRuntimeDxAiFunctions.cs'))
$imageSchemas = [regex]::Matches($imageFunctionSource, 'ParameterSchemaJson:\s*"""(?<json>\{[^\r\n]+\})"""')
$checkedNegativePrompts = 0
foreach ($match in $imageSchemas) {
    # An optional JSON property must be looked up through PSObject.Properties.
    # Under Set-StrictMode -Version Latest, accessing a missing property directly
    # throws PropertyNotFoundStrict *before* a null check can evaluate it.
    # Most DX function schemas legitimately have no negativePrompt property.
    try {
        $schema = $match.Groups['json'].Value | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        $findings.Add('LocalAiRuntimeDxAiFunctions.cs: malformed JSON function schema: ' + $_.Exception.Message)
        continue
    }
    if ($null -eq $schema) {
        $findings.Add('LocalAiRuntimeDxAiFunctions.cs: unexpectedly empty JSON function schema.')
        continue
    }
    $propertiesMember = $schema.PSObject.Properties['properties']
    if ($null -eq $propertiesMember -or $null -eq $propertiesMember.Value) { continue }
    $negativePromptMember = $propertiesMember.Value.PSObject.Properties['negativePrompt']
    if ($null -eq $negativePromptMember -or $null -eq $negativePromptMember.Value) { continue }

    $checkedNegativePrompts++
    $requiredMember = $schema.PSObject.Properties['required']
    if ($null -ne $requiredMember -and @($requiredMember.Value) -contains 'negativePrompt') {
        $findings.Add('LocalAiRuntimeDxAiFunctions.cs: optional negativePrompt must not be schema-required.')
    }
    $defaultMember = $negativePromptMember.Value.PSObject.Properties['default']
    if ($null -eq $defaultMember -or $defaultMember.Value -cne '') {
        $findings.Add('LocalAiRuntimeDxAiFunctions.cs: negativePrompt must default to an empty exclusion set.')
    }
}
if ($checkedNegativePrompts -ne 2) {
    $findings.Add('LocalAiRuntimeDxAiFunctions.cs: both image generation and edit schemas require optional negativePrompt contracts.')
}
if ($imageFunctionSource.IndexOf('binding.Value.NegativePrompt ??= string.Empty;', [StringComparison]::Ordinal) -lt 0) {
    $findings.Add('LocalAiRuntimeDxAiFunctions.cs: null optional prompt must be normalized before runtime dispatch.')
}
$registrySource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/DxAiFunctionRegistry.cs'))
if ($registrySource.IndexOf('propertyValue.ValueKind == JsonValueKind.Null', [StringComparison]::Ordinal) -lt 0 -or
    $registrySource.IndexOf('string.Equals(property.Name, "negativePrompt"', [StringComparison]::Ordinal) -lt 0) {
    $findings.Add('DxAiFunctionRegistry.cs: null optional negativePrompt must not fail the required parameter validator.')
}
$keyaccountSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/GoalExecutionService.KeyaccountManager.cs'))
if ($keyaccountSource.IndexOf('ReadKeyaccountReviewObject(text)', [StringComparison]::Ordinal) -lt 0 -or
    $keyaccountSource.IndexOf('JsonDocument.Parse(text.Substring(start, end - start + 1))', [StringComparison]::Ordinal) -ge 0) {
    $findings.Add('GoalExecutionService.KeyaccountManager.cs: parse a bounded single JSON review, not first-to-last brace prose.')
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
# Prevent misattached XML docs when adding Council spooler methods. A stale <param>
# on the next method generates CS1572, and an omitted tag can generate CS1573.
# Validate both contract and implementation, including exactly one summary block.
foreach ($relative in @(
    'src/LocalGPT/Services/Council/CouncilSpoolerService.cs',
    'src/LocalGPT/Interfaces/ICouncilSpoolerService.cs'
)) {
    $spoolerText = [IO.File]::ReadAllText((Join-Path $root $relative))
    foreach ($contract in @(
        @{ Name = 'GetRecentSummaries'; Parameter = 'take' },
        @{ Name = 'GetSnapshot'; Parameter = 'runId' }
    )) {
        $methodPattern = '(?m)(?<documentation>(?:^[ \t]*///[^\r\n]*\r?\n)+)[ \t]*(?:public[ \t]+)?[^\r\n;{}]*?\b' +
            [regex]::Escape($contract.Name) + '\s*\((?<parameters>[^)]*)\)'
        $methodMatch = [regex]::Match($spoolerText, $methodPattern)
        if (-not $methodMatch.Success) {
            $findings.Add($relative + ': missing adjacent XML documentation for ' + $contract.Name + '.')
            continue
        }
        $xmlDoc = $methodMatch.Groups['documentation'].Value
        $docParameters = @([regex]::Matches($xmlDoc, '<param\s+name="(?<name>[^"]+)"') |
            ForEach-Object { $_.Groups['name'].Value })
        $methodParameters = $methodMatch.Groups['parameters'].Value
        if ($docParameters.Count -ne 1 -or $docParameters[0] -cne $contract.Parameter -or
            $methodParameters -notmatch ('\b' + [regex]::Escape($contract.Parameter) + '\b')) {
            $findings.Add($relative + ': XML parameters do not match ' + $contract.Name + '(' + $contract.Parameter + ') (CS1572/CS1573).')
        }
        if ([regex]::Matches($xmlDoc, '<summary(?:\s|>)').Count -ne 1 -or
            [regex]::Matches($xmlDoc, '<returns>').Count -ne 1) {
            $findings.Add($relative + ': duplicate/missing XML summary or returns for ' + $contract.Name + '.')
        }
    }
}

# Keep new Chat selection properties from collecting XML comments belonging to
# the property below. Misattached documentation can be misleading even if it
# does not itself raise the same compiler diagnostic as an invalid <param>.
$chatComponentSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Pages/Chat.razor.cs'))
foreach ($propertyName in @('CouncilSessionHistoryOptions', 'FeedbackTargetOptions', 'AsciiFunButtonCssClass')) {
    $propertyPattern = '(?m)(?<documentation>(?:^[ \t]*///[^\r\n]*\r?\n)+)[ \t]*private[ \t]+[^\r\n]*\b' +
        [regex]::Escape($propertyName) + '[ \t]*=>'
    $propertyMatch = [regex]::Match($chatComponentSource, $propertyPattern)
    if (-not $propertyMatch.Success -or
        [regex]::Matches($propertyMatch.Groups['documentation'].Value, '<summary(?:\s|>)').Count -ne 1) {
        $findings.Add('Chat.razor.cs: misattached/duplicate XML documentation for ' + $propertyName + '.')
    }
}

# Whenever constructor injection changes, its XML documentation must change with it.
# CS1572/CS1573 are build errors; catch this Council-specific regression in the
# existing mandatory PowerShell source gate, before invoking the C# compiler.
$teamConfigurationSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/CouncilTeamConfigurationService.cs'))
$constructorDocumentationPattern = '(?m)(?<documentation>(?:^[ \t]*///[^\r\n]*(?:\r?\n))+)[ \t]*public[ \t]+CouncilTeamConfigurationService[ \t]*\((?<parameters>[\s\S]*?)\)[ \t]*(?:\r?\n)[ \t]*\{'
$constructorDocumentationMatch = [regex]::Match($teamConfigurationSource, $constructorDocumentationPattern)
if (-not $constructorDocumentationMatch.Success) {
    $findings.Add('CouncilTeamConfigurationService.cs: constructor and its XML documentation could not be located.')
}
else {
    $documented = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($parameterTag in [regex]::Matches($constructorDocumentationMatch.Groups['documentation'].Value, '<param\s+name="(?<name>[^"]+)"')) {
        [void]$documented.Add($parameterTag.Groups['name'].Value)
    }
    $declared = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($argument in $constructorDocumentationMatch.Groups['parameters'].Value.Split(',')) {
        if ($argument -notmatch '([A-Za-z_][A-Za-z0-9_]*)\s*$') {
            $findings.Add('CouncilTeamConfigurationService.cs: could not read constructor parameter: ' + $argument.Trim())
            continue
        }
        [void]$declared.Add($Matches[1])
    }
    foreach ($name in $declared) {
        if (-not $documented.Contains($name)) {
            $findings.Add('CouncilTeamConfigurationService.cs: constructor XML documentation is missing <param name="' + $name + '"> (CS1573).')
        }
    }
    foreach ($name in $documented) {
        if (-not $declared.Contains($name)) {
            $findings.Add('CouncilTeamConfigurationService.cs: XML documentation references an absent constructor parameter: ' + $name + ' (CS1572).')
        }
    }
}

if ($findings.Count -gt 0) { foreach ($finding in $findings) { Write-Error $finding }; throw ('Source contract audit failed: ' + $findings.Count + ' finding(s).') }
Write-Host 'Maintained PowerShell source contract audit passed.'
