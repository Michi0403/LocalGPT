Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "Runtime-value ownership validation failed: $Message" }
$root = Split-Path -Parent $PSScriptRoot
$rootPrefix = [IO.Path]::GetFullPath($root).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
$sourceRoot = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT'
$baselinePath = Join-Path $PSScriptRoot 'runtime-value-ownership-baseline.json'
if (-not (Test-Path -LiteralPath $baselinePath -PathType Leaf)) { Fail 'The removal-only runtime-value baseline is missing.' }
$known = @{}; foreach ($item in ([IO.File]::ReadAllText($baselinePath) | ConvertFrom-Json)) { $known[[string]$item] = $true }
$failures = [Collections.Generic.List[string]]::new()
$declarationPattern = '(?m)^\s*(?:public|private|protected|internal)\s+(?:(?:static|readonly|const|sealed|new|partial)\s+)*(?:Regex|TimeSpan|string|int|long|double|decimal|bool|char|Guid|Uri|FrozenSet<[^>]+>|IReadOnly(?:List|Dictionary|Set)<[^>]+>)\s+[A-Za-z_][A-Za-z0-9_]*\s*(?:=|=>)\s*[^\r\n]+'
$generatedPattern = '(?m)^\s*\[GeneratedRegex\([^\r\n]+'
foreach ($file in Get-ChildItem -LiteralPath $sourceRoot -Recurse -File | Where-Object { $_.Extension -in @('.cs','.razor') -and $_.FullName -notmatch '[\\/](?:bin|obj|Migrations)[\\/]' -and $_.FullName -notmatch '[\\/]Services[\\/]Persistence[\\/]' }) {
    $relative = $file.FullName.Substring($rootPrefix.Length).Replace('\','/')
    $text = [IO.File]::ReadAllText($file.FullName)
    foreach ($regex in @($declarationPattern, $generatedPattern)) {
        foreach ($match in [regex]::Matches($text, $regex)) {
            $declaration = ([regex]::Replace($match.Value, '\s+', ' ')).Trim()
            $id = "${relative}|${declaration}"
            if (-not $known.ContainsKey($id)) { $failures.Add("New runtime value outside a data boundary: $id") }
        }
    }
}
$councilPath = Join-Path $sourceRoot 'Services\CouncilTextService.cs'
$council = [IO.File]::ReadAllText($councilPath)
foreach ($forbidden in @('private readonly Regex', 'new Regex(', 'Regex.', 'RegexOptions.', 'NameCleaner()', 'ModIdCleaner()', 'PackagePartCleaner()', 'TimeSpan.FromSeconds(2)', 'LocalGptCatalogService._whitespacePattern', 'LocalGptCatalogService.MissingFeaturePattern()', 'LocalGptCatalogService.ThinkingBlockPattern()')) {
    if ($council.IndexOf($forbidden, [StringComparison]::Ordinal) -ge 0) { $failures.Add("CouncilTextService reintroduced service-owned runtime pattern data: $forbidden") }
}
foreach ($required in @('ICouncilTextPatternDataService _patterns', '_patterns.FormerThoughtBreakPattern', '_patterns.FormerThoughtCodeWrapperPattern', '_patterns.FormerThoughtOpeningFencePattern', '_patterns.FormerThoughtClosingFencePattern', '_patterns.FormerThoughtPresentationWrapperPattern', '_patterns.FormerThoughtExcessLineBreakPattern', '_patterns.WhitespacePattern', '_patterns.NameCleanerPattern', '_patterns.ModIdCleanerPattern', '_patterns.PackagePartCleanerPattern', '_patterns.KnowledgeBlockPattern', '_patterns.TargetFrameworkPattern', '_patterns.PackageReferencePattern', '_patterns.ThinkingBlockPattern', '_patterns.MinecraftQuotedProjectNamePattern', '_patterns.IdentifierSeparatorPattern', '_patterns.AlphaNumericWordPattern', '_patterns.IntegerPattern', '_patterns.ExtractStructuredField')) {
    if ($council.IndexOf($required, [StringComparison]::Ordinal) -lt 0) { $failures.Add("CouncilTextService lost data-service ownership token: $required") }
}
$runtimePath = Join-Path $sourceRoot 'Services\CouncilRuntimeService.cs'
$runtime = [IO.File]::ReadAllText($runtimePath)
foreach ($forbidden in @('LocalGptCatalogService.TargetFrameworkPattern()', 'LocalGptCatalogService.PackageReferencePattern()')) {
    if ($runtime.IndexOf($forbidden, [StringComparison]::Ordinal) -ge 0) { $failures.Add("CouncilRuntimeService reintroduced catalog-owned runtime regex access: $forbidden") }
}
foreach ($required in @('_text.ExtractTargetFrameworks(combined, logger)', '_text.ExtractPackageReferences(combined, logger)')) {
    if ($runtime.IndexOf($required, [StringComparison]::Ordinal) -lt 0) { $failures.Add("CouncilRuntimeService lost Council text data-boundary call: $required") }
}
$dataPath = Join-Path $sourceRoot 'Services\Persistence\CouncilTextPatternDataService.cs'
$data = [IO.File]::ReadAllText($dataPath)
foreach ($required in @('IDbContextFactory<LocalGptMemoryDbContext>', 'db.RegexPatterns.AsNoTracking()', 'db.SystemVariables.AsNoTracking()', 'systemVariables.RegexMatchTimeoutMilliseconds', 'TimeSpan.FromMilliseconds(timeoutMilliseconds)', 'ExtractStructuredField(string body, string name)', 'StructuredFieldPattern.Matches(body)', 'GetRequired("builtin.target-framework-pattern")', 'GetRequired("builtin.package-reference-pattern")')) {
    if ($data.IndexOf($required, [StringComparison]::Ordinal) -lt 0) { $failures.Add("Council text pattern data service lost database-backed token: $required") }
}
$program = [IO.File]::ReadAllText((Join-Path $sourceRoot 'Program.cs'))
if ($program.IndexOf('AddSingleton<ICouncilTextPatternDataService, CouncilTextPatternDataService>()', [StringComparison]::Ordinal) -lt 0) { $failures.Add('Council text pattern data service must remain a singleton dependency.') }
$seed = [IO.File]::ReadAllText((Join-Path $sourceRoot 'Services\Persistence\InitialDataCatalog.cs'))
foreach ($name in @('FormerThoughtBreakPattern','FormerThoughtCodeWrapperPattern','FormerThoughtOpeningFencePattern','FormerThoughtClosingFencePattern','FormerThoughtPresentationWrapperPattern','FormerThoughtExcessLineBreakPattern','StructuredFieldPattern','MinecraftQuotedProjectNamePattern','MinecraftExplicitProjectNamePattern','MinecraftNamedProjectPattern','MarkdownHeadingProjectNamePattern','IdentifierSeparatorPattern','AlphaNumericWordPattern','IntegerPattern')) {
    if ($seed.IndexOf("nameof(ICouncilTextPatternDataService.$name)", [StringComparison]::Ordinal) -lt 0) { $failures.Add("Database seed is missing required Council text pattern: $name") }
}

$runtimePolicySeedPath = Join-Path $sourceRoot 'Services\Persistence\LocalGptRuntimePolicySeedDataService.cs'
$runtimePolicyStorePath = Join-Path $sourceRoot 'Services\Persistence\LocalGptRuntimePolicyStoreService.cs'
$runtimePolicyDataPath = Join-Path $sourceRoot 'Services\Persistence\LocalGptRuntimePolicyDataService.cs'
$runtimePolicyControllerPath = Join-Path $sourceRoot 'Controller\RuntimePolicyController.cs'
$databaseInitializationPath = Join-Path $sourceRoot 'Services\Persistence\DatabaseInitializationService.cs'
foreach ($requiredPath in @($runtimePolicySeedPath, $runtimePolicyStorePath, $runtimePolicyDataPath, $runtimePolicyControllerPath, $databaseInitializationPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) { $failures.Add("Runtime-policy data boundary is missing: $requiredPath") }
}
if ((Test-Path -LiteralPath $runtimePolicySeedPath -PathType Leaf) -and
    (Test-Path -LiteralPath $runtimePolicyStorePath -PathType Leaf) -and
    (Test-Path -LiteralPath $runtimePolicyDataPath -PathType Leaf) -and
    (Test-Path -LiteralPath $runtimePolicyControllerPath -PathType Leaf) -and
    (Test-Path -LiteralPath $databaseInitializationPath -PathType Leaf)) {
    $runtimePolicySeed = [IO.File]::ReadAllText($runtimePolicySeedPath)
    $runtimePolicyStore = [IO.File]::ReadAllText($runtimePolicyStorePath)
    $runtimePolicyData = [IO.File]::ReadAllText($runtimePolicyDataPath)
    $runtimePolicyController = [IO.File]::ReadAllText($runtimePolicyControllerPath)
    $databaseInitialization = [IO.File]::ReadAllText($databaseInitializationPath)

    foreach ($required in @(
        'LocalGptRuntimeSystemVariableSeed',
        'LocalGptRuntimeRegexSeed',
        'runtime.native.powershell-inline-command',
        'runtime.native.powershell-file',
        'runtime.native.sensitive-argument',
        'AllowedNativeExecutablesJson',
        'RegexMatchTimeoutMilliseconds'
    )) {
        if ($runtimePolicySeed.IndexOf($required, [StringComparison]::Ordinal) -lt 0) { $failures.Add("Runtime-policy seed lost required database feed token: $required") }
    }
    foreach ($required in @(
        'db.SystemVariables.AsNoTracking()',
        'db.RegexPatterns.AsNoTracking()',
        'seed.PowerShellInlineCommandRegexName',
        'seed.PowerShellFileRegexName',
        'seed.SensitiveArgumentRegexName',
        'seed.RegexTimeoutVariableName'
    )) {
        if ($runtimePolicyStore.IndexOf($required, [StringComparison]::Ordinal) -lt 0) { $failures.Add("Runtime-policy store lost database ownership token: $required") }
    }
    foreach ($forbidden in @(
        '-EncodedCommand',
        'api[-_]?key',
        'powershell.exe',
        'Guid.Parse("7f4d7b4a-b622-4d15-8e44-9dfae2aa6101")'
    )) {
        if ($runtimePolicyData.IndexOf($forbidden, [StringComparison]::Ordinal) -ge 0) { $failures.Add("Runtime-policy runtime service reintroduced hidden seed data: $forbidden") }
    }
    foreach ($required in @(
        'ILocalGptRuntimePolicyStoreService store',
        'store.GetDefinition()',
        'new Regex(',
        'definition.PowerShellInlineCommandPattern.Pattern',
        'definition.PowerShellFilePattern.Pattern',
        'definition.SensitiveArgumentPattern.Pattern'
    )) {
        if ($runtimePolicyData.IndexOf($required, [StringComparison]::Ordinal) -lt 0) { $failures.Add("Runtime-policy runtime service lost compiled database-backed token: $required") }
    }
    foreach ($required in @(
        '[HttpGet("seed")]',
        '[HttpGet("definition")]',
        '[HttpPost("reload")]',
        'runtimePolicySeed.GetSeed()',
        'runtimePolicyStore.GetDefinition()',
        'runtimePolicy.Reload()'
    )) {
        if ($runtimePolicyController.IndexOf($required, [StringComparison]::Ordinal) -lt 0) { $failures.Add("Runtime-policy controller lost service boundary token: $required") }
    }
    if ($databaseInitialization.IndexOf('runtimePolicySeed.GetSeed().LocalGptCoreProjectId', [StringComparison]::Ordinal) -lt 0) {
        $failures.Add('Database initialization must use the runtime-policy seed model for the LocalGPT Core project identifier.')
    }
    if ($seed.IndexOf('runtimePolicySeed.GetSeed().RegexPatterns', [StringComparison]::Ordinal) -lt 0 -or
        $seed.IndexOf('runtimePolicySeed.GetSeed().SystemVariables', [StringComparison]::Ordinal) -lt 0) {
        $failures.Add('InitialDataCatalog must merge the runtime-policy regex and system-variable first-run feeds.')
    }
    foreach ($required in @(
        'AddSingleton<ILocalGptRuntimePolicySeedDataService, LocalGptRuntimePolicySeedDataService>()',
        'AddSingleton<ILocalGptRuntimePolicyStoreService, LocalGptRuntimePolicyStoreService>()',
        'AddSingleton<ILocalGptRuntimePolicyDataService, LocalGptRuntimePolicyDataService>()'
    )) {
        if ($program.IndexOf($required, [StringComparison]::Ordinal) -lt 0) { $failures.Add("Runtime-policy DI ownership is missing: $required") }
    }
}

if ($failures.Count -gt 0) { $failures | ForEach-Object { Write-Error $_ }; exit 1 }
Write-Host 'Runtime-value ownership passed. Council text and runtime command-policy values are database-backed, and the removal-only magic-value baseline did not grow.'

