[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $PSScriptRoot 'protected-files.sha256'

function Get-NormalizedTextSha256([string]$path) {
    $text = [IO.File]::ReadAllText($path)
    $normalized = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $encoding = [Text.UTF8Encoding]::new($false)
    $bytes = $encoding.GetBytes($normalized)
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha256.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }
}

$expectedFiles = @(
    'AGENTS.md',
    'CLAUDE.md',
    'llms.txt',
    'SECURITY.md',
    '.claude/settings.json',
    '.github/copilot-instructions.md',
    '.github/CODEOWNERS',
    '.github/workflows/source-hygiene.yml',
    'docs/HUMAN_AI_COLLABORATION.md',
    'docs/PEACEFUL_USE_COVENANT.md',
    'docs/SECURE_MAINTENANCE.md',
    'build/Assert-SecurityPolicy.ps1',
    'build/Assert-ProtectedRepositoryFiles.ps1',
    'build/Protect-GovernanceFiles.ps1',
    'build/protected-files.sha256'
)

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw 'Protected-file hash manifest is missing.'
}

$manifest = @{}
foreach ($line in Get-Content -LiteralPath $manifestPath) {
    $trimmed = $line.Trim()
    if (-not $trimmed -or $trimmed.StartsWith('#')) { continue }
    if ($trimmed -notmatch '^([0-9a-fA-F]{64})\s{2}(.+)$') {
        throw "Invalid protected-file manifest line: $line"
    }
    $relative = $Matches[2].Replace('\', '/')
    if ($manifest.ContainsKey($relative)) { throw "Duplicate protected-file manifest entry: $relative" }
    $manifest[$relative] = $Matches[1].ToLowerInvariant()
}

$errors = [System.Collections.Generic.List[string]]::new()
foreach ($relative in $expectedFiles) {
    if ($relative -eq 'build/protected-files.sha256') {
        if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
            $errors.Add("Missing protected file: $relative")
        }
        continue
    }

    if (-not $manifest.ContainsKey($relative)) {
        $errors.Add("Protected file is absent from the hash manifest: $relative")
        continue
    }

    $path = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $errors.Add("Missing protected file: $relative")
        continue
    }

    $actual = Get-NormalizedTextSha256 $path
    if ($actual -ne $manifest[$relative]) {
        $errors.Add("Protected file changed: $relative")
    }
}

$unexpected = $manifest.Keys | Where-Object { $_ -notin $expectedFiles }
foreach ($relative in $unexpected) {
    $errors.Add("Unexpected protected-file manifest entry: $relative")
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Protected repository files match the reviewed SHA-256 manifest.'
