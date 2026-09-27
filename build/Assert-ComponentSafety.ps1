param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$componentRoot = Join-Path $RepositoryRoot 'src/LocalGPT/Components'
$errors = [System.Collections.Generic.List[string]]::new()
$topDirectivePattern = '^\s*@(page|using|inject|inherits|implements|namespace|attribute|typeparam|layout|rendermode|preservewhitespace)(\s|\(|$)'

Get-ChildItem -Path $componentRoot -Recurse -Filter '*.razor' -File |
    Where-Object Name -ne '_Imports.razor' |
    ForEach-Object {
        $lines = @(Get-Content -LiteralPath $_.FullName)
        $componentName = $_.BaseName
        $required = @(
            "@inject ILogger<$componentName> Logger",
            '@inject INotificationService Notifier',
            '@inject IComponentActivityService ComponentActivity'
        )

        $boundaryLine = $lines.Count
        $inRazorComment = $false
        for ($index = 0; $index -lt $lines.Count; $index++) {
            $trimmed = $lines[$index].Trim()
            if ($inRazorComment) {
                if (($trimmed.IndexOf('*@', [System.StringComparison]::Ordinal) -ge 0)) {
                    $inRazorComment = $false
                }
                continue
            }
            if ([string]::IsNullOrWhiteSpace($trimmed)) {
                continue
            }
            if ($trimmed.StartsWith('@*', [System.StringComparison]::Ordinal)) {
                if (-not ($trimmed.IndexOf('*@', [System.StringComparison]::Ordinal) -ge 0)) {
                    $inRazorComment = $true
                }
                continue
            }
            if ($trimmed -match $topDirectivePattern) {
                continue
            }
            $boundaryLine = $index
            break
        }

        foreach ($directive in $required) {
            $matches = @()
            for ($index = 0; $index -lt $lines.Count; $index++) {
                if ($lines[$index].Trim().Equals($directive, [System.StringComparison]::Ordinal)) {
                    $matches += $index
                }
            }
            if ($matches.Count -eq 0) {
                $relative = $_.FullName.Substring($RepositoryRoot.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace('\','/')
                $errors.Add("${relative}(1,1): error RAZORLOG0001: missing top-level component safety directive '$directive'.`n  Architectural choices: restore the required top-level safety injection in the directive block; keep component-local logging/notification/activity ownership and do not replace it with a global-only shortcut.")
            }
            elseif ($matches.Count -gt 1) {
                $relative = $_.FullName.Substring($RepositoryRoot.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace('\','/')
                $lineNumber = $matches[0] + 1
                $errors.Add("${relative}(${lineNumber},1): error RAZORLOG0001: duplicate component safety directive '$directive'.`n  Architectural choices: keep exactly one top-level injection and remove only the duplicate declaration; do not remove the underlying safety service.")
            }
            elseif ($matches[0] -ge $boundaryLine) {
                $relative = $_.FullName.Substring($RepositoryRoot.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace('\','/')
                $lineNumber = $matches[0] + 1
                $errors.Add("${relative}(${lineNumber},1): error RAZORLOG0001: component safety directive '$directive' must stay in the top directive/using section.`n  Architectural choices: move the existing directive into the top directive block without changing the component lifecycle or replacing the service.")
            }
        }

        $content = [string]::Join([Environment]::NewLine, $lines)
        if ($content -match '\[Inject\][\s\S]{0,160}(ILogger<|INotificationService|IComponentActivityService)') {
            $errors.Add("$($_.FullName): component safety services must use top-level @inject directives, not property injection.")
        }
    }

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

$routes = Join-Path $componentRoot 'Routes.razor'
$routesContent = Get-Content -LiteralPath $routes -Raw
if (-not ($routesContent.IndexOf('<SafeErrorBoundary', [System.StringComparison]::Ordinal) -ge 0)) {
    Write-Error 'Routes.razor must retain the global SafeErrorBoundary.'
    exit 1
}

$app = Join-Path $componentRoot 'App.razor'
$appContent = Get-Content -LiteralPath $app -Raw
if (-not ($appContent.IndexOf('<ToastWrapper Name="ComponentSafetyToasts"', [System.StringComparison]::Ordinal) -ge 0)) {
    Write-Error 'App.razor must retain the shared ComponentSafetyToasts provider.'
    exit 1
}

$serviceRegistration = Join-Path $RepositoryRoot 'src/LocalGPT/Program.ServiceRegistration.cs'
if (-not (Test-Path -LiteralPath $serviceRegistration)) {
    Write-Error 'src/LocalGPT/Program.ServiceRegistration.cs(1,1): error RAZORLOG0010: component activity service registration source is missing. Architectural choices: restore the existing service-registration partial; do not move the bounded activity service into ad-hoc component construction.'
    exit 1
}
$serviceRegistrationContent = Get-Content -LiteralPath $serviceRegistration -Raw
foreach ($requiredFragment in @(
    'AddSingleton<ComponentActivityService>()',
    'AddSingleton<IComponentActivityService>(services =>',
    'GetRequiredService<ComponentActivityService>()',
    'AddSingleton<IServiceActivityService>(services =>')) {
    if ($serviceRegistrationContent.IndexOf($requiredFragment, [System.StringComparison]::Ordinal) -lt 0) {
        Write-Error "src/LocalGPT/Program.ServiceRegistration.cs(1,1): error RAZORLOG0010: bounded component activity registration is incomplete; missing '$requiredFragment'. Architectural choices: keep one singleton ComponentActivityService and map both activity interfaces back to that same instance. Do not weaken the component safety guard or create per-component activity stores."
        exit 1
    }
}

$bootstrap = Join-Path $RepositoryRoot 'src/LocalGPT/Services/AiContextBootstrapService.cs'
$bootstrapContent = Get-Content -LiteralPath $bootstrap -Raw
if (-not ($bootstrapContent.IndexOf('componentActivity.BuildBriefing', [System.StringComparison]::Ordinal) -ge 0)) {
    Write-Error 'AiContextBootstrapService must retain bounded UI activity awareness.'
    exit 1
}

$diagnosticController = Join-Path $RepositoryRoot 'src/LocalGPT/Controller/LocalGptDiagnosticController.cs'
$diagnosticContent = Get-Content -LiteralPath $diagnosticController -Raw
foreach ($requiredFragment in @(
    '[HttpGet("/__diag/component-activity")]',
    'IComponentActivityService componentActivity',
    'componentActivity.GetRecent(',
    'componentActivity.BuildBriefing(')) {
    if (-not ($diagnosticContent.IndexOf($requiredFragment, [System.StringComparison]::Ordinal) -ge 0)) {
        Write-Error "LocalGPT component-activity diagnostics must retain '$requiredFragment'."
        exit 1
    }
}


$notificationService = Join-Path $RepositoryRoot 'src/LocalGPT/Services/NotificationService.cs'
$notificationContent = Get-Content -LiteralPath $notificationService -Raw
foreach ($requiredFragment in @(
    'IComponentActivityService componentActivity',
    'componentActivity.RecordInformation(',
    'componentActivity.RecordFailure(')) {
    if (-not ($notificationContent.IndexOf($requiredFragment, [System.StringComparison]::Ordinal) -ge 0)) {
        Write-Error "NotificationService must retain bounded activity integration: $requiredFragment"
        exit 1
    }
}

Get-ChildItem -Path $componentRoot -Recurse -Filter '*.razor' -File |
    Where-Object Name -ne '_Imports.razor' |
    ForEach-Object {
        $content = Get-Content -LiteralPath $_.FullName -Raw
        if (($content.IndexOf('RunUiActionAsync(', [System.StringComparison]::Ordinal) -ge 0)) {
            if (-not ($content.IndexOf('ComponentActivity.Record', [System.StringComparison]::Ordinal) -ge 0)) {
                Write-Error "$($_.FullName): reusable UI-operation wrappers must report bounded component activity."
                exit 1
            }
            if (-not ($content.IndexOf('Notifier.Show', [System.StringComparison]::Ordinal) -ge 0)) {
                Write-Error "$($_.FullName): reusable UI-operation wrappers must retain human notification."
                exit 1
            }
            if (-not ($content.IndexOf('Logger.Log', [System.StringComparison]::Ordinal) -ge 0)) {
                Write-Error "$($_.FullName): reusable UI-operation wrappers must retain technical logging."
                exit 1
            }
        }
    }

Write-Host 'Component safety directives, notification boundary, and bounded UI awareness verified.'
