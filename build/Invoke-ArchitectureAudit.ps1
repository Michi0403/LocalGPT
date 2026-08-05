param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('static', 'methods', 'runtime', 'all')]
    [string]$Mode
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "Architecture policy validation failed: $Message" }
function Normalize-Signature([string]$Value) { return ([regex]::Replace($Value, '\s+', ' ')).Trim() }

$root = Split-Path -Parent $PSScriptRoot
$isLocalGpt = Test-Path -LiteralPath (Join-Path $root 'src\LocalGPT') -PathType Container
$product = if ($isLocalGpt) { 'localgpt' } else { 'publisherstudio' }
$sourceRoot = if ($isLocalGpt) { Join-Path $root 'src\LocalGPT' } else { Join-Path $root 'src\PublisherStudio.Web' }
$pythonScript = Join-Path $PSScriptRoot 'audit_application_architecture.py'

function Invoke-PythonAudit {
    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($python) {
        $auditOutput = @(& $python.Source $pythonScript --root $root --product $product --mode $Mode 2>&1)
        $auditExitCode = [int]$LASTEXITCODE
        foreach ($line in $auditOutput) { Write-Host ([string]$line) }
        return $auditExitCode
    }

    $launcher = Get-Command py -ErrorAction SilentlyContinue
    if ($launcher) {
        $auditOutput = @(& $launcher.Source -3 $pythonScript --root $root --product $product --mode $Mode 2>&1)
        $auditExitCode = [int]$LASTEXITCODE
        foreach ($line in $auditOutput) { Write-Host ([string]$line) }
        return $auditExitCode
    }

    return $null
}

function Remove-NonCode([string]$Text) {
    $withoutRawStrings = [regex]::Replace($Text, '(?s)\$*@?"{3,}.*?"{3,}', { param($match) ' ' * $match.Length })
    $withoutBlockComments = [regex]::Replace($withoutRawStrings, '(?s)/\*.*?\*/', { param($match) ' ' * $match.Length })
    return [regex]::Replace($withoutBlockComments, '(?m)//.*$', '')
}

function Invoke-StaticFallback {
    $failures = [Collections.Generic.List[string]]::new()
    $allowedFiles = @('Program.cs')
    if (-not $isLocalGpt) {
        $allowedFiles += @('PublisherStudioServiceCollectionExtensions.cs', 'StreamingServiceCollectionExtensions.cs')
    }

    $declarationPattern = '(?m)^\s*(?:public|private|protected|internal)\s+(?:(?:sealed|partial|new|unsafe|readonly|abstract)\s+)*static\s+[^\r\n]+'
    foreach ($file in Get-ChildItem -LiteralPath $sourceRoot -Recurse -File | Where-Object {
        $_.Extension -in @('.cs', '.razor') -and
        $_.FullName -notmatch '[\\/](?:bin|obj|Migrations)[\\/]' -and
        $_.Name -notlike '*.Designer.cs'
    }) {
        $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace('\', '/')
        $text = [IO.File]::ReadAllText($file.FullName, [Text.Encoding]::UTF8)
        if ($file.Extension -eq '.razor') {
            foreach ($line in [regex]::Matches($text, '(?m)^.*@using\s+static.*$')) {
                if ($line.Value -notmatch 'Microsoft\.AspNetCore\.Components\.Web\.RenderMode') {
                    $failures.Add("$relative contains an unsupported static Razor import: $($line.Value.Trim())")
                }
            }
            continue
        }

        $code = Remove-NonCode $text
        if ($code -match '\[GeneratedRegex\s*\(') {
            $failures.Add("$relative contains GeneratedRegex application state.")
        }
        foreach ($match in [regex]::Matches($code, $declarationPattern)) {
            $declaration = Normalize-Signature $match.Value
            if ($file.Name -eq 'Program.cs') { continue }
            if ($file.Name -in @('PublisherStudioServiceCollectionExtensions.cs', 'StreamingServiceCollectionExtensions.cs')) {
                if ($declaration -match '\bstatic\s+class\s+\w+ServiceCollectionExtensions\b') { continue }
                if ($declaration -match 'this\s+IServiceCollection' -and $declaration -match 'ILogger') { continue }
            }
            $failures.Add("$relative|$declaration")
        }
    }

    if (-not $isLocalGpt) {
        foreach ($name in @('PublisherStudioServiceCollectionExtensions.cs', 'StreamingServiceCollectionExtensions.cs')) {
            $path = Join-Path $sourceRoot $name
            if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
                $failures.Add("Required DI extension boundary is missing: $name")
                continue
            }
            $text = [IO.File]::ReadAllText($path, [Text.Encoding]::UTF8)
            if ($text -notmatch 'this\s+IServiceCollection' -or $text -notmatch 'ILogger' -or $text -notmatch '\btry\b' -or $text -notmatch '\bcatch\b' -or $text -notmatch '\blogger\.Log\w*\s*\(') {
                $failures.Add("$name must remain a logged try/catch DI extension boundary.")
            }
        }
    }

    if ($failures.Count -gt 0) {
        $failures | Sort-Object -Unique | ForEach-Object { Write-Error $_ }
        Fail "$($failures.Count) application-static policy finding(s)."
    }
    Write-Host 'Application static policy validation passed without a legacy baseline.'
}

function Get-MethodRecords([string]$Text) {
    $scan = Remove-NonCode $Text
    $pattern = '(?ms)^[ \t]*(?<signature>(?:public|private|protected|internal)\s+(?:(?:static|async|virtual|override|sealed|partial|new|unsafe)\s+)*(?:[\w\.\?<>,\[\]]+\s+)+(?<name>[A-Za-z_]\w*)\s*\([^;{}]*?\)\s*(?:where[^\{=>\r\n]+)?\s*)(?<body>=>|\{)'
    $records = @()
    foreach ($match in [regex]::Matches($scan, $pattern)) {
        $bodyStart = $match.Groups['body'].Index
        if ($match.Groups['body'].Value -eq '=>') {
            $end = $scan.IndexOf(';', $bodyStart)
            if ($end -lt 0) { continue }
        }
        else {
            $depth = 0
            $end = -1
            for ($index = $bodyStart; $index -lt $scan.Length; $index++) {
                if ($scan[$index] -eq '{') { $depth++ }
                elseif ($scan[$index] -eq '}') {
                    $depth--
                    if ($depth -eq 0) { $end = $index; break }
                }
            }
            if ($end -lt 0) { continue }
        }
        $records += [pscustomobject]@{
            Signature = Normalize-Signature $match.Groups['signature'].Value
            Name = $match.Groups['name'].Value
            Body = $Text.Substring($bodyStart, $end - $bodyStart + 1)
        }
    }
    return $records
}

function Invoke-RuntimeFallback {
    $failures = [Collections.Generic.List[string]]::new()
    $allowedRegexFiles = if ($isLocalGpt) {
        @(
            'Services/Persistence/LocalGptRuntimePolicyDataService.cs',
            'Services/Persistence/CouncilTextPatternDataService.cs',
            'Services/Persistence/RegexPatternService.cs',
            'Services/ProjectMaintenanceService.cs'
        )
    }
    else {
        @(
            'Services/Configuration/PublisherRuntimePatternService.cs',
            'Services/Configuration/PanelStudioTextPatternDataService.cs'
        )
    }

    foreach ($file in Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.cs' | Where-Object {
        $_.FullName -notmatch '[\\/](?:bin|obj|Migrations)[\\/]' -and $_.Name -notlike '*.Designer.cs'
    }) {
        $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace('\', '/')
        $code = Remove-NonCode ([IO.File]::ReadAllText($file.FullName, [Text.Encoding]::UTF8))
        if ($code -match '\[GeneratedRegex\s*\(') { $failures.Add("$relative contains GeneratedRegex runtime state.") }
        if ($code -match '\bnew\s+Regex\s*\(' -and $allowedRegexFiles -notcontains $relative) {
            $failures.Add("$relative compiles a Regex outside an approved policy/data service.")
        }
    }

    $requiredFiles = if ($isLocalGpt) {
        @(
            'Services/Persistence/LocalGptRuntimePolicySeedDataService.cs',
            'Services/Persistence/LocalGptRuntimePolicyDataService.cs',
            'Services/Persistence/LocalGptRuntimePolicyStoreService.cs',
            'Controller/RuntimePolicyController.cs'
        )
    }
    else {
        @(
            'Services/Configuration/PublisherRuntimePolicyDataService.cs',
            'Services/Configuration/PublisherRuntimePatternService.cs',
            'Controllers/RuntimePolicyController.cs',
            'appsettings.json'
        )
    }
    foreach ($relative in $requiredFiles) {
        if (-not (Test-Path -LiteralPath (Join-Path $sourceRoot $relative) -PathType Leaf)) {
            $failures.Add("Required runtime-value boundary is missing: $relative")
        }
    }

    if ($failures.Count -gt 0) {
        $failures | Sort-Object -Unique | ForEach-Object { Write-Error $_ }
        Fail "$($failures.Count) runtime-value ownership finding(s)."
    }
    Write-Host 'Runtime-value ownership validation passed without a legacy runtime-value baseline.'
}

function Invoke-MethodFallback {
    $strictFiles = if ($isLocalGpt) {
        @(
            'Controller\RuntimePolicyController.cs',
            'Services\Persistence\LocalGptRuntimePolicyDataService.cs',
            'Services\Persistence\LocalGptRuntimePolicyStoreService.cs',
            'Services\Persistence\LocalGptRuntimePolicySeedDataService.cs',
            'Services\Persistence\LocalGptVocabularyService.cs',
            'Services\Persistence\OneWireReplayPolicyDataService.cs',
            'Services\OneWire\OneWireTransportSecurityPolicy.cs'
        )
    }
    else {
        @(
            'Controllers\RuntimePolicyController.cs',
            'Services\Configuration\PublisherRuntimePolicyDataService.cs',
            'Services\Configuration\PublisherRuntimePatternService.cs',
            'Services\Configuration\OrganicReplayPolicyDataService.cs',
            'Services\OrganicPlugins\OrganicTransportSecurityPolicy.cs',
            'Services\Streaming\Hotkeys\WindowsHotkeyNativeService.cs',
            'Services\Streaming\Capture\WindowsProcessLoopbackNativeService.cs'
        )
    }

    $failures = [Collections.Generic.List[string]]::new()
    foreach ($relative in $strictFiles) {
        $path = Join-Path $sourceRoot $relative
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            $failures.Add("Maintained operational policy file is missing: $relative")
            continue
        }
        $text = [IO.File]::ReadAllText($path, [Text.Encoding]::UTF8)
        foreach ($method in Get-MethodRecords $text) {
            $body = [string]$method.Body
            $id = "$relative|$($method.Signature)"
            if ($body -notmatch '\b(?:logger|_logger|Logger)\s*\.\s*Log\w*\s*\(') { $failures.Add("$id|logging") }
            $hasYield = $body -match '\byield\s+(?:return|break)\b'
            if ($body -notmatch '\btry\b' -or ($hasYield -and $body -notmatch '\bfinally\b') -or (-not $hasYield -and $body -notmatch '\bcatch\b')) {
                $failures.Add("$id|exception-boundary")
            }
            foreach ($logCall in [regex]::Matches($body, '\b(?:logger|_logger|Logger)\s*\.\s*Log\w*\s*\((?<args>[\s\S]{0,500}?)\)')) {
                $arguments = $logCall.Groups['args'].Value
                if ($arguments.Contains('"') -and -not ($arguments.Contains('$"') -or $arguments.Contains('$@"') -or $arguments.Contains('@$"'))) {
                    $failures.Add("$id|interpolated-message")
                    break
                }
            }
        }
    }

    if ($failures.Count -gt 0) {
        $failures | Sort-Object -Unique | ForEach-Object { Write-Error $_ }
        Fail "$($failures.Count) maintained operational diagnostics finding(s)."
    }
    Write-Host 'Maintained operational diagnostics validation passed without a legacy method baseline.'
}

$pythonExit = Invoke-PythonAudit
if ($null -ne $pythonExit) {
    if ($pythonExit -ne 0) { Fail "Python architecture audit exited with code $pythonExit." }
    exit 0
}

Write-Warning 'Python was not found; using the bundled Windows PowerShell fallback audit.'
if ($Mode -in @('static', 'all')) { Invoke-StaticFallback }
if ($Mode -in @('methods', 'all')) { Invoke-MethodFallback }
if ($Mode -in @('runtime', 'all')) { Invoke-RuntimeFallback }
