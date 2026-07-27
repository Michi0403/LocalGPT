param([switch]$RefreshBaseline)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$baselinePath = Join-Path $root "build\logging-baseline.json"

function Get-RelativePath([string]$Path) {
    return [System.IO.Path]::GetRelativePath($root, $Path).Replace('\\', '/')
}

function Get-LoggingMetrics([string]$Path) {
    $content = Get-Content $Path -Raw
    return [ordered]@{
        loggerReferences = ([regex]::Matches($content, '\bILogger(?:<[^>]+>)?\b')).Count
        logCalls = ([regex]::Matches($content, '\.Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(')).Count
        catchBlocks = ([regex]::Matches($content, '\bcatch\b')).Count
        hasYield = $content -match '\byield\s+(?:return|break)\b'
        isPureHelper = $content -match 'logging-policy:\s*pure-helper'
        hasClass = $content -match '\bclass\s+[A-Za-z_]'
    }
}

$sourceFiles = Get-ChildItem $root -Recurse -File -Filter *.cs |
    Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj|artifacts|node_modules)[\\/]' -and
        $_.FullName -match '[\\/](Services|Controllers|HostedServices)[\\/]'
    } |
    Sort-Object FullName

if ($RefreshBaseline) {
    if ($env:ALLOW_LOGGING_BASELINE_REFRESH -ne '1') {
        throw "Refusing to refresh the logging baseline. Set ALLOW_LOGGING_BASELINE_REFRESH=1 only for a reviewed maintainer change."
    }
    $files = [ordered]@{}
    foreach ($file in $sourceFiles) {
        $metrics = Get-LoggingMetrics $file.FullName
        $files[(Get-RelativePath $file.FullName)] = [ordered]@{
            loggerReferences = $metrics.loggerReferences
            logCalls = $metrics.logCalls
            catchBlocks = $metrics.catchBlocks
        }
    }
    [ordered]@{
        schemaVersion = 1
        policy = "Logging removal is not cleanup; maintained diagnostics are monotonic."
        files = $files
    } | ConvertTo-Json -Depth 8 | Set-Content $baselinePath -Encoding utf8
    Write-Host "Logging baseline refreshed: $baselinePath" -ForegroundColor Yellow
    exit 0
}

if (-not (Test-Path $baselinePath)) { throw "Logging baseline is missing: $baselinePath" }
$baseline = Get-Content $baselinePath -Raw | ConvertFrom-Json
$failures = [System.Collections.Generic.List[string]]::new()
$currentPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

foreach ($file in $sourceFiles) {
    $relative = Get-RelativePath $file.FullName
    [void]$currentPaths.Add($relative)
    $metrics = Get-LoggingMetrics $file.FullName
    $property = $baseline.files.PSObject.Properties[$relative]
    if ($null -eq $property) {
        if ($metrics.hasClass -and -not $metrics.isPureHelper) {
            if ($metrics.loggerReferences -lt 1 -or $metrics.logCalls -lt 1) {
                $failures.Add("New operational source '$relative' has no structured ILogger/log call. Add diagnostics or mark a genuinely pure helper with '// logging-policy: pure-helper'.")
            }
            if (-not $metrics.hasYield -and $metrics.catchBlocks -lt 1) {
                $failures.Add("New operational source '$relative' has no catch/log boundary. Iterator sources containing yield are exempt.")
            }
        }
        continue
    }

    $expected = $property.Value
    foreach ($metricName in @('loggerReferences', 'logCalls', 'catchBlocks')) {
        $actual = [int]$metrics[$metricName]
        $minimum = [int]$expected.$metricName
        if ($actual -lt $minimum) {
            $failures.Add("Logging regression in '$relative': $metricName decreased from $minimum to $actual.")
        }
    }
}

foreach ($property in $baseline.files.PSObject.Properties) {
    if (-not $currentPaths.Contains($property.Name)) {
        $failures.Add("Maintained service/controller source from logging baseline was removed or moved without review: $($property.Name)")
    }
}

$policyPath = Join-Path $root 'docs\LOGGING_INTEGRITY.md'
if (-not (Test-Path $policyPath) -or (Get-Content $policyPath -Raw) -notmatch 'Logging removal is not cleanup') {
    $failures.Add("Logging integrity policy is missing or was weakened.")
}

if (Test-Path (Join-Path $root 'src\PublisherStudio.Web\Program.cs')) {
    $program = Get-Content (Join-Path $root 'src\PublisherStudio.Web\Program.cs') -Raw
    $filter = Join-Path $root 'src\PublisherStudio.Web\Diagnostics\ControllerRequestLoggingFilter.cs'
    if (-not (Test-Path $filter) -or $program -notmatch 'Filters\.AddService<ControllerRequestLoggingFilter>') {
        $failures.Add("PublisherStudio global controller logging filter is missing or not registered.")
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    throw "Logging integrity validation failed with $($failures.Count) problem(s)."
}

Write-Host "Logging integrity validation passed for $($sourceFiles.Count) maintained service/controller source files." -ForegroundColor Green
