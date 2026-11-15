using LocalGPT.BusinessObjects;
using LocalGptConfigurationRoot = LocalGPT.BusinessObjects.ConfigurationRoot;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LocalGPT.Services;

/// <summary>
/// Runs the packaged LoRA trainer on request and builds identity datasets from facts LocalGPT already owns. Every limit, timeout, path,
/// file name and hyperparameter comes from LocalGPT runtime values; nothing runs in the background and nothing is imported automatically.
/// </summary>
/// <param name="runtimePolicy">Runtime policy service that owns every tunable value of this workflow.</param>
/// <param name="paths">Application path service used to locate the per-user data folder that holds the training root.</param>
/// <param name="runtime">Local AI runtime service whose Python discovery is reused when no executable override is configured.</param>
/// <param name="options">Configuration monitor that exposes the Python executable already bound to the LocalGPT runtime.</param>
/// <param name="platform">Platform runtime service that owns host path comparison semantics.</param>
/// <param name="environment">Web host environment used to resolve the packaged trainer script.</param>
/// <param name="teams">Council team configuration service that provides the configured teams and role names.</param>
/// <param name="projectMaintenance">Project maintenance service that provides the stored toolchain installations.</param>
/// <param name="peers">One wire peer registry used to list peer names and versions.</param>
/// <param name="connections">One wire connection registry used to keep only connected peers.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ModelTrainingService(
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILocalGptApplicationPathService paths,
    ILocalAiRuntimeService runtime,
    IOptionsMonitor<LocalGptConfigurationRoot> options,
    IPlatformRuntimeService platform,
    IWebHostEnvironment environment,
    ICouncilTeamConfigurationService teams,
    IProjectMaintenanceService projectMaintenance,
    IOneWirePeerRegistry peers,
    IOneWireConnectionRegistry connections,
    ILogger<ModelTrainingService> logger) : IModelTrainingService
{
    /// <summary>
    /// Runs the trainer probe and returns its single JSON object.
    /// </summary>
    /// <param name="parametersBillions">Model size in billions of parameters, or null to probe the machine without a memory estimate.</param>
    /// <param name="loraRank">LoRA rank for the estimate, or null to use the rank of the runtime value ModelTrainingDefaultsJson.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The JSON object printed by the trainer probe.</returns>
    public async Task<string> ProbeAsync(double? parametersBillions, int? loraRank, CancellationToken cancellationToken = default)
    {
        try
        {
            var arguments = new List<string> { "probe" };
            if (parametersBillions is not null)
            {
                var rank = loraRank ?? ReadTrainingDefaults()["lora_rank"]?.GetValue<int>()
                    ?? throw new InvalidOperationException("The runtime value ModelTrainingDefaultsJson has no lora_rank for the memory estimate.");
                arguments.Add("--parameters-billions");
                arguments.Add(parametersBillions.Value.ToString(CultureInfo.InvariantCulture));
                arguments.Add("--lora-rank");
                arguments.Add(rank.ToString(CultureInfo.InvariantCulture));
            }
            else if (loraRank is not null)
                throw new InvalidOperationException("loraRank needs parametersBillions; give both or neither.");

            var timeout = TimeSpan.FromSeconds(runtimePolicy.GetInt("ModelTrainingProbeTimeoutSeconds"));
            var run = await RunTrainerAsync(arguments, timeout, "ModelTrainingProbeTimeoutSeconds", cancellationToken).ConfigureAwait(false);
            return ParseTrainerResult("probe", run.ExitCode, run.LastLine);
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ProbeAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ProbeAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Builds the identity train and eval files in a new job folder under the training root.
    /// </summary>
    /// <param name="modelKey">Name of the derived model the dataset teaches; it is used in the answers and in the job folder name.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The new job folder and the number of training and evaluation samples written.</returns>
    public async Task<(string JobFolder, int TrainSamples, int EvalSamples)> BuildIdentityDatasetAsync(string modelKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = (modelKey ?? string.Empty).Trim();
            var maximumLength = runtimePolicy.GetInt("ModelTrainingModelKeyMaximumLength");
            if (key.Length == 0 || key.Length > maximumLength || key.Any(character => !(char.IsLetterOrDigit(character) || character is '-' or '_' or '.')))
                throw new InvalidOperationException($"modelKey must have 1 to {maximumLength} letters, digits, '-', '_' or '.' (limit: runtime value ModelTrainingModelKeyMaximumLength).");

            var defaults = ReadTrainingDefaults();
            var evalFraction = defaults["eval_fraction"]?.GetValue<double>()
                ?? throw new InvalidOperationException("The runtime value ModelTrainingDefaultsJson has no eval_fraction.");
            if (evalFraction <= 0 || evalFraction >= 1)
                throw new InvalidOperationException("eval_fraction in the runtime value ModelTrainingDefaultsJson must be greater than 0 and less than 1.");
            var seed = defaults["seed"]?.GetValue<int>()
                ?? throw new InvalidOperationException("The runtime value ModelTrainingDefaultsJson has no seed.");

            var facts = await GatherIdentityFactsAsync(key, cancellationToken).ConfigureAwait(false);
            var templates = ParseRuntimeJson("ModelTrainingIdentityTemplate") as JsonArray
                ?? throw new InvalidOperationException("The runtime value ModelTrainingIdentityTemplate must be a JSON list of question/answer objects.");
            var samples = new List<JsonObject>();
            foreach (var template in templates)
            {
                var question = template?["question"]?.GetValue<string>();
                var answer = template?["answer"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(answer))
                    throw new InvalidOperationException("Every entry of the runtime value ModelTrainingIdentityTemplate needs a question and an answer.");
                var combined = question + answer;
                if (facts.Any(fact => fact.Value.Length == 0 && combined.Contains(fact.Key, StringComparison.Ordinal)))
                    continue;
                foreach (var fact in facts)
                {
                    question = question.Replace(fact.Key, fact.Value, StringComparison.Ordinal);
                    answer = answer.Replace(fact.Key, fact.Value, StringComparison.Ordinal);
                }
                samples.Add(new JsonObject { ["prompt"] = question, ["completion"] = answer });
            }
            if (samples.Count <= 1)
                throw new InvalidOperationException("Fewer than two identity samples could be built; check the runtime value ModelTrainingIdentityTemplate.");

            var random = new Random(seed);
            var shuffled = samples.OrderBy(_ => random.Next()).ToList();
            var evalCount = Math.Clamp((int)Math.Round(shuffled.Count * evalFraction), 1, shuffled.Count - 1);
            var evalSamples = shuffled.Take(evalCount).ToList();
            var trainSamples = shuffled.Skip(evalCount).ToList();

            var jobFolder = Path.Combine(ResolveTrainingRoot(), $"{key}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(jobFolder);
            var encoding = new UTF8Encoding(false);
            await File.WriteAllLinesAsync(Path.Combine(jobFolder, runtimePolicy.GetString("ModelTrainingTrainFileName")), trainSamples.Select(sample => sample.ToJsonString()), encoding, cancellationToken).ConfigureAwait(false);
            await File.WriteAllLinesAsync(Path.Combine(jobFolder, runtimePolicy.GetString("ModelTrainingEvalFileName")), evalSamples.Select(sample => sample.ToJsonString()), encoding, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Prepared identity training job {JobFolder} with {TrainSamples} training and {EvalSamples} evaluation samples.", jobFolder, trainSamples.Count, evalSamples.Count);
            return (jobFolder, trainSamples.Count, evalSamples.Count);
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(BuildIdentityDatasetAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(BuildIdentityDatasetAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Writes the trainer configuration for one prepared job and runs the trainer.
    /// </summary>
    /// <param name="jobFolder">Job folder created by the identity dataset preparation.</param>
    /// <param name="baseModelFolder">Local Hugging Face safetensors model folder used as the base; it must be outside the training root.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The final JSON line printed by the trainer.</returns>
    public async Task<string> TrainAsync(string jobFolder, string baseModelFolder, CancellationToken cancellationToken = default)
    {
        try
        {
            var root = ResolveTrainingRoot();
            if (string.IsNullOrWhiteSpace(jobFolder) || string.IsNullOrWhiteSpace(baseModelFolder))
                throw new InvalidOperationException("jobFolder and baseModelFolder are required.");
            var job = Path.GetFullPath(jobFolder);
            if (!platform.IsSameOrDescendantPath(root, job) || platform.PathsEqual(root, job))
                throw new InvalidOperationException("jobFolder must be a job folder below the training root (runtime value ModelTrainingRootFolderName).");
            var trainFile = Path.Combine(job, runtimePolicy.GetString("ModelTrainingTrainFileName"));
            var evalFile = Path.Combine(job, runtimePolicy.GetString("ModelTrainingEvalFileName"));
            if (!File.Exists(trainFile) || !File.Exists(evalFile))
                throw new InvalidOperationException("The job folder lacks its dataset files (runtime values ModelTrainingTrainFileName and ModelTrainingEvalFileName); prepare it with localgpt.training.identity.prepare.");

            var baseFolder = Path.GetFullPath(baseModelFolder);
            if (!Directory.Exists(baseFolder))
                throw new InvalidOperationException("baseModelFolder does not exist.");
            if (platform.IsSameOrDescendantPath(root, baseFolder) || platform.IsSameOrDescendantPath(baseFolder, root))
                throw new InvalidOperationException("baseModelFolder must be outside the training root and must not contain it (runtime value ModelTrainingRootFolderName); the base model is never modified.");
            if (!Directory.EnumerateFiles(baseFolder, "*.safetensors").Any())
                throw new InvalidOperationException(Directory.EnumerateFiles(baseFolder, "*.gguf").Any()
                    ? "GGUF inference files cannot be trained; use the original Hugging Face safetensors checkpoint as baseModelFolder."
                    : "baseModelFolder contains no *.safetensors weights.");
            var outputFolder = Path.Combine(job, runtimePolicy.GetString("ModelTrainingOutputFolderName"));
            if (Directory.Exists(outputFolder) || File.Exists(outputFolder))
                throw new InvalidOperationException("The output folder of this job already exists (runtime value ModelTrainingOutputFolderName); a trained model is never overwritten, prepare a new job.");

            var config = ReadTrainingDefaults();
            config.Remove("eval_fraction");
            config["base_model"] = baseFolder;
            config["train_dataset"] = trainFile;
            config["eval_dataset"] = evalFile;
            config["output_dir"] = outputFolder;
            var configFile = Path.Combine(job, runtimePolicy.GetString("ModelTrainingConfigFileName"));
            await File.WriteAllTextAsync(configFile, config.ToJsonString(), new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Starting identity training for job {JobFolder}.", job);
            var timeout = TimeSpan.FromSeconds(runtimePolicy.GetInt("ModelTrainingTrainTimeoutSeconds"));
            var run = await RunTrainerAsync(["train", configFile], timeout, "ModelTrainingTrainTimeoutSeconds", cancellationToken).ConfigureAwait(false);
            return ParseTrainerResult("training", run.ExitCode, run.LastLine);
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(TrainAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(TrainAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Collects the identity facts that may be taught: application name and version, machine, operating system, user-data layout, Council teams, toolchains and connected peers.
    /// </summary>
    /// <param name="modelKey">Validated name of the derived model.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A map from template placeholder to its value; an empty value means the fact is not available.</returns>
    private async Task<Dictionary<string, string>> GatherIdentityFactsAsync(string modelKey, CancellationToken cancellationToken)
    {
        try
        {
            var format = ParseRuntimeJson("ModelTrainingIdentityListFormatJson");
            var itemSeparator = ReadFormatPart(format, "itemSeparator");
            var detailOpen = ReadFormatPart(format, "detailOpen");
            var detailClose = ReadFormatPart(format, "detailClose");
            var detailSeparator = ReadFormatPart(format, "detailSeparator");
            var limit = runtimePolicy.GetInt("ModelTrainingIdentityMaximumListItems");

            var assembly = typeof(ModelTrainingService).Assembly.GetName();
            var version = assembly.Version is null ? string.Empty : $"{assembly.Version.Major}.{assembly.Version.Minor}.{assembly.Version.Build}";

            var layout = paths.GetLayout();
            var layoutParts = new[] { layout.RuntimeDirectory, layout.LogsDirectory, layout.KnowledgeDirectory }
                .Select(directory => Path.GetRelativePath(layout.UserDataRoot, directory))
                .Concat(new[] { Path.GetFileName(layout.ConfigurationFile), Path.GetFileName(layout.DatabaseFile) });

            var configuredTeams = await teams.GetTeamsAsync(false, cancellationToken).ConfigureAwait(false);
            var teamText = string.Join(itemSeparator, configuredTeams.Take(limit).Select(team =>
                FormatItem(team.DisplayName, string.Join(detailSeparator, team.Roles.Select(role => role.Role)), detailOpen, detailClose)));

            var installations = await projectMaintenance.GetCompilerInstallationsAsync(cancellationToken).ConfigureAwait(false);
            var toolchainText = string.Join(itemSeparator, installations.Where(installation => installation.IsEnabled)
                .Select(installation => FormatItem(installation.Name, installation.Version, detailOpen, detailClose))
                .Distinct().Take(limit));

            var peerText = string.Join(itemSeparator, peers.GetPeers().Where(peer => connections.IsConnected(peer.PeerId))
                .Select(peer => FormatItem(string.IsNullOrWhiteSpace(peer.Application) ? peer.DisplayName : peer.Application, peer.ApplicationVersion, detailOpen, detailClose))
                .Take(limit));

            return new Dictionary<string, string>
            {
                ["{ModelName}"] = modelKey,
                ["{AppName}"] = assembly.Name ?? string.Empty,
                ["{AppVersion}"] = version,
                ["{MachineName}"] = Environment.MachineName,
                ["{OperatingSystem}"] = RuntimeInformation.OSDescription,
                ["{UserDataLayout}"] = string.Join(itemSeparator, layoutParts),
                ["{Teams}"] = teamText,
                ["{Toolchains}"] = toolchainText,
                ["{Peers}"] = peerText
            };
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(GatherIdentityFactsAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(GatherIdentityFactsAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Formats one list item as its name followed by an optional detail in the configured delimiters.
    /// </summary>
    /// <param name="name">Item name.</param>
    /// <param name="detail">Optional detail such as a version or role list.</param>
    /// <param name="detailOpen">Delimiter placed before the detail.</param>
    /// <param name="detailClose">Delimiter placed after the detail.</param>
    /// <returns>The formatted item text.</returns>
    private string FormatItem(string name, string detail, string detailOpen, string detailClose)
    {
        try
        {
            return string.IsNullOrWhiteSpace(detail) ? name : name + detailOpen + detail + detailClose;
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(FormatItem)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(FormatItem)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Reads one string part of the runtime value ModelTrainingIdentityListFormatJson.
    /// </summary>
    /// <param name="format">Parsed list format object.</param>
    /// <param name="name">Property name of the part.</param>
    /// <returns>The configured string.</returns>
    private string ReadFormatPart(JsonNode format, string name)
    {
        try
        {
            return format[name]?.GetValue<string>()
                ?? throw new InvalidOperationException($"The runtime value ModelTrainingIdentityListFormatJson has no {name}.");
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ReadFormatPart)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ReadFormatPart)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Reads the runtime value ModelTrainingDefaultsJson as a fresh, modifiable JSON object.
    /// </summary>
    /// <returns>The trainer defaults including the eval_fraction split ratio.</returns>
    private JsonObject ReadTrainingDefaults()
    {
        try
        {
            return ParseRuntimeJson("ModelTrainingDefaultsJson") as JsonObject
                ?? throw new InvalidOperationException("The runtime value ModelTrainingDefaultsJson must be a JSON object.");
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ReadTrainingDefaults)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ReadTrainingDefaults)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Parses one JSON runtime value and names the runtime value when it is empty or malformed.
    /// </summary>
    /// <param name="key">Runtime value key.</param>
    /// <returns>The parsed JSON node.</returns>
    private JsonNode ParseRuntimeJson(string key)
    {
        try
        {
            try
            {
                return JsonNode.Parse(runtimePolicy.GetString(key))
                    ?? throw new InvalidOperationException($"The runtime value {key} is empty.");
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException($"The runtime value {key} is not valid JSON: {exception.Message}", exception);
            }
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ParseRuntimeJson)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ParseRuntimeJson)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Resolves the training root below the LocalGPT user-data folder.
    /// </summary>
    /// <returns>The absolute training root path.</returns>
    private string ResolveTrainingRoot()
    {
        try
        {
            return Path.GetFullPath(Path.Combine(paths.GetLayout().UserDataRoot, runtimePolicy.GetString("ModelTrainingRootFolderName")));
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ResolveTrainingRoot)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ResolveTrainingRoot)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Resolves the packaged trainer script from the runtime value ModelTrainingTrainerScriptRelativePath.
    /// </summary>
    /// <returns>The absolute path of the existing trainer script.</returns>
    private string ResolveTrainerScript()
    {
        try
        {
            var script = Path.GetFullPath(Path.Combine(environment.ContentRootPath, runtimePolicy.GetString("ModelTrainingTrainerScriptRelativePath")));
            if (!File.Exists(script))
                throw new FileNotFoundException("The packaged LocalGPT LoRA trainer is missing; check the runtime value ModelTrainingTrainerScriptRelativePath.", script);
            return script;
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ResolveTrainerScript)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ResolveTrainerScript)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Resolves the Python executable: the runtime value override first, then the executable bound to the LocalGPT runtime, then the first discovered Python.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The path of an existing Python executable.</returns>
    private async Task<string> ResolvePythonExecutableAsync(CancellationToken cancellationToken)
    {
        try
        {
            var configuredOverride = runtimePolicy.GetString("ModelTrainingPythonExecutableOverride").Trim();
            if (configuredOverride.Length > 0)
            {
                if (!File.Exists(configuredOverride))
                    throw new FileNotFoundException("The Python executable of the runtime value ModelTrainingPythonExecutableOverride does not exist.", configuredOverride);
                return configuredOverride;
            }
            var bound = options.CurrentValue.PythonCore?.PythonExecutable;
            if (!string.IsNullOrWhiteSpace(bound) && File.Exists(bound))
                return bound;
            var candidates = await runtime.DiscoverPythonAsync(cancellationToken).ConfigureAwait(false);
            var candidate = candidates.FirstOrDefault(item => File.Exists(item.ExecutablePath));
            return candidate?.ExecutablePath
                ?? throw new InvalidOperationException("No Python was found; configure Python for LocalGPT (localai.python.configure) or set the runtime value ModelTrainingPythonExecutableOverride.");
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ResolvePythonExecutableAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ResolvePythonExecutableAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Starts the trainer script with a scrubbed environment, streams its output to the log and stops its process tree when the timeout or the caller cancels.
    /// </summary>
    /// <param name="trainerArguments">Arguments passed to the trainer script after its path.</param>
    /// <param name="timeout">Maximum run time.</param>
    /// <param name="timeoutValueName">Name of the runtime value that owns the timeout, used in the timeout message.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The process exit code and the last non-empty line the trainer printed on standard output.</returns>
    private async Task<(int ExitCode, string LastLine)> RunTrainerAsync(IReadOnlyList<string> trainerArguments, TimeSpan timeout, string timeoutValueName, CancellationToken cancellationToken)
    {
        try
        {
            var script = ResolveTrainerScript();
            var python = await ResolvePythonExecutableAsync(cancellationToken).ConfigureAwait(false);
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = python,
                    WorkingDirectory = Path.GetDirectoryName(script)!,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.StartInfo.ArgumentList.Add(script);
            foreach (var argument in trainerArguments)
                process.StartInfo.ArgumentList.Add(argument);
            var markers = runtimePolicy.GetCollection("ModelTrainingScrubbedEnvironmentMarkers");
            foreach (var name in process.StartInfo.Environment.Keys.ToList())
            {
                if (markers.Any(marker => name.Contains(marker, StringComparison.OrdinalIgnoreCase)))
                    process.StartInfo.Environment.Remove(name);
            }

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);
            if (!process.Start())
                throw new InvalidOperationException("The trainer Python process could not be started.");
            var standardOutput = ReadTrainerOutputAsync(process.StandardOutput, LogLevel.Information, timeoutSource.Token);
            var standardError = ReadTrainerOutputAsync(process.StandardError, LogLevel.Debug, timeoutSource.Token);
            try
            {
                await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
                var lastLine = await standardOutput.ConfigureAwait(false);
                await standardError.ConfigureAwait(false);
                return (process.ExitCode, lastLine);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch (Exception killException)
                {
                    logger.LogDebug(killException, "Stopping the trainer process tree failed.");
                }
                if (!cancellationToken.IsCancellationRequested)
                    throw new InvalidOperationException($"The trainer exceeded the runtime value {timeoutValueName} and was stopped.");
                throw;
            }
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(RunTrainerAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(RunTrainerAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Reads one trainer output stream line by line, logs every non-empty line and remembers the last one.
    /// </summary>
    /// <param name="reader">Redirected output stream of the trainer process.</param>
    /// <param name="level">Log level used for each line.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The last non-empty line, or an empty string when the stream was empty.</returns>
    private async Task<string> ReadTrainerOutputAsync(StreamReader reader, LogLevel level, CancellationToken cancellationToken)
    {
        try
        {
            var lastLine = string.Empty;
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                lastLine = line;
                logger.Log(level, "Model training trainer output: {Line}", line);
            }
            return lastLine;
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ReadTrainerOutputAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ReadTrainerOutputAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Turns the trainer exit code and last output line into the result JSON or an error that carries the trainer's own error and remedy.
    /// </summary>
    /// <param name="operation">Operation name used in the error message.</param>
    /// <param name="exitCode">Exit code of the trainer process.</param>
    /// <param name="lastLine">Last non-empty standard output line.</param>
    /// <returns>The last output line when the trainer succeeded.</returns>
    private string ParseTrainerResult(string operation, int exitCode, string lastLine)
    {
        try
        {
            if (exitCode == 0 && lastLine.Length > 0)
                return lastLine;
            throw new InvalidOperationException($"The trainer {operation} did not succeed (exit code {exitCode}). {DescribeTrainerError(lastLine)}");
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ParseTrainerResult)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ParseTrainerResult)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Extracts the error and remedy fields of a trainer error object.
    /// </summary>
    /// <param name="line">Last standard output line of the trainer.</param>
    /// <returns>The trainer's error text with its remedy, or a pointer to the log when the line is not an error object.</returns>
    private string DescribeTrainerError(string line)
    {
        try
        {
            try
            {
                if (JsonNode.Parse(line) is JsonObject failure && failure["error"] is JsonNode error)
                    return $"{error.GetValue<string>()} Remedy: {failure["remedy"]?.GetValue<string>()}";
            }
            catch (JsonException exception)
            {
                logger.LogDebug(exception, "The last trainer output line is not a JSON error object.");
            }
            return "The trainer printed no error object; see the LocalGPT log for its output.";
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(DescribeTrainerError)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(DescribeTrainerError)} failed.");
            throw;
        }
    }
}
