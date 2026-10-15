param(
    [Parameter(Mandatory)][string]$PackagePath,
    [string]$RepositoryRoot = ""
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
}
$root = [IO.Path]::GetFullPath($RepositoryRoot)
if (-not (Test-Path -LiteralPath $PackagePath -PathType Leaf)) { throw "Release packaging NuGet package not found: $PackagePath" }
$expectedEntries = @{
    'licenses/THIRD-PARTY-NOTICES.txt' = (Join-Path $root 'THIRD-PARTY-NOTICES.txt')
    'licenses/PDFsharp-6.2.4-MIT-LICENSE.txt' = (Join-Path $root 'third_party/PDFsharp/LICENSE')
}
Add-Type -AssemblyName System.IO.Compression -ErrorAction SilentlyContinue
Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
$archive = [IO.Compression.ZipFile]::OpenRead($PackagePath)
try {
    foreach ($entryName in $expectedEntries.Keys) {
        $entry = $archive.GetEntry($entryName)
        if ($null -eq $entry -or $entry.Length -le 0) { throw "NuGet tool is missing its required license entry: $entryName" }
        $stream = $entry.Open()
        try {
            $sha = [Security.Cryptography.SHA256]::Create()
            try { $packedHash = ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '') }
            finally { $sha.Dispose() }
        }
        finally { $stream.Dispose() }
        $sourceHash = (Get-FileHash -LiteralPath $expectedEntries[$entryName] -Algorithm SHA256).Hash
        if (-not [string]::Equals($sourceHash, $packedHash, [StringComparison]::OrdinalIgnoreCase)) {
            throw "NuGet tool has a stale or modified third-party license entry: $entryName. Repack from the current source."
        }
    }
}
finally { $archive.Dispose() }
Write-Host 'Release-packaging NuGet notice entries match the original source files (SHA-256).' -ForegroundColor Green
