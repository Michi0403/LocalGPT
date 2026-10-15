param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Native PowerShell repository check. The former wrapper invoked two Python parsers and
# vanished during the PowerShell migration, leaving Invoke-RepositoryValidation broken.
# This validates file-level coverage without pretending to replace Roslyn/Razor semantic
# analysis. Compiler XML and DocFX are checked by the actual build/documentation targets.
$sourceRoot = Join-Path ([IO.Path]::GetFullPath($RepositoryRoot)) 'src'
if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) {
    throw "LocalGPT source directory was not found: $sourceRoot"
}

$errors = [Collections.Generic.List[string]]::new()
$csharpCount = 0
$razorCount = 0
$summaryPattern = '(?m)^\s*///\s*<summary>'

foreach ($file in Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.cs') {
    if ($file.FullName -match '[\\/](?:bin|obj|Migrations)[\\/]' -or $file.Name -match '\.(?:g|Designer)\.cs$') { continue }
    $text = [IO.File]::ReadAllText($file.FullName)
    if ($text -notmatch '(?m)^\s*(?:public|internal)\s') { continue }
    $csharpCount++
    if ($text -notmatch $summaryPattern) {
        $errors.Add("Maintained C# source declares an exposed symbol without any XML summary: $($file.FullName)")
    }
}

foreach ($file in Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.razor') {
    $text = [IO.File]::ReadAllText($file.FullName)
    if ($text -notmatch '@code\s*\{') { continue }
    $razorCount++
    if ($text -notmatch $summaryPattern) {
        $errors.Add("Razor component @code block has no XML summary: $($file.FullName)")
    }
}

if ($csharpCount -eq 0 -or $razorCount -eq 0) {
    $errors.Add("Documentation coverage audit found an unexpectedly empty C# or Razor source set (C#=$csharpCount, Razor=$razorCount).")
}
if ($errors.Count -gt 0) {
    foreach ($errorMessage in $errors) { Write-Host $errorMessage -ForegroundColor Red }
    throw "PowerShell documentation coverage gate failed with $($errors.Count) issue(s)."
}
Write-Host "Native PowerShell XML documentation file-level coverage passed ($csharpCount C# sources, $razorCount Razor components). The .NET compiler and DocFX validate generated XML and API content."
