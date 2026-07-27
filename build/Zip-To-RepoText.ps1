[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateNotNullOrEmpty()]
    [string]$ZipSource,

    [Parameter()]
    [string]$OutputFile,

    [Parameter()]
    [switch]$KeepExtracted,

    [Parameter()]
    [switch]$IncludeGenerated,

    [Parameter()]
    [switch]$IncludePotentialSecrets,

    [Parameter()]
    [ValidateRange(0, 4096)]
    [int]$MaxFileSizeMB = 0,

    [Parameter()]
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workDirectory = (Get-Location).Path
$runId = [Guid]::NewGuid().ToString('N').Substring(0, 10)
$tempItems = [System.Collections.Generic.List[string]]::new()
$writer = $null

# Exact extension matches only. The original script used substring matching,
# which could wrongly exclude a text file merely because its name contained
# text such as ".db" or ".log" somewhere in the middle.
$excludedExtensions = [System.Collections.Generic.HashSet[string]]::new(
    [StringComparer]::OrdinalIgnoreCase
)
@(
    '.dll', '.exe', '.com', '.obj', '.bin', '.class', '.jar', '.war',
    '.so', '.dylib', '.a', '.lib', '.pdb', '.cache',
    '.zip', '.7z', '.rar', '.tar', '.gz', '.bz2', '.xz',
    '.png', '.jpg', '.jpeg', '.gif', '.webp', '.bmp', '.tif', '.tiff', '.ico',
    '.mp4', '.mkv', '.avi', '.mov', '.wmv', '.mp3', '.wav', '.flac', '.ogg',
    '.pdf', '.doc', '.docx', '.xls', '.xlsx', '.ppt', '.pptx',
    '.sqlite', '.sqlite3', '.db', '.bak', '.log',
    '.nupkg', '.snupkg', '.msix', '.appx',
    '.cer', '.crt', '.der', '.pfx', '.p12', '.key', '.pem',
    '.ttf', '.otf', '.woff', '.woff2', '.eot',
    '.jpgux', '.svgux', '.cssux', '.userux', '.csux', '.razorux'
) | ForEach-Object { [void]$excludedExtensions.Add($_) }

$generatedDirectoryNames = [System.Collections.Generic.HashSet[string]]::new(
    [StringComparer]::OrdinalIgnoreCase
)
@(
    '.git', '.svn', '.hg', '.vs',
    'bin', 'obj', 'node_modules', 'packages', 'TestResults',
    'artifacts', 'publish', 'coverage', '.coverage', '.cache'
) | ForEach-Object { [void]$generatedDirectoryNames.Add($_) }

$knownTextExtensions = [System.Collections.Generic.HashSet[string]]::new(
    [StringComparer]::OrdinalIgnoreCase
)
@(
    '.cs', '.csx', '.razor', '.cshtml', '.vb', '.fs', '.fsx',
    '.c', '.h', '.cpp', '.hpp', '.java', '.kt', '.kts',
    '.js', '.jsx', '.ts', '.tsx', '.mjs', '.cjs',
    '.py', '.rb', '.php', '.go', '.rs', '.swift',
    '.ps1', '.psm1', '.psd1', '.cmd', '.bat', '.sh', '.bash', '.zsh', '.fish',
    '.sql', '.graphql', '.gql', '.proto', '.http', '.rest',
    '.html', '.htm', '.css', '.scss', '.sass', '.less', '.svg',
    '.xml', '.xaml', '.json', '.jsonc', '.yaml', '.yml', '.toml',
    '.ini', '.cfg', '.conf', '.config', '.properties',
    '.props', '.targets', '.csproj', '.vbproj', '.fsproj', '.sln', '.slnx',
    '.md', '.markdown', '.txt', '.csv', '.tsv', '.ruleset', '.nuspec', '.resx',
    '.editorconfig', '.gitattributes', '.gitignore', '.dockerignore',
    '.npmrc', '.yarnrc', '.prettierrc', '.eslintrc'
) | ForEach-Object { [void]$knownTextExtensions.Add($_) }

$potentialSecretNames = [System.Collections.Generic.HashSet[string]]::new(
    [StringComparer]::OrdinalIgnoreCase
)
@(
    '.env', 'secrets.json', 'credentials.json', 'service-account.json',
    'id_rsa', 'id_ed25519'
) | ForEach-Object { [void]$potentialSecretNames.Add($_) }

function Resolve-ZipSource {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source
    )

    $uri = $null
    if ([Uri]::TryCreate($Source, [UriKind]::Absolute, [ref]$uri) -and
        ($uri.Scheme -eq 'http' -or $uri.Scheme -eq 'https')) {

        $leafName = [Uri]::UnescapeDataString([IO.Path]::GetFileName($uri.AbsolutePath))
        if ([string]::IsNullOrWhiteSpace($leafName) -or
            -not $leafName.EndsWith('.zip', [StringComparison]::OrdinalIgnoreCase)) {
            $leafName = "repository-$runId.zip"
        }

        $downloadPath = Join-Path ([IO.Path]::GetTempPath()) $leafName
        if (Test-Path -LiteralPath $downloadPath) {
            $downloadPath = Join-Path ([IO.Path]::GetTempPath()) "$runId-$leafName"
        }

        Write-Host "Downloading ZIP: $Source"
        Invoke-WebRequest -Uri $Source -OutFile $downloadPath
        $tempItems.Add($downloadPath)

        return [PSCustomObject]@{
            LocalPath   = (Resolve-Path -LiteralPath $downloadPath).Path
            DisplayName = $leafName
            Description = $Source
        }
    }

    $resolved = Resolve-Path -LiteralPath $Source -ErrorAction Stop
    $item = Get-Item -LiteralPath $resolved.Path -ErrorAction Stop
    if ($item.PSIsContainer) {
        throw "ZipSource must point to a ZIP file, not a directory: $Source"
    }
    if (-not $item.Name.EndsWith('.zip', [StringComparison]::OrdinalIgnoreCase)) {
        throw "ZipSource must be a .zip file or an HTTP(S) URL ending in .zip: $Source"
    }

    return [PSCustomObject]@{
        LocalPath   = $item.FullName
        DisplayName = $item.Name
        Description = $item.FullName
    }
}

function Expand-ZipSafely {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ZipPath,

        [Parameter(Mandatory = $true)]
        [string]$Destination
    )

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    [IO.Directory]::CreateDirectory($Destination) | Out-Null
    $destinationFullPath = [IO.Path]::GetFullPath($Destination)
    $destinationPrefix = $destinationFullPath.TrimEnd(
        [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::AltDirectorySeparatorChar
    ) + [IO.Path]::DirectorySeparatorChar

    $archive = [IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        foreach ($entry in $archive.Entries) {
            $entryName = $entry.FullName.Replace('/', [IO.Path]::DirectorySeparatorChar)
            if ([string]::IsNullOrWhiteSpace($entryName)) {
                continue
            }

            $targetPath = [IO.Path]::GetFullPath((Join-Path $destinationFullPath $entryName))
            if (-not $targetPath.StartsWith($destinationPrefix, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Unsafe ZIP entry attempted to escape the extraction directory: $($entry.FullName)"
            }

            # Skip Unix symbolic links. A source bundle should contain file contents,
            # not links that could resolve outside the extracted tree.
            $unixFileType = (($entry.ExternalAttributes -shr 16) -band 0xF000)
            if ($unixFileType -eq 0xA000) {
                Write-Warning "Skipped symbolic link entry: $($entry.FullName)"
                continue
            }

            if ([string]::IsNullOrEmpty($entry.Name)) {
                [IO.Directory]::CreateDirectory($targetPath) | Out-Null
                continue
            }

            $targetDirectory = [IO.Path]::GetDirectoryName($targetPath)
            [IO.Directory]::CreateDirectory($targetDirectory) | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $targetPath, $true)
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Get-RepositoryRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ExtractionRoot
    )

    $children = @(
        Get-ChildItem -LiteralPath $ExtractionRoot -Force |
            Where-Object { $_.Name -ne '__MACOSX' }
    )

    $directories = @($children | Where-Object { $_.PSIsContainer })
    $files = @($children | Where-Object { -not $_.PSIsContainer })

    if ($directories.Count -eq 1 -and $files.Count -eq 0) {
        return $directories[0].FullName
    }

    return (Get-Item -LiteralPath $ExtractionRoot).FullName
}

function Get-RelativePathCompatible {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePath,

        [Parameter(Mandatory = $true)]
        [string]$TargetPath
    )

    $baseFullPath = [IO.Path]::GetFullPath($BasePath).TrimEnd(
        [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::AltDirectorySeparatorChar
    ) + [IO.Path]::DirectorySeparatorChar

    $baseUri = [Uri]$baseFullPath
    $targetUri = [Uri]([IO.Path]::GetFullPath($TargetPath))
    $relativeUri = $baseUri.MakeRelativeUri($targetUri)
    return [Uri]::UnescapeDataString($relativeUri.ToString()).Replace('\', '/')
}

function Test-IsUnderGeneratedDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [IO.FileInfo]$File,

        [Parameter(Mandatory = $true)]
        [string]$Root
    )

    if ($IncludeGenerated) {
        return $false
    }

    $relativePath = Get-RelativePathCompatible -BasePath $Root -TargetPath $File.FullName
    $segments = $relativePath -split '/'
    if ($segments.Count -le 1) {
        return $false
    }

    foreach ($segment in $segments[0..($segments.Count - 2)]) {
        if ($generatedDirectoryNames.Contains($segment)) {
            return $true
        }
    }

    return $false
}

function Try-ReadTextFile {
    param(
        [Parameter(Mandatory = $true)]
        [IO.FileInfo]$File
    )

    $bytes = [IO.File]::ReadAllBytes($File.FullName)
    if ($bytes.Length -eq 0) {
        return [PSCustomObject]@{ IsText = $true; Content = ''; Reason = $null }
    }

    $encoding = $null
    $offset = 0

    if ($bytes.Length -ge 3 -and
        $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        $encoding = [Text.UTF8Encoding]::new($false, $true)
        $offset = 3
    }
    elseif ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) {
        $encoding = [Text.Encoding]::Unicode
        $offset = 2
    }
    elseif ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFE -and $bytes[1] -eq 0xFF) {
        $encoding = [Text.Encoding]::BigEndianUnicode
        $offset = 2
    }
    else {
        $containsNull = $false
        foreach ($byte in $bytes) {
            if ($byte -eq 0) {
                $containsNull = $true
                break
            }
        }

        if ($containsNull -and -not $knownTextExtensions.Contains($File.Extension)) {
            return [PSCustomObject]@{
                IsText = $false
                Content = $null
                Reason = 'binary signature (NUL byte)'
            }
        }

        $encoding = [Text.UTF8Encoding]::new($false, $true)
    }

    try {
        $content = $encoding.GetString($bytes, $offset, $bytes.Length - $offset)
    }
    catch [Text.DecoderFallbackException] {
        if (-not $knownTextExtensions.Contains($File.Extension)) {
            return [PSCustomObject]@{
                IsText = $false
                Content = $null
                Reason = 'not valid UTF-8 and not a known text extension'
            }
        }

        # Preserve legacy Windows-encoded source/configuration files rather than
        # silently dropping them. StreamReader also detects a BOM when present.
        $reader = [IO.StreamReader]::new($File.FullName, [Text.Encoding]::Default, $true)
        try {
            $content = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }

    if (-not $knownTextExtensions.Contains($File.Extension)) {
        $controlCount = 0
        foreach ($character in $content.ToCharArray()) {
            $code = [int][char]$character
            if ($code -lt 32 -and $character -notin @("`t", "`r", "`n", "`f")) {
                $controlCount++
            }
        }

        if ($content.Length -gt 0 -and ($controlCount / $content.Length) -gt 0.01) {
            return [PSCustomObject]@{
                IsText = $false
                Content = $null
                Reason = 'binary-like control-character ratio'
            }
        }
    }

    return [PSCustomObject]@{ IsText = $true; Content = $content; Reason = $null }
}

try {
    $resolvedSource = Resolve-ZipSource -Source $ZipSource
    $zipBaseName = [IO.Path]::GetFileNameWithoutExtension($resolvedSource.DisplayName)

    if ([string]::IsNullOrWhiteSpace($OutputFile)) {
        # Correct default filename. The original script calculated repoastext.txt
        # but ignored it and wrote "$OutputDirectory.txt", producing LocalGPT.txt.
        $OutputFile = Join-Path $workDirectory "$zipBaseName-repoastext.txt"
    }
    elseif (-not [IO.Path]::IsPathRooted($OutputFile)) {
        $OutputFile = Join-Path $workDirectory $OutputFile
    }

    $OutputFile = [IO.Path]::GetFullPath($OutputFile)
    $outputParent = [IO.Path]::GetDirectoryName($OutputFile)
    [IO.Directory]::CreateDirectory($outputParent) | Out-Null

    if ((Test-Path -LiteralPath $OutputFile) -and -not $Force) {
        throw "Output file already exists: $OutputFile. Use -Force or choose -OutputFile."
    }

    if ($KeepExtracted) {
        $extractionRoot = Join-Path $workDirectory "$zipBaseName-extracted-$runId"
    }
    else {
        $extractionRoot = Join-Path ([IO.Path]::GetTempPath()) "RepoAsText-$runId"
        $tempItems.Add($extractionRoot)
    }

    Write-Host "Extracting $($resolvedSource.LocalPath)"
    Expand-ZipSafely -ZipPath $resolvedSource.LocalPath -Destination $extractionRoot
    $repositoryRoot = Get-RepositoryRoot -ExtractionRoot $extractionRoot

    $candidateFiles = @(
        Get-ChildItem -LiteralPath $repositoryRoot -Recurse -Force -File |
            Where-Object { -not (Test-IsUnderGeneratedDirectory -File $_ -Root $repositoryRoot) } |
            Where-Object { -not $excludedExtensions.Contains($_.Extension) } |
            Where-Object {
                $IncludePotentialSecrets -or
                -not $potentialSecretNames.Contains($_.Name)
            } |
            Where-Object {
                if ($MaxFileSizeMB -le 0) {
                    return $true
                }

                return $_.Length -le ($MaxFileSizeMB * 1MB)
            } |
            Where-Object {
                [IO.Path]::GetFullPath($_.FullName) -ne $OutputFile
            } |
            Sort-Object FullName
    )

    $utf8NoBom = [Text.UTF8Encoding]::new($false)
    $writer = [IO.StreamWriter]::new($OutputFile, $false, $utf8NoBom)

    $includedCount = 0
    $skippedReadCount = 0

    $writer.WriteLine('# Repository Text Bundle')
    $writer.WriteLine("Source ZIP: $($resolvedSource.Description)")
    $writer.WriteLine("Archive root: $([IO.Path]::GetFileName($repositoryRoot))")
    $writer.WriteLine("Generated: $(Get-Date -Format o)")
    $writer.WriteLine("Candidate files: $($candidateFiles.Count)")
    $writer.WriteLine('')
    $writer.WriteLine('Excluded by default: generated/cache directories, compiled binaries, archives, databases, certificates, media, office documents, logs, and likely secret files.')
    $writer.WriteLine('Use -IncludeGenerated or -IncludePotentialSecrets only when you explicitly need those files.')
    if ($MaxFileSizeMB -gt 0) {
        $writer.WriteLine("Maximum included file size: $MaxFileSizeMB MB")
    }
    else {
        $writer.WriteLine('Maximum included file size: unlimited')
    }
    $writer.WriteLine('')

    for ($index = 0; $index -lt $candidateFiles.Count; $index++) {
        $file = $candidateFiles[$index]
        $percent = if ($candidateFiles.Count -eq 0) { 100 } else {
            [int](($index + 1) * 100 / $candidateFiles.Count)
        }
        Write-Progress -Activity 'Creating repository text bundle' -Status $file.Name -PercentComplete $percent

        $relativePath = Get-RelativePathCompatible -BasePath $repositoryRoot -TargetPath $file.FullName
        $readResult = Try-ReadTextFile -File $file

        $writer.WriteLine('')
        $writer.WriteLine('================================================================================')
        $writer.WriteLine("FILE: $relativePath")
        $writer.WriteLine("SIZE: $($file.Length) bytes")
        $writer.WriteLine('================================================================================')
        $writer.WriteLine('')

        if ($readResult.IsText) {
            $writer.WriteLine($readResult.Content)
            $includedCount++
            Write-Verbose "Included: $relativePath"
        }
        else {
            $writer.WriteLine("[SKIPPED: $($readResult.Reason)]")
            $skippedReadCount++
            Write-Warning "Skipped unreadable/binary-looking file: $relativePath ($($readResult.Reason))"
        }
    }

    Write-Progress -Activity 'Creating repository text bundle' -Completed
    $writer.Flush()
    $writer.Dispose()
    $writer = $null

    Write-Host ''
    Write-Host "Done: $OutputFile"
    Write-Host "Included text files: $includedCount"
    Write-Host "Skipped after content inspection: $skippedReadCount"
    if ($KeepExtracted) {
        Write-Host "Extracted source kept at: $extractionRoot"
    }

    [PSCustomObject]@{
        OutputFile       = $OutputFile
        SourceZip        = $resolvedSource.Description
        RepositoryRoot   = $repositoryRoot
        IncludedFiles    = $includedCount
        SkippedOnRead    = $skippedReadCount
        ExtractedPath    = if ($KeepExtracted) { $extractionRoot } else { $null }
    }
}
finally {
    if ($null -ne $writer) {
        $writer.Dispose()
    }

    foreach ($tempItem in $tempItems) {
        if (Test-Path -LiteralPath $tempItem) {
            Remove-Item -LiteralPath $tempItem -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
