param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [ValidateSet('localgpt','publisherstudio')]
    [string]$Product
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Product)) {
    if (Test-Path -LiteralPath (Join-Path $RepositoryRoot 'src/LocalGPT') -PathType Container) {
        $Product = 'localgpt'
    }
    elseif (Test-Path -LiteralPath (Join-Path $RepositoryRoot 'src/PublisherStudio.Web') -PathType Container) {
        $Product = 'publisherstudio'
    }
    else {
        throw "Razor maintenance architecture could not determine the product under $RepositoryRoot."
    }
}

$audit = Join-Path $PSScriptRoot 'audit_razor_maintenance_contract.py'
if (-not (Test-Path -LiteralPath $audit -PathType Leaf)) {
    throw "Razor maintenance architecture audit is missing: $audit"
}

$pythonCommand = $null
foreach ($candidate in @('python', 'python3')) {
    $resolved = Get-Command $candidate -ErrorAction SilentlyContinue
    if ($null -ne $resolved) {
        $pythonCommand = $resolved
        break
    }
}

if ($null -ne $pythonCommand) {
    & $pythonCommand.Source $audit --root $RepositoryRoot --product $Product
    if ($LASTEXITCODE -ne 0) {
        throw "Razor maintenance architecture validation failed with exit code $LASTEXITCODE. Review every file/line and architectural choice emitted above."
    }
}
else {
    $launcher = Get-Command 'py' -ErrorAction SilentlyContinue
    if ($null -eq $launcher) {
        throw 'Python is required for the non-bypassable Razor maintenance architecture audit. Install/restore the repository Python runtime instead of skipping the UI/layout/logging contract.'
    }
    & $launcher.Source -3 $audit --root $RepositoryRoot --product $Product
    if ($LASTEXITCODE -ne 0) {
        throw "Razor maintenance architecture validation failed with exit code $LASTEXITCODE. Review every file/line and architectural choice emitted above."
    }
}
