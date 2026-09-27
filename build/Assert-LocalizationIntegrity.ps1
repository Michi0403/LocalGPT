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


function Get-LineNumberForText([string]$Raw, [string]$Needle) {
    $index = $Raw.IndexOf($Needle, [System.StringComparison]::Ordinal)
    if ($index -lt 0) { return 1 }
    return ([regex]::Matches($Raw.Substring(0, $index), "`n").Count + 1)
}

function Write-LocalizationError([string]$Path, [int]$Line, [string]$Code, [string]$Message, [string]$Choices) {
    $relative = $Path
    try {
        $repoRoot = Split-Path -Parent $PSScriptRoot
        if ($Path.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            $relative = $Path.Substring($repoRoot.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace('\','/')
        }
    } catch { }
    Write-Output ("{0}({1},1): error {2}: {3}" -f $relative, $Line, $Code, $Message)
    Write-Output ("  Architectural choices: {0}" -f $Choices)
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
        if ($seen.ContainsKey($key)) {
            $line = Get-LineNumberForText $raw $match.Value
            Write-LocalizationError $Path $line 'L10N0001' "Catalog contains case-insensitive duplicate keys '$($seen[$key])' and '$key'." "Choose one canonical source-text casing, update callers to that casing, and keep one catalog key across every culture. Do not retain case-only aliases because runtime dictionaries are case-insensitive."
            Fail "Catalog contains case-insensitive duplicate keys '$($seen[$key])' and '$key': $Path"
        }
        $seen[$key] = $key
        $keys.Add($key)
    }
    try { $catalog = ConvertFrom-Json -InputObject $raw }
    catch { Fail "A catalog is not valid JSON. $($_.Exception.Message)" }
    return [pscustomobject]@{ Catalog = $catalog; Keys = @($keys | Sort-Object) }
}

$root = Split-Path -Parent $PSScriptRoot
$localization = Join-Path $root 'src/LocalGPT/Localization'
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


# Keep every maintained culture in exact key parity. A translation may fall back to English, but the
# key itself must exist so language switching never removes labels, forms, controls, or important values.
foreach ($catalogFile in @(Get-ChildItem -LiteralPath $localization -File -Filter '*.json' | Sort-Object Name)) {
    if ($catalogFile.FullName -eq $englishPath -or $catalogFile.FullName -eq $germanPath) { continue }
    $catalogResult = Read-Catalog $catalogFile.FullName
    if (($englishKeys -join "`n") -cne ($catalogResult.Keys -join "`n")) {
        Fail "Localization catalog keys differ from en-US: $($catalogFile.Name)"
    }
}


# Literal localization call coverage: a developer/AI must not wrap a new visible string in LT/GetText
# and forget to add it to the catalogs. The source location is emitted before the build stops.
$englishValues = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
foreach ($property in $english.PSObject.Properties) { [void]$englishValues.Add([string]$property.Value) }
$applicationSource = Join-Path $root 'src\LocalGPT'
$literalPatterns = @(
    [regex]'\bLT\(\s*"((?:\\.|[^"\\])*)"\s*\)',
    [regex]'\.GetText\(\s*"((?:\\.|[^"\\])*)"'
)
foreach ($sourceFile in @(Get-ChildItem -LiteralPath $applicationSource -Recurse -File | Where-Object { $_.Extension -in @('.razor', '.cs') -and $_.FullName -notmatch '[\\/](?:bin|obj|wwwroot[\\/]help-docs)[\\/]' } | Sort-Object FullName)) {
    $sourceRaw = Read-StrictUtf8 $sourceFile.FullName
    foreach ($pattern in $literalPatterns) {
        foreach ($match in $pattern.Matches($sourceRaw)) {
            try { $literal = [string](ConvertFrom-Json -InputObject ('"' + $match.Groups[1].Value + '"')) }
            catch { continue }
            if ([string]::IsNullOrWhiteSpace($literal) -or $englishValues.Contains($literal)) { continue }
            $line = ([regex]::Matches($sourceRaw.Substring(0, $match.Index), "`n").Count + 1)
            Write-LocalizationError $sourceFile.FullName $line 'L10N0003' "Localized source text is not present in en-US.json: '$literal'." "Add one canonical English catalog entry for this exact source text and add the same key to every maintained culture. Provide a real German translation unless the text is intentionally language-neutral and reviewed into the baseline. Do not remove LT/GetText or hard-code the English string to bypass localization."
            Fail "Localized source text is missing from en-US.json: '$literal' in $($sourceFile.FullName):$line"
        }
    }
}

# Preserve a reviewed baseline of language-neutral English/German values. Any new key that simply
# copies English into the German catalog fails the build. This converts localization maintenance from
# a best-effort convention into an explicit source contract while allowing the legacy baseline to shrink.
$identicalBaselinePath = Join-Path $PSScriptRoot 'localization-identical-german-baseline.json'
if (-not (Test-Path -LiteralPath $identicalBaselinePath -PathType Leaf)) { Fail "Missing reviewed German identical-text baseline: $identicalBaselinePath" }
try { $identicalBaseline = ConvertFrom-Json -InputObject (Read-StrictUtf8 $identicalBaselinePath) }
catch { Fail "German identical-text baseline is not valid JSON. $($_.Exception.Message)" }
$allowedIdentical = @{}
foreach ($key in @($identicalBaseline)) {
    $normalizedKey = [string]$key
    if (-not [string]::IsNullOrWhiteSpace($normalizedKey)) { $allowedIdentical[$normalizedKey] = $true }
}
foreach ($key in $englishKeys) {
    $englishValue = [string]$english.PSObject.Properties[$key].Value
    $germanValue = [string]$german.PSObject.Properties[$key].Value
    if (-not [string]::IsNullOrWhiteSpace($englishValue) -and $englishValue -ceq $germanValue -and -not $allowedIdentical.ContainsKey($key)) {
        $germanRaw = Read-StrictUtf8 $germanPath
        $keyToken = '"' + $key.Replace('"', '\"') + '"'
        $line = Get-LineNumberForText $germanRaw $keyToken
        Write-LocalizationError $germanPath $line 'L10N0002' "German localization is missing for newly introduced or regressed UI text: $key = '$englishValue'." "Either provide a real German translation in de-DE.json, or, only when the spelling is intentionally language-neutral (for example a product/protocol name), add the key to build/localization-identical-german-baseline.json after explicit review. Never invent a fake translation merely to satisfy the guard."
        Fail "German localization is missing for newly introduced or regressed UI text: $key = '$englishValue'. Translate it or explicitly review it into the language-neutral baseline."
    }
}

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
    'Text.Open<SP>model<SP>actions',
    'Home.FirstRun.Kicker',
    'Home.FirstRun.Title',
    'Home.FirstRun.Intro',
    'Home.FirstRun.Profile.game-low-b.Name',
    'Home.FirstRun.Profile.development.Name',
    'Home.FirstRun.Profile.knowledge-only.Name',
    'Home.FirstRun.QuickStart.benchmark.Name',
    'Home.FirstRun.QuickStart.game.Name',
    'Home.FirstRun.QuickStart.csharp.Name',
    'Home.FirstRun.QuickStart.powershell.Name',
    'Home.FirstRun.QuickStart.java.Name',
    'Home.FirstRun.QuickStart.minecraft.Name',
    'Chat.Council.UnavailableRoutes.Title',
    'Chat.Council.UnavailableRoutes.Help',
    'ProviderModel.Unreachable'
)
foreach ($template in $requiredTemplates) {
    $key = Expand-SpaceMarkers $template
    $property = $german.PSObject.Properties[$key]
    if ($null -eq $property -or [string]::IsNullOrWhiteSpace([string]$property.Value)) { Fail "Required German UI string is missing: $key" }
}

# Keep the server loader tolerant without ever constructing an OrdinalIgnoreCase dictionary from an
# already materialized case-sensitive Dictionary. JsonDocument preserves duplicate properties so the
# service can resolve them deterministically while source-controlled catalogs remain build-fail strict.
$loaderPath = Join-Path $root 'src/LocalGPT/Services/Localization/LocalGptLocalizationService.cs'
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

$runtimePath = Join-Path $root 'src/LocalGPT/wwwroot/js/localgpt-localization.js'
if (-not (Test-Path -LiteralPath $runtimePath -PathType Leaf)) { Fail "Missing $runtimePath" }
$runtime = Read-StrictUtf8 $runtimePath
foreach ($requiredRuntimeToken in @('request(''en-US'')', 'document.createTreeWalker', 'characterData: true', 'sourceDictionary')) {
    if ($runtime.IndexOf($requiredRuntimeToken, [System.StringComparison]::Ordinal) -lt 0) {
        Fail "LocalGPT localization runtime is missing required coverage token: $requiredRuntimeToken"
    }
}

Write-Host "Localization integrity validation passed for $($englishKeys.Count) LocalGPT UI strings."
