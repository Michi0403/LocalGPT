param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$errors = [System.Collections.Generic.List[string]]::new()
function Need([string]$path, [string[]]$tokens) {
    $full = Join-Path $RepositoryRoot $path
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { $errors.Add("Missing architecture task file: $path"); return }
    $text = Get-Content -LiteralPath $full -Raw
    foreach ($token in $tokens) { if (-not $text.Contains($token, [StringComparison]::Ordinal)) { $errors.Add("$path must retain '$token'.") } }
}
Need 'CHANGELOG-v0.1.4-ef-snapshot-runtime-debug.md' @('## Closed in this iteration', '## Open tasks carried forward', '- [ ] Re-run the owner Debug startup against a disposable or backed-up database')
Need 'docs/OPEN_TASKS.md' @('Current unresolved work:', 'Copy every unresolved item into the next current changelog')
Need 'docs/DATABASE_FIRST_PROJECT_ARCHITECTURE.md' @('LocalGptProjectRevision', 'LocalGptProjectRequirementLink', 'Function availability is not a reason to call it')
Need 'docs/THEME_RUNTIME_ARCHITECTURE.md' @('DxResourceManager.RegisterTheme', 'IThemeChangeService', 'JavaScript boundary', 'Blazing Berry')
Need 'docs/EF_MIGRATION_SNAPSHOT_ARCHITECTURE.md' @('properties first', 'relationships', 'Dictionary<string, object>', 'Assert-EfSnapshotArchitecture.ps1')
Need 'build/Assert-EfSnapshotArchitecture.ps1' @('EF migration snapshot ordering and project navigation contracts verified.', 'WithMany("Artifacts")')
Need 'LocalGPTWebviewWrapper/LocalGPT/Services/EfChatMemoryService.cs' @('IChatMemoryMessageMapper messageMapper')
$memory = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Services/EfChatMemoryService.cs') -Raw
if ($memory.Contains('DevExpressChatService', [StringComparison]::Ordinal)) { $errors.Add('EfChatMemoryService must not depend on DevExpressChatService; that recreates the function-registry memory cycle.') }
Need 'LocalGPTWebviewWrapper/LocalGPT/Program.cs' @('AddScoped<IChatMemoryMessageMapper, ChatMemoryMessageMapper>()')
Need 'LocalGPTWebviewWrapper/LocalGPT/Migrations/20260726010000_AddDatabaseFirstProjectArchitecture.cs' @('CouncilModelPresets', 'SqliteEditorFieldOverrides', 'CouncilKnowledgeUserRatings')
Need 'LocalGPTWebviewWrapper/LocalGPT/Migrations/LocalGptMemoryDbContextModelSnapshot.cs' @('LocalGptProjectRevision', 'CouncilModelPreset', 'SqliteEditorFieldOverride', 'CouncilKnowledgeUserRating')

Need 'LocalGPTWebviewWrapper/LocalGPT/Components/Pages/Chat.razor' @('private async Task RunUiActionAsync(Func<Task> action, string operation)', 'SaveModelPresetAsync', 'ArchiveModelPresetAsync')
Need 'LocalGPTWebviewWrapper/LocalGPT/Services/MultiModelCouncilService.cs' @('var continuedConversation = await LoadContinuationConversationAsync(', 'result.ContinuedFromConversationId = continuedConversation.Id', '.Where(model => !string.IsNullOrWhiteSpace(model.Name))')
Need 'LocalGPTWebviewWrapper/LocalGPT/Services/SafeTextDocumentService.cs' @('StartsWith(new byte[] { 0xEF, 0xBB, 0xBF })', 'StartsWith(new byte[] { 0xFF, 0xFE })', 'StartsWith(new byte[] { 0xFE, 0xFF })')
Need 'LocalGPTWebviewWrapper/LocalGPT/Services/CouncilRuntimeService.cs' @('public ChatResponseUpdate CreateUpdate(', 'public MinecraftDatapackVersionInfo MinecraftDatapackVersionInfoResolve(', 'public EngineeringBenchmarkLaneResult NotRunLane(', 'public LocalGptCatalogService.GeneratedArchetypePage ArchetypePage(')
Need 'LocalGPTWebviewWrapper/LocalGPT/Services/ThemeService.cs' @('public ThemeService(ILogger<ThemeService> logger)', 'DxThemes.BootstrapExternal.Clone', 'properties.UseBootstrapStyles = true;', 'CreateClassic("blazing-berry"')
$safeText = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Services/SafeTextDocumentService.cs') -Raw
if ($safeText.Contains('StartsWith([0x', [StringComparison]::Ordinal)) { $errors.Add('Safe text BOM checks must use explicitly typed byte arrays; target-typed byte collection expressions previously failed compilation.') }
$runtime = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Services/CouncilRuntimeService.cs') -Raw
foreach ($forbidden in @('ChatResponseUpdate? CreateUpdate(', 'EngineeringBenchmarkLaneResult? NotRunLane(', 'GeneratedArchetypePage? ArchetypePage(', 'MinecraftDatapackVersionInfo? MinecraftDatapackVersionInfoKnown(')) {
    if ($runtime.Contains($forbidden, [StringComparison]::Ordinal)) { $errors.Add("CouncilRuntimeService must not restore nullable orchestration contract '$forbidden'.") }
}
if ($errors.Count) { $errors | ForEach-Object { Write-Error $_ }; exit 1 }
Write-Host 'Database-first architecture and carried-forward task contracts verified.'
