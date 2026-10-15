# Strict build-breaking Razor source-selection callback contract, with no Python prerequisite.
# Validate the current implementation and renderer ownership rather than pinning an old release.
param([string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$uiPath = Join-Path $RepositoryRoot 'src/LocalGPT/Components/Pages/Install.razor'
$codePath = Join-Path $RepositoryRoot 'src/LocalGPT/Components/Pages/Install.ProjectSource.razor.cs'
$editorPath = Join-Path $RepositoryRoot 'src/LocalGPT/Components/Pages/Install.RuntimePlugins.razor.cs'
$modelPath = Join-Path $RepositoryRoot 'src/LocalGPT/BusinessObjects/RuntimePluginSourceEditContext.cs'
$problems = New-Object 'System.Collections.Generic.List[string]'

function Read-ContractSource([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        $problems.Add(('{0}(1,1): error LGCB001: Missing implementation file.' -f $Path))
        return ''
    }
    return [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
}
function Assert-Contract([string]$Source, [string]$Path, [string]$Pattern, [string]$Reason, [int]$Line = 1) {
    if (-not [regex]::IsMatch($Source, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)) {
        $problems.Add(('{0}({1},1): error LGCB002: {2}' -f $Path, $Line, $Reason))
    }
}
function Assert-NotContract([string]$Source, [string]$Path, [string]$Pattern, [string]$Reason) {
    if ([regex]::IsMatch($Source, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)) {
        $problems.Add(('{0}(1,1): error LGCB003: {1}' -f $Path, $Reason))
    }
}

$ui = Read-ContractSource $uiPath
$code = Read-ContractSource $codePath
$editor = Read-ContractSource $editorPath
$model = Read-ContractSource $modelPath

Assert-Contract $ui $uiPath 'var\s+currentSourceContext\s*=\s*RuntimePluginSourceContext\s*;' 'Capture the current edit-context BusinessObject inside the Razor render branch.'
Assert-Contract $ui $uiPath '@key\s*=\s*"currentSourceContext"' 'Keep the keyed DevExpress project-source container.'

# Require explicit async Task-valued lambdas. The input value comes from the ComboBox,
# while the draft-bound context comes from the current render; neither can be inferred from a method group.
$selectors = @(
    @{ Caption = 'Project'; Property = 'ProjectId'; Handler = 'OnRuntimePluginSourceProjectChangedAsync' },
    @{ Caption = 'Revision'; Property = 'RevisionId'; Handler = 'OnRuntimePluginSourceRevisionChangedAsync' },
    @{ Caption = 'Curated source file'; Property = 'TrackedFileId'; Handler = 'OnRuntimePluginSourceFileChangedAsync' }
)
foreach ($selector in $selectors) {
    $caption = [regex]::Escape($selector.Caption)
    $itemPattern = '<DxFormLayoutItem\s+Caption="' + $caption + '"[^>]*>(.*?)</DxFormLayoutItem>'
    $itemMatch = [regex]::Match($ui, $itemPattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $itemMatch.Success) {
        $problems.Add(('{0}(1,1): error LGCB004: Source selector {1} must remain in its DevExpress form-layout item.' -f $uiPath, $selector.Caption))
        continue
    }
    $fragment = $itemMatch.Groups[1].Value
    $selectorLine = [regex]::Matches($ui.Substring(0, $itemMatch.Index), '\r\n|\r|\n').Count + 1
    Assert-Contract $fragment $uiPath '<DxComboBox\b' ('{0}: Keep the DevExpress ComboBox.' -f $selector.Caption) $selectorLine
    $handler = [regex]::Escape($selector.Handler)
    $callback = 'ValueChanged\s*=\s*"@\(\s*async\s*\(\s*Guid\s+(?<value>\w+)\s*\)\s*=>\s*await\s+' + $handler + '\s*\(\s*currentSourceContext\s*,\s*\k<value>\s*\)(?:\.ConfigureAwait\(true\))?\s*\)"'
    Assert-Contract $fragment $uiPath $callback ('{0}: Use async (Guid value) => await the Task-returning handler with the captured edit context and renderer-affine continuation.' -f $selector.Caption) $selectorLine
    $property = [regex]::Escape($selector.Property)
    Assert-Contract $fragment $uiPath ('Value\s*=\s*"@currentSourceContext\.' + $property + '"') ('{0}: Bind the selection to the active edit-context BusinessObject.' -f $selector.Caption) $selectorLine
    Assert-Contract $fragment $uiPath ('ValueExpression\s*=\s*"@\(\(\)\s*=>\s*currentSourceContext\.' + $property + '\)"') ('{0}: Preserve the typed ValueExpression for DevExpress.' -f $selector.Caption) $selectorLine
    Assert-Contract $fragment $uiPath 'Enabled\s*=\s*"@\(!RuntimePluginBusy\)"' ('{0}: Source selection must not change during publishing.' -f $selector.Caption) $selectorLine
}
Assert-NotContract $ui $uiPath 'ValueChanged\s*=\s*"@?OnRuntimePluginSource(Project|Revision)ChangedAsync"' 'Never bind an ambiguous method group to ValueChanged.'
Assert-NotContract $ui $uiPath '@bind-Value\s*=\s*"RuntimePluginSourceTrackedFileId"' 'Do not restore the contextless selection state.'

foreach ($name in @('PluginId', 'ProjectId', 'RevisionId', 'TrackedFileId', 'SelectionVersion')) {
    Assert-Contract $model $modelPath ('\b' + $name + '\s*\{\s*get;\s*set;\s*\}') ('The edit context requires {0}.' -f $name)
}
foreach ($handler in @('OnRuntimePluginSourceProjectChangedAsync', 'OnRuntimePluginSourceRevisionChangedAsync', 'OnRuntimePluginSourceFileChangedAsync')) {
    Assert-Contract $code $codePath ('private\s+async\s+Task\s+' + $handler + '\s*\(\s*RuntimePluginSourceEditContext\s+context\s*,\s*Guid\s+\w+\s*\)') ('{0} must return an awaited Task, not void.' -f $handler)
}
foreach ($contract in @(
    'ReferenceEquals\s*\(\s*context\s*,\s*RuntimePluginSourceContext\s*\)',
    'context\.PluginId\s*!=\s*RuntimePluginDraft\.Id',
    'context\.SelectionVersion\s*!=\s*selectionVersion',
    'RuntimePluginSourceContext\s*=\s*new\(\)',
    'await\s+InvokeAsync\s*\(',
    'StateHasChanged\s*\('
)) {
    Assert-Contract $code $codePath $contract ('The project-source handler must retain its render-context/async-stale-result contract: {0}' -f $contract)
}
Assert-Contract $code $codePath 'context\.TrackedFileId\s*=\s*trackedFileId\s*;' 'File selection must update the draft-bound tracked file.'
Assert-Contract $code $codePath 'context\.SelectionVersion\s*\+\+\s*;' 'File selection must invalidate previous async lookups.'
Assert-Contract $editor $editorPath 'RuntimePluginSourceContext\s*=\s*new\s+RuntimePluginSourceEditContext\s*\{\s*PluginId\s*=\s*RuntimePluginDraft\.Id\s*\}' 'A new plugin draft must receive a new owned edit context.'
Assert-Contract $editor $editorPath 'RuntimePluginSourceContext\.SelectionVersion\s*\+\+\s*;' 'Saving a draft must invalidate previous async callbacks.'
Assert-Contract $ui $uiPath '<DxHtmlEditor\s+@key="_runtimePluginEditorRevision"' 'Keep the existing DevExpress HTML editor.'

if ($problems.Count -ne 0) {
    foreach ($problem in $problems) { Write-Output $problem }
    throw ('Runtime plugin Razor callback audit failed ({0} finding(s)); fix the async callback/renderer contracts above.' -f $problems.Count)
}
Write-Output 'LocalGPT runtime-plugin Razor callback audit passed: typed awaited Guid closures, draft-bound render context, guarded asynchronous selections, DevExpress editor.'
