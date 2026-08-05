param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$errors = [System.Collections.Generic.List[string]]::new()
$projectFiles = Get-ChildItem -LiteralPath $root -Recurse -Filter *.csproj -File |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|artifacts)[\\/]' }

if ($projectFiles.Count -eq 0) {
    $errors.Add('No C# project files were found.')
}

foreach ($project in $projectFiles) {
    try {
        [xml]$xml = Get-Content -LiteralPath $project.FullName -Raw
    }
    catch {
        $errors.Add("Invalid project XML: $($project.FullName): $($_.Exception.Message)")
        continue
    }

    $projectDirectory = $project.DirectoryName
    foreach ($reference in @($xml.Project.ItemGroup.ProjectReference)) {
        if ($null -eq $reference) { continue }
        $include = [string]$reference.Include
        if ([string]::IsNullOrWhiteSpace($include)) { continue }
        $target = [IO.Path]::GetFullPath((Join-Path $projectDirectory $include))
        if (-not (Test-Path -LiteralPath $target -PathType Leaf)) {
            $errors.Add("Missing ProjectReference target '$include' from '$($project.FullName)'.")
        }
    }
}

$requiredFiles = @(
    'src/LocalGPT/Program.cs',
    'src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj',
    'src/LocalGPT.WireProtocolVersion/OneWireProtocolContracts.cs',
    'src/LocalGPT/BusinessObjects/OrganicSkillPersistenceModels.cs',
    'src/LocalGPT/BusinessObjects/OrganicCouncilModels.cs',
    'src/LocalGPT/Services/OneWire/OrganicPluginDxAiFunctions.cs',
    'src/LocalGPT/Migrations/20260726133000_AddOrganicSkillsAndHardwareRoutes.cs',
    'src/LocalGPT/Migrations/20260726150000_AddCouncilTeamScripting.cs',
    'src/LocalGPT/Migrations/LocalGptMemoryDbContextModelSnapshot.cs'
)
foreach ($relative in $requiredFiles) {
    $path = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $errors.Add("Required architecture/build-closure file is missing: $relative")
    }
}

$contractPath = Join-Path $root 'src/LocalGPT.WireProtocolVersion/OneWireProtocolContracts.cs'
if (Test-Path -LiteralPath $contractPath -PathType Leaf) {
    $contract = Get-Content -LiteralPath $contractPath -Raw
    foreach ($token in @(
        'public interface IOneWireInteractionContract',
        'RequiresHumanInteractionOnTargetSystem',
        'RequiresAutomatedInteractionOnTargetSystem',
        'InteractionValueJson',
        'public interface IOneWireCapabilityProvider',
        'public interface IOneWireTransportAdapter')) {
        if (-not ($contract.IndexOf($token, [StringComparison]::Ordinal) -ge 0)) {
            $errors.Add("Shared WireProtocolVersion contract is missing '$token'.")
        }
    }
}

$programPath = Join-Path $root 'src/LocalGPT/Program.cs'
if (Test-Path -LiteralPath $programPath -PathType Leaf) {
    $program = Get-Content -LiteralPath $programPath -Raw
    foreach ($token in @(
        'public const int DefaultPort = 5000;',
        'public static System.Int32 Port => System.Threading.Volatile.Read',
        'public static string BaseUrl')) {
        if (-not ($program.IndexOf($token, [StringComparison]::Ordinal) -ge 0)) {
            $errors.Add("Protected installer/bootstrap port contract is missing '$token'.")
        }
    }
    if ($program -match '(?<!System\.Threading\.)\bVolatile\.(Read|Write)') {
        $errors.Add('Program.cs contains an unqualified Volatile reference; DevExpress.CodeParser.Volatile would make the build ambiguous.')
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Project-reference, shared protocol, source inventory and protected bootstrap closure verified for $($projectFiles.Count) project(s)."
