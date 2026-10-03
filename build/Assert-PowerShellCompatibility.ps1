[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$unsupportedContainsPattern = '\.Contains\([^\r\n]*,\s*\[(?:System\.)?StringComparison\]::'
$unsupportedPathRelativePattern = '\[(?:System\.)?IO\.Path\]::GetRelativePath\s*\('
$unsupportedArgumentListPattern = '\.ArgumentList(?:\.|\s*=)'
$unsupportedKillTreePattern = '\.Kill\(\s*\$true\s*\)'
$readOnlyPlatformVariableAssignmentPattern = '(?i)\$(?:IsWindows|IsLinux|IsMacOS|IsCoreCLR)\s*='
$wrappedConvertFromJsonPattern = '@\(\s*ConvertFrom-Json\b'
$repositoryBackslashPathLiteralPattern = '(?i)^(?:\.\.?\\|(?:src|build|docs|wwwroot|controller|controllers|services|components|diagnostics|properties)[\/\\]).*\\'
$failures = [System.Collections.Generic.List[string]]::new()

function Get-RepositoryRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return $Path.Substring($root.Length).TrimStart([char[]]'\/').Replace('\', '/')
}

function Read-RepositoryScriptText {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo]$File
    )

    $relative = Get-RepositoryRelativePath -Path $File.FullName
    for ($attempt = 1; $attempt -le 2; $attempt++) {
        try {
            return [System.IO.File]::ReadAllText($File.FullName)
        }
        catch [System.IO.FileNotFoundException] {
            if ($attempt -lt 2) {
                Start-Sleep -Milliseconds 50
                continue
            }

            throw "PowerShell compatibility validation could not read source script '$relative' because it disappeared during validation. Re-run the build after checking for a concurrent checkout, cleanup, or generator process."
        }
        catch [System.IO.DirectoryNotFoundException] {
            if ($attempt -lt 2) {
                Start-Sleep -Milliseconds 50
                continue
            }

            throw "PowerShell compatibility validation could not read source script '$relative' because its directory disappeared during validation. Re-run the build after checking for a concurrent checkout, cleanup, or generator process."
        }
    }
}

# Windows PowerShell 5.1 can apply Get-ChildItem -Include inconsistently when
# -LiteralPath and -Recurse are combined. Filter extensions explicitly so the
# validator never attempts to read generated assets such as DocFX SVG files.
$scriptFiles = Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
    $isPowerShellScript =
        [string]::Equals($_.Extension, '.ps1', [System.StringComparison]::OrdinalIgnoreCase) -or
        [string]::Equals($_.Extension, '.psm1', [System.StringComparison]::OrdinalIgnoreCase)

    if (-not $isPowerShellScript) {
        return $false
    }

    $relative = Get-RepositoryRelativePath -Path $_.FullName
    return $relative -notmatch '(^|/)(\.git|\.vs|artifacts|bin|obj|packages|node_modules)(/|$)' -and
        $relative -notmatch '^docs/_site(/|$)'
}

foreach ($file in $scriptFiles) {
    $content = Read-RepositoryScriptText -File $file

    # Parse every repository script before any release/local-development helper can invoke it.
    # This catches interpolation mistakes such as an unbraced variable immediately followed by a colon (which PowerShell reads as an
    # invalid scoped-variable reference) at the initial compatibility preflight instead of deep
    # into a long build.
    $tokens = $null
    $parseErrors = $null
    $scriptAst = [System.Management.Automation.Language.Parser]::ParseInput(
        $content,
        [ref]$tokens,
        [ref]$parseErrors)
    foreach ($parseError in @($parseErrors)) {
        $relative = Get-RepositoryRelativePath -Path $file.FullName
        $line = $parseError.Extent.StartLineNumber
        $message = $parseError.Message
        $failures.Add("${relative}:$line has a PowerShell parser error: $message")
    }
    # Windows PowerShell 5.1 exposes Join-Path with only Path + ChildPath positional
    # arguments. PowerShell 6+ added AdditionalChildPath, so a bare three-argument
    # Join-Path expression can pass modern pwsh validation yet fail late in a Windows
    # documentation build. Reject that shape before the long DocFX/PDF pipeline starts.
    $joinPathCommands = @($scriptAst.FindAll({
        param($node)
        if (-not ($node -is [System.Management.Automation.Language.CommandAst])) { return $false }
        $commandName = $node.GetCommandName()
        return -not [string]::IsNullOrWhiteSpace($commandName) -and
            [string]::Equals($commandName, 'Join-Path', [System.StringComparison]::OrdinalIgnoreCase)
    }, $true))
    foreach ($commandAst in $joinPathCommands) {
        $arguments = @($commandAst.CommandElements | Select-Object -Skip 1)
        $namedParameterCount = @($arguments | Where-Object { $_ -is [System.Management.Automation.Language.CommandParameterAst] }).Count
        if ($namedParameterCount -eq 0 -and $arguments.Count -gt 2) {
            $relative = Get-RepositoryRelativePath -Path $file.FullName
            $line = $commandAst.Extent.StartLineNumber
            $failures.Add("${relative}:$line passes more than Path + ChildPath positionally to Join-Path. Windows PowerShell 5.1 has no AdditionalChildPath parameter; nest Join-Path calls instead.")
        }
    }
    foreach ($match in [regex]::Matches($content, $unsupportedContainsPattern)) {
        $line = [regex]::Matches($content.Substring(0, $match.Index), "`r`n|`r|`n").Count + 1
        $relative = Get-RepositoryRelativePath -Path $file.FullName
        $failures.Add("${relative}:$line uses String.Contains(value, StringComparison), which is unavailable in Windows PowerShell 5.1. Use String.IndexOf(value, comparison) instead.")
    }

    foreach ($compatibilityPattern in @(
        [pscustomobject]@{ Pattern = $unsupportedPathRelativePattern; Message = 'uses Path.GetRelativePath directly, which is unavailable on Windows PowerShell 5.1/.NET Framework. Use the portable reflection/URI helper instead.' },
        [pscustomobject]@{ Pattern = $unsupportedArgumentListPattern; Message = 'uses ProcessStartInfo.ArgumentList directly, which is unavailable on Windows PowerShell 5.1/.NET Framework. Use the portable process-argument helper instead.' },
        [pscustomobject]@{ Pattern = $unsupportedKillTreePattern; Message = 'uses Process.Kill(true), which is unavailable on Windows PowerShell 5.1/.NET Framework. Use the portable process-stop helper instead.' }
    )) {
        foreach ($match in [regex]::Matches($content, $compatibilityPattern.Pattern)) {
            $line = [regex]::Matches($content.Substring(0, $match.Index), "`r`n|`r|`n").Count + 1
            $relative = Get-RepositoryRelativePath -Path $file.FullName
            $failures.Add("${relative}:$line $($compatibilityPattern.Message)")
        }
    }

    foreach ($match in [regex]::Matches($content, $wrappedConvertFromJsonPattern)) {
        $line = [regex]::Matches($content.Substring(0, $match.Index), "`r`n|`r|`n").Count + 1
        $relative = Get-RepositoryRelativePath -Path $file.FullName
        $failures.Add("${relative}:$line wraps ConvertFrom-Json in an array subexpression. Windows PowerShell 5.1 can preserve a JSON array as one returned object, producing a nested collection shape. Assign ConvertFrom-Json to a variable first, then iterate/materialize that parsed value explicitly.")
    }

    $countLookaheadLines = $content -split "`r`n|`r|`n"
    for ($candidateIndex = 0; $candidateIndex -lt $countLookaheadLines.Length; $candidateIndex++) {
        $assignmentMatch = [regex]::Match($countLookaheadLines[$candidateIndex], '^\s*\$(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*@\(')
        if (-not $assignmentMatch.Success) { continue }
        $variableName = $assignmentMatch.Groups['name'].Value
        $windowEnd = [Math]::Min($countLookaheadLines.Length - 1, $candidateIndex + 17)
        $window = ($countLookaheadLines[$candidateIndex..$windowEnd] -join "`n")
        if ($window -notmatch ('\$' + [regex]::Escape($variableName) + '\.Count\b')) { continue }

        $directFilteredArrayPipeline = [regex]::IsMatch(
            $countLookaheadLines[$candidateIndex],
            '^\s*\$[A-Za-z_][A-Za-z0-9_]*\s*=\s*@\([^)]*\)\s*\|\s*Where-Object')
        if (-not $directFilteredArrayPipeline) {
            $sawNestedArrayExpression = $false
            for ($lookaheadIndex = $candidateIndex + 1; $lookaheadIndex -le $windowEnd; $lookaheadIndex++) {
                $lookaheadLine = $countLookaheadLines[$lookaheadIndex]
                if ($lookaheadLine -match '^\s*@\(') { $sawNestedArrayExpression = $true }
                if ($lookaheadLine -match '^\s*\)\s*\|\s*Where-Object') {
                    $directFilteredArrayPipeline = -not $sawNestedArrayExpression
                    break
                }
            }
        }
        if (-not $directFilteredArrayPipeline) { continue }

        $relative = Get-RepositoryRelativePath -Path $file.FullName
        $countExpression = '$' + $variableName + '.Count'
        $failures.Add("${relative}:$($candidateIndex + 1) pipes an array expression through Where-Object and later relies on $countExpression without materializing the pipeline result. Architectural choice: wrap the whole filtered pipeline in outer @(...), or use an explicit generic collection, so Windows PowerShell 5.1 + StrictMode has stable zero/one/many-result collection semantics.")
    }

    $sourceLines = $content -split "`r`n|`r|`n"
    for ($lineIndex = 0; $lineIndex -lt $sourceLines.Length; $lineIndex++) {
        $sourceLine = $sourceLines[$lineIndex]
        if ([regex]::IsMatch($sourceLine, $readOnlyPlatformVariableAssignmentPattern)) {
            $relative = Get-RepositoryRelativePath -Path $file.FullName
            $failures.Add("${relative}:$($lineIndex + 1) assigns to a PowerShell 7 read-only platform automatic variable (IsWindows/IsLinux/IsMacOS/IsCoreCLR). Use a repository-specific variable name such as runningOnWindows instead; PowerShell variable names are case-insensitive.")
        }
        foreach ($quoted in [regex]::Matches($sourceLine, '(["''])(?<value>[^"'']*\\[^"'']*)\1')) {
            $value = $quoted.Groups['value'].Value
            if (-not [regex]::IsMatch($value, $repositoryBackslashPathLiteralPattern)) { continue }
            $relative = Get-RepositoryRelativePath -Path $file.FullName
            $failures.Add("${relative}:$($lineIndex + 1) contains a repository-relative path literal with Windows-only backslash separators: '$value'. Use '/' inside repository-relative literals; PowerShell accepts forward slashes on Windows and pwsh on macOS/Linux resolves them consistently.")
        }
        if ($sourceLine.IndexOf('Join-Path', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) { continue }
        foreach ($quoted in [regex]::Matches($sourceLine, '(["''])(?<value>[^"'']*\\[^"'']*)\1')) {
            $value = $quoted.Groups['value'].Value
            # A Join-Path call can share a line with a regex literal. Only path-like literals are rejected here.
            if ($value.StartsWith('[', [System.StringComparison]::Ordinal) -or $value.IndexOf('(?:', [System.StringComparison]::Ordinal) -ge 0) { continue }
            $relative = Get-RepositoryRelativePath -Path $file.FullName
            $failures.Add("${relative}:$($lineIndex + 1) passes a backslash-delimited path literal to Join-Path. Use '/' or nested Join-Path calls so pwsh on macOS/Linux resolves the same path.")
        }
    }
}


# Release builds can live on an external volume only if the expensive tool caches and temp state
# follow the repository. Keep this contract in the early compatibility preflight so a future edit
# cannot silently move NuGet, npm, DocFX, Chromium profiles, packaging stages, or dotnet CLI state
# back to the system volume.
$buildStorageHelperPath = Join-Path $root 'build/RepositoryBuildStorage.Common.ps1'
$releaseEntryPath = Join-Path $root 'Build-Release.ps1'
$documentationEntryPath = Join-Path $root 'build/Build-Documentation.ps1'
$nodeRuntimePath = Join-Path $root 'build/NodeRuntime.Common.ps1'
foreach ($requiredPath in @($buildStorageHelperPath, $releaseEntryPath, $documentationEntryPath, $nodeRuntimePath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        $failures.Add("repository build-storage contract file is missing: $requiredPath")
    }
}
if ((Test-Path -LiteralPath $buildStorageHelperPath -PathType Leaf) -and
    (Test-Path -LiteralPath $releaseEntryPath -PathType Leaf) -and
    (Test-Path -LiteralPath $documentationEntryPath -PathType Leaf) -and
    (Test-Path -LiteralPath $nodeRuntimePath -PathType Leaf)) {
    $buildStorageHelperText = [IO.File]::ReadAllText($buildStorageHelperPath)
    foreach ($requiredToken in @(
        'FUTURE2_BUILD_STORAGE_ROOT',
        'FUTURE2_DOCUMENTATION_CACHE_ROOT',
        'DOTNET_CLI_HOME',
        'NUGET_PACKAGES',
        'NUGET_HTTP_CACHE_PATH',
        'NUGET_PLUGINS_CACHE_PATH',
        'NUGET_SCRATCH',
        'NPM_CONFIG_CACHE',
        'XDG_CACHE_HOME',
        'TMPDIR',
        '$env:TMP =',
        '$env:TEMP ='
    )) {
        if ($buildStorageHelperText.IndexOf($requiredToken, [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add("build/RepositoryBuildStorage.Common.ps1 no longer redirects '$requiredToken'. Architectural repair: keep heavy release-build cache/temp state under the repository (or FUTURE2_BUILD_STORAGE_ROOT) so external-volume builds do not consume the system partition.")
        }
    }

    $releaseEntryText = [IO.File]::ReadAllText($releaseEntryPath)
    $releaseInitIndex = $releaseEntryText.IndexOf('Initialize-Future2RepositoryBuildStorage', [System.StringComparison]::Ordinal)
    $releaseWorkIndex = $releaseEntryText.IndexOf('if ($CompileOnly)', [System.StringComparison]::Ordinal)
    if ($releaseInitIndex -lt 0 -or ($releaseWorkIndex -ge 0 -and $releaseInitIndex -gt $releaseWorkIndex)) {
        $failures.Add('Build-Release.ps1 must initialize repository build storage before compile/release work. Architectural repair: dot-source build/RepositoryBuildStorage.Common.ps1 immediately after resolving the repository root and initialize it before invoking dotnet/npm/DocFX/native packaging.')
    }

    $documentationEntryText = [IO.File]::ReadAllText($documentationEntryPath)
    if ($documentationEntryText.IndexOf('Initialize-Future2RepositoryBuildStorage', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('build/Build-Documentation.ps1 must initialize repository build storage for standalone documentation builds; otherwise Chromium profiles, DocFX/NuGet state, and temp files can fall back to the system partition.')
    }

    # Search source text literally. Do not use a double-quoted string containing a source-code
    # variable name here: under StrictMode PowerShell would try to resolve that variable in this
    # maintenance script instead of looking for its literal text.
    $browserProfileNeedle = 'Join-Path $documentationToolCacheRoot ''browser-profiles'''
    if ($documentationEntryText.IndexOf($browserProfileNeedle, [System.StringComparison]::Ordinal) -lt 0 -or
        $documentationEntryText.IndexOf('DocumentationBrowserProfiles', [System.StringComparison]::Ordinal) -ge 0) {
        $failures.Add('build/Build-Documentation.ps1 must place isolated browser profiles inside the repository-owned documentation cache. Architectural repair: use $documentationToolCacheRoot/browser-profiles instead of the operating-system temp directory.')
    }

    $defaultStorageNeedle = 'Join-Path $repository ''artifacts/.build-storage'''
    if ($buildStorageHelperText.IndexOf($defaultStorageNeedle, [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('build/RepositoryBuildStorage.Common.ps1 must provide a zero-configuration default at <repository>/artifacts/.build-storage when FUTURE2_BUILD_STORAGE_ROOT is unset. Environment variables are optional overrides, not prerequisites for a normal clone.')
    }

    $gitIgnorePath = Join-Path $root '.gitignore'
    if (-not (Test-Path -LiteralPath $gitIgnorePath -PathType Leaf)) {
        $failures.Add('repository .gitignore is missing; repository-local build storage must never become source-controlled content.')
    }
    else {
        $gitIgnoreText = [IO.File]::ReadAllText($gitIgnorePath)
        if ($gitIgnoreText.IndexOf('artifacts/', [System.StringComparison]::Ordinal) -lt 0 -and
            $gitIgnoreText.IndexOf('artifacts/.build-storage/', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('.gitignore must ignore artifacts/ (or at minimum artifacts/.build-storage/) so the zero-configuration build cache remains local to each developer/build machine.')
        }
    }

    $nodeRuntimeText = [IO.File]::ReadAllText($nodeRuntimePath)
    $fallbackIndex = $nodeRuntimeText.IndexOf('if (-not [string]::IsNullOrWhiteSpace($FallbackRoot))', [System.StringComparison]::Ordinal)
    $localDataIndex = $nodeRuntimeText.IndexOf('$localApplicationData = [Environment]::GetFolderPath', [System.StringComparison]::Ordinal)
    if ($fallbackIndex -lt 0 -or $localDataIndex -lt 0 -or $fallbackIndex -gt $localDataIndex) {
        $failures.Add('build/NodeRuntime.Common.ps1 must prefer its repository fallback cache before LocalApplicationData. Architectural repair: repository-local cache first, per-user cache only as a last-resort standalone fallback.')
    }
}

if ($failures.Count -gt 0) {
    throw "PowerShell compatibility validation failed:`n - $($failures -join "`n - ")"
}

Write-Host 'PowerShell compatibility validation passed for Windows PowerShell 5.1 and modern pwsh parser/runtime API usage, cross-platform path handling, and protected platform automatic-variable assignments.'
