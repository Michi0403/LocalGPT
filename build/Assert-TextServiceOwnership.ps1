Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw $Message }
$root = Split-Path -Parent $PSScriptRoot
$failures = New-Object System.Collections.Generic.List[string]
# An empty reviewed exception set is intentional: new UI/controller text manipulation
# must be implemented by an injected service, not grandfathered by a generated baseline.
$known = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$sourceRoot = (Join-Path $root 'src/LocalGPT')
$folders = @('Components','Controllers','Controller')
$pattern = '(?m)^(?<line>.*(?:\bRegex\s*\.|\bnew\s+Regex\s*\(|\.Replace\s*\(|\.Split\s*\(|\bstring\.Join\s*\(|\bWebUtility\.HtmlDecode\s*\(|\.StartsWith\s*\(|\.EndsWith\s*\(|\.IndexOf\s*\(|\.Substring\s*\(|\.Contains\s*\([^\r\n;]*StringComparison\.).*)$'
foreach ($folder in $folders) {
    $path = Join-Path $sourceRoot $folder
    if (-not (Test-Path -LiteralPath $path -PathType Container)) { continue }
    foreach ($file in Get-ChildItem -LiteralPath $path -Recurse -File | Where-Object { $_.Extension -in @('.cs','.razor') }) {
        $relative = $file.FullName.Substring($root.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace([char]'\', [char]'/')
        $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
        foreach ($match in [regex]::Matches($text, $pattern)) {
            $line = ([regex]::Replace($match.Groups['line'].Value.Trim(), '\s+', ' ')).Trim()
            if ($line -match '(?:CouncilText|PanelText|TextService|RegexService|StringService|ReviewerPolicy)\.') { continue }
            $id = "${relative}|${line}"
            if (-not $known.Contains($id)) { $failures.Add($id) }
        }
    }
}
if ($failures.Count -gt 0) {
    Write-Host 'Text-service ownership validation failed:'
    foreach ($failure in $failures | Sort-Object -Unique) { Write-Host "  - $failure" }
    Fail "Text-service ownership validation failed with $($failures.Count) new direct string/regex operation(s). Move manipulation and provisioning to an injected service."
}
Write-Host 'Text-service ownership validation passed. New component/controller string, regex, and provisioning behavior must be service-owned.'
