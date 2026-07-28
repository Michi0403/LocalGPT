Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw $Message }
$root = Split-Path -Parent $PSScriptRoot
$baselinePath = Join-Path $PSScriptRoot 'text-service-ownership-baseline.json'
if (-not (Test-Path -LiteralPath $baselinePath -PathType Leaf)) { Fail 'Text-service ownership baseline is missing.' }
$parsedBaseline = [System.IO.File]::ReadAllText($baselinePath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
$known = @{}; foreach ($item in $parsedBaseline) { $known[[string]$item] = $true }
$failures = New-Object System.Collections.Generic.List[string]
$sourceRoot = (Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT')
$folders = @('Components','Controllers','Controller')
$pattern = '(?m)^(?<line>.*(?:\bRegex\s*\.|\bnew\s+Regex\s*\(|\.Replace\s*\(|\.Split\s*\(|\bstring\.Join\s*\(|\bWebUtility\.HtmlDecode\s*\().*)$'
foreach ($folder in $folders) {
    $path = Join-Path $sourceRoot $folder
    if (-not (Test-Path -LiteralPath $path -PathType Container)) { continue }
    foreach ($file in Get-ChildItem -LiteralPath $path -Recurse -File | Where-Object { $_.Extension -in @('.cs','.razor') }) {
        $relative = $file.FullName.Substring($root.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace([char]'\', [char]'/')
        $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
        foreach ($match in [regex]::Matches($text, $pattern)) {
            $line = ([regex]::Replace($match.Groups['line'].Value.Trim(), '\s+', ' ')).Trim()
            if ($line -match '(?:CouncilText|PanelText|TextService|RegexService|StringService)\.') { continue }
            $id = "${relative}|${line}"
            if (-not $known.ContainsKey($id)) { $failures.Add($id) }
        }
    }
}
if ($failures.Count -gt 0) {
    Write-Host 'Text-service ownership validation failed:'
    foreach ($failure in $failures | Sort-Object -Unique) { Write-Host "  - $failure" }
    Fail "Text-service ownership validation failed with $($failures.Count) new direct string/regex operation(s). Move manipulation and provisioning to an injected service."
}
Write-Host 'Text-service ownership validation passed. New component/controller string, regex, and provisioning behavior must be service-owned.'
