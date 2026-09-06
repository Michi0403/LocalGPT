[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$AssemblyPath,
    [int]$TimeoutSeconds = 45
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$assembly = (Resolve-Path -LiteralPath $AssemblyPath -ErrorAction Stop).ProviderPath
if ($TimeoutSeconds -lt 10 -or $TimeoutSeconds -gt 180) {
    throw 'Startup health timeout must be between 10 and 180 seconds.'
}

$dotnetCommand = Get-Command dotnet -ErrorAction Stop
$dotnetPath = $dotnetCommand.Source
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('localgpt-startup-smoke-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
$dbPath = Join-Path $tempRoot 'localgpt-smoke.db'

$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
$listener.Start()
try {
    $port = ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
}
finally {
    $listener.Stop()
}

$process = $null
$client = $null
try {
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $dotnetPath
    $startInfo.WorkingDirectory = Split-Path -Parent $assembly
    $escapedAssembly = $assembly.Replace('"', '\"')
    $startInfo.Arguments = '"' + $escapedAssembly + '" --port ' + $port
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true

    # Keep the smoke run isolated from the developer's normal LocalGPT database/configuration.
    $startInfo.EnvironmentVariables['LocalGptDatabase__Path'] = $dbPath
    $startInfo.EnvironmentVariables['LOCALGPT_NETWORK_ENABLED'] = 'false'
    $startInfo.EnvironmentVariables['OneWire__Enabled'] = 'false'
    $startInfo.EnvironmentVariables['OneWire__EnableDiscovery'] = 'false'
    $startInfo.EnvironmentVariables['XDG_DATA_HOME'] = (Join-Path $tempRoot 'xdg-data')
    $startInfo.EnvironmentVariables['LOCALAPPDATA'] = (Join-Path $tempRoot 'local-app-data')
    $startInfo.EnvironmentVariables['APPDATA'] = (Join-Path $tempRoot 'app-data')
    $startInfo.EnvironmentVariables['HOME'] = $tempRoot
    $startInfo.EnvironmentVariables['USERPROFILE'] = $tempRoot

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    if (-not $process.Start()) {
        throw 'LocalGPT startup health process could not be started.'
    }

    Add-Type -AssemblyName System.Net.Http -ErrorAction SilentlyContinue
    $client = [System.Net.Http.HttpClient]::new()
    $client.Timeout = [TimeSpan]::FromSeconds(2)
    $healthUrl = "http://127.0.0.1:$port/health"
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    $healthy = $false
    $lastError = $null

    while ([DateTime]::UtcNow -lt $deadline) {
        if ($process.HasExited) {
            throw "LocalGPT startup health process exited before listening (exit code $($process.ExitCode))."
        }

        try {
            $response = $client.GetAsync($healthUrl).GetAwaiter().GetResult()
            try {
                if ($response.IsSuccessStatusCode) {
                    $healthy = $true
                    break
                }
                $lastError = "HTTP $([int]$response.StatusCode) $($response.ReasonPhrase)"
            }
            finally {
                $response.Dispose()
            }
        }
        catch {
            $lastError = $_.Exception.Message
        }

        Start-Sleep -Milliseconds 250
    }

    if (-not $healthy) {
        throw "LocalGPT did not answer $healthUrl within $TimeoutSeconds seconds. Last probe: $lastError"
    }

    Write-Output "LocalGPT startup health smoke test passed on $healthUrl before documentation generation."
}
finally {
    if ($client -ne $null) {
        $client.Dispose()
    }
    if ($process -ne $null) {
        try {
            if (-not $process.HasExited) {
                $process.Kill()
                [void]$process.WaitForExit(5000)
            }
        }
        catch {
            Write-Warning "Could not stop LocalGPT startup health process cleanly: $($_.Exception.Message)"
        }
        $process.Dispose()
    }
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
