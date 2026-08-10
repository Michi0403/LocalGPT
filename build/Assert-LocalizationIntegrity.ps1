Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "Localization integrity validation failed: $Message" }

# Keep this script ASCII-only. Windows PowerShell 5.1 reads UTF-8 scripts without a
# BOM using the active ANSI code page; non-ASCII source literals would be corrupted.
$script:StrictUtf8 = New-Object -TypeName System.Text.UTF8Encoding -ArgumentList @($false, $true)

function Read-StrictUtf8([string]$Path) {
    try { return [System.IO.File]::ReadAllText($Path, $script:StrictUtf8) }
    catch { Fail "Catalog is not valid UTF-8: $Path. $($_.Exception.Message)" }
}

function Expand-SpaceMarkers([string]$Template) {
    $spaceMarker = [string][char]0x2420
    return $Template.Replace('<SP>', $spaceMarker)
}

function Assert-NoMojibake([string]$Raw, [string]$Path) {
    foreach ($codePoint in @(0xFFFD, 0x00C2, 0x00C3)) {
        if ($Raw.IndexOf([char]$codePoint) -ge 0) {
            Fail "Catalog contains a replacement or mojibake marker (U+$('{0:X4}' -f $codePoint)): $Path"
        }
    }
}

function Read-Catalog([string]$Path) {
    $raw = Read-StrictUtf8 $Path
    Assert-NoMojibake $raw $Path
    $keyMatches = [regex]::Matches($raw, '(?m)^\s*"((?:\\.|[^"\\])*)"\s*:')
    $keys = New-Object System.Collections.Generic.List[string]
    $seen = New-Object 'System.Collections.Generic.Dictionary[string,string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($match in $keyMatches) {
        try { $decoded = ConvertFrom-Json -InputObject ('"' + $match.Groups[1].Value + '"') }
        catch { Fail "Catalog key JSON could not be decoded in $Path. $($_.Exception.Message)" }
        $key = [string]$decoded
        if ($seen.ContainsKey($key)) { Fail "Catalog contains case-insensitive duplicate keys '$($seen[$key])' and '$key': $Path" }
        $seen[$key] = $key
        $keys.Add($key)
    }
    try { $catalog = ConvertFrom-Json -InputObject $raw }
    catch { Fail "A catalog is not valid JSON. $($_.Exception.Message)" }
    return [pscustomobject]@{ Catalog = $catalog; Keys = @($keys | Sort-Object) }
}

$root = Split-Path -Parent $PSScriptRoot
$localization = Join-Path $root 'src\LocalGPT\Localization'
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
if ($englishKeys.Count -lt 1700) { Fail "English catalog coverage unexpectedly dropped to $($englishKeys.Count) entries." }
if (($englishKeys -join "`n") -cne ($germanKeys -join "`n")) { Fail 'English and German catalog keys differ.' }

# The 2.6.1 source-text expansion briefly created case-only Archive Preset keys. Keep the semantic
# SourceText key and reject the obsolete Text key explicitly so overlays cannot silently reintroduce it.
$obsoleteArchivePresetKey = Expand-SpaceMarkers 'Text.Archive<SP>Preset'
if ($english.PSObject.Properties[$obsoleteArchivePresetKey] -or $german.PSObject.Properties[$obsoleteArchivePresetKey]) {
    Fail "Obsolete localization key is still present: $obsoleteArchivePresetKey. Use SourceText.ArchivePreset.SentenceCase / Phrase.Archive<SP>Preset instead."
}
foreach ($catalog in @($english, $german)) {
    if ($null -eq $catalog.PSObject.Properties['SourceText.ArchivePreset.SentenceCase']) {
        Fail 'Required semantic Archive preset source-text key is missing: SourceText.ArchivePreset.SentenceCase'
    }
}
$requiredTemplates = @(
    'Text.Start<SP>new<SP>chat',
    'Text.Language',
    'Text.Apply<SP>language',
    'Text.Show<SP>ASCII<SP>games',
    'Text.Chat<SP>configuration',
    'Text.Running<SP>session<SP>tools',
    'Text.No<SP>Council<SP>heartbeat<SP>is<SP>running',
    'Text.Send<SP>message',
    'Text.Attach<SP>files',
    'Text.Former<SP>model<SP>thoughts',
    'Text.Install<SP>-<SP>Configure<SP>AI<SP>Connectivity',
    'Nav.ProjectMaintenance',
    'ProjectMaintenance.Title',
    'ProjectMaintenance.RevisionSourceRoot',
    'ProjectMaintenance.StructureRegex',
    'ProjectMaintenance.CompilerEnvironment',
    'ProjectMaintenance.SourceChanged',
    'ProjectMaintenance.RunBuildVerification',
    'ProjectMaintenance.ApproveReady',
    'Common.NotRun',
    'Text.Configured<SP>AI<SP>hosts',
    'Text.Save<SP>provider<SP>settings',
    'Text.Primary<SP>Ollama<SP>host',
    'Text.Provider-bound<SP>role<SP>models',
    'Text.Role<SP>boundary',
    'Text.Response<SP>language',
    'Text.Theme<SP>Fusion',
    'Text.Reset<SP>route',
    'Startup.Connecting',
    'Startup.ReconnectTitle',
    'Install.Workbench.Nav.ProvidersHelp',
    'Install.ConfiguredProviders.SummaryMany',
    'Text.Model<SP>status',
    'Text.Open<SP>model<SP>actions'
)
foreach ($template in $requiredTemplates) {
    $key = Expand-SpaceMarkers $template
    $property = $german.PSObject.Properties[$key]
    if ($null -eq $property -or [string]::IsNullOrWhiteSpace([string]$property.Value)) { Fail "Required German UI string is missing: $key" }
}

# Keep the server loader tolerant without ever constructing an OrdinalIgnoreCase dictionary from an
# already materialized case-sensitive Dictionary. JsonDocument preserves duplicate properties so the
# service can resolve them deterministically while source-controlled catalogs remain build-fail strict.
$loaderPath = Join-Path $root 'src\LocalGPT\Services\Localization\LocalGptLocalizationService.cs'
if (-not (Test-Path -LiteralPath $loaderPath -PathType Leaf)) { Fail "Missing $loaderPath" }
$loader = Read-StrictUtf8 $loaderPath
foreach ($requiredLoaderToken in @('JsonDocument.Parse(stream)', 'EnumerateObject()', 'StringComparer.OrdinalIgnoreCase')) {
    if ($loader.IndexOf($requiredLoaderToken, [System.StringComparison]::Ordinal) -lt 0) {
        Fail "LocalGPT localization loader is missing required duplicate-safe token: $requiredLoaderToken"
    }
}
if ($loader.IndexOf('JsonSerializer.Deserialize<Dictionary<string, string>>(stream)', [System.StringComparison]::Ordinal) -ge 0) {
    Fail 'LocalGPT localization loader regressed to Dictionary deserialization before case-insensitive normalization.'
}

$runtimePath = Join-Path $root 'src\LocalGPT\wwwroot\js\localgpt-localization.js'
if (-not (Test-Path -LiteralPath $runtimePath -PathType Leaf)) { Fail "Missing $runtimePath" }
$runtime = Read-StrictUtf8 $runtimePath
foreach ($requiredRuntimeToken in @('request(''en-US'')', 'document.createTreeWalker', 'characterData: true', 'sourceDictionary')) {
    if ($runtime.IndexOf($requiredRuntimeToken, [System.StringComparison]::Ordinal) -lt 0) {
        Fail "LocalGPT localization runtime is missing required coverage token: $requiredRuntimeToken"
    }
}

Write-Host "Localization integrity validation passed for $($englishKeys.Count) LocalGPT UI strings."
