<#
.SYNOPSIS
Removes generated build files and checkout-local caches.
.DESCRIPTION
Run after builds finish. Use -WhatIf to preview. Finished releases, package
feeds, documentation snapshots, and conventional .NET publish folders are
outside the cleanup scope. If you supplied a custom release output root that
overlaps a cleanup folder, pass the same path with -ReleaseOutputRoot.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string[]]$ReleaseOutputRoot = @()
)

$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath($PSScriptRoot)
$separator = [IO.Path]::DirectorySeparatorChar
$comparison = [StringComparison]::OrdinalIgnoreCase
$rootPrefix = $repository.TrimEnd($separator) + $separator
$git = (Get-Command git -CommandType Application -TotalCount 1).Source
$gitRoot = & $git -C $repository rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0 -or -not [string]::Equals([IO.Path]::GetFullPath($gitRoot), $repository, $comparison)) {
    throw 'Run this script from a Git checkout containing the repository build scripts.'
}

$trackedFiles = @(& $git -C $repository -c core.quotePath=false ls-files)
if ($LASTEXITCODE -ne 0) {
    throw 'Could not read the Git index for cleanup.'
}

# Index file and directory names once, including the parent of every tracked file.
$trackedPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($file in $trackedFiles) {
    $entry = $file
    while ($trackedPaths.Add($entry)) {
        $slash = $entry.LastIndexOf('/')
        if ($slash -lt 0) {
            break
        }
        $entry = $entry.Substring(0, $slash)
    }
}

$protectedRoots = foreach ($path in ($ReleaseOutputRoot + @($env:FUTURE2_RELEASE_OUTPUT_ROOT))) {
    if (-not [string]::IsNullOrWhiteSpace($path)) {
        $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($path).TrimEnd($separator)
    }
}

$targets = @(
    'docs/_site'
    'docs/input'
    'docs/.tools'
    'docs/.print-book'
    'artifacts/.build-storage/documentation'
    'artifacts/.build-storage/tmp'
    'artifacts/.build-storage/nuget/packages'
    'artifacts/.build-storage/nuget/http-cache'
    'artifacts/.build-storage/nuget/plugins-cache'
    'artifacts/.build-storage/nuget/scratch'
    'artifacts/.build-storage/npm-cache'
    'artifacts/.build-storage/xdg-cache'
    'src/LocalGPT/wwwroot/help-docs'
    'artifacts/development/.nuget-packages'
)

$projectFolders = $trackedFiles |
    Where-Object { $_ -match '\.(csproj|fsproj|vbproj)$' } |
    ForEach-Object { [IO.Path]::GetDirectoryName($_).Replace('\', '/') } |
    Sort-Object -Unique
foreach ($folder in $projectFolders) {
    $prefix = if ($folder) { "$folder/" } else { '' }
    $targets += "$($prefix)bin", "$($prefix)obj"
}

function Test-ReparsePoint {
    param([string]$Path)

    return [bool]([IO.File]::GetAttributes($Path) -band [IO.FileAttributes]::ReparsePoint)
}

function Get-ProtectedEntry {
    param([string]$Path)

    # Inspect every entry before descending, including file symlinks.
    $pending = [Collections.Generic.Stack[string]]::new()
    $pending.Push($Path)
    while ($pending.Count -gt 0) {
        foreach ($entry in [IO.Directory]::EnumerateFileSystemEntries($pending.Pop())) {
            $attributes = [IO.File]::GetAttributes($entry)
            if ($attributes -band [IO.FileAttributes]::ReparsePoint) {
                return $entry
            }
            if ($attributes -band [IO.FileAttributes]::Directory) {
                if ([IO.Path]::GetFileName($entry) -ieq 'publish') {
                    return $entry
                }
                $pending.Push($entry)
            }
        }
    }
}

if (Test-ReparsePoint $repository) {
    throw "The checkout root is a filesystem link: $repository"
}

# Keep authored Markdown alongside the generated API metadata.
$apiPath = Join-Path $repository 'docs/api'
$docsPath = Join-Path $repository 'docs'
if ((Test-Path -LiteralPath $docsPath -PathType Container) -and -not (Test-ReparsePoint $docsPath) -and
    (Test-Path -LiteralPath $apiPath -PathType Container) -and -not (Test-ReparsePoint $apiPath)) {
    $targets += @(Get-ChildItem -LiteralPath $apiPath -Filter '*.yml' -File -Force |
        ForEach-Object { 'docs/api/' + $_.Name })
}

foreach ($relative in ($targets | Sort-Object -Unique)) {
    $target = [IO.Path]::GetFullPath((Join-Path $repository $relative))
    if (-not $target.StartsWith($rootPrefix, $comparison)) {
        throw "Cleanup target is outside the checkout: $target"
    }
    if ($trackedPaths.Contains($relative)) {
        Write-Warning "Skipping $relative because it contains tracked files."
        continue
    }
    if ($protectedRoots | Where-Object {
        [string]::Equals($target, $_, $comparison) -or
        $_.StartsWith($target + $separator, $comparison) -or
        $target.StartsWith($_ + $separator, $comparison)
    } | Select-Object -First 1) {
        Write-Warning "Skipping $relative because it overlaps a custom release output."
        continue
    }

    $current = $repository
    $linkedPath = $null
    foreach ($part in $relative.Split('/')) {
        $current = Join-Path $current $part
        if (-not (Test-Path -LiteralPath $current)) {
            break
        }
        if (Test-ReparsePoint $current) {
            $linkedPath = $current
            break
        }
    }
    if ($linkedPath) {
        Write-Warning "Skipping $relative because it crosses a filesystem link: $linkedPath"
        continue
    }
    if (-not (Test-Path -LiteralPath $target)) {
        continue
    }

    $isDirectory = Test-Path -LiteralPath $target -PathType Container
    if ($isDirectory) {
        $protectedEntry = Get-ProtectedEntry $target
        if ($protectedEntry) {
            Write-Warning "Skipping $relative because it contains a filesystem link or publish directory: $protectedEntry"
            continue
        }
    }
    if ($PSCmdlet.ShouldProcess($target, 'Remove generated build output')) {
        Remove-Item -LiteralPath $target -Force -Recurse:$isDirectory
    }
}
