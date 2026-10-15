param(
    [string]$RepositoryRoot = ""
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
}
$root = [IO.Path]::GetFullPath($RepositoryRoot)
$notice = Join-Path $root 'THIRD-PARTY-NOTICES.txt'
if (-not (Test-Path -LiteralPath $notice -PathType Leaf)) { throw "Third-party release notice missing: $notice" }
$contents = [IO.File]::ReadAllText($notice).Replace("`r`n", "`n")
$requiredSources = @(
    'THIRD-PARTY-NOTICES.md',
    'third_party/PDFsharp/LICENSE',
    'third_party/PDFsharp/SOURCE-NOTICE.md',
    'src/LocalGPT/wwwroot/css/open.iconic/ICON-LICENSE.txt',
    'src/LocalGPT/wwwroot/css/open.iconic/FONT-LICENSE.txt'
)
foreach ($relative in $requiredSources) {
    $source = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Third-party source notice missing: $source" }
    $original = [IO.File]::ReadAllText($source).TrimStart([char]0xFEFF).Replace("`r`n", "`n").TrimEnd()
    if (-not $contents.Contains($original)) { throw "Third-party release notice does not preserve the current full source text of $relative. Rebuild THIRD-PARTY-NOTICES.txt before release." }
}
foreach ($marker in @('PDFsharp 6.2.4', 'Bootstrap 5.3.1', 'Bootswatch themes 5.3.1', 'Kazuhiko Arase', 'DevExpress', 'Open Iconic fonts')) {
    if (-not $contents.Contains($marker)) { throw "Third-party release notice missing $marker." }
}
Write-Host 'Third-party release notice validation passed: original PDFsharp MIT, Open Iconic MIT/OFL, provenance and identified frontend notices are present.' -ForegroundColor Green
