using DevExpress.Blazor;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a workspace context.
/// </summary>
public sealed record WorkspaceContext(
    string ProjectName,
    string ModId,
    string PackageName,
    string MainClassName,
    string ProjectRoot,
    string JavaRoot,
    string ResourceRoot,
    string AssetsRoot,
    string BuildFilePath,
    string MainClassPath,
    string MetadataPath,
    string ReadmePath);

/// <summary>
/// Represents a workspace layout.
/// </summary>
public sealed class WorkspaceLayout(WorkspaceContext context)
{
    /// <summary>
    /// Gets or sets context.
    /// </summary>
    public WorkspaceContext Context { get; } = context;

    /// <summary>
    /// Runs the to result operation.
    /// </summary>
    public MinecraftModWorkspace ToResult(
        string buildCommand = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\\build-local.ps1",
        string eclipseImportHint = "File > Import > Gradle > Existing Gradle Project") => new()
        {
            ProjectName = Context.ProjectName,
            RootPath = Context.ProjectRoot,
            MainClassPath = Context.MainClassPath,
            MetadataPath = Context.MetadataPath,
            BuildFilePath = Context.BuildFilePath,
            ReadmePath = Context.ReadmePath,
            BuildCommand = buildCommand,
            EclipseImportHint = eclipseImportHint
        };
}

    /// <summary>
    /// Represents a minecraft dependency version info.
    /// </summary>
    public sealed record MinecraftDependencyVersionInfo(
string Loader,
string RequestedMinecraftVersion,
string MatchedMinecraftVersion,
string JavaVersion,
string GradleVersion,
string? FabricLoaderVersion,
string? FabricApiVersion,
string? NeoForgeVersion,
string? PaperApiVersion,
string? DatapackPackFormat,
bool IsExactMatch,
bool NeedsVerification,
string Notes,
string Source);

    /// <summary>
    /// Represents a catalog entry.
    /// </summary>
    public sealed record CatalogEntry(
string MinecraftVersion,
string? FabricApiVersion,
string? NeoForgeVersion,
string? PaperApiVersion,
string? JavaVersion,
string Notes);

    /// <summary>
    /// Represents a minecraft datapack version info.
    /// </summary>
    public sealed record MinecraftDatapackVersionInfo(
string RequestedVersion,
string MatchedVersion,
string PackFormat,
string FunctionRegistryFolder,
bool IsExactMatch,
bool NeedsVerification,
string Notes,
string Source);

/// <summary>
/// Represents an ollama tags response.
/// </summary>
public sealed class OllamaTagsResponse
{
    /// <summary>
    /// Gets or sets models.
    /// </summary>
    public List<OllamaModelEntry> Models { get; set; } = new();
}

/// <summary>
/// Represents an ollama model entry.
/// </summary>
public sealed class OllamaModelEntry
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// Gets or sets model.
    /// </summary>
    public string? Model { get; set; }
    /// <summary>
    /// Gets or sets details.
    /// </summary>
    public OllamaModelDetails? Details { get; set; }
}

/// <summary>
/// Represents a benchmark task definition.
/// </summary>
public sealed record BenchmarkTaskDefinition(
           string Id,
           string Name,
           string Prompt,
           string ManualExpectedOutput,
           string LocalGptFinalAnswer,
           int LocalGptBuildabilityScore,
           IReadOnlyList<string> RequiredArtifactEntries,
           IReadOnlyList<string> ArchitectureEvidence,
           IReadOnlyList<string> WrongTemplateGuards);

/// <summary>
/// Represents an open aimodels response.
/// </summary>
public sealed class OpenAIModelsResponse
{
    /// <summary>
    /// Gets or sets data.
    /// </summary>
    public List<OpenAIModelEntry> Data { get; set; } = new();
}

/// <summary>
/// Represents an open aimodel entry.
/// </summary>
public sealed class OpenAIModelEntry
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Represents an ollama model response.
/// </summary>
public sealed class OllamaModelResponse
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets model.
    /// </summary>
    public string Model { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets details.
    /// </summary>
    public OllamaModelDetails? Details { get; set; }
}

/// <summary>
/// Represents an ollama model details.
/// </summary>
public sealed class OllamaModelDetails
{
    /// <summary>
    /// Gets or sets family.
    /// </summary>
    public string? Family { get; set; }

    /// <summary>
    /// Gets or sets parameter size.
    /// </summary>
    [JsonPropertyName("parameter_size")]
    public string? ParameterSize { get; set; }

    /// <summary>
    /// Gets or sets quantization level.
    /// </summary>
    [JsonPropertyName("quantization_level")]
    public string? QuantizationLevel { get; set; }
}

/// <summary>
/// Represents an ollama unload request.
/// </summary>
public sealed class OllamaUnloadRequest
{
    /// <summary>
    /// Gets or sets model.
    /// </summary>
    public string Model { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets prompt.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets stream.
    /// </summary>
    public bool Stream { get; set; }

    /// <summary>
    /// Gets or sets keep alive.
    /// </summary>
    [JsonPropertyName("keep_alive")]
    public string KeepAlive { get; set; } = "0s";
}

/// <summary>
/// Represents an artifact workspace summary.
/// </summary>
public sealed record ArtifactWorkspaceSummary(
  string WorkspaceName,
  string RootPath,
  DateTime LastWriteTimeUtc,
  int SourceFileCount,
  int RazorFileCount,
  int CSharpFileCount,
  List<string> ZipNames);

/// <summary>
/// Represents an artifact workspace file summary.
/// </summary>
public sealed record ArtifactWorkspaceFileSummary(
    string RelativePath,
    long Length,
    DateTime LastWriteTimeUtc);

/// <summary>
/// Represents an artifact workspace file save request.
/// </summary>
public sealed record ArtifactWorkspaceFileSaveRequest(
    string RelativePath,
    string? Content);

    /// <summary>
    /// Represents an analyzed upload file.
    /// </summary>
    public sealed record AnalyzedUploadFile(
ChatUploadWorkspaceFileSummary Summary,
string Excerpt);

/// <summary>
/// Represents an artifact contract report.
/// </summary>
public sealed record ArtifactContractReport(
    string QualityStatus,
    string ContractStatus,
    IReadOnlyList<string> ContractChecks,
    IReadOnlyList<string> MissingRequirements,
    string Summary);

/// <summary>
/// Represents a minecraft datapack artifact identity.
/// </summary>
public sealed record MinecraftDatapackArtifactIdentity(
    string ProjectName,
    string ModId,
    string PackageName,
    string DisplayName);

/// <summary>
/// Lists supported generated solution archetype values.
/// </summary>
public enum GeneratedSolutionArchetype
{
    Generic,
    LocalGpt,
    TacosPortal,
    BotBackend,
    AiHost
}

/// <summary>
/// Represents a generated archetype page.
/// </summary>
public sealed record GeneratedArchetypePage(string FileName, string Source);

/// <summary>
/// Represents a generated promise module.
/// </summary>
public sealed record GeneratedPromiseModule(
    string FileName,
    string Route,
    string Title,
    string Summary,
    IReadOnlyList<string> Areas);

/// <summary>
/// Represents a test lab route.
/// </summary>
public sealed record TestLabRoute(string Label, string Path, ButtonRenderStyle Style);

/// <summary>
/// Represents a test lab download link.
/// </summary>
public sealed record TestLabDownloadLink(string Label, string AbsoluteUrl);

/// <summary>
/// Represents a learn base preset.
/// </summary>
public sealed record LearnBasePreset(string Label, string RootPath, string Description, int RecommendedMaxProjects);

/// <summary>
/// Represents a learn base scan profile.
/// </summary>
public sealed record LearnBaseScanProfile(string Label, int MaxProjects, string Description);

/// <summary>
/// Represents an artifact workspace list response.
/// </summary>
public sealed record ArtifactWorkspaceListResponse(
    string BaseUrl,
    string ArtifactRoot,
    int Count,
    ArtifactWorkspaceSummary? LatestWorkspace,
    List<ArtifactWorkspaceSummary> Workspaces);

/// <summary>
/// Represents an artifact workspace files response.
/// </summary>
public sealed record ArtifactWorkspaceFilesResponse(
    string WorkspaceName,
    string RootPath,
    List<ArtifactWorkspaceFileSummary> Files);

/// <summary>
/// Represents an artifact workspace file response.
/// </summary>
public sealed record ArtifactWorkspaceFileResponse(
    string WorkspaceName,
    string RootPath,
    string RelativePath,
    string FullPath,
    long Length,
    DateTime LastWriteTimeUtc,
    string Content);

  /// <summary>
  /// Represents a datapack reference comparison.
  /// </summary>
  public sealed record DatapackReferenceComparison(
string GeneratedZipPath,
string ReferenceZipPath,
bool ReferenceExists,
int GeneratedFileCount,
int GeneratedFunctionFileCount,
int GeneratedPlaceholderCount,
int ReferenceFileCount,
int ReferenceFunctionFileCount,
int ReferencePlaceholderCount,
bool GeneratedHasRootPackMcmeta,
bool ReferenceHasRootPackMcmeta,
bool ReferenceHasNestedPackMcmeta,
bool GeneratedHasLoadTag,
bool GeneratedHasTickTag,
bool ReferenceHasLoadTag,
bool ReferenceHasTickTag,
int CriticalFileCount,
int PreservedCriticalFileCount,
string[] PreservedCriticalFiles,
string[] ReferencePlaceholderSamples,
string Summary);
