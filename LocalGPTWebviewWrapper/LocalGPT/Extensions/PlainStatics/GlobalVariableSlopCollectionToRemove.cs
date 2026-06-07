using Markdig;
using System.Text.RegularExpressions;

namespace LocalGPT.Extensions.PlainStatics
{
    public static class GlobalVariableSlopCollectionToRemove
    {
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
    }
}
