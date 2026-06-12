param(
    [Parameter(Mandatory)]
    [string]$LocalGptExePath,   # Path to LocalGPT binary
    [string]$porti = "0",            # Port
    [int]$TimeoutSeconds                  # How long to wait for the listening log
)
Set-ExecutionPolicy Bypass -Scope Process
$LocalGptExe = $LocalGptExePath
$port = $porti
if($port -eq "0")
{
   $port = "5000"
}
if ([string]::IsNullOrEmpty($LocalGptExe) -or $LocalGptExe -eq "0") {
    $LocalGptExe = Join-Path $env:LOCALAPPDATA "LocalGPT\LocalGPT.exe"
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
