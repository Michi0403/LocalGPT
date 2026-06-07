using Markdig;
using System.Text.RegularExpressions;

namespace LocalGPT.Extensions.PlainStatics
{
    public static partial class GlobalVariableSlopCollectionToRemove
    {

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

        [GeneratedRegex("(review|code review|diagnose|diagnostic|release readiness|readiness|go or no-go|blockers|evidence|what failed|why failed|build/deploy/package/publish|publish cycle|release cycle|maintenance cycle)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex AdviceOnlyPromptPattern();

        [GeneratedRegex("(generate|create|produce|write|implement|make|build)\\b.{0,120}\\b(downloadable|artifact|zip|solution|source code|\\.sln|\\.csproj|\\.cs\\b|\\.razor\\b|ai host|localgpt replacement|application|app|datapack|modpack)\\b|\\b(downloadable|artifact|zip|solution)\\b.{0,120}\\b(generate|create|produce|write|implement|make|build)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
        public static partial Regex ExplicitArtifactCreationCommandPattern();

        [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
        public static partial Regex WhitespacePattern();

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
        public const int MaxUploadFiles = 12;
        public const int MaxUploadBytes = 32 * 1024 * 1024;
        public static readonly List<string> AllowedUploadExtensions =
        [
            ".txt", ".md", ".json", ".xml", ".csv", ".cs", ".razor", ".cshtml", ".css", ".scss",
        ".js", ".ts", ".tsx", ".html", ".htm", ".xaml", ".sln", ".csproj", ".props",
        ".targets", ".config", ".editorconfig", ".yml", ".yaml", ".toml", ".sql", ".ps1",
        ".cmd", ".bat", ".sh", ".java", ".kt", ".gradle", ".mcfunction", ".mcmeta",
        ".properties", ".zip", ".dll", ".exe", ".pdb", ".appxsym", ".nupkg", ".wasm"
        ];
        public static readonly List<string> AllowedUploadMimeTypes =
        [
            "text/*",
        "application/json",
        "application/xml",
        "application/zip",
        "application/x-zip-compressed",
        "application/octet-stream",
        "application/x-msdownload"
        ];
        public const string OllamaModeAutoGpu = "auto-gpu";
        public const string OllamaModeSafeCpu = "safe-cpu";
        public const string OllamaModeLimitedGpu = "limited-gpu";
 
        public const string DetectedOllamaSessionPrefix = "Ollama detected — ";
      
        public const string DefaultOllamaEndpoint = "http://127.0.0.1:11434";
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
        public const int DefaultMaxOutputTokens = 65536;
        public const int DefaultMaxPromptCharacters = 250000;
        public const int MaxPromptCharacters = 1_000_000;
        public const int MaxBootstrapCharacters = 6000;
        public const int MaxSingleConversationMessageCharacters = 5000;
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

        public sealed record GeneratedArchetypePage(string FileName, string Source);

        public sealed record GeneratedPromiseModule(
            string FileName,
            string Route,
            string Title,
            string Summary,
            IReadOnlyList<string> Areas);

    }
}
