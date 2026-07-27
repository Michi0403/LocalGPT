[CmdletBinding()]
param(
    [switch]$RefreshBaseline,
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    # The script lives in <repository>\build. Deriving the root here avoids
    # native-command quoting problems when an MSBuild directory ends in '\'.
    $RepositoryRoot = Split-Path -Parent $PSScriptRoot
}
else {
    # Be defensive for manual/native invocations that accidentally preserve a
    # surrounding quote (for example: "C:\repo\" on Windows).
    $RepositoryRoot = $RepositoryRoot.Trim().Trim([char[]]@([char]34, [char]39))
}

$resolvedRoot = Resolve-Path -LiteralPath $RepositoryRoot -ErrorAction Stop
$root = $resolvedRoot.ProviderPath.TrimEnd([char[]]@(
    [System.IO.Path]::DirectorySeparatorChar,
    [System.IO.Path]::AltDirectorySeparatorChar
))
$baselinePath = Join-Path $root 'build\logging-baseline.json'

function Get-NormalizedRelativePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootPrefix = $root + [System.IO.Path]::DirectorySeparatorChar
    $comparison = if ($env:OS -eq 'Windows_NT') {
        [System.StringComparison]::OrdinalIgnoreCase
    }
    else {
        [System.StringComparison]::Ordinal
    }

    if (-not $fullPath.StartsWith($rootPrefix, $comparison)) {
        throw "Source path '$fullPath' is outside repository root '$root'."
    }

    # Do not use Path.GetRelativePath here: Windows PowerShell 5.1 runs on
    # .NET Framework, where that API is unavailable. Normalize both Windows
    # and Unix separators because the committed baseline always uses '/'.
    return $fullPath.Substring($rootPrefix.Length).Replace('\', '/')
}

function Get-LoggingMetrics {
    param([Parameter(Mandatory = $true)][string]$Path)

    $content = Get-Content -LiteralPath $Path -Raw
    return [pscustomobject]@{
        loggerReferences = ([regex]::Matches($content, '\bILogger(?:<[^>]+>)?\b')).Count
        logCalls = ([regex]::Matches($content, '\.Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(')).Count
        catchBlocks = ([regex]::Matches($content, '\bcatch\b')).Count
        hasYield = $content -match '\byield\s+(?:return|break)\b'
        isPureHelper = $content -match 'logging-policy:\s*pure-helper'
        hasClass = $content -match '\bclass\s+[A-Za-z_]'
    }
}

$sourceFiles = Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.cs' |
    Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj|artifacts|node_modules)[\\/]' -and
        $_.FullName -match '[\\/](Services|Controllers|HostedServices)[\\/]'
    } |
    Sort-Object FullName

if ($RefreshBaseline) {
    if ($env:ALLOW_LOGGING_BASELINE_REFRESH -ne '1') {
        throw 'Refusing to refresh the logging baseline. Set ALLOW_LOGGING_BASELINE_REFRESH=1 only for a reviewed maintainer change.'
    }

    $files = [ordered]@{}
    foreach ($file in $sourceFiles) {
        $metrics = Get-LoggingMetrics -Path $file.FullName
        $files[(Get-NormalizedRelativePath -Path $file.FullName)] = [ordered]@{
            loggerReferences = $metrics.loggerReferences
            logCalls = $metrics.logCalls
            catchBlocks = $metrics.catchBlocks
        }
    }

    [ordered]@{
        schemaVersion = 1
        policy = 'Logging removal is not cleanup; maintained diagnostics are monotonic.'
        files = $files
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $baselinePath -Encoding utf8

    Write-Host "Logging baseline refreshed: $baselinePath" -ForegroundColor Yellow
    exit 0
}

if (-not (Test-Path -LiteralPath $baselinePath)) {
    throw "Logging baseline is missing: $baselinePath"
}

$baseline = Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json
$baselineFiles = @{}
foreach ($property in $baseline.files.PSObject.Properties) {
    $baselineFiles[$property.Name.Replace('\', '/')] = $property.Value
}

$failures = New-Object 'System.Collections.Generic.List[string]'
$currentPaths = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)

foreach ($file in $sourceFiles) {
    $relative = Get-NormalizedRelativePath -Path $file.FullName
    [void]$currentPaths.Add($relative)
    $metrics = Get-LoggingMetrics -Path $file.FullName
    $expected = $baselineFiles[$relative]

    if ($null -eq $expected) {
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

    foreach ($metricName in @('loggerReferences', 'logCalls', 'catchBlocks')) {
        $actual = [int]$metrics.PSObject.Properties[$metricName].Value
        $minimum = [int]$expected.PSObject.Properties[$metricName].Value
        if ($actual -lt $minimum) {
            $failures.Add("Logging regression in '$relative': $metricName decreased from $minimum to $actual.")
        }
    }
}

foreach ($relative in $baselineFiles.Keys) {
    if (-not $currentPaths.Contains($relative)) {
        $failures.Add("Maintained service/controller source from logging baseline was removed or moved without review: $relative")
    }
}

$policyPath = Join-Path $root 'docs\LOGGING_INTEGRITY.md'
if (-not (Test-Path -LiteralPath $policyPath) -or (Get-Content -LiteralPath $policyPath -Raw) -notmatch 'Logging removal is not cleanup') {
    $failures.Add('Logging integrity policy is missing or was weakened.')
}

$localGptProgramPath = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Program.cs'
if (Test-Path -LiteralPath $localGptProgramPath) {
    $program = Get-Content -LiteralPath $localGptProgramPath -Raw
    $filter = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Diagnostics\ControllerRequestLoggingFilter.cs'
    if (-not (Test-Path -LiteralPath $filter) -or $program -notmatch 'Filters\.AddService<ControllerRequestLoggingFilter>') {
        $failures.Add('LocalGPT global controller logging filter is missing or not registered.')
    }
}

$publisherProgramPath = Join-Path $root 'src\PublisherStudio.Web\Program.cs'
if (Test-Path -LiteralPath $publisherProgramPath) {
    $program = Get-Content -LiteralPath $publisherProgramPath -Raw
    $filter = Join-Path $root 'src\PublisherStudio.Web\Diagnostics\ControllerRequestLoggingFilter.cs'
    if (-not (Test-Path -LiteralPath $filter) -or $program -notmatch 'Filters\.AddService<ControllerRequestLoggingFilter>') {
        $failures.Add('PublisherStudio global controller logging filter is missing or not registered.')
    }
}

if ($failures.Count -gt 0) {
    Write-Host 'Logging integrity validation failed:' -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host "  - $failure" -ForegroundColor Red
    }
    throw "Logging integrity validation failed with $($failures.Count) problem(s)."
}

Write-Host "Logging integrity validation passed for $($sourceFiles.Count) maintained service/controller source files." -ForegroundColor Green
