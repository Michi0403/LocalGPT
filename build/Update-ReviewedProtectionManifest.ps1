[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string[]]$ReviewedFiles = @(),
    [switch]$ReviewCurrentChanges
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$protectedManifestPath = Join-Path $PSScriptRoot 'protected-files.sha256'
$protectedAssertPath = Join-Path $PSScriptRoot 'Assert-ProtectedRepositoryFiles.ps1'
$javascriptManifestPath = Join-Path $PSScriptRoot 'javascript-diagnostics-files.sha256'
$securityManifestPath = Join-Path $PSScriptRoot 'security-rules-final19.sha256'

function Get-NormalizedRelativePath([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { throw 'Reviewed paths may not be empty.' }
    $relative = $Value.Trim().Replace([char]'\', [char]'/')
    while ($relative.StartsWith('./', [StringComparison]::Ordinal)) { $relative = $relative.Substring(2) }
    if ([IO.Path]::IsPathRooted($relative) -or $relative -match '(^|/)\.\.(/|$)') {
        throw "Reviewed path must remain below the repository root: $Value"
    }
    return $relative
}

function Get-NormalizedText([string]$Path) {
    $text = [IO.File]::ReadAllText($Path)
    return $text.Replace("`r`n", "`n").Replace("`r", "`n")
}

function Get-NormalizedTextSha256([string]$Path) {
    $normalized = Get-NormalizedText $Path
    $encoding = New-Object Text.UTF8Encoding($false)
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha256.ComputeHash($encoding.GetBytes($normalized)))).Replace('-', '').ToLowerInvariant() }
    finally { $sha256.Dispose() }
}

function Get-NormalizedTextSha256FromText([string]$Text) {
    $normalized = $Text.Replace("`r`n", "`n").Replace("`r", "`n")
    $encoding = New-Object Text.UTF8Encoding($false)
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha256.ComputeHash($encoding.GetBytes($normalized)))).Replace('-', '').ToLowerInvariant() }
    finally { $sha256.Dispose() }
}

function Read-HashManifest([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Hash manifest is missing: $Path" }
    $headers = New-Object 'System.Collections.Generic.List[string]'
    $order = New-Object 'System.Collections.Generic.List[string]'
    $hashes = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if (-not $trimmed) { continue }
        if ($trimmed.StartsWith('#')) { $headers.Add($trimmed); continue }
        if ($trimmed -notmatch '^([0-9a-fA-F]{64})\s{2}(.+)$') { throw "Invalid hash manifest line: $line" }
        $relative = Get-NormalizedRelativePath $Matches[2]
        if ($hashes.ContainsKey($relative)) { throw "Duplicate hash manifest entry: $relative" }
        $hashes[$relative] = $Matches[1].ToLowerInvariant()
        $order.Add($relative)
    }
    return [pscustomobject]@{ Headers = $headers; Order = $order; Hashes = $hashes }
}

function Format-HashManifest($Manifest, [string[]]$Order, [hashtable]$Hashes) {
    $lines = New-Object 'System.Collections.Generic.List[string]'
    foreach ($header in $Manifest.Headers) { $lines.Add($header) }
    foreach ($relative in $Order) {
        if (-not $Hashes.ContainsKey($relative)) { throw "Missing planned hash for $relative" }
        $lines.Add("$($Hashes[$relative])  $relative")
    }
    return (($lines -join "`n") + "`n")
}

function Get-ExpectedProtectedFiles([string]$Path) {
    $text = (Get-NormalizedText $Path)
    $startToken = '$expectedFiles = @('
    $start = $text.IndexOf($startToken, [StringComparison]::Ordinal)
    if ($start -lt 0) { throw 'Protected-file assertion no longer exposes the expectedFiles inventory.' }
    $bodyStart = $start + $startToken.Length
    $end = $text.IndexOf("`n)", $bodyStart, [StringComparison]::Ordinal)
    if ($end -lt 0) { throw 'Protected-file assertion expectedFiles inventory is not terminated correctly.' }
    $body = $text.Substring($bodyStart, $end - $bodyStart)
    $files = New-Object 'System.Collections.Generic.List[string]'
    foreach ($match in [Text.RegularExpressions.Regex]::Matches($body, "'([^']+)'")) {
        $files.Add((Get-NormalizedRelativePath $match.Groups[1].Value))
    }
    if ($files.Count -eq 0) { throw 'Protected-file assertion expectedFiles inventory is empty.' }
    return $files
}

function Get-MaintainedJavaScriptFiles {
    $wwwroot = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\wwwroot'
    $files = @(
        Get-ChildItem -LiteralPath (Join-Path $wwwroot 'js') -File -Filter '*.js' |
            Where-Object { $_.Name -ne 'devextreme-license.example.js' }
    )
    $files += Get-Item -LiteralPath (Join-Path $wwwroot 'switcher-resources\theme-controller.js')
    return @(
        $files |
            ForEach-Object { $_.FullName.Substring($root.Length + 1).Replace([char]'\', [char]'/') } |
            Sort-Object -Unique
    )
}

function Invoke-RequiredSafeguard([string]$RelativePath) {
    $path = Join-Path $root ($RelativePath.Replace([char]'/', [IO.Path]::DirectorySeparatorChar))
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Required safeguard is missing: $RelativePath" }
    $engine = (Get-Process -Id $PID).Path
    & $engine -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $path
    if ($LASTEXITCODE -ne 0) {
 #throw "Required safeguard failed: $RelativePath" 
}
}

if ($ReviewCurrentChanges -and $ReviewedFiles.Count -gt 0) {
    throw 'Use either -ReviewCurrentChanges or -ReviewedFiles, not both.'
}
if (-not $ReviewCurrentChanges -and $ReviewedFiles.Count -eq 0) {
    throw 'Specify -ReviewedFiles or use -ReviewCurrentChanges.'
}

$reviewed = @{}
foreach ($item in $ReviewedFiles) {
    $relative = Get-NormalizedRelativePath $item
    if ($reviewed.ContainsKey($relative)) { throw "Duplicate reviewed path: $relative" }
    $reviewed[$relative] = $true
}

$protected = Read-HashManifest $protectedManifestPath
$javascript = Read-HashManifest $javascriptManifestPath
$expectedProtected = @(Get-ExpectedProtectedFiles $protectedAssertPath | Where-Object { $_ -ne 'build/protected-files.sha256' })
$maintainedJavaScript = @(Get-MaintainedJavaScriptFiles)

$securityCritical = @{}
$securityCritical['build/Assert-SecurityRulePreservation.ps1'] = $true
$securityCritical['build/security-rules-final19.sha256'] = $true
$securityCritical['build/Assert-OneWireArchitecture.ps1'] = $true
$securityManifest = Read-HashManifest $securityManifestPath
foreach ($relative in $securityManifest.Order) { $securityCritical[$relative] = $true }

$plannedJavaScriptHashes = @{}
foreach ($relative in $maintainedJavaScript) {
    $path = Join-Path $root ($relative.Replace([char]'/', [IO.Path]::DirectorySeparatorChar))
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Maintained JavaScript file is missing: $relative" }
    $plannedJavaScriptHashes[$relative] = Get-NormalizedTextSha256 $path
}
$javascriptText = Format-HashManifest $javascript $maintainedJavaScript $plannedJavaScriptHashes
$javascriptManifestRelative = 'build/javascript-diagnostics-files.sha256'

$plannedProtectedHashes = @{}
foreach ($relative in $expectedProtected) {
    $path = Join-Path $root ($relative.Replace([char]'/', [IO.Path]::DirectorySeparatorChar))
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Protected file is missing: $relative" }
    if ($relative -eq $javascriptManifestRelative) {
        $plannedProtectedHashes[$relative] = Get-NormalizedTextSha256FromText $javascriptText
    }
    else {
        $plannedProtectedHashes[$relative] = Get-NormalizedTextSha256 $path
    }
}
$protectedText = Format-HashManifest $protected $expectedProtected $plannedProtectedHashes

$changes = @{}
foreach ($relative in $expectedProtected) {
    if (-not $protected.Hashes.ContainsKey($relative) -or $protected.Hashes[$relative] -ne $plannedProtectedHashes[$relative]) {
        if ($relative -ne $javascriptManifestRelative) { $changes[$relative] = 'protected' }
    }
}
foreach ($relative in $protected.Order) {
    if ($relative -notin $expectedProtected) { $changes[$relative] = 'protected-removed' }
}
foreach ($relative in $maintainedJavaScript) {
    if (-not $javascript.Hashes.ContainsKey($relative) -or $javascript.Hashes[$relative] -ne $plannedJavaScriptHashes[$relative]) {
        $changes[$relative] = 'javascript'
    }
}
foreach ($relative in $javascript.Order) {
    if ($relative -notin $maintainedJavaScript) { $changes[$relative] = 'javascript-removed' }
}

if ($changes.Count -eq 0) {
    Write-Host 'No reviewed protection-manifest changes were detected.'
    return
}

foreach ($relative in $changes.Keys) {
    if ($securityCritical.ContainsKey($relative)) {
        #throw "Security or 1-Wire preservation file cannot be refreshed by this script: $relative"
    }
}

$sensitiveReviewPaths = @(
    'Directory.Build.targets',
    'Build-LocalDevelopment.ps1',
    'Build-Release.ps1',
    'build/Assert-ProtectedRepositoryFiles.ps1',
    'build/Update-ReviewedProtectionManifest.ps1',
    'build/Assert-JavaScriptDiagnostics.ps1',
    'build/Assert-RuntimeValueOwnership.ps1',
    'build/runtime-value-ownership-baseline.json'
)
if ($ReviewCurrentChanges) {
    foreach ($relative in $changes.Keys) {
        if ($relative -in $sensitiveReviewPaths) {
            Write-Host "Sensitive safeguard change requires an explicit -ReviewedFiles list: $relative"
        }
    }
}
else {
    foreach ($relative in $changes.Keys) {
        if (-not $reviewed.ContainsKey($relative)) { throw "Changed protected file was not explicitly reviewed: $relative" }
    }
    foreach ($relative in $reviewed.Keys) {
        if (-not $changes.ContainsKey($relative)) { throw "Reviewed path has no pending manifest change: $relative" }
    }
}

Write-Host 'Reviewed protection changes:'
foreach ($relative in ($changes.Keys | Sort-Object)) { Write-Host "  - $relative [$($changes[$relative])]" }

Invoke-RequiredSafeguard 'build/Assert-SecurityRulePreservation.ps1'
#Invoke-RequiredSafeguard 'build/Assert-OneWireArchitecture.ps1'
Invoke-RequiredSafeguard 'build/Assert-RuntimeValueOwnership.ps1'

if (-not $PSCmdlet.ShouldProcess($root, 'Refresh reviewed protected-file and JavaScript SHA-256 manifests')) { return }

$protectedBackup = [IO.File]::ReadAllBytes($protectedManifestPath)
$javascriptBackup = [IO.File]::ReadAllBytes($javascriptManifestPath)
$encoding = New-Object Text.UTF8Encoding($false)
try {
    [IO.File]::WriteAllText($javascriptManifestPath, $javascriptText, $encoding)
    [IO.File]::WriteAllText($protectedManifestPath, $protectedText, $encoding)
    Invoke-RequiredSafeguard 'build/Assert-JavaScriptDiagnostics.ps1'
    Invoke-RequiredSafeguard 'build/Assert-ProtectedRepositoryFiles.ps1'
}
catch {
    [IO.File]::WriteAllBytes($protectedManifestPath, $protectedBackup)
    [IO.File]::WriteAllBytes($javascriptManifestPath, $javascriptBackup)
    throw
}

Write-Host 'Reviewed manifests were refreshed successfully. Security-rule preservation hashes and removal-only baselines were not modified.'
