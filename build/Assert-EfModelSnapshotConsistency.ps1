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
$businessObjectsRoot = Join-Path $root 'src/LocalGPT/BusinessObjects'
$contextPath = Join-Path $businessObjectsRoot 'EFCore/LocalGptMemoryDbContext.cs'
$snapshotPath = Join-Path $root 'src/LocalGPT/Migrations/LocalGptMemoryDbContextModelSnapshot.cs'

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

# Validate relational indexes as well as scalar properties for the Council role
# assignment migration. A foreign-key index is NOT conventionally generated when
# the same columns are already the leading columns of a composite primary key.
# A hand-edited snapshot containing such an index creates EF Core pending model
# changes (and blocks startup/seeding), even though the migration file exists.
$bindingContextMatch = [regex]::Match($contextText, 'modelBuilder\.Entity<CouncilRoleModelBinding>\s*\(')
$bindingSnapshotMatch = [regex]::Match($snapshotText, 'modelBuilder\.Entity\("LocalGPT\.BusinessObjects\.CouncilRoleModelBinding", b =>\s*\{')
$roleSnapshotMatch = [regex]::Match($snapshotText, 'modelBuilder\.Entity\("LocalGPT\.BusinessObjects\.CouncilRoleIndex", b =>\s*\{')
$roleMigrationPath = Join-Path $root 'src/LocalGPT/Migrations/20261029123000_AddCouncilRoleAssignments.cs'
if ($bindingContextMatch.Success -or $bindingSnapshotMatch.Success -or $roleSnapshotMatch.Success) {
    if (-not ($bindingContextMatch.Success -and $bindingSnapshotMatch.Success -and $roleSnapshotMatch.Success)) {
        $failures.Add('Council role binding context and both snapshot entity definitions must be present together.')
    }
    else {
        $contextBindingBlock = Get-BracedBlock $contextText $bindingContextMatch.Index
        $snapshotBindingBlock = Get-BracedBlock $snapshotText $bindingSnapshotMatch.Index
        $snapshotRoleBlock = Get-BracedBlock $snapshotText $roleSnapshotMatch.Index
        if (-not $contextBindingBlock -or -not $snapshotBindingBlock -or -not $snapshotRoleBlock) {
            $failures.Add('Council role binding source/snapshot entity definitions contain an invalid block.')
        }
        else {
            foreach ($expected in @('b.HasKey("TeamKey", "RoleName", "ModelKey")', 'b.HasIndex("ModelKey")')) {
                if (-not $snapshotBindingBlock.Contains($expected)) {
                    $failures.Add('CouncilRoleModelBinding snapshot is missing: ' + $expected)
                }
            }
            if (-not $snapshotRoleBlock.Contains('b.HasIndex("RoleName")')) {
                $failures.Add('CouncilRoleIndex snapshot must include the role lookup index.')
            }
            $redundantIndex = 'b.HasIndex("TeamKey", "RoleName")'
            $contextDeclaresIndex = $contextBindingBlock -match 'entity\.HasIndex\(\s*item\s*=>\s*new\s*\{\s*item\.TeamKey\s*,\s*item\.RoleName\s*\}'
            if ($snapshotBindingBlock.Contains($redundantIndex) -and -not $contextDeclaresIndex) {
                $failures.Add('CouncilRoleModelBinding snapshot has a redundant FK index absent from OnModelCreating (EF PendingModelChangesWarning).')
            }
        }
    }
    if (-not (Test-Path -LiteralPath $roleMigrationPath -PathType Leaf)) {
        $failures.Add('Council role assignment model has no schema migration file.')
    }
    else {
        $migrationSource = [System.IO.File]::ReadAllText($roleMigrationPath, [System.Text.Encoding]::UTF8)
        foreach ($name in @('CouncilRoleIndexes', 'CouncilRoleModelBindings',
                'FK_CouncilRoleModelBindings_CouncilRoleIndexes_TeamKey_RoleName',
                'IX_CouncilRoleIndexes_RoleName', 'IX_CouncilRoleModelBindings_ModelKey')) {
            if (-not $migrationSource.Contains($name)) {
                $failures.Add('Council role migration is missing schema contract: ' + $name)
            }
        }
    }
}

# SQLite cannot execute these EF operations: the SQLite migrations SQL generator throws NotSupportedException
# (for example 'SQLite does not support this migration operation (DropColumnOperation)') and startup stops.
# Down methods keep them by repository convention; an Up must never contain them. Replace the column in Up and let
# DatabaseMigrationCompatibilityService drop the obsolete column after migration with state-checked SQL.
$migrationsRoot = Join-Path $root 'src/LocalGPT/Migrations'
$sqliteUnsupportedOperations = 'DropColumn|AlterColumn|DropForeignKey|AddForeignKey|DropPrimaryKey|AddPrimaryKey|AddUniqueConstraint|DropUniqueConstraint|AddCheckConstraint|DropCheckConstraint'
$migrationFiles = @(Get-ChildItem -LiteralPath $migrationsRoot -File -Filter '*.cs' |
    Where-Object { $_.Name -notlike '*.Designer.cs' -and $_.Name -ne 'LocalGptMemoryDbContextModelSnapshot.cs' })
foreach ($migrationFile in $migrationFiles) {
    $migrationText = [System.IO.File]::ReadAllText($migrationFile.FullName, [System.Text.Encoding]::UTF8)
    $upMatch = [regex]::Match($migrationText, '\bvoid\s+Up\s*\(')
    if (-not $upMatch.Success) { continue }
    $upBody = Get-BracedBlock $migrationText $upMatch.Index
    if ($null -eq $upBody) { continue }
    foreach ($operation in [regex]::Matches($upBody, '\bmigrationBuilder\s*\.\s*(?<op>' + $sqliteUnsupportedOperations + ')\b')) {
        $failures.Add('src/LocalGPT/Migrations/' + $migrationFile.Name + ': Up uses migrationBuilder.' + $operation.Groups['op'].Value +
            ', which SQLite migrations cannot execute. Keep the old column in Up and add a SqliteColumnRetirement in ' +
            'DatabaseMigrationCompatibilityService.ColumnRetirement.cs so it is dropped after migration with state-checked SQL.')
    }
}

if ($failures.Count -gt 0) {
    Write-Host 'EF model/snapshot consistency validation failed:'
    foreach ($failure in $failures | Sort-Object -Unique) { Write-Host "  - $failure" }
    Fail 'EF model/snapshot consistency validation failed. Add a real migration and update the model snapshot before shipping.'
}

Write-Host "EF model/snapshot consistency validation passed for $($entityTypes.Count) DbSet entity type(s)."
