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
$baselinePath = Join-Path $PSScriptRoot 'method-diagnostics-baseline.json'
if (-not (Test-Path -LiteralPath $baselinePath -PathType Leaf)) { Fail 'Method diagnostics baseline is missing.' }
$parsedBaseline = [System.IO.File]::ReadAllText($baselinePath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
$known = @{}
foreach ($item in $parsedBaseline) { $known[[string]$item] = $true }
$violations = New-Object System.Collections.Generic.List[string]
$files = @(Get-ChildItem -LiteralPath (Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT') -Recurse -File | Where-Object { $_.Extension -in @('.cs', '.razor') -and $_.FullName -notmatch '[\\/](?:bin|obj|Migrations)[\\/]' -and $_.Name -notlike '*.Designer.cs' })
foreach ($file in $files) {
    $relative = $file.FullName.Substring($root.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace([char]'\', [char]'/')
    $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    foreach ($method in Get-MethodRecords $text) {
        $id = "${relative}|$($method.Signature)"
        $body = [string]$method.Body
        $hasLogger = [regex]::IsMatch($body, '\b(?:Logger|logger|_logger)\s*\.\s*Log\w*\s*\(')
        $hasTry = [regex]::IsMatch($body, '\btry\b')
        $hasYield = [regex]::IsMatch($body, '\byield\s+(?:return|break)\b')
        $hasBoundaryEnd = if ($hasYield) { [regex]::IsMatch($body, '\bfinally\b') } else { [regex]::IsMatch($body, '\bcatch\b') }
        $awaitCount = [regex]::Matches($body, '\bawait\b').Count
        $continuationCount = [regex]::Matches($body, '\.ConfigureAwait\s*\(\s*(?:true|false)\s*\)').Count
        $checks = @()
        if (-not $hasLogger) { $checks += 'logging' }
        if (-not ($hasTry -and $hasBoundaryEnd)) { $checks += 'exception-boundary' }
        if ($awaitCount -gt $continuationCount) { $checks += 'configure-await' }
        foreach ($kind in $checks) {
            $violation = "${id}|${kind}"
            if (-not $known.ContainsKey($violation)) { $violations.Add($violation) }
        }
        foreach ($logCall in [regex]::Matches($body, '\b(?:Logger|logger|_logger)\s*\.\s*Log\w*\s*\((?<args>[\s\S]{0,400}?)\)')) {
            $args = $logCall.Groups['args'].Value
            if ($args.Contains('"') -and -not ($args.Contains('$"') -or $args.Contains('$@"') -or $args.Contains('@$"'))) {
                $violation = "${id}|interpolated-message"
                if (-not $known.ContainsKey($violation)) { $violations.Add($violation) }
                break
            }
        }
    }
}
if ($violations.Count -gt 0) {
    Write-Host 'Method diagnostics validation failed:'
    foreach ($failure in $violations | Sort-Object -Unique) { Write-Host "  - $failure" }
    Fail "Method diagnostics validation failed with $($violations.Count) new problem(s)."
}
Write-Host "Method diagnostics validation passed. Existing reviewed debt may only decrease; every new runtime method requires try/catch (or try/finally for iterators), logging, interpolated log messages, and configured awaits."
