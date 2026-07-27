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
    'build/Invoke-RepositoryValidation.ps1',
    'tests/localization_encoding_git_visibility_contracts.py',
    'LocalGPTWebviewWrapper/LocalGPT/Localization/en-US.json',
    'LocalGPTWebviewWrapper/LocalGPT/Localization/de-DE.json'
)
$requiredIgnoreRules = @(
    '!Directory.Build.targets',
    '!Build-LocalDevelopment.ps1',
    '!Build-Release.ps1',
    '!build/',
    '!build/*.ps1',
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
