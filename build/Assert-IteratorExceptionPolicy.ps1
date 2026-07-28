Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw $Message }
function Normalize-Signature([string]$Value) { return ([regex]::Replace($Value, '\s+', ' ')).Trim() }
function Get-MethodRecords([string]$Text) {
    $pattern = '(?ms)^[ \t]*(?<signature>(?:public|private|protected|internal)\s+(?:(?:static|async|virtual|override|sealed|partial|new)\s+)*(?:[\w\.\?<>,\[\]]+\s+)+(?<name>[A-Za-z_]\w*)\s*\([^;{}]*?\)\s*(?:where[^\{=>\r\n]+)?\s*)(?<body>=>|\{)'
    $records = @()
    foreach ($match in [regex]::Matches($Text, $pattern)) {
        $bodyStart = $match.Groups['body'].Index
        $body = $null
        if ($match.Groups['body'].Value -eq '=>') {
            $end = $Text.IndexOf(';', $bodyStart)
            if ($end -lt 0) { continue }
            $body = $Text.Substring($bodyStart, $end - $bodyStart + 1)
        }
        else {
            $depth = 0
            $end = -1
            for ($index = $bodyStart; $index -lt $Text.Length; $index++) {
                $character = $Text[$index]
                if ($character -eq '{') { $depth++ }
                elseif ($character -eq '}') {
                    $depth--
                    if ($depth -eq 0) { $end = $index; break }
                }
            }
            if ($end -lt 0) { continue }
            $body = $Text.Substring($bodyStart, $end - $bodyStart + 1)
        }
        $records += [pscustomobject]@{
            Signature = Normalize-Signature $match.Groups['signature'].Value
            Name = $match.Groups['name'].Value
            Body = $body
        }
    }
    return $records
}

$root = Split-Path -Parent $PSScriptRoot
$baselinePath = Join-Path $PSScriptRoot 'iterator-exception-baseline.json'
if (-not (Test-Path -LiteralPath $baselinePath -PathType Leaf)) { Fail 'Iterator exception baseline is missing.' }
$parsedBaseline = [System.IO.File]::ReadAllText($baselinePath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
$known = @{}; foreach ($item in $parsedBaseline) { $known[[string]$item] = $true }
$failures = New-Object System.Collections.Generic.List[string]
$files = @(Get-ChildItem -LiteralPath (Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT') -Recurse -File | Where-Object { $_.Extension -in @('.cs','.razor') -and $_.FullName -notmatch '[\\/](?:bin|obj|Migrations)[\\/]' -and $_.Name -notlike '*.Designer.cs' })
foreach ($file in $files) {
    $relative = $file.FullName.Substring($root.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace([char]'\', [char]'/')
    $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    foreach ($method in Get-MethodRecords $text) {
        $body = [string]$method.Body
        if (-not [regex]::IsMatch($body, '\byield\s+(?:return|break)\b')) { continue }
        if ([regex]::IsMatch($body, '\bcatch\b')) { $candidate = "${relative}|$($method.Signature)|iterator contains catch"; if (-not $known.ContainsKey($candidate)) { $failures.Add($candidate) } }
        if (-not [regex]::IsMatch($body, '\btry\b') -or -not [regex]::IsMatch($body, '\bfinally\b')) { $candidate = "${relative}|$($method.Signature)|iterator requires try/finally"; if (-not $known.ContainsKey($candidate)) { $failures.Add($candidate) } }
        if (-not [regex]::IsMatch($body, '\b(?:Logger|logger|_logger)\s*\.\s*Log\w*\s*\(')) { $candidate = "${relative}|$($method.Signature)|iterator requires logging"; if (-not $known.ContainsKey($candidate)) { $failures.Add($candidate) } }
    }
}
if ($failures.Count -gt 0) {
    Write-Host 'Iterator exception policy validation failed:'
    foreach ($failure in $failures | Sort-Object -Unique) { Write-Host "  - $failure" }
    Fail "Iterator exception policy validation failed with $($failures.Count) problem(s). Yield methods require logged try/finally and may not contain catch."
}
Write-Host 'Iterator exception policy validation passed. New iterator methods require logged try/finally and may not contain catch.'
