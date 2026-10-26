[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectPath = Join-Path $root 'src/LocalGPT/LocalGPT.csproj'
$projectDirectory = Split-Path -Parent $projectPath
$errors = New-Object 'System.Collections.Generic.List[string]'

# Check the actual project inventory, not a hand-maintained list of seed
# document names or version-specific paths. This catches NETSDK1022-style
# duplicate physical includes without requiring Python or a .NET build.
if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw "Embedded resource project missing: $projectPath"
}
$project = [xml]([IO.File]::ReadAllText($projectPath))
$physicalPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$logicalNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$resources = @($project.SelectNodes('//EmbeddedResource[@Include]'))
foreach ($resource in $resources) {
    $include = [string]$resource.GetAttribute('Include')
    if ([string]::IsNullOrWhiteSpace($include)) { continue }
    # Property-expanded and wildcard includes are resolved by MSBuild. Do not
    # pretend a source-only check can enumerate their physical resources.
    if ($include.Contains('$(') -or $include.Contains('*') -or $include.Contains('?')) { continue }

    $nativeInclude = $include.Replace([char]'\', [IO.Path]::DirectorySeparatorChar).Replace([char]'/', [IO.Path]::DirectorySeparatorChar)
    $fullPath = [IO.Path]::GetFullPath((Join-Path $projectDirectory $nativeInclude))
    if (-not $physicalPaths.Add($fullPath)) {
        $errors.Add("$($projectPath): duplicate EmbeddedResource Include '$include'; each physical file may be included only once.")
    }
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        $errors.Add("$($projectPath): missing EmbeddedResource file '$include'.")
    }

    $logicalName = [string]$resource.GetAttribute('LogicalName')
    if ([string]::IsNullOrWhiteSpace($logicalName)) { continue }
    if (-not $logicalNames.Add($logicalName)) {
        $errors.Add("$($projectPath): duplicate EmbeddedResource LogicalName '$logicalName'.")
    }
    if ($logicalName.StartsWith('LocalGPT.SeedDocument.', [StringComparison]::Ordinal)) {
        $prefix = $root.TrimEnd([char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)) + [IO.Path]::DirectorySeparatorChar
        if (-not $fullPath.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
            $errors.Add("$($projectPath): seed document '$include' is outside the repository.")
            continue
        }
        $relative = $fullPath.Substring($prefix.Length).Replace([char]'\', [char]'/')
        $expectedName = 'LocalGPT.SeedDocument.' + $relative.Replace('/', '.')
        if (-not [string]::Equals($logicalName, $expectedName, [StringComparison]::Ordinal)) {
            $errors.Add("$($projectPath): seed document '$include' must use derived LogicalName '$expectedName', not '$logicalName'.")
        }
    }
}
if ($errors.Count -gt 0) {
    foreach ($issue in $errors) { Write-Error $issue }
    throw "Embedded resource architecture validation found $($errors.Count) issue(s)."
}
Write-Host "Embedded resource architecture verified for $($resources.Count) declared resources (no pinned release or seed-file list)." -ForegroundColor Green
