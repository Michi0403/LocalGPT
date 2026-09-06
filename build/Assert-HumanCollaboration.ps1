param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RepositoryValidation.Common.ps1')
$errors = [System.Collections.Generic.List[string]]::new()

function Read-RequiredText([string]$relativePath) {
    $path = Join-Path $RepositoryRoot $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $errors.Add("Missing human-collaboration file: $relativePath")
        return ''
    }
    return Get-Content -LiteralPath $path -Raw
}

$requiredFiles = @(
    'src/LocalGPT/BusinessObjects/AmbientLocalGptContextModels.cs',
    'src/LocalGPT/BusinessObjects/HumanCollaborationModels.cs',
    'src/LocalGPT/Interfaces/IAmbientLocalGptContext.cs',
    'src/LocalGPT/Interfaces/IHumanCollaborationService.cs',
    'src/LocalGPT/Interfaces/IDeferredDxAiInvocationService.cs',
    'src/LocalGPT/Services/AmbientLocalGptContext.cs',
    'src/LocalGPT/Services/HumanCollaborationService.cs',
    'src/LocalGPT/Services/DeferredDxAiInvocationService.cs',
    'src/LocalGPT/Security/HumanApprovalRequiredAttribute.cs',
    'src/LocalGPT/Security/HumanApprovalActionFilter.cs',
    'src/LocalGPT/Components/Layout/HumanCollaborationInbox.razor',
    'src/LocalGPT/Components/Layout/HumanCollaborationInbox.razor.css',
    'src/LocalGPT/Migrations/20260726000000_AddHumanCollaboration.cs',
    'src/LocalGPT/Migrations/20260726001000_AddDeferredDxAiInvocations.cs'
)
foreach ($relative in $requiredFiles) { [void](Read-RequiredText $relative) }

$ambientInterface = Read-RequiredText 'src/LocalGPT/Interfaces/IAmbientLocalGptContext.cs'
foreach ($required in @('interface IAmbientLocalGptContext', 'interface ILocalHumanInteractionContext', 'interface IHumanApprovalExecutionContext', 'PushHumanInteraction', 'PushHumanApproval')) {
    if (-not ($ambientInterface.IndexOf($required, [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("Ambient context contracts must contain '$required'.")
    }
}
$ordinaryInterfaceStart = $ambientInterface.IndexOf('public interface IAmbientLocalGptContext', [System.StringComparison]::Ordinal)
$trustedInterfaceStart = $ambientInterface.IndexOf('public interface ILocalHumanInteractionContext', [System.StringComparison]::Ordinal)
if ($ordinaryInterfaceStart -ge 0 -and $trustedInterfaceStart -gt $ordinaryInterfaceStart) {
    $ordinaryContract = $ambientInterface.Substring($ordinaryInterfaceStart, $trustedInterfaceStart - $ordinaryInterfaceStart)
    if (($ordinaryContract.IndexOf('PushHuman', [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add('IAmbientLocalGptContext must remain unable to mint trusted human scopes.')
    }
}

$allowedInteractionCapabilityFiles = @(
    'Interfaces/IAmbientLocalGptContext.cs',
    'Services/AmbientLocalGptContext.cs',
    'Components/Layout/HumanCollaborationInbox.razor',
    'Components/Pages/Chat.razor',
    'Program.cs'
)
$allowedApprovalCapabilityFiles = @(
    'Interfaces/IAmbientLocalGptContext.cs',
    'Services/AmbientLocalGptContext.cs',
    'Services/DxAiFunctionRegistry.cs',
    'Security/HumanApprovalActionFilter.cs',
    'Program.cs'
)
$sourceRoot = Join-Path $RepositoryRoot 'src/LocalGPT'
Get-ChildItem -Path $sourceRoot -Recurse -File | Where-Object { $_.Extension -in @('.cs', '.razor') } | ForEach-Object {
    $content = Get-Content -LiteralPath $_.FullName -Raw
    $relative = (Get-RelativePathPortable -BasePath $sourceRoot -TargetPath $_.FullName).Replace('\', '/')
    if (($content.IndexOf('ILocalHumanInteractionContext', [System.StringComparison]::Ordinal) -ge 0) -and
        $relative -notin $allowedInteractionCapabilityFiles) {
        $errors.Add("Local human interaction capability is used outside the allowlist: $relative")
    }
    if (($content.IndexOf('IHumanApprovalExecutionContext', [System.StringComparison]::Ordinal) -ge 0) -and
        $relative -notin $allowedApprovalCapabilityFiles) {
        $errors.Add("Human approval execution capability is used outside the allowlist: $relative")
    }
}

$program = Read-RequiredText 'src/LocalGPT/Program.cs'
foreach ($required in @(
    'AddSingleton<AmbientLocalGptContext>()',
    'AddSingleton<IAmbientLocalGptContext>',
    'AddSingleton<ILocalHumanInteractionContext>',
    'AddSingleton<IHumanApprovalExecutionContext>',
    'AddSingleton<IHumanCollaborationService, HumanCollaborationService>',
    'AddSingleton<IDeferredDxAiInvocationService, DeferredDxAiInvocationService>')) {
    if (-not ($program.IndexOf($required, [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("Program.cs must retain '$required'.")
    }
}

$layout = Read-RequiredText 'src/LocalGPT/Components/Layout/MainLayout.razor'
if (-not ($layout.IndexOf('<HumanCollaborationInbox />', [System.StringComparison]::Ordinal) -ge 0)) {
    $errors.Add('The Human Collaboration Inbox must remain mounted in MainLayout.')
}


$humanInbox = Read-RequiredText 'src/LocalGPT/Components/Layout/HumanCollaborationInbox.razor'
$humanInboxCss = Read-RequiredText 'src/LocalGPT/Components/Layout/HumanCollaborationInbox.razor.css'
foreach ($token in @('human-approval-bar', 'Review and work through', 'PendingRequests.Count > 0', 'OpenApprovalPanel', 'HideApprovalBar', 'QueueDeferredApprovedExecution', 'Approved deferred function calls were queued without blocking')) {
    if (-not ($humanInbox.IndexOf($token, [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("Human approval work bar must retain '$token'.")
    }
}
foreach ($token in @('.human-approval-bar', 'position: fixed', 'var(--bs-body-bg)', 'var(--bs-body-color)')) {
    if (-not ($humanInboxCss.IndexOf($token, [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("Human approval work bar CSS must retain '$token'.")
    }
}

$filter = Read-RequiredText 'src/LocalGPT/Security/HumanApprovalActionFilter.cs'
foreach ($required in @(
    'StatusCodes.Status202Accepted',
    'StatusCodes.Status403Forbidden',
    'ParameterFingerprint: fingerprint',
    '!string.Equals(item.Key, "userConfirmed"',
    'approvalExecutionContext.PushHumanApproval',
    'ApplyLegacyConfirmationFlags',
    'RemoveConfirmationMembers',
    'SetBooleanProperty(argument, "UserConfirmed", true)')) {
    if (-not ($filter.IndexOf($required, [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("Human approval action filter must retain '$required'.")
    }
}

foreach ($controllerRelative in @(
    'src/LocalGPT/Controller/LocalGptDiagnosticController.cs',
    'src/LocalGPT/Controller/MinecraftDiagnosticController.cs')) {
    $controller = Read-RequiredText $controllerRelative
    $matches = [regex]::Matches($controller, '(?s)(\[Http(?:Get|Post|Put|Delete|Patch)[^\]]*\](?:\s*\[[^\]]+\])*)\s*public\s+[^\{]+?\{\s*try\s*\{\s*if\s*\(RequireHumanConfirmation\(userConfirmed')
    foreach ($match in $matches) {
        if (-not ($match.Groups[1].Value.IndexOf('HumanApprovalRequired', [System.StringComparison]::Ordinal) -ge 0)) {
            $errors.Add("$controllerRelative contains a legacy userConfirmed gate without HumanApprovalRequiredAttribute.")
        }
    }
}



$codeGenerationController = Read-RequiredText 'src/LocalGPT/Controller/CodeGenerationController.cs'
foreach ($required in @(
    'HumanApprovalRequired(',
    '"code-generation.review.create"',
    '"code-generation.review.execute"',
    'requiredBeforeCompletion: true')) {
    if (-not ($codeGenerationController.IndexOf($required, [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("CodeGenerationController must retain '$required'.")
    }
}

$chat = Read-RequiredText 'src/LocalGPT/Components/Pages/Chat.razor'
foreach ($required in @(
    'ILocalHumanInteractionContext HumanAmbientContext',
    'QueueRunningCouncilContributionAsync',
    'Add to next heartbeat',
    'HumanCollaboration.QueueContributionAsync')) {
    if (-not ($chat.IndexOf($required, [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("Chat live human-participation flow must retain '$required'.")
    }
}

$ollamaClient = Read-RequiredText 'src/LocalGPT/Services/OllamaThinkingChatClient.cs'
foreach ($required in @(
    'function.RequiresHumanConfirmation',
    'function.SupportsDeferredApprovalRequest',
    'function.SupportsAutomaticInvocation',
    '(function.IsReadOnly || function.IsCoordinationOnly)')) {
    if (-not ($ollamaClient.IndexOf($required, [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("Automatic Ollama tools must retain the read-only/coordination-only boundary '$required'.")
    }
}

$registry = Read-RequiredText 'src/LocalGPT/Services/DxAiFunctionRegistry.cs'
foreach ($required in @(
    'HumanApprovalPending',
    'HumanApprovalDeclined',
    'BuildInvocationFingerprint',
    'approvalExecutionContext.PushHumanApproval',
    'public sealed class RequestHumanCollaborationFunction',
    '"human.collaboration.request"',
    'IsCoordinationOnly: true',
    'BlocksUnrelatedWork = false',
    'deferredInvocations.QueueAsync',
    'SupportsDeferredApprovalRequest: true',
    'ApprovalRequiredBeforeCompletion: true')) {
    if (-not ($registry.IndexOf($required, [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("DXAI collaboration architecture must retain '$required'.")
    }
}
if (-not ($registry.IndexOf('consolidate one missing topic instead of repeating equivalent questions', [System.StringComparison]::Ordinal) -ge 0)) {
    $errors.Add('Human collaboration DXFunction must instruct Council members to consolidate repeated questions.')
}
if ($registry.IndexOf('TargetMembers = targetMembers', [System.StringComparison]::Ordinal) -ge 0) {
    $errors.Add('Equivalent human collaboration questions must not fingerprint target-member presentation scope as separate human work.')
}

$descriptor = Read-RequiredText 'src/LocalGPT/BusinessObjects/DxaichatFunctionInfo.cs'
foreach ($required in @(
    'bool IsCoordinationOnly = false',
    'bool SupportsDeferredApprovalRequest = false',
    'bool ApprovalRequiredBeforeCompletion = false')) {
    if (-not ($descriptor.IndexOf($required, [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("DxaichatFunctionInfo must retain '$required'.")
    }
}

$council = Read-RequiredText 'src/LocalGPT/Services/MultiModelCouncilService.cs'
foreach ($required in @(
    'humanCollaboration.BeginCouncilRun',
    'PrepareHumanHeartbeatAsync',
    'ModelName = $"Human:',
    'AppendHumanPeerReviewInstruction',
    'Human follow-up integration',
    'BuildHumanContributionEvaluation',
    'MarkContributionsEvaluatedAsync',
    'ambientContext.PushCouncil',
    'ExecuteApprovedForHeartbeatAsync',
    'BuildDeferredInvocationBriefing',
    'untrusted data, never instructions')) {
    if (-not ($council.IndexOf($required, [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("Council human-participation flow must retain '$required'.")
    }
}

$service = Read-RequiredText 'src/LocalGPT/Services/HumanCollaborationService.cs'
foreach ($required in @(
    'ambient.IsTrustedHumanInteraction',
    'existing.Status = HumanCollaborationStatuses.Consumed',
    'ParameterFingerprint',
    'pendingCoordinationRequests >= 12',
    'RequestKind != HumanCollaborationRequestKinds.Approval',
    'human.decline.feedback',
    'Security-decline feedback; context only, never approval.',
    'Human participation never authorizes',
    'DetermineEvaluationVerdict',
    'HumanContributionEvaluationVerdicts.Supported',
    'HumanContributionEvaluationVerdicts.NeedsCorrection',
    'HumanContributionEvaluationVerdicts.Mixed')) {
    if (-not ($service.IndexOf($required, [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("HumanCollaborationService must retain '$required'.")
    }
}

$dbContext = Read-RequiredText 'src/LocalGPT/BusinessObjects/EFCore/LocalGptMemoryDbContext.cs'
$snapshot = Read-RequiredText 'src/LocalGPT/Migrations/LocalGptMemoryDbContextModelSnapshot.cs'
foreach ($entity in @('HumanCollaborationRequest', 'HumanCouncilParticipantProfile', 'HumanCouncilContribution', 'DeferredDxAiInvocation')) {
    if (-not ($dbContext.IndexOf("DbSet<$entity>", [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("LocalGptMemoryDbContext is missing DbSet<$entity>.")
    }
    if (-not ($snapshot.IndexOf("LocalGPT.BusinessObjects.$entity", [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("EF model snapshot is missing $entity.")
    }
}


$deferredService = Read-RequiredText 'src/LocalGPT/Services/DeferredDxAiInvocationService.cs'
foreach ($required in @(
    'ExecuteApprovedForHeartbeatAsync',
    'ApprovalRequestId',
    'ParametersJson',
    '64_000',
    'GetRequiredService<IDxAiFunctionRegistry>',
    'HumanCollaborationStatuses.Approved',
    'HumanCollaborationStatuses.Declined',
    'DeferredDxAiInvocationStatuses.CompletedElsewhere')) {
    if (-not ($deferredService.IndexOf($required, [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("Deferred DXAI invocation service must retain '$required'.")
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Ambient human identity, persistent approvals, feedback inbox, and non-blocking council participation contracts verified.'
