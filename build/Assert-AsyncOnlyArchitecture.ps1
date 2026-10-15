Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Join-Path (Split-Path -Parent $PSScriptRoot) 'src/LocalGPT'
$findings = [Collections.Generic.List[string]]::new()
$count = 0
foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object { $_.Extension -in @('.cs','.razor') -and $_.FullName -notmatch '[\\/](?:obj|bin|Migrations)[\\/]' }) {
    $count++
    if ($file.FullName -notmatch '[\\/](?:Controller|Components)[\\/]') { continue }
    $source = [IO.File]::ReadAllText($file.FullName)
    foreach ($match in [regex]::Matches($source, '(?m)\b(?:GetAwaiter\(\)\.GetResult\(\)|\.Wait\s*\(|Task\.Run\s*\([^)]*\)\.Wait\s*\()')) {
        # Ignore documentation and comments; retain strict bans for executable signatures.
        $prefix = $source.Substring(0,$match.Index)
        $line = @($prefix.Split("`n")).Count
        $lineText = ($source.Split("`n")[$line-1]).Trim()
        if ($lineText.StartsWith('//') -or $lineText.StartsWith('*') -or $lineText.StartsWith('///')) { continue }
        $findings.Add("$($file.FullName):$line sync-over-async invocation")
    }
}
if ($findings.Count) { $findings | Select-Object -First 80 | ForEach-Object { Write-Error $_ }; throw "Async-only source audit rejected $($findings.Count) findings." }
Write-Host "PowerShell async-only UI/controller audit inspected $count files (no sync-over-async)."
