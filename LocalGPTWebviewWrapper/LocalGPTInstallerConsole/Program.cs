
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
    private const string InstallationManifestFileName = "localgpt-installation.json";
    private const string InstallerRelocatedEnvironmentVariable = "LOCALGPT_INSTALLER_RELOCATED";
    private static readonly HttpClient Http = CreateHttpClient();
    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        WriteIndented = true
    };

    private sealed record GitHubReleaseAsset(
        string Name,
        string DownloadUrl,
        long Size);

    private sealed record GitHubReleaseSelection(
        string Repository,
        string TagName,
        DateTimeOffset PublishedAt,
        DateTimeOffset CreatedAt,
        long TagRank,
        string ResolutionSource,
        IReadOnlyList<GitHubReleaseAsset> Assets);

    private sealed record LocalGptInstallationManifest(
        string ReleaseTag,
        string AppAsset,
        string SetupAsset,
        string InstallRoot,
        string DataRoot,
        string ApplicationExecutable,
        string InstallerExecutable,
        DateTimeOffset InstalledAtUtc);

    private static readonly string[] SlimModels =
    [
        "gpt-oss:20b",
        "gemma3:27b",
        "deepseek-r1:8b",
        "qwen3-coder:30b"
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
        "llama2:7b", "llama2:13b",
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
        "llama2:7b", "llama2:13b",
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
        try
        {
            var relocatedExitCode = TryRunInstallerFromTemporaryLocation(args);
            if (relocatedExitCode.HasValue)
                return relocatedExitCode.Value;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not relocate the setup helper for installation maintenance: {ex.Message}");
            return 1;
        }

        var launchedByDoubleClick = args.Length == 0 && Environment.UserInteractive;
        CliOptions? options = null;

        try
        {
            options = CliOptions.Parse(args);
            return await RunAsync(options).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Setup helper failed: {ex.Message}");
            return 1;
        }
        finally
        {
            if (launchedByDoubleClick || options?.WaitOnExit == true)
            {
                Console.WriteLine("Setup helper finished.");
                Console.WriteLine();
                Console.WriteLine("Press any key to close...");
                Console.ReadKey(intercept: true);
            }
        }
        
    }
    private static int? TryRunInstallerFromTemporaryLocation(string[] args)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            || string.Equals(
                Environment.GetEnvironmentVariable(InstallerRelocatedEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            return null;
        }

        var performsInstallMaintenance =
            args.Length == 0
            || args.Any(argument =>
                string.Equals(argument, "--install-localgpt", StringComparison.OrdinalIgnoreCase)
                || string.Equals(argument, "--all", StringComparison.OrdinalIgnoreCase)
                || string.Equals(argument, "--uninstall", StringComparison.OrdinalIgnoreCase));

        if (!performsInstallMaintenance)
            return null;

        var currentExecutable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExecutable) || !File.Exists(currentExecutable))
            return null;

        var installRoot = GetCanonicalLocalGptInstallRoot();
        if (!IsPathWithinRoot(currentExecutable, installRoot))
            return null;

        var relocationDirectory = Path.Combine(
            Path.GetTempPath(),
            "LocalGPTInstallerConsole",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(relocationDirectory);

        var relocatedExecutable = Path.Combine(
            relocationDirectory,
            Path.GetFileName(currentExecutable));
        File.Copy(currentExecutable, relocatedExecutable, overwrite: true);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = relocatedExecutable,
                WorkingDirectory = relocationDirectory,
                UseShellExecute = false
            }
        };

        foreach (var argument in args)
            process.StartInfo.ArgumentList.Add(argument);

        process.StartInfo.Environment[InstallerRelocatedEnvironmentVariable] = "1";

        if (!process.Start())
            throw new InvalidOperationException("The relocated LocalGPT setup helper could not be started.");

        process.WaitForExit();
        var exitCode = process.ExitCode;

        try
        {
            Directory.Delete(relocationDirectory, recursive: true);
        }
        catch
        {
            // Best effort. Windows can briefly retain the relocated executable after exit.
        }

        return exitCode;
    }

    private static async Task<int> RunAsync(CliOptions options)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;

            var colorLoggerProviderOptions = new ColorConsoleLoggerConfiguration { EventId = 0 };
            using var colorLoggerProvider = new ColorConsoleLoggerProvider(colorLoggerProviderOptions);
            using var loggerFactory = LoggerFactory.Create(configure =>
            {
                configure.ClearProviders();
                configure.AddProvider(colorLoggerProvider);
            });

            var logger = loggerFactory.CreateLogger("Startup");
            logger.LogInformation("Configured setup helper.");

            if (options.ShowHelp)
            {
                CliOptions.PrintHelp(logger);
                return 0;
            }

            if (options.Uninstall)
            {
                UninstallLocalGptWindows(options, logger);
                return 0;
            }

            // Setup operations are intentionally fail-closed. A failed requested step
            // stops the workflow and returns a non-zero exit code instead of reporting
            // success after partial installation.
            if (options.InstallOllama)
            {
                logger.LogInformation("Installing or verifying Ollama for the selected setup workflow.");
                await InstallOllamaAsync(options, logger).ConfigureAwait(false);
            }

            if (options.PullOllamaModels)
            {
                var ollamaExe = EnsureOllamaAvailable(options, logger);
                StartOllamaServer(ollamaExe, logger);
                await PullModelsAsync(ollamaExe, GetModelSet(options.Range), logger).ConfigureAwait(false);
            }

            if (options.InstallLocalGptWin)
                await InstallLocalGptAsync(options, logger).ConfigureAwait(false);

            if (options.DesktopShortcuts || options.StartMenuShortcuts)
                ProvisionWindowsShortcuts(options, logger);

            if (options.SetupLearningBase)
            {
                Directory.CreateDirectory(options.LearningBasePath);
                var repos = options.ImportRecommended
                    ? RecommendedRepos.Concat(options.ExtraRepos).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                    : options.ExtraRepos.Count > 0 ? options.ExtraRepos.ToArray() : [LocalGptRepo];

                foreach (var repo in repos)
                    await ImportGitHubSourceToLearningBaseAsync(repo, options, logger).ConfigureAwait(false);

                logger.LogInformation("Downloaded sources still require explicit review/import inside LocalGPT.");
            }

            if (options.StartLocalGpt)
                StartLocalGpt(options, logger);

            logger.LogInformation("All selected setup operations completed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(options.Verbose ? ex.ToString() : $"Setup failed: {ex.Message}");
            return 1;
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
            logger.LogError(ex, "The selected Ollama installation or verification step failed; option paths were omitted from logs.");
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
            logger.LogError(ex, "Could not resolve Ollama; option paths were omitted from logs.");
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
            logger.LogError(ex, "Could not locate Ollama; option paths were omitted from logs.");
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
            if (WaitForExistingOllamaProcess(TimeSpan.FromSeconds(3)))
            {
                logger.LogInformation("An Ollama app or server process is already running. The installer will reuse it instead of starting a duplicate ollama.exe instance.");
                return;
            }

            logger.LogInformation("No Ollama process appeared during the duplicate-start guard window. Starting one local Ollama server...");
            try
            {
                using var process = Process.Start(new ProcessStartInfo
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
            logger.LogError(ex, "Error while checking or starting the Ollama server; executable paths were omitted from logs.");
            throw;
        }
    }

    private static bool WaitForExistingOllamaProcess(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        do
        {
            if (IsOllamaProcessRunning())
                return true;
            Thread.Sleep(TimeSpan.FromMilliseconds(250));
        }
        while (DateTime.UtcNow < deadline);

        return false;
    }

    private static bool IsOllamaProcessRunning()
    {
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    var normalizedName = new string(process.ProcessName
                        .Where(char.IsLetterOrDigit)
                        .Select(char.ToLowerInvariant)
                        .ToArray());
                    if (normalizedName is "ollama" or "ollamaapp")
                        return true;
                }
                catch
                {
                    // Processes can exit or become inaccessible while enumerating them.
                }
            }
        }

        return false;
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
        string? stagingPath = null;
        try
        {
            var release = await ResolveNewestCompatibleReleaseAsync(LocalGptRepo, logger).ConfigureAwait(false);
            var zipPath = options.LocalGptZipPath ?? Path.Combine(Environment.CurrentDirectory, LocalGptZipName);

            await DownloadReleaseAssetAsync(
                release,
                zipPath,
                logger,
                options,
                setupAsset: false).ConfigureAwait(false);

            var setupZipPath = Path.Combine(Environment.CurrentDirectory, LocalGptSetupZipName);
            await DownloadReleaseAssetAsync(
                release,
                setupZipPath,
                logger,
                options,
                setupAsset: true).ConfigureAwait(false);

            var targetPath = GetLocalGptInstallRoot(logger);
            var dataPath = GetLocalGptDataRoot(logger);
            var parentDirectory = Path.GetDirectoryName(targetPath)
                ?? throw new InvalidOperationException("LocalGPT install parent directory could not be resolved.");

            Directory.CreateDirectory(parentDirectory);
            stagingPath = Path.Combine(
                parentDirectory,
                $".LocalGPT.install-{Guid.NewGuid():N}");
            Directory.CreateDirectory(stagingPath);

            logger.LogInformation(
                "Installing LocalGPT application binaries to '{InstallRoot}'. Existing user data remains at '{DataRoot}'.",
                targetPath,
                dataPath);

            logger.LogInformation(
                "Extracting LocalGPT app archive '{ArchivePath}' into staging directory '{StagingPath}'.",
                zipPath,
                stagingPath);
            ExtractReleaseArchiveSafely(zipPath, stagingPath, logger);

            logger.LogInformation(
                "Extracting LocalGPT setup/bootstrap archive '{ArchivePath}' into staging directory '{StagingPath}'.",
                setupZipPath,
                stagingPath);
            ExtractReleaseArchiveSafely(setupZipPath, stagingPath, logger);

            ValidateInstalledLayout(stagingPath);

            var platform = GetPlatformToken();
            var architecture = GetArchitectureToken();
            var appAssetName = $"{platform}{architecture}.zip";
            var setupAssetName = $"setup{platform}{architecture}.zip";
            var appExecutableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "LocalGPT.exe"
                : "LocalGPT";
            var installerExecutableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "LocalGPTInstallerConsole.exe"
                : "LocalGPTInstallerConsole";

            var manifest = new LocalGptInstallationManifest(
                ReleaseTag: release.TagName,
                AppAsset: appAssetName,
                SetupAsset: setupAssetName,
                InstallRoot: targetPath,
                DataRoot: dataPath,
                ApplicationExecutable: Path.Combine(targetPath, appExecutableName),
                InstallerExecutable: Path.Combine(targetPath, installerExecutableName),
                InstalledAtUtc: DateTimeOffset.UtcNow);

            File.WriteAllText(
                Path.Combine(stagingPath, InstallationManifestFileName),
                JsonSerializer.Serialize(manifest, ManifestJsonOptions),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            EnsureLocalGptInstallRootIsNotRunning(targetPath, logger);
            ReplaceInstalledDirectory(stagingPath, targetPath, logger);
            stagingPath = null;

            logger.LogInformation("LocalGPT release {ReleaseTag} installed successfully.", release.TagName);
            logger.LogInformation("LocalGPT executable: {ExecutablePath}", manifest.ApplicationExecutable);
            logger.LogInformation("LocalGPT setup helper: {InstallerPath}", manifest.InstallerExecutable);
            logger.LogInformation("LocalGPT user-data directory: {DataRoot}", manifest.DataRoot);

            LogLegacyMisplacedInstallFolders(dataPath, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LocalGPT installation failed.");
            throw;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(stagingPath) && Directory.Exists(stagingPath))
            {
                try
                {
                    Directory.Delete(stagingPath, recursive: true);
                }
                catch (Exception cleanupException)
                {
                    logger.LogWarning(
                        cleanupException,
                        "Could not remove failed LocalGPT staging directory '{StagingPath}'.",
                        stagingPath);
                }
            }
        }
    }

    private static void ValidateInstalledLayout(string installRoot)
    {
        var appExecutable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(installRoot, "LocalGPT.exe")
            : Path.Combine(installRoot, "LocalGPT");
        var installerExecutable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(installRoot, "LocalGPTInstallerConsole.exe")
            : Path.Combine(installRoot, "LocalGPTInstallerConsole");

        if (!File.Exists(appExecutable))
            throw new InvalidDataException(
                $"The LocalGPT application archive did not produce the expected executable at '{appExecutable}'.");

        if (!File.Exists(installerExecutable))
            throw new InvalidDataException(
                $"The LocalGPT setup archive did not produce the expected executable at '{installerExecutable}'.");

        string[] requiredLaunchers =
        [
            "Default.cmd",
            "Install.cmd",
            "Update.cmd",
            "Start.cmd",
            "Start-NoBrowser.cmd",
            "Install-Ollama.cmd",
            "Pull-Models-Slim.cmd",
            "Pull-Models-RTX3060.cmd",
            "Pull-Models-Full.cmd",
            "Setup-Learning-Base.cmd",
            "Import-Recommended.cmd",
            "Uninstall.cmd"
        ];

        var missingLaunchers = requiredLaunchers
            .Where(fileName => !File.Exists(Path.Combine(installRoot, fileName)))
            .ToArray();
        if (missingLaunchers.Length > 0)
        {
            throw new InvalidDataException(
                $"The LocalGPT setup archive is incomplete. Missing root launcher files: {string.Join(", ", missingLaunchers)}");
        }
    }

    private static void ReplaceInstalledDirectory(
        string stagingPath,
        string targetPath,
        ILogger logger)
    {
        var backupPath = targetPath + $".backup-{Guid.NewGuid():N}";
        var targetMovedToBackup = false;

        try
        {
            if (Directory.Exists(targetPath))
            {
                logger.LogInformation(
                    "Moving existing LocalGPT application directory to temporary backup '{BackupPath}'.",
                    backupPath);
                Directory.Move(targetPath, backupPath);
                targetMovedToBackup = true;
            }

            Directory.Move(stagingPath, targetPath);

            if (targetMovedToBackup && Directory.Exists(backupPath))
            {
                try
                {
                    Directory.Delete(backupPath, recursive: true);
                }
                catch (Exception cleanupException)
                {
                    logger.LogWarning(
                        cleanupException,
                        "The new LocalGPT installation is active, but the previous application backup could not be removed: {BackupPath}",
                        backupPath);
                }
            }
        }
        catch
        {
            if (!Directory.Exists(targetPath)
                && targetMovedToBackup
                && Directory.Exists(backupPath))
            {
                Directory.Move(backupPath, targetPath);
            }

            throw;
        }
    }

    private static void EnsureLocalGptInstallRootIsNotRunning(
        string installRoot,
        ILogger logger)
    {
        var endpointPath = Path.Combine(
            GetLocalGptDataRoot(logger),
            "runtime",
            "server.json");
        if (!File.Exists(endpointPath))
            return;

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(endpointPath));
            if (!document.RootElement.TryGetProperty("ProcessId", out var processIdElement)
                || !processIdElement.TryGetInt32(out var processId)
                || processId <= 0)
            {
                return;
            }

            using var process = Process.GetProcessById(processId);
            process.Refresh();
            if (process.HasExited)
                return;

            string? processPath = null;
            try
            {
                processPath = process.MainModule?.FileName;
            }
            catch
            {
                // Access to MainModule can be denied. The installer still checks the known root below.
            }

            if (!string.IsNullOrWhiteSpace(processPath)
                && IsPathWithinRoot(processPath, installRoot))
            {
                throw new InvalidOperationException(
                    $"LocalGPT is currently running from '{processPath}' (PID {processId}). Close it before installing or updating.");
            }
        }
        catch (ArgumentException)
        {
            // Stale process id.
        }
        catch (InvalidOperationException ex)
            when (!ex.Message.StartsWith("LocalGPT is currently running", StringComparison.Ordinal))
        {
            logger.LogDebug(ex, "Ignored a stale LocalGPT runtime endpoint during installer preflight.");
        }
        catch (IOException ex)
        {
            logger.LogDebug(ex, "Could not read the LocalGPT runtime endpoint during installer preflight.");
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogDebug(ex, "Could not inspect the LocalGPT runtime process during installer preflight.");
        }
        catch (JsonException ex)
        {
            logger.LogDebug(ex, "Ignored an invalid LocalGPT runtime endpoint during installer preflight.");
        }
    }

    private static void LogLegacyMisplacedInstallFolders(
        string dataRoot,
        ILogger logger)
    {
        if (!Directory.Exists(dataRoot))
            return;

        var platform = GetPlatformToken();
        var architecture = GetArchitectureToken();
        string[] legacyFolders =
        [
            Path.Combine(dataRoot, $"{platform}{architecture}"),
            Path.Combine(dataRoot, $"setup{platform}{architecture}")
        ];

        foreach (var legacyFolder in legacyFolders.Where(Directory.Exists))
        {
            logger.LogWarning(
                "A misplaced legacy application folder remains in the LocalGPT user-data directory and is no longer used: {LegacyFolder}. " +
                "It was not deleted automatically because the installer preserves user data.",
                legacyFolder);
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
            logger.LogError(ex, "LocalGPT uninstall failed.");
            throw;
        }
    }
    private static List<string> GetLocalGptUninstallTargets(CliOptions options, ILogger logger)
    {
        try
        {
            var targets = new List<string>
            {
                GetLocalGptInstallRoot(logger),
                GetStartMenuFolder(options, logger)
            };

            var desktop = GetDesktopFolder(logger);
            string[] shortcutNames =
            [
                "LocalGPT Folder",
                "LocalGPT Default Install and Update",
                "LocalGPT Install",
                "LocalGPT Update",
                "LocalGPT Start",
                "LocalGPT Start without Browser",
                "LocalGPT Install Ollama",
                "LocalGPT Pull Slim Models",
                "LocalGPT Pull RTX 3060 Models",
                "LocalGPT Pull Full Models",
                "LocalGPT Setup Learning Base",
                "LocalGPT Import Recommended Sources",
                "LocalGPT Uninstall"
            ];

            foreach (var shortcutName in shortcutNames)
            {
                targets.Add(Path.Combine(desktop, Path.ChangeExtension(shortcutName, ".lnk")));
                targets.Add(Path.Combine(desktop, Path.ChangeExtension(shortcutName, ".url")));
            }

            logger.LogInformation(
                "The LocalGPT user-data directory is preserved during uninstall: {DataRoot}",
                GetLocalGptDataRoot(logger));
            logger.LogInformation(
                "The learning-base directory is preserved during uninstall: {LearningBasePath}",
                options.LearningBasePath);

            return targets
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not determine LocalGPT uninstall targets.");
            throw;
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
            logger.LogError(ex, "Could not provision Windows shortcuts; option paths were omitted from logs.");
            throw;
        }
    }
    private static List<ShortcutDefinition> GetShortcutTargets(string localGptRoot, ILogger logger)
    {
        try
        {
            var shortcuts = new List<ShortcutDefinition>
            {
                new(
                    ShortcutName: "LocalGPT Folder",
                    TargetPath: localGptRoot,
                    Arguments: string.Empty,
                    WorkingDirectory: localGptRoot)
            };

            var launchers = new (string FileName, string ShortcutName)[]
            {
                ("Default.cmd", "LocalGPT Default Install and Update"),
                ("Install.cmd", "LocalGPT Install"),
                ("Update.cmd", "LocalGPT Update"),
                ("Start.cmd", "LocalGPT Start"),
                ("Start-NoBrowser.cmd", "LocalGPT Start without Browser"),
                ("Install-Ollama.cmd", "LocalGPT Install Ollama"),
                ("Pull-Models-Slim.cmd", "LocalGPT Pull Slim Models"),
                ("Pull-Models-RTX3060.cmd", "LocalGPT Pull RTX 3060 Models"),
                ("Pull-Models-Full.cmd", "LocalGPT Pull Full Models"),
                ("Setup-Learning-Base.cmd", "LocalGPT Setup Learning Base"),
                ("Import-Recommended.cmd", "LocalGPT Import Recommended Sources"),
                ("Uninstall.cmd", "LocalGPT Uninstall")
            };

            foreach (var launcher in launchers)
            {
                AddCmdShortcutIfExists(
                    shortcuts,
                    localGptRoot,
                    launcher.FileName,
                    launcher.ShortcutName,
                    logger);
            }

            return shortcuts;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not determine LocalGPT shortcut targets under {InstallRoot}.", localGptRoot);
            return [];
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
            var iconPath = FindLocalGptIcon(logger);

            foreach (var shortcut in shortcuts)
            {
                DeleteLegacyUrlShortcut(targetDirectory, shortcut.ShortcutName, logger);

                var shortcutPath = Path.Combine(
                    targetDirectory,
                    Path.ChangeExtension(shortcut.ShortcutName, ".lnk"));

                CreateWindowsShellLink(
                    shortcutPath,
                    shortcut,
                    iconPath,
                    logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not create LocalGPT shortcuts in {TargetDirectory}.", targetDirectory);
            throw;
        }
    }

    private static void DeleteLegacyUrlShortcut(
        string targetDirectory,
        string shortcutName,
        ILogger logger)
    {
        var legacyUrlPath = Path.Combine(
            targetDirectory,
            Path.ChangeExtension(shortcutName, ".url"));
        if (!File.Exists(legacyUrlPath))
            return;

        try
        {
            File.Delete(legacyUrlPath);
            logger.LogInformation("Removed obsolete URL shortcut: {ShortcutPath}", legacyUrlPath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not remove obsolete URL shortcut: {ShortcutPath}", legacyUrlPath);
        }
    }

    private static void CreateWindowsShellLink(
        string shortcutPath,
        ShortcutDefinition shortcut,
        string? iconPath,
        ILogger logger)
    {
        EnsureWindowsOnly(nameof(CreateWindowsShellLink), logger);

        object? shell = null;
        object? shellLink = null;

        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell")
                ?? throw new PlatformNotSupportedException("Windows Script Host is unavailable.");

            shell = Activator.CreateInstance(shellType)
                ?? throw new InvalidOperationException("Windows Script Host could not be created.");

            shellLink = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                binder: null,
                target: shell,
                args: [shortcutPath])
                ?? throw new InvalidOperationException("Windows shortcut object could not be created.");

            var linkType = shellLink.GetType();
            var targetPath = shortcut.TargetPath;
            var arguments = shortcut.Arguments;
            var workingDirectory = shortcut.WorkingDirectory;

            if (Directory.Exists(shortcut.TargetPath))
            {
                var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                targetPath = string.IsNullOrWhiteSpace(windowsDirectory)
                    ? Path.Combine(Environment.SystemDirectory, "explorer.exe")
                    : Path.Combine(windowsDirectory, "explorer.exe");
                arguments = $"\"{shortcut.TargetPath}\"";
            }
            else if (string.Equals(
                Path.GetExtension(shortcut.TargetPath),
                ".cmd",
                StringComparison.OrdinalIgnoreCase))
            {
                var commandProcessor = Environment.GetEnvironmentVariable("ComSpec");
                targetPath = string.IsNullOrWhiteSpace(commandProcessor)
                    ? Path.Combine(Environment.SystemDirectory, "cmd.exe")
                    : commandProcessor;

                arguments = $"/d /c \"\"{shortcut.TargetPath}\"\"";
            }

            SetComProperty(linkType, shellLink, "TargetPath", targetPath);
            SetComProperty(linkType, shellLink, "Arguments", arguments);
            SetComProperty(linkType, shellLink, "WorkingDirectory", workingDirectory);
            SetComProperty(linkType, shellLink, "Description", shortcut.ShortcutName);

            if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
                SetComProperty(linkType, shellLink, "IconLocation", $"{Path.GetFullPath(iconPath)},0");

            linkType.InvokeMember(
                "Save",
                BindingFlags.InvokeMethod,
                binder: null,
                target: shellLink,
                args: null);

            logger.LogInformation(
                "Created Windows shortcut '{ShortcutPath}' -> '{TargetPath}' {Arguments}.",
                shortcutPath,
                targetPath,
                arguments);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Could not create Windows shortcut '{ShortcutPath}' for target '{TargetPath}'.",
                shortcutPath,
                shortcut.TargetPath);
            throw;
        }
        finally
        {
            if (shellLink is not null && Marshal.IsComObject(shellLink))
                Marshal.FinalReleaseComObject(shellLink);
            if (shell is not null && Marshal.IsComObject(shell))
                Marshal.FinalReleaseComObject(shell);
        }
    }

    private static void SetComProperty(
        Type linkType,
        object shellLink,
        string propertyName,
        string propertyValue)
    {
        linkType.InvokeMember(
            propertyName,
            BindingFlags.SetProperty,
            binder: null,
            target: shellLink,
            args: [propertyValue]);
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
            if (!Directory.Exists(localGptRoot))
            {
                logger.LogWarning("LocalGPT install root does not exist while resolving the icon: {InstallRoot}", localGptRoot);
                return null;
            }

            string[] knownCandidates =
            [
                Path.Combine(localGptRoot, "wwwroot", "favicon.ico"),
                Path.Combine(localGptRoot, "favicon.ico"),
                Path.Combine(localGptRoot, "LocalGPT.exe")
            ];

            foreach (var candidate in knownCandidates)
            {
                if (File.Exists(candidate))
                {
                    logger.LogInformation("Resolved LocalGPT shortcut icon: {IconPath}", candidate);
                    return candidate;
                }
            }

            logger.LogWarning("No LocalGPT shortcut icon was found under the canonical install root: {InstallRoot}", localGptRoot);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not resolve the LocalGPT shortcut icon.");
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
                logger.LogWarning(
                    "LocalGPT install root does not exist while searching for '{FileName}': {InstallRoot}",
                    fileName,
                    localGptRoot);
                return null;
            }

            var directPath = Path.Combine(localGptRoot, fileName);
            if (File.Exists(directPath))
                return directPath;

            logger.LogWarning(
                "Expected LocalGPT launcher is missing from the canonical install root: {LauncherPath}",
                directPath);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Could not resolve LocalGPT file '{FileName}' under '{InstallRoot}'.",
                fileName,
                localGptRoot);
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
                if (File.Exists(explicitPath))
                    return Path.GetFullPath(explicitPath);

                logger.LogWarning("--localgpt-exe does not exist: {ExecutablePath}", explicitPath);
            }

            var localGptRoot = GetLocalGptInstallRoot(logger);
            var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "LocalGPT.exe"
                : "LocalGPT";
            var executablePath = Path.Combine(localGptRoot, executableName);

            if (File.Exists(executablePath))
            {
                logger.LogInformation("Resolved LocalGPT executable: {ExecutablePath}", executablePath);
                return executablePath;
            }

            logger.LogWarning(
                "LocalGPT executable is missing from the canonical install location: {ExecutablePath}",
                executablePath);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not resolve the LocalGPT executable.");
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
            logger.LogError(ex, "Windows-only feature {FeatureName} was rejected.", featureName);
            throw;
        }
    }

    private static string GetLocalGptInstallRoot(ILogger logger)
    {
        try
        {
            var installRoot = GetCanonicalLocalGptInstallRoot();
            logger.LogDebug("Resolved canonical LocalGPT application directory: {InstallRoot}", installRoot);
            return installRoot;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not resolve the LocalGPT install root.");
            throw;
        }
    }

    private static string GetCanonicalLocalGptInstallRoot()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
            throw new InvalidOperationException("LOCALAPPDATA could not be resolved.");

        return Path.Combine(localAppData, "LocalGPT");
    }

    private static string GetLocalGptDataRoot(ILogger logger)
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
            logger.LogError(ex, "Could not resolve the LocalGPT user-data root.");
            throw;
        }
    }

    private static bool IsPathWithinRoot(string path, string root)
    {
        var fullPath = Path.GetFullPath(path);
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(fullPath, fullRoot, comparison)
            || fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, comparison);
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

            var programsRoot = Path.GetFullPath(Path.Combine(startMenu, "Programs"));
            var candidate = Path.GetFullPath(Path.Combine(programsRoot, groupName));
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!candidate.StartsWith(programsRoot + Path.DirectorySeparatorChar, comparison))
                throw new InvalidOperationException("The Start Menu group name escapes the Programs directory.");

            return candidate;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not resolve the LocalGPT Start Menu folder.");
            throw;
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

            var result = value.Trim().Trim('.');
            if (string.IsNullOrWhiteSpace(result) || result is "." or "..")
                return "LocalGPT by Michi0403";

            return result.Length <= 100 ? result : result[..100];
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
            logger.LogError(ex, "Could not resolve the Desktop folder.");
            throw;
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
                ExtractZipSafely(zipPath, targetPath, logger);
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

            logger.LogInformation($"Starting LocalGPT: {exePath}");
            logger.LogInformation($"LocalGPT requested loopback port: {port}");

            if (TryGetRunningEndpoint(
                "LocalGPT",
                "LocalGPT",
                out var existingUrl,
                out var existingProcessId,
                out var existingProcessPath,
                logger))
            {
                var canonicalRoot = GetLocalGptInstallRoot(logger);
                if (options.InstallLocalGptWin
                    && !string.IsNullOrWhiteSpace(existingProcessPath)
                    && !IsPathWithinRoot(existingProcessPath, canonicalRoot))
                {
                    throw new InvalidOperationException(
                        $"A previous LocalGPT process is still running from '{existingProcessPath}' (PID {existingProcessId}). " +
                        $"The new application is installed at '{canonicalRoot}'. Close the old process and run LocalGPT Start again.");
                }

                Console.WriteLine();
                Console.WriteLine($"LocalGPT is already running: {existingUrl}");
                if (!string.IsNullOrWhiteSpace(existingProcessPath))
                    Console.WriteLine($"Executable: {existingProcessPath}");
                Console.WriteLine("Ctrl+click the URL above if your console does not open links on a normal click.");
                if (options.OpenBrowser)
                    OpenDefaultBrowser(existingUrl, logger);
                return;
            }

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                ArgumentList = { "--port", port.ToString() },
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(exePath)
            }) ?? throw new InvalidOperationException("LocalGPT process could not be started.");

            var url = WaitForRuntimeEndpoint(
                productName: "LocalGPT",
                runtimeProductDirectory: "LocalGPT",
                process: process,
                fallbackPort: port,
                logger: logger);

            Console.WriteLine();
            Console.WriteLine($"LocalGPT is ready: {url}");
            Console.WriteLine("Ctrl+click the URL above if your console does not open links on a normal click.");
            logger.LogInformation("LocalGPT is ready at {BaseUrl}.", url);

            if (options.OpenBrowser)
            {
                logger.LogInformation($"Opening browser: {url}");
                OpenDefaultBrowser(url, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LocalGPT startup failed.");
            throw;
        }
    }
    private static bool TryGetRunningEndpoint(
        string productName,
        string runtimeProductDirectory,
        out string url,
        out int processId,
        out string? processPath,
        ILogger logger)
    {
        url = string.Empty;
        processId = 0;
        processPath = null;
        var endpointPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            runtimeProductDirectory,
            "runtime",
            "server.json");
        try
        {
            if (!File.Exists(endpointPath))
                return false;

            using var document = JsonDocument.Parse(File.ReadAllText(endpointPath));
            var root = document.RootElement;
            if (!root.TryGetProperty("ProcessId", out var processIdElement)
                || !processIdElement.TryGetInt32(out processId)
                || processId <= 0
                || !root.TryGetProperty("BaseUrl", out var baseUrlElement))
                return false;

            var baseUrl = baseUrlElement.GetString();
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
                || !uri.IsLoopback
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return false;

            using var process = Process.GetProcessById(processId);
            process.Refresh();
            if (process.HasExited)
                return false;

            try
            {
                processPath = process.MainModule?.FileName;
            }
            catch
            {
                processPath = null;
            }

            url = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            logger.LogInformation(
                "Using already running {ProductName} process {ProcessId} at {BaseUrl}. Executable: {ExecutablePath}",
                productName,
                processId,
                url,
                processPath ?? "(unavailable)");
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (IOException ex)
        {
            logger.LogDebug(ex, "Could not inspect the existing {ProductName} runtime endpoint.", productName);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogDebug(ex, "Could not inspect the existing {ProductName} runtime endpoint.", productName);
            return false;
        }
        catch (JsonException ex)
        {
            logger.LogDebug(ex, "Ignored an invalid existing {ProductName} runtime endpoint file.", productName);
            return false;
        }
    }

    private static string WaitForRuntimeEndpoint(
        string productName,
        string runtimeProductDirectory,
        Process process,
        int fallbackPort,
        ILogger logger)
    {
        var endpointPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            runtimeProductDirectory,
            "runtime",
            "server.json");
        var fallbackUrl = $"http://127.0.0.1:{fallbackPort}";
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        Exception? lastReadFailure = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            process.Refresh();
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"{productName} exited with code {process.ExitCode} before publishing its runtime URL.");
            }

            if (File.Exists(endpointPath))
            {
                try
                {
                    using var document = JsonDocument.Parse(File.ReadAllText(endpointPath));
                    var root = document.RootElement;
                    if (!root.TryGetProperty("ProcessId", out var processIdElement)
                        || !processIdElement.TryGetInt32(out var endpointProcessId)
                        || endpointProcessId != process.Id)
                    {
                        Thread.Sleep(250);
                        continue;
                    }

                    if (!root.TryGetProperty("BaseUrl", out var baseUrlElement))
                    {
                        throw new JsonException("Runtime endpoint file does not contain BaseUrl.");
                    }

                    var baseUrl = baseUrlElement.GetString();
                    if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
                        || !uri.IsLoopback
                        || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                    {
                        throw new InvalidDataException(
                            $"{productName} published an invalid non-loopback runtime URL '{baseUrl}'.");
                    }

                    return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
                }
                catch (IOException ex)
                {
                    lastReadFailure = ex;
                }
                catch (UnauthorizedAccessException ex)
                {
                    lastReadFailure = ex;
                }
                catch (JsonException ex)
                {
                    lastReadFailure = ex;
                }
            }

            Thread.Sleep(250);
        }

        logger.LogError(
            lastReadFailure,
            "{ProductName} did not publish a usable runtime endpoint at {EndpointPath}. Requested fallback was {FallbackUrl}.",
            productName,
            endpointPath,
            fallbackUrl);
        throw new TimeoutException(
            $"{productName} did not become ready within 45 seconds. Requested URL: {fallbackUrl}");
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

    private static async Task<GitHubReleaseSelection> ResolveNewestCompatibleReleaseAsync(
        string repo,
        ILogger logger)
    {
        try
        {
            ValidateRepo(repo, logger);

            var platform = GetPlatformToken();
            var arch = GetArchitectureToken();
            if (string.IsNullOrWhiteSpace(platform) || string.IsNullOrWhiteSpace(arch))
                throw new PlatformNotSupportedException(
                    $"No LocalGPT release asset naming rule exists for platform '{platform}' and architecture '{arch}'.");

            var appAssetName = $"{platform}{arch}.zip";
            var setupAssetName = $"setup{platform}{arch}.zip";
            var candidates = new List<GitHubReleaseSelection>();
            const int perPage = 100;
            const int maximumPages = 10;

            for (var page = 1; page <= maximumPages; page++)
            {
                var releasesUrl =
                    $"https://api.github.com/repos/{repo}/releases?per_page={perPage}&page={page}";
                using var request = CreateGitHubApiRequest(releasesUrl);
                using var response = await Http.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var json = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
                var root = json.RootElement;
                if (root.ValueKind != JsonValueKind.Array)
                    throw new InvalidDataException($"GitHub returned a non-array release response for {repo}.");

                foreach (var release in root.EnumerateArray())
                {
                    if (IsTrue(release, "draft") || IsTrue(release, "prerelease"))
                        continue;

                    var candidate = TryCreateReleaseSelection(
                        repo,
                        release,
                        appAssetName,
                        setupAssetName,
                        "release-list");
                    if (candidate is not null)
                        candidates.Add(candidate);
                }

                if (root.GetArrayLength() < perPage)
                    break;
            }

            var selected = candidates
                .OrderByDescending(candidate => candidate.PublishedAt)
                .ThenByDescending(candidate => candidate.TagRank)
                .ThenByDescending(candidate => candidate.CreatedAt)
                .FirstOrDefault();

            if (selected is null)
            {
                logger.LogWarning(
                    $"No complete published {repo} release containing both {appAssetName} and {setupAssetName} " +
                    "was found in the release list. Falling back to GitHub's latest-release endpoint.");
                selected = await ResolveLatestEndpointReleaseAsync(
                    repo,
                    appAssetName,
                    setupAssetName).ConfigureAwait(false);
            }

            logger.LogInformation(
                $"Resolved newest compatible {repo} release {selected.TagName} from {selected.ResolutionSource}. " +
                $"Published {selected.PublishedAt:O}; normalized tag rank {selected.TagRank}.");
            return selected;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not resolve the newest compatible GitHub release for {repo}.");
            throw;
        }
    }

    private static async Task<GitHubReleaseSelection> ResolveLatestEndpointReleaseAsync(
        string repo,
        string appAssetName,
        string setupAssetName)
    {
        var latestUrl = $"https://api.github.com/repos/{repo}/releases/latest";
        using var request = CreateGitHubApiRequest(latestUrl);
        using var response = await Http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var json = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
        var selected = TryCreateReleaseSelection(
            repo,
            json.RootElement,
            appAssetName,
            setupAssetName,
            "latest-endpoint-fallback");

        return selected
            ?? throw new InvalidOperationException(
                $"GitHub's latest {repo} release does not contain both {appAssetName} and {setupAssetName}.");
    }

    private static GitHubReleaseSelection? TryCreateReleaseSelection(
        string repo,
        JsonElement release,
        string appAssetName,
        string setupAssetName,
        string resolutionSource)
    {
        if (!release.TryGetProperty("tag_name", out var tagElement))
            return null;

        var tagName = tagElement.GetString();
        if (string.IsNullOrWhiteSpace(tagName)
            || !release.TryGetProperty("assets", out var assetsElement)
            || assetsElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var assets = new List<GitHubReleaseAsset>(2);
        foreach (var asset in assetsElement.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString()
                : null;
            if (!string.Equals(name, appAssetName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(name, setupAssetName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var downloadUrl = asset.TryGetProperty("browser_download_url", out var urlElement)
                ? urlElement.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(downloadUrl))
                continue;

            var size = asset.TryGetProperty("size", out var sizeElement)
                       && sizeElement.TryGetInt64(out var parsedSize)
                ? parsedSize
                : 0L;
            assets.Add(new GitHubReleaseAsset(name!, downloadUrl, size));
        }

        if (!assets.Any(asset => string.Equals(asset.Name, appAssetName, StringComparison.OrdinalIgnoreCase))
            || !assets.Any(asset => string.Equals(asset.Name, setupAssetName, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var createdAt = ReadGitHubTimestamp(release, "created_at");
        var publishedAt = ReadGitHubTimestamp(release, "published_at");
        if (publishedAt == DateTimeOffset.MinValue)
            publishedAt = createdAt;

        return new GitHubReleaseSelection(
            repo,
            tagName,
            publishedAt,
            createdAt,
            GetNormalizedTagRank(tagName),
            resolutionSource,
            assets);
    }

    private static async Task DownloadReleaseAssetAsync(
        GitHubReleaseSelection release,
        string outFile,
        ILogger logger,
        CliOptions options,
        bool setupAsset)
    {
        try
        {
            var expectedAssetName =
                $"{(setupAsset ? "setup" : string.Empty)}{GetPlatformToken()}{GetArchitectureToken()}.zip";
            var selected = release.Assets.FirstOrDefault(asset =>
                string.Equals(asset.Name, expectedAssetName, StringComparison.OrdinalIgnoreCase));
            if (selected is null)
                throw new InvalidOperationException(
                    $"Release {release.TagName} does not contain required asset {expectedAssetName}.");

            if (!Uri.TryCreate(selected.DownloadUrl, UriKind.Absolute, out var releaseUri)
                || releaseUri.Scheme != Uri.UriSchemeHttps
                || !releaseUri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Selected release asset {selected.Name} did not use an approved GitHub HTTPS URL.");
            }

            logger.LogInformation($"Selected release: {release.TagName}");
            logger.LogInformation($"Selected asset: {selected.Name}");
            logger.LogInformation($"Downloading {selected.Name} to {outFile}");
            await DownloadFileAsync(selected.DownloadUrl, outFile, logger, options).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                $"Could not download release {release.TagName} asset. outFile={outFile}, setupAsset={setupAsset}");
            throw;
        }
    }

    private static HttpRequestMessage CreateGitHubApiRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache, no-store");
        request.Headers.TryAddWithoutValidation("Pragma", "no-cache");
        return request;
    }

    private static DateTimeOffset ReadGitHubTimestamp(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return DateTimeOffset.MinValue;

        return DateTimeOffset.TryParse(property.GetString(), out var timestamp)
            ? timestamp
            : DateTimeOffset.MinValue;
    }

    private static bool IsTrue(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
               && property.ValueKind == JsonValueKind.True;
    }

    private static long GetNormalizedTagRank(string tagName)
    {
        var digits = new string(tagName.Where(char.IsDigit).ToArray());
        return long.TryParse(digits, out var rank) ? rank : -1L;
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
            logger.LogError(ex, "GitHub source download failed for {Repository}.", repo);
            throw;
        }
    }
    private static async Task<string> GetGitHubDefaultBranchCommitShaAsync(
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
            logger.LogError(ex, "Could not resolve the default-branch commit for {Repository}.", repo);
            throw;
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

            var json = JsonSerializer.Serialize(manifest, ManifestJsonOptions);

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
            throw;
        }


    }
    private static async Task MoveFileWithRetryAsync(string source, string destination, ILogger logger, CliOptions options)
    {
        try
        {
            _ = options;
            for (var i = 1; i <= 10; i++)
            {
                try
                {
                    if (!File.Exists(source))
                        throw new FileNotFoundException($"Source file for move does not exist: {source}", source);

                    // The destination is a download-cache file, not an installation directory.
                    // Replacing an older archive must not require --force-delete. Because source
                    // and destination are created in the same directory, File.Move(overwrite:true)
                    // finalizes the validated .part file without exposing a half-written ZIP.
                    File.Move(source, destination, overwrite: true);
                    return;
                }
                catch (Exception ex) when ((ex is IOException or UnauthorizedAccessException) && i < 10)
                {
                    logger.LogWarning(ex, "Could not finalize downloaded file {Source} as {Destination}. Retry {Attempt}/10.", source, destination, i);
                    await Task.Delay(TimeSpan.FromMilliseconds(300 * i)).ConfigureAwait(false);
                }
            }

            if (!File.Exists(source))
                throw new FileNotFoundException($"Source file for move does not exist: {source}", source);

            File.Move(source, destination, overwrite: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in MoveFileWithRetryAsync. source {Source} destination {Destination}", source, destination);
            throw;
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

            var safePath = EnsureSafeDeleteTarget(path);
            logger.LogWarning("Deleting explicitly reviewed target because --force-delete was used: {Path}", safePath);

            var attrs = File.GetAttributes(safePath);
            if (attrs.HasFlag(FileAttributes.ReparsePoint))
                throw new InvalidOperationException($"Refusing to delete reparse-point target: {safePath}");
            if (attrs.HasFlag(FileAttributes.Directory))
                Directory.Delete(safePath, recursive: true);
            else
                File.Delete(safePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in DeleteIfExists. path {Path}", path);
            throw;
        }
    }

    private static string EnsureSafeDeleteTarget(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Delete target is required.", nameof(path));

        var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        var rootPath = Path.TrimEndingDirectorySeparator(Path.GetPathRoot(fullPath) ?? string.Empty);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (string.IsNullOrWhiteSpace(rootPath) || string.Equals(fullPath, rootPath, comparison))
            throw new InvalidOperationException($"Refusing to delete filesystem root: {fullPath}");

        string[] protectedPaths =
        [
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            AppContext.BaseDirectory,
            Environment.CurrentDirectory
        ];

        foreach (var protectedPath in protectedPaths.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var normalizedProtected = Path.TrimEndingDirectorySeparator(Path.GetFullPath(protectedPath));
            if (string.Equals(fullPath, normalizedProtected, comparison) ||
                normalizedProtected.StartsWith(fullPath + Path.DirectorySeparatorChar, comparison))
            {
                throw new InvalidOperationException($"Refusing to delete a protected path or one of its parents: {fullPath}");
            }
        }

        return fullPath;
    }

    private static void ExtractReleaseArchiveSafely(
        string zipPath,
        string targetPath,
        ILogger logger)
    {
        try
        {
            Directory.CreateDirectory(targetPath);
            var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(targetPath));
            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            using var archive = ZipFile.OpenRead(zipPath);
            var fileEntries = archive.Entries
                .Where(entry => !string.IsNullOrEmpty(entry.Name))
                .ToArray();

            if (fileEntries.Length == 0)
                throw new InvalidDataException($"Release archive contains no files: {zipPath}");

            var topLevelSegments = fileEntries
                .Select(entry => NormalizeArchiveEntryPath(entry.FullName))
                .Select(path => path.Split('/', 2, StringSplitOptions.RemoveEmptyEntries))
                .ToArray();

            string? wrapperPrefix = null;
            if (topLevelSegments.All(parts => parts.Length == 2)
                && topLevelSegments
                    .Select(parts => parts[0])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() == 1)
            {
                wrapperPrefix = topLevelSegments[0][0] + "/";
                logger.LogInformation(
                    "Removing release ZIP wrapper directory '{WrapperDirectory}' while extracting '{ArchivePath}'.",
                    wrapperPrefix.TrimEnd('/'),
                    zipPath);
            }

            foreach (var entry in archive.Entries)
            {
                var relativePath = NormalizeArchiveEntryPath(entry.FullName);
                if (!string.IsNullOrWhiteSpace(wrapperPrefix)
                    && relativePath.StartsWith(wrapperPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    relativePath = relativePath[wrapperPrefix.Length..];
                }

                relativePath = relativePath.TrimStart('/');
                if (string.IsNullOrWhiteSpace(relativePath))
                    continue;

                var destination = Path.GetFullPath(
                    Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
                if (!destination.StartsWith(root + Path.DirectorySeparatorChar, comparison))
                    throw new InvalidDataException($"Archive entry escapes extraction root: {entry.FullName}");

                var unixFileType = (entry.ExternalAttributes >> 16) & 0xF000;
                if (unixFileType == 0xA000)
                    throw new InvalidDataException($"Archive symlink entries are not allowed: {entry.FullName}");

                if (string.IsNullOrEmpty(entry.Name)
                    || relativePath.EndsWith("/", StringComparison.Ordinal))
                {
                    Directory.CreateDirectory(destination);
                    continue;
                }

                var destinationDirectory = Path.GetDirectoryName(destination);
                if (!string.IsNullOrWhiteSpace(destinationDirectory))
                    Directory.CreateDirectory(destinationDirectory);

                entry.ExtractToFile(destination, overwrite: true);

                if (!OperatingSystem.IsWindows())
                {
                    var permissionBits = (entry.ExternalAttributes >> 16) & 0x1FF;
                    if (permissionBits != 0)
                    {
                        try
                        {
                            File.SetUnixFileMode(destination, (UnixFileMode)permissionBits);
                        }
                        catch (Exception modeException)
                        {
                            logger.LogWarning(
                                modeException,
                                "Could not restore Unix file mode for '{DestinationPath}'.",
                                destination);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Release ZIP extraction failed. zipPath {ZipPath} targetPath {TargetPath}",
                zipPath,
                targetPath);
            throw;
        }
    }

    private static string NormalizeArchiveEntryPath(string path) =>
        path.Replace('\\', '/');

    private static void ExtractZipSafely(string zipPath, string targetPath, ILogger logger)
    {
        try
        {
            ValidateArchiveEntries(zipPath, targetPath);
            Directory.CreateDirectory(targetPath);
            ZipFile.ExtractToDirectory(zipPath, targetPath, overwriteFiles: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Safe ZIP extraction failed. zipPath {ZipPath} targetPath {TargetPath}", zipPath, targetPath);
            throw;
        }
    }

    private static void ValidateArchiveEntries(string zipPath, string targetPath)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(targetPath));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            var destination = Path.GetFullPath(Path.Combine(root, entry.FullName));
            if (!string.Equals(destination, root, comparison) &&
                !destination.StartsWith(root + Path.DirectorySeparatorChar, comparison))
            {
                throw new InvalidDataException($"Archive entry escapes extraction root: {entry.FullName}");
            }

            var unixFileType = (entry.ExternalAttributes >> 16) & 0xF000;
            if (unixFileType == 0xA000)
                throw new InvalidDataException($"Archive symlink entries are not allowed: {entry.FullName}");
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
            logger.LogError(ex, "Could not sanitize a repository file name.");
            throw;
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
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LocalGptSetupTool", "1.0"));
        client.Timeout = TimeSpan.FromMinutes(20);
        return client;
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
            options.InstallLocalGptWin = true;
            options.StartLocalGpt = true;
            options.Range = ModelRange.Slim;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                options.InstallOllama = true;
                options.PullOllamaModels = true;
                options.DesktopShortcuts = true;
                options.StartMenuShortcuts = true;
            }

            return options;
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
                        throw new ArgumentOutOfRangeException(
                            nameof(args),
                            options.LocalGptPort,
                            "The value supplied for --port must be between 1 and 65535.");
                    break;

                case "--no-browser":
                    options.OpenBrowser = false;
                    break;

                case "--force":
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
  localgpt-setup --import-recommended --force-delete
  localgpt-setup --all --range Slim --force-delete

Options:
  --install-ollama           Install Ollama by downloading and running the official Windows EXE installer.
  --pull-models              Pull Ollama models.
  --range <Slim|RTX3060|Full> Model set to pull. Default: Slim.
  --install-localgpt         Download and install latest LocalGPT Windows release.
  --setup-learning-base      Prepare/import repositories into the learning base path.
  --import-recommended       Import the hardcoded recommended repository list.
  --repo <owner/repo>         Import one extra GitHub repository. Can be repeated.
  --learnbase <path>          Learning base target path. Default: C:\learnbaseforlocalgpt.
  --start-localgpt           Start LocalGPT from %LOCALAPPDATA%\LocalGPT. User data remains in %LOCALAPPDATA%\LocalGPT.
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
  --uninstall                Preview LocalGPT uninstall. Shows what would be removed, deletes nothing. DOESN'T TOUCH OLLAMA OR ITS MODELS
  --uninstall --force-delete Actually remove LocalGPT app files, launchers, and shortcuts. The learning base is preserved. DOESN'T TOUCH OLLAMA OR ITS MODELS
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

