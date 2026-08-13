using DevExpress.Blazor;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a workspace context application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="ProjectName">Project name value supplied to the workspace context operation and used when producing its result.</param>
/// <param name="ModId">Identifier of the mod to use for this operation.</param>
/// <param name="PackageName">Package name value supplied to the workspace context operation and used when producing its result.</param>
/// <param name="MainClassName">Main class name value supplied to the workspace context operation and used when producing its result.</param>
/// <param name="ProjectRoot">Project root value supplied to the workspace context operation and used when producing its result.</param>
/// <param name="JavaRoot">Java root value supplied to the workspace context operation and used when producing its result.</param>
/// <param name="ResourceRoot">Resource root value supplied to the workspace context operation and used when producing its result.</param>
/// <param name="AssetsRoot">Assets root value supplied to the workspace context operation and used when producing its result.</param>
/// <param name="BuildFilePath">Build file path value supplied to the workspace context operation and used when producing its result.</param>
/// <param name="MainClassPath">Main class path value supplied to the workspace context operation and used when producing its result.</param>
/// <param name="MetadataPath">Metadata path value supplied to the workspace context operation and used when producing its result.</param>
/// <param name="ReadmePath">Readme path value supplied to the workspace context operation and used when producing its result.</param>
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
/// Represents a workspace layout application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="context">Context value supplied to the workspace layout operation and used when producing its result.</param>
public sealed class WorkspaceLayout(WorkspaceContext context)
{
    /// <summary>
    /// Gets the context value that forms part of the workspace layout state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The context value exposed by <see cref="WorkspaceLayout"/>.</value>
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
    /// Represents a minecraft dependency version info application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="Loader">Loader value supplied to the minecraft dependency version info operation and used when producing its result.</param>
    /// <param name="RequestedMinecraftVersion">Requested minecraft version value supplied to the minecraft dependency version info operation and used when producing its result.</param>
    /// <param name="MatchedMinecraftVersion">Matched minecraft version value supplied to the minecraft dependency version info operation and used when producing its result.</param>
    /// <param name="JavaVersion">Java version value supplied to the minecraft dependency version info operation and used when producing its result.</param>
    /// <param name="GradleVersion">Gradle version value supplied to the minecraft dependency version info operation and used when producing its result.</param>
    /// <param name="FabricLoaderVersion">Fabric loader version value supplied to the minecraft dependency version info operation and used when producing its result.</param>
    /// <param name="FabricApiVersion">Fabric api version value supplied to the minecraft dependency version info operation and used when producing its result.</param>
    /// <param name="NeoForgeVersion">Neo forge version value supplied to the minecraft dependency version info operation and used when producing its result.</param>
    /// <param name="PaperApiVersion">Paper api version value supplied to the minecraft dependency version info operation and used when producing its result.</param>
    /// <param name="DatapackPackFormat">Datapack pack format value supplied to the minecraft dependency version info operation and used when producing its result.</param>
    /// <param name="IsExactMatch">Value indicating whether exact match should apply to this operation.</param>
    /// <param name="NeedsVerification">Value indicating whether verification should apply to this operation.</param>
    /// <param name="Notes">Notes value supplied to the minecraft dependency version info operation and used when producing its result.</param>
    /// <param name="Source">Source value supplied to the minecraft dependency version info operation and used when producing its result.</param>
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
    /// Represents catalog state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
    /// </summary>
    /// <param name="MinecraftVersion">Minecraft version value supplied to the catalog operation and used when producing its result.</param>
    /// <param name="FabricApiVersion">Fabric api version value supplied to the catalog operation and used when producing its result.</param>
    /// <param name="NeoForgeVersion">Neo forge version value supplied to the catalog operation and used when producing its result.</param>
    /// <param name="PaperApiVersion">Paper api version value supplied to the catalog operation and used when producing its result.</param>
    /// <param name="JavaVersion">Java version value supplied to the catalog operation and used when producing its result.</param>
    /// <param name="Notes">Notes value supplied to the catalog operation and used when producing its result.</param>
    public sealed record CatalogEntry(
string MinecraftVersion,
string? FabricApiVersion,
string? NeoForgeVersion,
string? PaperApiVersion,
string? JavaVersion,
string Notes);

    /// <summary>
    /// Represents a minecraft datapack version info application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="RequestedVersion">Requested version value supplied to the minecraft datapack version info operation and used when producing its result.</param>
    /// <param name="MatchedVersion">Matched version value supplied to the minecraft datapack version info operation and used when producing its result.</param>
    /// <param name="PackFormat">Pack format value supplied to the minecraft datapack version info operation and used when producing its result.</param>
    /// <param name="FunctionRegistryFolder">Function registry folder value supplied to the minecraft datapack version info operation and used when producing its result.</param>
    /// <param name="IsExactMatch">Value indicating whether exact match should apply to this operation.</param>
    /// <param name="NeedsVerification">Value indicating whether verification should apply to this operation.</param>
    /// <param name="Notes">Notes value supplied to the minecraft datapack version info operation and used when producing its result.</param>
    /// <param name="Source">Source value supplied to the minecraft datapack version info operation and used when producing its result.</param>
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
/// Represents the outcome of Ollama tags, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class OllamaTagsResponse
{
    /// <summary>
    /// Gets or sets the models collection maintained or exposed by this Ollama tags instance for downstream processing.
    /// </summary>
    /// <value>The models value exposed by <see cref="OllamaTagsResponse"/>.</value>
    public List<OllamaModelEntry> Models { get; set; } = new();
}

/// <summary>
/// Represents Ollama model state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class OllamaModelEntry
{
    /// <summary>
    /// Gets or sets the name value that forms part of the Ollama model state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="OllamaModelEntry"/>.</value>
    public string? Name { get; set; }
    /// <summary>
    /// Gets or sets the model value that forms part of the Ollama model state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model value exposed by <see cref="OllamaModelEntry"/>.</value>
    public string? Model { get; set; }
    /// <summary>
    /// Gets or sets the details value that forms part of the Ollama model state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The details value exposed by <see cref="OllamaModelEntry"/>.</value>
    public OllamaModelDetails? Details { get; set; }
}

/// <summary>
/// Represents a benchmark task definition application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Id">Identifier of the resource to use for this operation.</param>
/// <param name="Name">Name value supplied to the benchmark task definition operation and used when producing its result.</param>
/// <param name="Prompt">Prompt value supplied to the benchmark task definition operation and used when producing its result.</param>
/// <param name="ManualExpectedOutput">Manual expected output value supplied to the benchmark task definition operation and used when producing its result.</param>
/// <param name="LocalGptFinalAnswer">Local gpt final answer value supplied to the benchmark task definition operation and used when producing its result.</param>
/// <param name="LocalGptBuildabilityScore">Local gpt buildability score value supplied to the benchmark task definition operation and used when producing its result.</param>
/// <param name="RequiredArtifactEntries">String dependency used by the benchmark task definition workflow to provide the corresponding application capability.</param>
/// <param name="ArchitectureEvidence">String dependency used by the benchmark task definition workflow to provide the corresponding application capability.</param>
/// <param name="WrongTemplateGuards">String dependency used by the benchmark task definition workflow to provide the corresponding application capability.</param>
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
/// Represents the outcome of OpenAI models, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class OpenAIModelsResponse
{
    /// <summary>
    /// Gets or sets the data collection maintained or exposed by this OpenAI models instance for downstream processing.
    /// </summary>
    /// <value>The data value exposed by <see cref="OpenAIModelsResponse"/>.</value>
    public List<OpenAIModelEntry> Data { get; set; } = new();
}

/// <summary>
/// Represents OpenAI model state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class OpenAIModelEntry
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this OpenAI model instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="OpenAIModelEntry"/>.</value>
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Represents the outcome of Ollama model, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class OllamaModelResponse
{
    /// <summary>
    /// Gets or sets the name value that forms part of the Ollama model state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="OllamaModelResponse"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the model value that forms part of the Ollama model state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model value exposed by <see cref="OllamaModelResponse"/>.</value>
    public string Model { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the details value that forms part of the Ollama model state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The details value exposed by <see cref="OllamaModelResponse"/>.</value>
    public OllamaModelDetails? Details { get; set; }
}

/// <summary>
/// Represents an Ollama model details application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OllamaModelDetails
{
    /// <summary>
    /// Gets or sets the family value that forms part of the Ollama model details state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The family value exposed by <see cref="OllamaModelDetails"/>.</value>
    public string? Family { get; set; }

    /// <summary>
    /// Gets or sets the parameter size that quantifies the associated Ollama model details data.
    /// </summary>
    /// <value>The parameter size value exposed by <see cref="OllamaModelDetails"/>.</value>
    [JsonPropertyName("parameter_size")]
    public string? ParameterSize { get; set; }

    /// <summary>
    /// Gets or sets the quantization level value that forms part of the Ollama model details state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The quantization level value exposed by <see cref="OllamaModelDetails"/>.</value>
    [JsonPropertyName("quantization_level")]
    public string? QuantizationLevel { get; set; }
}

/// <summary>
/// Represents the input contract for Ollama unload, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class OllamaUnloadRequest
{
    /// <summary>
    /// Gets or sets the model value that forms part of the Ollama unload state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model value exposed by <see cref="OllamaUnloadRequest"/>.</value>
    public string Model { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the prompt value that forms part of the Ollama unload state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prompt value exposed by <see cref="OllamaUnloadRequest"/>.</value>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether stream applies to the Ollama unload state.
    /// </summary>
    /// <value>The stream value exposed by <see cref="OllamaUnloadRequest"/>.</value>
    public bool Stream { get; set; }

    /// <summary>
    /// Gets or sets the keep alive value that forms part of the Ollama unload state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The keep alive value exposed by <see cref="OllamaUnloadRequest"/>.</value>
    [JsonPropertyName("keep_alive")]
    public string KeepAlive { get; set; } = "0s";
}

/// <summary>
/// Represents an artifact workspace summary application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="WorkspaceName">Workspace name value supplied to the artifact workspace summary operation and used when producing its result.</param>
/// <param name="RootPath">Root path value supplied to the artifact workspace summary operation and used when producing its result.</param>
/// <param name="LastWriteTimeUtc">Last write time utc value supplied to the artifact workspace summary operation and used when producing its result.</param>
/// <param name="SourceFileCount">Source file count value supplied to the artifact workspace summary operation and used when producing its result.</param>
/// <param name="RazorFileCount">Razor file count value supplied to the artifact workspace summary operation and used when producing its result.</param>
/// <param name="CSharpFileCount">C sharp file count value supplied to the artifact workspace summary operation and used when producing its result.</param>
/// <param name="ZipNames">Zip names value supplied to the artifact workspace summary operation and used when producing its result.</param>
public sealed record ArtifactWorkspaceSummary(
  string WorkspaceName,
  string RootPath,
  DateTime LastWriteTimeUtc,
  int SourceFileCount,
  int RazorFileCount,
  int CSharpFileCount,
  List<string> ZipNames);

/// <summary>
/// Represents an artifact workspace file summary application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="RelativePath">Relative path value supplied to the artifact workspace file summary operation and used when producing its result.</param>
/// <param name="Length">Length value supplied to the artifact workspace file summary operation and used when producing its result.</param>
/// <param name="LastWriteTimeUtc">Last write time utc value supplied to the artifact workspace file summary operation and used when producing its result.</param>
public sealed record ArtifactWorkspaceFileSummary(
    string RelativePath,
    long Length,
    DateTime LastWriteTimeUtc);

/// <summary>
/// Represents the input contract for artifact workspace file save, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
/// <param name="RelativePath">Relative path value supplied to the artifact workspace file save operation and used when producing its result.</param>
/// <param name="Content">Content value supplied to the artifact workspace file save operation and used when producing its result.</param>
public sealed record ArtifactWorkspaceFileSaveRequest(
    string RelativePath,
    string? Content);

    /// <summary>
    /// Represents an analyzed upload file application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="Summary">Summary value supplied to the analyzed upload file operation and used when producing its result.</param>
    /// <param name="Excerpt">Excerpt value supplied to the analyzed upload file operation and used when producing its result.</param>
    public sealed record AnalyzedUploadFile(
ChatUploadWorkspaceFileSummary Summary,
string Excerpt);

/// <summary>
/// Represents an artifact contract report application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="QualityStatus">Quality status value supplied to the artifact contract report operation and used when producing its result.</param>
/// <param name="ContractStatus">Contract status value supplied to the artifact contract report operation and used when producing its result.</param>
/// <param name="ContractChecks">String dependency used by the artifact contract report workflow to provide the corresponding application capability.</param>
/// <param name="MissingRequirements">String dependency used by the artifact contract report workflow to provide the corresponding application capability.</param>
/// <param name="Summary">Summary value supplied to the artifact contract report operation and used when producing its result.</param>
public sealed record ArtifactContractReport(
    string QualityStatus,
    string ContractStatus,
    IReadOnlyList<string> ContractChecks,
    IReadOnlyList<string> MissingRequirements,
    string Summary);

/// <summary>
/// Represents a minecraft datapack artifact identity application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="ProjectName">Project name value supplied to the minecraft datapack artifact identity operation and used when producing its result.</param>
/// <param name="ModId">Identifier of the mod to use for this operation.</param>
/// <param name="PackageName">Package name value supplied to the minecraft datapack artifact identity operation and used when producing its result.</param>
/// <param name="DisplayName">Display name value supplied to the minecraft datapack artifact identity operation and used when producing its result.</param>
public sealed record MinecraftDatapackArtifactIdentity(
    string ProjectName,
    string ModId,
    string PackageName,
    string DisplayName);

/// <summary>
/// Defines the supported generated solution archetype values used to select or describe behavior in the surrounding workflow.
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
/// Represents a generated archetype page application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="FileName">File name value supplied to the generated archetype page operation and used when producing its result.</param>
/// <param name="Source">Source value supplied to the generated archetype page operation and used when producing its result.</param>
public sealed record GeneratedArchetypePage(string FileName, string Source);

/// <summary>
/// Represents a generated promise module application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="FileName">File name value supplied to the generated promise module operation and used when producing its result.</param>
/// <param name="Route">Route value supplied to the generated promise module operation and used when producing its result.</param>
/// <param name="Title">Title value supplied to the generated promise module operation and used when producing its result.</param>
/// <param name="Summary">Summary value supplied to the generated promise module operation and used when producing its result.</param>
/// <param name="Areas">String dependency used by the generated promise module workflow to provide the corresponding application capability.</param>
public sealed record GeneratedPromiseModule(
    string FileName,
    string Route,
    string Title,
    string Summary,
    IReadOnlyList<string> Areas);

/// <summary>
/// Represents a test lab route application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Label">Label value supplied to the test lab route operation and used when producing its result.</param>
/// <param name="Path">Path value supplied to the test lab route operation and used when producing its result.</param>
/// <param name="Style">Style value supplied to the test lab route operation and used when producing its result.</param>
public sealed record TestLabRoute(string Label, string Path, ButtonRenderStyle Style);

/// <summary>
/// Represents a test lab download link application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Label">Label value supplied to the test lab download link operation and used when producing its result.</param>
/// <param name="AbsoluteUrl">Absolute url value supplied to the test lab download link operation and used when producing its result.</param>
public sealed record TestLabDownloadLink(string Label, string AbsoluteUrl);

/// <summary>
/// Represents a learn base preset application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Label">Label value supplied to the learn base preset operation and used when producing its result.</param>
/// <param name="RootPath">Root path value supplied to the learn base preset operation and used when producing its result.</param>
/// <param name="Description">Description value supplied to the learn base preset operation and used when producing its result.</param>
/// <param name="RecommendedMaxProjects">Recommended max projects value supplied to the learn base preset operation and used when producing its result.</param>
public sealed record LearnBasePreset(string Label, string RootPath, string Description, int RecommendedMaxProjects);

/// <summary>
/// Represents a learn base scan profile application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Label">Label value supplied to the learn base scan profile operation and used when producing its result.</param>
/// <param name="MaxProjects">Max projects value supplied to the learn base scan profile operation and used when producing its result.</param>
/// <param name="Description">Description value supplied to the learn base scan profile operation and used when producing its result.</param>
public sealed record LearnBaseScanProfile(string Label, int MaxProjects, string Description);

/// <summary>
/// Represents the outcome of artifact workspace list, carrying the data and status produced by the corresponding application operation.
/// </summary>
/// <param name="BaseUrl">Base url value supplied to the artifact workspace list operation and used when producing its result.</param>
/// <param name="ArtifactRoot">Artifact root value supplied to the artifact workspace list operation and used when producing its result.</param>
/// <param name="Count">Count value supplied to the artifact workspace list operation and used when producing its result.</param>
/// <param name="LatestWorkspace">Latest workspace value supplied to the artifact workspace list operation and used when producing its result.</param>
/// <param name="Workspaces">Workspaces value supplied to the artifact workspace list operation and used when producing its result.</param>
public sealed record ArtifactWorkspaceListResponse(
    string BaseUrl,
    string ArtifactRoot,
    int Count,
    ArtifactWorkspaceSummary? LatestWorkspace,
    List<ArtifactWorkspaceSummary> Workspaces);

/// <summary>
/// Represents the outcome of artifact workspace files, carrying the data and status produced by the corresponding application operation.
/// </summary>
/// <param name="WorkspaceName">Workspace name value supplied to the artifact workspace files operation and used when producing its result.</param>
/// <param name="RootPath">Root path value supplied to the artifact workspace files operation and used when producing its result.</param>
/// <param name="Files">Files value supplied to the artifact workspace files operation and used when producing its result.</param>
public sealed record ArtifactWorkspaceFilesResponse(
    string WorkspaceName,
    string RootPath,
    List<ArtifactWorkspaceFileSummary> Files);

/// <summary>
/// Represents the outcome of artifact workspace file, carrying the data and status produced by the corresponding application operation.
/// </summary>
/// <param name="WorkspaceName">Workspace name value supplied to the artifact workspace file operation and used when producing its result.</param>
/// <param name="RootPath">Root path value supplied to the artifact workspace file operation and used when producing its result.</param>
/// <param name="RelativePath">Relative path value supplied to the artifact workspace file operation and used when producing its result.</param>
/// <param name="FullPath">Full path value supplied to the artifact workspace file operation and used when producing its result.</param>
/// <param name="Length">Length value supplied to the artifact workspace file operation and used when producing its result.</param>
/// <param name="LastWriteTimeUtc">Last write time utc value supplied to the artifact workspace file operation and used when producing its result.</param>
/// <param name="Content">Content value supplied to the artifact workspace file operation and used when producing its result.</param>
public sealed record ArtifactWorkspaceFileResponse(
    string WorkspaceName,
    string RootPath,
    string RelativePath,
    string FullPath,
    long Length,
    DateTime LastWriteTimeUtc,
    string Content);

  /// <summary>
  /// Represents a datapack reference comparison application type, grouping the state and behavior that belong to that domain concept.
  /// </summary>
  /// <param name="GeneratedZipPath">Generated zip path value supplied to the datapack reference comparison operation and used when producing its result.</param>
  /// <param name="ReferenceZipPath">Reference zip path value supplied to the datapack reference comparison operation and used when producing its result.</param>
  /// <param name="ReferenceExists">Value indicating whether reference exists should apply to this operation.</param>
  /// <param name="GeneratedFileCount">Generated file count value supplied to the datapack reference comparison operation and used when producing its result.</param>
  /// <param name="GeneratedFunctionFileCount">Generated function file count value supplied to the datapack reference comparison operation and used when producing its result.</param>
  /// <param name="GeneratedPlaceholderCount">Generated placeholder count value supplied to the datapack reference comparison operation and used when producing its result.</param>
  /// <param name="ReferenceFileCount">Reference file count value supplied to the datapack reference comparison operation and used when producing its result.</param>
  /// <param name="ReferenceFunctionFileCount">Reference function file count value supplied to the datapack reference comparison operation and used when producing its result.</param>
  /// <param name="ReferencePlaceholderCount">Reference placeholder count value supplied to the datapack reference comparison operation and used when producing its result.</param>
  /// <param name="GeneratedHasRootPackMcmeta">Value indicating whether generated has root pack mcmeta should apply to this operation.</param>
  /// <param name="ReferenceHasRootPackMcmeta">Value indicating whether reference has root pack mcmeta should apply to this operation.</param>
  /// <param name="ReferenceHasNestedPackMcmeta">Value indicating whether reference has nested pack mcmeta should apply to this operation.</param>
  /// <param name="GeneratedHasLoadTag">Value indicating whether generated has load tag should apply to this operation.</param>
  /// <param name="GeneratedHasTickTag">Value indicating whether generated has tick tag should apply to this operation.</param>
  /// <param name="ReferenceHasLoadTag">Value indicating whether reference has load tag should apply to this operation.</param>
  /// <param name="ReferenceHasTickTag">Value indicating whether reference has tick tag should apply to this operation.</param>
  /// <param name="CriticalFileCount">Critical file count value supplied to the datapack reference comparison operation and used when producing its result.</param>
  /// <param name="PreservedCriticalFileCount">Preserved critical file count value supplied to the datapack reference comparison operation and used when producing its result.</param>
  /// <param name="PreservedCriticalFiles">Preserved critical files value supplied to the datapack reference comparison operation and used when producing its result.</param>
  /// <param name="ReferencePlaceholderSamples">Reference placeholder samples value supplied to the datapack reference comparison operation and used when producing its result.</param>
  /// <param name="Summary">Summary value supplied to the datapack reference comparison operation and used when producing its result.</param>
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
