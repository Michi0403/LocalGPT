param(
    [Parameter(Mandatory)][string[]]$ToolManifest,
    [ValidateSet('None', 'Update', 'UpdateWithSudo')][string]$DotNetWorkloads = 'None'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$dotnet = (Get-Command dotnet -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
$isWindowsHost = [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Windows)

if ($DotNetWorkloads -eq 'UpdateWithSudo' -and $isWindowsHost) {
    throw 'UpdateWithSudo is for Linux and macOS. On Windows, use -DotNetWorkloads Update from an elevated PowerShell session if required.'
}

Push-Location -LiteralPath $repositoryRoot
try {
    if ($DotNetWorkloads -ne 'None') {
        Write-Host '[Build stage: preflight] Updating workloads for the repository .NET SDK.' -ForegroundColor Cyan
        if ($DotNetWorkloads -eq 'UpdateWithSudo') {
            $sudo = (Get-Command sudo -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
            & $sudo -- $dotnet workload update
        }
        else {
            & $dotnet workload update
        }
        if ($LASTEXITCODE -ne 0) {
            throw ".NET workload update failed with exit code $LASTEXITCODE. On Linux or macOS with a protected SDK installation, use -DotNetWorkloads UpdateWithSudo."
        }
    }

    foreach ($manifest in $ToolManifest) {
        $manifestPath = Join-Path $repositoryRoot $manifest
        if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
            throw ".NET tool manifest missing: $manifest"
        }
        Write-Host "[Build stage: preflight] Restoring .NET tools from $manifest." -ForegroundColor Cyan
        Push-Location -LiteralPath (Split-Path -Parent $manifestPath)
        try {
            & $dotnet tool restore --tool-manifest $manifestPath
            if ($LASTEXITCODE -ne 0) {
                throw ".NET tool restore failed for '$manifest' with exit code $LASTEXITCODE."
            }
        }
        finally {
            Pop-Location
        }
    }
}
finally {
    Pop-Location
}
