Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "InteractiveServer render-mode validation failed: $Message" }

$root = Split-Path -Parent $PSScriptRoot
$appRoot = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT'
$expected = [ordered]@{
    'Components/InteractiveStartupMarker.razor' = '@rendermode @(new InteractiveServerRenderMode(prerender: false))'
    'Components/Layout/CouncilSpoolerPanel.razor' = '@rendermode @(new InteractiveServerRenderMode(prerender: false))'
    'Components/Layout/HumanCollaborationInbox.razor' = '@rendermode @(new InteractiveServerRenderMode(prerender: false))'
    'Components/Layout/MenuIsland.razor' = '@rendermode @(new InteractiveServerRenderMode(prerender: false))'
    'Components/Layout/NavMenu.razor' = '@rendermode InteractiveServer'
    'Components/Layout/ToastWrapper.razor' = '@rendermode @(new InteractiveServerRenderMode(prerender: false))'
    'Components/Pages/Chat.razor' = '@rendermode InteractiveServer'
    'Components/Pages/CouncilTeams.razor' = '@rendermode InteractiveServer'
    'Components/Pages/Database.razor' = '@rendermode InteractiveServer'
    'Components/Pages/DxFunctionCatalog.razor' = '@rendermode InteractiveServer'
    'Components/Pages/Install.razor' = '@rendermode InteractiveServer'
    'Components/Pages/MinecraftModBuilder.razor' = '@rendermode InteractiveServer'
    'Components/Pages/ModelCouncil.razor' = '@rendermode InteractiveServer'
    'Components/Pages/OneWireSecurity.razor' = '@rendermode InteractiveServer'
    'Components/Pages/ProjectMaintenance.razor' = '@rendermode InteractiveServer'
    'Components/Pages/Projects.razor' = '@rendermode InteractiveServer'
    'Components/Pages/TestLab.razor' = '@rendermode InteractiveServer'
}

$utf8 = [System.Text.Encoding]::UTF8
foreach ($entry in $expected.GetEnumerator()) {
    $relative = [string]$entry.Key
    $path = Join-Path $appRoot ($relative.Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar))
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Fail "Required component is missing: $relative" }
    $text = [System.IO.File]::ReadAllText($path, $utf8)
    $first = @($text -split '\r?\n' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })[0].Trim()
    if ($first -cne [string]$entry.Value) {
        Fail "Render mode changed in $relative. Expected first directive '$($entry.Value)' but found '$first'."
    }
}

$inheritedThemeChildren = @(
    'Components/Layout/ThemeSwitcher.razor',
    'Components/Layout/ThemeSwitcherContainer.razor',
    'Components/Layout/ThemeSwitcherItem.razor'
)
foreach ($relative in $inheritedThemeChildren) {
    $path = Join-Path $appRoot ($relative.Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar))
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Fail "Required component is missing: $relative" }
    $text = [System.IO.File]::ReadAllText($path, $utf8)
    if ($text.IndexOf('@rendermode', [StringComparison]::Ordinal) -ge 0) {
        Fail "$relative must inherit MenuIsland's InteractiveServer circuit instead of creating a competing nested circuit."
    }
}

$appPath = Join-Path $appRoot 'Components\App.razor'
$programPath = Join-Path $appRoot 'Program.cs'
$importsPath = Join-Path $appRoot 'Components\_Imports.razor'
foreach ($path in @($appPath, $programPath, $importsPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Fail "Required source file is missing: $path" }
}
$app = [System.IO.File]::ReadAllText($appPath, $utf8)
$program = [System.IO.File]::ReadAllText($programPath, $utf8)
$imports = [System.IO.File]::ReadAllText($importsPath, $utf8)

foreach ($required in @(
    '<HeadOutlet />',
    '<ToastWrapper Name="ComponentSafetyToasts" />',
    '<Routes></Routes>',
    '<InteractiveStartupMarker />',
    '<body data-enhance-nav="false">',
    'ssr: { disableDomPreservation: true }',
    '.then(() => window.localGptReady.markInteractive())'
)) {
    if (-not $app.Contains($required)) { Fail "App.razor is missing the reviewed render contract: $required" }
}
if ($app -match '<Routes\s+@rendermode' -or $app -match '<HeadOutlet\s+@rendermode') {
    Fail 'App.razor must not replace the reviewed page/island render modes with a single root render boundary.'
}
if (-not $program.Contains('AddInteractiveServerComponents()')) { Fail 'Program.cs no longer registers interactive server components.' }
if (-not $program.Contains('AddInteractiveServerRenderMode()')) { Fail 'Program.cs no longer maps the InteractiveServer render mode.' }
if (-not $program.Contains('AddSingleton<CircuitHandler, LocalGptCircuitDiagnosticsHandler>()')) { Fail 'Program.cs no longer registers circuit diagnostics.' }
if (-not $imports.Contains('@using static Microsoft.AspNetCore.Components.Web.RenderMode')) { Fail 'Components/_Imports.razor no longer imports RenderMode.' }

Write-Host "InteractiveServer render-mode validation passed for $($expected.Count) explicit islands/pages and $($inheritedThemeChildren.Count) inherited theme children."
