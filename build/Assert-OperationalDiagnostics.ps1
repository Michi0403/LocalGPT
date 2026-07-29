[CmdletBinding()]
param([string]$RepositoryRoot)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Split-Path -Parent $PSScriptRoot
}

$root = (Resolve-Path -LiteralPath $RepositoryRoot -ErrorAction Stop).ProviderPath
$failures = New-Object 'System.Collections.Generic.List[string]'

function Require-Text {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string[]]$Patterns,
        [Parameter(Mandatory = $true)][string]$Purpose
    )

    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Required diagnostics source is missing: $RelativePath")
        return
    }

    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($content -notmatch $pattern) {
            $failures.Add("$Purpose is missing in '$RelativePath' (pattern: $pattern).")
        }
    }
}

# Every Razor component inherits these circuit-scoped diagnostics dependencies from _Imports.
Require-Text 'LocalGPTWebviewWrapper\LocalGPT\Components\_Imports.razor' @(
    '@inject\s+ILoggerFactory\s+OperationalLoggerFactory',
    '@inject\s+INotificationService\s+OperationalNotifier',
    '@inject\s+IComponentActivityService\s+OperationalActivity'
) 'Global component logger/notifier availability'

# The reviewed architecture uses page/island InteractiveServer boundaries. The app shell
# owns the shared toast host and startup marker; MainLayout owns only the routed body boundary.
Require-Text 'LocalGPTWebviewWrapper\LocalGPT\Components\App.razor' @(
    '<ToastWrapper\s+Name="ComponentSafetyToasts"\s*/>',
    '<Routes>\s*</Routes>',
    '<InteractiveStartupMarker\s*/>',
    'Blazor\.start\(\{',
    'disableDomPreservation:\s*true',
    '<body\s+data-enhance-nav="false">'
) 'Application shell diagnostics hosts'
Require-Text 'LocalGPTWebviewWrapper\LocalGPT\Components\Layout\MainLayout.razor' @(
    '<SafeErrorBoundary\s+@key="NavigationManager\.Uri"',
    'ILogger<MainLayout>',
    'INotificationService'
) 'Layout diagnostics boundary'
Require-Text 'LocalGPTWebviewWrapper\LocalGPT\Components\Routes.razor' @(
    '<SafeErrorBoundary\s+@key="NavigationManager\.Uri"',
    'RecordNavigation\(',
    'LocationChanged\s*\+=\s*HandleLocationChanged'
) 'Route replacement and navigation diagnostics'

# Chat is the highest-risk interactive page. It retains its reviewed InteractiveServer
# boundary and operational diagnostics. Async continuation policy is validated separately.
# Dispose methods are exempt.
Require-Text 'LocalGPTWebviewWrapper\LocalGPT\Components\Pages\Chat.razor' @(
    '@rendermode\s+InteractiveServer',
    'ILogger<Chat>',
    'INotificationService',
    'interactiveAttached\s*=\s*true',
    'StartInitialModelRefresh\(\)',
    'StartAutoSaveLoop\(\)',
    'catch\s*\(Exception\s+ex\)',
    'Logger\.Log',
    'Notifier\.Show'
) 'Chat operational diagnostics'
$appPath = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Components\App.razor'
$layoutPath = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Components\Layout\MainLayout.razor'
if (Test-Path -LiteralPath $appPath) {
    $app = Get-Content -LiteralPath $appPath -Raw
    if ($app -match '<(?:Routes|HeadOutlet)\s+@rendermode') {
        $failures.Add('App must not replace page/island render modes with a root Routes or HeadOutlet render boundary.')
    }
}
if (Test-Path -LiteralPath $layoutPath) {
    $layout = Get-Content -LiteralPath $layoutPath -Raw
    if ($layout -match '<ToastWrapper\s+Name="ComponentSafetyToasts"' -or $layout -match '<InteractiveStartupMarker\s*/>') {
        $failures.Add('MainLayout must not duplicate the app-level toast host or interactive startup marker.')
    }
}

$chatPath = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Components\Pages\Chat.razor'
if (Test-Path -LiteralPath $chatPath) {
    $chat = Get-Content -LiteralPath $chatPath -Raw
    if ($chat -match 'JS\.InvokeVoidAsync\("localGptReady\.markInteractive"') {
        $failures.Add('Chat must not own the global interactive-ready JS marker; the layout marker owns it.')
    }
}

# Complex renderer-affine pages use their reviewed InteractiveServer boundaries and bounded initialization.
Require-Text 'LocalGPTWebviewWrapper\LocalGPT\Components\Pages\OneWireSecurity.razor' @(
    '@rendermode\s+InteractiveServer',
    'CancelAfter\(TimeSpan\.FromSeconds\(8\)\)',
    'InitialSecurityRefresh',
    'InitializeAfterRenderAsync',
    'ConfigureAwait\(true\)',
    'OperationCanceledException',
    'JSDisconnectedException'
) '1-Wire renderer and timeout diagnostics'
Require-Text 'LocalGPTWebviewWrapper\LocalGPT\Diagnostics\LocalGptCircuitDiagnosticsHandler.cs' @(
    'CircuitHandler',
    'OnCircuitOpenedAsync',
    'OnConnectionDownAsync',
    'OnCircuitClosedAsync'
) 'Blazor circuit diagnostics'

# Controllers are covered centrally, while maintained service/controller files remain
# protected by Assert-LoggingIntegrity.ps1. This avoids injecting circuit UI services into
# singleton/boot services, which would break startup.
Require-Text 'LocalGPTWebviewWrapper\LocalGPT\Program.cs' @(
    'AddScoped<ControllerRequestLoggingFilter>',
    'Filters\.AddService<ControllerRequestLoggingFilter>',
    'AddScoped<INotificationService',
    'AddHostedService<DatabaseInitializationHostedService>',
    'AddSingleton<CircuitHandler, LocalGptCircuitDiagnosticsHandler>'
) 'Controller, notifier, and startup diagnostics registration'
Require-Text 'LocalGPTWebviewWrapper\LocalGPT\Diagnostics\ControllerRequestLoggingFilter.cs' @(
    'IAsyncActionFilter',
    'ILogger<ControllerRequestLoggingFilter>',
    'IComponentActivityService',
    'LogInformation',
    'LogError',
    'RecordFailure'
) 'Global controller action diagnostics'
Require-Text 'LocalGPTWebviewWrapper\LocalGPT\Services\Persistence\DatabaseInitializationService.cs' @(
    'MigrateAsync\(cancellationToken\)',
    'ILogger<DatabaseInitializationService>',
    'catch\s*\(Exception'
) 'Automatic migration diagnostics'

if ($failures.Count -gt 0) {
    Write-Host 'Operational diagnostics validation failed:' -ForegroundColor Red
    foreach ($failure in $failures) { Write-Host "  - $failure" -ForegroundColor Red }
    throw "Operational diagnostics validation failed with $($failures.Count) problem(s)."
}

Write-Host 'Operational diagnostics validation passed for the reviewed InteractiveServer islands, Chat diagnostics, controllers, and automatic migration startup.' -ForegroundColor Green
