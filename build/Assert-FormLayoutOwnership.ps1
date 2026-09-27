Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw $Message }

$root = Split-Path -Parent $PSScriptRoot
$componentRoot = Join-Path $root 'src/LocalGPT/Components'
$navPath = Join-Path $componentRoot 'Layout/NavMenu.razor'
$surfacePath = Join-Path $componentRoot 'Shared/LocalGptFormLayoutSurface.razor'

if (-not (Test-Path -LiteralPath $navPath -PathType Leaf)) { Fail 'NavMenu.razor is missing.' }
if (-not (Test-Path -LiteralPath $surfacePath -PathType Leaf)) { Fail 'LocalGptFormLayoutSurface.razor is missing.' }

$surfaceText = [System.IO.File]::ReadAllText($surfacePath, [System.Text.Encoding]::UTF8)
if ($surfaceText -notmatch '<DxFormLayout\b' -or $surfaceText -notmatch '<DxFormLayoutItem\b') {
    Fail 'LocalGptFormLayoutSurface must remain a DevExpress DxFormLayout/DxFormLayoutItem envelope.'
}

$navText = [System.IO.File]::ReadAllText($navPath, [System.Text.Encoding]::UTF8)
$routes = [regex]::Matches($navText, 'NavigateUrl="(?<route>/[^"?]*)"') |
    ForEach-Object { $_.Groups['route'].Value.ToLowerInvariant() } |
    Sort-Object -Unique

$pageByRoute = @{}
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $componentRoot 'Pages') -Recurse -File -Filter '*.razor') {
    $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    foreach ($match in [regex]::Matches($text, '(?m)^@page\s+"(?<route>/[^"?]*)"')) {
        $pageByRoute[$match.Groups['route'].Value.ToLowerInvariant()] = [pscustomobject]@{ File = $file; Text = $text }
    }
}

$failures = New-Object System.Collections.Generic.List[string]
foreach ($route in $routes) {
    if (-not $pageByRoute.ContainsKey($route)) {
        $failures.Add("NavMenu route '$route' does not resolve to a Razor @page component.")
        continue
    }
    $record = $pageByRoute[$route]
    $relative = $record.File.FullName.Substring($root.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace([char]'\', [char]'/')
    if ($record.Text -notmatch '@attribute\s+\[LocalGptUiLayoutSurface\("page"') {
        $failures.Add("$relative|missing LocalGptUiLayoutSurface page declaration")
    }
    if ($record.Text -notmatch '<LocalGptFormLayoutSurface\b[^>]*SectionName="page"') {
        $failures.Add("$relative|missing LocalGptFormLayoutSurface page envelope")
    }
}

if ($failures.Count -gt 0) {
    Write-Host 'Form-layout ownership validation failed:'
    foreach ($failure in $failures | Sort-Object -Unique) { Write-Host "  - $failure" }
    Fail "Form-layout ownership validation failed with $($failures.Count) problem(s). NavMenu pages must keep the persisted DevExpress form-layout envelope."
}

Write-Host "Form-layout ownership validation passed for $($routes.Count) NavMenu route(s); the shared wrapper remains DevExpress FormLayout-backed."
