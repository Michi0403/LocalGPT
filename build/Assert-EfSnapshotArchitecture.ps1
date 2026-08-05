param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
$snapshotPath = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Migrations/LocalGptMemoryDbContextModelSnapshot.cs'
$contextPath = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/BusinessObjects/EFCore/LocalGptMemoryDbContext.cs'
$projectModelPath = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/BusinessObjects/LocalGptProjectModels.cs'

foreach ($path in @($snapshotPath, $contextPath, $projectModelPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required EF architecture file is missing: $path"
    }
}

$snapshot = (Get-Content -LiteralPath $snapshotPath -Raw).Replace("`r`n", "`n").Replace("`r", "`n")
$context = Get-Content -LiteralPath $contextPath -Raw
$projectModel = Get-Content -LiteralPath $projectModelPath -Raw
$errors = [System.Collections.Generic.List[string]]::new()

function Position([string]$name, [string]$token) {
    $index = $snapshot.IndexOf($token, [StringComparison]::Ordinal)
    if ($index -lt 0) {
        $errors.Add("EF snapshot is missing $name.")
    }
    return $index
}

$projectProperties = Position 'the LocalGptProject property block' "modelBuilder.Entity(`"LocalGPT.BusinessObjects.LocalGptProject`", b =>`n                {`n                    b.Property<Guid>(`"Id`")"
$revisionProperties = Position 'the LocalGptProjectRevision property block' "modelBuilder.Entity(`"LocalGPT.BusinessObjects.LocalGptProjectRevision`", b =>`n                {`n                    b.Property<Guid>(`"Id`")"
$requirementProperties = Position 'the LocalGptProjectRequirement property block' "modelBuilder.Entity(`"LocalGPT.BusinessObjects.LocalGptProjectRequirement`", b =>`n                {`n                    b.Property<Guid>(`"Id`")"
$topicProperties = Position 'the LocalGptProjectTopic property block' "modelBuilder.Entity(`"LocalGPT.BusinessObjects.LocalGptProjectTopic`", b =>`n                {`n                    b.Property<Guid>(`"Id`")"
$versionProperties = Position 'the LocalGptProjectVersion property block' "modelBuilder.Entity(`"LocalGPT.BusinessObjects.LocalGptProjectVersion`", b =>`n                {`n                    b.Property<Guid>(`"Id`")"

$revisionRelationship = Position 'the revision-to-project relationship block' "modelBuilder.Entity(`"LocalGPT.BusinessObjects.LocalGptProjectRevision`", b =>`n                {`n                    b.HasOne"
$topicRelationship = Position 'the topic-to-project relationship block' "modelBuilder.Entity(`"LocalGPT.BusinessObjects.LocalGptProjectTopic`", b =>`n                {`n                    b.HasOne"
$versionRelationship = Position 'the version-to-project relationship block' "modelBuilder.Entity(`"LocalGPT.BusinessObjects.LocalGptProjectVersion`", b =>`n                {`n                    b.HasOne"
$projectNavigation = Position 'the final LocalGptProject navigation block' "modelBuilder.Entity(`"LocalGPT.BusinessObjects.LocalGptProject`", b =>`n                {`n                    b.Navigation(`"Artifacts`")"

if ($errors.Count -eq 0) {
    if (-not ($projectProperties -lt $revisionRelationship)) {
        $errors.Add('LocalGptProject must be declared before relationships target it. Otherwise EF creates a shared Dictionary<string, object> entity.')
    }
    if (-not ($revisionProperties -lt $revisionRelationship -and $requirementProperties -lt $revisionRelationship)) {
        $errors.Add('Database-first project property blocks must precede their relationship blocks.')
    }
    if (-not ($topicProperties -lt $topicRelationship -and $versionProperties -lt $versionRelationship)) {
        $errors.Add('Legacy project topic/version property blocks must precede their relationship blocks.')
    }
    if (-not ($revisionRelationship -lt $projectNavigation -and $topicRelationship -lt $projectNavigation -and $versionRelationship -lt $projectNavigation)) {
        $errors.Add('Collection navigation declarations must remain after relationship configuration.')
    }
}

foreach ($navigation in @('Artifacts', 'Requirements', 'Revisions', 'Topics', 'Versions')) {
    $count = ([regex]::Matches($snapshot, [regex]::Escape("b.Navigation(`"$navigation`");"))).Count
    if ($count -ne 1) {
        $errors.Add("LocalGptProject navigation '$navigation' must occur exactly once in the snapshot; found $count.")
    }
    if (-not ($projectModel.IndexOf("ICollection<", [StringComparison]::Ordinal) -ge 0) -or -not ($projectModel.IndexOf(" $navigation { get; set; }", [StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("LocalGptProject CLR model must retain collection navigation '$navigation'.")
    }
}

foreach ($relationship in @('.WithMany("Artifacts")', '.WithMany("Requirements")', '.WithMany("Revisions")', '.WithMany("Topics")', '.WithMany("Versions")')) {
    $count = ([regex]::Matches($snapshot, [regex]::Escape($relationship))).Count
    if ($count -ne 1) {
        $errors.Add("Snapshot relationship '$relationship' must occur exactly once; found $count.")
    }
}

foreach ($token in @(
    'modelBuilder.Entity<LocalGptProject>(entity =>',
    '.WithMany(project => project.Artifacts)',
    '.WithMany(project => project.Requirements)',
    '.WithMany(project => project.Revisions)',
    '.WithMany(project => project.Topics)',
    '.WithMany(project => project.Versions)')) {
    if (-not ($context.IndexOf($token, [StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("DbContext must retain '$token'.")
    }
}


foreach ($token in @(
    'b.Property<string>("ModelRoutesJson").IsRequired().HasColumnType("TEXT")',
    'b.Property<bool>("AllowParallelHardwareRoads").HasColumnType("INTEGER")',
    'modelBuilder.Entity("LocalGPT.BusinessObjects.OrganicSkillDefinition", b =>',
    'modelBuilder.Entity("LocalGPT.BusinessObjects.ProjectOrganicSkillLink", b =>',
    'modelBuilder.Entity("LocalGPT.BusinessObjects.CouncilMemberOrganicSkillLink", b =>',
    'modelBuilder.Entity("LocalGPT.BusinessObjects.CouncilTeamConfiguration", b =>',
    'b.ToTable("OrganicSkills", (string)null)',
    'b.ToTable("CouncilTeamConfigurations", (string)null)',
    '.WithMany("ProjectLinks")',
    '.WithMany("MemberLinks")')) {
    if (-not ($snapshot.IndexOf($token, [StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("EF snapshot must retain organic/council architecture token '$token'.")
    }
}

foreach ($token in @(
    'DbSet<OrganicSkillDefinition> OrganicSkills',
    'DbSet<ProjectOrganicSkillLink> ProjectOrganicSkillLinks',
    'DbSet<CouncilMemberOrganicSkillLink> CouncilMemberOrganicSkillLinks',
    'DbSet<CouncilTeamConfiguration> CouncilTeamConfigurations',
    'entity.Property(item => item.ModelRoutesJson).IsRequired()')) {
    if (-not ($context.IndexOf($token, [StringComparison]::Ordinal) -ge 0)) {
        $errors.Add("DbContext must retain organic/council architecture token '$token'.")
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'EF migration snapshot ordering and project navigation contracts verified.'
