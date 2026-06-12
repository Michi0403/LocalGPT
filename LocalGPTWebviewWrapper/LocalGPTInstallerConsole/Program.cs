
using LocalGPT.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

internal static class Program
{
    private const string LocalGptRepo = "Michi0403/LocalGPT";
    private const string LocalGptZipName = "LocalGPTByMichi0403.zip";
    private static readonly HttpClient Http = CreateHttpClient();

    private static readonly string[] SlimModels =
    [
        "gpt-oss:20b",
        "gemma3:27b",
        "deepseek-r1:8b",
        "qwen3-coder:30b",
        "llama2-uncensored:7b"
    ];

    private static readonly string[] Rtx3060Models =
    [
        "qwen3.5:0.8b", "qwen3.5:2b", "qwen3.5:4b", "qwen3.5:9b",
        "gpt-oss:20b",
        "llama3.1:8b", "llama3.2:1b", "llama3.2:3b",
        "gemma3:4b", "gemma3:12b",
        "qwen3:1.7b", "qwen3:4b", "qwen3:8b", "qwen3:14b",
        "phi3:3.8b", "phi3:14b",
        "deepseek-coder:6.7b",
        "dolphin3:8b",
        "codegemma:2b", "codegemma:7b",
        "gemma4:e2b", "gemma4:e4b", "gemma4:12b",
        "llama3:8b", "llama3.2-vision:11b",
        "llama2:7b", "llama2:13b", "llama2-uncensored:7b",
        "llama-guard3:1b", "llama-guard3:8b",
        "deepseek-ocr:3b",
        "deepseek-r1:1.5b", "deepseek-r1:7b", "deepseek-r1:8b", "deepseek-r1:14b",
        "deepseek-coder-v2:16b", "deepseek-v2:16b",
        "deepscaler:1.5b",
        "openthinker:7b"
    ];

    private static readonly string[] FullModels =
    [
        "qwen3.5:0.8b", "qwen3.5:2b", "qwen3.5:4b", "qwen3.5:9b", "qwen3.5:27b", "qwen3.5:35b",
        "gpt-oss:20b",
        "llama3.1:8b", "llama3.2:1b", "llama3.2:3b",
        "gemma3:4b", "gemma3:12b", "gemma3:27b",
        "qwen3:1.7b", "qwen3:4b", "qwen3:8b", "qwen3:14b", "qwen3:30b", "qwen3:32b",
        "phi3:3.8b", "phi3:14b",
        "deepseek-coder:6.7b", "deepseek-coder:33b",
        "dolphin3:8b",
        "codegemma:2b", "codegemma:7b",
        "laguna-xs.2:nvfp4", "laguna-xs.2:q4_K_M",
        "qwen3.6:27b", "qwen3.6:35b",
        "gemma4:e2b", "gemma4:e4b", "gemma4:12b", "gemma4:26b", "gemma4:31b",
        "llama3:8b", "llama3.2-vision:11b",
        "llama2:7b", "llama2:13b", "llama2-uncensored:7b",
        "llama-guard3:1b", "llama-guard3:8b",
        "deepseek-ocr:3b",
        "deepseek-r1:1.5b", "deepseek-r1:7b", "deepseek-r1:8b", "deepseek-r1:14b", "deepseek-r1:32b",
        "deepseek-coder-v2:16b", "deepseek-v2:16b",
        "deepscaler:1.5b",
        "openthinker:7b", "qwen3-coder:30b", "openthinker:32b"
    ];

    private static readonly string[] RecommendedRepos =
    [
        "Michi0403/LocalGPT",
        "TelegramBots/Telegram.Bot",
        "Michi0403/TacosPortalOpen",
        "Michi0403/OpenMorph.NET",
        "Michi0403/AutomatedDiscordLogin",
        "Michi0403/3DOpenScad",
        "dotnet/docs",
        "MicrosoftDocs/windows-dev-docs",
        "MicrosoftDocs/microsoftgraph-docs-powershell",
        "Mojang/bedrock-samples",
        "Mojang/bedrock-protocol-docs",
        "Mojang/minecraft-editor",
        "Mojang/minecraft-debugger",
        "Mojang/minecraft-editor-extension-starter-kit",
        "Mojang/minecraft-editor-extension-samples",
        "Mojang/minecraft-scripting-libraries",
        "Mojang/minecraft-creator-tools",
        "Mojang/bedrock-schemas",
        "DevExpress/Blazor",
        "DevExpress/DevExtreme",
        "DevExpress/devextreme-documentation",
        "DevExpress-Examples/XAF_Security_E4908"
    ];

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        FileLoggerCoreOptions fileLoggerProviderOptions = new FileLoggerCoreOptions() { CoreLogLevel = CoreLogLevel.Debug, FilePath = Path.Combine(Environment.CurrentDirectory, "installlog.log") };
        FileLoggerProvider fileLoggerProvider = new FileLoggerProvider(fileLoggerProviderOptions);
        ColorConsoleLoggerConfiguration colorLoggerProviderOptions = new ColorConsoleLoggerConfiguration() { EventId = 0 };
        ColorConsoleLoggerProvider colorLoggerProvider = new ColorConsoleLoggerProvider(colorLoggerProviderOptions);


        using var loggerFactory = LoggerFactory.Create(configure => 
        {
            configure.ClearProviders();
            configure.AddProvider(colorLoggerProvider);
            configure.AddProvider(fileLoggerProvider);
            //configure.AddProvider()
        });
        var logger = loggerFactory.CreateLogger("Startup");
        logger.LogInformation("Configured app configuration.");

        var options = CliOptions.Parse(args);
        if (options.ShowHelp)
        {
            CliOptions.PrintHelp(logger);
            return 0;
        }

        try
        {
            try
            {
                if (options.InstallOllama)
                {
                    logger.LogInformation("InstallOllamaAsync.");
                    await InstallOllamaAsync(options, logger);
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex,"Error in InstallOllama.");
            }

            try
            {
                if (options.PullOllamaModels)
                {
                    EnsureOllamaAvailable(options, logger);
                    StartOllamaServer( logger);
                    await PullModelsAsync(GetModelSet(options.Range), logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex,"Error in PullOllamaModels.");
            }

            try
            {
                if (options.InstallLocalGptWin)
                    await InstallLocalGptAsync(options, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,"Error in InstallLocalGptWin.");
            }

            try
            {
                if (options.SetupLearningBase)
                {
                    Directory.CreateDirectory(options.LearningBasePath);
                    var repos = options.ImportRecommended
                        ? RecommendedRepos.Concat(options.ExtraRepos).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                        : options.ExtraRepos.Count > 0 ? options.ExtraRepos.ToArray() : [LocalGptRepo];

                    foreach (var repo in repos)
                        await ImportGitHubSourceToLearningBaseAsync(repo, options , logger);
                    logger.LogInformation("Remember: still import/teach the downloaded repositories inside LocalGPT's learning-base importer.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex,"Error in SetupLearningBase.");
            }

            try
            {
                if (options.StartLocalGpt)
                    StartLocalGpt(options , logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,"Error in StartLocalGpt.");
            }


            logger.LogDebug("Done.");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,$"Error in Setup: {ex.ToString()}");
            if (options.Verbose)
                logger.LogWarning(ex.ToString());
            return 1;
        }
    }

    private static async Task InstallOllamaAsync(CliOptions options, ILogger logger)
    {
        try
        {
            var installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ollama", "ollama.exe");
            if (File.Exists(installPath))
            {
                logger.LogDebug("Ollama already appears to be installed.");
                return;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new InvalidOperationException("The built-in Ollama installer path currently targets Windows only.");

            var installer = Path.Combine(Path.GetTempPath(), "OllamaSetup.exe");
            await DownloadFileAsync("https://ollama.com/download/OllamaSetup.exe", installer,logger);

            logger.LogInformation("Running official Ollama Windows installer...");
            await RunProcessAsync(installer, string.Empty,logger);
            logger.LogInformation("Ollama installer finished. Check the output above if Windows got dramatic.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex,$"Error in InstallOllamaAsync. options {options.ToString()}");
        }
    }

    private static void EnsureOllamaAvailable(CliOptions options, ILogger logger)
    {
        try
        {
            if (CommandExists("ollama", logger))
                return;

            var defaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ollama");
            var exe = Path.Combine(defaultDir, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ollama.exe" : "ollama");

            if (!File.Exists(exe))
                throw new FileNotFoundException($"Ollama was not found in PATH or at '{exe}'. Install Ollama first or add it to PATH.");

            var currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
            if (!currentPath.Split(Path.PathSeparator).Any(p => string.Equals(p.TrimEnd(Path.DirectorySeparatorChar), defaultDir, StringComparison.OrdinalIgnoreCase)))
            {
                var newPath = string.IsNullOrWhiteSpace(currentPath) ? defaultDir : currentPath + Path.PathSeparator + defaultDir;
                Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + Path.PathSeparator + defaultDir);
                logger.LogInformation($"Added Ollama to user PATH: {defaultDir}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in EnsureOllamaAvailable. options {options.ToString()}");
        }
    }

    private static void StartOllamaServer(ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting Ollama server if it is not already running...");
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ollama",
                    ArgumentList = { "serve" },
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,$"Could not start 'ollama serve'. Continuing anyway: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in StartOllamaServer.");
        }
    }

    private static async Task PullModelsAsync(string[] models, ILogger logger)
    {
        try
        {
            foreach (var model in models)
            {
                logger.LogInformation($"Pulling {model}");
                await RunProcessAsync("ollama", $"pull {model}",logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in PullModelsAsync. models {models.ToString()}");
        }

    }

    private static async Task InstallLocalGptAsync(CliOptions options, ILogger logger)
    {
        try
        {
            var zipPath = options.LocalGptZipPath ?? Path.Combine(Environment.CurrentDirectory, LocalGptZipName);
            await DownloadLatestReleaseAssetAsync(LocalGptRepo, zipPath, logger);

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localAppData))
                throw new InvalidOperationException("LOCALAPPDATA could not be resolved.");

            var targetPath = Path.Combine(localAppData, "LocalGPT");
            RemoveIfExists(targetPath, options.Force, logger);

            logger.LogInformation($"Extracting '{zipPath}' to '{targetPath}'");
            ExtractZipWithFallback(zipPath, targetPath, logger);
            logger.LogDebug($"LocalGPT installed to '{targetPath}'.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in InstallLocalGptAsync. options {options.ToString()}");
        }
       
    }

    private static async Task ImportGitHubSourceToLearningBaseAsync(string repo, CliOptions options, ILogger logger)
    {
        try
        {
            ValidateRepo(repo, logger);
            var cleanName = SanitizeFileName(repo, logger);
            var targetPath = Path.Combine(options.LearningBasePath, cleanName);
            var zipPath = targetPath + ".zip";

            logger.LogInformation($"Downloading GitHub source: {repo}");
            await DownloadGitHubSourceZipAsync(repo, zipPath, logger);

            RemoveIfExists(targetPath, options.Force, logger);

            logger.LogInformation($"Extracting '{zipPath}' to '{targetPath}'");
            ExtractZipWithFallback(zipPath, targetPath, logger);
            logger.LogDebug($"Imported {repo}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in ImportGitHubSourceToLearningBaseAsync. repo {repo.ToString()} options {options.ToString()}");
        }
    }

    private static void StartLocalGpt(CliOptions options, ILogger logger)
    {
        try
        {
            var exePath = options.LocalGptExePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocalGPT", "LocalGPT.exe");
            if (!File.Exists(exePath))
                throw new FileNotFoundException($"LocalGPT executable not found at '{exePath}'. Install it first or pass --localgpt-exe.");

            logger.LogInformation($"Starting {exePath}");
            Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in StartLocalGpt. options {options.ToString()}");
        }
    }

    private static async Task DownloadLatestReleaseAssetAsync(string repo, string outFile, ILogger logger)
    {
        try
        {
            ValidateRepo(repo, logger);
            var latestUrl = $"https://api.github.com/repos/{repo}/releases/latest";
            using var stream = await Http.GetStreamAsync(latestUrl);
            using var json = await JsonDocument.ParseAsync(stream);

            var root = json.RootElement;
            var tagName = root.TryGetProperty("tag_name", out var tag) ? tag.GetString() : "unknown";
            logger.LogInformation($"Latest {repo} release: {tagName}");

            if (!root.TryGetProperty("assets", out var assets) || assets.GetArrayLength() == 0)
                throw new InvalidOperationException($"No downloadable release assets found for {repo}.");

            var platform = GetPlatformToken();
            var arch = GetArchitectureToken();

            JsonElement? selected = null;
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? string.Empty;
                if (name.Contains(platform, StringComparison.OrdinalIgnoreCase) && name.Contains(arch, StringComparison.OrdinalIgnoreCase))
                {
                    selected = asset;
                    break;
                }
            }

            selected ??= assets.EnumerateArray().First();
            var downloadUrl = selected.Value.GetProperty("browser_download_url").GetString();
            var assetName = selected.Value.GetProperty("name").GetString();

            if (string.IsNullOrWhiteSpace(downloadUrl))
                throw new InvalidOperationException($"Selected release asset for {repo} has no download URL.");

            logger.LogInformation($"Downloading {assetName} to {outFile}");
            await DownloadFileAsync(downloadUrl, outFile, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in DownloadLatestReleaseAssetAsync. repo {repo.ToString()} outFile {outFile.ToString()}");
        }
      
    }

    private static async Task DownloadGitHubSourceZipAsync(string repo, string outFile, ILogger logger)
    {
        try
        {
            ValidateRepo(repo, logger);
            var url = $"https://api.github.com/repos/{repo}/zipball";
            await DownloadFileAsync(url, outFile, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in DownloadGitHubSourceZipAsync. repo {repo.ToString()} outFile {outFile.ToString()}");
        }
    }

    private static async Task DownloadFileAsync(string url, string outFile, ILogger logger)
    {
        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(outFile));
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            if (File.Exists(outFile))
                File.Delete(outFile);

            using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            await using var input = await response.Content.ReadAsStreamAsync();
            await using var output = File.Create(outFile);
            await input.CopyToAsync(output);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in DownloadFileAsync. url {url.ToString()} outFile {outFile.ToString()}");
        }
      
    }

    private static void RemoveIfExists(string path, bool force, ILogger logger)
    {
        try
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                return;

            if (!force)
                throw new IOException($"'{path}' already exists. Re-run with --force to delete it first.");

            logger.LogInformation($"Deleting existing path: {path}");
            var attrs = File.GetAttributes(path);
            if (attrs.HasFlag(FileAttributes.Directory))
                Directory.Delete(path, recursive: true);
            else
                File.Delete(path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in RemoveIfExists. path {path.ToString()} force {force.ToString()}");
        }
    }

    private static void ExtractZipWithFallback(string zipPath, string targetPath, ILogger logger)
    {
        try
        {
            Directory.CreateDirectory(targetPath);
            try
            {
                ZipFile.ExtractToDirectory(zipPath, targetPath, overwriteFiles: true);
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,$".NET ZIP extraction failed: {ex.Message}");
            }

            var sevenZip = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe");
            if (!File.Exists(sevenZip))
                throw new InvalidOperationException("ZIP extraction failed and 7-Zip was not found. Install 7-Zip or enable long paths.");

            RunProcessAsync(sevenZip, $"x \"{zipPath}\" -o\"{targetPath}\" -y", logger).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in ExtractZipWithFallback. zipPath {zipPath.ToString()} targetPath {targetPath.ToString()}");
        }
    }

    private static async Task RunProcessAsync(string fileName, string arguments, ILogger logger)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            };

            process.OutputDataReceived += (_, e) => { if (e.Data is not null) logger.LogInformation(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) logger.LogError(e.Data); };

            if (!process.Start())
                throw new InvalidOperationException($"Could not start process: {fileName}");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Command failed with exit code {process.ExitCode}: {fileName} {arguments}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in RunProcessAsync. fileName {fileName.ToString()} arguments {arguments.ToString()}");
        }
       
    }

    private static bool CommandExists(string command, ILogger logger)
    {
        try
        {
            var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            var extensions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.BAT;.CMD").Split(';')
                : [string.Empty];

            return paths.Any(path => extensions.Any(ext => File.Exists(Path.Combine(path, command + ext))));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in CommandExists. command {command.ToString()}");
            return false;
        }
    
    }

    private static string[] GetModelSet(ModelRange range) => range switch
    {

        ModelRange.Slim => SlimModels,
        ModelRange.RTX3060 => Rtx3060Models,
        ModelRange.Full => FullModels,
        _ => SlimModels
    };

    private static string GetPlatformToken()
    {

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "win";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macosx";
        return "";
    }

    private static string GetArchitectureToken() => RuntimeInformation.OSArchitecture switch
    {
        Architecture.X64 => "x64",
        Architecture.X86 => "x86",
        Architecture.Arm => "arm",
        Architecture.Arm64 => "arm64",
        _ => ""
    };

    private static string SanitizeFileName(string value, ILogger logger)
    {
        try
        {
            var invalid = Path.GetInvalidFileNameChars().Concat(['/', '\\', ':', '*', '?', '"', '<', '>', '|']).Distinct().ToArray();
            foreach (var ch in invalid)
                value = value.Replace(ch, '_');
            return value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in SanitizeFileName. value {value.ToString()}");
            return string.Empty;
        }
   
    }

    private static void ValidateRepo(string repo, ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(repo) || repo.Count(c => c == '/') != 1)
                throw new ArgumentException($"Invalid GitHub repo '{repo}'. Expected format: owner/repository");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in ValidateRepo. repo {repo.ToString()}");
        }
    
    }

    private static HttpClient? CreateHttpClient( )
    {
        try
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LocalGptSetupTool", "1.0"));
            client.Timeout = TimeSpan.FromMinutes(20);
            return client;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CreateHttpClient. {ex.ToString()}");
            return null;
        }
    }
}

internal enum ModelRange
{
    Slim,
    RTX3060,
    Full
}

internal sealed class CliOptions
{
    public bool ShowHelp { get; private set; }
    public bool InstallOllama { get; private set; }
    public bool PullOllamaModels { get; private set; }
    public bool InstallLocalGptWin { get; private set; }
    public bool SetupLearningBase { get; private set; }
    public bool ImportRecommended { get; private set; }
    public bool StartLocalGpt { get; private set; }
    public bool Force { get; private set; }
    public bool Verbose { get; private set; }
    public ModelRange Range { get; private set; } = ModelRange.Slim;
    public string LearningBasePath { get; private set; } = @"C:\learnbaseforlocalgpt";
    public string? LocalGptZipPath { get; private set; }
    public string? LocalGptExePath { get; private set; }
    public List<string> ExtraRepos { get; } = [];

    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg.ToLowerInvariant())
            {
                case "-h":
                case "--help":
                case "/?":
                    options.ShowHelp = true;
                    break;
                case "--install-ollama":
                    options.InstallOllama = true;
                    break;
                case "--pull-models":
                    options.PullOllamaModels = true;
                    break;
                case "--install-localgpt":
                    options.InstallLocalGptWin = true;
                    break;
                case "--setup-learning-base":
                    options.SetupLearningBase = true;
                    break;
                case "--import-recommended":
                    options.ImportRecommended = true;
                    options.SetupLearningBase = true;
                    break;
                case "--start-localgpt":
                    options.StartLocalGpt = true;
                    break;
                case "--force":
                case "-f":
                    options.Force = true;
                    break;
                case "--verbose":
                    options.Verbose = true;
                    break;
                case "--all":
                    options.InstallOllama = true;
                    options.PullOllamaModels = true;
                    options.InstallLocalGptWin = true;
                    options.SetupLearningBase = true;
                    options.ImportRecommended = true;
                    options.StartLocalGpt = true;
                    break;
                case "--range":
                    options.Range = ParseEnum<ModelRange>(NextValue(args, ref i, arg));
                    break;
                case "--learnbase":
                    options.LearningBasePath = NextValue(args, ref i, arg);
                    break;
                case "--repo":
                    options.ExtraRepos.Add(NextValue(args, ref i, arg));
                    options.SetupLearningBase = true;
                    break;
                case "--localgpt-zip":
                    options.LocalGptZipPath = NextValue(args, ref i, arg);
                    break;
                case "--localgpt-exe":
                    options.LocalGptExePath = NextValue(args, ref i, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}. Use --help.");
            }
        }

        if (args.Length == 0)
            options.ShowHelp = true;

        return options;
    }

    public static void PrintHelp(ILogger logger)
    {
        logger.LogInformation("""
LocalGPT setup helper

Usage:
  localgpt-setup [options]

Common examples:
  localgpt-setup --install-ollama
  localgpt-setup --pull-models --range RTX3060
  localgpt-setup --install-localgpt --force
  localgpt-setup --setup-learning-base --repo Michi0403/LocalGPT --force
  localgpt-setup --import-recommended --force
  localgpt-setup --all --range Slim --force

Options:
  --install-ollama           Install Ollama by running the official Windows install script.
  --pull-models              Pull Ollama models.
  --range <Slim|RTX3060|Full> Model set to pull. Default: Slim.
  --install-localgpt         Download and install latest LocalGPT Windows release.
  --setup-learning-base      Prepare/import repositories into the learning base path.
  --import-recommended       Import the hardcoded recommended repository list.
  --repo <owner/repo>         Import one extra GitHub repository. Can be repeated.
  --learnbase <path>          Learning base target path. Default: C:\learnbaseforlocalgpt.
  --start-localgpt           Start LocalGPT.exe from %LOCALAPPDATA%\LocalGPT.
  --localgpt-zip <path>      Override LocalGPT ZIP download path.
  --localgpt-exe <path>      Override LocalGPT executable path.
  --force                    Delete existing target folders/files without prompting.
  --all                      Install Ollama, pull models, install LocalGPT, import recommended repos, start LocalGPT.
  --verbose                  Print full exception details on failure.
  --help                     Show this help.
""");
    }

    private static string NextValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
            throw new ArgumentException($"Missing value for {optionName}.");
        return args[++index];
    }

    private static T ParseEnum<T>(string value) where T : struct
    {
        if (Enum.TryParse<T>(value, ignoreCase: true, out var result))
            return result;
        throw new ArgumentException($"Invalid value '{value}' for {typeof(T).Name}.");
    }
}
