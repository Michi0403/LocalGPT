param(
    [string]$DocumentationRoot = "",
    [string]$OutputArchive = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $repositoryRoot 'src\\LocalGPT\\LocalGPT.csproj'
if (-not (Test-Path -LiteralPath $projectFile -PathType Leaf)) {
    throw "LocalGPT project file was not found: $projectFile"
}
$projectText = [IO.File]::ReadAllText($projectFile)
$versionMatch = [regex]::Match($projectText, '<Version>\s*(?<Version>[^<]+?)\s*</Version>', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
if (-not $versionMatch.Success) { throw "LocalGPT source version could not be resolved from $projectFile" }
$expectedVersion = $versionMatch.Groups['Version'].Value.Trim()

if ([string]::IsNullOrWhiteSpace($DocumentationRoot)) {
    $releaseRoot = Join-Path $repositoryRoot 'src\\LocalGPT\\bin\\Release\\net10.0\\wwwroot\\help-docs'
    $debugRoot = Join-Path $repositoryRoot 'src\\LocalGPT\\bin\\Debug\\net10.0\\wwwroot\\help-docs'
    $DocumentationRoot = if (Test-Path -LiteralPath $releaseRoot -PathType Container) { $releaseRoot } else { $debugRoot }
}
$DocumentationRoot = [IO.Path]::GetFullPath($DocumentationRoot)
if (-not (Test-Path -LiteralPath $DocumentationRoot -PathType Container)) {
    throw "Generated LocalGPT documentation was not found: $DocumentationRoot"
}

if ([string]::IsNullOrWhiteSpace($OutputArchive)) {
    $OutputArchive = Join-Path $repositoryRoot '.github\\pages\\localgpt-kawaii-docs.zip'
}
$OutputArchive = [IO.Path]::GetFullPath($OutputArchive)
$validator = Join-Path $repositoryRoot ".github\scripts\prepare-pages-artifact.py"
if (-not (Test-Path -LiteralPath $validator -PathType Leaf)) {
    throw "Repository-root GitHub Pages validator was not found: $validator"
}

$python = Get-Command python -ErrorAction SilentlyContinue
if ($null -eq $python) { $python = Get-Command python3 -ErrorAction SilentlyContinue }
if ($null -eq $python) { throw "Python 3 is required to validate the GitHub Pages snapshot." }

$operationId = [Guid]::NewGuid().ToString("N")
$preparedRoot = Join-Path ([IO.Path]::GetTempPath()) ("LocalGPT-Pages-Prepared-" + $operationId)
$verificationRoot = Join-Path ([IO.Path]::GetTempPath()) ("LocalGPT-Pages-Verify-" + $operationId)
$temporaryArchive = "$OutputArchive.$operationId.tmp"
$backupArchive = "$OutputArchive.$operationId.backup"
$installed = $false

try {
    & $python.Source $validator --source $DocumentationRoot --output $preparedRoot --expected-version $expectedVersion
    if ($LASTEXITCODE -ne 0) {
        throw "Generated LocalGPT documentation did not pass the GitHub Pages validator for version $expectedVersion."
    }
    Remove-Item -LiteralPath (Join-Path $preparedRoot "github-pages-deployment.json") -Force -ErrorAction SilentlyContinue
    if (-not (Test-Path -LiteralPath (Join-Path $preparedRoot ".nojekyll") -PathType Leaf)) {
        [IO.File]::WriteAllText((Join-Path $preparedRoot ".nojekyll"), [string]::Empty, [Text.UTF8Encoding]::new($false))
    }

    $archiveDirectory = Split-Path -Parent $OutputArchive
    New-Item -ItemType Directory -Path $archiveDirectory -Force | Out-Null
    Add-Type -AssemblyName System.IO.Compression -ErrorAction SilentlyContinue
    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
    $sourceRoot = $preparedRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $files = @(Get-ChildItem -LiteralPath $sourceRoot -File -Recurse -Force | Sort-Object FullName)
    if ($files.Count -eq 0) { throw "Prepared documentation directory is empty: $sourceRoot" }

    Remove-Item -LiteralPath $temporaryArchive -Force -ErrorAction SilentlyContinue
    $archive = [IO.Compression.ZipFile]::Open($temporaryArchive, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in $files) {
            $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart([char[]]"\/").Replace('\', '/')
            if ([string]::IsNullOrWhiteSpace($relative) -or $relative.Split('/') -contains '..') {
                throw "Unsafe GitHub Pages snapshot source path: $($file.FullName)"
            }
            $written = $false
            $lastReadError = $null
            for ($attempt = 1; $attempt -le 4 -and -not $written; $attempt++) {
                $entry = $null
                try {
                    $input = [IO.File]::Open($file.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
                    try {
                        $entry = $archive.CreateEntry($relative, [IO.Compression.CompressionLevel]::Optimal)
                        $entry.LastWriteTime = [DateTimeOffset]::new(1980, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
                        $output = $entry.Open()
                        try { $input.CopyTo($output) } finally { $output.Dispose() }
                    } finally { $input.Dispose() }
                    $written = $true
                } catch {
                    $lastReadError = $_.Exception
                    if ($null -ne $entry) { try { $entry.Delete() } catch { } }
                    if ($attempt -lt 4) { Start-Sleep -Milliseconds (150 * $attempt) }
                }
            }
            if (-not $written) { throw "Could not add '$($file.FullName)' after 4 attempts: $($lastReadError.Message)" }
        }
    } finally { $archive.Dispose() }

    & $python.Source $validator --archive $temporaryArchive --output $verificationRoot --expected-version $expectedVersion
    if ($LASTEXITCODE -ne 0) { throw "The new LocalGPT Pages snapshot failed final validation for version $expectedVersion." }

    Remove-Item -LiteralPath $backupArchive -Force -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $OutputArchive -PathType Leaf) { [IO.File]::Move($OutputArchive, $backupArchive) }
    try {
        [IO.File]::Move($temporaryArchive, $OutputArchive)
        $installed = $true
    } catch {
        if (Test-Path -LiteralPath $backupArchive -PathType Leaf) { [IO.File]::Move($backupArchive, $OutputArchive) }
        throw
    }
    Remove-Item -LiteralPath $backupArchive -Force -ErrorAction SilentlyContinue
    Write-Host "Updated the single LocalGPT $expectedVersion GitHub Pages snapshot: $OutputArchive" -ForegroundColor Green
} finally {
    Remove-Item -LiteralPath $preparedRoot -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $verificationRoot -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $temporaryArchive -Force -ErrorAction SilentlyContinue
    if (-not $installed -and (Test-Path -LiteralPath $backupArchive -PathType Leaf) -and -not (Test-Path -LiteralPath $OutputArchive -PathType Leaf)) {
        [IO.File]::Move($backupArchive, $OutputArchive)
    }
}
