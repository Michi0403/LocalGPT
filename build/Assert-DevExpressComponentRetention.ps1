param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$contractPath = Join-Path $PSScriptRoot 'devexpress-component-retention.json'
if (-not (Test-Path -LiteralPath $contractPath -PathType Leaf)) {
    throw "DevExpress component retention contract is missing: $contractPath"
}

$contract = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
$componentsRoot = Join-Path $RepositoryRoot $contract.ComponentsRoot
if (-not (Test-Path -LiteralPath $componentsRoot -PathType Container)) {
    throw "DevExpress component retention root is missing: $componentsRoot"
}

$baseline = @{}
foreach ($property in $contract.Files.PSObject.Properties) {
    $baseline[$property.Name] = $property.Value
}

$nativePattern = '<\s*(?:button|input|select|option|textarea|datalist|InputText|InputTextArea|InputCheckbox|InputNumber|InputDate|InputSelect)\b'
$nativeDisclosurePattern = '<\s*(?:details|summary)\b'
$forbiddenNativeEditorPattern = '<\s*datalist\b|<\s*input\b[^>]*\btype\s*=\s*[\"'']?color\b'
$dxPattern = '<\s*Dx[A-Za-z0-9_]+' 
$violations = New-Object 'System.Collections.Generic.List[object]'
$targetsPath = Join-Path $RepositoryRoot 'Directory.Build.targets'
$maintenanceScriptPath = Join-Path $PSScriptRoot 'Assert-RazorMaintenanceArchitecture.ps1'
$maintenanceAuditPath = Join-Path $PSScriptRoot 'audit_razor_maintenance_contract.py'
$transientStateScriptPath = Join-Path $PSScriptRoot 'Assert-TransientUiStateOwnership.ps1'
$transientStateAuditPath = Join-Path $PSScriptRoot 'audit_transient_ui_state_ownership.py'
if (-not (Test-Path -LiteralPath $targetsPath -PathType Leaf)) {
    $violations.Add([pscustomobject]@{ File = 'Directory.Build.targets'; Line = 1; Code = 'PSDX0003'; Message = 'Directory.Build.targets is missing, so the Razor maintenance architecture guard cannot be enforced.'; Choices = 'Restore the repository build-target file and the Razor maintenance architecture target. Do not bypass the UI/layout/logging contract.' })
}
else {
    $targetsContent = [IO.File]::ReadAllText($targetsPath)
    foreach ($requiredToken in @('RazorMaintenanceArchitectureScript', 'Assert-RazorMaintenanceArchitecture.ps1', 'SkipRazorMaintenanceArchitectureGuard', 'TransientUiStateOwnershipScript', 'Assert-TransientUiStateOwnership.ps1', 'SkipTransientUiStateOwnershipGuard')) {
        if ($targetsContent.IndexOf($requiredToken, [StringComparison]::Ordinal) -lt 0) {
            $violations.Add([pscustomobject]@{ File = 'Directory.Build.targets'; Line = 1; Code = 'PSDX0003'; Message = "Required Razor maintenance architecture wiring '$requiredToken' was removed."; Choices = 'Restore the build-breaking Razor maintenance target. The correct repair for a violation is to fix the component architecture, not to detach the guard.' })
        }
    }
}
foreach ($requiredGuardFile in @($maintenanceScriptPath, $maintenanceAuditPath, $transientStateScriptPath, $transientStateAuditPath)) {
    if (-not (Test-Path -LiteralPath $requiredGuardFile -PathType Leaf)) {
        $relativeGuard = $requiredGuardFile.Substring($RepositoryRoot.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace('\','/')
        $violations.Add([pscustomobject]@{ File = $relativeGuard; Line = 1; Code = 'PSDX0003'; Message = 'Required Razor maintenance architecture guard file was removed.'; Choices = 'Restore the guard file and fix reported source violations instead of weakening or deleting the enforcement path.' })
    }
}
if (Test-Path -LiteralPath $maintenanceAuditPath -PathType Leaf) {
    $maintenanceAuditContent = [IO.File]::ReadAllText($maintenanceAuditPath)
    foreach ($requiredSemanticToken in @(
        'COMPONENT_BOUNDARY_CLASS',
        'DxFormLayout',
        'RAZORUI0006',
        'RAZORUI0007',
        'RAZORUI0008',
        'RAZORUI0009',
        'RAZORUI0010',
        'RAZORUI0011',
        'RAZORUI0012',
        'RAZORUI0013',
        'POPUP_VIEWPORT_CSS_MARKER',
        'LAYOUT_COMPATIBILITY_CSS_MARKER',
        'maintenance-only DevExpress wrapper shells remain box-neutral',
        'generic StackLayout wrappers and unexplained Grid-to-Stack nesting are rejected')) {
        if ($maintenanceAuditContent.IndexOf($requiredSemanticToken, [StringComparison]::Ordinal) -lt 0) {
            $violations.Add([pscustomobject]@{ File = 'build/audit_razor_maintenance_contract.py'; Line = 1; Code = 'PSDX0004'; Message = "Semantic Razor layout enforcement token '$requiredSemanticToken' was removed."; Choices = 'Restore the FormLayout-first semantic ownership rule, containment boundary, StackLayout anti-shortcut rule, form/editor ownership check, explicit FormLayout template-context check and Grid-to-Stack intent check. Change the architecture only through an explicit reviewed maintenance decision, not by weakening the guard to make a build pass.' })
        }
    }
}
$currentFiles = Get-ChildItem -LiteralPath $componentsRoot -Recurse -File -Filter '*.razor' | Sort-Object FullName
foreach ($file in $currentFiles) {
    $relative = ($file.FullName.Substring($RepositoryRoot.Length).TrimStart([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) -replace '\\', '/')
    $content = [IO.File]::ReadAllText($file.FullName)
    $codeIndex = $content.IndexOf('@code', [StringComparison]::Ordinal)
    $markup = if ($codeIndex -ge 0) { $content.Substring(0, $codeIndex) } else { $content }
    $dxCount = [regex]::Matches($markup, $dxPattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase).Count
    $nativeCount = [regex]::Matches($markup, $nativePattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase).Count
    $nativeDisclosureCount = [regex]::Matches($markup, $nativeDisclosurePattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase).Count
    $forbiddenNativeEditor = [regex]::Match($markup, $forbiddenNativeEditorPattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $entry = $baseline[$relative]
    $minimumDx = if ($null -eq $entry) { 0 } else { [int]$entry.MinimumDevExpressTags }
    $maximumNative = if ($null -eq $entry) { 0 } else { [int]$entry.MaximumNativeInteractiveTags }
    $maximumNativeDisclosures = if ($null -eq $entry -or $null -eq $entry.PSObject.Properties['MaximumNativeDisclosureTags']) { 0 } else { [int]$entry.MaximumNativeDisclosureTags }

    if ($forbiddenNativeEditor.Success) {
        $line = ([regex]::Matches($markup.Substring(0, $forbiddenNativeEditor.Index), "`n").Count + 1)
        $violations.Add([pscustomobject]@{ File = $relative; Line = $line; Code = 'PSDX0006'; Message = 'A native datalist or input type=color was introduced into maintained Razor UI.'; Choices = 'Use a searchable DevExpress ComboBox/ListBox/SearchBox workflow for suggestions and DxColorPalette (optionally hosted by DxDropDownBox) for color selection. These native substitutes are no longer accepted anywhere in maintained application UI.' })
    }

    if ($dxCount -lt $minimumDx) {
        $violations.Add([pscustomobject]@{ File = $relative; Line = 1; Code = 'PSDX0001'; Message = "DevExpress component count dropped from protected minimum $minimumDx to $dxCount."; Choices = "Restore the maintained DevExpress Blazor control(s), or explicitly revise the retention contract only when the product architecture intentionally changes. Native HTML is not an equivalent shortcut." })
    }
    if ($nativeCount -gt $maximumNative) {
        $firstNative = [regex]::Match($markup, $nativePattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
        $line = if ($firstNative.Success) { ([regex]::Matches($markup.Substring(0, $firstNative.Index), "`n").Count + 1) } else { 1 }
        $violations.Add([pscustomobject]@{ File = $relative; Line = $line; Code = 'PSDX0002'; Message = "Native/legacy interactive Razor tag count increased from protected maximum $maximumNative to $nativeCount."; Choices = "Use the corresponding DevExpress Blazor control and preserve its existing render/circuit semantics. Prefer searchable DxComboBox/DxListBox/DxSearchBox workflows over select/datalist, DxColorPalette integration over input type=color, and DxButton over visual action buttons. If a browser-owned primitive truly must remain native, keep the exception narrow and documented." })
    }
    if ($nativeDisclosureCount -gt $maximumNativeDisclosures) {
        $firstDisclosure = [regex]::Match($markup, $nativeDisclosurePattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
        $line = if ($firstDisclosure.Success) { ([regex]::Matches($markup.Substring(0, $firstDisclosure.Index), "`n").Count + 1) } else { 1 }
        $violations.Add([pscustomobject]@{ File = $relative; Line = $line; Code = 'PSDX0005'; Message = "Native disclosure tag count increased from protected maximum $maximumNativeDisclosures to $nativeDisclosureCount."; Choices = "Use DxAccordion for collapsible content, or a DevExpress menu/flyout when the interaction is an action menu. Existing legacy disclosures may be migrated downward over time, but new native details/summary UI must not increase the debt." })
    }
}

if ($violations.Count -gt 0) {
    foreach ($violation in $violations) {
        Write-Output ("{0}({1},1): error {2}: {3}" -f $violation.File, $violation.Line, $violation.Code, $violation.Message)
        Write-Output ("  Architectural choices: {0}" -f $violation.Choices)
    }
    throw "DevExpress component retention validation failed with $($violations.Count) violation(s)."
}

Write-Host "DevExpress component retention validation passed: protected DevExpress controls were not removed, native editor/action controls did not increase, datalist/native color-editor substitutions are absent, and legacy native disclosure debt did not grow."
