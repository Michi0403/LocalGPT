using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilRoleAiSelectionMode
{
    AllSelected,
    RandomRange
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HumanParticipationMode
{
    None,
    Optional,
    Required,
    HumanOnly
}

public sealed class OrganicCouncilTeamDefinition
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public List<OrganicCouncilRoleDefinition> Roles { get; set; } = [];
    public List<string> PreferredCapabilities { get; set; } = [];
    public List<string> ArchitectureContracts { get; set; } = [];
    public List<CouncilWorkflowStepDefinition> WorkflowSteps { get; set; } = [];
    public string ExpertPreparationPromptTemplate { get; set; } = string.Empty;
    public string LeaderSynthesisPromptTemplate { get; set; } = string.Empty;
    public string MainRoundInstructionTemplate { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsSystemSeed { get; set; }
    public bool IsUserModified { get; set; }
}

public sealed class OrganicCouncilRoleDefinition
{
    public string Role { get; set; } = string.Empty;
    public string Expertise { get; set; } = string.Empty;
    public string Responsibility { get; set; } = string.Empty;
    public CouncilRoleAiSelectionMode AiSelectionMode { get; set; } = CouncilRoleAiSelectionMode.AllSelected;
    public int MinimumAiParticipants { get; set; } = 1;
    public int MaximumAiParticipants { get; set; } = 1;
    public HumanParticipationMode HumanParticipationMode { get; set; } = HumanParticipationMode.None;
    public string DistinctAiAssignmentGroup { get; set; } = string.Empty;
    public string MatchAiParticipantCountToRole { get; set; } = string.Empty;
    public string PairedRole { get; set; } = string.Empty;
}

public class ProjectOrganicContext
{
    public Guid ProjectId { get; set; }
    public Guid? RevisionId { get; set; }
    public bool? HasInstaller { get; set; }
    public string InstallerPath { get; set; } = string.Empty;
    public List<string> Compilers { get; set; } = [];
    public List<string> SystemCommands { get; set; } = [];
    public List<string> KnowledgeReferences { get; set; } = [];
    public List<string> ProjectRegexPatterns { get; set; } = [];
    public List<string> FileRegexPatterns { get; set; } = [];
    public List<string> DebugPaths { get; set; } = [];
    public bool? BuildSuccessful { get; set; }
    public DateTimeOffset? LastCouncilActivityUtc { get; set; }
    public List<string> RequiredOrganicCapabilities { get; set; } = [];
    public List<string> ExternalOrganPlugins { get; set; } = [];
}

public sealed class SaveProjectOrganicContextRequest : ProjectOrganicContext
{
    public bool UserConfirmed { get; set; }
}

public sealed class CouncilWorkflowStepDefinition
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string Phase { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string PromptTemplate { get; set; } = string.Empty;
    public string ExecutionMode { get; set; } = "AllMembersParallel";
    public string AssignedModelName { get; set; } = string.Empty;
    public int RepeatCount { get; set; } = 1;
    public bool IncludePriorTranscript { get; set; } = true;
    public bool ProducesFinalAnswer { get; set; }
    public bool UseBuiltInBehavior { get; set; }
    public string LoopGroup { get; set; } = string.Empty;
    public int MaximumLoopIterations { get; set; } = 1;
    public string LoopCompletionMarker { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool RequiresHumanCheckpoint { get; set; }
    public bool CanUseOrganicFunctions { get; set; } = true;
}

public sealed class CouncilTeamConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string RolesJson { get; set; } = "[]";
    public string PreferredCapabilitiesJson { get; set; } = "[]";
    public string ArchitectureContractsJson { get; set; } = "[]";
    public string WorkflowStepsJson { get; set; } = "[]";
    public string ExpertPreparationPromptTemplate { get; set; } = string.Empty;
    public string LeaderSynthesisPromptTemplate { get; set; } = string.Empty;
    public string MainRoundInstructionTemplate { get; set; } = string.Empty;
    public int SeedVersion { get; set; } = 1;
    public bool IsSystemSeed { get; set; } = true;
    public bool IsUserModified { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class SaveCouncilTeamConfigurationRequest
{
    public OrganicCouncilTeamDefinition Team { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
    public bool UserConfirmed { get; set; }
}
