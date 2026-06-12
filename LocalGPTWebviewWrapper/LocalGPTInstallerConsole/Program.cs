
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
                logger.LogError(ex, "Error in InstallOllama.");
            }

            try
            {
                if (options.PullOllamaModels)
                {
                    var ollamaExe = EnsureOllamaAvailable(options, logger);
                    StartOllamaServer(ollamaExe, logger);
                    await PullModelsAsync(ollamaExe, GetModelSet(options.Range), logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in PullOllamaModels.");
            }

            try
            {
                if (options.InstallLocalGptWin)
                    await InstallLocalGptAsync(options, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in InstallLocalGptWin.");
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
                        await ImportGitHubSourceToLearningBaseAsync(repo, options, logger);
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
            await DownloadFileAsync("https://ollama.com/download/OllamaSetup.exe", installer, logger);

            logger.LogInformation($"Running official Ollama Windows EXE installer: {installer}");
            await RunProcessAsync(installer, string.Empty, logger);

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

    private static void AddDirectoryToUserPathIfMissing(string directory, ILogger logger)
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
            logger.LogError(ex, $"Error in StartOllamaServer.");
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
                await RunProcessAsync(ollamaExe, $"pull {model}", logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in PullModelsAsync. models {string.Join(", ", models)}");
            throw;
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
                    request.Headers.Accept.ParseAdd("application/octet-stream");
                    request.Headers.Accept.ParseAdd("*/*");

                    using var response = await Http.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cts.Token);

                    response.EnsureSuccessStatusCode();

                    var contentLength = response.Content.Headers.ContentLength;
                    logger.LogInformation(contentLength.HasValue
                        ? $"Remote size: {FormatBytes(contentLength.Value, logger)}"
                        : "Remote size: unknown");

                    await using var input = await response.Content.ReadAsStreamAsync(cts.Token);
                    await using var output = new FileStream(
                        tempFile,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 1024 * 128,
                        useAsync: true);

                    var buffer = new byte[1024 * 128];
                    long totalRead = 0;
                    long lastLoggedBytes = 0;
                    var lastProgress = DateTimeOffset.UtcNow;
                    var lastLog = DateTimeOffset.UtcNow;

                    while (true)
                    {
                        using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
                        readTimeout.CancelAfter(TimeSpan.FromSeconds(45));

                        var read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), readTimeout.Token);
                        if (read == 0)
                            break;

                        await output.WriteAsync(buffer.AsMemory(0, read), cts.Token);
                        totalRead += read;

                        var now = DateTimeOffset.UtcNow;

                        if (totalRead != lastLoggedBytes)
                        {
                            lastProgress = now;
                        }

                        if (now - lastLog > TimeSpan.FromSeconds(1))
                        {
                            if (contentLength.HasValue && contentLength.Value > 0)
                            {
                                var percent = totalRead * 100.0 / contentLength.Value;
                                logger.LogInformation($"Downloaded {FormatBytes(totalRead, logger)} / {FormatBytes(contentLength.Value, logger)} ({percent:F1}%)");
                            }
                            else
                            {
                                logger.LogInformation($"Downloaded {FormatBytes(totalRead, logger)}");
                            }

                            lastLoggedBytes = totalRead;
                            lastLog = now;
                        }

                        if (now - lastProgress > TimeSpan.FromSeconds(60))
                            throw new TimeoutException($"Download stalled for 60 seconds at {FormatBytes(totalRead, logger)}.");
                    }

                    await output.FlushAsync(cts.Token);

                    if (contentLength.HasValue && totalRead != contentLength.Value)
                        throw new IOException($"Incomplete download. Got {totalRead} bytes, expected {contentLength.Value} bytes.");

                    if (totalRead == 0)
                        throw new IOException("Downloaded file is empty.");

                    if (File.Exists(outFile))
                        File.Delete(outFile);

                    File.Move(tempFile, outFile);

                    logger.LogInformation($"Download complete: {outFile} ({FormatBytes(totalRead, logger)})");
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

                    await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in DownloadFileAsync. url {url.ToString()} outFile {outFile.ToString()}");
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
        }

    }

    private static HttpClient? CreateHttpClient()
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
    public string? OllamaExePath { get; private set; }
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
                case "--ollama-exe":
                    options.OllamaExePath = NextValue(args, ref i, arg);
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
  --install-ollama           Install Ollama by downloading and running the official Windows EXE installer.
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
  --ollama-exe <path>        Override Ollama executable path. Default Windows location is %LOCALAPPDATA%\Programs\Ollama\ollama.exe.
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

