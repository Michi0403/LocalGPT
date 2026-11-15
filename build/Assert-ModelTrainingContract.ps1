param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [IO.Path]::GetFullPath($RepositoryRoot)
$findings = New-Object 'System.Collections.Generic.List[string]'

function Read-SourceText([string]$relative) {
    $file = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        $findings.Add(('{0}(1,1): error LGMT7001: Missing model training component. Restore the file or move its responsibility to the owning service; the identity training integration keeps the packaged trainer, its service and its approval-gated DX functions together.' -f $relative))
        return $null
    }
    return [IO.File]::ReadAllText($file)
}

function Assert-SourceTokens([string]$relative, [string[]]$required) {
    $text = Read-SourceText $relative
    if ($null -eq $text) { return }
    foreach ($token in $required) {
        if ($text.IndexOf($token, [StringComparison]::Ordinal) -lt 0) {
            $findings.Add(('{0}(1,1): error LGMT7002: Missing model training contract: {1}' -f $relative, $token))
        }
    }
}

# 1. The packaged trainer and its project Content entry.
Assert-SourceTokens 'src/LocalGPT/Runtime/Python/localgpt_lora_trainer.py' @('def load_config')
Assert-SourceTokens 'src/LocalGPT/LocalGPT.csproj' @('<Content Include="Runtime\Python\localgpt_lora_trainer.py"')

# 2. Model training sources hold no static members and no constants: every tunable value is a database runtime value.
$sourceRoot = Join-Path $root 'src/LocalGPT'
$trainingFiles = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*ModelTraining*.cs' |
    Where-Object { $_.FullName -notmatch '[\\/](?:bin|obj)[\\/]' })
if ($trainingFiles.Count -eq 0) {
    $findings.Add('src/LocalGPT(1,1): error LGMT7001: No ModelTraining*.cs source files were found. Restore the model training service, interface, functions and seed partial.')
}
foreach ($trainingFile in $trainingFiles) {
    $relative = $trainingFile.FullName.Substring($root.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace('\', '/')
    $lines = [IO.File]::ReadAllLines($trainingFile.FullName)
    for ($index = 0; $index -lt $lines.Length; $index++) {
        $trimmed = $lines[$index].TrimStart()
        if ($trimmed.StartsWith('//', [StringComparison]::Ordinal)) { continue }
        if ($lines[$index] -match '\bstatic\s') {
            $findings.Add(('{0}({1},1): error LGMT7003: static is not allowed in model training sources. Keep the behavior in the DI-owned ModelTrainingService instance.' -f $relative, ($index + 1)))
        }
        if ($lines[$index] -match '\bconst\s') {
            $findings.Add(('{0}({1},1): error LGMT7004: const is not allowed in model training sources. Move the value to a seeded runtime value in LocalGptRuntimePolicySeedDataService.ModelTraining.cs and read it through ILocalGptRuntimePolicyDataService.' -f $relative, ($index + 1)))
        }
    }
}

# 3. Consequential functions stay behind the human approval system; the service is registered.
foreach ($function in @('ModelTrainingIdentityPrepareFunction', 'ModelTrainingRunFunction')) {
    Assert-SourceTokens ('src/LocalGPT/Services/{0}.cs' -f $function) @(
        'RequiresHumanConfirmation: true', 'SupportsDeferredApprovalRequest: true',
        'ApprovalRequiredBeforeCompletion: true', 'SupportsAutomaticInvocation: false', 'IsReadOnly: false'
    )
}
Assert-SourceTokens 'src/LocalGPT/Services/ModelTrainingProbeFunction.cs' @('IsReadOnly: true', 'RequiresHumanConfirmation: false')
Assert-SourceTokens 'src/LocalGPT/Program.ServiceRegistration.cs' @('AddScoped<IModelTrainingService, ModelTrainingService>()')
Assert-SourceTokens 'src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs' @('CreateModelTrainingValues()', 'CreateModelTrainingCollections()')
Assert-SourceTokens 'Directory.Build.targets' @('AssertLocalGptModelTrainingContract', 'Assert-ModelTrainingContract.ps1')

if ($findings.Count -gt 0) {
    foreach ($finding in $findings) { Write-Output $finding }
    throw ('Model training contract failed with {0} finding(s).' -f $findings.Count)
}
Write-Output 'Model training: packaged trainer, runtime-value ownership, approval-gated functions and service registration validated.'
