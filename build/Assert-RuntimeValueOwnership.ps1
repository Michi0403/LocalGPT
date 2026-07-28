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
if ($failures.Count -gt 0) { $failures | ForEach-Object { Write-Error $_ }; exit 1 }
Write-Host 'Runtime-value ownership passed. Council text values are database-backed and the removal-only magic-value baseline did not grow.'
