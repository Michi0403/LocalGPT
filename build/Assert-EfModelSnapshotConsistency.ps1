Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw $Message }

function Get-BracedBlock([string]$Text, [int]$SearchIndex) {
    $open = $Text.IndexOf('{', $SearchIndex)
    if ($open -lt 0) { return $null }
    $depth = 0
    for ($i = $open; $i -lt $Text.Length; $i++) {
        if ($Text[$i] -eq '{') { $depth++ }
        elseif ($Text[$i] -eq '}') {
            $depth--
            if ($depth -eq 0) { return $Text.Substring($open, $i - $open + 1) }
        }
    }
    return $null
}

$root = Split-Path -Parent $PSScriptRoot
$businessObjectsRoot = Join-Path $root 'src\LocalGPT\BusinessObjects'
$contextPath = Join-Path $businessObjectsRoot 'EFCore\LocalGptMemoryDbContext.cs'
$snapshotPath = Join-Path $root 'src\LocalGPT\Migrations\LocalGptMemoryDbContextModelSnapshot.cs'

if (-not (Test-Path -LiteralPath $contextPath -PathType Leaf)) { Fail 'LocalGptMemoryDbContext.cs is missing.' }
if (-not (Test-Path -LiteralPath $snapshotPath -PathType Leaf)) { Fail 'LocalGptMemoryDbContextModelSnapshot.cs is missing.' }

$contextText = [System.IO.File]::ReadAllText($contextPath, [System.Text.Encoding]::UTF8)
$snapshotText = [System.IO.File]::ReadAllText($snapshotPath, [System.Text.Encoding]::UTF8)
$sourceFiles = @(Get-ChildItem -LiteralPath $businessObjectsRoot -Recurse -File -Filter '*.cs')

$enumNames = @{}
foreach ($file in $sourceFiles) {
    $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    foreach ($match in [regex]::Matches($text, '\benum\s+(?<name>[A-Za-z_]\w*)')) {
        $enumNames[$match.Groups['name'].Value] = $true
    }
}

$scalarTypes = @{}
foreach ($name in @('string','Guid','DateTime','DateTimeOffset','DateOnly','TimeOnly','TimeSpan','bool','byte','sbyte','short','ushort','int','uint','long','ulong','float','double','decimal','char')) {
    $scalarTypes[$name] = $true
}

$entityTypes = New-Object System.Collections.Generic.HashSet[string]
foreach ($match in [regex]::Matches($contextText, 'DbSet<(?<name>[A-Za-z_]\w*)>')) {
    [void]$entityTypes.Add($match.Groups['name'].Value)
}

$failures = New-Object System.Collections.Generic.List[string]
foreach ($entityType in ($entityTypes | Sort-Object)) {
    $classPattern = '\b(?:public|internal)\s+(?:(?:sealed|partial|abstract)\s+)*class\s+' + [regex]::Escape($entityType) + '\b'
    $classText = $null
    foreach ($file in $sourceFiles) {
        $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
        $classMatch = [regex]::Match($text, $classPattern)
        if (-not $classMatch.Success) { continue }
        $classText = Get-BracedBlock $text $classMatch.Index
        break
    }
    if ([string]::IsNullOrWhiteSpace($classText)) {
        $failures.Add("${entityType}: entity class body could not be located")
        continue
    }

    $snapshotPattern = 'modelBuilder\.Entity\("[^\"]*\.' + [regex]::Escape($entityType) + '\", b =>\s*\{'
    $snapshotMatch = [regex]::Match($snapshotText, $snapshotPattern)
    if (-not $snapshotMatch.Success) {
        $failures.Add("${entityType}: model snapshot entity block is missing")
        continue
    }
    $snapshotBlock = Get-BracedBlock $snapshotText $snapshotMatch.Index
    if ([string]::IsNullOrWhiteSpace($snapshotBlock)) {
        $failures.Add("${entityType}: model snapshot entity block is malformed")
        continue
    }

    $snapshotProperties = @{}
    foreach ($propertyMatch in [regex]::Matches($snapshotBlock, 'b\.Property<[^>]+>\("(?<name>[A-Za-z_]\w*)"\)')) {
        $snapshotProperties[$propertyMatch.Groups['name'].Value] = $true
    }

    $propertyPattern = 'public\s+(?:required\s+)?(?<type>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*\??)\s+(?<name>[A-Za-z_]\w*)\s*\{\s*get;\s*(?:private\s+)?set;\s*\}'
    foreach ($propertyMatch in [regex]::Matches($classText, $propertyPattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)) {
        $typeName = $propertyMatch.Groups['type'].Value.TrimEnd('?')
        if (-not $scalarTypes.ContainsKey($typeName) -and -not $enumNames.ContainsKey($typeName)) { continue }

        $prefixStart = [Math]::Max(0, $propertyMatch.Index - 240)
        $prefix = $classText.Substring($prefixStart, $propertyMatch.Index - $prefixStart)
        if ($prefix -match '\[NotMapped(?:Attribute)?\]') { continue }

        $propertyName = $propertyMatch.Groups['name'].Value
        if (-not $snapshotProperties.ContainsKey($propertyName)) {
            $failures.Add("$entityType.$propertyName is persisted by convention but missing from LocalGptMemoryDbContextModelSnapshot")
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host 'EF model/snapshot consistency validation failed:'
    foreach ($failure in $failures | Sort-Object -Unique) { Write-Host "  - $failure" }
    Fail 'EF model/snapshot consistency validation failed. Add a real migration and update the model snapshot before shipping.'
}

Write-Host "EF model/snapshot consistency validation passed for $($entityTypes.Count) DbSet entity type(s)."
