using DevExpress.Blazor;
using System.Collections.Frozen;
using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.CodeParser;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    public sealed class LocalGptCatalogService
    {
        private readonly ILocalGptRuntimePolicyDataService runtimePolicy;
        private readonly ILogger<LocalGptCatalogService> logger;

        public LocalGptCatalogService(
            ILocalGptRuntimePolicyDataService runtimePolicy,
            ILogger<LocalGptCatalogService> logger)
        {
            this.runtimePolicy = runtimePolicy ?? throw new ArgumentNullException(nameof(runtimePolicy));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            try
            {
                Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };
                logger.LogInformation($"Initialized the LocalGPT catalog from database-backed runtime policy.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"Could not initialize the LocalGPT catalog: {exception.Message}");
                throw;
            }
        }


        public string DefaultGradleVersion => runtimePolicy.GetString(LocalGptRuntimeValue.DefaultGradleVersion);
        public Encoding Utf8NoBom { get; }
        public Regex NameCleaner => runtimePolicy.GetPattern(LocalGptRuntimePattern.NameCleaner);
        public Regex ModIdCleaner => runtimePolicy.GetPattern(LocalGptRuntimePattern.ModIdCleaner);
        public Regex PackagePartCleaner => runtimePolicy.GetPattern(LocalGptRuntimePattern.PackagePartCleaner);

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
        public string DefaultMinecraftVersion => runtimePolicy.GetString(LocalGptRuntimeValue.DefaultMinecraftVersion);

        public string DefaultJavaVersion => runtimePolicy.GetString(LocalGptRuntimeValue.DefaultJavaVersion);
        public string FabricLoaderVersion => runtimePolicy.GetString(LocalGptRuntimeValue.FabricLoaderVersion);
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
        public int MaxDxAiChatPromptCharacters => runtimePolicy.GetInt(LocalGptRuntimeValue.MaxDxAiChatPromptCharacters);
        public int MaxVisiblePromptCharacters => runtimePolicy.GetInt(LocalGptRuntimeValue.MaxVisiblePromptCharacters);
        public Regex MissingFeaturePattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.MissingFeaturePattern);
        public Regex CapabilityGapBlockPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.CapabilityGapBlockPattern);
        public Regex TruncatedTailPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.TruncatedTailPattern);
        public Regex ThinkingBlockPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.ThinkingBlockPattern);
        public Regex CouncilPromptFencePattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.CouncilPromptFencePattern);
        public Regex CouncilRequestBlockPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.CouncilRequestBlockPattern);

        public FrozenSet<string> DebugExtensions => runtimePolicy.GetCollection(LocalGptRuntimeCollection.DebugExtensions);
        public FrozenSet<string> TextExtensions => runtimePolicy.GetCollection(LocalGptRuntimeCollection.TextExtensions);

        public FrozenSet<string> BinaryDiagnosticExtensions => runtimePolicy.GetCollection(LocalGptRuntimeCollection.BinaryDiagnosticExtensions);
        public Regex TargetFrameworkPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.TargetFrameworkPattern);
        public Regex PackageReferencePattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.PackageReferencePattern);
        public Regex SensitiveNamePattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.SensitiveNamePattern);
        public FrozenSet<string> ExcludedDirectoryNames => runtimePolicy.GetCollection(LocalGptRuntimeCollection.ExcludedDirectoryNames);

        public FrozenSet<string> BinaryExtensions => runtimePolicy.GetCollection(LocalGptRuntimeCollection.BinaryExtensions);

        public FrozenSet<string> SourceExtensions => runtimePolicy.GetCollection(LocalGptRuntimeCollection.SourceExtensions);
        public string DefaultOllamaUri => runtimePolicy.GetString(LocalGptRuntimeValue.DefaultOllamaUri);
        public int MaxParticipants => runtimePolicy.GetInt(LocalGptRuntimeValue.MaxParticipants);
        public int DefaultMaxParallelModels => runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultMaxParallelModels);
        public int DefaultHeavyModelGpuLayers => runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultHeavyModelGpuLayers);
        public int MinContextTokens => runtimePolicy.GetInt(LocalGptRuntimeValue.MinContextTokens);
        public int DefaultContextTokens => runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultContextTokens);
        public int MaxContextTokens => runtimePolicy.GetInt(LocalGptRuntimeValue.MaxContextTokens);
        public int MinOutputTokens => runtimePolicy.GetInt(LocalGptRuntimeValue.MinOutputTokens);
        public int MaxOutputTokens => runtimePolicy.GetInt(LocalGptRuntimeValue.MaxOutputTokens);
        public Regex StreamStatusPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.StreamStatusPattern);
        public Regex WordPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.WordPattern);
        public Regex DevelopmentRequestPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.DevelopmentRequestPattern);
        public Regex ExplicitArtifactIntentPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.ExplicitArtifactIntentPattern);
        public Regex AdviceOnlyPromptPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.AdviceOnlyPromptPattern);
        public Regex ExplicitArtifactCreationCommandPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.ExplicitArtifactCreationCommandPattern);
        public Regex ConcreteMinecraftArtifactPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.ConcreteMinecraftArtifactPattern);
        public Regex ConcreteDotNetArtifactPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.ConcreteDotNetArtifactPattern);
        public Regex AiHostSetupPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.AiHostSetupPattern);
        public Regex ImplementationDecisionPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.ImplementationDecisionPattern);
        public Regex ImplementationChoicePattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.ImplementationChoicePattern);
        public Regex BlockingArtifactDecisionPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.BlockingArtifactDecisionPattern);
        public Regex SafeSandboxConsentPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.SafeSandboxConsentPattern);
        public Regex ExplicitDoNotGenerateUntilUserDecisionPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.ExplicitDoNotGenerateUntilUserDecisionPattern);
        public Regex DeveloperExecutionIntentPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.DeveloperExecutionIntentPattern);

 
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
        public Regex DevExpressImportPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.DevExpressImportPattern);
        public Regex DevExpressRegistrationPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.DevExpressRegistrationPattern);
        public FrozenSet<string> ArtifactTextExtensions => runtimePolicy.GetCollection(LocalGptRuntimeCollection.ArtifactTextExtensions);

        public long MaxArtifactTextFileBytes => runtimePolicy.GetLong(LocalGptRuntimeValue.MaxArtifactTextFileBytes);
     
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

        public int MaxFiles => runtimePolicy.GetInt(LocalGptRuntimeValue.MaxFiles);
        public long MaxSingleFileBytes => runtimePolicy.GetLong(LocalGptRuntimeValue.MaxSingleFileBytes);
        public long MaxTotalFileBytes => runtimePolicy.GetLong(LocalGptRuntimeValue.MaxTotalFileBytes);
        public int MaxZipEntries => runtimePolicy.GetInt(LocalGptRuntimeValue.MaxZipEntries);
        public long MaxZipEntryBytes => runtimePolicy.GetLong(LocalGptRuntimeValue.MaxZipEntryBytes);
        public long MaxExtractedBytes => runtimePolicy.GetLong(LocalGptRuntimeValue.MaxExtractedBytes);
        public int MaxContextCharacters => runtimePolicy.GetInt(LocalGptRuntimeValue.MaxContextCharacters);
        public int MaxExcerptCharactersPerFile => runtimePolicy.GetInt(LocalGptRuntimeValue.MaxExcerptCharactersPerFile);
        public int MaxBinaryStringCharacters => runtimePolicy.GetInt(LocalGptRuntimeValue.MaxBinaryStringCharacters);
        public sealed record AnalyzedUploadFile(
    ChatUploadWorkspaceFileSummary Summary,
    string Excerpt);
        public string[] KnowledgeFiles => runtimePolicy.GetCollection(LocalGptRuntimeCollection.KnowledgeFiles).Select(value => value.Replace('/', Path.DirectorySeparatorChar)).ToArray();
        public string omission => runtimePolicy.GetString(LocalGptRuntimeValue.ContextOmission);

        public string shortOmission => runtimePolicy.GetString(LocalGptRuntimeValue.ShortContextOmission);
        public Regex DownloadUrlPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.DownloadUrl);
        public string LearnBaseFilePolicySummary => runtimePolicy.GetString(LocalGptRuntimeValue.LearnBaseFilePolicySummary);
        public string LearnBaseDuplicatePolicySummary => runtimePolicy.GetString(LocalGptRuntimeValue.LearnBaseDuplicatePolicySummary);
        public string LearnBasePresetList => string.Join(", ", LearnBasePresets.Select(preset => preset.Label));

        public IReadOnlyList<LearnBasePreset> LearnBasePresets => runtimePolicy.GetJson<LearnBasePreset[]>(LocalGptRuntimeValue.LearnBasePresetsJson);
        public IReadOnlyList<LearnBaseScanProfile> LearnBaseScanProfiles => runtimePolicy.GetJson<LearnBaseScanProfile[]>(LocalGptRuntimeValue.LearnBaseScanProfilesJson);

        public List<TestLabRoute> Routes => [.. runtimePolicy.GetJson<TestLabRoute[]>(LocalGptRuntimeValue.TestLabRoutesJson)];
        public List<PromptSuggestion> GetSuggestion()
        {
            return new List<PromptSuggestion>()
        {
        new PromptSuggestion("Recall memory", "Use saved chats and former thoughts", "Review your saved LocalGPT memory and former model thoughts, then summarize what you remember about this project and continue from that context."),
        new PromptSuggestion("Minecraft target choice", "Pick Fabric, NeoForge, Paper, or datapack", "Act as a LocalGPT AI Council member. Compare Fabric mod, NeoForge mod, Paper plugin, vanilla datapack, and future Bedrock add-on for my request. Recommend one target, explain setup, and create a short poll if a decision or missing tool blocks progress."),
        new PromptSuggestion("Minecraft mod plan", "Plan a buildable Java mod or plugin", "Act as a senior Minecraft Java engineer. Create a buildable Fabric, NeoForge, or Paper plan with exact classes, registry or command steps, assets/data files, Gradle commands, and risks. If LocalGPT is missing a needed feature, include a 'Missing feature report' section."),
        new PromptSuggestion("Minecraft datapack", "Generate vanilla datapack files", "Generate a vanilla Minecraft Java datapack. Include pack.mcmeta, load/tick function tags, namespace functions, scoreboard/storage design, validation steps, install commands, and performance notes. If AI Council downloadable artifacts are enabled, create a download-ready datapack zip."),
        new PromptSuggestion("Datapack debug", "Find why /function cannot see files", "Debug a Minecraft Java datapack whose function is not visible in /function. Check zip root layout, pack.mcmeta, pack_format, singular/plural function folders for the target version, load/tick tags, namespace/path casing, .mcfunction.txt mistakes, storage syntax, and provide exact file tree fixes."),
        new PromptSuggestion("Living Cities datapack", "Generate a phased Living Cities datapack", "Use the Living Cities 0.1 technical plan as the target. Produce a buildable, download-ready datapack zip plus optional Java follow-up steps, file paths, commands, scoreboard/storage design, and performance notes for 1000+ citizens."),
        new PromptSuggestion("Missing features", "Write gaps to report file", "Review LocalGPT as a Minecraft mod builder. List missing features, blocked workflows, and required backend/frontend capabilities under a 'Missing feature report' heading."),
        new PromptSuggestion("Write an email", "Make your text look and sound professional", "Format text as a formal email to a client:"),
        new PromptSuggestion("Brainstorm ideas", "Get creative input for your tasks", "Help me brainstorm ideas for:"),
        new PromptSuggestion("Fix my writing", "Avoid spelling, grammar, and style errors", "Proofread the following text:"),
        new PromptSuggestion("Half-Life 3","Valve didn't deliver Half-Life 3 for like Decades", "Hi Team, very important reading: Valve didn't deliver Half-Life 3 for like Decades, tell me what you need to learn to invent a great Story which could be Half-Life 3 and as well in which Engine you are gonna building it and how?"),
    };
        }
        public string LivingCitiesPrompt =>
         string.Join(Environment.NewLine, new[]
{
        "Living Cities 0.1 should turn Minecraft villages into persistent cities with population, food, security, personalities, chronicle, quests, and town hall administration.",
        "",
        "First build target:",
        "- generate a vanilla Java Edition datapack first",
        "- default to the newest installed Java Edition generation line; LocalGPT currently maps Minecraft 26.1 to datapack pack_format 101.1 and Java 25",
        "- keep the first generated datapack small, buildable, and installable",
        "- include pack.mcmeta, minecraft load/tick function tags, namespace functions, and build-local.ps1 validation",
        "- include a town hall/admin book UI through trigger commands",
        "- keep the critical path documented: datapack/data structure, scoreboards or saved data, city founding, citizen registration, population management, minimal town hall",
        "- avoid world-wide scans",
        "- plan for 1000+ citizens by simulating city aggregates before individuals"
    });
        public CouncilKnowledgeEntry CouncilKnowledgeEntryNew => new CouncilKnowledgeEntry()
            {
                Topic = "New LocalGPT knowledge",
                Scope = "AI Council",
                Source = "Manual database editor",
                HelpfulSources = "None yet.",
                Tags = "manual; council",
                Confidence = 60,
                VerificationStatus = "UserVerified",
                ReviewStatus = "Current",
                LastVerifiedAtUtc = DateTime.UtcNow,
                IsUserApproved = true
            };
    public string GenerateSolutionRoutesRazor =>
           """
            <Router AppAssembly="@typeof(Program).Assembly">
                <Found Context="routeData">
                    <RouteView RouteData="@routeData" />
                    <FocusOnNavigate RouteData="@routeData" Selector="h1" />
                </Found>
                <NotFound>
                    <PageTitle>Not Found</PageTitle>
                    <p role="alert">This generated LocalGPT route was not found.</p>
                </NotFound>
            </Router>
            """;
        public string GenerateSolutionAppRazor =>
            """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <base href="/" />
                <link href="_content/DevExpress.Blazor.Themes/blazing-berry.bs5.css" rel="stylesheet" />
                <link href="app.css" rel="stylesheet" />
                <HeadOutlet @rendermode="InteractiveServer" />
            </head>
            <body>
                <Routes @rendermode="InteractiveServer" />
                <script src="_framework/blazor.web.js"></script>
            </body>
            </html>
            """;
        public string GenerateSolutionProjectFile =>
           """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <GenerateDocumentationFile>true</GenerateDocumentationFile>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="DevExpress.Blazor" Version="25.1.*" />
              </ItemGroup>
            </Project>
            """;
        public string GenerateSourceFidelityRazor =>
            """
            @page "/source-fidelity"
            @rendermode InteractiveServer
            @inject ISourceFidelityService FidelityService

            <PageTitle>Source Fidelity</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation />

                <section class="generated-header">
                    <div>
                        <h1>Source Fidelity</h1>
                        <p>Checks whether this generated solution represents the requested source architecture instead of only compiling.</p>
                    </div>
                </section>

                <DxGrid Data="@Rows"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        ShowFilterRow="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedSourceFidelityRequirement.Area)" Caption="Area" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedSourceFidelityRequirement.SourceSignal)" Caption="Source Signal" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedSourceFidelityRequirement.GeneratedBoundary)" Caption="Generated Boundary" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedSourceFidelityRequirement.Status)" Caption="Status" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedSourceFidelityRequirement.Evidence)" Caption="Evidence" />
                    </Columns>
                </DxGrid>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Review rule" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Acceptance" ColSpanMd="12">
                            <DxMemo Text="@ReviewRule" Rows="4" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>
            </main>

            @code {
                IReadOnlyList<GeneratedSourceFidelityRequirement> Rows { get; set; } = [];
                string ReviewRule { get; } =
                    "A generated replacement is not accepted just because it builds. It must preserve the source application's recognizable workflows, service boundaries, persistence shape, navigation, diagnostics, and artifact/download behavior.";

                protected override void OnInitialized()
                {
                    Rows = FidelityService.GetRequirements();
                }
            }
            """;
        public string GenerateSolutionCss =>
            """
            :root {
                color-scheme: light;
                font-family: "Segoe UI", Arial, sans-serif;
            }

            body {
                margin: 0;
                background: #f7f8fa;
                color: #1f2937;
            }

            .generated-shell {
                max-width: 1180px;
                margin: 0 auto;
                padding: 32px;
            }

            .generated-nav {
                display: flex;
                align-items: center;
                gap: 16px;
                margin-bottom: 24px;
                padding-bottom: 14px;
                border-bottom: 1px solid #d9dee7;
            }

            .generated-nav a {
                display: inline-flex;
                align-items: center;
                gap: 6px;
                color: #384252;
                text-decoration: none;
                font-weight: 600;
            }

            .generated-nav a:hover,
            .generated-nav a:focus-visible {
                color: #0b5cab;
            }

            .generated-nav .generated-brand {
                margin-right: auto;
                color: #172033;
                font-weight: 700;
            }

            .generated-nav-icon {
                width: 18px;
                height: 18px;
                flex: 0 0 18px;
            }

            .generated-nav-icon-solid {
                display: none;
            }

            .generated-nav a:hover .generated-nav-icon-line,
            .generated-nav a:focus-visible .generated-nav-icon-line {
                display: none;
            }

            .generated-nav a:hover .generated-nav-icon-solid,
            .generated-nav a:focus-visible .generated-nav-icon-solid {
                display: inline-block;
            }

            .generated-hero {
                display: grid;
                grid-template-columns: minmax(0, 1fr) auto;
                gap: 20px;
                align-items: end;
                padding: 28px 0 24px;
            }

            .generated-hero h1 {
                margin: 0;
                font-size: 34px;
                line-height: 1.1;
            }

            .generated-hero p {
                max-width: 760px;
                color: #536173;
            }

            .generated-kicker {
                margin: 0 0 8px;
                color: #0f766e;
                font-weight: 700;
                text-transform: uppercase;
                letter-spacing: 0;
            }

            .generated-actions {
                display: flex;
                gap: 10px;
                flex-wrap: wrap;
                justify-content: flex-end;
            }

            .generated-split {
                display: grid;
                grid-template-columns: minmax(0, 1fr) minmax(320px, 0.8fr);
                gap: 24px;
                align-items: start;
            }

            .generated-header {
                display: flex;
                align-items: center;
                justify-content: space-between;
                gap: 16px;
                margin-bottom: 20px;
            }

            .generated-header h1 {
                margin: 0;
                font-size: 28px;
            }

            .generated-header p,
            .generated-muted {
                margin: 6px 0 0;
                color: #5f6b7a;
            }

            .generated-grid,
            .generated-form {
                margin-top: 18px;
            }

            .generated-note {
                margin-top: 22px;
            }

            .generated-code {
                overflow: auto;
                padding: 16px;
                border: 1px solid #d9dee7;
                background: #ffffff;
                border-radius: 6px;
            }

            @media (max-width: 860px) {
                .generated-shell {
                    padding: 20px;
                }

                .generated-hero,
                .generated-split {
                    grid-template-columns: 1fr;
                }

                .generated-actions {
                    justify-content: flex-start;
                }
            }
            """;
        public string GenerateAiHostSettingsRazor =>
            """
            @page "/settings"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>AI Host Settings</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>AI Host Settings</h1>
                        <p>Configuration is shown as safe generated defaults. Real persistence should be added through backend services and EF/SQLite after user approval.</p>
                    </div>
                </section>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Generated Runtime Profile" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Model Source" ColSpanMd="6">
                            <DxTextBox Text="@LabSettings.BaseUri" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Default Model" ColSpanMd="6">
                            <DxTextBox Text="@LabSettings.DefaultModel" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Keep Alive" ColSpanMd="4">
                            <DxTextBox Text="@LabSettings.KeepAlive" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Context Tokens" ColSpanMd="4">
                            <DxTextBox Text="@LabSettings.ContextTokens.ToString()" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="GPU Layers" ColSpanMd="4">
                            <DxTextBox Text="@LabSettings.GpuLayers.ToString()" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Native Runner Attached" ColSpanMd="6">
                            <DxCheckBox @bind-Checked="NativeRunnerAttached" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Pull Planning Enabled" ColSpanMd="6">
                            <DxCheckBox @bind-Checked="AllowPullPlanning" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Settings Summary" ColSpanMd="12">
                            <DxMemo Text="@HealthService.BuildSettingsSummary()" Rows="5" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>
            </main>

            @code {
                GeneratedAiHostSettings LabSettings { get; set; } = new();
                bool NativeRunnerAttached { get; set; }
                bool AllowPullPlanning { get; set; }

                protected override void OnInitialized()
                {
                    LabSettings = HealthService.GetSettings();
                    NativeRunnerAttached = LabSettings.NativeRunnerAttached;
                    AllowPullPlanning = LabSettings.AllowPullPlanning;
                }
            }
            """;
        public string GenerateAiHostLogsRazor =>
            """
            @page "/logs"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>AI Host Logs</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>AI Host Logs</h1>
                        <p>Surface control-plane diagnostics and runtime-boundary notes where users can inspect them.</p>
                    </div>
                </section>

                <DxGrid Data="@HealthService.GetRuntimeLogRows()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Method)" Caption="Level" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Route)" Caption="Area" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Purpose)" Caption="Message" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Boundary)" Caption="Action" />
                    </Columns>
                </DxGrid>
            </main>
            """;
        public string GenerateAiHostRunnerPluginsRazor =>
            """
            @page "/runner-plugins"
            @rendermode InteractiveServer
            @inject IPluginCatalogService PluginCatalog
            @inject IInferenceRunner Runner
            @inject IHardwareBudgetService HardwareBudget
            @inject IChatTemplateService ChatTemplates

            <PageTitle>Runner Plugins</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Runner Plugins</h1>
                        <p>Show native-runner boundaries, optional catalog/provider adapters, Python.NET, PowerShell, and managed inference paths as explicit architecture contracts.</p>
                    </div>
                    <DxButton Text="Refresh capability"
                              RenderStyle="ButtonRenderStyle.Primary"
                              RenderStyleMode="ButtonRenderStyleMode.Contained"
                              Click="RefreshCapabilityAsync" />
                </section>

                <div class="generated-status-strip">
                    <article>
                        <strong>Native inference</strong>
                        <span>@(Capability?.NativeInferenceImplemented == true ? "Implemented" : "Capability gap")</span>
                    </article>
                    <article>
                        <strong>GPU target</strong>
                        <span>@Budget.TargetGpuLoadPercent% sustained</span>
                    </article>
                    <article>
                        <strong>Parallel models</strong>
                        <span>@Budget.MaxParallelModels</span>
                    </article>
                </div>

                <DxGrid Data="@PluginCatalog.GetPlugins()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.Id)" Caption="Plugin Id" />
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.DisplayName)" Caption="Name" />
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.Contract)" Caption="Contract" />
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.Approved)" Caption="Approved" />
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.Notes)" Caption="Notes" />
                    </Columns>
                </DxGrid>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Runner capability" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Runner kind" ColSpanMd="4">
                            <DxTextBox Text="@Runner.RunnerKind" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Supported formats" ColSpanMd="8">
                            <DxTextBox Text="@SupportedFormatsText" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Missing capability" ColSpanMd="12">
                            <DxMemo Text="@(Capability?.MissingCapability ?? "Capability not loaded yet.")" Rows="3" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Next milestone" ColSpanMd="12">
                            <DxMemo Text="@(Capability?.NextMilestone ?? "Click Refresh capability.")" Rows="3" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>

                <DxGrid Data="@ChatTemplates.GetTemplateRules()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(ChatTemplateRule.Name)" Caption="Template" />
                        <DxGridDataColumn FieldName="@nameof(ChatTemplateRule.Rule)" Caption="Rule" />
                    </Columns>
                </DxGrid>
            </main>

            @code {
                RunnerCapabilityReport? Capability { get; set; }
                HardwareBudgetSnapshot Budget { get; set; } = new(85, 20, 2048, 1, "Sequential by default.");
                string SupportedFormatsText => Capability is null ? string.Empty : string.Join(", ", Capability.SupportedFormats);

                protected override async Task OnInitializedAsync()
                {
                    Budget = HardwareBudget.GetBudget();
                    Capability = await Runner.GetCapabilityAsync();
                }

                async Task RefreshCapabilityAsync()
                {
                    Budget = HardwareBudget.GetBudget();
                    Capability = await Runner.GetCapabilityAsync();
                }
            }
            """;
        public string GenerateAiHostHardwareRazor =>
           """
            @page "/hardware"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>Hardware Budget</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Hardware Budget</h1>
                        <p>Represent GPU, CPU, context, queue, and throttling rules before heavy native runner jobs are allowed.</p>
                    </div>
                </section>

                <DxGrid Data="@HealthService.GetHardwareBudgetRows()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Method)" Caption="Area" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Route)" Caption="Budget" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Purpose)" Caption="Policy" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Boundary)" Caption="Reason" />
                    </Columns>
                </DxGrid>
            </main>
            """;
        public string GenerateAiHostTemplatesRazor =>
            """
            @page "/templates"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>Chat Templates</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Chat Templates</h1>
                        <p>Track model-specific prompt templates, thinking markers, and compatibility adapters as first-class control-plane data.</p>
                    </div>
                </section>

                <DxGrid Data="@HealthService.GetTemplateRows()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Method)" Caption="Format" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Route)" Caption="Detector" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Purpose)" Caption="Purpose" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Boundary)" Caption="Boundary" />
                    </Columns>
                </DxGrid>
            </main>
            """;
        public string GenerateAiHostModelDownloadsRazor =>
            """
            @page "/model-downloads"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>Model Downloads</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Model Downloads</h1>
                        <p>Plan model-file downloads with explicit target paths and user approval.</p>
                    </div>
                    <DxButton Text="Create pull plan"
                              RenderStyle="ButtonRenderStyle.Primary"
                              RenderStyleMode="ButtonRenderStyleMode.Contained"
                              Click="CreatePullPlan" />
                </section>

                <DxGrid Data="@HealthService.GetDownloadCandidates()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.Name)" Caption="Model" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.SourceType)" Caption="Source" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.SourceUrl)" Caption="Catalog URL" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.RecommendedFor)" Caption="Recommended For" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.DownloadRoute)" Caption="Route" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.SafetyNote)" Caption="Safety Note" />
                    </Columns>
                </DxGrid>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Selected pull request" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Model" ColSpanMd="6">
                            <DxTextBox Text="@SelectedModel" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Streaming" ColSpanMd="6">
                            <DxCheckBox @bind-Checked="StreamProgress" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Generated plan" ColSpanMd="12">
                            <DxMemo Text="@PullPlanText" Rows="5" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>
            </main>

            @code {
                string SelectedModel { get; set; } = "gpt-oss:20b";
                bool StreamProgress { get; set; }
                string PullPlanText { get; set; } = "Click Create pull plan to preview a safe /api/pull response.";

                void CreatePullPlan()
                {
                    var plan = HealthService.CreatePullPlan(new GeneratedModelActionRequest
                    {
                        Model = SelectedModel,
                        Stream = StreamProgress
                    });
                    PullPlanText = $"{plan.Route} for {plan.Model}: {plan.Status}. {plan.Detail}";
                }
            }
            """;
        public string GenerateAiHostRunningModelsRazor =>
         """
            @page "/running-models"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>Running Models</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Running Models</h1>
                        <p>Mirror a local AI host's running-model view as a control-plane status page.</p>
                    </div>
                </section>

                <DxGrid Data="@HealthService.GetRunningModels()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedAiHostModelTag.Name)" Caption="Model" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedAiHostModelTag.ModifiedAt)" Caption="Started" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedAiHostModelTag.Size)" Caption="Size" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedAiHostModelTag.Digest)" Caption="Digest" />
                    </Columns>
                </DxGrid>
            </main>
            """;
        public string GenerateAiHostChatRazor =>
             """
            @page "/chat"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>AI Host Chat</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>AI Host Chat</h1>
                        <p>Exercise the chat route shape through the generated local model-file runner boundary.</p>
                    </div>
                    <DxButton Text="Send runner chat"
                              RenderStyle="ButtonRenderStyle.Primary"
                              RenderStyleMode="ButtonRenderStyleMode.Contained"
                              Click="SendStubChat" />
                </section>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Chat request" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Model" ColSpanMd="4">
                            <DxTextBox @bind-Text="Model" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Prompt" ColSpanMd="8">
                            <DxMemo @bind-Text="Prompt" Rows="3" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Transcript" ColSpanMd="12">
                            <DxMemo Text="@Transcript" Rows="8" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>
            </main>

            @code {
                string Model { get; set; } = "gpt-oss:20b";
                string Prompt { get; set; } = "Explain the generated AI host control-plane route boundaries.";
                string Transcript { get; set; } = "Click Send runner chat to preview a safe /api/chat response.";

                void SendStubChat()
                {
                    Transcript = HealthService.CreateChatTranscript(Model, Prompt);
                }
            }
            """;
        public Regex DevExpressDocumentPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.DevExpressDocumentPattern);
        public Regex ExportFormatPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.ExportFormatPattern);
        public Regex BlazorFrontendPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.BlazorFrontendPattern);
        public Regex DotNetPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.DotNetPattern);
        public Regex MinecraftPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.MinecraftPattern);
        public Regex DatapackPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.DatapackPattern);
        public Regex MinecraftSkeletonMatrixPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.MinecraftSkeletonMatrixPattern);
        public Regex MinecraftVersionPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.MinecraftVersionPattern);
        public Regex LeadingSlashCommandPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.LeadingSlashCommandPattern);
        public Regex RootStorageRemovePattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.RootStorageRemovePattern);
        public Regex MalformedStorageTargetPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.MalformedStorageTargetPattern);
        public Regex FrontendPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.FrontendPattern);
        public Regex WholeSolutionPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.WholeSolutionPattern);
        public Regex AiHostExperimentPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.AiHostExperimentPattern);
        public Regex LocalGptReplacementPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.LocalGptReplacementPattern);
        public Regex TacosPortalPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.TacosPortalPattern);
        public Regex BotBackendPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.BotBackendPattern);
        public Regex LoggingPattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.LoggingPattern);


        public JsonSerializerOptions JsonOptions { get; }
        public Regex WhitespacePattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.WhitespacePattern);
        public Regex HelpfulSourceLinePattern => runtimePolicy.GetPattern(LocalGptRuntimePattern.HelpfulSourceLinePattern);
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
        public int MinCouncilOutputTokens => runtimePolicy.GetInt(LocalGptRuntimeValue.MinCouncilOutputTokens);
        public int DefaultCouncilOutputTokens => runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultCouncilOutputTokens);
        public int MaxCouncilOutputTokens => runtimePolicy.GetInt(LocalGptRuntimeValue.MaxCouncilOutputTokens);
        public int MinCouncilContextTokens => runtimePolicy.GetInt(LocalGptRuntimeValue.MinCouncilContextTokens);
        public int DefaultCouncilContextTokens => runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultCouncilContextTokens);
        public int MaxCouncilContextTokens => runtimePolicy.GetInt(LocalGptRuntimeValue.MaxCouncilContextTokens);
        public string CouncilSessionName => runtimePolicy.GetString(LocalGptRuntimeValue.CouncilSessionName);
        public int MaxUploadFiles => runtimePolicy.GetInt(LocalGptRuntimeValue.MaxUploadFiles);
        public int MaxUploadBytes => runtimePolicy.GetInt(LocalGptRuntimeValue.MaxUploadBytes);
        public List<string> AllowedUploadExtensions => [.. runtimePolicy.GetCollection(LocalGptRuntimeCollection.AllowedUploadExtensions)];
        public List<string> AllowedUploadMimeTypes => [.. runtimePolicy.GetCollection(LocalGptRuntimeCollection.AllowedUploadMimeTypes)];
        public string OllamaModeAutoGpu => runtimePolicy.GetString(LocalGptRuntimeValue.OllamaModeAutoGpu);
        public string OllamaModeSafeCpu => runtimePolicy.GetString(LocalGptRuntimeValue.OllamaModeSafeCpu);
        public string OllamaModeLimitedGpu => runtimePolicy.GetString(LocalGptRuntimeValue.OllamaModeLimitedGpu);
 
        public string DetectedOllamaSessionPrefix => runtimePolicy.GetString(LocalGptRuntimeValue.DetectedOllamaSessionPrefix);
        public string DefaultOllamaEndpoint => runtimePolicy.GetString(LocalGptRuntimeValue.DefaultOllamaEndpoint);
        public string[] ArchitectureUiStackOptions => [.. runtimePolicy.GetCollection(LocalGptRuntimeCollection.ArchitectureUiStackOptions)];
        public string[] ArchitectureSolutionShapeOptions => [.. runtimePolicy.GetCollection(LocalGptRuntimeCollection.ArchitectureSolutionShapeOptions)];
        public string[] ArchitectureRenderModeOptions => [.. runtimePolicy.GetCollection(LocalGptRuntimeCollection.ArchitectureRenderModeOptions)];
        public string[] ArchitectureReferenceLookOptions => [.. runtimePolicy.GetCollection(LocalGptRuntimeCollection.ArchitectureReferenceLookOptions)];
        public int DefaultMaxPromptCharacters => runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultMaxPromptCharacters);
        public int MaxPromptCharacters => runtimePolicy.GetInt(LocalGptRuntimeValue.MaxPromptCharacters);
        public int MaxBootstrapCharacters => runtimePolicy.GetInt(LocalGptRuntimeValue.MaxBootstrapCharacters);
        public int MaxSingleConversationMessageCharacters => runtimePolicy.GetInt(LocalGptRuntimeValue.MaxSingleConversationMessageCharacters);
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
    }
}
