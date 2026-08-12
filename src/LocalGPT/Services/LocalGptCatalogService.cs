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
    /// <summary>
    /// Provides local gpt catalog service operations.
    /// </summary>
    public sealed class LocalGptCatalogService(
        ILocalGptRuntimePolicyDataService runtimePolicy,
        ILogger<LocalGptCatalogService> logger)
    {
        private readonly ILocalGptRuntimePolicyDataService _runtimePolicy =
            runtimePolicy ?? throw new ArgumentNullException(nameof(runtimePolicy));
        private readonly ILogger<LocalGptCatalogService> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// Gets or sets default gradle version.
        /// </summary>
        public string DefaultGradleVersion => _runtimePolicy.GetString(LocalGptRuntimeValue.DefaultGradleVersion);
        /// <summary>
        /// Gets or sets utf8 no bom.
        /// </summary>
        public Encoding Utf8NoBom { get; } =
            /// <summary>
            /// Runs the utf8 encoding operation.
            /// </summary>
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        /// <summary>
        /// Gets or sets name cleaner.
        /// </summary>
        public Regex NameCleaner => _runtimePolicy.GetPattern(LocalGptRuntimePattern.NameCleaner);
        /// <summary>
        /// Gets or sets mod identifier cleaner.
        /// </summary>
        public Regex ModIdCleaner => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ModIdCleaner);
        /// <summary>
        /// Gets or sets package part cleaner.
        /// </summary>
        public Regex PackagePartCleaner => _runtimePolicy.GetPattern(LocalGptRuntimePattern.PackagePartCleaner);

        /// <summary>
        /// Gets or sets default minecraft version.
        /// </summary>
        public string DefaultMinecraftVersion => _runtimePolicy.GetString(LocalGptRuntimeValue.DefaultMinecraftVersion);

        /// <summary>
        /// Gets or sets default java version.
        /// </summary>
        public string DefaultJavaVersion => _runtimePolicy.GetString(LocalGptRuntimeValue.DefaultJavaVersion);
        /// <summary>
        /// Gets or sets fabric loader version.
        /// </summary>
        public string FabricLoaderVersion => _runtimePolicy.GetString(LocalGptRuntimeValue.FabricLoaderVersion);




        /// <summary>
        /// Gets or sets max DevExpress ai chat prompt characters.
        /// </summary>
        public int MaxDxAiChatPromptCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxDxAiChatPromptCharacters);
        /// <summary>
        /// Gets or sets max visible prompt characters.
        /// </summary>
        public int MaxVisiblePromptCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxVisiblePromptCharacters);
        /// <summary>
        /// Gets or sets missing feature pattern.
        /// </summary>
        public Regex MissingFeaturePattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.MissingFeaturePattern);
        /// <summary>
        /// Gets or sets capability gap block pattern.
        /// </summary>
        public Regex CapabilityGapBlockPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.CapabilityGapBlockPattern);
        /// <summary>
        /// Gets or sets truncated tail pattern.
        /// </summary>
        public Regex TruncatedTailPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.TruncatedTailPattern);
        /// <summary>
        /// Gets or sets thinking block pattern.
        /// </summary>
        public Regex ThinkingBlockPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ThinkingBlockPattern);
        /// <summary>
        /// Gets or sets council prompt fence pattern.
        /// </summary>
        public Regex CouncilPromptFencePattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.CouncilPromptFencePattern);
        /// <summary>
        /// Gets or sets council request block pattern.
        /// </summary>
        public Regex CouncilRequestBlockPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.CouncilRequestBlockPattern);

        /// <summary>
        /// Gets or sets debug extensions.
        /// </summary>
        public FrozenSet<string> DebugExtensions => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.DebugExtensions);
        /// <summary>
        /// Gets or sets text extensions.
        /// </summary>
        public FrozenSet<string> TextExtensions => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.TextExtensions);
        /// <summary>
        /// Gets or sets learn base known extensions.
        /// </summary>
        public FrozenSet<string> LearnBaseKnownExtensions => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.LearnBaseKnownExtensions);

        /// <summary>
        /// Gets or sets binary diagnostic extensions.
        /// </summary>
        public FrozenSet<string> BinaryDiagnosticExtensions => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.BinaryDiagnosticExtensions);
        /// <summary>
        /// Gets or sets target framework pattern.
        /// </summary>
        public Regex TargetFrameworkPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.TargetFrameworkPattern);
        /// <summary>
        /// Gets or sets package reference pattern.
        /// </summary>
        public Regex PackageReferencePattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.PackageReferencePattern);
        /// <summary>
        /// Gets or sets sensitive name pattern.
        /// </summary>
        public Regex SensitiveNamePattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.SensitiveNamePattern);
        /// <summary>
        /// Gets or sets excluded directory names.
        /// </summary>
        public FrozenSet<string> ExcludedDirectoryNames => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.ExcludedDirectoryNames);

        /// <summary>
        /// Gets or sets binary extensions.
        /// </summary>
        public FrozenSet<string> BinaryExtensions => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.BinaryExtensions);

        /// <summary>
        /// Gets or sets source extensions.
        /// </summary>
        public FrozenSet<string> SourceExtensions => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.SourceExtensions);
        /// <summary>
        /// Gets or sets default ollama URI.
        /// </summary>
        public string DefaultOllamaUri => _runtimePolicy.GetString(LocalGptRuntimeValue.DefaultOllamaUri);
        /// <summary>
        /// Gets or sets max participants.
        /// </summary>
        public int MaxParticipants => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxParticipants);
        /// <summary>
        /// Gets or sets default max parallel models.
        /// </summary>
        public int DefaultMaxParallelModels => _runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultMaxParallelModels);
        /// <summary>
        /// Gets or sets default heavy model gpu layers.
        /// </summary>
        public int DefaultHeavyModelGpuLayers => _runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultHeavyModelGpuLayers);
        /// <summary>
        /// Gets or sets min context tokens.
        /// </summary>
        public int MinContextTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.MinContextTokens);
        /// <summary>
        /// Gets or sets default context tokens.
        /// </summary>
        public int DefaultContextTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultContextTokens);
        /// <summary>
        /// Gets or sets max context tokens.
        /// </summary>
        public int MaxContextTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxContextTokens);
        /// <summary>
        /// Gets or sets min output tokens.
        /// </summary>
        public int MinOutputTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.MinOutputTokens);
        /// <summary>
        /// Gets or sets max output tokens.
        /// </summary>
        public int MaxOutputTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxOutputTokens);
        /// <summary>
        /// Gets or sets stream status pattern.
        /// </summary>
        public Regex StreamStatusPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.StreamStatusPattern);
        /// <summary>
        /// Gets or sets word pattern.
        /// </summary>
        public Regex WordPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.WordPattern);
        /// <summary>
        /// Gets or sets development request pattern.
        /// </summary>
        public Regex DevelopmentRequestPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.DevelopmentRequestPattern);
        /// <summary>
        /// Gets or sets explicit artifact intent pattern.
        /// </summary>
        public Regex ExplicitArtifactIntentPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ExplicitArtifactIntentPattern);
        /// <summary>
        /// Gets or sets advice only prompt pattern.
        /// </summary>
        public Regex AdviceOnlyPromptPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.AdviceOnlyPromptPattern);
        /// <summary>
        /// Gets or sets explicit artifact creation command pattern.
        /// </summary>
        public Regex ExplicitArtifactCreationCommandPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ExplicitArtifactCreationCommandPattern);
        /// <summary>
        /// Gets or sets concrete minecraft artifact pattern.
        /// </summary>
        public Regex ConcreteMinecraftArtifactPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ConcreteMinecraftArtifactPattern);
        /// <summary>
        /// Gets or sets concrete dot net artifact pattern.
        /// </summary>
        public Regex ConcreteDotNetArtifactPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ConcreteDotNetArtifactPattern);
        /// <summary>
        /// Gets or sets ai host setup pattern.
        /// </summary>
        public Regex AiHostSetupPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.AiHostSetupPattern);
        /// <summary>
        /// Gets or sets implementation decision pattern.
        /// </summary>
        public Regex ImplementationDecisionPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ImplementationDecisionPattern);
        /// <summary>
        /// Gets or sets implementation choice pattern.
        /// </summary>
        public Regex ImplementationChoicePattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ImplementationChoicePattern);
        /// <summary>
        /// Gets or sets blocking artifact decision pattern.
        /// </summary>
        public Regex BlockingArtifactDecisionPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.BlockingArtifactDecisionPattern);
        /// <summary>
        /// Gets or sets safe sandbox consent pattern.
        /// </summary>
        public Regex SafeSandboxConsentPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.SafeSandboxConsentPattern);
        /// <summary>
        /// Gets or sets explicit do not generate until user decision pattern.
        /// </summary>
        public Regex ExplicitDoNotGenerateUntilUserDecisionPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ExplicitDoNotGenerateUntilUserDecisionPattern);
        /// <summary>
        /// Gets or sets developer execution intent pattern.
        /// </summary>
        public Regex DeveloperExecutionIntentPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.DeveloperExecutionIntentPattern);

 


        /// <summary>
        /// Gets or sets dev express import pattern.
        /// </summary>
        public Regex DevExpressImportPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.DevExpressImportPattern);
        /// <summary>
        /// Gets or sets dev express registration pattern.
        /// </summary>
        public Regex DevExpressRegistrationPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.DevExpressRegistrationPattern);
        /// <summary>
        /// Gets or sets artifact text extensions.
        /// </summary>
        public FrozenSet<string> ArtifactTextExtensions => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.ArtifactTextExtensions);

        /// <summary>
        /// Gets or sets max artifact text file bytes.
        /// </summary>
        public long MaxArtifactTextFileBytes => _runtimePolicy.GetLong(LocalGptRuntimeValue.MaxArtifactTextFileBytes);
     


        /// <summary>
        /// Gets or sets max files.
        /// </summary>
        public int MaxFiles => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxFiles);
        /// <summary>
        /// Gets or sets max single file bytes.
        /// </summary>
        public long MaxSingleFileBytes => _runtimePolicy.GetLong(LocalGptRuntimeValue.MaxSingleFileBytes);
        /// <summary>
        /// Gets or sets max total file bytes.
        /// </summary>
        public long MaxTotalFileBytes => _runtimePolicy.GetLong(LocalGptRuntimeValue.MaxTotalFileBytes);
        /// <summary>
        /// Gets or sets max zip entries.
        /// </summary>
        public int MaxZipEntries => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxZipEntries);
        /// <summary>
        /// Gets or sets max zip entry bytes.
        /// </summary>
        public long MaxZipEntryBytes => _runtimePolicy.GetLong(LocalGptRuntimeValue.MaxZipEntryBytes);
        /// <summary>
        /// Gets or sets max extracted bytes.
        /// </summary>
        public long MaxExtractedBytes => _runtimePolicy.GetLong(LocalGptRuntimeValue.MaxExtractedBytes);
        /// <summary>
        /// Gets or sets max context characters.
        /// </summary>
        public int MaxContextCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxContextCharacters);
        /// <summary>
        /// Gets or sets max excerpt characters per file.
        /// </summary>
        public int MaxExcerptCharactersPerFile => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxExcerptCharactersPerFile);
        /// <summary>
        /// Gets or sets max binary string characters.
        /// </summary>
        public int MaxBinaryStringCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxBinaryStringCharacters);
        /// <summary>
        /// Gets or sets knowledge files.
        /// </summary>
        public string[] KnowledgeFiles => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.KnowledgeFiles).Select(value => value.Replace('/', Path.DirectorySeparatorChar)).ToArray();
        /// <summary>
        /// Gets or sets omission.
        /// </summary>
        public string omission => _runtimePolicy.GetString(LocalGptRuntimeValue.ContextOmission);

        /// <summary>
        /// Gets or sets short omission.
        /// </summary>
        public string shortOmission => _runtimePolicy.GetString(LocalGptRuntimeValue.ShortContextOmission);
        /// <summary>
        /// Gets or sets download URL pattern.
        /// </summary>
        public Regex DownloadUrlPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.DownloadUrl);
        /// <summary>
        /// Gets or sets learn base file policy summary.
        /// </summary>
        public string LearnBaseFilePolicySummary => _runtimePolicy.GetString(LocalGptRuntimeValue.LearnBaseFilePolicySummary);
        /// <summary>
        /// Gets or sets learn base duplicate policy summary.
        /// </summary>
        public string LearnBaseDuplicatePolicySummary => _runtimePolicy.GetString(LocalGptRuntimeValue.LearnBaseDuplicatePolicySummary);
        /// <summary>
        /// Gets or sets learn base preset list.
        /// </summary>
        public string LearnBasePresetList => string.Join(", ", LearnBasePresets.Select(preset => preset.Label));

        /// <summary>
        /// Gets or sets learn base presets.
        /// </summary>
        public IReadOnlyList<LearnBasePreset> LearnBasePresets => _runtimePolicy.GetJson<LearnBasePreset[]>(LocalGptRuntimeValue.LearnBasePresetsJson);
        /// <summary>
        /// Gets or sets learn base scan profiles.
        /// </summary>
        public IReadOnlyList<LearnBaseScanProfile> LearnBaseScanProfiles => _runtimePolicy.GetJson<LearnBaseScanProfile[]>(LocalGptRuntimeValue.LearnBaseScanProfilesJson);

        /// <summary>
        /// Gets or sets routes.
        /// </summary>
        public List<TestLabRoute> Routes => [.. _runtimePolicy.GetJson<TestLabRoute[]>(LocalGptRuntimeValue.TestLabRoutesJson)];
        /// <summary>Creates the maintained generic and Council-team prompt suggestion catalog.</summary>
        /// <returns>Prompt suggestions with stable keys and optional direct-Council ownership.</returns>
        public List<PromptSuggestion> GetSuggestion()
        {
    try
    {
                _logger.LogTrace("Creating the LocalGPT prompt suggestion catalog.");
                return
                [
                    new("Recall memory", "Use saved chats and former thoughts", "Review your saved LocalGPT memory and former model thoughts, then summarize what you remember about this project and continue from that context."),
                    new("Council starter: project work", "Start the general Organic Project Team", "Start a fresh Organic Project Team Council run. Ask me for the exact project goal, current state, repository or workspace, compiler evidence and approval boundaries, then execute the maintained preparation, architecture, implementation, verification and consensus workflow.", "general-project-council-start", ["general-project"], true),
                    new("Council starter: benchmark models", "Compare installed Ollama models", "Start a fresh adaptive model benchmark Council run. Discover the currently installed Ollama models, ask me which models and workload categories to include, then run bounded comparable tasks. Separate measured speed, deterministic build or test results, code-curator ratings and user judgement. Do not silently overwrite approved presets.", "benchmark-council-start", ["adaptive-model-benchmark"], true),
                    new("Council starter: GameDirector", "Create a governed game session", "Start a fresh GameDirector Runtime Council run. Ask me for the game concept, player objective, creature and reactive-object families, map rules and preferred low-B controller models. Keep the GameDirector authoritative: every player, creature and map-object move is only a proposal until validated and applied by the director.", "game-director-council-start", ["game-director-runtime", "ascii-doom-council-adventure", "green-dragon-runtime-story", "kernel-creature-tournament"], true),
                    new("Council starter: modern C# host", "Build clean hosted .NET architecture", "Start a fresh Modern C# Host Development Team Council run. Ask for the workspace, target runtime, current solution and acceptance criteria. Follow the LocalGPT PowerShell build order: preflight and regex evidence, hosted architecture, bounded implementation, policy checks, build and tests, independent code-curator review, then release and changelog synthesis.", "csharp-host-council-start", ["csharp-modern-host-development"], true),
                    new("Council starter: PowerShell build", "Improve scripts and build policy", "Start a fresh PowerShell Build-System Development Team Council run. Inspect the requested scripts, repository policies, strict-mode behavior, idempotency, logging and exit-code contracts. Produce a bounded patch, execute available static checks, and finish with curator review and reproducible verification commands.", "powershell-build-council-start", ["powershell-build-development"], true),
                    new("Council starter: Java host", "Build Maven or Gradle services", "Start a fresh Java Hosted Application Development Team Council run. Ask for Java version, Maven or Gradle, framework, module structure and deployment target. Plan a modern hosted application, implement within the workspace policy, verify compilation and tests, and perform independent architecture and security review.", "java-hosted-council-start", ["java-hosted-development"], true),
                    new("Council starter: Minecraft", "Build a mod, plugin, datapack or add-on", "Start a fresh Minecraft Development Team Council run. First ask whether the target is Fabric, NeoForge, Paper, vanilla datapack or Bedrock add-on and which game version applies. Then assign Java, data-pack, asset, command and verification roles and produce a buildable, testable project plan without inventing unavailable tools.", "minecraft-development-council-start", ["minecraft-development"], true),
                    new("Council starter: ESP32 / Arduino", "Plan pins, wiring and firmware", "Start a fresh ESP32 / Arduino Wiring Council run. Ask for the exact board, sensors, voltage, pin layout and return transport. Produce a reviewed GPIO map, electrical warnings, transport-neutral telemetry contract, small firmware plan and learning-round checklist before any compile or flash action.", "embedded-wiring-council-start", ["embedded-firmware-wiring"], true),
                    new("Minecraft target choice", "Pick Fabric, NeoForge, Paper, or datapack", "Act as a LocalGPT AI Council member. Compare Fabric mod, NeoForge mod, Paper plugin, vanilla datapack, and future Bedrock add-on for my request. Recommend one target, explain setup, and create a short poll if a decision or missing tool blocks progress.", "minecraft-target-choice", ["minecraft-development"], false),
                    new("Minecraft mod plan", "Plan a buildable Java mod or plugin", "Act as a senior Minecraft Java engineer. Create a buildable Fabric, NeoForge, or Paper plan with exact classes, registry or command steps, assets/data files, Gradle commands, and risks. If LocalGPT is missing a needed feature, include a 'Missing feature report' section.", "minecraft-mod-plan", ["minecraft-development"], false),
                    new("Minecraft datapack", "Generate vanilla datapack files", "Generate a vanilla Minecraft Java datapack. Include pack.mcmeta, load/tick function tags, namespace functions, scoreboard/storage design, validation steps, install commands, and performance notes. If AI Council downloadable artifacts are enabled, create a download-ready datapack zip.", "minecraft-datapack", ["minecraft-development"], false),
                    new("Datapack debug", "Find why /function cannot see files", "Debug a Minecraft Java datapack whose function is not visible in /function. Check zip root layout, pack.mcmeta, pack_format, singular/plural function folders for the target version, load/tick tags, namespace/path casing, .mcfunction.txt mistakes, storage syntax, and provide exact file tree fixes.", "minecraft-datapack-debug", ["minecraft-development"], false),
                    new("Living Cities datapack", "Generate a phased Living Cities datapack", "Use the Living Cities 0.1 technical plan as the target. Produce a buildable, download-ready datapack zip plus optional Java follow-up steps, file paths, commands, scoreboard/storage design, and performance notes for 1000+ citizens.", "living-cities-datapack", ["minecraft-development"], false),
                    new("Missing features", "Write gaps to report file", "Review LocalGPT as a Minecraft mod builder. List missing features, blocked workflows, and required backend/frontend capabilities under a 'Missing feature report' heading.", "minecraft-missing-features", ["minecraft-development"], false),
                    new("Write an email", "Make your text look and sound professional", "Format text as a formal email to a client:"),
                    new("Brainstorm ideas", "Get creative input for your tasks", "Help me brainstorm ideas for:"),
                    new("Fix my writing", "Avoid spelling, grammar, and style errors", "Proofread the following text:"),
                    new("Lost sci-fi sequel", "Imagine the long-awaited continuation of an original science-fiction series", "Hi Team, invent an original science-fiction sequel for a fictional series that has been absent for decades. Explain what you need to learn, how the story should evolve, which engine could support it, and how you would build a convincing prototype without copying an existing franchise.")
                ];
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptCatalogService)}.{nameof(GetSuggestion)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptCatalogService)}.{nameof(GetSuggestion)} failed.");
        throw;
    }
}
        /// <summary>
        /// Gets or sets living cities prompt.
        /// </summary>
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
        /// <summary>
        /// Gets or sets council knowledge entry new.
        /// </summary>
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
    /// <summary>
    /// Gets or sets generate solution routes razor.
    /// </summary>
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
        /// <summary>
        /// Gets or sets generate solution app razor.
        /// </summary>
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
        /// <summary>
        /// Gets or sets generate solution project file.
        /// </summary>
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
        /// <summary>
        /// Gets or sets generate source fidelity razor.
        /// </summary>
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
        /// <summary>
        /// Gets or sets generate solution CSS.
        /// </summary>
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
        /// <summary>
        /// Gets or sets generate ai host settings razor.
        /// </summary>
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
        /// <summary>
        /// Gets or sets generate ai host logs razor.
        /// </summary>
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
        /// <summary>
        /// Gets or sets generate ai host runner plugins razor.
        /// </summary>
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
                        <strong>Parallel models per AI host</strong>
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
        /// <summary>
        /// Gets or sets generate ai host hardware razor.
        /// </summary>
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
        /// <summary>
        /// Gets or sets generate ai host templates razor.
        /// </summary>
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
        /// <summary>
        /// Gets or sets generate ai host model downloads razor.
        /// </summary>
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
        /// <summary>
        /// Gets or sets generate ai host running models razor.
        /// </summary>
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
        /// <summary>
        /// Gets or sets generate ai host chat razor.
        /// </summary>
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
        /// <summary>
        /// Gets or sets dev express document pattern.
        /// </summary>
        public Regex DevExpressDocumentPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.DevExpressDocumentPattern);
        /// <summary>
        /// Gets or sets export format pattern.
        /// </summary>
        public Regex ExportFormatPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ExportFormatPattern);
        /// <summary>
        /// Gets or sets blazor frontend pattern.
        /// </summary>
        public Regex BlazorFrontendPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.BlazorFrontendPattern);
        /// <summary>
        /// Gets or sets dot net pattern.
        /// </summary>
        public Regex DotNetPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.DotNetPattern);
        /// <summary>
        /// Gets or sets minecraft pattern.
        /// </summary>
        public Regex MinecraftPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.MinecraftPattern);
        /// <summary>
        /// Gets or sets datapack pattern.
        /// </summary>
        public Regex DatapackPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.DatapackPattern);
        /// <summary>
        /// Gets or sets minecraft skeleton matrix pattern.
        /// </summary>
        public Regex MinecraftSkeletonMatrixPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.MinecraftSkeletonMatrixPattern);
        /// <summary>
        /// Gets or sets minecraft version pattern.
        /// </summary>
        public Regex MinecraftVersionPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.MinecraftVersionPattern);
        /// <summary>
        /// Gets or sets leading slash command pattern.
        /// </summary>
        public Regex LeadingSlashCommandPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.LeadingSlashCommandPattern);
        /// <summary>
        /// Gets or sets root storage remove pattern.
        /// </summary>
        public Regex RootStorageRemovePattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.RootStorageRemovePattern);
        /// <summary>
        /// Gets or sets malformed storage target pattern.
        /// </summary>
        public Regex MalformedStorageTargetPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.MalformedStorageTargetPattern);
        /// <summary>
        /// Gets or sets frontend pattern.
        /// </summary>
        public Regex FrontendPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.FrontendPattern);
        /// <summary>
        /// Gets or sets whole solution pattern.
        /// </summary>
        public Regex WholeSolutionPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.WholeSolutionPattern);
        /// <summary>
        /// Gets or sets ai host experiment pattern.
        /// </summary>
        public Regex AiHostExperimentPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.AiHostExperimentPattern);
        /// <summary>
        /// Gets or sets local gpt replacement pattern.
        /// </summary>
        public Regex LocalGptReplacementPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.LocalGptReplacementPattern);
        /// <summary>
        /// Gets or sets tacos portal pattern.
        /// </summary>
        public Regex TacosPortalPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.TacosPortalPattern);
        /// <summary>
        /// Gets or sets bot backend pattern.
        /// </summary>
        public Regex BotBackendPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.BotBackendPattern);
        /// <summary>
        /// Gets or sets logging pattern.
        /// </summary>
        public Regex LoggingPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.LoggingPattern);


        /// <summary>
        /// Gets or sets JSON options.
        /// </summary>
        public JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        /// <summary>
        /// Gets or sets whitespace pattern.
        /// </summary>
        public Regex WhitespacePattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.WhitespacePattern);
        /// <summary>
        /// Gets or sets helpful source line pattern.
        /// </summary>
        public Regex HelpfulSourceLinePattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.HelpfulSourceLinePattern);

        /// <summary>
        /// Gets or sets min council output tokens.
        /// </summary>
        public int MinCouncilOutputTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.MinCouncilOutputTokens);
        /// <summary>
        /// Gets or sets default council output tokens.
        /// </summary>
        public int DefaultCouncilOutputTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultCouncilOutputTokens);
        /// <summary>
        /// Gets or sets max council output tokens.
        /// </summary>
        public int MaxCouncilOutputTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxCouncilOutputTokens);
        /// <summary>
        /// Gets or sets min council context tokens.
        /// </summary>
        public int MinCouncilContextTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.MinCouncilContextTokens);
        /// <summary>
        /// Gets or sets default council context tokens.
        /// </summary>
        public int DefaultCouncilContextTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultCouncilContextTokens);
        /// <summary>
        /// Gets or sets max council context tokens.
        /// </summary>
        public int MaxCouncilContextTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxCouncilContextTokens);
        /// <summary>
        /// Gets or sets council session name.
        /// </summary>
        public string CouncilSessionName => _runtimePolicy.GetString(LocalGptRuntimeValue.CouncilSessionName);
        /// <summary>
        /// Gets or sets max upload files.
        /// </summary>
        public int MaxUploadFiles => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxUploadFiles);
        /// <summary>
        /// Gets or sets max upload bytes.
        /// </summary>
        public int MaxUploadBytes => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxUploadBytes);
        /// <summary>
        /// Gets or sets allowed upload extensions.
        /// </summary>
        public List<string> AllowedUploadExtensions => [.. _runtimePolicy.GetCollection(LocalGptRuntimeCollection.AllowedUploadExtensions)];
        /// <summary>
        /// Gets or sets allowed upload mime types.
        /// </summary>
        public List<string> AllowedUploadMimeTypes => [.. _runtimePolicy.GetCollection(LocalGptRuntimeCollection.AllowedUploadMimeTypes)];
        /// <summary>
        /// Gets or sets ollama mode auto gpu.
        /// </summary>
        public string OllamaModeAutoGpu => _runtimePolicy.GetString(LocalGptRuntimeValue.OllamaModeAutoGpu);
        /// <summary>
        /// Gets or sets ollama mode safe cpu.
        /// </summary>
        public string OllamaModeSafeCpu => _runtimePolicy.GetString(LocalGptRuntimeValue.OllamaModeSafeCpu);
        /// <summary>
        /// Gets or sets ollama mode limited gpu.
        /// </summary>
        public string OllamaModeLimitedGpu => _runtimePolicy.GetString(LocalGptRuntimeValue.OllamaModeLimitedGpu);
 
        /// <summary>
        /// Gets or sets detected ollama session prefix.
        /// </summary>
        public string DetectedOllamaSessionPrefix => _runtimePolicy.GetString(LocalGptRuntimeValue.DetectedOllamaSessionPrefix);
        /// <summary>
        /// Gets or sets default ollama endpoint.
        /// </summary>
        public string DefaultOllamaEndpoint => _runtimePolicy.GetString(LocalGptRuntimeValue.DefaultOllamaEndpoint);
        /// <summary>
        /// Gets the database-provisioned language and toolchain choices for architecture guidance.
        /// </summary>
        public string[] ArchitectureLanguageToolchainOptions => [.. _runtimePolicy.GetCollection(LocalGptRuntimeCollection.ArchitectureLanguageToolchainOptions)];
        /// <summary>
        /// Gets or sets architecture UI stack options.
        /// </summary>
        public string[] ArchitectureUiStackOptions => [.. _runtimePolicy.GetCollection(LocalGptRuntimeCollection.ArchitectureUiStackOptions)];
        /// <summary>
        /// Gets or sets architecture solution shape options.
        /// </summary>
        public string[] ArchitectureSolutionShapeOptions => [.. _runtimePolicy.GetCollection(LocalGptRuntimeCollection.ArchitectureSolutionShapeOptions)];
        /// <summary>
        /// Gets or sets architecture render mode options.
        /// </summary>
        public string[] ArchitectureRenderModeOptions => [.. _runtimePolicy.GetCollection(LocalGptRuntimeCollection.ArchitectureRenderModeOptions)];
        /// <summary>
        /// Gets or sets architecture reference look options.
        /// </summary>
        public string[] ArchitectureReferenceLookOptions => [.. _runtimePolicy.GetCollection(LocalGptRuntimeCollection.ArchitectureReferenceLookOptions)];
        /// <summary>
        /// Gets or sets default max prompt characters.
        /// </summary>
        public int DefaultMaxPromptCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultMaxPromptCharacters);
        /// <summary>
        /// Gets or sets max prompt characters.
        /// </summary>
        public int MaxPromptCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxPromptCharacters);
        /// <summary>
        /// Gets or sets max bootstrap characters.
        /// </summary>
        public int MaxBootstrapCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxBootstrapCharacters);
        /// <summary>
        /// Gets or sets max single conversation message characters.
        /// </summary>
        public int MaxSingleConversationMessageCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxSingleConversationMessageCharacters);

    }
}
