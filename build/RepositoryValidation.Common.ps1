Set-StrictMode -Version Latest

function Get-LocalGptRepositoryRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

function Test-ExcludedRepositoryPath {
    param([Parameter(Mandatory)][string]$RelativePath)

    $normalized = $RelativePath.Replace('\', '/')
    return $normalized -match '(^|/)(\.git|\.vs|\.cr|\.idea|node_modules|artifacts|bin|obj|AppPackages|BundleArtifacts)(/|$)'
}

function Get-RelativePathPortable {
    param(
        [Parameter(Mandatory)][string]$BasePath,
        [Parameter(Mandatory)][string]$TargetPath
    )

    $method = [IO.Path].GetMethod('GetRelativePath', [Type[]]@([string], [string]))
    if ($method) {
        return [string]$method.Invoke($null, @($BasePath, $TargetPath))
    }

    $baseFull = [IO.Path]::GetFullPath($BasePath).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $targetFull = [IO.Path]::GetFullPath($TargetPath)
    $baseUri = [Uri]::new($baseFull)
    $targetUri = [Uri]::new($targetFull)
    return [Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('/', [IO.Path]::DirectorySeparatorChar)
}

function Get-MaintainedRepositoryFiles {
    param([Parameter(Mandatory)][string]$RepositoryRoot)

    Get-ChildItem -LiteralPath $RepositoryRoot -Recurse -File -Force |
        Where-Object {
            $relative = Get-RelativePathPortable -BasePath $RepositoryRoot -TargetPath $_.FullName
            -not (Test-ExcludedRepositoryPath -RelativePath $relative)
        } |
        Sort-Object { (Get-RelativePathPortable -BasePath $RepositoryRoot -TargetPath $_.FullName).Replace('\', '/') }
}

function Get-RepositorySourceFingerprint {
    param([Parameter(Mandatory)][string]$RepositoryRoot)

    $builder = [Text.StringBuilder]::new()
    foreach ($file in Get-MaintainedRepositoryFiles -RepositoryRoot $RepositoryRoot) {
        $relative = (Get-RelativePathPortable -BasePath $RepositoryRoot -TargetPath $file.FullName).Replace('\', '/')
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        [void]$builder.Append($relative).Append("`n").Append($hash).Append("`n")
    }

    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes($builder.ToString())
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
}
