param([string]$ReportPath = '')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$source = Join-Path $root 'src/LocalGPT'
$failures = [Collections.Generic.List[string]]::new()
$patterns = @(
    @{ Regex = '(?m)\[GeneratedRegex\s*\('; Message = 'GeneratedRegex bypasses the persisted RegexPatternService.' },
    @{ Regex = '(?m)\bnew\s+Regex\s*\(\s*@?"'; Message = 'Literal Regex construction bypasses the approved regex curator.' },
    @{ Regex = '(?m)\bnew\s+Regex\s*\(\s*\$@?"'; Message = 'Interpolated literal Regex construction bypasses curator ownership.' }
)
$allowed = @('Services/Persistence/LocalGptRuntimePolicyDataService.cs','Services/Persistence/CouncilTextPatternDataService.cs','Services/Persistence/RegexPatternService.cs','Services/RegexCompilationService.cs')
$count = 0
foreach ($file in Get-ChildItem -LiteralPath $source -Recurse -File -Filter '*.cs' | Where-Object { $_.FullName -notmatch '[\\/](?:obj|bin|Migrations)[\\/]' }) {
    $count++
    $relative = $file.FullName.Substring($source.Length).TrimStart([char[]]@('/', '\')).Replace('\','/')
    $text = [IO.File]::ReadAllText($file.FullName)
    $withoutComments = [regex]::Replace($text, '(?s)/\*.*?\*/|(?m)//[^\r\n]*', '')
    foreach ($item in $patterns) {
        if ($allowed -contains $relative) { continue }
        foreach ($match in [regex]::Matches($withoutComments, $item.Regex)) {
            $line = @($withoutComments.Substring(0,$match.Index).ToCharArray() | Where-Object { $_ -eq "`n" }).Count + 1
            $failures.Add("$relative($line): $($item.Message)")
        }
    }
}
& (Join-Path $PSScriptRoot 'Invoke-ArchitectureAudit.ps1') -Mode runtime
if ($ReportPath) {
    New-Item -ItemType Directory -Path (Split-Path -Parent ([IO.Path]::GetFullPath($ReportPath))) -Force | Out-Null
    [IO.File]::WriteAllText([IO.Path]::GetFullPath($ReportPath), ($failures -join [Environment]::NewLine))
}
if ($failures.Count) { $failures | ForEach-Object { Write-Error $_ }; throw "Configurable runtime text/regex ownership validation failed: $($failures.Count) findings; do not baseline, suppress, or hardcode around the guard." }
Write-Host "PowerShell runtime text/regex ownership audit passed across $count C# files; no Python or baseline required."
