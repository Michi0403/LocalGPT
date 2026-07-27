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

# The route tree, toast host, and error boundary must share one interactive circuit.
Require-Text 'LocalGPTWebviewWrapper\LocalGPT\Components\App.razor' @(
    '<Routes\s+@rendermode="@\(new InteractiveServerRenderMode\(prerender:\s*false\)\)"',
    'Blazor\.start\(\)'
) 'Single non-prerendered route tree'
Require-Text 'LocalGPTWebviewWrapper\LocalGPT\Components\Layout\MainLayout.razor' @(
    '<ToastWrapper\s+Name="ComponentSafetyToasts"',
    '<SafeErrorBoundary',
    '<InteractiveStartupMarker\s*/>',
    'ILogger<MainLayout>',
    'INotificationService'
) 'Layout diagnostics boundary'
Require-Text 'LocalGPTWebviewWrapper\LocalGPT\Components\Routes.razor' @(
    '<SafeErrorBoundary\s+@key="NavigationManager\.Uri"',
    'RecordNavigation\(',
    'LocationChanged\s*\+=\s*HandleLocationChanged'
) 'Route replacement and navigation diagnostics'

# Chat is the highest-risk interactive page. Its attach path must not call JS itself,
# and its operational paths keep structured logging and user notification. Dispose methods are exempt.
Require-Text 'LocalGPTWebviewWrapper\LocalGPT\Components\Pages\Chat.razor' @(
    'ILogger<Chat>',
    'INotificationService',
    'interactiveAttached\s*=\s*true',
    'StartInitialModelRefresh\(\)',
    'StartAutoSaveLoop\(\)',
    'catch\s*\(Exception\s+ex\)',
    'Logger\.Log',
    'Notifier\.Show'
) 'Chat operational diagnostics'
$chatPath = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Components\Pages\Chat.razor'
if (Test-Path -LiteralPath $chatPath) {
    $chat = Get-Content -LiteralPath $chatPath -Raw
    if ($chat -match 'JS\.InvokeVoidAsync\("localGptReady\.markInteractive"') {
        $failures.Add('Chat must not own the global interactive-ready JS marker; the layout marker owns it.')
    }
    if ($chat -match 'ConfigureAwait\(false\)') {
        $failures.Add('Chat contains ConfigureAwait(false), which can leave the Blazor renderer synchronization context.')
    }
}

# Controllers are covered centrally, while maintained service/controller files remain
# protected by Assert-LoggingIntegrity.ps1. This avoids injecting circuit UI services into
# singleton/boot services, which would break startup.
Require-Text 'LocalGPTWebviewWrapper\LocalGPT\Program.cs' @(
    'AddScoped<ControllerRequestLoggingFilter>',
    'Filters\.AddService<ControllerRequestLoggingFilter>',
    'AddScoped<INotificationService',
    'AddHostedService<DatabaseInitializationHostedService>'
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

Write-Host 'Operational diagnostics validation passed for the component circuit, Chat attach path, controllers, and automatic migration startup.' -ForegroundColor Green
