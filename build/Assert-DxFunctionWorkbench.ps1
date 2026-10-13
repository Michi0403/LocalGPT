param([string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$audit = Join-Path $PSScriptRoot 'audit_dx_function_workbench_5_6_3.py'
if (-not (Test-Path -LiteralPath $audit -PathType Leaf)) {
    throw "DX function workbench audit is missing: $audit"
}
$pythonCommand = $null
foreach ($candidate in @('python', 'python3')) {
    $resolved = Get-Command $candidate -ErrorAction SilentlyContinue
    if ($null -ne $resolved) { $pythonCommand = $resolved; break }
}
if ($null -ne $pythonCommand) {
    & $pythonCommand.Source $audit --root $RepositoryRoot
    if ($LASTEXITCODE -ne 0) { throw "DX function workbench audit failed (exit $LASTEXITCODE). Repair the reported contract, not the guard." }
}
else {
    $launcher = Get-Command 'py' -ErrorAction SilentlyContinue
    if ($null -eq $launcher) { throw 'Python 3 is required for the DX function workbench audit.' }
    & $launcher.Source -3 $audit --root $RepositoryRoot
    if ($LASTEXITCODE -ne 0) { throw "DX function workbench audit failed (exit $LASTEXITCODE). Repair the reported contract, not the guard." }
}
