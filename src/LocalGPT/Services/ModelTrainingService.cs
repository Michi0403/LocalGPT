using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
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
/// <param name="runtime">Local AI runtime service that resolves the managed Python environment used for package installation and training.</param>
/// <param name="pythonJobs">Owner of Python process capacity, progress and cancellation.</param>
/// <param name="platform">Platform runtime service that owns host path comparison semantics.</param>
/// <param name="environment">Web host environment used to resolve the packaged trainer script.</param>
/// <param name="teams">Council team configuration service that provides the configured teams and role names.</param>
/// <param name="projectMaintenance">Project maintenance service that provides the stored toolchain installations.</param>
/// <param name="peers">One wire peer registry used to list peer names and versions.</param>
/// <param name="connections">One wire connection registry used to keep only connected peers.</param>
/// <param name="knowledge">Council knowledge service that provides reviewed source revisions for training.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed partial class ModelTrainingService(
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILocalGptApplicationPathService paths,
    ILocalAiRuntimeService runtime,
    IPythonJobOrchestrationService pythonJobs,
    IPlatformRuntimeService platform,
    IWebHostEnvironment environment,
    ICouncilTeamConfigurationService teams,
    IProjectMaintenanceService projectMaintenance,
    IOneWirePeerRegistry peers,
    IOneWireConnectionRegistry connections,
    ICouncilKnowledgeService knowledge,
    ILogger<ModelTrainingService> logger) : IModelTrainingService
{
    /// <summary>
    /// Runs the trainer probe and returns its single JSON object.
    /// </summary>
    /// <param name="parametersBillions">Model size in billions of parameters, or null to probe the machine without a memory estimate.</param>
    /// <param name="loraRank">LoRA rank for the estimate, or null to use the rank of the runtime value ModelTrainingDefaultsJson.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The JSON object printed by the trainer probe.</returns>
    public Task<string> ProbeAsync(double? parametersBillions, int? loraRank, CancellationToken cancellationToken = default)
        => ProbeBackendAsync(parametersBillions, loraRank, ReadTrainingDefaults(), cancellationToken);

    private async Task<string> ProbeBackendAsync(double? parametersBillions, int? loraRank, JsonObject defaults, CancellationToken cancellationToken)
    {
        try
        {
            var backend = defaults["backend"]?.GetValue<string>()
                ?? throw new InvalidOperationException("Set backend in the ModelTrainingDefaultsJson system setting.");
            var arguments = new List<string> { "probe", "--backend", backend };
            if (defaults["allow_cpu"]?.GetValue<bool>() == true)
                arguments.Add("--allow-cpu");
            if (parametersBillions is not null)
            {
                var rank = loraRank ?? defaults["lora_rank"]?.GetValue<int>()
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
            var probe = JsonNode.Parse(ParseTrainerResult("probe", run.ExitCode, run.LastLine))!.AsObject();
            probe["python_source"] = string.IsNullOrWhiteSpace(runtimePolicy.GetString("ModelTrainingPythonExecutableOverride"))
                ? "LocalGPT managed environment" : "ModelTrainingPythonExecutableOverride";
            return probe.ToJsonString();
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

    /// <inheritdoc />
    public async Task<string> PrepareAsync(bool userConfirmed, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Approve preparing the LoRA training environment first.");
            if (!string.IsNullOrWhiteSpace(runtimePolicy.GetString("ModelTrainingPythonExecutableOverride")))
                throw new InvalidOperationException("Training uses ModelTrainingPythonExecutableOverride. Use Check training readiness to inspect that interpreter, or clear the override in system settings to prepare LocalGPT's managed environment.");

            var status = await runtime.GetStatusAsync(cancellationToken).ConfigureAwait(false);
            if (!status.VirtualEnvironmentExists)
            {
                if (!status.PythonExecutableExists)
                    throw new InvalidOperationException("Select or install Python under Install → Toolchains, then return here to prepare training.");
                await runtime.CreateManagedEnvironmentAsync(userConfirmed, cancellationToken, progress).ConfigureAwait(false);
            }

            var defaults = ReadTrainingDefaults();
            progress?.Report("Checking the training backend in the managed Python environment…");
            var initialProbe = await ProbeBackendAsync(null, null, defaults, cancellationToken).ConfigureAwait(false);
            var probe = JsonNode.Parse(initialProbe)!.AsObject();
            var backend = defaults["backend"]!.GetValue<string>();
            if (probe["training_readiness"]?["ready"]?.GetValue<bool>() == true)
                return initialProbe;
            var preparationBackends = new Dictionary<string, string>(
                runtimePolicy.GetJson<Dictionary<string, string>>("ModelTrainingAutoPreparationBackendsJson"),
                StringComparer.OrdinalIgnoreCase);
            var host = $"{platform.ToolchainPlatform}-{RuntimeInformation.ProcessArchitecture}";
            var detected = probe["backend"]?.GetValue<string>();
            if (backend == "auto")
                backend = detected is "none" or "cpu" or null ? preparationBackends.GetValueOrDefault(host, detected ?? "none") : detected;
            var useMlx = backend == "mlx";
            await runtime.InstallTrainingDependenciesAsync(useMlx, userConfirmed, progress, cancellationToken).ConfigureAwait(false);
            progress?.Report("Checking the installed training APIs and available devices…");
            return await ProbeBackendAsync(null, null, defaults, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "LoRA training preparation was canceled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning("LoRA training preparation failed with {ExceptionType}.", exception.GetType().Name);
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
            var key = ValidateTrainingModelKey(modelKey);

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
            await WritePreparedDatasetAsync(jobFolder, trainSamples.Select(sample => sample.ToJsonString()),
                evalSamples.Select(sample => sample.ToJsonString()), null, cancellationToken).ConfigureAwait(false);
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
                throw new InvalidOperationException("Prepare the job first with localgpt.training.identity.prepare or localgpt.training.knowledge.prepare.");

            var baseFolder = Path.GetFullPath(baseModelFolder);
            if (!Directory.Exists(baseFolder))
                throw new InvalidOperationException("baseModelFolder does not exist.");
            if (platform.IsSameOrDescendantPath(root, baseFolder) || platform.IsSameOrDescendantPath(baseFolder, root))
                throw new InvalidOperationException("baseModelFolder must be outside the training root and must not contain it (runtime value ModelTrainingRootFolderName); the base model is never modified.");
            if (!Directory.EnumerateFiles(baseFolder, "*.safetensors").Any())
                throw new InvalidOperationException(Directory.EnumerateFiles(baseFolder, "*.gguf").Any()
                    ? "GGUF inference files cannot be trained; use the original Hugging Face safetensors checkpoint as baseModelFolder."
                    : "baseModelFolder contains no *.safetensors weights.");
            var attemptId = Guid.NewGuid();
            var attemptFolder = Path.Combine(job, "attempts", attemptId.ToString("N"));
            Directory.CreateDirectory(attemptFolder);
            var outputFolder = Path.Combine(attemptFolder, runtimePolicy.GetString("ModelTrainingOutputFolderName"));
            var config = ReadTrainingDefaults();
            config.Remove("eval_fraction");
            var workerOptions = runtimePolicy.GetJson<Dictionary<string, bool>>("ModelTrainingWorkerOptionsJson");
            foreach (var key in new[] { "async_weight_loading", "isolated_worker_exit" })
            {
                if (workerOptions.TryGetValue(key, out var enabled))
                    config[key] = enabled;
            }
            config["base_model"] = baseFolder;
            var trainSnapshot = Path.Combine(attemptFolder, Path.GetFileName(trainFile));
            var evalSnapshot = Path.Combine(attemptFolder, Path.GetFileName(evalFile));
            config["train_dataset"] = trainSnapshot;
            config["eval_dataset"] = evalSnapshot;
            config["output_dir"] = outputFolder;
            var configFile = Path.Combine(attemptFolder, runtimePolicy.GetString("ModelTrainingConfigFileName"));
            var submittedAtUtc = DateTime.UtcNow;
            var statePath = Path.Combine(attemptFolder, "attempt.json");
            async Task WriteStateAsync(string status)
            {
                var temporary = statePath + ".tmp";
                await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(new
                {
                    attemptId, status, submittedAtUtc, updatedAtUtc = DateTime.UtcNow
                }), CancellationToken.None).ConfigureAwait(false);
                File.Move(temporary, statePath, overwrite: true);
            }

            await WriteStateAsync("queued").ConfigureAwait(false);
            try
            {
                var dataset = await InspectDatasetAsync(job, trainFile, evalFile, cancellationToken).ConfigureAwait(false);
                File.Copy(trainFile, trainSnapshot);
                File.Copy(evalFile, evalSnapshot);
                if (dataset.TrainSha256 != await HashTrainingFileAsync(trainSnapshot, cancellationToken).ConfigureAwait(false)
                    || dataset.EvaluationSha256 != await HashTrainingFileAsync(evalSnapshot, cancellationToken).ConfigureAwait(false))
                    throw new InvalidOperationException("The prepared dataset changed while creating the training attempt.");
                await File.WriteAllTextAsync(configFile, config.ToJsonString(), new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
                var result = await pythonJobs.RunPackagedTrainingAsync(async (report, token) =>
                {
                    await WriteStateAsync("running").ConfigureAwait(false);
                    report(new PythonTrainingProgress("Identifying the base checkpoint", null, null));
                    var baseFiles = await InspectTrainingCheckpointAsync(baseFolder, token).ConfigureAwait(false);
                    var timeout = TimeSpan.FromSeconds(runtimePolicy.GetInt("ModelTrainingTrainTimeoutSeconds"));
                    var run = await RunTrainerAsync(["train", configFile], timeout, "ModelTrainingTrainTimeoutSeconds", token, report).ConfigureAwait(false);
                    var response = JsonNode.Parse(ParseTrainerResult("training", run.ExitCode, run.LastLine))!.AsObject();
                    if (response["status"]?.GetValue<string>() != "adapter_ready"
                        || response["adapter_verification"] is not JsonObject verification)
                        throw new InvalidOperationException("The trainer exited without verifying that the saved adapter loads and runs.");
                    var adapterPath = Path.GetFullPath(response["adapter_path"]?.GetValue<string>()
                        ?? throw new InvalidOperationException("The trainer did not return an adapter folder."));
                    if (!platform.IsSameOrDescendantPath(outputFolder, adapterPath))
                        throw new InvalidOperationException("The trainer returned an adapter folder outside this attempt.");
                    report(new PythonTrainingProgress("Recording adapter provenance", null, null));
                    var adapterFiles = await InspectTrainingCheckpointAsync(adapterPath, token).ConfigureAwait(false);
                    var verifiedBaseFiles = await InspectTrainingCheckpointAsync(baseFolder, token).ConfigureAwait(false);
                    if (!baseFiles.SequenceEqual(verifiedBaseFiles))
                        throw new InvalidOperationException("The base checkpoint changed during training.");
                    var provenance = JsonSerializer.Serialize(new
                    {
                        attemptId, Dataset = dataset, BaseFiles = baseFiles, AdapterFiles = adapterFiles, AdapterVerification = verification,
                        ConfigurationSha256 = await HashTrainingFileAsync(configFile, token).ConfigureAwait(false)
                    });
                    await File.WriteAllTextAsync(Path.Combine(attemptFolder, "provenance.json"), provenance, token).ConfigureAwait(false);
                    response["attempt_id"] = attemptId;
                    await WriteStateAsync("completed").ConfigureAwait(false);
                    return response.ToJsonString();
                }, cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (Exception exception)
            {
                try
                {
                    await WriteStateAsync(exception is OperationCanceledException ? "cancelled" : "failed").ConfigureAwait(false);
                }
                catch (Exception stateException)
                {
                    logger.LogWarning(stateException, "Could not save the terminal status for training attempt {AttemptId}.", attemptId);
                }
                throw;
            }
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
    /// Resolves the explicit training override or the LocalGPT-managed interpreter.
    /// </summary>
    /// <returns>The path of an existing Python executable.</returns>
    private string ResolvePythonExecutable()
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
            return runtime.GetManagedPythonExecutable();
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ResolvePythonExecutable)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(ModelTrainingService)}.{nameof(ResolvePythonExecutable)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Starts the trainer with a scrubbed environment and reports validated progress until completion or cancellation.
    /// </summary>
    /// <param name="trainerArguments">Arguments passed to the trainer script after its path.</param>
    /// <param name="timeout">Maximum run time.</param>
    /// <param name="timeoutValueName">Name of the runtime value that owns the timeout, used in the timeout message.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="progress">Optional recipient for validated trainer phases and step counts.</param>
    /// <returns>The process exit code and the last non-empty line the trainer printed on standard output.</returns>
    private async Task<(int ExitCode, string LastLine)> RunTrainerAsync(IReadOnlyList<string> trainerArguments, TimeSpan timeout, string timeoutValueName, CancellationToken cancellationToken, Action<PythonTrainingProgress>? progress = null)
    {
        try
        {
            var script = ResolveTrainerScript();
            var python = ResolvePythonExecutable();
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
            var standardOutput = ReadTrainerOutputAsync(process.StandardOutput, timeoutSource.Token, progress);
            var standardError = ReadTrainerOutputAsync(process.StandardError, timeoutSource.Token);
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
                    await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception killException)
                {
                    logger.LogDebug(killException, "Stopping the trainer process tree failed.");
                }
                try
                {
                    await Task.WhenAll(standardOutput, standardError).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // The stream reads share the process timeout token.
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
    /// Reads trainer output without persisting raw third-party diagnostics or model content.
    /// </summary>
    /// <param name="reader">Redirected output stream of the trainer process.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="progress">Optional recipient for recognized progress events.</param>
    /// <returns>The last non-empty line, or an empty string when the stream was empty.</returns>
    private async Task<string> ReadTrainerOutputAsync(StreamReader reader, CancellationToken cancellationToken, Action<PythonTrainingProgress>? progress = null)
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
                if (progress is not null && TryReadTrainerProgress(line) is { } update)
                    progress(update);
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

    /// <summary>Reads an allowlisted progress event without retaining arbitrary trainer output.</summary>
    /// <param name="line">One line emitted by the trainer.</param>
    /// <returns>A recognized progress update, or null when the line carries other output.</returns>
    private PythonTrainingProgress? TryReadTrainerProgress(string line)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            var value = document.RootElement;
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("event", out var name)
                || name.ValueKind != JsonValueKind.String)
                return null;
            var phase = name.GetString() switch
            {
                "preflight" => "Memory preflight",
                "start" => "Starting training",
                "loading_model" => "Loading model",
                "baseline_eval" => "Evaluating base model",
                "progress" => "Training",
                "checkpoint" => "Saving checkpoint",
                "eval" => "Evaluating adapter",
                "verifying_adapter" => "Reloading and verifying the saved adapter",
                "result" => "Adapter ready",
                _ => null
            };
            if (phase is null)
                return null;
            int? Number(string key) => value.TryGetProperty(key, out var field) && field.ValueKind == JsonValueKind.Number
                && field.TryGetInt32(out var number) && number >= 0 ? number : null;
            return new PythonTrainingProgress(phase, Number("step"), Number("max_steps"));
        }
        catch (JsonException)
        {
            return null;
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
