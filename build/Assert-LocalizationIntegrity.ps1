Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "Localization integrity validation failed: $Message" }
function Read-Catalog([string]$Path) {
    $raw = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $keyMatches = [regex]::Matches($raw, '(?m)^\s*"((?:\\.|[^"\\])*)"\s*:')
    $keys = New-Object System.Collections.Generic.List[string]
    $seen = @{}
    foreach ($match in $keyMatches) {
        try { $decoded = ('"' + $match.Groups[1].Value + '"') | ConvertFrom-Json }
        catch { Fail "Catalog key JSON could not be decoded in $Path. $($_.Exception.Message)" }
        $normalized = ([string]$decoded).ToUpperInvariant()
        if ($seen.ContainsKey($normalized)) { Fail "Catalog contains case-insensitive duplicate keys '$($seen[$normalized])' and '$decoded': $Path" }
        $seen[$normalized] = [string]$decoded
        $keys.Add([string]$decoded)
    }
    try { $catalog = $raw | ConvertFrom-Json }
    catch { Fail "A catalog is not valid JSON. $($_.Exception.Message)" }
    return [pscustomobject]@{ Catalog = $catalog; Keys = @($keys | Sort-Object) }
}

$root = Split-Path -Parent $PSScriptRoot
$localization = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Localization'
$englishPath = Join-Path $localization 'en-US.json'
$germanPath = Join-Path $localization 'de-DE.json'
if (-not (Test-Path -LiteralPath $englishPath -PathType Leaf)) { Fail "Missing $englishPath" }
if (-not (Test-Path -LiteralPath $germanPath -PathType Leaf)) { Fail "Missing $germanPath" }
$englishResult = Read-Catalog $englishPath
$germanResult = Read-Catalog $germanPath
$english = $englishResult.Catalog
$german = $germanResult.Catalog
$englishKeys = $englishResult.Keys
$germanKeys = $germanResult.Keys
if ($englishKeys.Count -lt 1200) { Fail "English catalog coverage unexpectedly dropped to $($englishKeys.Count) entries." }
if (($englishKeys -join "`n") -cne ($germanKeys -join "`n")) { Fail 'English and German catalog keys differ.' }
$required = @(
    'Text.Start␠new␠chat',
    'Text.Send␠message',
    'Text.Attach␠files',
    'Text.Former␠model␠thoughts',
    'Text.Install␠-␠Configure␠AI␠Connectivity',
    'Nav.ProjectMaintenance',
    'ProjectMaintenance.Title',
    'ProjectMaintenance.RevisionSourceRoot',
    'ProjectMaintenance.StructureRegex',
    'ProjectMaintenance.CompilerEnvironment',
    'ProjectMaintenance.SourceChanged',
    'ProjectMaintenance.RunBuildVerification',
    'ProjectMaintenance.ApproveReady',
    'Common.NotRun'
)
foreach ($key in $required) {
    $property = $german.PSObject.Properties[$key]
    if ($null -eq $property -or [string]::IsNullOrWhiteSpace([string]$property.Value)) { Fail "Required German UI string is missing: $key" }
}
Write-Host "Localization integrity validation passed for $($englishKeys.Count) LocalGPT UI strings."
