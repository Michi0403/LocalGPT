param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$errors = [System.Collections.Generic.List[string]]::new()

function Read-RepositoryText([string]$relativePath) {
    $path = Join-Path $RepositoryRoot $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $errors.Add("Missing workflow-contract file: $relativePath")
        return ''
    }
    return Get-Content -LiteralPath $path -Raw
}

$components = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Components'
Get-ChildItem -Path $components -Recurse -Filter '*.razor' -File | ForEach-Object {
    $content = Get-Content -LiteralPath $_.FullName -Raw
    if (($content.IndexOf('Name = NavigationUrls.ToggleSidebarName', [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("$($_.FullName): static navigation query constants must be qualified with NavigationUrlService, not the injected NavigationUrls instance.")
    }
}

foreach ($relative in @(
    'LocalGPTWebviewWrapper/LocalGPT/Components/Layout/Drawer.razor',
    'LocalGPTWebviewWrapper/LocalGPT/Components/Layout/MainLayout.razor',
    'LocalGPTWebviewWrapper/LocalGPT/Components/Pages/Index.razor')) {
    $content = Read-RepositoryText $relative
    if (-not ($content.IndexOf('Name = NavigationUrlService.ToggleSidebarName', [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("$relative must retain the type-qualified sidebar query constant.")
    }
}

$sharedDescriptor = 'LocalGPTWebviewWrapper/LocalGPT/BusinessObjects/DxaichatFunctionInfo.cs'
$descriptorContent = Read-RepositoryText $sharedDescriptor
if (-not ($descriptorContent.IndexOf('namespace LocalGPT.BusinessObjects;', [System.StringComparison]::Ordinal) -ge 0)) {
    $errors.Add("$sharedDescriptor must remain in the shared LocalGPT.BusinessObjects contract namespace.")
}
$obsoleteDescriptor = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Services/DxaichatFunctionCatalog.cs'
if (Test-Path -LiteralPath $obsoleteDescriptor) {
    $errors.Add('DxaichatFunctionInfo must not be reintroduced as a service-layer contract copy.')
}

$interfaceContent = Read-RepositoryText 'LocalGPTWebviewWrapper/LocalGPT/Interfaces/IDxAiFunctionServiceClient.cs'
if (-not ($interfaceContent.IndexOf('using LocalGPT.BusinessObjects;', [System.StringComparison]::Ordinal) -ge 0) -or
    -not ($interfaceContent.IndexOf('IReadOnlyList<DxaichatFunctionInfo> GetFunctions();', [System.StringComparison]::Ordinal) -ge 0)) {
    $errors.Add('IDxAiFunctionServiceClient must use the shared DxaichatFunctionInfo contract.')
}

$forbiddenSignatures = [ordered]@{
    'LocalGPTWebviewWrapper/LocalGPT/Services/BuildDebugInventoryService.cs' = @('Task<BuildDebugInventory?> CaptureAsync')
    'LocalGPTWebviewWrapper/LocalGPT/Services/ChatUploadWorkspaceService.cs' = @('Task<ChatUploadWorkspaceResult?> CreateWorkspaceAsync')
    'LocalGPTWebviewWrapper/LocalGPT/Services/CompositeChatClient.cs' = @('IAsyncEnumerable<ChatResponseUpdate>? GetStreamingResponseAsync')
    'LocalGPTWebviewWrapper/LocalGPT/Services/CouncilArtifactService.cs' = @('Task<CouncilArtifact?> CreateSolutionZipArtifactAsync')
    'LocalGPTWebviewWrapper/LocalGPT/Services/CouncilChatClient.cs' = @('Task<ChatResponse?> GetResponseAsync')
    'LocalGPTWebviewWrapper/LocalGPT/Services/LearnBaseKnowledgeImporterService.cs' = @('Task<LearnBaseImportResult?> ImportAsync')
    'LocalGPTWebviewWrapper/LocalGPT/Services/MinecraftModWorkspaceService.cs' = @('Task<MinecraftModWorkspace?> CreateWorkspaceAsync', 'Task<MinecraftModWorkspace?> CreateFabricWorkspaceAsync')
    'LocalGPTWebviewWrapper/LocalGPT/Services/OllamaThinkingChatClient.cs' = @('Task<ChatResponse?> GetResponseAsync', 'IAsyncEnumerable<ChatResponseUpdate>? GetStreamingResponseAsync')
    'LocalGPTWebviewWrapper/LocalGPT/Services/SqliteTableEditorService.cs' = @('Task<SqliteTableSnapshot?> GetTableAsync')
}
foreach ($entry in $forbiddenSignatures.GetEnumerator()) {
    $content = Read-RepositoryText $entry.Key
    foreach ($signature in $entry.Value) {
        if (($content.IndexOf($signature, [System.StringComparison]::Ordinal) -ge 0)) {
            $errors.Add("$($entry.Key): implementation nullability must match the non-null workflow contract; forbidden signature '$signature'.")
        }
    }
}

$minecraftService = Read-RepositoryText 'LocalGPTWebviewWrapper/LocalGPT/Services/MinecraftModWorkspaceService.cs'
foreach ($required in @(
    'public async Task<MinecraftModWorkspace> CreateWorkspaceAsync',
    'return await workspaceTask.ConfigureAwait(false);',
    'private LocalGptCatalogService.WorkspaceLayout CreateWorkspaceLayout',
    'private WorkspaceLayout CreateDatapackLayout')) {
    if (-not ($minecraftService.IndexOf($required, [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("Minecraft workspace workflow must retain '$required'.")
    }
}
if (($minecraftService.IndexOf('return string.Empty;', [System.StringComparison]::Ordinal) -ge 0)) {
    $errors.Add('Minecraft workspace path allocation must not convert failures into an empty path.')
}

$minecraftComponent = Read-RepositoryText 'LocalGPTWebviewWrapper/LocalGPT/Components/Pages/MinecraftModBuilder.razor'
foreach ($operation in @('CreateWorkspaceCoreAsync', 'RunCommandCoreAsync')) {
    $operationIndex = $minecraftComponent.IndexOf("async Task $operation", [System.StringComparison]::Ordinal)
    if ($operationIndex -lt 0) {
        $errors.Add("MinecraftModBuilder is missing $operation.")
        continue
    }
    $nextOperation = $minecraftComponent.IndexOf('async Task ', $operationIndex + 12, [System.StringComparison]::Ordinal)
    $length = if ($nextOperation -gt $operationIndex) { $nextOperation - $operationIndex } else { $minecraftComponent.Length - $operationIndex }
    $operationBody = $minecraftComponent.Substring($operationIndex, $length)
    if (-not ($operationBody.IndexOf('throw;', [System.StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("MinecraftModBuilder.$operation must propagate logged core failures to the shared UI safety wrapper.")
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Navigation, shared-contract, nullability, streaming, and workflow failure contracts verified.'
