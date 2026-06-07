param(
    [string]$RepoUrl = "https://github.com/Michi0403/LocalGPT.git",
    [switch]$KeepClone
)
$length = 10
$env:GIT_REDIRECT_STDERR = '2>&1'

$characters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'.ToCharArray()
$randomi = -join ($characters | Get-Random -Count $length)

$randomString = $randomi.ToString()
$ErrorActionPreference = "Stop"

function Test-CommandExists {
    param([string]$Name)
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

if (-not (Test-CommandExists git)) {
    throw "git.exe was not found. Install Git for Windows first: https://git-scm.com/download/win"
}

$RepoDir = Join-Path $randomString "LocalGPT"
$WorkDir = (Get-Location).Path
$OutputDirectory = Join-Path $WorkDir $RepoDir 
$RepoDirOuter = Join-Path $WorkDir $randomString
$OutputFile = Join-Path $OutputDirectory "repoastext"
if (Test-Path $RepoDir) {
    Remove-Item $RepoDir -Recurse -Force
}
Write-Host "Cloning $RepoUrl ..."
git clone $RepoUrl $randomString

$ExcludeExtensions = @(
    ".dll", ".exe", ".pdb", ".obj", ".bin", ".zip", ".7z", ".rar", ".tar", ".gz",
    ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico", ".svgz", ".mp4", ".mp3", ".wav",
    ".pdf", ".docx", ".xlsx", ".pptx", ".sqlite", ".db", ".bak", ".log",
    ".nupkg", ".snupkg", ".msix", ".appx", ".cer", ".pfx", ".key", ".jpgux", ".svgux", ".cssux",
    ".cssux", ".userux", ".csux", ".razorux", ".snupkg", ".snupkg"
)


function Is-TextWanted {
    param([System.IO.FileInfo]$File)

    $name = $File.Name.ToLowerInvariant()
    $name = $File.Name.ToLowerInvariant()
    $ext  = $File.Extension.ToLowerInvariant()

    if ($name -like "*.png*" -or $ext -eq ".png") {
        Write-Host "DEBUG PNG:"
        Write-Host "  FullName:  [$($File.FullName)]"
        Write-Host "  Name:      [$name]"
        Write-Host "  Extension:[$ext]"
        Write-Host "  Excludes contains .png? [$($ExcludeExtensions -contains '.png')]"

        foreach ($extension in $ExcludeExtensions) {
            Write-Host "  Testing [$extension] against [$name] -> [$($name -like "*$extension*")]"
        }
    }
 
    foreach ($extension in $ExcludeExtensions) {
        if ($name -like "*$extension*") {
            Write-Host "SKIP: $($File.Name) matched [$extension]"
            return $false
        }
    }
    Write-Host "KEEP: $($File.Name) Extension=[$ext]"
    return $true
}


$files = Get-ChildItem -Path $RepoDirOuter -Recurse -File |
    Where-Object { Is-TextWanted $_ } |
#    Where-Object { -not (Looks-Binary $_) } |
    Sort-Object FullName
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$writer = New-Object System.IO.StreamWriter($OutputDirectory, $false, $utf8NoBom)

try {


    $writer.WriteLine("# LocalGPT Repository Debug Bundle")
    Write-Host "# LocalGPT Repository Debug Bundle"
    $writer.WriteLine("Repository: $RepoUrl")
    $writer.WriteLine("Generated: $(Get-Date -Format o)")
    Write-Host "Generated: $(Get-Date -Format o)"
    $writer.WriteLine("Included files: $($files.Count)")
    Write-Host "Included files: $($files.Count)"
    $writer.WriteLine("")
    Write-Host ""
    $writer.WriteLine("This bundle intentionally excludes binaries, build outputs, NuGet packages, caches, databases, certificates, logs, media, and oversized files.")
    Write-Host "This bundle intentionally excludes binaries, build outputs, NuGet packages, caches, databases, certificates, logs, media, and oversized files."
    $writer.WriteLine("")
    Write-Host ""

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
            Write-Host "$content"
            $writer.WriteLine($content)
        }
        catch {
            Write-Host "[SKIPPED: could not read as text: $($_.Exception.Message)]"
            $writer.WriteLine("[SKIPPED: could not read as text: $($_.Exception.Message)]")
        }
    }
}
finally {
    $writer.Dispose()
}

Write-Host "Done: $RepoDir"
