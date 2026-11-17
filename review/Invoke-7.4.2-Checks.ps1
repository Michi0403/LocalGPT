# LocalGPT 7.4.2 review-only smoke gate. Does not edit Git or source files.
# Run after applying review/7.4.1-to-7.4.2.diff to a LocalGPT checkout.
param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $root 'src/LocalGPT/LocalGPT.csproj'
$contract = Join-Path $root 'src/LocalGPT/Interfaces/ICodeGenerationWorkflowService.cs'
$source = Join-Path $root 'src/LocalGPT/Services/CodeGenerationWorkflowService.DraftReadiness.cs'
$council = Join-Path $root 'src/LocalGPT/Services/MultiModelCouncilService.ConfiguredWorkflowExecution.cs'

foreach ($file in @($project, $contract, $source, $council)) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        throw "Missing 7.4.2 source file: $file. Apply the patch to the repo root first."
    }
}

[xml]$projectXml = Get-Content -LiteralPath $project -Raw
if ([string]$projectXml.Project.PropertyGroup.Version -ne '7.4.2') {
    throw 'LocalGPT.csproj does not declare version 7.4.2.'
}

$interfaceText = Get-Content -LiteralPath $contract -Raw
$serviceText = Get-Content -LiteralPath $source -Raw
$councilText = Get-Content -LiteralPath $council -Raw
$signature = 'GetCouncilDraftReadinessAsync'
if ($interfaceText.IndexOf($signature, [StringComparison]::Ordinal) -lt 0 -or
    $serviceText.IndexOf($signature, [StringComparison]::Ordinal) -lt 0 -or
    $councilText.IndexOf($signature, [StringComparison]::Ordinal) -lt 0) {
    throw 'The Council-to-CodeGeneration DI handoff is incomplete.'
}
if ($serviceText -notmatch 'AsNoTracking\(\)' -or
    $serviceText -notmatch 'files\.Count\s*==\s*manifest\.Count' -or
    $serviceText -notmatch 'logger\.LogWarning\(' -or
    $serviceText -match 'static\s+(?:bool|int|Task|void)\s+') {
    throw 'The persisted draft readiness check violates the expected source contract.'
}
if ($councilText -notmatch 'completionReportedThisIteration\s*=\s*draft\.ReadyToSubmit' -or
    $councilText -notmatch 'if\s*\(draft\.Exists\)' -or
    $councilText -notmatch 'codegen\.review\.create') {
    throw 'Council completion must be gated by the persisted source-draft state.'
}

Write-Host 'PASS: 7.4.2 version, direct DI handoff and persisted draft gating source contracts.'
if ($SkipBuild) {
    Write-Host 'SKIPPED: .NET 10 build (-SkipBuild selected).'
    return
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET 10 SDK not found. Install/configure the required SDK and run this check again.'
}
Push-Location $root
try {
    & dotnet build $project -c Debug
    if ($LASTEXITCODE -ne 0) {
        throw "LocalGPT Debug build / architecture gates failed with exit code $LASTEXITCODE."
    }
    Write-Host 'PASS: .NET Debug build and repository MSBuild architecture targets.'
}
finally {
    Pop-Location
}
