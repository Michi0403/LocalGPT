param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
$sourceRoot = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT'
$servicesRoot = Join-Path $sourceRoot 'Services'
$errors = [System.Collections.Generic.List[string]]::new()

if (-not (Test-Path -LiteralPath $servicesRoot -PathType Container)) {
    throw "Services directory not found: $servicesRoot"
}

$serviceFiles = Get-ChildItem -LiteralPath $servicesRoot -Recurse -File -Filter '*.cs'
foreach ($file in $serviceFiles) {
    $relative = [IO.Path]::GetRelativePath($RepositoryRoot, $file.FullName).Replace('\', '/')
    $text = Get-Content -LiteralPath $file.FullName -Raw

    $staticClasses = [regex]::Matches(
        $text,
        '(?m)^\s*(?:public|internal|private|protected)?\s*static\s+class\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)')
    foreach ($match in $staticClasses) {
        $name = $match.Groups['name'].Value
        $isApprovedHelper = $relative.Contains('/Services/Helpers/', [StringComparison]::Ordinal) -and
            $name.EndsWith('Helper', [StringComparison]::Ordinal)
        if (-not $isApprovedHelper) {
            $errors.Add("Static runtime class '$name' is not an approved pure helper: $relative")
        }
    }

    if ($text -match '(?m)^\s*(?:public|internal)\s+static\s+class\s+[A-Za-z_][A-Za-z0-9_]*(Service|Client|Registry|Runner)\b') {
        $errors.Add("Runtime services/clients/registries/runners must be DI instances, not static classes: $relative")
    }

    if ($text -match '(?m)^\s*(?:public|internal|private|protected)?\s*static\s+(?:readonly\s+)?(?:HashSet|Dictionary|List|ConcurrentDictionary|ConcurrentQueue|ConcurrentBag|ObservableCollection)<[^>]+>\s+[A-Za-z_][A-Za-z0-9_]*\s*(?:=|\{)') {
        $errors.Add("Mutable collection state must not be stored in static service fields; use an immutable/frozen catalog or a DI-owned instance: $relative")
    }
}

$maintainedFiles = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File |
    Where-Object { $_.Extension -in @('.cs', '.razor') -and
        $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
foreach ($file in $maintainedFiles) {
    $relative = [IO.Path]::GetRelativePath($RepositoryRoot, $file.FullName).Replace('\', '/')
    $text = Get-Content -LiteralPath $file.FullName -Raw

    if ($text -match '(?m)^\s*_\s*=(?!>)\s*[^;\r\n]*Async\s*\(') {
        $errors.Add("Discarded asynchronous work is forbidden; await it or use ISupervisedTaskRunner: $relative")
    }

    if ($text -match '(?m)^\s*(?!await\s).*DevExpressThemeChangeService\s*\.\s*SetTheme\s*\(') {
        $errors.Add("Every DevExpress theme change must be awaited: $relative")
    }

    if ($text -match '(?m)^\s*new\s+(ThemeService|DatabaseInitializationService|DatabaseMigrationCompatibilityService|ComponentActivityService|SupervisedTaskRunner)\s*\(') {
        $errors.Add("Runtime services must be resolved through DI, not manually constructed: $relative")
    }
}

$programPath = Join-Path $sourceRoot 'Program.cs'
$program = Get-Content -LiteralPath $programPath -Raw
foreach ($token in @(
    'AddSingleton<ComponentActivityService>()',
    'AddSingleton<IServiceActivityService>',
    'AddSingleton<ISupervisedTaskRunner, SupervisedTaskRunner>()',
    'AddSingleton<IDatabaseMigrationCompatibilityService, DatabaseMigrationCompatibilityService>()',
    'AddScoped<ThemeService>()')) {
    if (-not $program.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("Program.cs must retain DI registration '$token'.")
    }
}

$themeDispatcherPath = Join-Path $sourceRoot 'Components/Layout/ThemeJsChangeDispatcher.cs'
$themeDispatcher = Get-Content -LiteralPath $themeDispatcherPath -Raw
$setThemeCount = ([regex]::Matches($themeDispatcher, '\.SetTheme\(')).Count
$awaitedSetThemeCount = ([regex]::Matches($themeDispatcher, 'await\s+DevExpressThemeChangeService\s*\r?\n\s*\.SetTheme\(')).Count
if ($setThemeCount -ne $awaitedSetThemeCount) {
    $errors.Add("ThemeJsChangeDispatcher has $setThemeCount theme changes but only $awaitedSetThemeCount awaited calls.")
}

$compatibilityPath = Join-Path $sourceRoot 'Services/Persistence/DatabaseMigrationCompatibilityService.cs'
$compatibility = Get-Content -LiteralPath $compatibilityPath -Raw
foreach ($token in @(
    'public sealed class DatabaseMigrationCompatibilityService',
    'IServiceActivityService serviceActivity',
    'signature.Requirements.Length')) {
    if (-not $compatibility.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("Database migration compatibility service must retain '$token'.")
    }
}
if ($compatibility -match 'signature\.Requirements\.Count\s*(\r?\n|\?|:)') {
    $errors.Add('Migration signature cardinality must not regress to the Enumerable.Count method group.')
}


$catalogPath = Join-Path $sourceRoot 'Services/LocalGptCatalogService.cs'
$catalog = Get-Content -LiteralPath $catalogPath -Raw
foreach ($token in @(
    'using System.Collections.Frozen;',
    'FrozenSet<string> ExcludedDirectoryNames',
    'FrozenSet<string> BinaryExtensions',
    'FrozenSet<string> SourceExtensions',
    'FrozenSet<string> ArtifactTextExtensions')) {
    if (-not $catalog.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("LocalGptCatalogService must retain immutable catalog token '$token'.")
    }
}
if ($catalog -match 'static\s+(?:readonly\s+)?HashSet<') {
    $errors.Add('LocalGptCatalogService must not expose mutable static HashSet state.')
}

foreach ($component in @(
    'Components/Layout/HumanCollaborationInbox.razor',
    'Components/Routes.razor',
    'Components/Pages/Chat.razor')) {
    $path = Join-Path $sourceRoot $component
    $text = Get-Content -LiteralPath $path -Raw
    foreach ($token in @(
        '@inject ISupervisedTaskRunner TaskRunner',
        'CancellationTokenSource',
        'isDisposed',
        'catch (ObjectDisposedException) when (isDisposed)')) {
        if (-not $text.Contains($token, [StringComparison]::Ordinal)) {
            $errors.Add("$component must retain supervised lifetime token '$token'.")
        }
    }
}

$chatPath = Join-Path $sourceRoot 'Components/Pages/Chat.razor'
$chat = Get-Content -LiteralPath $chatPath -Raw
if (-not $chat.Contains('CancellationTokenSource.CreateLinkedTokenSource(componentLifetimeCts.Token)', [StringComparison]::Ordinal)) {
    $errors.Add('Chat autosave must remain linked to the owning component lifetime token.')
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Service lifecycle, static-state, and asynchronous supervision contracts verified.'
