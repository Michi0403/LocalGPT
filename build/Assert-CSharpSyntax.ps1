param(
    [string]$SourceRoot = 'src'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RepositoryValidation.Common.ps1')

$repoRoot = Get-LocalGptRepositoryRoot
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw 'The .NET SDK is required for Roslyn syntax validation. Install the SDK pinned by global.json.'
}

$basePathLine = (& dotnet --info) | Where-Object { $_ -match '^\s*Base Path:\s*(.+)$' } | Select-Object -First 1
if (-not $basePathLine -or $basePathLine -notmatch '^\s*Base Path:\s*(.+)$') {
    throw 'Could not resolve the active .NET SDK Base Path from dotnet --info.'
}

$sdkBasePath = $Matches[1].Trim()
$roslynPath = Join-Path $sdkBasePath 'Roslyn/bincore'
$codeAnalysis = Join-Path $roslynPath 'Microsoft.CodeAnalysis.dll'
$csharpAnalysis = Join-Path $roslynPath 'Microsoft.CodeAnalysis.CSharp.dll'
if (-not (Test-Path -LiteralPath $codeAnalysis) -or -not (Test-Path -LiteralPath $csharpAnalysis)) {
    throw "Roslyn compiler assemblies were not found under $roslynPath."
}

Add-Type -Path $codeAnalysis
Add-Type -Path $csharpAnalysis

$sourcePath = Join-Path $repoRoot $SourceRoot
if (-not (Test-Path -LiteralPath $sourcePath -PathType Container)) {
    throw "C# source root does not exist: $SourceRoot"
}

$parseOptions = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithLanguageVersion(
    [Microsoft.CodeAnalysis.CSharp.LanguageVersion]::Preview)
$errors = [Collections.Generic.List[string]]::new()
$files = Get-ChildItem -LiteralPath $sourcePath -Recurse -File -Filter '*.cs' |
    Where-Object {
        $relative = Get-RelativePathPortable -BasePath $repoRoot -TargetPath $_.FullName
        -not (Test-ExcludedRepositoryPath -RelativePath $relative)
    }

foreach ($file in $files) {
    $relative = (Get-RelativePathPortable -BasePath $repoRoot -TargetPath $file.FullName).Replace('\', '/')
    $source = Get-Content -LiteralPath $file.FullName -Raw
    $tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText(
        $source,
        $parseOptions,
        $relative,
        [Text.Encoding]::UTF8)

    foreach ($diagnostic in $tree.GetDiagnostics()) {
        if ($diagnostic.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Error) {
            $errors.Add($diagnostic.ToString())
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Error ("C# Roslyn syntax validation failed:`n" + ($errors -join "`n"))
}

Write-Host "C# Roslyn syntax validation passed for $($files.Count) source files."
