param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$errors = [System.Collections.Generic.List[string]]::new()
function Need([string]$path, [string[]]$tokens) {
    $full = Join-Path $RepositoryRoot $path
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { $errors.Add("Missing architecture task file: $path"); return }
    $text = Get-Content -LiteralPath $full -Raw
    foreach ($token in $tokens) { if (-not $text.Contains($token, [StringComparison]::Ordinal)) { $errors.Add("$path must retain '$token'.") } }
}
Need 'CHANGELOG-v0.1.4-database-first-debug.md' @('## Closed in this iteration', '## Open tasks carried forward', '- [ ] Run the licensed Windows/DevExpress Debug and Release builds')
Need 'docs/OPEN_TASKS.md' @('Current unresolved work:', 'Copy every unresolved item into the next current changelog')
Need 'docs/DATABASE_FIRST_PROJECT_ARCHITECTURE.md' @('LocalGptProjectRevision', 'LocalGptProjectRequirementLink', 'Function availability is not a reason to call it')
Need 'LocalGPTWebviewWrapper/LocalGPT/Services/EfChatMemoryService.cs' @('IChatMemoryMessageMapper messageMapper')
$memory = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Services/EfChatMemoryService.cs') -Raw
if ($memory.Contains('DevExpressChatService', [StringComparison]::Ordinal)) { $errors.Add('EfChatMemoryService must not depend on DevExpressChatService; that recreates the function-registry memory cycle.') }
Need 'LocalGPTWebviewWrapper/LocalGPT/Program.cs' @('AddScoped<IChatMemoryMessageMapper, ChatMemoryMessageMapper>()')
Need 'LocalGPTWebviewWrapper/LocalGPT/Migrations/20260726010000_AddDatabaseFirstProjectArchitecture.cs' @('CouncilModelPresets', 'SqliteEditorFieldOverrides', 'CouncilKnowledgeUserRatings')
Need 'LocalGPTWebviewWrapper/LocalGPT/Migrations/LocalGptMemoryDbContextModelSnapshot.cs' @('LocalGptProjectRevision', 'CouncilModelPreset', 'SqliteEditorFieldOverride', 'CouncilKnowledgeUserRating')
if ($errors.Count) { $errors | ForEach-Object { Write-Error $_ }; exit 1 }
Write-Host 'Database-first architecture and carried-forward task contracts verified.'
