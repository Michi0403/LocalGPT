$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$required = @(
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.RunOrchestration.cs'; Tokens = @('LoadContinuationConversationAsync','ContinuedFromConversationId') }
    @{ Path = 'src/LocalGPT/Services/CouncilRuntimeService.PromptAndTextRuntime.cs'; Tokens = @('CreateUpdate') }
    @{ Path = 'src/LocalGPT/Services/CouncilRuntimeService.cs'; Tokens = @('NotRunLane') }
    @{ Path = 'src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs'; Tokens = @('RunUiActionAsync') }
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
if ($findings.Count -gt 0) { foreach ($finding in $findings) { Write-Error $finding }; throw ('Source contract audit failed: ' + $findings.Count + ' finding(s).') }
Write-Host 'Maintained PowerShell source contract audit passed.'
