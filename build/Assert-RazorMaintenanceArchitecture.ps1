param([string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path, [ValidateSet('localgpt','publisherstudio')][string]$Product='localgpt')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($Product -ne 'localgpt') { throw 'This LocalGPT-specific Razor maintenance guard cannot validate PublisherStudio.' }
$components = Join-Path $RepositoryRoot 'src/LocalGPT/Components'
if (-not (Test-Path -LiteralPath $components -PathType Container)) { throw 'Missing LocalGPT components.' }
$failures = [Collections.Generic.List[string]]::new()
$count = 0
foreach ($file in Get-ChildItem -LiteralPath $components -Recurse -File -Filter '*.razor') {
    $count++
    $text = [IO.File]::ReadAllText($file.FullName)
    if ($text -match '(?is)<\s*(?:dialog|section)\b' -and $file.Name -notin @('App.razor')) {
        $failures.Add("$($file.FullName): native dialog/section cannot replace a maintained component owner")
    }
    if ($text -match '(?is)<\s*DxGridLayoutItem\b[^>]*>\s*<(?!Template\b)' ) {
        $failures.Add("$($file.FullName): DxGridLayoutItem must own an explicit Template")
    }
}
# The separately wired Assert-FormLayoutOwnership and Assert-DevExpressBlazorControls
# targets enforce layout semantics and native-control substitution without rerunning them here.
if ($failures.Count) { $failures | ForEach-Object { Write-Error $_ }; throw "Razor maintenance contract failed: $($failures.Count) finding(s)." }
Write-Host "PowerShell Razor maintenance architecture validated $count components."
