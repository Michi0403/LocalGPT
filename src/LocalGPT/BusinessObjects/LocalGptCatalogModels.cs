using DevExpress.Blazor;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

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

public sealed class WorkspaceLayout(WorkspaceContext context)
{
    public WorkspaceContext Context { get; } = context;

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

    public sealed record CatalogEntry(
string MinecraftVersion,
string? FabricApiVersion,
string? NeoForgeVersion,
string? PaperApiVersion,
string? JavaVersion,
string Notes);

    public sealed record MinecraftDatapackVersionInfo(
string RequestedVersion,
string MatchedVersion,
string PackFormat,
string FunctionRegistryFolder,
bool IsExactMatch,
bool NeedsVerification,
string Notes,
string Source);

public sealed class OllamaTagsResponse
{
    public List<OllamaModelEntry> Models { get; set; } = new();
}

public sealed class OllamaModelEntry
{
    public string? Name { get; set; }
    public string? Model { get; set; }
    public OllamaModelDetails? Details { get; set; }
}

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

public sealed class OpenAIModelsResponse
{
    public List<OpenAIModelEntry> Data { get; set; } = new();
}

public sealed class OpenAIModelEntry
{
    public string Id { get; set; } = string.Empty;
}

public sealed class OllamaModelResponse
{
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public OllamaModelDetails? Details { get; set; }
}

public sealed class OllamaModelDetails
{
    public string? Family { get; set; }

    [JsonPropertyName("parameter_size")]
    public string? ParameterSize { get; set; }

    [JsonPropertyName("quantization_level")]
    public string? QuantizationLevel { get; set; }
}

public sealed class OllamaUnloadRequest
{
    public string Model { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public bool Stream { get; set; }

    [JsonPropertyName("keep_alive")]
    public string KeepAlive { get; set; } = "0s";
}

public sealed record ArtifactWorkspaceSummary(
  string WorkspaceName,
  string RootPath,
  DateTime LastWriteTimeUtc,
  int SourceFileCount,
  int RazorFileCount,
  int CSharpFileCount,
  List<string> ZipNames);

public sealed record ArtifactWorkspaceFileSummary(
    string RelativePath,
    long Length,
    DateTime LastWriteTimeUtc);

public sealed record ArtifactWorkspaceFileSaveRequest(
    string RelativePath,
    string? Content);

    public sealed record AnalyzedUploadFile(
ChatUploadWorkspaceFileSummary Summary,
string Excerpt);

public sealed record ArtifactContractReport(
    string QualityStatus,
    string ContractStatus,
    IReadOnlyList<string> ContractChecks,
    IReadOnlyList<string> MissingRequirements,
    string Summary);

public sealed record MinecraftDatapackArtifactIdentity(
    string ProjectName,
    string ModId,
    string PackageName,
    string DisplayName);

public enum GeneratedSolutionArchetype
{
    Generic,
    LocalGpt,
    TacosPortal,
    BotBackend,
    AiHost
}

public sealed record GeneratedArchetypePage(string FileName, string Source);

public sealed record GeneratedPromiseModule(
    string FileName,
    string Route,
    string Title,
    string Summary,
    IReadOnlyList<string> Areas);

public sealed record TestLabRoute(string Label, string Path, ButtonRenderStyle Style);

public sealed record TestLabDownloadLink(string Label, string AbsoluteUrl);

public sealed record LearnBasePreset(string Label, string RootPath, string Description, int RecommendedMaxProjects);

public sealed record LearnBaseScanProfile(string Label, int MaxProjects, string Description);

public sealed record ArtifactWorkspaceListResponse(
    string BaseUrl,
    string ArtifactRoot,
    int Count,
    ArtifactWorkspaceSummary? LatestWorkspace,
    List<ArtifactWorkspaceSummary> Workspaces);

public sealed record ArtifactWorkspaceFilesResponse(
    string WorkspaceName,
    string RootPath,
    List<ArtifactWorkspaceFileSummary> Files);

public sealed record ArtifactWorkspaceFileResponse(
    string WorkspaceName,
    string RootPath,
    string RelativePath,
    string FullPath,
    long Length,
    DateTime LastWriteTimeUtc,
    string Content);

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
