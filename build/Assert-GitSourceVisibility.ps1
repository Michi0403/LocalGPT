Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "Git source visibility validation failed: $Message" }

$root = Split-Path -Parent $PSScriptRoot
$ignorePath = Join-Path $root '.gitignore'
$protectedPaths = @(
    'Directory.Build.targets',
    'Build-LocalDevelopment.ps1',
    'Build-Release.ps1',
    'build/Assert-LocalizationIntegrity.ps1',
    'build/Assert-GitSourceVisibility.ps1',
    'build/Assert-InteractiveServerRenderModes.ps1',
    'build/Assert-AsyncContinuationPolicy.ps1',
    'build/Assert-MethodDiagnostics.ps1',
    'build/Assert-ApplicationStaticPolicy.ps1',
    'build/Assert-TextServiceOwnership.ps1',
    'build/Assert-RuntimeValueOwnership.ps1',
    'build/Assert-SecurityRulePreservation.ps1',
    'build/runtime-value-ownership-baseline.json',
    'build/security-rules-final19.sha256',
    'build/Assert-IteratorExceptionPolicy.ps1',
    'build/Assert-SystemVariableInitialization.ps1',
    'build/method-diagnostics-baseline.json',
    'build/application-static-baseline.json',
    'build/text-service-ownership-baseline.json',
    'build/iterator-exception-baseline.json',
    'build/system-variable-initialization-baseline.json',
    'build/async-continuation-baseline.json',
    'build/Invoke-RepositoryValidation.ps1',
    'docs/RUNTIME_VALUE_OWNERSHIP.md',
    'CHANGELOG-v2.0.1-final20-runtime-value-ownership.md',
    'TEST-RESULTS-v2.0.1-final20-runtime-value-ownership.txt',
    'tests/localization_encoding_git_visibility_contracts.py',
    'tests/final17_system_variable_and_ps51_contracts.py',
    'tests/final18_ps51_baseline_contracts.py',
    'tests/final19_compile_regressions.py',
    'tests/final20_runtime_value_ownership.py',
    'LocalGPTWebviewWrapper/LocalGPT/Localization/en-US.json',
    'LocalGPTWebviewWrapper/LocalGPT/Localization/de-DE.json'
)
$requiredIgnoreRules = @(
    '!Directory.Build.targets',
    '!Build-LocalDevelopment.ps1',
    '!Build-Release.ps1',
    '!build/',
    '!build/*.ps1',
    '!build/*.json',
    '!build/*.sha256',
    '!tests/',
    '!tests/*.py',
    '!LocalGPTWebviewWrapper/LocalGPT/Localization/',
    '!LocalGPTWebviewWrapper/LocalGPT/Localization/*.json'
)

if (-not (Test-Path -LiteralPath $ignorePath -PathType Leaf)) { Fail "Missing $ignorePath" }
$ignoreLines = @([System.IO.File]::ReadAllLines($ignorePath) | ForEach-Object { $_.Trim() })
foreach ($rule in $requiredIgnoreRules) {
    if ($ignoreLines -cnotcontains $rule) { Fail "Required .gitignore protection rule is missing: $rule" }
}
foreach ($relative in $protectedPaths) {
    $nativeRelative = $relative.Replace('/', [System.IO.Path]::DirectorySeparatorChar.ToString())
    if (-not (Test-Path -LiteralPath (Join-Path $root $nativeRelative) -PathType Leaf)) { Fail "Protected source file is missing: $relative" }
}

$gitDirectory = Join-Path $root '.git'
$git = Get-Command git -ErrorAction SilentlyContinue
if ((Test-Path -LiteralPath $gitDirectory) -and $git) {
    foreach ($relative in $protectedPaths) {
        & $git.Source -C $root check-ignore --no-index --quiet -- $relative
        if ($LASTEXITCODE -eq 0) { Fail "Protected source file is ignored by Git: $relative" }
        if ($LASTEXITCODE -ne 1) { Fail "git check-ignore failed for $relative with exit code $LASTEXITCODE" }
    }
}
Write-Host "Git source visibility validation passed for $($protectedPaths.Count) LocalGPT files."
