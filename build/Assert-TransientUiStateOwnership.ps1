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
        throw "Transient UI-state ownership guard could not determine the product under $RepositoryRoot."
    }
}

$audit = Join-Path $PSScriptRoot 'audit_transient_ui_state_ownership.py'
if (-not (Test-Path -LiteralPath $audit -PathType Leaf)) {
    throw "Transient UI-state ownership audit is missing: $audit"
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
        throw "Transient UI-state ownership validation failed with exit code $LASTEXITCODE. Preserve vendor/browser ownership of live caret, selection, document, drag and slider state; commit durable state at explicit boundaries instead of feeding intermediate interaction state through InteractiveServer rerenders."
    }
}
else {
    $launcher = Get-Command 'py' -ErrorAction SilentlyContinue
    if ($null -eq $launcher) {
        throw 'Python is required for the non-bypassable transient UI-state ownership audit.'
    }
    & $launcher.Source -3 $audit --root $RepositoryRoot --product $Product
    if ($LASTEXITCODE -ne 0) {
        throw "Transient UI-state ownership validation failed with exit code $LASTEXITCODE. Preserve vendor/browser ownership of live caret, selection, document, drag and slider state; commit durable state at explicit boundaries instead of feeding intermediate interaction state through InteractiveServer rerenders."
    }
}
