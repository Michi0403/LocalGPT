param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
$errors = [System.Collections.Generic.List[string]]::new()

function Read-RequiredText([string]$relativePath) {
    $path = Join-Path $RepositoryRoot $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $errors.Add("Missing theme architecture file: $relativePath")
        return ''
    }
    return Get-Content -LiteralPath $path -Raw
}

$regexService = Read-RequiredText 'LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/RegexPatternService.cs'
if (-not $regexService.Contains('using LocalGPT.Interfaces;', [StringComparison]::Ordinal)) {
    $errors.Add('RegexPatternService must import LocalGPT.Interfaces for IDatabaseInitializationService.')
}

$themeService = Read-RequiredText 'LocalGPTWebviewWrapper/LocalGPT/Services/ThemeService.cs'
foreach ($token in @(
    'public sealed class ThemeService',
    'DxThemes.BootstrapExternal.Clone',
    'DxThemes.Fluent.Clone',
    'properties.UseBootstrapStyles = true;',
    'properties.ApplyToPageElements = false;',
    'public Theme ActiveShellTheme => activeShellTheme;',
    'public Theme ActiveComponentTheme => activeComponentTheme;',
    'ShellThemeCookieName',
    'ComponentThemeCookieName',
    'properties.AddFilePaths(LocalThemeContractPath);',
    'CreateClassic("blazing-berry"')) {
    if (-not $themeService.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("ThemeService must retain '$token'.")
    }
}


$themeModel = Read-RequiredText 'LocalGPTWebviewWrapper/LocalGPT/BusinessObjects/Theme.cs'
foreach ($token in @(
    'public ITheme DevExpressTheme { get; }',
    'public string BootstrapThemeMode { get; }',
    'public bool IsBootstrapNative { get; }')) {
    if (-not $themeModel.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("Theme model must retain '$token'.")
    }
}

$dispatcher = Read-RequiredText 'LocalGPTWebviewWrapper/LocalGPT/Components/Layout/ThemeJsChangeDispatcher.cs'
foreach ($token in @(
    'IThemeChangeService DevExpressThemeChangeService',
    '.SetTheme(theme.DevExpressTheme)',
    'private ThemeService Themes { get; set; } = default!;',
    'InitialShellThemeName',
    'InitialComponentThemeName',
    'ThemeApplicationTarget target',
    '"readThemeState"',
    '"applyThemeState"')) {
    if (-not $dispatcher.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("ThemeJsChangeDispatcher must retain '$token'.")
    }
}
if ($dispatcher.Contains('new ThemeService(', [StringComparison]::Ordinal)) {
    $errors.Add('ThemeJsChangeDispatcher must not construct ThemeService manually.')
}

$setThemeCount = ([regex]::Matches($dispatcher, '\.SetTheme\(')).Count
$awaitedSetThemeCount = ([regex]::Matches($dispatcher, 'await\s+DevExpressThemeChangeService\s*\r?\n\s*\.SetTheme\(')).Count
if ($setThemeCount -ne $awaitedSetThemeCount) {
    $errors.Add("Every DevExpress theme change must be awaited; found $setThemeCount calls and $awaitedSetThemeCount awaited calls.")
}

$app = Read-RequiredText 'LocalGPTWebviewWrapper/LocalGPT/Components/App.razor'
if (-not $app.Contains('@DxResourceManager.RegisterTheme(Themes.ActiveComponentTheme.DevExpressTheme)', [StringComparison]::Ordinal)) {
    $errors.Add('App.razor must register the validated component ITheme with DxResourceManager.')
}
foreach ($token in @('data-localgpt-shell-theme', 'data-localgpt-component-theme', 'ShellThemeCookieName', 'ComponentThemeCookieName')) {
    if (-not $app.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("App.razor must retain dual-theme startup token '$token'.")
    }
}
foreach ($forbidden in @('bs-theme-link', 'dx-theme-link', 'GetThemeCssUrl(Themes.ActiveTheme)', 'GetBootstrapThemeCssUrl(Themes.ActiveTheme)')) {
    if ($app.Contains($forbidden, [StringComparison]::Ordinal)) {
        $errors.Add("App.razor must not restore manual theme-link wiring: $forbidden")
    }
}

$controller = Read-RequiredText 'LocalGPTWebviewWrapper/LocalGPT/wwwroot/switcher-resources/theme-controller.js'
foreach ($required in @('readThemeState', 'applyThemeState', 'data-bs-theme', 'data-localgpt-shell-theme', 'data-localgpt-component-theme', 'ActiveShellTheme', 'ActiveComponentTheme')) {
    if (-not $controller.Contains($required, [StringComparison]::Ordinal)) {
        $errors.Add("theme-controller.js must retain '$required'.")
    }
}
foreach ($forbidden in @('setStylesheetLinks', 'dx-theme-link', 'bs-theme-link', 'bootstrap-external.bs5')) {
    if ($controller.Contains($forbidden, [StringComparison]::Ordinal)) {
        $errors.Add("theme-controller.js must not manage DevExpress/Bootstrap component-theme links: $forbidden")
    }
}

$contract = Read-RequiredText 'LocalGPTWebviewWrapper/LocalGPT/wwwroot/css/localgpt-theme-contract.css'
foreach ($token in @('--localgpt-body-bg', '--localgpt-border-color', '--localgpt-primary-rgb', 'Do not override DevExpress internal control selectors')) {
    if (-not $contract.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("LocalGPT theme CSS contract must retain '$token'.")
    }
}

$themeSwitcher = Read-RequiredText 'LocalGPTWebviewWrapper/LocalGPT/Components/Layout/ThemeSwitcher.razor'
foreach ($token in @('IHttpContextAccessor HttpContextAccessor', 'LegacyThemeCookieName', 'ShellThemeCookieName', 'ComponentThemeCookieName', 'Themes.InitializeThemes') {
    if (-not $themeSwitcher.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("ThemeSwitcher must retain '$token'.")
    }
}


$themeSwitcherContainer = Read-RequiredText 'LocalGPTWebviewWrapper/LocalGPT/Components/Layout/ThemeSwitcherContainer.razor'
foreach ($token in @('await InvokeAsync(async () =>', 'await ShownChanged.InvokeAsync(shown)', 'await ShellThemeNameChanged.InvokeAsync(theme.Name)', 'await ComponentThemeNameChanged.InvokeAsync(theme.Name)', 'ThemeApplicationTarget.Shell', 'ThemeApplicationTarget.Components')) {
    if (-not $themeSwitcherContainer.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("ThemeSwitcherContainer must retain dispatcher-safe token '$token'.")
    }
}
if ($themeSwitcherContainer.Contains('ConfigureAwait(false)', [StringComparison]::Ordinal)) {
    $errors.Add('Blazor theme component callbacks must not leave the renderer dispatcher with ConfigureAwait(false).')
}

foreach ($relative in @(
    'LocalGPTWebviewWrapper/LocalGPT/Components/Layout/ThemeSwitcher.razor',
    'LocalGPTWebviewWrapper/LocalGPT/Components/Layout/ThemeSwitcherContainer.razor',
    'LocalGPTWebviewWrapper/LocalGPT/Components/Layout/ThemeSwitcherItem.razor')) {
    $themeIslandChild = Read-RequiredText $relative
    if ($themeIslandChild.Contains('@rendermode', [StringComparison]::Ordinal)) {
        $errors.Add("$relative must inherit the single MenuIsland render mode instead of creating a competing theme circuit.")
    }
}

$minecraft = Read-RequiredText 'LocalGPTWebviewWrapper/LocalGPT/Controller/MinecraftDiagnosticController.cs'
if (-not $minecraft.Contains('?? throw new InvalidOperationException("The approved datapack build did not produce a command result.")', [StringComparison]::Ordinal)) {
    $errors.Add('MinecraftDiagnosticController must handle a missing approved command result explicitly.')
}
if ($minecraft.Contains('Confidence = build?.Succeeded', [StringComparison]::Ordinal)) {
    $errors.Add('MinecraftDiagnosticController must not restore nullable build-result confidence logic.')
}

foreach ($relative in @(
    'LocalGPTWebviewWrapper/LocalGPT/wwwroot/switcher-resources/css/theme-switcher.css',
    'LocalGPTWebviewWrapper/LocalGPT/wwwroot/switcher-resources/css/themes.css',
    'LocalGPTWebviewWrapper/LocalGPT/wwwroot/switcher-resources/css/themes/default/bootstrap.min.css',
    'LocalGPTWebviewWrapper/LocalGPT/wwwroot/js/getCookie.js',
    'LocalGPTWebviewWrapper/LocalGPT/Components/InteractiveStartupMarker.razor',
    'LocalGPTWebviewWrapper/LocalGPT/Components/Layout/ThemeSwitcher.razor',
    'LocalGPTWebviewWrapper/LocalGPT/Components/Layout/ThemeSwitcherContainer.razor',
    'LocalGPTWebviewWrapper/LocalGPT/Components/Layout/ThemeSwitcherItem.razor')) {
    if (-not (Test-Path -LiteralPath (Join-Path $RepositoryRoot $relative) -PathType Leaf)) {
        $errors.Add("Older component/theme feature resource is missing: $relative")
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'DevExpress theme runtime, CSS contract, feature-preservation, and compiler-feedback guards verified.'
