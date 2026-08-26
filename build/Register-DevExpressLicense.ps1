[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$LicenseFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$source = [IO.Path]::GetFullPath($LicenseFile)
if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
    throw "DevExpress license file not found: $source"
}
if ((Get-Item -LiteralPath $source).Length -le 0) {
    throw 'The selected DevExpress license file is empty.'
}

$runningOnWindows = [IO.Path]::DirectorySeparatorChar -eq '\'
$homePath = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
if ([string]::IsNullOrWhiteSpace($homePath)) { $homePath = $env:HOME }

if ($runningOnWindows) {
    if ([string]::IsNullOrWhiteSpace($env:APPDATA)) { throw 'APPDATA is not available for the current Windows user.' }
    $destinationDirectory = Join-Path $env:APPDATA 'DevExpress'
}
else {
    if ([string]::IsNullOrWhiteSpace($homePath)) { throw 'The current user home directory could not be determined.' }
    $unixName = ''
    try { $unixName = [string](& uname -s 2>$null | Select-Object -First 1) } catch { $unixName = '' }
    if ([string]::Equals($unixName.Trim(), 'Darwin', [StringComparison]::OrdinalIgnoreCase)) {
        $destinationDirectory = Join-Path (Join-Path $homePath 'Library') 'Application Support/DevExpress'
    }
    else {
        $destinationDirectory = Join-Path $homePath '.config/DevExpress'
    }
}

New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
$destination = Join-Path $destinationDirectory 'DevExpress_License.txt'
Copy-Item -LiteralPath $source -Destination $destination -Force

# Keep the current PowerShell process deterministic for commands run immediately after registration.
[Environment]::SetEnvironmentVariable('DevExpress_LicensePath', $destinationDirectory, [EnvironmentVariableTarget]::Process)

Write-Host "Registered the DevExpress .NET license for the current user at '$destination'." -ForegroundColor Green
Write-Host 'The license value was not printed or added to the LocalGPT repository. Start a new IDE/terminal process if an already-running IDE still reports an old license state.' -ForegroundColor DarkGreen
