Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Join-Path (Split-Path -Parent $PSScriptRoot) 'src/LocalGPT'
$issues = [Collections.Generic.List[string]]::new()
$files = 0
foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object { $_.Extension -in @('.cs','.razor') -and $_.FullName -notmatch '[\\/](?:obj|bin|Migrations)[\\/]' }) {
    $files++
    $text = [IO.File]::ReadAllText($file.FullName)
    if ($text -match '(?m)^\s*async\s+void\s+' -and $file.Name -notmatch 'Designer') { $issues.Add("$($file.FullName): async void breaks supervised continuation ownership") }
    if ($text -match '(?m)\b(?:Task|ValueTask)\.Run\s*\(\s*async\s+void\b') { $issues.Add("$($file.FullName): async-void worker") }
    # FileLoggerSink is a dedicated synchronous background Thread, not a renderer, controller,
    # service-async continuation or ThreadPool worker. Its short, bounded I/O retry is intentional.
    # Keep this exception scoped to that one call, not to the entire logger source file.
    $relative = $file.FullName.Substring($root.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace('\', '/')
    $blockingMatches = [regex]::Matches($text, '(?m)\b(?:Thread\.Sleep|Task\.WaitAll|Task\.WaitAny)\s*\(')
    $documentedLoggerWorker = $relative -eq 'Logging/FileLogger.cs' -and
        $text.Contains('_loggingThread = new Thread(ProcessLogQueue)') -and
        $text.Contains('private void ProcessLogQueue()')
    foreach ($blockingMatch in $blockingMatches) {
        $callText = $text.Substring($blockingMatch.Index, [Math]::Min(100, $text.Length - $blockingMatch.Index))
        $boundedLoggerRetry = $documentedLoggerWorker -and
            $blockingMatches.Count -eq 1 -and
            $callText -match '^Thread\.Sleep\s*\(\s*25\s*\*\s*\(attempt\s*\+\s*1\)\s*\)'
        if (-not $boundedLoggerRetry) {
            $lineNumber = 1 + ([regex]::Matches($text.Substring(0, $blockingMatch.Index), "`n")).Count
            $issues.Add("$($file.FullName):${lineNumber}: blocking wait outside the dedicated bounded file-logger retry")
        }
    }
}
if ($issues.Count -gt 0) { $issues | ForEach-Object { Write-Error $_ }; throw "Async continuation ownership rejected $($issues.Count) findings." }
Write-Host "PowerShell async-continuation boundary audit passed for $files sources (blocking and unsupervised async-void entry points)."
