using DevExpress.Blazor;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using Markdig;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static LocalGPT.Services.MinecraftModWorkspaceService;

namespace LocalGPT.Extensions.PlainStatics
{
    public static partial class GlobalVariableSlopCollectionToRemove
    {
        public static bool EnsureCreatedMemoryDbTable { get; set; } = false;
        public static bool EnsureCreatedLogsDbTable { get; set; } = false;
        public static bool EnsureCreatedKnowledgeDbTable { get; set; } = false;

        public const string DefaultGradleVersion = "8.14.2";
        public static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        [GeneratedRegex("[^a-zA-Z0-9_.-]")]
        public static partial Regex NameCleaner();

        [GeneratedRegex("[^a-z0-9_]")]
        public static partial Regex ModIdCleaner();

        [GeneratedRegex("[^a-z0-9_]")]
        public static partial Regex PackagePartCleaner();

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
        public const string DefaultMinecraftVersion = "26.1";

        public const string DefaultJavaVersion = "25";
        public const string FabricLoaderVersion = "0.16.9";
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
        public const int MaxDxAiChatPromptCharacters = 60000;
        public const int MaxVisiblePromptCharacters = 12000;
        [GeneratedRegex("(missing feature|missing capability|not implemented|not yet implemented|blocked by|cannot build|requires implementation|feature gap|capability gap|<localgpt-capability-gap>)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex MissingFeaturePattern();

        [GeneratedRegex("<localgpt-capability-gap>(?<body>.*?)</localgpt-capability-gap>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
        public static partial Regex CapabilityGapBlockPattern();
        [GeneratedRegex(@"\b(?:with|and|or|the|a|an|for|to|in|of|by|as|if|when|once|then|because|from|into|that|this|which|th)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex TruncatedTailPattern();
        [GeneratedRegex("<details\\s+class=\"model-thinking\"[^>]*>\\s*<summary>Model thinking</summary>\\s*(?<thinking>.*?)\\s*</details>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
        public static partial Regex ThinkingBlockPattern();

        [GeneratedRegex("```text\\s*(?<prompt>.*?)\\s*```", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
        public static partial Regex CouncilPromptFencePattern();

        [GeneratedRegex("AI Council (?:continuation )?request:\\s*(?<prompt>.*?)(?:\\n\\s*##|\\z)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
        public static partial Regex CouncilRequestBlockPattern();

        public static readonly HashSet<string> DebugExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdb",
            ".pdg",
            ".appxsym"
        };
        public static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".txt",
            ".md",
            ".json",
            ".xml",
            ".csv",
            ".cs",
            ".razor",
            ".cshtml",
            ".css",
            ".scss",
            ".js",
            ".ts",
            ".tsx",
            ".html",
            ".htm",
            ".xaml",
            ".sln",
            ".csproj",
            ".vbproj",
            ".fsproj",
            ".props",
            ".targets",
            ".config",
            ".editorconfig",
            ".yml",
            ".yaml",
            ".toml",
            ".sql",
            ".ps1",
            ".cmd",
            ".bat",
            ".sh",
            ".java",
            ".kt",
            ".gradle",
            ".mcfunction",
            ".mcmeta",
            ".properties"
        };

        public static readonly HashSet<string> BinaryDiagnosticExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".dll",
            ".exe",
            ".pdb",
            ".appxsym",
            ".nupkg",
            ".wasm"
        };
        [GeneratedRegex("<TargetFrameworks?>(?<value>[^<]+)</TargetFrameworks?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex TargetFrameworkPattern();

        [GeneratedRegex("<PackageReference\\s+Include=\"(?<value>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex PackageReferencePattern();

        [GeneratedRegex("(?i)(fuck|shit|bitch|cunt|dick|pussy|whore|slut|porn|xxx)")]
        public static partial Regex SensitiveNamePattern();
        public static readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ".git",
            ".vs",
            ".idea",
            "bin",
            "obj",
            "node_modules",
            "packages",
            ".venv",
            "__pycache__",
            ".gradle",
            ".mypy_cache",
            ".pytest_cache",
            "build",
            "dist",
            "publish",
            "AppPackages"
        };

        public static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".dll",
            ".exe",
            ".pdb",
            ".msi",
            ".pfx",
            ".png",
            ".jpg",
            ".jpeg",
            ".gif",
            ".ico",
            ".pdf",
            ".db",
            ".sqlite",
            ".sqlite3",
            ".zip",
            ".nupkg"
        };

        public static readonly HashSet<string> SourceExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
            ".csproj",
            ".sln",
            ".razor",
            ".xaml",
            ".json",
            ".xml",
            ".py",
            ".js",
            ".ts",
            ".html",
            ".css",
            ".sql",
            ".md",
            ".yml",
            ".yaml",
            ".ps1",
            ".props",
            ".targets",
            ".config",
            ".resx",
            ".mdx",
            ".go",
            ".mod",
            ".sum",
            ".proto",
            ".toml",
            ".ini",
            ".cmake",
            ".sh",
            ".bat",
            ".cmd",
            ".gotmpl",
            ".txt",
            ".text",
            ".log",
            ".csv",
            ".tsv",
            ".http",
            ".rest",
            ".tmpl"
        };
        public const string DefaultOllamaUri = "http://localhost:11434";
        public const int MaxParticipants = int.MaxValue;
        public const int DefaultMaxParallelModels = 1;
        public const int DefaultHeavyModelGpuLayers = 20;
        public const int MinContextTokens = 2048;
        public const int DefaultContextTokens = 65536;
        public const int MaxContextTokens = 262144;
        public const int MinOutputTokens = 64;
        public const int MaxOutputTokens = 262144;


        [GeneratedRegex("<p\\s+class=\"localgpt-stream-status\"[^>]*>.*?</p>\\s*", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
        public static partial Regex StreamStatusPattern();

        [GeneratedRegex("\\b[\\p{L}\\p{N}_'-]+\\b", RegexOptions.CultureInvariant)]
        public static partial Regex WordPattern();

        [GeneratedRegex("(implement|implementation|develop|development|build|create|add|generate|scaffold|feature|code|page|component|service|endpoint|database|settings|artifact|solution|plugin|mod|datapack)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex DevelopmentRequestPattern();

        [GeneratedRegex("(downloadable|download link|download route|zip|\\.zip|\\.cs\\b|\\.razor\\b|\\.dll\\b|\\.sln\\b|\\.csproj\\b|artifact|solution zip|project zip|whole solution|full solution)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex ExplicitArtifactIntentPattern();

        [GeneratedRegex("(review|code review|diagnose|diagnostic|release readiness|readiness|go or no-go|blockers|evidence|what failed|why failed|build/deploy/package/publish|publish cycle|release cycle|maintenance cycle)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex AdviceOnlyPromptPattern();

        [GeneratedRegex("(generate|create|produce|write|implement|make|build)\\b.{0,120}\\b(downloadable|artifact|zip|solution|source code|\\.sln|\\.csproj|\\.cs\\b|\\.razor\\b|ai host|localgpt replacement|application|app|datapack|modpack)\\b|\\b(downloadable|artifact|zip|solution)\\b.{0,120}\\b(generate|create|produce|write|implement|make|build)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
        public static partial Regex ExplicitArtifactCreationCommandPattern();

        [GeneratedRegex("(minecraft|living cities|modpack|datapack|data pack|pack\\.mcmeta|mcfunction).*(generate|create|build|zip|download|artifact)|(generate|create|build|zip|download|artifact).*(minecraft|living cities|modpack|datapack|data pack|pack\\.mcmeta|mcfunction)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex ConcreteMinecraftArtifactPattern();

        [GeneratedRegex("(dotnet|\\.net|c#|blazor|razor|devexpress|aspnet|asp\\.net|ollama).*(solution|project|zip|download|artifact|page|component|api|route|service)|(solution|project|zip|download|artifact|page|component|api|route|service).*(dotnet|\\.net|c#|blazor|razor|devexpress|aspnet|asp\\.net|ollama)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex ConcreteDotNetArtifactPattern();

        [GeneratedRegex("(ai host|local ai host|model host|inference host|native runner|model-file runner|model file runner|iinferencerunner|nativemodelfile|llama\\.cpp|gguf)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex AiHostSetupPattern();

        [GeneratedRegex("(decision poll required|user decision poll|implementation path|architecture choice|architecture decision|target platform|runtime choice|ui stack|unclear implementation|unclear scope|scope is uncertain|ownership is uncertain|ask the user|needs user choice|choose between|pick between|multiple reasonable|trade-?off|depends on|which path|which approach)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex ImplementationDecisionPattern();

        [GeneratedRegex("(choose|decide|pick|option|alternative|trade-?off|depends|uncertain|scope|ownership|clarify|question)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex ImplementationChoicePattern();

        [GeneratedRegex("(decision poll required|no (?:code|files?|artifacts?) will be generated until|do not generate (?:code|files?|artifacts?) until|stop before generating|await (?:your )?(?:selection|choice|answer|decision)|waiting for (?:your )?(?:selection|choice|answer|decision)|please choose .* before|select .* and reply|will generate .* once (?:chosen|selected|confirmed))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex BlockingArtifactDecisionPattern();

        [GeneratedRegex("(prior consent for safe sandbox details:\\s*granted|let council choose safe sandbox details|you may decide safe sandbox details|council may choose safe sandbox defaults|make reasonable sandbox assumptions|decide yourself for the sandbox)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex SafeSandboxConsentPattern();

        [GeneratedRegex("(ask me first|do not generate|don't generate|wait for my decision|stop before coding|stop before generating|no files until|no artifact until)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex ExplicitDoNotGenerateUntilUserDecisionPattern();

        [GeneratedRegex("(work as (?:the )?developers|you are the developers|continue until (?:you )?(?:produce|create|generate)|develop and debug|produce .* artifact|generate .* artifact|create .* artifact)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex DeveloperExecutionIntentPattern();

 
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
        public sealed record CommandPolicyDecision(bool Allowed, string Decision, string Reason, string Profile);
        public const int ProbeCommandTimeoutSeconds = 5;
        public static readonly string[] SidecarSuffixes = ["", "-wal", "-shm"];
        [GeneratedRegex("^\\s*@using\\s+(?<namespace>DevExpress(?:\\.[A-Za-z0-9_]+)+)", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
        public static partial Regex DevExpressImportPattern();

        [GeneratedRegex("AddDevExpress[A-Za-z0-9_]*\\(", RegexOptions.CultureInvariant)]
        public static partial Regex DevExpressRegistrationPattern();
        public static readonly HashSet<string> ArtifactTextExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
            ".razor",
            ".cshtml",
            ".csproj",
            ".sln",
            ".props",
            ".targets",
            ".md",
            ".txt",
            ".json",
            ".xml",
            ".css",
            ".scss",
            ".js",
            ".ts",
            ".yml",
            ".yaml",
            ".ps1",
            ".sql",
            ".html",
            ".htm",
            ".mcfunction",
            ".mcmeta",
            ".toml",
            ".properties",
            ".java"
        };

        public const long MaxArtifactTextFileBytes = 2 * 1024 * 1024;
     
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

        public static bool IsHarmonyModel { get; set; } = false;
        public const int MaxFiles = 12;
        public const long MaxSingleFileBytes = 32 * 1024 * 1024;
        public const long MaxTotalFileBytes = 96 * 1024 * 1024;
        public const int MaxZipEntries = 400;
        public const long MaxZipEntryBytes = 8 * 1024 * 1024;
        public const long MaxExtractedBytes = 64 * 1024 * 1024;
        public const int MaxContextCharacters = 80_000;
        public const int MaxExcerptCharactersPerFile = 6_000;
        public const int MaxBinaryStringCharacters = 8_000;
        public sealed record AnalyzedUploadFile(
    ChatUploadWorkspaceFileSummary Summary,
    string Excerpt);
        public static readonly string[] KnowledgeFiles =
       [
           "AGENTS.md",
            "CLAUDE.md",
            "llms.txt",
            Path.Combine("docs", "ARCHITECTURE_FOR_AI.md"),
            Path.Combine("docs", "COUNCIL_KNOWLEDGE_SEED.sql"),
            Path.Combine("docs", "MINECRAFT_MOD_AI_BUILDER.md"),
            Path.Combine("docs", "MINECRAFT_SOURCE_KNOWLEDGE.md"),
            Path.Combine("docs", "AI_HOST_DOTNET_EXPERIMENT.md"),
            Path.Combine("docs", "AI_HOST_DOTNET_BLAZOR_REBUILD_GUIDE.md"),
            Path.Combine("docs", "AI_HOST_CONTROL_PLANE_ARCHITECTURE.md"),
            Path.Combine("docs", "DOTNET_AI_HOST_ARCHITECTURE_PATTERNS.md"),
            Path.Combine("docs", "LOCALGPT_DEVELOPER_DIARY.md"),
            Path.Combine("docs", "LOCALGPT_WORKFLOW_MEMORY.md"),
            Path.Combine("docs", "CAPABILITY_GAP_CONTRACT.md"),
            Path.Combine("docs", "BLAZOR_DEVEXPRESS_AI_GENERATION.md"),
            Path.Combine("docs", "BLAZOR_BOOTSTRAP_DEVEXPRESS_DESIGN.md"),
            Path.Combine("docs", "FRONTEND_DESIGN_PATTERN_LIBRARY.md"),
            Path.Combine("docs", "MICROSOFT_DOTNET_SAMPLE_CURRICULUM.md"),
            Path.Combine("docs", "EF_DEVEXPRESS_BUSINESS_OBJECTS.md"),
            Path.Combine("docs", "GENERATION_ARCHETYPE_CONTRACTS.md")
       ];
        public const string omission =
                 "\n\n[...older context trimmed by LocalGPT to fit the local model context window...]\n\n";

        public const string shortOmission =
            "\n... truncated by LocalGPT upload workspace budget ...";
        public sealed class OllamaChatRequest
        {
            public string Model { get; set; } = string.Empty;
            public bool Stream { get; set; }
            public string KeepAlive { get; set; } = "10m";
            public List<OllamaChatMessage> Messages { get; set; } = new();
            public OllamaRequestOptions? Options { get; set; }
        }

        public sealed class OllamaRequestOptions
        {
            [JsonPropertyName("num_predict")]
            public int NumPredict { get; set; }

            [JsonPropertyName("num_ctx")]
            public int? NumCtx { get; set; }

            [JsonPropertyName("num_gpu")]
            public int? NumGpu { get; set; }

            [JsonPropertyName("temperature")]
            public double? Temperature { get; set; }
        }

        public sealed class OllamaChatMessage
        {
            public string Role { get; set; } = "user";
            public string Content { get; set; } = string.Empty;
            public string? Thinking { get; set; }
        }

        public sealed class OllamaChatResponse
        {
            public OllamaChatMessage? Message { get; set; }
        }

  

        public static readonly Regex DownloadUrlPattern =
     new("\"downloadUrl\"\\s*:\\s*\"(?<url>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public const string LearnBaseFilePolicySummary =
            "Reads source and docs such as .cs, .razor, .csproj, .sln, .md, .yml, .json, .xml, .py, .js, .ts, .go, .ps1, and .sql. Skips build/cache folders such as bin, obj, node_modules, packages, .git, build, dist, and publish. Binary files, installers, archives, PDFs, certificates, SQLite files, and images are counted or ignored, not stored as knowledge text.";
        public const string LearnBaseDuplicatePolicySummary =
            "Duplicate handling: each project path and known docs-corpus section gets a stable database id. Re-importing the same source updates/upserts the existing knowledge row instead of adding another copy.";
        public static string LearnBasePresetList => string.Join(", ", GlobalVariableSlopCollectionToRemove.LearnBasePresets.Select(preset => preset.Label));

        public static readonly IReadOnlyList<GlobalVariableSlopCollectionToRemove.LearnBasePreset> LearnBasePresets =
   [
       new(
            "All selected local learn-base",
            @"C:\learnbaseforlocalgpt",
            "Scans the curated local learn-base root and auto-detects known docs corpora plus project architecture roots.",
            80),
        new(
            "Microsoft .NET docs + C# compiler",
            @"C:\learnbaseforlocalgpt\docs-main\docs-main",
            "Teaches source maps for .NET architecture, C# language/compiler diagnostics, C# 12-era syntax, ASP.NET Core, Blazor, data, and DocFX/Microsoft Learn authoring.",
            30),
        new(
            "Windows developer docs",
            @"C:\learnbaseforlocalgpt\windows-dev-docs-docs",
            "Teaches source maps for Windows App SDK, WinUI, WebView2, MSIX, Windows setup/support, design, accessibility, and technician workflows.",
            24),
        new(
            "DevExpress Blazor 25.2 samples",
            @"C:\learnbaseforlocalgpt\Blazor-25.2\Blazor-25.2",
            "Scans DevExpress Blazor demos and examples so generated pages can choose real components, services, layout patterns, and file/download workflows.",
            60),
        new(
            "DevExpress examples",
            @"C:\learnbaseforlocalgpt\DevExpress-Examples",
            "Scans local DevExpress example repositories for reusable component and service wiring patterns.",
            60),
        new(
            "Custom path",
            @"C:\learnbaseforlocalgpt",
            "Use this when you want to paste or edit a specific local source/docs folder path.",
            40)
   ];
        public static readonly IReadOnlyList<GlobalVariableSlopCollectionToRemove.LearnBaseScanProfile> LearnBaseScanProfiles =
        [
            new("Focused scan", 12, "Best for one documentation corpus or one repository. Fast and low noise."),
        new("Balanced scan", 40, "Best default: enough project roots to teach patterns without importing every nested sample."),
        new("Broad scan", 100, "Best after adding many repositories or documentation corpora. Slower, but still bounded."),
        new("Custom limit", 40, "Use the advanced import limit below.")
        ];

        public static readonly List<GlobalVariableSlopCollectionToRemove.TestLabRoute> Routes =
 [
     new("Health", "/health", ButtonRenderStyle.Secondary),
        new("Diagnostics", "/__diag", ButtonRenderStyle.Secondary),
        new("DXAiFunctions", "/__diag/dxaichat-functions", ButtonRenderStyle.Secondary),
        new("Minecraft 26.1", "/__diag/minecraft/datapack-version?minecraftVersion=26.1", ButtonRenderStyle.Secondary),
        new("Datapack ZIP", "/__diag/council/artifact-smoke?target=datapack", ButtonRenderStyle.Primary),
        new("AI Host ZIP", "/__diag/council/artifact-smoke?target=ai-host", ButtonRenderStyle.Primary),
        new("Minecraft Benchmark", "/__diag/minecraft/datapack-benchmark?minecraftVersion=26.1", ButtonRenderStyle.Secondary),
        new("Engineering Benchmark", "/__diag/benchmark/engineering?taskSet=engineering&saveToKnowledge=true", ButtonRenderStyle.Secondary),
        new("Replacement Benchmark", "/__diag/benchmark/engineering?taskSet=replacement&validateBuildableArtifacts=true&maxBuildArtifacts=4&saveToKnowledge=true", ButtonRenderStyle.Primary),
        new("Council Feedback", "/__diag/council/development-feedback-talk?maxOutputTokens=2048&maxContextTokens=32768&maxRounds=0", ButtonRenderStyle.Primary)
 ];
        public static  List<PromptSuggestion> GetSuggestion()
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
        public static string LivingCitiesPrompt =>
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
        public static CouncilKnowledgeEntry CouncilKnowledgeEntryNew => new CouncilKnowledgeEntry()
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
    public static string GenerateSolutionRoutesRazor =>
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
        public static string GenerateSolutionAppRazor =>
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
        public static string GenerateSolutionProjectFile =>
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
        public static string GenerateSourceFidelityRazor =>
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
        public static string GenerateSolutionCss =>
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
        public static string GenerateAiHostSettingsRazor =>
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
        public static string GenerateAiHostLogsRazor =>
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
        public static string GenerateAiHostRunnerPluginsRazor =>
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
        public static string GenerateAiHostHardwareRazor =>
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
        public static string GenerateAiHostTemplatesRazor =>
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
        public static string GenerateAiHostModelDownloadsRazor =>
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
        public static string GenerateAiHostRunningModelsRazor =>
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
        public static string GenerateAiHostChatRazor =>
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


        [GeneratedRegex("(devexpress|richedit|pdfviewer|pivot|report|xtrareport|office|docx|xlsx|pdf export|spreadsheet|document generation)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex DevExpressDocumentPattern();

        [GeneratedRegex("(\\.xlsx|xlsx|excel|\\.pptx|pptx|powerpoint|\\.pdf|pdf|\\.docx|docx|word|export format|file generation)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex ExportFormatPattern();

        [GeneratedRegex("(blazor|razor|component|page|dxgrid|dxformlayout|dxbutton|dxmemo|dxtextbox|dxcombobox|dxaichat|devexpress blazor|interactive(server|webassembly|auto))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex BlazorFrontendPattern();

        [GeneratedRegex("(dotnet|\\.net|aspnet|asp\\.net|blazor|c#|codedom|entityframework|sqlite|winui|webview2)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex DotNetPattern();

        [GeneratedRegex("(minecraft|fabric|neoforge|paper|datapack|gradle|java)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex MinecraftPattern();

        [GeneratedRegex("(datapack|data pack|pack\\.mcmeta|mcfunction|living cities)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex DatapackPattern();

        [GeneratedRegex("(fabric.*paper.*neoforge|neoforge.*paper.*fabric|loader.*matrix|skeleton.*distinction|project skeleton distinction)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex MinecraftSkeletonMatrixPattern();

        [GeneratedRegex("(?<!\\d)(?<version>(?:1\\.\\d{1,2}|26\\.\\d)(?:\\.\\d{1,2})?(?:-snapshot-\\d+)?)(?!\\d)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex MinecraftVersionPattern();

        [GeneratedRegex("(?m)^\\s*/", RegexOptions.CultureInvariant)]
        public static partial Regex LeadingSlashCommandPattern();

        [GeneratedRegex("\\bdata\\s+remove\\s+storage\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex RootStorageRemovePattern();

        [GeneratedRegex("\\bstore\\s+result\\s+storage\\s+[a-z0-9_.-]+:[a-z0-9_/-]+\\.[a-z0-9_.-]+\\s+(?:byte|short|int|long|float|double)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex MalformedStorageTargetPattern();

        [GeneratedRegex("(frontend|razor|devexpress|dxaichat|css|javascript)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex FrontendPattern();

        [GeneratedRegex("(whole solution|full solution|entire solution|solution zip|project zip|\\.sln|\\.csproj|all source files|tacosportalopen|localgpt\\s+(?:clone|replacement|workbench|app|application|solution)|(?:clone|replace|rebuild)\\s+localgpt|whole ai host|ai host dotnet|local ai host|whole ollama|ollama dotnet|ollama \\.net)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex WholeSolutionPattern();

        [GeneratedRegex(
            "(ai\\s*host|local\\s*model\\s*host|model[- ]file\\s*runner|native\\s*runner|ollama[- ]compatible|" +
            "/api/(?:chat|generate|tags|ps|version)|host\\s+gpt-oss|provider[- ]compatible).*" +
            "(dotnet|\\.net|blazor|devexpress|aspnet|asp\\.net|api|route|endpoint|sqlite|ollama|model|runner)|" +
            "(dotnet|\\.net|blazor|devexpress|aspnet|asp\\.net|api|route|endpoint|sqlite|model|runner).*" +
            "(ai\\s*host|local\\s*model\\s*host|model[- ]file\\s*runner|native\\s*runner|ollama[- ]compatible|" +
            "/api/(?:chat|generate|tags|ps|version)|provider[- ]compatible)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
        public static partial Regex AiHostExperimentPattern();

        [GeneratedRegex("(localgpt|local gpt).*(clone|replacement|workbench|app|application|solution|dxaichat|ai council|sqlite memory|test lab)|(clone|replace|rebuild).*(localgpt|local gpt)|(dxaichat|ai council|sqlite memory|test lab).*(localgpt|local gpt)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
        public static partial Regex LocalGptReplacementPattern();

        [GeneratedRegex("(tacosportalopen|tacos portal|restaurant portal|orders.*menu|menu.*orders|reservation|kitchen queue)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex TacosPortalPattern();

        [GeneratedRegex("(bot backend|telegram bot|botapi|webhook|conversation state|python\\.net|whisper|translator bot)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex BotBackendPattern();

        [GeneratedRegex("(log|logger|diagnostic|error|warning|telemetry)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex LoggingPattern();


        public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
        public static partial Regex WhitespacePattern();
        [GeneratedRegex("(?im)^\\s*(?:[-*]\\s*)?(?<line>(?:helpful sources?|source request|needed sources?|references?|docs?|documentation|official docs?|examples?|sample projects?|spec(?:ification)?s?|tutorials?)\\s*[:\\-].+)$", RegexOptions.CultureInvariant)]
        public static partial Regex HelpfulSourceLinePattern();
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
        public const int MinCouncilOutputTokens = 256;
        public const int DefaultCouncilOutputTokens = 262144;
        public const int MaxCouncilOutputTokens = 262144;
        public const int MinCouncilContextTokens = 2048;
        public const int DefaultCouncilContextTokens = 262144;
        public const int MaxCouncilContextTokens = 262144;
        public const string CouncilSessionName = "AI Council — selected Ollama models";
        public static readonly Regex HarmonyMarkerCleanupRegex = new("<\\|[^|>]+\\|>", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        public static readonly Regex OpenThinkingDetailsRegex = new("(?i)<details\\s+class=\"model-thinking\"\\s+open>", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        public static readonly Regex ListAfterHtmlRegex = new("(?i)(</(?:p|details|pre|div)>)\\s*((?:[-*]|\\d+\\.)\\s+)", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        public static readonly MarkdownPipeline ChatMarkdownPipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    .Build();
        public const int MaxUploadFiles = int.MaxValue;
        public const int MaxUploadBytes = int.MaxValue;
        public static readonly List<string> AllowedUploadExtensions =
        [
            ".3dm", ".3ds", ".3g2", ".3gp", ".7z", ".aab", ".aac", ".abc", ".abf", ".ac3", ".accdb", ".ace", ".ada", ".adb", ".adml", ".admx", ".ads", ".aep", ".aepx", ".afdesign", ".afm", ".afphoto", ".agda", ".age", ".ahk", ".ai", ".aif", ".aifc", ".aiff", ".alac", ".als", ".amr", ".ani", ".apk", ".apks", ".apng", ".app", ".app.src", ".AppImage", ".appinstaller", ".applescript", ".appx", ".appxbundle", ".arc", ".arj", ".arm", ".arrow", ".arsc", ".arw", ".asc", ".ascx", ".ase", ".aseprite", ".asf", ".asm", ".asmx", ".asp", ".aspx", ".ass", ".asset", ".assets", ".astro", ".atom", ".au", ".au3", ".aup3", ".authorized_keys", ".automount", ".av1", ".avhd", ".avhdx", ".avi", ".avif", ".avro", ".awk", ".azw", ".azw3", ".babelrc", ".backup", ".bai", ".bak", ".bam", ".band", ".bas", ".bash", ".bash_logout", ".bash_profile", ".bashrc", ".bat", ".bazel", ".bb", ".bcf", ".bdf", ".beam", ".bed", ".bib", ".bibtex", ".bicep", ".bigbed", ".bigwig", ".bin", ".bkf", ".bkp", ".blend", ".blf", ".blg", ".bmp", ".br", ".brep", ".browserslistrc", ".bru", ".bson", ".bsp", ".buck", ".build", ".bw", ".bz2", ".bzl", ".bzrignore", ".c", ".c++", ".c4d", ".cab", ".caf", ".cairo", ".cap", ".capnp", ".car", ".cat", ".cc", ".cdf", ".cdr", ".cer", ".cert", ".cfg", ".chart", ".chd", ".chm", ".cif", ".circleci", ".cjs", ".cl", ".clang-format", ".clang-tidy", ".clangd", ".class", ".classpath", ".clj", ".cljc", ".cljs", ".cls", ".cmake", ".cmake.in", ".cmd", ".cmx", ".cnc", ".cnf", ".cob", ".code-workspace", ".com", ".compose.yaml", ".compose.yml", ".conf", ".config", ".containerfile", ".coq", ".core", ".cov", ".coverage", ".coveragerc", ".cpio", ".cpl", ".cpp", ".cpy", ".cr", ".cr2", ".cr3", ".crash", ".crate", ".crdownload", ".crt", ".cs", ".csh", ".cshtml", ".csproj", ".csr", ".css", ".csv", ".cts", ".cue", ".cur", ".curlrc", ".cxx", ".d", ".dae", ".dart", ".dat", ".data", ".db", ".db3", ".dbf", ".dcm", ".dds", ".deb", ".dem", ".der", ".deskthemepack", ".desktop", ".dex", ".dfont", ".dfxp", ".di", ".diagcab", ".diagpkg", ".dib", ".dicom", ".dif", ".diff", ".dist-info", ".divx", ".djvu", ".dll", ".dmg", ".dmp", ".dng", ".doc", ".dockerfile", ".dockerignore", ".docm", ".docx", ".dot", ".dotm", ".dotx", ".download", ".dpr", ".drv", ".dsp", ".dsw", ".dta", ".dtd", ".dts", ".dump", ".dwg", ".dxf", ".ear", ".ebuild", ".edf", ".editorconfig", ".edn", ".egg", ".egg-info", ".ejs", ".el", ".elc", ".elm", ".emf", ".eml", ".entitlements", ".env", ".env.dev", ".env.development", ".env.example", ".env.local", ".env.production", ".env.test", ".eot", ".eps", ".epub", ".erb", ".erl", ".err", ".esd", ".eslintignore", ".eslintrc", ".eta", ".etl", ".evtx", ".ex", ".exe", ".exr", ".exs", ".f", ".f03", ".f08", ".f4v", ".f77", ".f90", ".f95", ".fa", ".faa", ".fasta", ".fastq", ".fb2", ".fbx", ".fcs", ".fcstd", ".feather", ".fig", ".fish", ".fit", ".fits", ".flac", ".flake8", ".flatpak", ".flatpakref", ".flatpakrepo", ".flp", ".flutter-plugins", ".flutter-plugins-dependencies", ".flv", ".fna", ".fnt", ".fodp", ".fods", ".fodt", ".fon", ".for", ".fq", ".frm", ".fs", ".fsi", ".fsproj", ".fsx", ".gadget", ".gb", ".gba", ".gbc", ".gcda", ".gcno", ".gcode", ".gd", ".gdshader", ".gem", ".gemspec", ".geojson", ".gff", ".gff3", ".gho", ".ghs", ".gif", ".gitattributes", ".gitconfig", ".gitignore", ".gitkeep", ".gitlab-ci.yml", ".gitmodules", ".glb", ".gltf", ".gn", ".gni", ".go", ".godot", ".gotmpl", ".gpg", ".gpkg", ".gpx", ".gql", ".gradle", ".graphql", ".grb", ".grib", ".groovy", ".gsp", ".gtf", ".gvimrc", ".gvy", ".gy", ".gz", ".h", ".h++", ".h264", ".h265", ".h5", ".hack", ".hadolint.yaml", ".haml", ".handlebars", ".har", ".hbs", ".hcl", ".hdd", ".hdf5", ".hdr", ".hds", ".heic", ".heif", ".helmignore", ".hevc", ".hgignore", ".hgtags", ".hh", ".hlp", ".hpp", ".hrl", ".hs", ".hta", ".htm", ".html", ".http", ".hxx", ".hyper", ".i", ".iam", ".ibd", ".icl", ".icns", ".ico", ".ics", ".idea", ".idl", ".idml", ".idr", ".idw", ".idx", ".ifc", ".iges", ".igs", ".ii", ".ima", ".img", ".iml", ".inc", ".indd", ".index", ".indt", ".inf", ".ini", ".inputrc", ".ipa", ".ipr", ".ipt", ".ipynb", ".iso", ".istanbul.yml", ".it", ".iws", ".j2", ".jade", ".jar", ".java", ".jfif", ".jinja", ".jks", ".jl", ".jmod", ".job", ".jpe", ".jpeg", ".jpg", ".js", ".jscsrc", ".jse", ".jshintignore", ".json", ".json5", ".jsonc", ".jsonl", ".jsonnet", ".jsp", ".jsx", ".junit", ".kar", ".kbx", ".kdb", ".key", ".keystore", ".kml", ".kmz", ".known_hosts", ".kra", ".ksh", ".kt", ".kts", ".ktx", ".ktx2", ".kubeconfig", ".las", ".latex", ".launch", ".laz", ".lcov", ".lean", ".less", ".lha", ".lhs", ".library-ms", ".libsonnet", ".liquid", ".lisp", ".list", ".lnk", ".lnk2", ".lock", ".log", ".logic", ".lottie", ".love", ".lpr", ".lsp", ".lst", ".ltx", ".lua", ".luacheckrc", ".lz", ".lzh", ".lzma", ".m", ".m2ts", ".m2v", ".m3u", ".m3u8", ".m4a", ".m4v", ".ma", ".mak", ".make", ".manifest", ".map", ".markdown", ".marko", ".mat", ".max", ".mb", ".mbox", ".mbtiles", ".md", ".md5", ".mdb", ".mdmp", ".mdown", ".me", ".meson", ".mid", ".midi", ".minisig", ".mjs", ".mk", ".mka", ".mkd", ".mkv", ".ml", ".mli", ".mll", ".mlx", ".mly", ".mm", ".mobi", ".mobileconfig", ".mobileprovision", ".mod", ".mol", ".mol2", ".mount", ".mov", ".move", ".mp3", ".mp4", ".mpeg", ".mpg", ".mpkg", ".mrimg", ".msc", ".msg", ".msi", ".msix", ".msixbundle", ".msm", ".msp", ".mtl", ".mts", ".mui", ".mustache", ".myd", ".myi", ".mysql", ".mzml", ".mzxml", ".n64", ".nanorc", ".nasm", ".nb", ".nc", ".ndjson", ".nds", ".nef", ".nes", ".netmon", ".nfo", ".nii", ".nii.gz", ".nim", ".nims", ".ninja", ".njk", ".nomad", ".npmignore", ".npmrc", ".nrw", ".numbers", ".nunjucks", ".nupkg", ".nuspec", ".nut", ".nvram", ".nycrc", ".obj", ".ocx", ".odb", ".odex", ".odp", ".ods", ".odt", ".oga", ".ogg", ".ogv", ".old", ".one", ".onetoc2", ".opml", ".opus", ".ora", ".orc", ".orf", ".orig", ".osm", ".otc", ".otf", ".ots", ".ott", ".out", ".ova", ".ovf", ".ovpn", ".oxps", ".p12", ".p7b", ".p7c", ".p7m", ".p7s", ".p8", ".pacnew", ".pacsave", ".pages", ".pak", ".pants", ".parameters", ".params", ".parquet", ".part", ".pas", ".patch", ".path", ".pbix", ".pbm", ".pbxproj", ".pcap", ".pcapng", ".pcf", ".pck", ".pdb", ".pdf", ".pef", ".pem", ".perf", ".pfb", ".pfm", ".pfx", ".pgm", ".pgp", ".phar", ".php", ".phtml", ".pic", ".pickle", ".pid", ".pif", ".pk3", ".pk4", ".pkg", ".pkg.tar.gz", ".pkg.tar.xz", ".pkg.tar.zst", ".pkl", ".pl", ".plist", ".pls", ".ply", ".pm", ".png", ".pnm", ".pod", ".podspec", ".pol", ".policy", ".pom", ".postman_collection", ".postman_environment", ".pot", ".potx", ".pp", ".ppm", ".pps", ".ppsx", ".ppt", ".pptm", ".pptx", ".prefab", ".prefs", ".prettierignore", ".prettierrc", ".pri", ".prj", ".pro", ".prof", ".profile", ".project", ".properties", ".props", ".proto", ".prproj", ".ps", ".ps1", ".ps1xml", ".psb", ".psd", ".psd1", ".psm1", ".psql", ".psv", ".ptx", ".pub", ".pubring", ".pug", ".purs", ".py", ".pyc", ".pyd", ".pyi", ".pylintrc", ".pyo", ".pyw", ".qbs", ".qcow", ".qcow2", ".qgs", ".qgz", ".qmd", ".qt", ".qvd", ".qvw", ".qxp", ".r", ".r00", ".r01", ".ra", ".raf", ".rake", ".ram", ".rar", ".raw", ".razor", ".rb", ".rc", ".rdata", ".rdp", ".rds", ".readme", ".reg", ".rego", ".regtrans-ms", ".rej", ".repo", ".res", ".rest", ".rfa", ".riot", ".rkt", ".rm", ".rmd", ".rmi", ".rmvb", ".rom", ".room", ".rpgproject", ".rpm", ".rpp", ".rs", ".rspec", ".rss", ".rst", ".rtf", ".rubocop.yml", ".rules", ".run", ".rvt", ".rw2", ".s", ".s3m", ".sab", ".sam", ".sarif", ".sas7bdat", ".sass", ".sat", ".sav", ".sbt", ".sc", ".scad", ".scala", ".scf", ".schema", ".schemas", ".scm", ".scpt", ".scr", ".screenrc", ".scss", ".sct", ".sdb", ".sdf", ".sea", ".search-ms", ".secring", ".sed", ".service", ".settings", ".sf2", ".sfc", ".sfd", ".sfv", ".sfz", ".sh", ".sha1", ".sha256", ".sha512", ".shellcheckrc", ".shp", ".shtml", ".shx", ".sig", ".sit", ".sitx", ".skaffold", ".sketch", ".skp", ".sldasm", ".slddrw", ".sldprt", ".slim", ".slk", ".sln", ".smc", ".smi", ".snap", ".snapshot", ".snd", ".snupkg", ".socket", ".sol", ".sources", ".sparsebundle", ".sparseimage", ".spc", ".spec", ".sprite", ".sql", ".sqlite", ".sqlite3", ".srt", ".srv", ".srw", ".ss", ".ssa", ".ssh/config", ".sst", ".stackdump", ".step", ".stl", ".storyboard", ".stp", ".strings", ".stringsdict", ".styl", ".stylelintrc", ".sub", ".sublime-project", ".sublime-workspace", ".sum", ".suo", ".sv", ".svelte", ".svg", ".svgz", ".svh", ".svnignore", ".swift", ".swm", ".swo", ".swp", ".sys", ".t", ".tap", ".tar", ".tar.bz2", ".tar.gz", ".tar.lz", ".tar.lzma", ".tar.xz", ".tar.zst", ".target", ".targets", ".tbz", ".tbz2", ".tcsh", ".teal", ".temp", ".template", ".tex", ".text", ".tf", ".tf.json", ".tfstate", ".tfstate.backup", ".tfvars", ".tga", ".tgz", ".theme", ".themepack", ".thrift", ".tib", ".tif", ".tiff", ".tiltfile", ".timer", ".tlz", ".tmp", ".tmpl", ".tmproj", ".tmux.conf", ".toml", ".tpl", ".trace", ".traj", ".travis.yml", ".tres", ".trr", ".truststore", ".trx", ".ts", ".tscn", ".tsv", ".tsx", ".ttc", ".ttf", ".ttml", ".twb", ".twbx", ".twig", ".txt", ".txz", ".tzst", ".uasset", ".umap", ".unit", ".unity", ".unitypackage", ".uproject", ".url", ".usd", ".usda", ".usdc", ".usdz", ".v", ".v64", ".vb", ".vba", ".vbe", ".vbhtml", ".vbox", ".vbox-prev", ".vbproj", ".vbs", ".vcf", ".vcxproj", ".vdex", ".vdi", ".veg", ".vfd", ".vhd", ".vhdl", ".vhdx", ".vimrc", ".vmdk", ".vmx", ".vmxf", ".vob", ".vssettings", ".vst", ".vst3", ".vsv", ".vtt", ".vue", ".vy", ".wad", ".war", ".wasm", ".wat", ".wav", ".wave", ".webm", ".webmanifest", ".webp", ".wgetrc", ".whl", ".wig", ".wim", ".wma", ".wmf", ".wmv", ".woff", ".woff2", ".wpd", ".wps", ".wrl", ".wsf", ".wsh", ".x3d", ".xapk", ".xar", ".xcassets", ".xcconfig", ".xcf", ".xcodeproj", ".xcworkspace", ".xd", ".Xdefaults", ".xhtml", ".xib", ".xinitrc", ".xls", ".xlsb", ".xlsm", ".xlsx", ".xlt", ".xltx", ".xm", ".xml", ".xmp", ".xprofile", ".xps", ".Xresources", ".xsd", ".xsl", ".xslt", ".xtc", ".xvid", ".xyz", ".xz", ".yaml", ".yamllint", ".yang", ".yarnrc", ".yml", ".yubikey", ".yuv", ".yy", ".yyp", ".Z", ".z64", ".zig", ".zip", ".zipx", ".zlogin", ".zlogout", ".zprofile", ".zsh", ".zshrc", ".zst"

        ];
        public static readonly List<string> AllowedUploadMimeTypes =
        [
  "text/*",
  "application/json",
  "application/xml",
  "application/zip",
  "application/x-zip-compressed",
  "application/octet-stream",
  "application/x-msdownload",
  "application/appx",
  "application/atom+xml",
  "application/avro",
  "application/cpl+xml",
  "application/dicom",
  "application/epub+zip",
  "application/fits",
  "application/font-woff",
  "application/font-woff2",
  "application/geo+json",
  "application/geopackage+sqlite3",
  "application/gpg-keys",
  "application/graphql",
  "application/graphql-response+json",
  "application/gzip",
  "application/hta",
  "application/java-archive",
  "application/java-vm",
  "application/json-patch+json",
  "application/json-seq",
  "application/json5",
  "application/jsonlines",
  "application/junit+xml",
  "application/ld+json",
  "application/manifest+json",
  "application/mathematica",
  "application/mbox",
  "application/merge-patch+json",
  "application/msaccess",
  "application/msix",
  "application/msword",
  "application/netcdf",
  "application/ogg",
  "application/onenote",
  "application/opml+xml",
  "application/oxps",
  "application/p21",
  "application/pdf",
  "application/pem-certificate-chain",
  "application/pgp-encrypted",
  "application/pgp-keys",
  "application/pgp-signature",
  "application/pkcs10",
  "application/pkcs12",
  "application/pkcs7-mime",
  "application/pkcs7-signature",
  "application/pkcs8",
  "application/pkix-cert",
  "application/postscript",
  "application/protobuf",
  "application/rls-services+xml",
  "application/rss+xml",
  "application/rtf",
  "application/sarif+json",
  "application/schema+json",
  "application/simple-filter+xml",
  "application/sla",
  "application/smil+xml",
  "application/sql",
  "application/toml",
  "application/ttml+xml",
  "application/vnd.3gpp.pic-bw-small",
  "application/vnd.age",
  "application/vnd.amazon.ebook",
  "application/vnd.amazon.mobi8-ebook",
  "application/vnd.android.package-archive",
  "application/vnd.apache.arrow.file",
  "application/vnd.apache.parquet",
  "application/vnd.apache.thrift.binary",
  "application/vnd.api+json",
  "application/vnd.apple.installer+xml",
  "application/vnd.apple.keynote",
  "application/vnd.apple.mpegurl",
  "application/vnd.apple.numbers",
  "application/vnd.apple.pages",
  "application/vnd.audiograph",
  "application/vnd.clonk.c4group",
  "application/vnd.dart",
  "application/vnd.dbf",
  "application/vnd.debian.binary-package",
  "application/vnd.exstream-package",
  "application/vnd.flatpak",
  "application/vnd.font-fontforge-sfd",
  "application/vnd.gentoo.ebuild",
  "application/vnd.geo+json",
  "application/vnd.google-earth.kml+xml",
  "application/vnd.google-earth.kmz",
  "application/vnd.google.protobuf",
  "application/vnd.groove-tool-template",
  "application/vnd.ibm.secure-container",
  "application/vnd.ipld.car",
  "application/vnd.isac.fcs",
  "application/vnd.koan",
  "application/vnd.las",
  "application/vnd.laszip",
  "application/vnd.lotus-screencam",
  "application/vnd.lotus-wordpro",
  "application/vnd.microsoft.portable-executable",
  "application/vnd.ms-3mfdocument",
  "application/vnd.ms-appx",
  "application/vnd.ms-asf",
  "application/vnd.ms-cab-compressed",
  "application/vnd.ms-excel",
  "application/vnd.ms-excel.sheet.binary.macroEnabled.12",
  "application/vnd.ms-excel.sheet.macroEnabled.12",
  "application/vnd.ms-fontobject",
  "application/vnd.ms-htmlhelp",
  "application/vnd.ms-pki.seccat",
  "application/vnd.ms-powerpoint",
  "application/vnd.ms-powerpoint.presentation.macroEnabled.12",
  "application/vnd.ms-powerpoint.slideshow.macroEnabled.12",
  "application/vnd.ms-word.document.macroEnabled.12",
  "application/vnd.ms-word.template.macroEnabled.12",
  "application/vnd.ms-works",
  "application/vnd.ms-xpsdocument",
  "application/vnd.nintendo.nitro.rom",
  "application/vnd.nintendo.snes.rom",
  "application/vnd.npm",
  "application/vnd.oasis.opendocument.base",
  "application/vnd.oasis.opendocument.chart-template",
  "application/vnd.oasis.opendocument.graphics",
  "application/vnd.oasis.opendocument.graphics-template",
  "application/vnd.oasis.opendocument.presentation",
  "application/vnd.oasis.opendocument.presentation-template",
  "application/vnd.oasis.opendocument.spreadsheet",
  "application/vnd.oasis.opendocument.spreadsheet-template",
  "application/vnd.oasis.opendocument.text",
  "application/vnd.oasis.opendocument.text-template",
  "application/vnd.openstreetmap.data+xml",
  "application/vnd.openxmlformats-officedocument.presentationml.presentation",
  "application/vnd.openxmlformats-officedocument.presentationml.slideshow",
  "application/vnd.openxmlformats-officedocument.presentationml.template",
  "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  "application/vnd.openxmlformats-officedocument.spreadsheetml.template",
  "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
  "application/vnd.openxmlformats-officedocument.wordprocessingml.template",
  "application/vnd.previewsystems.box",
  "application/vnd.python.wheel",
  "application/vnd.rar",
  "application/vnd.realvnc.bed",
  "application/vnd.shp",
  "application/vnd.shx",
  "application/vnd.sketchometry",
  "application/vnd.sqlite3",
  "application/vnd.sybyl.mol2",
  "application/vnd.tcpdump.pcap",
  "application/vnd.theqvd",
  "application/vnd.visio",
  "application/vnd.wolfram.mathematica",
  "application/vnd.wolfram.mathematica.package",
  "application/vnd.wordperfect",
  "application/vnd.xara",
  "application/wasm",
  "application/x-7z-compressed",
  "application/x-appimage",
  "application/x-apple-diskimage",
  "application/x-archive",
  "application/x-arj",
  "application/x-avro",
  "application/x-bat",
  "application/x-bibtex",
  "application/x-blender",
  "application/x-brotli",
  "application/x-bytecode.python",
  "application/x-bzip",
  "application/x-bzip2",
  "application/x-cab",
  "application/x-cdf",
  "application/x-chm",
  "application/x-cmd",
  "application/x-compress",
  "application/x-core",
  "application/x-coredump",
  "application/x-cpio",
  "application/x-crashdump",
  "application/x-debian-package",
  "application/x-desktop",
  "application/x-doom",
  "application/x-dosexec",
  "application/x-elf",
  "application/x-executable",
  "application/x-fictionbook+xml",
  "application/x-font",
  "application/x-font-bdf",
  "application/x-font-otf",
  "application/x-font-pcf",
  "application/x-font-snf",
  "application/x-font-ttf",
  "application/x-font-type1",
  "application/x-freemind",
  "application/x-gtar",
  "application/x-gzip",
  "application/x-hcl",
  "application/x-hdf",
  "application/x-hdf5",
  "application/x-httpd-php",
  "application/x-icns",
  "application/x-ico",
  "application/x-iso9660-image",
  "application/x-java-archive",
  "application/x-java-class",
  "application/x-json5",
  "application/x-latex",
  "application/x-lha",
  "application/x-lzh",
  "application/x-lzip",
  "application/x-lzma",
  "application/x-mach-binary",
  "application/x-maker",
  "application/x-matlab-data",
  "application/x-mobipocket-ebook",
  "application/x-ms-installer",
  "application/x-ms-shortcut",
  "application/x-msdos-program",
  "application/x-msi",
  "application/x-ndjson",
  "application/x-netcdf",
  "application/x-nupkg",
  "application/x-object",
  "application/x-openvpn-profile",
  "application/x-parquet",
  "application/x-pcapng",
  "application/x-pem-file",
  "application/x-perl",
  "application/x-php",
  "application/x-pickle",
  "application/x-pkcs12",
  "application/x-powershell",
  "application/x-protobuf",
  "application/x-python-code",
  "application/x-qgis",
  "application/x-rar",
  "application/x-rar-compressed",
  "application/x-rdp",
  "application/x-redhat-package-manager",
  "application/x-registry",
  "application/x-rpm",
  "application/x-rss+xml",
  "application/x-ruby",
  "application/x-sh",
  "application/x-shapefile",
  "application/x-sharedlib",
  "application/x-shellscript",
  "application/x-silverlight",
  "application/x-snap",
  "application/x-sql",
  "application/x-sqlite3",
  "application/x-stuffit",
  "application/x-subrip",
  "application/x-systemd-unit",
  "application/x-tar",
  "application/x-terraform",
  "application/x-tex",
  "application/x-theme",
  "application/x-toml",
  "application/x-trash",
  "application/x-troff-me",
  "application/x-wais-source",
  "application/x-wheel+zip",
  "application/x-wine-extension-ini",
  "application/x-www-form-urlencoded",
  "application/x-x509-ca-cert",
  "application/x-x509-user-cert",
  "application/x-xar",
  "application/x-xfig",
  "application/x-xz",
  "application/x-yaml",
  "application/x-zip",
  "application/x-zstd",
  "application/xhtml+xml",
  "application/xml-dtd",
  "application/xslt+xml",
  "application/yaml",
  "application/yang",
  "application/zstd",
  "audio/*",
  "audio/3gpp",
  "audio/3gpp2",
  "audio/aac",
  "audio/ac3",
  "audio/aiff",
  "audio/amr",
  "audio/basic",
  "audio/csound",
  "audio/flac",
  "audio/matroska",
  "audio/midi",
  "audio/mp4",
  "audio/mpeg",
  "audio/mpegurl",
  "audio/ogg",
  "audio/opus",
  "audio/sp-midi",
  "audio/vnd.dts",
  "audio/vnd.dts.hd",
  "audio/wav",
  "audio/webm",
  "audio/x-aiff",
  "audio/x-flac",
  "audio/x-matroska",
  "audio/x-midi",
  "audio/x-ms-wma",
  "audio/x-pn-realaudio",
  "audio/x-realaudio",
  "audio/x-scpls",
  "audio/x-wav",
  "chemical/x-chemdraw",
  "chemical/x-cif",
  "chemical/x-galactic-spc",
  "chemical/x-mdl-molfile",
  "chemical/x-mdl-sdfile",
  "chemical/x-pdb",
  "chemical/x-xyz",
  "font/*",
  "font/collection",
  "font/otf",
  "font/ttf",
  "font/woff",
  "font/woff2",
  "image/*",
  "image/aces",
  "image/apng",
  "image/avif",
  "image/bmp",
  "image/emf",
  "image/fits",
  "image/gif",
  "image/heic",
  "image/heif",
  "image/jpeg",
  "image/jxl",
  "image/ktx",
  "image/ktx2",
  "image/openraster",
  "image/png",
  "image/svg+xml",
  "image/tiff",
  "image/vnd.adobe.photoshop",
  "image/vnd.djvu",
  "image/vnd.dwg",
  "image/vnd.dxf",
  "image/vnd.microsoft.icon",
  "image/vnd.radiance",
  "image/vnd.tencent.tap",
  "image/webp",
  "image/wmf",
  "image/x-canon-cr2",
  "image/x-coreldraw",
  "image/x-dds",
  "image/x-icon",
  "image/x-ms-bmp",
  "image/x-nikon-nef",
  "image/x-olympus-orf",
  "image/x-photoshop",
  "image/x-portable-anymap",
  "image/x-portable-bitmap",
  "image/x-portable-graymap",
  "image/x-portable-pixmap",
  "image/x-tga",
  "image/x-xbitmap",
  "image/x-xcf",
  "image/x-xpixmap",
  "message/rfc822",
  "model/*",
  "model/gltf+json",
  "model/gltf-binary",
  "model/iges",
  "model/mtl",
  "model/obj",
  "model/step",
  "model/stl",
  "model/vnd.collada+xml",
  "model/vnd.gdl",
  "model/vnd.mts",
  "model/vnd.usda",
  "model/vnd.usdz+zip",
  "model/vnd.valve.source.compiled-map",
  "model/vrml",
  "model/x3d+binary",
  "model/x3d+vrml",
  "model/x3d+xml",
  "text/cache-manifest",
  "text/calendar",
  "text/css",
  "text/csv",
  "text/directory",
  "text/ecmascript",
  "text/gff3",
  "text/html",
  "text/javascript",
  "text/markdown",
  "text/plain",
  "text/prs.fallenstein.rst",
  "text/richtext",
  "text/rtf",
  "text/tab-separated-values",
  "text/troff",
  "text/vcard",
  "text/vnd.abc",
  "text/vnd.graphviz",
  "text/vnd.in3d.3dml",
  "text/vnd.trolltech.linguist",
  "text/vtt",
  "text/x-bibtex",
  "text/x-c",
  "text/x-c++hdr",
  "text/x-c++src",
  "text/x-chdr",
  "text/x-config",
  "text/x-csh",
  "text/x-csharp",
  "text/x-csrc",
  "text/x-diff",
  "text/x-dsrc",
  "text/x-go",
  "text/x-haskell",
  "text/x-ini",
  "text/x-java",
  "text/x-java-source",
  "text/x-literate-haskell",
  "text/x-log",
  "text/x-lua",
  "text/x-markdown",
  "text/x-pascal",
  "text/x-patch",
  "text/x-perl",
  "text/x-php",
  "text/x-python",
  "text/x-rst",
  "text/x-ruby",
  "text/x-rust",
  "text/x-scala",
  "text/x-script.perl",
  "text/x-script.python",
  "text/x-script.ruby",
  "text/x-script.sh",
  "text/x-sfv",
  "text/x-sh",
  "text/x-shellscript",
  "text/x-sql",
  "text/x-tex",
  "text/x-toml",
  "text/x-vcard",
  "text/x-yaml",
  "text/xml",
  "text/yaml",
  "video/*",
  "video/3gpp",
  "video/3gpp2",
  "video/av1",
  "video/dv",
  "video/h264",
  "video/h265",
  "video/hevc",
  "video/matroska",
  "video/mp2t",
  "video/mp4",
  "video/mpeg",
  "video/ogg",
  "video/quicktime",
  "video/vnd.nokia.interleaved-multimedia",
  "video/vnd.planar",
  "video/webm",
  "video/x-flv",
  "video/x-matroska",
  "video/x-ms-wmv",
  "video/x-msvideo"
];
        public const string OllamaModeAutoGpu = "auto-gpu";
        public const string OllamaModeSafeCpu = "safe-cpu";
        public const string OllamaModeLimitedGpu = "limited-gpu";
 
        public const string DetectedOllamaSessionPrefix = "Ollama detected — ";
        public static string DefaultOllamaEndpoint { get; set; }= "http://127.0.0.1:11434";
        public static readonly string[] ArchitectureUiStackOptions =
[
    "Ask me before choosing UI stack",
        "DevExpress Blazor components",
        "Plain Blazor components",
        "No UI / backend or tool only",
        "Other target-specific UI"
];
        public static readonly string[] ArchitectureSolutionShapeOptions =
        [
            "Ask me before choosing solution shape",
        "Single cohesive solution",
        "Split backend and frontend projects",
        "Library/plugin/package only",
        "Datapack/mod workspace only"
        ];
        public static readonly string[] ArchitectureRenderModeOptions =
        [
            "Ask me before choosing runtime/rendering",
        "Blazor Server / InteractiveServer",
        "Blazor WebAssembly with ASP.NET Core backend",
        "Static SSR plus interactive islands",
        "ASP.NET Core API / backend only",
        "Desktop wrapper / WebView2",
        "Minecraft Java/datapack runtime",
        "CLI/tooling runtime"
        ];
        public static readonly string[] ArchitectureReferenceLookOptions =
        [
            "Ask me before choosing visual fidelity",
        "Recreate the goal app look closely",
        "Use LocalGPT style but preserve goal app structure",
        "Functional prototype first",
        "No visual reference"
        ];
        public const int DefaultMaxOutputTokens = 262144;
        public const int DefaultMaxPromptCharacters = int.MaxValue;
        public const int MaxPromptCharacters = int.MaxValue;
        public const int MaxBootstrapCharacters = 6000;
        public const int MaxSingleConversationMessageCharacters = int.MaxValue;
        public const string RuntimeDecisionPolicy =
            "LocalGPT runtime decision policy: When the user asks to generate, scaffold, implement, modify, or package code/artifacts and important architecture choices are unresolved, do not start coding yet. " +
            "First return a short section titled \"Decision poll required\" with concrete choices and tradeoffs, then stop and wait for the user's answer. " +
            "Ask only for decisions that materially affect the result, such as target platform/runtime, language/framework, UI stack, solution shape, data/persistence model, deployment target, security boundary, reference-app fidelity, and whether downloadable artifacts are expected. " +
            "If the user explicitly asks for a Minecraft datapack/modpack zip, .cs/.razor/.dll files, a whole .NET solution zip, a local AI host control-plane app, or another concrete downloadable artifact, treat that as supplied scope and generate a safe milestone artifact rather than refusing because the task is large. " +
            "Never claim the user failed to answer a poll inside the same response that created it; a poll pauses the next step until the next user turn unless the prompt already supplied a concrete artifact target. " +
            "Do not assume Blazor, DevExpress, ASP.NET Core, or a split frontend/backend unless the user selected it, the existing repository requires it, or the requested target clearly calls for it. " +
            "If the user already supplied the needed decisions, proceed normally and restate the selected path briefly. " +
            "If LocalGPT lacks a function, source, version map, or domain knowledge needed to fulfill the request, add a \"Capability gap report\" and a <localgpt-capability-gap> block with requested languages, frameworks, versions, domain knowledge, local sources, external official sources, missing LocalGPT functions, safe workflow, and artifact plan.";
        public enum GeneratedSolutionArchetype
        {
            Generic,
            LocalGpt,
            TacosPortal,
            BotBackend,
            AiHost
        }
        public const string HarmonyResponseProtocol =
           "Response protocol for Harmony/OpenAI-style local models: keep analysis short, " +
           "emit user-visible final answer text early in the final channel, never spend the whole budget on analysis, and if the request is too large, " +
           "say what is missing or what to do next in final instead of spending the whole answer budget on analysis.";
        public const string MissingFinalAnswerNotice =
            "**No final answer was emitted.** The model only sent thinking. LocalGPT kept the thinking visible and stopped the spinner; " +
            "send a short \"continue with the final answer\" request or raise the answer-token budget for this model.";

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
