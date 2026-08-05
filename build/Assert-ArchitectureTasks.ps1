param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$errors = [System.Collections.Generic.List[string]]::new()
function Need([string]$path, [string[]]$tokens) {
    $full = Join-Path $RepositoryRoot $path
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { $errors.Add("Missing architecture task file: $path"); return }
    $text = Get-Content -LiteralPath $full -Raw
    foreach ($token in $tokens) { if (-not ($text.IndexOf($token, [StringComparison]::Ordinal) -ge 0)) { $errors.Add("$path must retain '$token'.") } }
}
Need 'docs/engineering/maintenance-status.md' @('Build and runtime gates', 'Every unresolved item moves into the next current changelog')
Need 'docs/architecture/project-data.md' @('LocalGptProjectRevision', 'LocalGptProjectRequirementLink', 'Function availability is not a reason to call it')
Need 'docs/architecture/frontend-and-themes.md' @('DxResourceManager.RegisterTheme', 'IThemeChangeService', 'JavaScript boundary', 'Blazing Berry')
Need 'docs/architecture/project-data.md' @('properties first', 'relationships', 'Dictionary<string, object>', 'Assert-EfSnapshotArchitecture.ps1')
Need 'docs/architecture/project-data.md' @('SQLite online backup', 'complete signature', 'Refuse partially applied ambiguous schemas')
Need 'build/Assert-DatabaseMigrationBootstrap.ps1' @('Database migration bootstrap and compatibility-service contracts verified.')
Need 'docs/architecture/system-overview.md' @('Mutable application state must not be hidden in static fields', 'Hosted services', 'Cancellation')
Need 'build/Assert-ServiceArchitecture.ps1' @('Service lifecycle, static-state, and asynchronous supervision contracts verified.', 'Mutable collection state must not be stored in static service fields')
Need 'LocalGPTWebviewWrapper/LocalGPT/Services/LocalGptCatalogService.cs' @('using System.Collections.Frozen;', 'FrozenSet<string> ExcludedDirectoryNames', 'FrozenSet<string> SourceExtensions')
Need 'LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/DatabaseMigrationCompatibilityService.cs' @('public sealed class DatabaseMigrationCompatibilityService', 'signature.Requirements.Length', 'IServiceActivityService serviceActivity')
Need 'build/Assert-EfSnapshotArchitecture.ps1' @('EF migration snapshot ordering and project navigation contracts verified.', 'WithMany("Artifacts")')
Need 'LocalGPTWebviewWrapper/LocalGPT/Services/EfChatMemoryService.cs' @('IChatMemoryMessageMapper messageMapper')
$memory = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Services/EfChatMemoryService.cs') -Raw
if (($memory.IndexOf('DevExpressChatService', [StringComparison]::Ordinal) -ge 0)) { $errors.Add('EfChatMemoryService must not depend on DevExpressChatService; that recreates the function-registry memory cycle.') }
Need 'LocalGPTWebviewWrapper/LocalGPT/Program.cs' @('AddScoped<IChatMemoryMessageMapper, ChatMemoryMessageMapper>()')
Need 'LocalGPTWebviewWrapper/LocalGPT/Migrations/20260726010000_AddDatabaseFirstProjectArchitecture.cs' @('CouncilModelPresets', 'SqliteEditorFieldOverrides', 'CouncilKnowledgeUserRatings')
Need 'LocalGPTWebviewWrapper/LocalGPT/Migrations/LocalGptMemoryDbContextModelSnapshot.cs' @('LocalGptProjectRevision', 'CouncilModelPreset', 'SqliteEditorFieldOverride', 'CouncilKnowledgeUserRating')

Need 'LocalGPTWebviewWrapper/LocalGPT/Components/Pages/Chat.razor' @('private async Task RunUiActionAsync(Func<Task> action, string operation)', 'SaveModelPresetAsync', 'ArchiveModelPresetAsync')
Need 'LocalGPTWebviewWrapper/LocalGPT/Services/MultiModelCouncilService.cs' @('var continuedConversation = await LoadContinuationConversationAsync(', 'result.ContinuedFromConversationId = continuedConversation.Id', '.Where(model => !string.IsNullOrWhiteSpace(model.Name))')
Need 'LocalGPTWebviewWrapper/LocalGPT/Services/SafeTextDocumentService.cs' @('StartsWith(new byte[] { 0xEF, 0xBB, 0xBF })', 'StartsWith(new byte[] { 0xFF, 0xFE })', 'StartsWith(new byte[] { 0xFE, 0xFF })')
Need 'LocalGPTWebviewWrapper/LocalGPT/Services/CouncilRuntimeService.cs' @('public ChatResponseUpdate CreateUpdate(', 'public MinecraftDatapackVersionInfo MinecraftDatapackVersionInfoResolve(', 'public EngineeringBenchmarkLaneResult NotRunLane(', 'public GeneratedArchetypePage ArchetypePage(')
Need 'LocalGPTWebviewWrapper/LocalGPT/Services/ThemeService.cs' @('public ThemeService(', 'IServiceActivityService serviceActivity', 'DxThemes.BootstrapExternal.Clone', 'properties.UseBootstrapStyles = true;', 'CreateClassic("blazing-berry"')
$safeText = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Services/SafeTextDocumentService.cs') -Raw
if (($safeText.IndexOf('StartsWith([0x', [StringComparison]::Ordinal) -ge 0)) { $errors.Add('Safe text BOM checks must use explicitly typed byte arrays; target-typed byte collection expressions previously failed compilation.') }
$runtime = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Services/CouncilRuntimeService.cs') -Raw
foreach ($forbidden in @('ChatResponseUpdate? CreateUpdate(', 'EngineeringBenchmarkLaneResult? NotRunLane(', 'GeneratedArchetypePage? ArchetypePage(', 'MinecraftDatapackVersionInfo? MinecraftDatapackVersionInfoKnown(')) {
    if (($runtime.IndexOf($forbidden, [StringComparison]::Ordinal) -ge 0)) { $errors.Add("CouncilRuntimeService must not restore nullable orchestration contract '$forbidden'.") }
}
if ($errors.Count) { $errors | ForEach-Object { Write-Error $_ }; exit 1 }
Write-Host 'Database-first architecture and carried-forward task contracts verified.'
