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


function Test-ContainsOrdinal([string]$text, [string]$token) {
    if ($null -eq $text -or $null -eq $token) {
        return $false
    }
    return $text.IndexOf($token, [StringComparison]::Ordinal) -ge 0
}

$regexService = Read-RequiredText 'src/LocalGPT/Services/Persistence/RegexPatternService.cs'
if (-not (Test-ContainsOrdinal $regexService 'using LocalGPT.Interfaces;')) {
    $errors.Add('RegexPatternService must import LocalGPT.Interfaces for IDatabaseInitializationService.')
}

$themeService = Read-RequiredText 'src/LocalGPT/Services/ThemeService.cs'
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
    if (-not (Test-ContainsOrdinal $themeService $token)) {
        $errors.Add("ThemeService must retain '$token'.")
    }
}


$themeModel = Read-RequiredText 'src/LocalGPT/BusinessObjects/Theme.cs'
foreach ($token in @(
    'public ITheme DevExpressTheme { get; }',
    'public string BootstrapThemeMode { get; }',
    'public bool IsBootstrapNative { get; }')) {
    if (-not (Test-ContainsOrdinal $themeModel $token)) {
        $errors.Add("Theme model must retain '$token'.")
    }
}

$dispatcher = Read-RequiredText 'src/LocalGPT/Components/Layout/ThemeJsChangeDispatcher.cs'
foreach ($token in @(
    'IThemeChangeService DevExpressThemeChangeService',
    'private IJSRuntime JsRuntime { get; set; } = default!;',
    'private NavigationManager NavigationManager { get; set; } = default!;',
    '.ToAbsoluteUri(versionedThemeModulePath)',
    '.SetTheme(theme.DevExpressTheme)',
    'private ThemeService Themes { get; set; } = default!;',
    'InitialShellThemeName',
    'InitialComponentThemeName',
    'ThemeApplicationTarget target',
    '"readThemeState"',
    '"applyThemeState"')) {
    if (-not (Test-ContainsOrdinal $dispatcher $token)) {
        $errors.Add("ThemeJsChangeDispatcher must retain '$token'.")
    }
}
if ((Test-ContainsOrdinal $dispatcher 'new ThemeService(')) {
    $errors.Add('ThemeJsChangeDispatcher must not construct ThemeService manually.')
}
if ((Test-ContainsOrdinal $dispatcher 'ISafeJSRuntime')) {
    $errors.Add('ThemeJsChangeDispatcher must use the interactive circuit IJSRuntime; ISafeJSRuntime can return a null module reference for dynamic imports.')
}
if ((Test-ContainsOrdinal $dispatcher '.InvokeAsync<IJSObjectReference>("import", versionedThemeModulePath)')) {
    $errors.Add('ThemeJsChangeDispatcher must resolve the versioned module path to an absolute URI before dynamic import; browsers reject a bare module specifier.')
}

$setThemeCount = ([regex]::Matches($dispatcher, '\.SetTheme\(')).Count
$awaitedSetThemeCount = ([regex]::Matches($dispatcher, 'await\s+DevExpressThemeChangeService\s*\r?\n\s*\.SetTheme\(')).Count
if ($setThemeCount -ne $awaitedSetThemeCount) {
    $errors.Add("Every DevExpress theme change must be awaited; found $setThemeCount calls and $awaitedSetThemeCount awaited calls.")
}

$app = Read-RequiredText 'src/LocalGPT/Components/App.razor'
if (-not (Test-ContainsOrdinal $app '@DxResourceManager.RegisterTheme(Themes.ActiveComponentTheme.DevExpressTheme)')) {
    $errors.Add('App.razor must register the validated component ITheme with DxResourceManager.')
}
foreach ($token in @('data-localgpt-shell-theme', 'data-localgpt-component-theme', 'ShellThemeCookieName', 'ComponentThemeCookieName')) {
    if (-not (Test-ContainsOrdinal $app $token)) {
        $errors.Add("App.razor must retain dual-theme startup token '$token'.")
    }
}
foreach ($forbidden in @('bs-theme-link', 'dx-theme-link', 'GetThemeCssUrl(Themes.ActiveTheme)', 'GetBootstrapThemeCssUrl(Themes.ActiveTheme)')) {
    if ((Test-ContainsOrdinal $app $forbidden)) {
        $errors.Add("App.razor must not restore manual theme-link wiring: $forbidden")
    }
}

$controller = Read-RequiredText 'src/LocalGPT/wwwroot/switcher-resources/theme-controller.js'
foreach ($required in @('readThemeState', 'applyThemeState', 'persistThemeState', 'data-bs-theme', 'data-localgpt-shell-theme', 'data-localgpt-component-theme', 'ActiveShellTheme', 'ActiveComponentTheme')) {
    if (-not (Test-ContainsOrdinal $controller $required)) {
        $errors.Add("theme-controller.js must retain '$required'.")
    }
}

$persistIndex = $controller.IndexOf('persistThemeState(shellThemeName, componentThemeName);', [StringComparison]::Ordinal)
$highlightIndex = $controller.IndexOf('await updateHighlightTheme(highlightUrl, signal);', [StringComparison]::Ordinal)
if ($persistIndex -lt 0 -or $highlightIndex -lt 0 -or $persistIndex -gt $highlightIndex) {
    $errors.Add('theme-controller.js must persist and apply both theme names before asynchronous highlight stylesheet loading.')
}

foreach ($forbidden in @('setStylesheetLinks', 'dx-theme-link', 'bs-theme-link', 'bootstrap-external.bs5')) {
    if ((Test-ContainsOrdinal $controller $forbidden)) {
        $errors.Add("theme-controller.js must not manage DevExpress/Bootstrap component-theme links: $forbidden")
    }
}

$contract = Read-RequiredText 'src/LocalGPT/wwwroot/css/localgpt-theme-contract.css'
foreach ($token in @('--localgpt-body-bg', '--localgpt-border-color', '--localgpt-primary-rgb', 'Do not override DevExpress internal control selectors')) {
    if (-not (Test-ContainsOrdinal $contract $token)) {
        $errors.Add("LocalGPT theme CSS contract must retain '$token'.")
    }
}

$themeSwitcher = Read-RequiredText 'src/LocalGPT/Components/Layout/ThemeSwitcher.razor'
foreach ($token in @('IHttpContextAccessor HttpContextAccessor', 'LegacyThemeCookieName', 'ShellThemeCookieName', 'ComponentThemeCookieName', 'Themes.InitializeThemes', 'Themes.GetThemeLayerCssClass')) {
    if (-not (Test-ContainsOrdinal $themeSwitcher $token)) {
        $errors.Add("ThemeSwitcher must retain '$token'.")
    }
}



$themeLoadNotifier = Read-RequiredText 'src/LocalGPT/Interfaces/IThemeLoadNotifier.cs'
if ((Test-ContainsOrdinal $themeLoadNotifier 'namespace LocalGPT.Interfaces;')) {
    $errors.Add('IThemeLoadNotifier must not mix a file-scoped namespace semicolon with a block-scoped namespace body.')
}
if (-not (Test-ContainsOrdinal $themeLoadNotifier 'namespace LocalGPT.Interfaces') -or
    -not (Test-ContainsOrdinal $themeLoadNotifier 'public interface IThemeLoadNotifier')) {
    $errors.Add('IThemeLoadNotifier must retain a valid block-scoped LocalGPT.Interfaces declaration.')
}

$themeSwitcherContainer = Read-RequiredText 'src/LocalGPT/Components/Layout/ThemeSwitcherContainer.razor'
foreach ($token in @('await InvokeAsync(async () =>', 'await ShownChanged.InvokeAsync(shown)', 'await ShellThemeNameChanged.InvokeAsync(theme.Name)', 'await ComponentThemeNameChanged.InvokeAsync(theme.Name)', 'ThemeApplicationTarget.Shell', 'ThemeApplicationTarget.Components')) {
    if (-not (Test-ContainsOrdinal $themeSwitcherContainer $token)) {
        $errors.Add("ThemeSwitcherContainer must retain dispatcher-safe token '$token'.")
    }
}
if ((Test-ContainsOrdinal $themeSwitcherContainer 'ConfigureAwait(false)')) {
    $errors.Add('Blazor theme component callbacks must not leave the renderer dispatcher with ConfigureAwait(false).')
}

foreach ($relative in @(
    'src/LocalGPT/Components/Layout/ThemeSwitcher.razor',
    'src/LocalGPT/Components/Layout/ThemeSwitcherContainer.razor',
    'src/LocalGPT/Components/Layout/ThemeSwitcherItem.razor')) {
    $themeIslandChild = Read-RequiredText $relative
    if ((Test-ContainsOrdinal $themeIslandChild '@rendermode')) {
        $errors.Add("$relative must inherit the single MenuIsland render mode instead of creating a competing theme circuit.")
    }
}

$minecraft = Read-RequiredText 'src/LocalGPT/Controller/MinecraftDiagnosticController.cs'
if (-not (Test-ContainsOrdinal $minecraft '?? throw new InvalidOperationException("The approved datapack build did not produce a command result.")')) {
    $errors.Add('MinecraftDiagnosticController must handle a missing approved command result explicitly.')
}
if ((Test-ContainsOrdinal $minecraft 'Confidence = build?.Succeeded')) {
    $errors.Add('MinecraftDiagnosticController must not restore nullable build-result confidence logic.')
}

foreach ($relative in @(
    'src/LocalGPT/wwwroot/switcher-resources/css/theme-switcher.css',
    'src/LocalGPT/wwwroot/switcher-resources/css/themes.css',
    'src/LocalGPT/wwwroot/switcher-resources/css/themes/default/bootstrap.min.css',
    'src/LocalGPT/wwwroot/js/getCookie.js',
    'src/LocalGPT/Components/InteractiveStartupMarker.razor',
    'src/LocalGPT/Components/Layout/ThemeSwitcher.razor',
    'src/LocalGPT/Components/Layout/ThemeSwitcherContainer.razor',
    'src/LocalGPT/Components/Layout/ThemeSwitcherItem.razor')) {
    if (-not (Test-Path -LiteralPath (Join-Path $RepositoryRoot $relative) -PathType Leaf)) {
        $errors.Add("Older component/theme feature resource is missing: $relative")
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'DevExpress theme runtime, CSS contract, feature-preservation, and compiler-feedback guards verified.'
