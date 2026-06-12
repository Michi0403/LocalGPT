```powershell
<#
.SYNOPSIS
    Starts LocalGPT and opens the default browser at the URL it reports.

.DESCRIPTION
    - Launches LocalGPT.exe (assumes it is in the same directory or on PATH).
    - Captures stdout to find the “Now listening on: http://127.0.0.1:<port>” line.
    - Opens that URL in the default browser.
#>

param(
    [Parameter(Mandatory)]
    [string]$LocalGptExe,   # Path to LocalGPT binary
    [string]$port = "0",            # Port
    [int]$TimeoutSeconds                  # How long to wait for the listening log
)

function Get-ListeningPort {
    param([string]$OutputFile)
    $regex = 'Now listening on:\s*(http://127\.0\.0\.1:(\d+))'
    foreach ($line in Get-Content -Path $OutputFile) {
        if ($line -match $regex) { return $Matches[2] }
    }
    return $null
}
if($port -eq "0")
{
   $port = "5000"
}
if ([string]::IsNullOrEmpty($LocalGptExe) -or $LocalGptExe -eq "0") {
    $LocalGptExe = Join-Path $env:LOCALAPPDATA "LocalGPT\winx64\LocalGPT.exe"
    Write-Host "LocalGPTExe Path was empty set default from $LocalGptExe Path with Port $port"
}

Write-Host "Try to start LocalGPTExe $LocalGptExe with Port $port"
# Start LocalGPT with redirected output
$proc = Start-Process -FilePath $LocalGptExe `
                       -ArgumentList $port `
                       -PassThru `
                       -NoNewWindow 
$uri = "http://127.0.0.1:$port"
Write-Host "LocalGPT is listening at $uri"

# Open default browser
Start-Process -FilePath $uri
