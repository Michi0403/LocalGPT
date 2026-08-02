
using LocalGPT.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

internal static class Program
{
    private const string LocalGptRepo = "Michi0403/LocalGPT";
    private const string LocalGptZipName = "LocalGPTByMichi0403.zip";
    private const string LocalGptSetupZipName = "LocalGPTSetupByMichi0403.zip";
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
        var launchedByDoubleClick = args.Length == 0 && Environment.UserInteractive;

        Console.WriteLine($"Your args to string {ArgsToString(args)}");
        var options = CliOptions.Parse(args);
        if(args.Length<=0)
        {
            Console.WriteLine($"args were initially empty !");
        }
        Console.WriteLine($"Parsed options:{Environment.NewLine}{options}");
        try
        {
            // your existing Main logic
            Console.WriteLine($"Starting RunAsync");
            return await RunAsync(args, options).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            if (launchedByDoubleClick | options.WaitOnExit)
            {
                Console.WriteLine($"Wait for Exit send me to Doomland.");
                Console.WriteLine();
                Console.WriteLine("Press any key to close...");
                Console.ReadKey(intercept: true);
            }
        }
        
    }
    private static string ArgsToString(string[]? args)
    {
        if (args is null)
            return "args=null";

        if (args.Length == 0)
            return "args=[]";

        var builder = new StringBuilder();
        builder.AppendLine($"args.Length={args.Length}");

        for (var i = 0; i < args.Length; i++)
            builder.AppendLine($"args[{i}]=\"{args[i]}\"");

        return builder.ToString().TrimEnd();
    }
    private static async Task<int> RunAsync(string[] args, CliOptions options)
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            ColorConsoleLoggerConfiguration colorLoggerProviderOptions = new ColorConsoleLoggerConfiguration() { EventId = 0 };
            ColorConsoleLoggerProvider colorLoggerProvider = new ColorConsoleLoggerProvider(colorLoggerProviderOptions);


            using var loggerFactory = LoggerFactory.Create(configure =>
            {
                configure.ClearProviders();
                configure.AddProvider(colorLoggerProvider);
                //configure.AddProvider()
            });
            var logger = loggerFactory.CreateLogger("Startup");
            logger.LogInformation("Configured app configuration.");

            if (options.ShowHelp)
            {
                CliOptions.PrintHelp(logger);
                return 0;
            }
            try
            {
                if (options.Uninstall)
                {
                    UninstallLocalGptWindows(options, logger);
                    return 0;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in InstallOllama.");
            }

            try
            {
                try
                {
                    if (options.InstallOllama)
                    {
                        logger.LogInformation("InstallOllamaAsync.");
                        await InstallOllamaAsync(options, logger).ConfigureAwait(false);
                    }

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in InstallOllama.");
                }
               
                try
                {
                    if (options.PullOllamaModels)
                    {
                        var ollamaExe = EnsureOllamaAvailable(options, logger);
                        StartOllamaServer(ollamaExe, logger);
                        await PullModelsAsync(ollamaExe, GetModelSet(options.Range), logger).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in PullOllamaModels.");
                }

                try
                {
                    if (options.InstallLocalGptWin)
                        await InstallLocalGptAsync(options, logger).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in InstallLocalGptWin.");
                }
                try
                {
                    if (options.DesktopShortcuts || options.StartMenuShortcuts)
                        ProvisionWindowsShortcuts(options, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in ProvisionWindowsShortcuts.");
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
                            await ImportGitHubSourceToLearningBaseAsync(repo, options, logger).ConfigureAwait(false);
                        logger.LogInformation("Remember: still import/teach the downloaded repositories inside LocalGPT's learning-base importer.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in SetupLearningBase.");
                }

                try
                {
                    if (options.StartLocalGpt)
                        StartLocalGpt(options, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in StartLocalGpt.");
                }


                logger.LogDebug("Done.");
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in Setup: {ex.ToString()}");
                if (options.Verbose)
                    logger.LogWarning(ex.ToString());
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in RunAsync {ex.ToString()}");
            return -1;
        }
    }

    private static async Task InstallOllamaAsync(CliOptions options, ILogger logger)
    {
        try
        {
            var existing = FindOllamaExecutable(options, logger);
            if (!string.IsNullOrWhiteSpace(existing) && File.Exists(existing))
            {
                logger.LogInformation($"Ollama already appears to be installed: {existing}");
                return;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new InvalidOperationException("The built-in Ollama EXE installer currently targets Windows only.");

            var installer = Path.Combine(Path.GetTempPath(), "OllamaSetup.exe");
            await DownloadFileAsync("https://ollama.com/download/OllamaSetup.exe", installer, logger, options).ConfigureAwait(false);

            logger.LogInformation($"Running official Ollama Windows EXE installer: {installer}");
            await RunProcessAsync(installer, string.Empty, logger).ConfigureAwait(false);

            existing = FindOllamaExecutable(options, logger);
            if (string.IsNullOrWhiteSpace(existing) || !File.Exists(existing))
                throw new FileNotFoundException(@"Ollama installer finished, but ollama.exe was not found. Check the installer output and %LOCALAPPDATA%\Ollama\server.log if needed.");

            AddDirectoryToUserPathIfMissing(Path.GetDirectoryName(existing)!, logger);
            logger.LogInformation($"Ollama installer finished. Resolved ollama.exe: {existing}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in InstallOllamaAsync. options {options.ToString()}");
            throw;
        }
    }

    private static string EnsureOllamaAvailable(CliOptions options, ILogger logger)
    {
        try
        {
            var exe = FindOllamaExecutable(options, logger);
            if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            {
                throw new FileNotFoundException(
                    @"Ollama was not found. Install it first or pass --ollama-exe. Expected Windows default: %LOCALAPPDATA%\Programs\Ollama\ollama.exe");
            }

            AddDirectoryToUserPathIfMissing(Path.GetDirectoryName(exe)!, logger);
            logger.LogInformation($"Using Ollama executable: {exe}");
            return exe;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in EnsureOllamaAvailable. options {options.ToString()}");
            throw;
        }
    }

    private static string? FindOllamaExecutable(CliOptions options, ILogger logger)
    {
        try
        {
            var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ollama.exe" : "ollama";

            if (!string.IsNullOrWhiteSpace(options.OllamaExePath))
            {
                var explicitPath = Environment.ExpandEnvironmentVariables(options.OllamaExePath);
                if (File.Exists(explicitPath))
                    return Path.GetFullPath(explicitPath);
                logger.LogWarning($"--ollama-exe was provided but does not exist: {explicitPath}");
            }

            var fromPath = FindCommandOnPath("ollama", logger);
            if (!string.IsNullOrWhiteSpace(fromPath))
                return fromPath;

            var candidates = new List<string>();
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
                candidates.Add(Path.Combine(localAppData, "Programs", "Ollama", exeName));

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
                candidates.Add(Path.Combine(programFiles, "Ollama", exeName));

            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrWhiteSpace(programFilesX86))
                candidates.Add(Path.Combine(programFilesX86, "Ollama", exeName));

            return candidates.FirstOrDefault(File.Exists);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in FindOllamaExecutable. options {options.ToString()}");
            return null;
        }
    }

    private static void AddDirectoryToUserPathIfMissing(string directory, ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return;

            var userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
            var parts = userPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

            if (!parts.Any(p => string.Equals(p.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)))
            {
                var newUserPath = string.IsNullOrWhiteSpace(userPath) ? directory : userPath + Path.PathSeparator + directory;
                Environment.SetEnvironmentVariable("PATH", newUserPath, EnvironmentVariableTarget.User);
                logger.LogInformation($"Added Ollama directory to user PATH: {directory}");
            }

            var processPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (!processPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Any(p => string.Equals(p.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)))
                Environment.SetEnvironmentVariable("PATH", processPath + Path.PathSeparator + directory);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in AddDirectoryToUserPathIfMissing. directory {directory.ToString()}");
            throw;
        }

    }

    private static void StartOllamaServer(string ollamaExe, ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting Ollama server if it is not already running...");
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = ollamaExe,
                    ArgumentList = { "serve" },
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Could not start 'ollama serve'. Continuing anyway: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in StartOllamaServer. ollamaExe {ollamaExe.ToString()}");
            throw;
        }
    }

    private static async Task PullModelsAsync(string ollamaExe, string[] models, ILogger logger)
    {
        try
        {
            foreach (var model in models)
            {
                logger.LogInformation($"Pulling {model}");
                await RunProcessAsync(ollamaExe, $"pull {model}", logger).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in PullModelsAsync. ollamaExe {ollamaExe.ToString()} models {string.Join(", ", models)}");
            throw;
        }

    }

    private static async Task InstallLocalGptAsync(CliOptions options, ILogger logger)
    {
        try
        {
            var zipPath = options.LocalGptZipPath ?? Path.Combine(Environment.CurrentDirectory, LocalGptZipName);

            await DownloadLatestReleaseAssetAsync(
                LocalGptRepo,
                zipPath,
                logger,
                options,
                setupAsset: false).ConfigureAwait(false);

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localAppData))
                throw new InvalidOperationException("LOCALAPPDATA could not be resolved.");

            var targetPath = Path.Combine(localAppData, "LocalGPT");

            if (options.ForceDelete)
                DeleteIfExists(targetPath, logger);

            Directory.CreateDirectory(targetPath);

            logger.LogInformation($"Extracting LocalGPT app '{zipPath}' to '{targetPath}'");
            ExtractZipWithFallback(zipPath, targetPath, logger);

            var setupZipPath = Path.Combine(Environment.CurrentDirectory, LocalGptSetupZipName);

            await DownloadLatestReleaseAssetAsync(
                LocalGptRepo,
                setupZipPath,
                logger,
                options,
                setupAsset: true).ConfigureAwait(false);

            logger.LogInformation($"Extracting LocalGPT setup/bootstrap '{setupZipPath}' to '{targetPath}'");
            ExtractZipWithFallback(setupZipPath, targetPath, logger);

            logger.LogDebug($"LocalGPT installed to '{targetPath}'.");
            logger.LogInformation($"LocalGPT app and setup/bootstrap files now reside in '{targetPath}'.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in InstallLocalGptAsync. options {options}");
        }
    }

    private static void UninstallLocalGptWindows(CliOptions options, ILogger logger)
    {
        try
        {
            EnsureWindowsOnly(nameof(UninstallLocalGptWindows), logger);

            var targets = GetLocalGptUninstallTargets(options, logger);

            logger.LogWarning("LocalGPT uninstall preview:");
            logger.LogWarning("Ollama and Ollama models are not touched.");

            foreach (var target in targets)
            {
                var exists = File.Exists(target) || Directory.Exists(target);
                logger.LogInformation($"{(exists ? "[exists]" : "[missing]")} {target}");
            }

            if (!options.ForceDelete)
            {
                logger.LogWarning("Dry run only. Nothing was deleted.");
                logger.LogWarning("Run again with --uninstall --force-delete to delete the listed LocalGPT files.");
                return;
            }

            logger.LogWarning("--force-delete was used. Removing listed LocalGPT files.");

            foreach (var target in targets)
            {
                DeleteIfExists(target, logger);
            }

            logger.LogInformation("LocalGPT uninstall finished.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in UninstallLocalGptWindows. options {options.ToString()}");
        }
    }
    private static List<string> GetLocalGptUninstallTargets(CliOptions options, ILogger logger)
    {
        try
        {
            var targets = new List<string>();

            var localGptRoot = GetLocalGptInstallRoot( logger);
            targets.Add(localGptRoot);

            var startMenuFolder = GetStartMenuFolder(options,logger);
            targets.Add(startMenuFolder);

            var desktop = GetDesktopFolder(logger);

            var shortcutDefinitions = GetShortcutTargets(localGptRoot, logger);

            foreach (var shortcut in shortcutDefinitions)
            {
                var shortcutFileName = Path.ChangeExtension(shortcut.ShortcutName, ".url");
                targets.Add(Path.Combine(desktop, shortcutFileName));
            }

            // Since this is now explicit destructive uninstall, learning base can be included.
            targets.Add(options.LearningBasePath);

            return targets
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in GetLocalGptUninstallTargets. options {options.ToString()}");
            return new List<string>();
        }
    }

    private static void ProvisionWindowsShortcuts(CliOptions options, ILogger logger)
    {
        try
        {
            EnsureWindowsOnly(nameof(ProvisionWindowsShortcuts), logger);

            var localGptRoot = GetLocalGptInstallRoot(logger);

            if (string.IsNullOrWhiteSpace(localGptRoot) || !Directory.Exists(localGptRoot))
                throw new DirectoryNotFoundException($"LocalGPT directory was not found: {localGptRoot}");

            logger.LogInformation($"Provisioning Windows shortcuts from LocalGPT directory: {localGptRoot}");

            var shortcuts = GetShortcutTargets(localGptRoot, logger);

            if (shortcuts.Count == 0)
            {
                logger.LogWarning($"No shortcut targets found in LocalGPT directory: {localGptRoot}");
                return;
            }

            if (options.DesktopShortcuts)
            {
                var desktop = GetDesktopFolder(logger);
                logger.LogInformation($"Creating Desktop shortcuts in: {desktop}");
             
                CreateShortcutSet(shortcuts, desktop, logger);
            }

            if (options.StartMenuShortcuts)
            {
                var startMenuFolder = GetStartMenuFolder(options,logger);
                Directory.CreateDirectory(startMenuFolder);

                logger.LogInformation($"Creating Start Menu shortcuts in: {startMenuFolder}");
                CreateShortcutSet(shortcuts, startMenuFolder, logger);
            }

            logger.LogInformation("Windows shortcut provisioning finished.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in ProvisionWindowsShortcuts. options {options}");
            throw;
        }
    }
    private static List<ShortcutDefinition> GetShortcutTargets(string localGptRoot, ILogger logger)
    {
        try
        {
            var shortcuts = new List<ShortcutDefinition>();

            shortcuts.Add(new ShortcutDefinition(
                ShortcutName: "LocalGPT Folder.lnk",
                TargetPath: localGptRoot,
                Arguments: string.Empty,
                WorkingDirectory: localGptRoot));

            AddCmdShortcutIfExists(
                shortcuts,
                localGptRoot,
                "Start.cmd",
                "LocalGPT Start.url",
                logger);

            AddCmdShortcutIfExists(
                shortcuts,
                localGptRoot,
                "Pull-Models-Slim.cmd",
                "LocalGPT Pull Models Slim.url",
                logger);

            AddCmdShortcutIfExists(
                shortcuts,
                localGptRoot,
                "Pull-Models-RTX306012GSet.cmd",
                "LocalGPT Pull Models RTX3060 12G Set.url",
                logger);

            AddCmdShortcutIfExists(
                shortcuts,
                localGptRoot,
                "Pull-Models-RX7900XTXSet.cmd",
                "LocalGPT Pull Models RX7900XTX Set.url",
                logger);

            AddCmdShortcutIfExists(
                shortcuts,
                localGptRoot,
                "Force-Delete-Repull-LB-Slim-Model.cmd",
                "LocalGPT Force Delete Repull Learnbase Slim Model.url",
                logger);

            AddCmdShortcutIfExists(
                shortcuts,
                localGptRoot,
                "Install-Force-Delete-Start.cmd",
                "LocalGPT Install Force Delete Start.url",
                logger);

            AddCmdShortcutIfExists(
                shortcuts,
                localGptRoot,
                "LocalGPT-Install-Start.cmd",
                "LocalGPT Install Start.url",
                logger);

            AddCmdShortcutIfExists(
                shortcuts,
                localGptRoot,
                "Update-Default-Learnbase.cmd",
                "LocalGPT Update Default Learnbase.url",
                logger);

            return shortcuts;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in GetShortcutTargets. localGptRoot {localGptRoot}");
            return new List<ShortcutDefinition>();
        }
    }
    private static void CreateShortcutSet(
    List<ShortcutDefinition> shortcuts,
    string targetDirectory,
    ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(targetDirectory))
                throw new InvalidOperationException("Shortcut target directory is empty.");

            Directory.CreateDirectory(targetDirectory);

            var localGptRoot = GetLocalGptInstallRoot(logger);
            var iconPath = FindLocalGptIcon(logger);

            foreach (var shortcut in shortcuts)
            {
                var shortcutPath = Path.Combine(
                    targetDirectory,
                    Path.ChangeExtension(shortcut.ShortcutName, ".url"));

                CreateWindowsUrlShortcut(
                    shortcutPath,
                    shortcut.TargetPath,
                    iconPath, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in CreateShortcutSet. targetDirectory {targetDirectory}");
            throw;
        }
    }
    private static void CreateWindowsUrlShortcut(
    string shortcutPath,
    string targetPath,
      string? iconPath,
    ILogger logger)
    {
        try
        {
            EnsureWindowsOnly(nameof(CreateWindowsUrlShortcut), logger);

            if (string.IsNullOrWhiteSpace(shortcutPath))
                throw new ArgumentException("Shortcut path is empty.", nameof(shortcutPath));

            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException("Target path is empty.", nameof(targetPath));

            var fullTargetPath = Path.GetFullPath(targetPath);
            var targetUri = new Uri(fullTargetPath).AbsoluteUri;

            logger.LogInformation($"Creating URL shortcut: {shortcutPath}");
            logger.LogInformation($"URL shortcut target path: {fullTargetPath}");
            logger.LogInformation($"URL shortcut target uri: {targetUri}");
            logger.LogInformation($"adding shortcut to iconPath uri: {iconPath} if empty then not");
            var directory = Path.GetDirectoryName(Path.GetFullPath(shortcutPath));
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            var builder = new StringBuilder();
            builder.AppendLine("[InternetShortcut]");
            builder.AppendLine($"URL={targetUri}");
            if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
            {
                var fullIconPath = Path.GetFullPath(iconPath);

                logger.LogInformation($"URL shortcut icon: {fullIconPath}");

                builder.AppendLine($"IconFile={fullIconPath}");
                builder.AppendLine("IconIndex=0");
            }
            else
            {
                logger.LogWarning($"Shortcut icon not found, creating shortcut without custom icon: {iconPath}");
            }
            File.WriteAllText(shortcutPath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            logger.LogInformation($"URL shortcut created: {shortcutPath}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in CreateWindowsUrlShortcut. shortcutPath {shortcutPath} targetPath {targetPath}");
            throw;
        }
    }
    private static IEnumerable<string> EnumerateFilesSafe(
    string root,
    string searchPattern,
    ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                return Enumerable.Empty<string>();

            return Directory.EnumerateFiles(
                root,
                searchPattern,
                new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    MatchCasing = MatchCasing.CaseInsensitive
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in EnumerateFilesSafe. root {root} searchPattern {searchPattern}");
            return Enumerable.Empty<string>();
        }
    }
    private static string? FindLocalGptIcon(ILogger logger)
    {
        try
        {
            var localGptRoot = GetLocalGptInstallRoot(logger);

            if (string.IsNullOrWhiteSpace(localGptRoot) || !Directory.Exists(localGptRoot))
            {
                logger.LogWarning($"LocalGPT root does not exist while resolving icon: {localGptRoot}");
                return null;
            }

            var knownCandidates = new[]
            {
            Path.Combine(localGptRoot, "favicon.ico"),
            Path.Combine(localGptRoot, "winx64", "favicon.ico")
        };

            foreach (var candidate in knownCandidates)
            {
                logger.LogInformation($"Checking LocalGPT icon candidate: {candidate}");

                if (File.Exists(candidate))
                {
                    logger.LogInformation($"Resolved LocalGPT icon from known path: {candidate}");
                    return candidate;
                }
            }

            logger.LogWarning($"Known favicon.ico paths failed. Searching recursively under: {localGptRoot}");

            var favicon = EnumerateFilesSafe(localGptRoot, "favicon.ico", logger)
                .OrderBy(path => GetRelativePathDepth(localGptRoot, path))
                .ThenBy(path => path.Length)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(favicon) && File.Exists(favicon))
            {
                logger.LogInformation($"Resolved LocalGPT favicon recursively: {favicon}");
                return favicon;
            }

            logger.LogWarning($"favicon.ico not found. Falling back to any .ico under: {localGptRoot}");

            var anyIcon = EnumerateFilesSafe(localGptRoot, "*.ico", logger)
                .OrderBy(path => GetRelativePathDepth(localGptRoot, path))
                .ThenBy(path => path.Length)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(anyIcon) && File.Exists(anyIcon))
            {
                logger.LogInformation($"Resolved LocalGPT icon recursively: {anyIcon}");
                return anyIcon;
            }

            logger.LogWarning($"No .ico file found under: {localGptRoot}");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in FindLocalGptIcon.");
            return null;
        }
    }
    private static string? FindLocalGptFile(
    string localGptRoot,
    string fileName,
    ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(localGptRoot) || !Directory.Exists(localGptRoot))
            {
                logger.LogWarning($"LocalGPT root does not exist while searching for file '{fileName}': {localGptRoot}");
                return null;
            }

            var directPath = Path.Combine(localGptRoot, fileName);

            logger.LogInformation($"Checking direct LocalGPT file candidate: {directPath}");

            if (File.Exists(directPath))
            {
                logger.LogInformation($"Resolved LocalGPT file from direct path: {directPath}");
                return directPath;
            }

            logger.LogWarning($"Direct LocalGPT file candidate not found. Searching recursively for '{fileName}' under: {localGptRoot}");

            var recursiveCandidate = EnumerateFilesSafe(localGptRoot, fileName, logger)
                .OrderBy(path => GetRelativePathDepth(localGptRoot, path))
                .ThenBy(path => path.Length)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(recursiveCandidate) && File.Exists(recursiveCandidate))
            {
                logger.LogInformation($"Resolved LocalGPT file recursively: {recursiveCandidate}");
                return recursiveCandidate;
            }

            logger.LogWarning($"Could not find '{fileName}' under: {localGptRoot}");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in FindLocalGptFile. localGptRoot {localGptRoot} fileName {fileName}");
            return null;
        }
    }
    private static string? FindLocalGptExecutable(CliOptions options, ILogger logger)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(options.LocalGptExePath))
            {
                var explicitPath = Environment.ExpandEnvironmentVariables(options.LocalGptExePath);

                logger.LogInformation($"Checking explicit LocalGPT executable path: {explicitPath}");

                if (File.Exists(explicitPath))
                    return Path.GetFullPath(explicitPath);

                logger.LogWarning($"--localgpt-exe was provided but does not exist: {explicitPath}");
            }

            var localGptRoot = GetLocalGptInstallRoot(logger);

            if (string.IsNullOrWhiteSpace(localGptRoot) || !Directory.Exists(localGptRoot))
            {
                logger.LogWarning($"LocalGPT root does not exist: {localGptRoot}");
                return null;
            }

            var knownCandidates = new[]
            {
            Path.Combine(localGptRoot, "winx64", "LocalGPT.exe"),
            Path.Combine(localGptRoot, "LocalGPT.exe")
        };

            foreach (var candidate in knownCandidates)
            {
                logger.LogInformation($"Checking LocalGPT executable candidate: {candidate}");

                if (File.Exists(candidate))
                {
                    logger.LogInformation($"Resolved LocalGPT executable from known path: {candidate}");
                    return candidate;
                }
            }

            logger.LogWarning($"Known LocalGPT executable paths failed. Searching recursively under: {localGptRoot}");

            var recursiveCandidate = EnumerateFilesSafe(localGptRoot, "LocalGPT.exe", logger)
                .OrderBy(path => GetRelativePathDepth(localGptRoot, path))
                .ThenBy(path => path.Length)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(recursiveCandidate) && File.Exists(recursiveCandidate))
            {
                logger.LogInformation($"Resolved LocalGPT executable recursively: {recursiveCandidate}");
                return recursiveCandidate;
            }

            logger.LogWarning($"Could not find LocalGPT.exe under: {localGptRoot}");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in FindLocalGptExecutable. options {options}");
            return null;
        }
    }
    private static int GetRelativePathDepth(string root, string path)
    {
        try
        {
            var relative = Path.GetRelativePath(root, path);
            return relative.Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return int.MaxValue;
        }
    }
    private static void AddCmdShortcutIfExists(
    List<ShortcutDefinition> shortcuts,
    string localGptRoot,
    string cmdFileName,
    string shortcutName,
    ILogger logger)
    {
        try
        {
            var cmdPath = FindLocalGptFile(localGptRoot, cmdFileName, logger);

            if (string.IsNullOrWhiteSpace(cmdPath) || !File.Exists(cmdPath))
            {
                logger.LogWarning($"Shortcut target CMD not found, skipping: {cmdFileName}");
                return;
            }

            var workingDirectory = Path.GetDirectoryName(cmdPath);

            if (string.IsNullOrWhiteSpace(workingDirectory))
                workingDirectory = localGptRoot;

            shortcuts.Add(new ShortcutDefinition(
                ShortcutName: shortcutName,
                TargetPath: cmdPath,
                Arguments: string.Empty,
                WorkingDirectory: workingDirectory));

            logger.LogInformation($"Shortcut target found: {cmdPath}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in AddCmdShortcutIfExists. cmdFileName {cmdFileName}");
        }
    }
    private static void EnsureWindowsOnly(string featureName, ILogger logger)
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException($"{featureName} is Windows-only.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in EnsureWindowsOnly. featureName {featureName.ToString()}");
        }
    }

    private static string GetLocalGptInstallRoot( ILogger logger)
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (string.IsNullOrWhiteSpace(localAppData))
                throw new InvalidOperationException("LOCALAPPDATA could not be resolved.");

            return Path.Combine(localAppData, "LocalGPT");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in GetLocalGptInstallRoot.  {ex.ToString()}");
            return string.Empty;
        }

    }

    private static string GetStartMenuFolder(CliOptions options, ILogger logger)
    {
        try
        {
            var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);

            if (string.IsNullOrWhiteSpace(startMenu))
                throw new InvalidOperationException("Start Menu folder could not be resolved.");

            var groupName = SanitizeShortcutGroupName(options.ShortcutGroupName, logger);

            if (string.IsNullOrWhiteSpace(groupName))
                groupName = "LocalGPT by Michi0403";

            return Path.Combine(startMenu, "Programs", groupName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in GetStartMenuFolder. {ex}");
            return string.Empty;
        }
    }
    private static string SanitizeShortcutGroupName(string value, ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(value))
                return "LocalGPT by Michi0403";

            var invalid = Path.GetInvalidFileNameChars();

            foreach (var ch in invalid)
                value = value.Replace(ch, '_');

            return value.Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in SanitizeShortcutGroupName. value {value}");
            return "LocalGPT by Michi0403";
        }
    }
    private static string GetDesktopFolder(ILogger logger)
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            if (string.IsNullOrWhiteSpace(desktop))
                throw new InvalidOperationException("Desktop folder could not be resolved.");

            return desktop;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in GetDesktopFolder. {ex.ToString()}");
            return string.Empty;
        }
    }
    private static async Task ImportGitHubSourceToLearningBaseAsync(
        string repo,
        CliOptions options,
        ILogger logger)
    {
        try
        {
            ValidateRepo(repo, logger);

            var cleanName = SanitizeFileName(repo, logger);
            var targetPath = Path.Combine(options.LearningBasePath, cleanName);
            var zipPath = targetPath + ".zip";
            var manifestPath = targetPath + ".manifest.json";

            var remoteSha = await GetGitHubDefaultBranchCommitShaAsync(repo, logger)
                .ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(remoteSha))
            {
                remoteSha = "unknown";
                logger.LogWarning("GitHub did not return a commit SHA for {Repo}; the cache manifest records 'unknown'.", repo);
            }

            var manifest = ReadGitHubSourceCacheManifest(manifestPath, logger);

            var sameVersionAlreadyExtracted =
                manifest is not null
                && string.Equals(manifest.Repo, repo, StringComparison.OrdinalIgnoreCase)
                && string.Equals(manifest.CommitSha, remoteSha, StringComparison.OrdinalIgnoreCase)
                && DirectoryHasFiles(targetPath,logger);

            if (sameVersionAlreadyExtracted && !options.ForceDelete)
            {
                logger.LogInformation($"Skipping {repo}. Same commit already extracted: {remoteSha}");
                return;
            }

            var sameVersionZipAlreadyExists =
                manifest is not null
                && string.Equals(manifest.Repo, repo, StringComparison.OrdinalIgnoreCase)
                && string.Equals(manifest.CommitSha, remoteSha, StringComparison.OrdinalIgnoreCase)
                && File.Exists(zipPath)
                && new FileInfo(zipPath).Length > 0;

            if (!sameVersionZipAlreadyExists || options.ForceDelete)
            {
                logger.LogInformation($"Downloading GitHub source: {repo}");
                await DownloadGitHubSourceZipAsync(repo, zipPath, logger, options)
                    .ConfigureAwait(false);
            }
            else
            {
                logger.LogInformation($"Reusing cached ZIP for {repo}: {zipPath}");
            }

            if (!File.Exists(zipPath))
                throw new FileNotFoundException($"ZIP is missing, cannot extract: {zipPath}");

            if (new FileInfo(zipPath).Length == 0)
                throw new IOException($"ZIP is empty, cannot extract: {zipPath}");

            if (options.ForceDelete)
                DeleteIfExists(targetPath, logger);

            if (!DirectoryHasFiles(targetPath,logger))
            {
                Directory.CreateDirectory(targetPath);

                logger.LogInformation($"Extracting '{zipPath}' to '{targetPath}'");
                ExtractZipWithFallback(zipPath, targetPath, logger);
            }
            else
            {
                logger.LogInformation($"Target already contains files, not extracting again: {targetPath}");
            }

            WriteGitHubSourceCacheManifest(
                manifestPath,
                new GitHubSourceCacheManifest(
                    Repo: repo,
                    CommitSha: remoteSha,
                    ZipPath: zipPath,
                    TargetPath: targetPath,
                    CachedAtUtc: DateTimeOffset.UtcNow),
                logger);

            logger.LogInformation($"Imported {repo} at commit {remoteSha}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error importing repo {repo} {ex}");
            throw;
        }
    }

    private static void StartLocalGpt(CliOptions options, ILogger logger)
    {
        try
        {
            var exePath = FindLocalGptExecutable(options, logger);


            if (!File.Exists(exePath))
                throw new FileNotFoundException(
                    $"LocalGPT executable not found at '{exePath}'. Install it first or pass --localgpt-exe.");

            var port = options.LocalGptPort <= 0 ? 5000 : options.LocalGptPort;
            var url = $"http://127.0.0.1:{port}";

            logger.LogInformation($"Starting LocalGPT: {exePath}");
            logger.LogInformation($"LocalGPT port: {port}");

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                ArgumentList = { port.ToString() },
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(exePath)
            });

            Thread.Sleep(TimeSpan.FromSeconds(2));

            if (options.OpenBrowser)
            {
                logger.LogInformation($"Opening browser: {url}");
                OpenDefaultBrowser(url, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in StartLocalGpt. options {options}");
        }
    }
    private static void OpenDefaultBrowser(string url, ILogger logger)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not open default browser for URL: {url}");
            throw;
        }
    }

    private static async Task DownloadLatestReleaseAssetAsync(
    string repo,
    string outFile,
    ILogger logger,
    CliOptions options,
    bool setupAsset)
    {
        try
        {
            ValidateRepo(repo, logger);
            var latestUrl = $"https://api.github.com/repos/{repo}/releases/latest";
            using var stream = await Http.GetStreamAsync(latestUrl).ConfigureAwait(false);
            using var json = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

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

                var isPlatformMatch =
                    name.Contains(platform, StringComparison.OrdinalIgnoreCase)
                    && name.Contains(arch, StringComparison.OrdinalIgnoreCase);

                var isSetupAsset =
                    name.Contains("setup", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("installer", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("bootstrap", StringComparison.OrdinalIgnoreCase);

                logger.LogInformation(
                    $"Checking asset '{name}'. PlatformMatch={isPlatformMatch}, SetupAsset={isSetupAsset}, WantedSetupAsset={setupAsset}");

                if (isPlatformMatch && isSetupAsset == setupAsset)
                {
                    selected = asset;
                    break;
                }
            }

            if (selected is null)
            {
                logger.LogWarning(
                    $"No exact asset match found for setupAsset={setupAsset}, platform={platform}, arch={arch}. Falling back to first matching setup mode.");

                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? string.Empty;

                    var isSetupAsset =
                        name.Contains("setup", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("installer", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("bootstrap", StringComparison.OrdinalIgnoreCase);

                    if (isSetupAsset == setupAsset)
                    {
                        selected = asset;
                        break;
                    }
                }
            }

            selected ??= assets.EnumerateArray().First();

            var downloadUrl = selected.Value.GetProperty("browser_download_url").GetString();
            var assetName = selected.Value.GetProperty("name").GetString();

            if (string.IsNullOrWhiteSpace(downloadUrl))
                throw new InvalidOperationException($"Selected release asset for {repo} has no download URL.");

            logger.LogInformation($"Selected asset: {assetName}");
            logger.LogInformation($"Downloading {assetName} to {outFile}");

            await DownloadFileAsync(downloadUrl, outFile, logger, options).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in DownloadLatestReleaseAssetAsync. repo {repo} outFile {outFile} setupAsset={setupAsset}");
            throw;
        }
    }

    private static async Task DownloadGitHubSourceZipAsync(string repo, string outFile, ILogger logger, CliOptions options)
    {
        try
        {
            ValidateRepo(repo, logger);
            var url = $"https://api.github.com/repos/{repo}/zipball";
            await DownloadFileAsync(url, outFile, logger, options).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in DownloadGitHubSourceZipAsync. repo {repo.ToString()} outFile {outFile.ToString()}");
        }
    }
    private static async Task<string?> GetGitHubDefaultBranchCommitShaAsync(
    string repo,
    ILogger logger)
    {
        try
        {
            ValidateRepo(repo, logger);

            var repoUrl = $"https://api.github.com/repos/{repo}";

            using var repoResponse = await Http.GetAsync(repoUrl).ConfigureAwait(false);
            repoResponse.EnsureSuccessStatusCode();

            using var repoStream = await repoResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var repoJson = await JsonDocument.ParseAsync(repoStream).ConfigureAwait(false);

            var defaultBranch = repoJson.RootElement.GetProperty("default_branch").GetString();

            if (string.IsNullOrWhiteSpace(defaultBranch))
                throw new InvalidOperationException($"Could not resolve default branch for {repo}.");

            var branchUrl = $"https://api.github.com/repos/{repo}/branches/{defaultBranch}";

            using var branchResponse = await Http.GetAsync(branchUrl).ConfigureAwait(false);
            branchResponse.EnsureSuccessStatusCode();

            using var branchStream = await branchResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var branchJson = await JsonDocument.ParseAsync(branchStream).ConfigureAwait(false);

            var sha = branchJson.RootElement
                .GetProperty("commit")
                .GetProperty("sha")
                .GetString();

            if (string.IsNullOrWhiteSpace(sha))
                throw new InvalidOperationException($"Could not resolve commit SHA for {repo} branch {defaultBranch}.");

            logger.LogInformation($"Resolved {repo}@{defaultBranch}: {sha}");
            return sha;
        }
        catch (Exception ex)
        {
            logger.LogError($"Error in GetGitHubDefaultBranchCommitShaAsync repo {repo.ToString()} {ex.ToString()}");
            return null;
        }
       
    }
    private static GitHubSourceCacheManifest? ReadGitHubSourceCacheManifest(
    string manifestPath,
    ILogger logger)
    {
        try
        {
            if (!File.Exists(manifestPath))
                return null;

            var json = File.ReadAllText(manifestPath);
            return JsonSerializer.Deserialize<GitHubSourceCacheManifest>(json);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, $"Could not read cache manifest: {manifestPath}");
            return null;
        }
    }

    private static void WriteGitHubSourceCacheManifest(
        string manifestPath,
        GitHubSourceCacheManifest manifest,
        ILogger logger)
    {
        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(manifestPath));
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(
                manifest,
                new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(manifestPath, json);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, $"Could not write cache manifest: {manifestPath}");
        }
    }

    private static bool DirectoryHasFiles(string path, ILogger logger)
    {
        try
        {
            return Directory.Exists(path)
           && Directory.EnumerateFileSystemEntries(path).Any();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, $"Error in DirectoryHasFiles path: {path}");
            return false;
        }
       
    }
    private static async Task DownloadFileAsync(string url, string outFile, ILogger logger, CliOptions options)
    {
        try
        {
            const int maxAttempts = 3;
            var tempFile = outFile + ".part";

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var directory = Path.GetDirectoryName(Path.GetFullPath(outFile));
                    if (!string.IsNullOrWhiteSpace(directory))
                        Directory.CreateDirectory(directory);

                    if (File.Exists(tempFile))
                        File.Delete(tempFile);

                    logger.LogInformation($"Downloading attempt {attempt}/{maxAttempts}: {url}");
                    logger.LogInformation($"Target: {outFile}");

                    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));

                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.UserAgent.ParseAdd("LocalGptSetupTool/1.0");
                    request.Headers.Accept.ParseAdd("*/*");

                    using var response = await Http.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cts.Token).ConfigureAwait(false);

                    response.EnsureSuccessStatusCode();

                    var contentLength = response.Content.Headers.ContentLength;
                    logger.LogInformation(contentLength.HasValue
                        ? $"Remote size: {FormatBytes(contentLength.Value, logger)}"
                        : "Remote size: unknown");

                    long totalRead = 0;

                    await using (var input = await response.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false))
                    await using (var output = new FileStream(
                        tempFile,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 4 * 1024 * 1024,
                        useAsync: true))
                    {
                        var buffer = new byte[4* 1024 * 1024];
                        var lastLog = DateTimeOffset.UtcNow;

                        while (true)
                        {
                            var read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cts.Token)
                                .ConfigureAwait(false);

                            if (read == 0)
                                break;

                            await output.WriteAsync(buffer.AsMemory(0, read), cts.Token)
                                .ConfigureAwait(false);

                            totalRead += read;

                            var now = DateTimeOffset.UtcNow;
                            if (now - lastLog >= TimeSpan.FromSeconds(5))
                            {
                                if (contentLength.HasValue && contentLength.Value > 0)
                                {
                                    var percent = totalRead * 100.0 / contentLength.Value;
                                    logger.LogInformation(
                                        $"Downloaded {FormatBytes(totalRead, logger)} / {FormatBytes(contentLength.Value, logger)} ({percent:F1}%)");
                                }
                                else
                                {
                                    logger.LogInformation($"Downloaded {FormatBytes(totalRead, logger)}");
                                }

                                lastLog = now;
                            }
                        }

                        await output.FlushAsync(cts.Token).ConfigureAwait(false);
                    }

                    // Streams are closed here. Now the file must exist.
                    if (!File.Exists(tempFile))
                        throw new FileNotFoundException($"Temporary download file does not exist after download: {tempFile}");

                    var actualSize = new FileInfo(tempFile).Length;

                    if (actualSize == 0)
                        throw new IOException("Downloaded file is empty.");

                    if (contentLength.HasValue && actualSize != contentLength.Value)
                    {
                        var missing = contentLength.Value - actualSize;
                        throw new IOException(
                            $"Incomplete download. Got {actualSize:N0} bytes, expected {contentLength.Value:N0} bytes. Missing {missing:N0} bytes.");
                    }

                    await MoveFileWithRetryAsync(tempFile, outFile, logger, options).ConfigureAwait(false);

                    logger.LogInformation($"Download complete: {outFile} ({FormatBytes(actualSize, logger)})");
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, $"Download attempt {attempt}/{maxAttempts} failed.");

                    try
                    {
                        if (File.Exists(tempFile))
                            File.Delete(tempFile);
                    }
                    catch
                    {
                        // best effort cleanup
                    }

                    if (attempt == maxAttempts)
                    {
                        logger.LogError(ex, $"Download failed permanently. url {url} outFile {outFile}");
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2 * attempt)).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in DownloadFileAsync. url {url} outFile {outFile}");
        }


    }
    private static async Task MoveFileWithRetryAsync(string source, string destination, ILogger logger, CliOptions options)
    {
        try
        {
            for (var i = 1; i <= 10; i++)
            {
                try
                {
                    if (!File.Exists(source))
                        throw new FileNotFoundException($"Source file for move does not exist: {source}", source);

                    if (File.Exists(destination))
                        File.Delete(destination);
                    if (options.ForceDelete)
                    {
                        DeleteIfExists(destination, logger);
                        File.Move(source, destination, overwrite: true);
                    }
                    else
                    {
                        File.Move(source, destination, overwrite: false);
                    }
                    return;
                }
                catch (IOException ex) when (i < 10)
                {
                    logger.LogWarning(ex, $"Move failed because file is locked. Retry {i}/10...");
                    await Task.Delay(300).ConfigureAwait(false);
                }
            }

            if (File.Exists(destination))
                File.Delete(destination);
            if (options.ForceDelete)
            {
                DeleteIfExists(destination, logger);
                File.Move(source, destination, overwrite: true);
            }
            else
            {
                File.Move(source, destination, overwrite: false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in MoveFileWithRetryAsync. source {source} destination {destination}");
        }

    }
    private static string FormatBytes(long bytes, ILogger logger)
    {
        try
        {
            string[] units = ["B", "KB", "MB", "GB", "TB"];
            double value = bytes;
            var unit = 0;

            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }

            return $"{value:F2} {units[unit]}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in FormatBytes. bytes {bytes.ToString()}");
            throw;
        }
      
    }

    private static void DeleteIfExists(string path, ILogger logger)
    {
        try
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                return;

            logger.LogWarning($"Deleting existing path because --force-delete was used: {path}");

            var attrs = File.GetAttributes(path);
            if (attrs.HasFlag(FileAttributes.Directory))
                Directory.Delete(path, recursive: true);
            else
                File.Delete(path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in DeleteIfExists. path {path.ToString()}");
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
                logger.LogWarning(ex, $".NET ZIP extraction failed: {ex.Message}");
            }

            var sevenZip = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe");
            if (!File.Exists(sevenZip))
                throw new InvalidOperationException("ZIP extraction failed and 7-Zip was not found. Install 7-Zip or enable long paths.");

            RunProcessAsync(sevenZip, $"x \"{zipPath}\" -o\"{targetPath}\" -y", logger).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in ExtractZipWithFallback. zipPath {zipPath.ToString()} targetPath {targetPath.ToString()}");
            throw;
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
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) logger.LogWarning(e.Data); };

            if (!process.Start())
                throw new InvalidOperationException($"Could not start process: {fileName}");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync().ConfigureAwait(false);

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Command failed with exit code {process.ExitCode}: {fileName} {arguments}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in RunProcessAsync. fileName {fileName.ToString()} arguments {arguments.ToString()}");
            throw;
        }

    }

    private static string? FindCommandOnPath(string command, ILogger logger)
    {
        try
        {
            var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            var extensions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.BAT;.CMD").Split(';', StringSplitOptions.RemoveEmptyEntries)
                : [string.Empty];

            foreach (var path in paths)
            {
                foreach (var ext in extensions)
                {
                    var candidate = Path.Combine(path.Trim(), command + ext.ToLowerInvariant());
                    if (File.Exists(candidate))
                        return Path.GetFullPath(candidate);

                    candidate = Path.Combine(path.Trim(), command + ext.ToUpperInvariant());
                    if (File.Exists(candidate))
                        return Path.GetFullPath(candidate);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in FindCommandOnPath. command {command.ToString()}");
            return null;
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
            throw;
        }

    }

    private static HttpClient CreateHttpClient()
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
            Console.Error.WriteLine($"Error in CreateHttpClient. {ex}");
            throw;
        }
    }
}


internal enum ModelRange
{
    Slim,
    RTX3060,
    Full
}
//To not download already downloaded again and again and again and again and get banned by githubs rate limit
internal sealed record ShortcutDefinition(
    string ShortcutName,
    string TargetPath,
    string Arguments,
    string WorkingDirectory
);

internal sealed record GitHubSourceCacheManifest(
    string Repo,
    string CommitSha,
    string ZipPath,
    string TargetPath,
    DateTimeOffset CachedAtUtc
);
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
    public string? OllamaExePath { get; private set; }
    public List<string> ExtraRepos { get; } = [];
    public int LocalGptPort { get; private set; } = 5000;
    public bool OpenBrowser { get; private set; } = true;
    public bool ForceDelete { get; private set; }
    public bool WaitOnExit { get; private set; }
    public bool Uninstall { get; private set; }
    public bool DesktopShortcuts { get; private set; }
    public bool StartMenuShortcuts { get; private set; }
    public string ShortcutGroupName { get; private set; } = "LocalGPT by Michi0403";
    public static CliOptions Parse(string[] args)
    {
        List<string> argsList = args.ToList();
        var options = new CliOptions();
        if (argsList.Count == 0)
        {
            argsList.Add("--install-ollama");
            argsList.Add("--pull-models");
            argsList.Add("--range");
            argsList.Add("Slim");
            argsList.Add("--install-localgpt");
            argsList.Add("--start-localgpt");
            argsList.Add("--shortcuts");
        }
        for (var i = 0; i < argsList.Count; i++)
        {
            var arg = argsList[i];
            switch (arg.ToLowerInvariant().TrimStart())
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
                case "--wait":
                case "--pause":
                    options.WaitOnExit = true;
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
                    options.Range = ParseEnum<ModelRange>(NextValue(argsList, ref i, arg));
                    break;
                case "--learnbase":
                    options.LearningBasePath = NextValue(argsList, ref i, arg);
                    break;
                case "--repo":
                    options.ExtraRepos.Add(NextValue(argsList, ref i, arg));
                    options.SetupLearningBase = true;
                    break;
                case "--localgpt-zip":
                    options.LocalGptZipPath = NextValue(argsList, ref i, arg);
                    break;
                case "--localgpt-exe":
                    options.LocalGptExePath = NextValue(argsList, ref i, arg);
                    break;
                case "--ollama-exe":
                    options.OllamaExePath = NextValue(argsList, ref i, arg);
                    break;
                case "--desktop-shortcuts":
                    options.DesktopShortcuts = true;
                    break;

                case "--startmenu-shortcuts":
                    options.StartMenuShortcuts = true;
                    break;
                case "--shortcut-group-name":
                case "--startmenu-name":
                    options.ShortcutGroupName = NextValue(argsList, ref i, arg);
                    break;
                case "--shortcuts":
                    options.DesktopShortcuts = true;
                    options.StartMenuShortcuts = true;
                    break;
                case "--port":
                    options.LocalGptPort = int.Parse(NextValue(argsList, ref i, arg));
                    if (options.LocalGptPort <= 0 || options.LocalGptPort > 65535)
                        throw new ArgumentOutOfRangeException(nameof(options.LocalGptPort), "Port must be between 1 and 65535.");
                    break;

                case "--no-browser":
                    options.OpenBrowser = false;
                    break;

                case "--force-delete":
                    options.ForceDelete = true;
                    break;

                case "--uninstall":
                    options.Uninstall = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}. Use --help.");
            }
        }

        if (argsList.Count == 0)
            options.ShowHelp = true;

        return options;
    }
    public override string ToString()
    {
        return string.Join(Environment.NewLine,
        [
            $"{nameof(ShowHelp)}={ShowHelp}",
        $"{nameof(InstallOllama)}={InstallOllama}",
        $"{nameof(PullOllamaModels)}={PullOllamaModels}",
        $"{nameof(InstallLocalGptWin)}={InstallLocalGptWin}",
        $"{nameof(SetupLearningBase)}={SetupLearningBase}",
        $"{nameof(ImportRecommended)}={ImportRecommended}",
        $"{nameof(StartLocalGpt)}={StartLocalGpt}",
        $"{nameof(ForceDelete)}={ForceDelete}",
        $"{nameof(Verbose)}={Verbose}",
        $"{nameof(Range)}={Range}",
        $"{nameof(LearningBasePath)}={LearningBasePath}",
        $"{nameof(LocalGptZipPath)}={LocalGptZipPath}",
        $"{nameof(LocalGptExePath)}={LocalGptExePath}",
        $"{nameof(OllamaExePath)}={OllamaExePath}",
        $"{nameof(LocalGptPort)}={LocalGptPort}",
        $"{nameof(OpenBrowser)}={OpenBrowser}",
        $"{nameof(WaitOnExit)}={WaitOnExit}",
        $"{nameof(Uninstall)}={Uninstall}",
        $"{nameof(DesktopShortcuts)}={DesktopShortcuts}",
        $"{nameof(StartMenuShortcuts)}={StartMenuShortcuts}",
        $"{nameof(ShortcutGroupName)}={ShortcutGroupName}",
        $"{nameof(ExtraRepos)}=[{string.Join(", ", ExtraRepos.Select(x => $"{x}"))}]"
        ]);
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
  localgpt-setup --install-localgpt --force-delete
  localgpt-setup --setup-learning-base --repo Michi0403/LocalGPT --force-delete
  localgpt-setup --setup-learning-base --repo id-Software/DOOM
  localgpt-setup --setup-learning-base --repo lotgd/lotgd
  localgpt-setup --setup-learning-base --repo php/doc-en
  localgpt-setup --setup-learning-base --repo llvm/llvm-project
  localgpt-setup --import-recommended --force-delete
  localgpt-setup --all --range Slim --force-delete

Options:
  --install-ollama           Install Ollama by downloading and running the official Windows EXE installer.
  --pull-models              Pull Ollama models.
  --range <Slim|RTX3060|Full> Model set to pull. Default: Slim.
  --install-localgpt         Download and install latest LocalGPT Windows release.
  --setup-learning-base      Prepare/import repositories into the learning base path.
  --import-recommended       Import the hardcoded recommended repository list.
  --repo <owner/repo>         Import one user-selected GitHub repository. Can be repeated; nothing in these examples is pulled automatically.
  --learnbase <path>          Learning base target path. Default: C:\learnbaseforlocalgpt.
  --start-localgpt           Start LocalGPT.exe from %LOCALAPPDATA%\LocalGPT.
  --localgpt-zip <path>      Override LocalGPT ZIP download path.
  --localgpt-exe <path>      Override LocalGPT executable path.
  --ollama-exe <path>        Override Ollama executable path. Default Windows location is %LOCALAPPDATA%\Programs\Ollama\ollama.exe.
  --port <number>            Port for LocalGPT. Default: 5000.
  --wait                     An options beside of opening with mouse to keep it running
  --no-browser               Start LocalGPT without opening the browser.
  --force-delete             Delete existing install/import folders before extracting. Not used by default.
  --all                      Install Ollama, pull models, install LocalGPT, import recommended repos, start LocalGPT.
  --verbose                  Print full exception details on failure.
  --help                     Show this help.
  --desktop-shortcuts        Create Desktop shortcuts to selected LocalGPT command files.
  --startmenu-shortcuts      Create Start Menu shortcuts to selected LocalGPT command files.
  --shortcuts                Create both Desktop and Start Menu shortcuts.
  --uninstall                Preview LocalGPT uninstall. Shows what would be removed, deletes nothing. DOESN'T TOUCH OLLAMA OR IT'S MODELS
  --uninstall --force-delete Actually remove LocalGPT files, launchers, shortcuts, and learning base. DOESN'T TOUCH OLLAMA OR IT'S MODELS
""");
    }

    private static string NextValue(List<string> args, ref int index, string optionName)
    {
        if (index + 1 >= args.Count)
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

