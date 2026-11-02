# Source-only cross-platform boundary audit. Release and local-development entrypoints
# run this on Windows, Linux, and macOS; no Python or dotnet execution is required.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$app = Join-Path $root 'src/LocalGPT'
$failures = [System.Collections.Generic.List[string]]::new()
$contractCount = 0

function Assert-SourceContains {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Expected
    )

    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $failures.Add("Missing cross-platform contract source: $RelativePath")
        return
    }

    $source = [IO.File]::ReadAllText($path)
    if ($source.IndexOf($Expected, [StringComparison]::Ordinal) -lt 0) {
        $failures.Add("$RelativePath is missing cross-platform contract: $Expected")
        return
    }

    $script:contractCount++
}

$projectPath = Join-Path $app 'LocalGPT.csproj'
if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw 'LocalGPT project file is missing from the cross-platform boundary audit.'
}
$projectSource = [IO.File]::ReadAllText($projectPath)
$versionMatch = [regex]::Match($projectSource, '<Version>\s*(?<Version>[^<]+?)\s*</Version>')
$sourceVersion = if ($versionMatch.Success) { $versionMatch.Groups['Version'].Value } else { 'unknown' }
Write-Host "Cross-platform preflight: LocalGPT $sourceVersion from $root (checking the actual extracted source tree)."
foreach ($package in @(
    'System.Drawing.Common',
    'System.Data.OleDb',
    'System.Diagnostics.PerformanceCounter',
    'Microsoft.Windows.AI.MachineLearning'
)) {
    if ($projectSource.IndexOf($package, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        $failures.Add("Platform-neutral LocalGPT backend references Windows-oriented package $package")
    }
    else { $contractCount++ }
}

# OS-specific behavior belongs in composition/platform implementations, never in
# shared business services. These are the deliberately reviewed exceptions.
$approvedOsBranches = @(
    'src/LocalGPT/Program.ServiceRegistration.cs',
    'src/LocalGPT/Services/PlatformRuntimeServices.cs',
    'src/LocalGPT/Services/HardwarePlatformProbeServices.cs',
    'src/LocalGPT/Services/LocalConsolePlatformServices.cs'
)
$approvedWindowsDetails = @(
    'src/LocalGPT/Services/LocalConsolePlatformServices.cs',
    'src/LocalGPT/Services/PlatformRuntimeServices.cs',
    'src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs',
    'src/LocalGPT/Services/SqliteUtilityService.cs'
)
$osPattern = '\b(?:OperatingSystem\.Is(?:Windows|Linux|MacOS)|RuntimeInformation\.IsOSPlatform|OSPlatform\.(?:Windows|Linux|OSX))'
$windowsDetailPattern = 'GetEnvironmentVariable\("(?:LOCALAPPDATA|APPDATA|WINDIR|ProgramFiles|PROGRAMFILES)"\)|"(?:cmd\.exe|powershell\.exe|where\.exe|explorer\.exe)"'
$drawingPattern = 'using System\.Drawing;|System\.Drawing\.(?:Bitmap|Graphics)'
$sourceCount = 0
foreach ($file in (Get-ChildItem -LiteralPath $app -Recurse -File -Filter '*.cs' | Where-Object {
    $_.FullName -notmatch '[\\/](?:bin|obj)[\\/]'
})) {
    $sourceCount++
    $relative = $file.FullName.Substring($root.Length).TrimStart([char[]]'\/').Replace('\', '/')
    $source = [IO.File]::ReadAllText($file.FullName)
    if ([regex]::IsMatch($source, $drawingPattern)) {
        $failures.Add("$relative uses backend GDI/System.Drawing APIs")
    }
    if ($approvedOsBranches -cnotcontains $relative) {
        foreach ($match in [regex]::Matches($source, $osPattern)) {
            $line = [regex]::Matches($source.Substring(0, $match.Index), "`r`n|`r|`n").Count + 1
            $failures.Add("${relative}:$line branches on the OS outside a reviewed composition/platform boundary ($($match.Value)); use IPlatformRuntimeService instead")
        }
    }
    if ($approvedWindowsDetails -cnotcontains $relative) {
        foreach ($match in [regex]::Matches($source, $windowsDetailPattern)) {
            $line = [regex]::Matches($source.Substring(0, $match.Index), "`r`n|`r|`n").Count + 1
            $failures.Add("${relative}:$line contains a Windows-only executable/environment assumption outside a reviewed platform boundary ($($match.Value))")
        }
    }
}
if ($sourceCount -eq 0) { $failures.Add('No C# sources were found for the cross-platform boundary audit.') }

# Physical-path security checks must not silently treat case-distinct Unix paths
# as the same directory. The source-level prohibited expressions are retained from
# the historical audit; obsolete/moved files may be absent, but new OS leakage is
# still checked above and platform path comparison is checked below.
$forbiddenContainment = @(
    [pscustomobject]@{ File = 'src/LocalGPT/Services/CouncilRuntimeService.ArtifactWorkspaceRuntime.cs'; Snippet = 'path.StartsWith(root, StringComparison.OrdinalIgnoreCase)' },
    [pscustomobject]@{ File = 'src/LocalGPT/Services/CouncilRuntimeService.PromptAndTextRuntime.cs'; Snippet = 'normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)' },
    [pscustomobject]@{ File = 'src/LocalGPT/Services/RemoteKnowledgeImportService.ArchiveHandling.cs'; Snippet = 'destination.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)' },
    [pscustomobject]@{ File = 'src/LocalGPT/Services/RemoteKnowledgeImportService.ArchiveHandling.cs'; Snippet = 'source.StartsWith(cacheFull, StringComparison.OrdinalIgnoreCase)' },
    [pscustomobject]@{ File = 'src/LocalGPT/Services/RemoteKnowledgeImportService.ArchiveHandling.cs'; Snippet = 'source.StartsWith(selectedFull, StringComparison.OrdinalIgnoreCase)' },
    [pscustomobject]@{ File = 'src/LocalGPT/Services/RemoteKnowledgeImportService.ArchiveHandling.cs'; Snippet = 'destination.StartsWith(selectedFull, StringComparison.OrdinalIgnoreCase)' },
    [pscustomobject]@{ File = 'src/LocalGPT/Services/DocumentationCatalogService.cs'; Snippet = 'normalizedCandidate.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)' },
    [pscustomobject]@{ File = 'src/LocalGPT/Services/ProjectMaintenanceService.TextAndValidation.cs'; Snippet = 'directory.StartsWith(root, StringComparison.OrdinalIgnoreCase)' }
)
foreach ($case in $forbiddenContainment) {
    $path = Join-Path $root $case.File
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
    $source = [IO.File]::ReadAllText($path)
    if ($source.IndexOf($case.Snippet, [StringComparison]::Ordinal) -ge 0) {
        $failures.Add("$($case.File) contains hard-coded case-insensitive physical path containment: $($case.Snippet)")
    }
    else { $contractCount++ }
}

$platformPath = Join-Path $app 'Services/PlatformRuntimeServices.cs'
if (-not (Test-Path -LiteralPath $platformPath -PathType Leaf)) {
    $failures.Add('Missing platform path-comparison implementation.')
}
else {
    $platformSource = [IO.File]::ReadAllText($platformPath)
    $unixStart = $platformSource.IndexOf('public sealed class UnixPlatformRuntimeService', [StringComparison]::Ordinal)
    if ($unixStart -lt 0) {
        $failures.Add('Missing UnixPlatformRuntimeService implementation.')
    }
    else {
        $unixSource = $platformSource.Substring($unixStart)
        foreach ($expected in @(
            'public StringComparer PathComparer => StringComparer.Ordinal;',
            'public StringComparison PathComparison => StringComparison.Ordinal;',
            'public string GetNativeExecutableName(string baseName) => baseName;'
        )) {
            if ($unixSource.IndexOf($expected, [StringComparison]::Ordinal) -lt 0) {
                $failures.Add("UnixPlatformRuntimeService must retain its path and native executable contract: $expected")
            }
            else { $contractCount++ }
        }
    }
    $windowsStart = $platformSource.IndexOf('public sealed class WindowsPlatformRuntimeService', [StringComparison]::Ordinal)
    if ($windowsStart -lt 0 -or $unixStart -le $windowsStart) {
        $failures.Add('Missing or misordered WindowsPlatformRuntimeService; cannot verify Windows executable naming within its own class.')
    }
    else {
        $windowsSource = $platformSource.Substring($windowsStart, $unixStart - $windowsStart)
        $expected = 'public string GetNativeExecutableName(string baseName) => baseName + ".exe";'
        if ($windowsSource.IndexOf($expected, [StringComparison]::Ordinal) -lt 0) {
            $failures.Add("WindowsPlatformRuntimeService must implement native executable naming within its own class: $expected")
        }
        else { $contractCount++ }
    }
}

foreach ($expected in @(
    'protected virtual StringComparer ExecutablePathComparer => StringComparer.Ordinal;',
    'protected override StringComparer ExecutablePathComparer => StringComparer.OrdinalIgnoreCase;'
)) {
    Assert-SourceContains -RelativePath 'src/LocalGPT/Services/OllamaPlatformServices.cs' -Expected $expected
}
foreach ($contract in @(
    [pscustomobject]@{ File = 'src/LocalGPT/Interfaces/IPlatformRuntimeService.cs'; Text = 'interface IPlatformRuntimeService' },
    [pscustomobject]@{ File = 'src/LocalGPT/Interfaces/ILocalConsolePlatformService.cs'; Text = 'interface ILocalConsolePlatformService' },
    [pscustomobject]@{ File = 'src/LocalGPT/Interfaces/IHardwarePlatformProbeService.cs'; Text = 'interface IHardwarePlatformProbeService' },
    [pscustomobject]@{ File = 'src/LocalGPT/Interfaces/IRuntimeSecretFileProtectionService.cs'; Text = 'interface IRuntimeSecretFileProtectionService' },
    [pscustomobject]@{ File = 'src/LocalGPT/Services/PlatformRuntimeServices.cs'; Text = 'sealed class UnixPlatformRuntimeService' },
    [pscustomobject]@{ File = 'src/LocalGPT/Services/PlatformRuntimeServices.cs'; Text = 'sealed class WindowsPlatformRuntimeService' }
)) {
    Assert-SourceContains -RelativePath $contract.File -Expected $contract.Text
}
# Host-native sandbox executable naming is a platform concern. Guard both platform
# implementations and the consumer wiring: a host-only success must not hide an
# incorrect Windows/Unix counterpart or a future direct OS branch in the sandbox.
foreach ($contract in @(
    [pscustomobject]@{ File = 'src/LocalGPT/Interfaces/IPlatformRuntimeService.cs'; Text = 'string GetNativeExecutableName(string baseName);' },
    [pscustomobject]@{ File = 'src/LocalGPT/Services/HyperlightUnikraftSessionSandboxProvider.cs'; Text = 'platformRuntime.GetNativeExecutableName("hluk")' },
    [pscustomobject]@{ File = 'src/LocalGPT/Services/SessionSandboxExecutionService.cs'; Text = 'platformRuntime.GetNativeExecutableName("hluk")' }
)) {
    Assert-SourceContains -RelativePath $contract.File -Expected $contract.Text
}

# Fail with explicit file evidence when an old source file is combined with a newer
# validator during extraction or checkout. Do not waive the OS architecture rule.
$inconsistentSandboxSources = [System.Collections.Generic.List[string]]::new()
foreach ($relative in @(
    'src/LocalGPT/Services/HyperlightUnikraftSessionSandboxProvider.cs',
    'src/LocalGPT/Services/SessionSandboxExecutionService.cs'
)) {
    $path = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
    $source = [IO.File]::ReadAllText($path)
    $directOsBranch = [regex]::Match($source, $osPattern)
    $hasPortableCall = $source.IndexOf('platformRuntime.GetNativeExecutableName("hluk")', [StringComparison]::Ordinal) -ge 0
    if ($directOsBranch.Success -and -not $hasPortableCall) {
        $line = [regex]::Matches($source.Substring(0, $directOsBranch.Index), "`r`n|`r|`n").Count + 1
        $digest = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.Substring(0, 16)
        $inconsistentSandboxSources.Add("${relative}:$line (SHA256 prefix $digest)")
    }
}
if ($inconsistentSandboxSources.Count -gt 0) {
    $failures.Add('Source/validator revision mismatch: the cross-platform guard expects IPlatformRuntimeService, but older sandbox OS-branch implementations are present at ' +
        ($inconsistentSandboxSources -join '; ') + '. Extract the COMPLETE source archive into a NEW EMPTY directory and launch Build-Release.ps1 from that directory. Do not merge source archives or copy only the build/ scripts.')
}

foreach ($registration in @(
    'IPlatformRuntimeService, WindowsPlatformRuntimeService',
    'IPlatformRuntimeService, UnixPlatformRuntimeService',
    'ILocalConsolePlatformService, WindowsLocalConsolePlatformService',
    'ILocalConsolePlatformService, UnixLocalConsolePlatformService',
    'IHardwarePlatformProbeService, WindowsHardwarePlatformProbeService',
    'IHardwarePlatformProbeService, UnixHardwarePlatformProbeService',
    'IRuntimeSecretFileProtectionService, WindowsRuntimeSecretFileProtectionService',
    'IRuntimeSecretFileProtectionService, UnixRuntimeSecretFileProtectionService'
)) {
    Assert-SourceContains -RelativePath 'src/LocalGPT/Program.ServiceRegistration.cs' -Expected $registration
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "Cross-platform boundary: $failure" -ForegroundColor Red }
    throw "LocalGPT cross-platform boundary audit failed with $($failures.Count) issue(s)."
}
Write-Host "LocalGPT cross-platform boundary audit passed: $sourceCount C# sources and $contractCount explicit contracts; no platform leaks detected."
