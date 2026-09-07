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


function Require-TextAcrossFiles {
    param(
        [Parameter(Mandatory = $true)][string[]]$RelativePaths,
        [Parameter(Mandatory = $true)][string[]]$Patterns,
        [Parameter(Mandatory = $true)][string]$Purpose
    )

    $content = ''
    foreach ($relativePath in $RelativePaths) {
        $path = Join-Path $root $relativePath
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            $failures.Add("Required diagnostics source is missing: $relativePath")
            continue
        }
        $content += [Environment]::NewLine + (Get-Content -LiteralPath $path -Raw)
    }
    foreach ($pattern in $Patterns) {
        if ($content -notmatch $pattern) {
            $failures.Add("$Purpose is missing across '$($RelativePaths -join ', ')' (pattern: $pattern).")
        }
    }
}

# Every Razor component inherits these circuit-scoped diagnostics dependencies from _Imports.
Require-Text 'src/LocalGPT\Components/_Imports.razor' @(
    '@inject\s+ILoggerFactory\s+OperationalLoggerFactory',
    '@inject\s+INotificationService\s+OperationalNotifier',
    '@inject\s+IComponentActivityService\s+OperationalActivity'
) 'Global component logger/notifier availability'

# The reviewed architecture uses page/island InteractiveServer boundaries. The app shell
# owns the shared toast host and startup marker; MainLayout owns only the routed body boundary.
Require-Text 'src/LocalGPT/Components/App.razor' @(
    '<ToastWrapper\s+Name="ComponentSafetyToasts"\s*/>',
    '<Routes>\s*</Routes>',
    '<InteractiveStartupMarker\s*/>',
    'Blazor\.start\(\{',
    'disableDomPreservation:\s*false',
    '<body\s+data-enhance-nav="false">'
) 'Application shell diagnostics hosts'
Require-Text 'src/LocalGPT/Components/Layout/MainLayout.razor' @(
    '<SafeErrorBoundary\s+@key="NavigationManager\.Uri"',
    'ILogger<MainLayout>',
    'INotificationService'
) 'Layout diagnostics boundary'
Require-Text 'src/LocalGPT\Components\Routes.razor' @(
    '<SafeErrorBoundary\s+@key="NavigationManager\.Uri"',
    'RecordNavigation\(',
    'LocationChanged\s*\+=\s*HandleLocationChanged'
) 'Route replacement and navigation diagnostics'

# Chat is the highest-risk interactive page. It retains its reviewed InteractiveServer
# boundary and operational diagnostics. Async continuation policy is validated separately.
# Dispose methods are exempt.
Require-Text 'src/LocalGPT/Components/Pages/Chat.razor' @(
    '@rendermode\s+InteractiveServer',
    'ILogger<Chat>',
    'INotificationService'
) 'Chat render boundary and injected diagnostics'
$chatDiagnosticsFiles = @(
    'src/LocalGPT/Components/Pages/Chat.razor',
    'src/LocalGPT\Components\Pages\Chat.Lifecycle.razor.cs',
    'src/LocalGPT\Components\Pages\Chat.ProviderRuntime.razor.cs',
    'src/LocalGPT\Components\Pages\Chat.PersistenceAndMemory.razor.cs',
    'src/LocalGPT\Components\Pages\Chat.LiveCouncil.razor.cs',
    'src/LocalGPT\Components\Pages\Chat.PresetsAndCouncilConfiguration.razor.cs'
)
Require-TextAcrossFiles $chatDiagnosticsFiles @(
    'interactiveAttached\s*=\s*true',
    'StartInitialModelRefresh\(\)',
    'StartAutoSaveLoop\(\)',
    'catch\s*\(Exception\s+ex\)',
    'Logger\.Log',
    'Notifier\.Show'
) 'Chat operational diagnostics'
$appPath = Join-Path $root 'src/LocalGPT/Components/App.razor'
$layoutPath = Join-Path $root 'src/LocalGPT/Components/Layout/MainLayout.razor'
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

$chatPath = Join-Path $root 'src/LocalGPT/Components/Pages/Chat.razor'
if (Test-Path -LiteralPath $chatPath) {
    $chat = Get-Content -LiteralPath $chatPath -Raw
    if ($chat -match 'JS\.InvokeVoidAsync\("localGptReady\.markInteractive"') {
        $failures.Add('Chat must not own the global interactive-ready JS marker; the layout marker owns it.')
    }
}

# Complex renderer-affine pages use their reviewed InteractiveServer boundaries and bounded initialization.
Require-Text 'src/LocalGPT\Components\Pages\OneWireSecurity.razor' @(
    '@rendermode\s+InteractiveServer',
    'CancelAfter\(TimeSpan\.FromSeconds\(8\)\)',
    'InitialSecurityRefresh',
    'InitializeAfterRenderAsync',
    'ConfigureAwait\(true\)',
    'OperationCanceledException',
    'JSDisconnectedException'
) '1-Wire renderer and timeout diagnostics'
Require-Text 'src/LocalGPT\Diagnostics\LocalGptCircuitDiagnosticsHandler.cs' @(
    'CircuitHandler',
    'OnCircuitOpenedAsync',
    'OnConnectionDownAsync',
    'OnCircuitClosedAsync'
) 'Blazor circuit diagnostics'


# Boot-critical database services must not depend on IServiceActivityService. That service is
# implemented by ComponentActivityService, which depends on the database-backed runtime-policy
# graph; routing it back into database initialization creates a hidden singleton cycle that can
# deadlock host startup before Kestrel listens.
foreach ($relativePath in @(
    'src/LocalGPT/Services/Persistence/DatabaseInitializationService.cs',
    'src/LocalGPT/Services/Persistence/DatabaseMigrationCompatibilityService.cs'
)) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $failures.Add("Required boot-critical database source is missing: $relativePath")
        continue
    }
    $content = Get-Content -LiteralPath $path -Raw
    if ($content -match '\bIServiceActivityService\b') {
        $failures.Add("Boot-critical database source '$relativePath' must not depend on IServiceActivityService; this reintroduces the database/runtime-policy startup cycle.")
    }
}

Require-Text 'src/LocalGPT/Services/Persistence/LocalGptRuntimePolicyDataService.cs' @(
    'CreateSeedDefinition\(seedData\.GetSeed\(\)\)',
    'Initialized LocalGPT runtime policy from the built-in seed',
    'private LocalGptRuntimePolicyDefinition CreateSeedDefinition',
    'private LocalGptRuntimePolicyState BuildState'
) 'Non-blocking runtime-policy bootstrap'
Require-Text 'src/LocalGPT/Services/Persistence/DatabaseInitializationService.cs' @(
    'ILocalGptRuntimePolicyDataService runtimePolicy',
    'await initializer\.InitializeAsync\(stoppingToken\)\.ConfigureAwait\(false\);',
    'runtimePolicy\.Reload\(\);',
    'the built-in seed remains active'
) 'Post-database persisted runtime-policy reload'

$runtimePolicyPath = Join-Path $root 'src/LocalGPT/Services/Persistence/LocalGptRuntimePolicyDataService.cs'
if (Test-Path -LiteralPath $runtimePolicyPath -PathType Leaf) {
    $runtimePolicyText = Get-Content -LiteralPath $runtimePolicyPath -Raw
    $constructorStart = $runtimePolicyText.IndexOf('public LocalGptRuntimePolicyDataService(', [StringComparison]::Ordinal)
    $firstMethod = $runtimePolicyText.IndexOf('public string GetString', [StringComparison]::Ordinal)
    if ($constructorStart -lt 0 -or $firstMethod -le $constructorStart) {
        $failures.Add('Could not identify the LocalGptRuntimePolicyDataService constructor boundary for startup-cycle validation.')
    }
    else {
        $constructorBlock = $runtimePolicyText.Substring($constructorStart, $firstMethod - $constructorStart)
        if ($constructorBlock -match 'Reload\(\)\s*;') {
            $failures.Add('LocalGptRuntimePolicyDataService constructor must not synchronously reload database-backed policy during host construction.')
        }
    }
}

# Controllers are covered centrally, while maintained service/controller files remain
# kept separate from circuit UI services. This avoids injecting circuit UI services into
# singleton/boot services, which would break startup.
$programDiagnosticsFiles = @(
    'src/LocalGPT\Program.cs',
    'src/LocalGPT\Program.Hosting.cs',
    'src/LocalGPT\Program.Middleware.cs',
    'src/LocalGPT\Program.ServiceRegistration.cs',
    'src/LocalGPT\Program.WebFeatures.cs'
)
Require-TextAcrossFiles $programDiagnosticsFiles @(
    'AddScoped<ControllerRequestLoggingFilter>',
    'Filters\.AddService<ControllerRequestLoggingFilter>',
    'AddScoped<INotificationService',
    'AddHostedService<DatabaseInitializationHostedService>',
    'AddHostedService<RemoteControlPollingHostedService>',
    'AddHostedService<LocalGPT\.Services\.Council\.RuntimeCapabilityDirectoryHostedService>',
    'AddHostedService<DxAiFunctionCatalogHostedService>',
    'AddHostedService<OneWireTcpHostedService>',
    'AddHostedService<OneWireDiscoveryHostedService>',
    'AddHostedService<OneWireCouncilApprovalProcessorHostedService>',
    'AddHostedService<OneWireWorkProcessorHostedService>',
    'AddSingleton<CircuitHandler, LocalGptCircuitDiagnosticsHandler>'
) 'Controller, notifier, and startup diagnostics registration'
Require-Text 'src/LocalGPT\Diagnostics\ControllerRequestLoggingFilter.cs' @(
    'IAsyncActionFilter',
    'ILogger<ControllerRequestLoggingFilter>',
    'IComponentActivityService',
    'LogInformation',
    'LogError',
    'RecordFailure'
) 'Global controller action diagnostics'
Require-Text 'src/LocalGPT\Services\Persistence\DatabaseInitializationService.cs' @(
    'MigrateAsync\(cancellationToken\)',
    'ILogger<DatabaseInitializationService>',
    'catch\s*\(Exception'
) 'Automatic migration diagnostics'

if ($failures.Count -gt 0) {
    Write-Output 'Operational diagnostics validation failed:'
    foreach ($failure in $failures) { Write-Output "  - $failure" }
    throw "Operational diagnostics validation failed with $($failures.Count) problem(s)."
}

Write-Output 'Operational diagnostics validation passed for the reviewed InteractiveServer islands, Chat diagnostics, controllers, hosted-service ownership, and non-blocking database/runtime-policy startup.'
