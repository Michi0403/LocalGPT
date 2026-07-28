Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw $Message }
$root = Split-Path -Parent $PSScriptRoot
$baselinePath = Join-Path $PSScriptRoot 'application-static-baseline.json'
if (-not (Test-Path -LiteralPath $baselinePath -PathType Leaf)) { Fail 'Application static baseline is missing.' }
$parsedBaseline = [System.IO.File]::ReadAllText($baselinePath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
$known = @{}; foreach ($item in $parsedBaseline) { $known[[string]$item] = $true }
$failures = New-Object System.Collections.Generic.List[string]
$files = @(Get-ChildItem -LiteralPath (Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT') -Recurse -File | Where-Object { $_.Extension -in @('.cs','.razor') -and $_.FullName -notmatch '[\\/](?:bin|obj|Migrations|Extensions)[\\/]' -and $_.Name -notlike '*.Designer.cs' -and $_.Name -ne 'Program.cs' })
$pattern = '(?m)^\s*(?:public|private|protected|internal)\s+(?:(?:sealed|partial|new|unsafe)\s+)*static\s+[^\r\n]+'
foreach ($file in $files) {
    $relative = $file.FullName.Substring($root.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace([char]'\', [char]'/')
    $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    foreach ($match in [regex]::Matches($text, $pattern)) {
        $declaration = ([regex]::Replace($match.Value, '\s+', ' ')).Trim()
        $id = "${relative}|${declaration}"
        if (-not $known.ContainsKey($id)) { $failures.Add($id) }
    }
}
if ($failures.Count -gt 0) {
    Write-Host 'Application static policy validation failed:'
    foreach ($failure in $failures | Sort-Object -Unique) { Write-Host "  - $failure" }
    Fail "Application static policy validation failed with $($failures.Count) new static declaration(s). Move behavior to an injected singleton service or the Extensions namespace."
}
Write-Host 'Application static policy validation passed. New application statics outside Extensions are forbidden; reviewed legacy declarations may only be removed.'
