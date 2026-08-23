param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
$snapshotPath = Join-Path $RepositoryRoot 'src/LocalGPT/Migrations/LocalGptMemoryDbContextModelSnapshot.cs'
$contextPath = Join-Path $RepositoryRoot 'src/LocalGPT/BusinessObjects/EFCore/LocalGptMemoryDbContext.cs'
$projectModelPath = Join-Path $RepositoryRoot 'src/LocalGPT/BusinessObjects/LocalGptProjectModels.cs'

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

function Get-BracedBlock([string]$text, [int]$searchIndex) {
    if ($searchIndex -lt 0) { return $null }
    $open = $text.IndexOf('{', $searchIndex)
    if ($open -lt 0) { return $null }
    $depth = 0
    for ($i = $open; $i -lt $text.Length; $i++) {
        if ($text[$i] -eq '{') { $depth++ }
        elseif ($text[$i] -eq '}') {
            $depth--
            if ($depth -eq 0) { return $text.Substring($open, $i - $open + 1) }
        }
    }
    return $null
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

$projectNavigationBlock = Get-BracedBlock $snapshot $projectNavigation
if ([string]::IsNullOrWhiteSpace($projectNavigationBlock)) {
    $errors.Add('Final LocalGptProject navigation block could not be parsed.')
} else {
    foreach ($navigation in @('Artifacts', 'Requirements', 'Revisions', 'Topics', 'Versions', 'BuildVerifications', 'DocumentImports', 'EmbeddedFirmwarePlans', 'OrganicSkillLinks', 'WorkspaceRoots')) {
        $token = "b.Navigation(`"$navigation`");"
        $count = ([regex]::Matches($projectNavigationBlock, [regex]::Escape($token))).Count
        if ($count -ne 1) {
            $errors.Add("LocalGptProject navigation '$navigation' must occur exactly once in its final snapshot block; found $count.")
        }
        if (-not ($projectModel.IndexOf("ICollection<", [StringComparison]::Ordinal) -ge 0) -or -not ($projectModel.IndexOf(" $navigation { get; set; }", [StringComparison]::Ordinal) -ge 0)) {
            $errors.Add("LocalGptProject CLR model must retain collection navigation '$navigation'.")
        }
    }
}

# Relationship checks are entity-specific because revision, requirement and project entities now deliberately
# share navigation names such as Artifacts and BuildVerifications. Global token counts would reject valid
# reverse navigations and hide the actual architectural contract that each FK must target the right owner.
foreach ($relationshipToken in @(
    'b.HasOne("LocalGPT.BusinessObjects.LocalGptProject", "Project")' + "`n" + '                        .WithMany("Artifacts")',
    'b.HasOne("LocalGPT.BusinessObjects.LocalGptProject", "Project")' + "`n" + '                        .WithMany("Requirements")',
    'b.HasOne("LocalGPT.BusinessObjects.LocalGptProject", "Project")' + "`n" + '                        .WithMany("Revisions")',
    'b.HasOne("LocalGPT.BusinessObjects.LocalGptProject", "Project")' + "`n" + '                        .WithMany("Topics")',
    'b.HasOne("LocalGPT.BusinessObjects.LocalGptProject", "Project")' + "`n" + '                        .WithMany("Versions")',
    'b.HasOne("LocalGPT.BusinessObjects.LocalGptProjectRevision", "Revision")' + "`n" + '                        .WithMany("Artifacts")',
    'b.HasOne("LocalGPT.BusinessObjects.LocalGptProjectRevision", "Revision")' + "`n" + '                        .WithMany("Requirements")',
    'b.HasOne("LocalGPT.BusinessObjects.LocalGptProjectRequirement", "Requirement")' + "`n" + '                        .WithMany("Artifacts")',
    'b.HasOne("LocalGPT.BusinessObjects.ProjectCompilerInstallation", "CompilerInstallation")' + "`n" + '                        .WithMany("BuildVerifications")',
    'b.HasOne("LocalGPT.BusinessObjects.CouncilKnowledgeEntry", "KnowledgeEntry")' + "`n" + '                        .WithMany("ProjectTopicLinks")',
    'b.HasOne("LocalGPT.BusinessObjects.CouncilKnowledgeEntry", "KnowledgeEntry")' + "`n" + '                        .WithMany("RegexPatternLinks")',
    'b.HasOne("LocalGPT.BusinessObjects.RegexPattern", "RegexPattern")' + "`n" + '                        .WithMany("KnowledgeLinks")'
)) {
    if ($snapshot.IndexOf($relationshipToken, [StringComparison]::Ordinal) -lt 0) {
        $errors.Add("Snapshot relationship contract is missing: $relationshipToken")
    }
}

foreach ($contextToken in @(
    '.WithMany(project => project.Artifacts)',
    '.WithMany(project => project.Requirements)',
    '.WithMany(project => project.Revisions)',
    '.WithMany(project => project.Topics)',
    '.WithMany(project => project.Versions)',
    '.WithMany(revision => revision.Artifacts)',
    '.WithMany(revision => revision.Requirements)',
    '.WithMany(requirement => requirement.Artifacts)',
    '.WithMany(installation => installation.BuildVerifications)',
    '.WithMany(entry => entry.ProjectTopicLinks)',
    '.WithMany(entry => entry.RegexPatternLinks)',
    '.WithMany(pattern => pattern.KnowledgeLinks)'
)) {
    if ($context.IndexOf($contextToken, [StringComparison]::Ordinal) -lt 0) {
        $errors.Add("DbContext must retain '$contextToken'.")
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
