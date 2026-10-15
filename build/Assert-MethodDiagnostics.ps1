Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# The maintained high-risk service set is inspected for method-owned diagnostics by the
# existing native architecture scanner. This does not silently skip when Python is absent.
& (Join-Path $PSScriptRoot 'Invoke-ArchitectureAudit.ps1') -Mode methods
Write-Host 'PowerShell service diagnostics architecture validation completed.'
