Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Invoke-ArchitectureAudit.ps1') -Mode methods

$serviceAudit = Join-Path $PSScriptRoot 'audit_service_resilience.py'
$repoRoot = Split-Path -Parent $PSScriptRoot
$python = Get-Command python -ErrorAction SilentlyContinue
if ($null -eq $python) { $python = Get-Command python3 -ErrorAction SilentlyContinue }
if ($python) {
    & $python.Source $serviceAudit --root $repoRoot --product localgpt
    if ($LASTEXITCODE -ne 0) { throw 'Service resilience audit failed.' }
} else {
    $launcher = Get-Command py -ErrorAction SilentlyContinue
    if ($launcher) {
        & $launcher.Source -3 $serviceAudit --root $repoRoot --product localgpt
        if ($LASTEXITCODE -ne 0) { throw 'Service resilience audit failed.' }
    } else {
        Write-Warning 'Python was not found; the broad service resilience audit could not run. The maintained PowerShell architecture fallback still ran above.'
    }
}
