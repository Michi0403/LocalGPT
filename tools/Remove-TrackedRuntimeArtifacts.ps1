[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$targets = @(
    'localgpt-memory.db',
    'LocalGPTWebviewWrapper/LocalGPT/FirstVersionlocalgpt-memory.db',
    'LocalGPTWebviewWrapper/LocalGPT/localgpt-memory-example-database.db',
    'LocalGPTWebviewWrapper/LocalGPT/wwwroot/js/devextreme-license.js'
)

foreach ($relativePath in $targets) {
    $fullPath = Join-Path $RepositoryRoot $relativePath
    if (Test-Path -LiteralPath $fullPath) {
        if ($PSCmdlet.ShouldProcess($fullPath, 'Remove tracked runtime/generated artifact')) {
            Remove-Item -LiteralPath $fullPath -Force
        }
    }
}

Write-Host 'Cleanup finished. Review git status, then stage the exact deletions you approve.'
