param(
    [string]$RepoUrl = "https://github.com/Michi0403/LocalGPT.git",
    [string]$WorkDir = "$env:TEMP\LocalGPT_repo_export",
    [string]$OutputFile = "$PWD\LocalGPT_repository_debug_bundle.txt",
    [switch]$KeepClone
)

$ErrorActionPreference = "Stop"

function Test-CommandExists {
    param([string]$Name)
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

if (-not (Test-CommandExists git)) {
    throw "git.exe was not found. Install Git for Windows first: https://git-scm.com/download/win"
}

$RepoDir = Join-Path $WorkDir "LocalGPT"

if (Test-Path $WorkDir) {
    Remove-Item $WorkDir -Recurse -Force
}
New-Item -ItemType Directory -Path $WorkDir | Out-Null

Write-Host "Cloning $RepoUrl ..."
git clone --depth 1 $RepoUrl $RepoDir

$IncludeExtensions = @(
    ".cs", ".csproj", ".sln", ".slnx", ".razor", ".cshtml",
    ".json", ".config", ".xml", ".props", ".targets", ".pubxml",
    ".xaml", ".resw", ".resx", ".manifest", ".appxmanifest",
    ".ps1", ".psm1", ".cmd", ".bat", ".sh", ".yml", ".yaml",
    ".html", ".htm", ".css", ".scss", ".js", ".ts", ".mjs",
    ".md", ".txt", ".editorconfig", ".gitignore", ".gitattributes",
    ".props", ".targets", ".sql", ".http", ".env.example"
)

$IncludeExactNames = @(
    "Dockerfile", "Makefile", "global.json", "Directory.Build.props", "Directory.Build.targets",
    "NuGet.config", "README", "LICENSE", "AGENTS.md", "CLAUDE.md", "llms.txt"
)

$ExcludeDirNames = @(
    ".git", ".vs", ".vscode", "bin", "obj", "node_modules", "packages",
    "artifacts", "publish", "published", "Release", "Debug", "x64", "x86", "arm64",
    ".nuget", ".cache", ".idea", "TestResults", "coverage", "logs",
    "LocalState", "AppPackages", "BundleArtifacts", "PackageArtifacts"
)

$ExcludeExtensions = @(
    ".dll", ".exe", ".pdb", ".obj", ".bin", ".zip", ".7z", ".rar", ".tar", ".gz",
    ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico", ".svgz", ".mp4", ".mp3", ".wav",
    ".pdf", ".docx", ".xlsx", ".pptx", ".sqlite", ".db", ".bak", ".log",
    ".nupkg", ".snupkg", ".msix", ".appx", ".cer", ".pfx", ".key"
)

function Is-ExcludedPath {
    param([System.IO.FileInfo]$File)
    $relativeParts = $File.FullName.Substring($RepoDir.Length).TrimStart('\','/') -split '[\\/]'
    foreach ($part in $relativeParts) {
        if ($ExcludeDirNames -contains $part) { return $true }
    }
    return $false
}

function Is-TextWanted {
    param([System.IO.FileInfo]$File)

    if (Is-ExcludedPath $File) { return $false }
    if ($ExcludeExtensions -contains $File.Extension.ToLowerInvariant()) { return $false }
    if ($File.Length -gt 2MB) { return $false }

    $name = $File.Name
    if ($IncludeExactNames -contains $name) { return $true }
    if ($IncludeExtensions -contains $File.Extension.ToLowerInvariant()) { return $true }

    return $false
}

function Looks-Binary {
    param([System.IO.FileInfo]$File)
    $fs = [System.IO.File]::OpenRead($File.FullName)
    try {
        $max = [Math]::Min(4096, $File.Length)
        $buffer = New-Object byte[] $max
        $read = $fs.Read($buffer, 0, $max)
        for ($i = 0; $i -lt $read; $i++) {
            if ($buffer[$i] -eq 0) { return $true }
        }
        return $false
    }
    finally {
        $fs.Dispose()
    }
}

$files = Get-ChildItem -Path $RepoDir -Recurse -File |
    Where-Object { Is-TextWanted $_ } |
    Where-Object { -not (Looks-Binary $_) } |
    Sort-Object FullName

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$writer = New-Object System.IO.StreamWriter($OutputFile, $false, $utf8NoBom)

try {
    $commit = git -C $RepoDir rev-parse HEAD
    $branch = git -C $RepoDir rev-parse --abbrev-ref HEAD

    $writer.WriteLine("# LocalGPT Repository Debug Bundle")
    $writer.WriteLine("Repository: $RepoUrl")
    $writer.WriteLine("Branch: $branch")
    $writer.WriteLine("Commit: $commit")
    $writer.WriteLine("Generated: $(Get-Date -Format o)")
    $writer.WriteLine("Included files: $($files.Count)")
    $writer.WriteLine("")
    $writer.WriteLine("This bundle intentionally excludes binaries, build outputs, NuGet packages, caches, databases, certificates, logs, media, and oversized files.")
    $writer.WriteLine("")

    foreach ($file in $files) {
        $rel = $file.FullName.Substring($RepoDir.Length).TrimStart('\','/') -replace '\\','/'
        Write-Host "Adding $rel"
        $writer.WriteLine("")
        $writer.WriteLine("================================================================================")
        $writer.WriteLine("FILE: $rel")
        $writer.WriteLine("SIZE: $($file.Length) bytes")
        $writer.WriteLine("================================================================================")
        $writer.WriteLine("")

        try {
            $content = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction Stop
            $writer.WriteLine($content)
        }
        catch {
            $writer.WriteLine("[SKIPPED: could not read as text: $($_.Exception.Message)]")
        }
    }
}
finally {
    $writer.Dispose()
}

Write-Host "Done: $OutputFile"

if (-not $KeepClone) {
    Remove-Item $WorkDir -Recurse -Force -ErrorAction SilentlyContinue
}
