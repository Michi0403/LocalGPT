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
    '"applyThemeState"', 'InvokeAsync(() => NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true))')) {
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
foreach ($required in @('readThemeState', 'applyThemeState', 'persistThemeState', 'data-bs-theme', 'data-localgpt-shell-theme', 'data-localgpt-component-theme', 'ActiveShellTheme', 'ActiveComponentTheme', 'export function resetFusionRoute(shellThemeName, componentThemeName)')) {
    if (-not (Test-ContainsOrdinal $controller $required)) {
        $errors.Add("theme-controller.js must retain '$required'.")
    }
}

$persistIndex = $controller.IndexOf('persistThemeState(shellThemeName, componentThemeName);', [StringComparison]::Ordinal)
$highlightIndex = $controller.IndexOf('await updateHighlightTheme(highlightUrl, signal);', [StringComparison]::Ordinal)
$resetFunctionStart = $controller.IndexOf('export function resetFusionRoute(', [StringComparison]::Ordinal)
$resetFunctionEnd = $controller.IndexOf('export async function applyThemeState(', [StringComparison]::Ordinal)
if ($resetFunctionStart -lt 0 -or $resetFunctionEnd -le $resetFunctionStart -or
    $controller.Substring($resetFunctionStart, $resetFunctionEnd - $resetFunctionStart) -match 'location\.reload\(') {
    $errors.Add('Theme reset must return from JS interop before a Blazor-owned reload; doing both in one JS call cancels its awaited response.')
}

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

# Navigation follows the independently selected SHELL palette; DevExpress theme selection
# must not reintroduce a dark hard-coded drawer into light themes such as Office White.
foreach ($token in @(
    '--localgpt-navigation-bg: var(--localgpt-shell-surface-bg, var(--localgpt-surface-raised-bg));',
    '--localgpt-navigation-color: var(--localgpt-shell-foreground, var(--localgpt-body-color));',
    '--localgpt-navigation-hover-bg: color-mix(',
    'html[data-localgpt-shell-theme="office-white"]',
    '--bs-secondary-bg: #f2f3f5;')) {
    if (-not (Test-ContainsOrdinal $contract $token)) {
        $errors.Add("Navigation must follow the selected shell palette: $token")
    }
}
# Literal independent shell tokens protect mixed DevExpress/Bootstrap theme selection.
$blocks = [regex]::Matches($contract, 'html\[data-localgpt-shell-theme="(?<name>[^"]+)"\]\s*\{(?<rules>[^}]+)\}')
if ($blocks.Count -lt 21) { $errors.Add('Shell palette coverage is incomplete.') }
foreach ($block in $blocks) {
    foreach ($token in @('accent','accent-contrast','foreground','background','surface-bg','border')) {
        if ($block.Groups['rules'].Value -notmatch ('--localgpt-shell-' + $token + ':\s*#[0-9a-fA-F]{6};')) {
            $errors.Add("Shell theme $($block.Groups['name'].Value) lacks literal $token palette.")
        }
    }
}
$microphoneCss = Read-RequiredText 'src/LocalGPT/Components/Shared/ChatMicrophone.razor.css'
foreach ($token in @('--localgpt-shell-foreground', '--localgpt-shell-surface-bg', '.chat-microphone.is-recording')) {
    if (-not (Test-ContainsOrdinal $microphoneCss $token)) { $errors.Add("Microphone lost mixed-theme contrast protection: $token") }
}
$drawerCss = Read-RequiredText 'src/LocalGPT/Components/Layout/Drawer.razor.css'
foreach ($token in @('--localgpt-navigation-bg', '--localgpt-navigation-color')) {
    if (-not (Test-ContainsOrdinal $drawerCss $token)) {
        $errors.Add("Navigation drawer lost its shell palette binding: $token")
    }
}

$switcherCss = Read-RequiredText 'src/LocalGPT/wwwroot/switcher-resources/css/theme-switcher.css'
foreach ($token in @('position: static;', 'position: relative;', 'background-color: var(--localgpt-body-bg);', 'grid-column: 1 / -1;', '.list-group > .razor-component-boundary')) {
    if (-not (Test-ContainsOrdinal $switcherCss $token)) {
        $errors.Add("Theme selector must remain shell-colored and viewport-contained: $token")
    }
}
if ((Test-ContainsOrdinal $switcherCss 'width: min(1920px, 78vw);') -or
    (Test-ContainsOrdinal $switcherCss 'min-width: min(960px, 96vw);') -or
    (Test-ContainsOrdinal $switcherCss 'position: fixed;')) {
    $errors.Add('Theme selector must not restore its oversized 78vw or forced 960px popup.')
}
$themeSwitcher = Read-RequiredText 'src/LocalGPT/Components/Layout/ThemeSwitcher.razor'
if (-not (Test-ContainsOrdinal $themeSwitcher '"themeswitcher-button theme-settings is-open"')) {
    $errors.Add('Theme selector must retain semantic open-state styling (not a literal True/False class).')
}
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
foreach ($token in @('<DxPopup Visible="@Shown"', 'VisibleChanged="ToggleThemeSwitcherPanel"', 'Width="min(1360px, 96vw)"', 'await InvokeAsync(async () =>', 'await ShownChanged.InvokeAsync(shown)', 'await ShellThemeNameChanged.InvokeAsync(theme.Name)', 'await ComponentThemeNameChanged.InvokeAsync(theme.Name)', 'ThemeApplicationTarget.Shell', 'ThemeApplicationTarget.Components')) {
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
