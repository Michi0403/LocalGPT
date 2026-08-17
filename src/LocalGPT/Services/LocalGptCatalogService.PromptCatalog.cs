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
    /// Coordinates local GPT catalog behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class LocalGptCatalogService
    {
    /// <summary>
        /// <summary>
        /// Retrieves suggestion as part of the local GPT catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// Retrieves suggestion as part of the LocalGPT catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
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
                    new("Council starter: calibrate installed models", "Run the recommended first-install model calibration", "Start the maintained Initial Hardware Calibration Benchmark Council. Keep the selected provider-qualified membership authoritative: prepare one role-owned checkable task pack, make every selected Council member execute that assigned Benchmark Subject task pack, deterministically benchmark every benchmark-capable selected member with the full maintained four-task suite at four bounded profile points, preserve failures as coverage evidence, and store measured Low, Middle, High and Expert hardware profiles before synthesis. Do not sample representatives and do not overwrite unrelated approved presets.", "benchmark-council-start", ["adaptive-model-benchmark"], true),
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
        /// Gets the living cities prompt value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The living cities prompt value exposed by <see cref="LocalGptCatalogService"/>.</value>
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
        /// Gets the council knowledge entry new value that forms part of the local GPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The council knowledge entry new value exposed by <see cref="LocalGptCatalogService"/>.</value>
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
    /// Gets the generate solution routes razor value that forms part of the local GPT catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The generate solution routes razor value exposed by <see cref="LocalGptCatalogService"/>.</value>
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
        /// Gets the generate solution app razor value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate solution app razor value exposed by <see cref="LocalGptCatalogService"/>.</value>
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
        /// Gets the generate solution project file value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate solution project file value exposed by <see cref="LocalGptCatalogService"/>.</value>
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
        /// Gets the generate source fidelity razor value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate source fidelity razor value exposed by <see cref="LocalGptCatalogService"/>.</value>
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

    }
}
