using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;

namespace LocalGPT.Services;

/// <summary>
/// Runs a bounded, user-approved benchmark against models already installed in the configured local
/// Ollama runtime. It never pulls models, changes global Ollama state, or overwrites an existing preset.
/// </summary>
public sealed class AdaptiveOllamaBenchmarkWiring(
    IOptionsMonitor<global::LocalGPT.BusinessObjects.ConfigurationRoot> configuration,
    IHardwareInventoryService hardwareInventory,
    IModelPresetService modelPresets,
    ILogger<AdaptiveOllamaBenchmarkWiring> logger) : IDxAiFunctionHandler
{
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.models.benchmark.autotune", "POST", "/api/dxai/functions/localgpt.models.benchmark.autotune/invoke",
        "Benchmarks already-installed local Ollama models with bounded peer-authored and deterministic tasks, stops tuning a model when the next profile improves by less than the configured threshold, and optionally saves a new user-approved model preset.",
        "Optional endpoint, modelNames, maxModels, maxProfilesPerModel, maxTasks, maxSecondsPerCall, improvementThresholdPercent, includePeerAuthoredTask, persistPreset, presetName, makeDefault, maximumContextTokens and maximumOutputTokens.",
        "Calls only a loopback Ollama endpoint, never downloads models, never modifies an existing preset, and requires fresh human confirmation before the benchmark or preset save starts.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, Source: "DIHandler",
        ParameterSchemaJson: """
            {
              "type": "object",
              "properties": {
                "endpoint": { "type": "string" },
                "modelNames": { "type": "array", "items": { "type": "string" }, "maxItems": 24 },
                "maxModels": { "type": "integer", "minimum": 1, "maximum": 24 },
                "maxProfilesPerModel": { "type": "integer", "minimum": 1, "maximum": 6 },
                "maxTasks": { "type": "integer", "minimum": 1, "maximum": 4 },
                "maxSecondsPerCall": { "type": "integer", "minimum": 10, "maximum": 900 },
                "improvementThresholdPercent": { "type": "number", "minimum": 0, "maximum": 50 },
                "includePeerAuthoredTask": { "type": "boolean" },
                "persistPreset": { "type": "boolean" },
                "presetName": { "type": "string", "maxLength": 160 },
                "makeDefault": { "type": "boolean" },
                "maximumContextTokens": { "type": "integer", "minimum": 2048, "maximum": 262144 },
                "maximumOutputTokens": { "type": "integer", "minimum": 128, "maximum": 4096 }
              },
              "additionalProperties": false
            }
            """,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: true);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            if (!request.UserConfirmed)
            {
                return new DxAiFunctionInvocationResult
                {
                    Succeeded = false,
                    Status = "HumanConfirmationRequired",
                    Error = "Fresh human confirmation is required before local models are benchmarked."
                };
            }

            var options = BindOptions(request.Parameters);
            var report = await RunEmpiricalBenchmarkAsync(options, request.UserConfirmed, cancellationToken).ConfigureAwait(false);
            return new DxAiFunctionInvocationResult
            {
                Succeeded = report.Models.Any(model => model.BestScore > 0),
                Status = report.Models.Any(model => model.BestScore > 0) ? "Completed" : "NoSuccessfulModel",
                Value = report,
                Error = report.Models.Any(model => model.BestScore > 0)
                    ? null
                    : "No installed model completed the bounded benchmark. Review the returned warnings and LocalGPT logs."
            };
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Adaptive Ollama benchmark invocation was cancelled; no existing preset was changed.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Adaptive Ollama benchmark invocation failed; prompts and generated model text were omitted from logs.");
            return new DxAiFunctionInvocationResult
            {
                Succeeded = false,
                Status = "Failed",
                Error = "Adaptive Ollama benchmark invocation failed. Review LocalGPT logs for the operation details."
            };
        }
    }

    private AdaptiveOllamaBenchmarkOptions BindOptions(JsonElement parameters)
    {
        try
        {
            var options = parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? new AdaptiveOllamaBenchmarkOptions()
                : JsonSerializer.Deserialize<AdaptiveOllamaBenchmarkOptions>(parameters.GetRawText(), jsonOptions)
                    ?? new AdaptiveOllamaBenchmarkOptions();

            options.MaxModels = Math.Clamp(options.MaxModels, 1, 24);
            options.MaxProfilesPerModel = Math.Clamp(options.MaxProfilesPerModel, 1, 6);
            options.MaxTasks = Math.Clamp(options.MaxTasks, 1, 4);
            options.MaxSecondsPerCall = Math.Clamp(options.MaxSecondsPerCall, 10, 900);
            options.ImprovementThresholdPercent = Math.Clamp(options.ImprovementThresholdPercent, 0d, 50d);
            options.MaximumContextTokens = Math.Clamp(options.MaximumContextTokens, 2048, 262144);
            options.MaximumOutputTokens = Math.Clamp(options.MaximumOutputTokens, 128, 4096);
            options.ModelNames = options.ModelNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(24)
                .ToList();
            options.PresetName = string.IsNullOrWhiteSpace(options.PresetName)
                ? "Adaptive Ollama Benchmark"
                : options.PresetName.Trim()[..Math.Min(options.PresetName.Trim().Length, 160)];
            return options;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Adaptive Ollama benchmark parameters were not valid JSON; parameter content was omitted.");
            throw new InvalidOperationException("Adaptive Ollama benchmark parameters are invalid.", exception);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Binding adaptive Ollama benchmark parameters failed; parameter content was omitted.");
            throw;
        }
    }

    private async Task<AdaptiveOllamaBenchmarkReport> RunEmpiricalBenchmarkAsync(
        AdaptiveOllamaBenchmarkOptions options,
        bool userConfirmed,
        CancellationToken cancellationToken)
    {
        var report = new AdaptiveOllamaBenchmarkReport();
        try
        {
            var endpoint = ResolveLoopbackEndpoint(options.Endpoint);
            report.Endpoint = endpoint.GetLeftPart(UriPartial.Authority);
            var hardware = await hardwareInventory.GetHardwareAsync(cancellationToken).ConfigureAwait(false);
            report.HardwareSummary = BuildHardwareSummary(hardware);

            using var http = new HttpClient
            {
                BaseAddress = endpoint,
                Timeout = Timeout.InfiniteTimeSpan
            };
            var installed = await ReadInstalledModelsAsync(http, cancellationToken).ConfigureAwait(false);
            var selected = SelectModels(installed, options);
            if (selected.Count == 0)
            {
                report.Warnings.Add("The configured loopback Ollama runtime returned no matching installed models.");
                report.CompletedAtUtc = DateTimeOffset.UtcNow;
                return report;
            }

            var deterministicTaskCount = options.IncludePeerAuthoredTask
                ? Math.Max(1, options.MaxTasks - 1)
                : options.MaxTasks;
            var tasks = BuildDeterministicTasks(deterministicTaskCount);
            if (options.IncludePeerAuthoredTask && tasks.Count < options.MaxTasks)
            {
                var author = selected[0];
                var authored = await CreatePeerAuthoredTaskAsync(http, author, options, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(authored))
                {
                    report.PeerAuthoredTask = authored;
                    tasks.Add(new AdaptiveOllamaBenchmarkTask
                    {
                        Name = $"Peer task from {DisplayModelName(author)}",
                        Prompt = authored,
                        ExpectedTokens = [],
                        ExpectJson = false
                    });
                }
            }

            tasks = tasks.Take(options.MaxTasks).ToList();
            var profiles = BuildProfiles(hardware, options);
            foreach (var model in selected)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var modelResult = await BenchmarkModelAsync(
                    http,
                    model,
                    profiles,
                    tasks,
                    options,
                    cancellationToken).ConfigureAwait(false);
                report.Models.Add(modelResult);
            }

            var best = report.Models
                .Where(model => model.BestScore > 0)
                .OrderByDescending(model => model.BestScore)
                .FirstOrDefault();
            if (best is not null)
            {
                report.BestModel = best.ModelName;
                report.BestProfile = best.BestProfile;
                report.BestScore = best.BestScore;
            }

            if (options.PersistPreset && best is not null)
            {
                var saved = await SaveBenchmarkPresetAsync(report, hardware, options, userConfirmed, cancellationToken).ConfigureAwait(false);
                report.SavedPresetId = saved.Id;
                report.SavedPresetName = saved.Name;
            }

            report.CompletedAtUtc = DateTimeOffset.UtcNow;
            logger.LogInformation(
                "Adaptive Ollama benchmark {BenchmarkRunId} completed for {ModelCount} installed model(s); best model={BestModel}; best score={BestScore:F2}; generated text was omitted from logs.",
                report.RunId,
                report.Models.Count,
                string.IsNullOrWhiteSpace(report.BestModel) ? "none" : report.BestModel,
                report.BestScore);
            return report;
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Adaptive Ollama benchmark {BenchmarkRunId} was cancelled.", report.RunId);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Adaptive Ollama benchmark {BenchmarkRunId} failed; prompts and generated model text were omitted from logs.", report.RunId);
            throw;
        }
    }

    private Uri ResolveLoopbackEndpoint(string requestedEndpoint)
    {
        try
        {
            var configured = configuration.CurrentValue.AICore?.OllamaCore?.Uri;
            var value = string.IsNullOrWhiteSpace(requestedEndpoint) ? configured : requestedEndpoint;
            value = string.IsNullOrWhiteSpace(value) ? "http://127.0.0.1:11434" : value.Trim();
            if (!Uri.TryCreate(value.TrimEnd('/') + "/", UriKind.Absolute, out var endpoint))
                throw new InvalidOperationException("The Ollama endpoint is not a valid absolute URI.");
            if (!endpoint.IsLoopback)
                throw new InvalidOperationException("Adaptive benchmarking is restricted to a loopback Ollama endpoint.");
            if (!string.Equals(endpoint.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The Ollama endpoint must use HTTP or HTTPS.");
            return endpoint;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the loopback Ollama benchmark endpoint failed; endpoint credentials and paths were omitted.");
            throw;
        }
    }

    private async Task<List<OllamaBenchmarkModelInfo>> ReadInstalledModelsAsync(
        HttpClient http,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.GetAsync("api/tags", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<OllamaBenchmarkTagsResponse>(jsonOptions, cancellationToken).ConfigureAwait(false)
                ?? new OllamaBenchmarkTagsResponse();
            return payload.Models
                .Where(model => !string.IsNullOrWhiteSpace(DisplayModelName(model)))
                .GroupBy(DisplayModelName, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(model => model.Size <= 0 ? long.MaxValue : model.Size)
                .ThenBy(DisplayModelName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading installed Ollama models for adaptive benchmarking failed.");
            throw new InvalidOperationException("The configured loopback Ollama runtime did not return its installed models.", exception);
        }
    }

    private List<OllamaBenchmarkModelInfo> SelectModels(
        IReadOnlyList<OllamaBenchmarkModelInfo> installed,
        AdaptiveOllamaBenchmarkOptions options)
    {
        try
        {
            var requested = options.ModelNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var selected = installed
                .Where(model => requested.Count == 0 || requested.Contains(DisplayModelName(model)))
                .OrderBy(model => model.Size <= 0 ? long.MaxValue : model.Size)
                .ThenBy(DisplayModelName, StringComparer.OrdinalIgnoreCase)
                .Take(options.MaxModels)
                .ToList();
            logger.LogInformation(
                "Selected {SelectedModelCount} of {InstalledModelCount} installed Ollama model(s) for adaptive benchmarking; model output is not logged.",
                selected.Count,
                installed.Count);
            return selected;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Selecting installed Ollama models for adaptive benchmarking failed.");
            throw;
        }
    }

    private List<AdaptiveOllamaBenchmarkTask> BuildDeterministicTasks(int maximumTasks)
    {
        try
        {
            var tasks = new List<AdaptiveOllamaBenchmarkTask>
            {
                new()
                {
                    Name = "Structured response",
                    Prompt = "Return one compact JSON object with keys action, x and y. Set action to move, x to 3 and y to 4. Emit JSON only.",
                    ExpectedTokens = ["action", "move", "3", "4"],
                    ExpectJson = true
                },
                new()
                {
                    Name = "Shared game control",
                    Prompt = "You control the same 2.5D ASCII game interface as a human. The player is at (2,2), faces east, an enemy is visible at (5,2), and ammo is available. Reply with exactly one control from MOVE_FORWARD, TURN_LEFT, TURN_RIGHT, SHOOT, DUCK or USE.",
                    ExpectedTokens = ["SHOOT"],
                    ExpectJson = false
                },
                new()
                {
                    Name = ".NET repair",
                    Prompt = "In one sentence, explain why a singleton service must not resolve a scoped dependency from the root IServiceProvider and name the correct lifetime rule.",
                    ExpectedTokens = ["singleton", "scoped", "scope"],
                    ExpectJson = false
                }
            };
            return tasks.Take(Math.Clamp(maximumTasks, 1, tasks.Count)).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating deterministic adaptive benchmark tasks failed.");
            throw;
        }
    }

    private async Task<string> CreatePeerAuthoredTaskAsync(
        HttpClient http,
        OllamaBenchmarkModelInfo author,
        AdaptiveOllamaBenchmarkOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = new AdaptiveOllamaTuningProfile
            {
                Name = "PeerTaskAuthor",
                ContextTokens = Math.Min(4096, options.MaximumContextTokens),
                OutputTokens = Math.Min(160, options.MaximumOutputTokens),
                NumBatch = 256,
                OllamaNumGpu = null
            };
            var response = await GenerateAsync(
                http,
                DisplayModelName(author),
                "Write one short, objective benchmark prompt for another local model. It must test practical reasoning for a C# or ASCII-game task and must have a clearly checkable answer. Return only the prompt, no analysis and no answer.",
                profile,
                options.MaxSecondsPerCall,
                cancellationToken).ConfigureAwait(false);
            var candidate = response.Response.Trim();
            if (candidate.Length < 20)
                return string.Empty;
            return candidate[..Math.Min(candidate.Length, 800)];
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "The peer-authored benchmark task could not be generated; deterministic tasks remain available.");
            return string.Empty;
        }
    }

    private List<AdaptiveOllamaTuningProfile> BuildProfiles(
        IReadOnlyList<OneWireHardwareDescriptor> hardware,
        AdaptiveOllamaBenchmarkOptions options)
    {
        try
        {
            var gpu = hardware.FirstOrDefault(item => item.IsOnline && item.Kind is OneWireHardwareKind.Gpu or OneWireHardwareKind.Accelerator);
            var gpuMemoryGiB = gpu is { DedicatedMemoryBytes: > 0 }
                ? gpu.DedicatedMemoryBytes.Value / 1024d / 1024d / 1024d
                : 0d;
            var capacityContext = gpuMemoryGiB switch
            {
                >= 24d => 16384,
                >= 12d => 12288,
                >= 6d => 8192,
                _ => 4096
            };
            capacityContext = Math.Min(capacityContext, options.MaximumContextTokens);

            var profiles = new List<AdaptiveOllamaTuningProfile>
            {
                new()
                {
                    Name = "Low latency auto-GPU",
                    ContextTokens = Math.Min(2048, options.MaximumContextTokens),
                    OutputTokens = Math.Min(256, options.MaximumOutputTokens),
                    NumBatch = 512,
                    OllamaNumGpu = gpu is null ? 0 : null
                },
                new()
                {
                    Name = "Balanced auto-GPU",
                    ContextTokens = Math.Min(4096, options.MaximumContextTokens),
                    OutputTokens = Math.Min(384, options.MaximumOutputTokens),
                    NumBatch = 256,
                    OllamaNumGpu = gpu is null ? 0 : null
                },
                new()
                {
                    Name = "Capacity auto-GPU",
                    ContextTokens = capacityContext,
                    OutputTokens = Math.Min(512, options.MaximumOutputTokens),
                    NumBatch = 256,
                    OllamaNumGpu = gpu is null ? 0 : null
                }
            };
            if (gpu is not null)
            {
                profiles.Add(new AdaptiveOllamaTuningProfile
                {
                    Name = "CPU comparison",
                    ContextTokens = Math.Min(2048, options.MaximumContextTokens),
                    OutputTokens = Math.Min(256, options.MaximumOutputTokens),
                    NumBatch = 128,
                    OllamaNumGpu = 0
                });
            }
            return profiles.Take(options.MaxProfilesPerModel).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating hardware-aware adaptive Ollama tuning profiles failed.");
            throw;
        }
    }

    private async Task<AdaptiveOllamaBenchmarkModelResult> BenchmarkModelAsync(
        HttpClient http,
        OllamaBenchmarkModelInfo model,
        IReadOnlyList<AdaptiveOllamaTuningProfile> profiles,
        IReadOnlyList<AdaptiveOllamaBenchmarkTask> tasks,
        AdaptiveOllamaBenchmarkOptions options,
        CancellationToken cancellationToken)
    {
        var result = new AdaptiveOllamaBenchmarkModelResult
        {
            ModelName = DisplayModelName(model),
            InstalledSizeBytes = model.Size > 0 ? model.Size : null,
            ParameterSize = model.Details?.ParameterSize ?? string.Empty
        };
        try
        {
            foreach (var profile in profiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var profileResult = await BenchmarkProfileAsync(
                    http,
                    result.ModelName,
                    profile,
                    tasks,
                    options.MaxSecondsPerCall,
                    cancellationToken).ConfigureAwait(false);
                result.Profiles.Add(profileResult);

                if (profileResult.Score > result.BestScore)
                {
                    var improvementPercent = result.BestScore <= 0
                        ? double.PositiveInfinity
                        : ((profileResult.Score - result.BestScore) / result.BestScore) * 100d;
                    result.BestScore = profileResult.Score;
                    result.BestProfile = profileResult.ProfileName;
                    if (!double.IsPositiveInfinity(improvementPercent) && improvementPercent < options.ImprovementThresholdPercent)
                    {
                        result.StoppedBecauseImprovementWasBelowThreshold = true;
                        break;
                    }
                }
                else if (result.BestScore > 0)
                {
                    result.StoppedBecauseImprovementWasBelowThreshold = true;
                    break;
                }
            }
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Adaptive benchmark failed for installed model {ModelName}; generated text was omitted.", result.ModelName);
            result.Error = exception.Message;
            return result;
        }
    }

    private async Task<AdaptiveOllamaBenchmarkProfileResult> BenchmarkProfileAsync(
        HttpClient http,
        string modelName,
        AdaptiveOllamaTuningProfile profile,
        IReadOnlyList<AdaptiveOllamaBenchmarkTask> tasks,
        int maxSecondsPerCall,
        CancellationToken cancellationToken)
    {
        var result = new AdaptiveOllamaBenchmarkProfileResult
        {
            ProfileName = profile.Name,
            ContextTokens = profile.ContextTokens,
            OutputTokens = profile.OutputTokens,
            OllamaNumGpu = profile.OllamaNumGpu,
            NumBatch = profile.NumBatch
        };
        try
        {
            foreach (var task in tasks)
            {
                var taskResult = await BenchmarkTaskAsync(
                    http,
                    modelName,
                    profile,
                    task,
                    maxSecondsPerCall,
                    cancellationToken).ConfigureAwait(false);
                result.Tasks.Add(taskResult);
            }

            var successful = result.Tasks.Where(task => task.Succeeded).ToList();
            if (successful.Count == 0)
                return result;
            result.AverageTokensPerSecond = successful.Average(task => task.TokensPerSecond);
            result.AverageQualityScore = successful.Average(task => task.QualityScore);
            result.AverageTotalMilliseconds = successful.Average(task => task.TotalMilliseconds);
            var latencyFactor = 1d / (1d + result.AverageTotalMilliseconds / 30000d);
            result.Score = result.AverageTokensPerSecond * (0.35d + 0.65d * result.AverageQualityScore) * latencyFactor;
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Adaptive benchmark profile {ProfileName} failed for model {ModelName}; generated text was omitted.", profile.Name, modelName);
            return result;
        }
    }

    private async Task<AdaptiveOllamaBenchmarkTaskResult> BenchmarkTaskAsync(
        HttpClient http,
        string modelName,
        AdaptiveOllamaTuningProfile profile,
        AdaptiveOllamaBenchmarkTask task,
        int maxSecondsPerCall,
        CancellationToken cancellationToken)
    {
        var result = new AdaptiveOllamaBenchmarkTaskResult { TaskName = task.Name };
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await GenerateAsync(
                http,
                modelName,
                task.Prompt,
                profile,
                maxSecondsPerCall,
                cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            result.Succeeded = response.Done && !string.IsNullOrWhiteSpace(response.Response);
            result.EvaluatedTokens = response.EvaluationCount;
            result.TotalMilliseconds = response.TotalDurationNanoseconds > 0
                ? Math.Max(1, response.TotalDurationNanoseconds / 1_000_000)
                : Math.Max(1, stopwatch.ElapsedMilliseconds);
            result.TokensPerSecond = response.EvaluationDurationNanoseconds > 0 && response.EvaluationCount > 0
                ? response.EvaluationCount / (response.EvaluationDurationNanoseconds / 1_000_000_000d)
                : response.EvaluationCount / Math.Max(0.001d, result.TotalMilliseconds / 1000d);
            result.QualityScore = ScoreQuality(response.Response, task);
            var compact = response.Response.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
            result.ResponsePreview = compact[..Math.Min(compact.Length, 240)];
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Adaptive benchmark task {TaskName} failed for model {ModelName}; generated text was omitted.", task.Name, modelName);
            result.Error = exception.Message;
            return result;
        }
    }

    private async Task<OllamaBenchmarkGenerateResponse> GenerateAsync(
        HttpClient http,
        string modelName,
        string prompt,
        AdaptiveOllamaTuningProfile profile,
        int maxSecondsPerCall,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(maxSecondsPerCall));
            var options = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["num_ctx"] = profile.ContextTokens,
                ["num_predict"] = profile.OutputTokens,
                ["num_batch"] = profile.NumBatch,
                ["temperature"] = 0,
                ["seed"] = 42
            };
            if (profile.OllamaNumGpu.HasValue)
                options["num_gpu"] = profile.OllamaNumGpu.Value;

            var request = new OllamaBenchmarkGenerateRequest
            {
                Model = modelName,
                Prompt = prompt,
                Stream = false,
                KeepAlive = "5m",
                Options = options
            };
            using var response = await http.PostAsJsonAsync("api/generate", request, jsonOptions, timeout.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OllamaBenchmarkGenerateResponse>(jsonOptions, timeout.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Ollama returned an empty benchmark response.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogWarning(exception, "Ollama benchmark call for model {ModelName} exceeded its bounded timeout; prompt content was omitted.", modelName);
            throw new TimeoutException($"Model {modelName} did not finish within {maxSecondsPerCall} seconds.", exception);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Ollama benchmark generation failed for model {ModelName}; prompt and response content were omitted.", modelName);
            throw;
        }
    }

    private double ScoreQuality(string response, AdaptiveOllamaBenchmarkTask task)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(response))
                return 0d;
            var score = 0.25d;
            if (task.ExpectedTokens.Count > 0)
            {
                var matches = task.ExpectedTokens.Count(token => response.Contains(token, StringComparison.OrdinalIgnoreCase));
                score += 0.55d * matches / task.ExpectedTokens.Count;
            }
            else
            {
                score += Math.Min(0.55d, response.Trim().Length / 400d);
            }

            if (task.ExpectJson && IsJsonResponse(response))
                score += 0.20d;
            else if (!task.ExpectJson)
                score += 0.20d;
            return Math.Clamp(score, 0d, 1d);
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Scoring an Ollama benchmark response failed; generated text was omitted.");
            return 0d;
        }
    }

    private bool IsJsonResponse(string response)
    {
        try
        {
            var trimmed = response.Trim();
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                var firstNewLine = trimmed.IndexOf('\n');
                var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (firstNewLine >= 0 && lastFence > firstNewLine)
                    trimmed = trimmed[(firstNewLine + 1)..lastFence].Trim();
            }
            using var document = JsonDocument.Parse(trimmed);
            return document.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Checking benchmark JSON shape failed; generated text was omitted.");
            return false;
        }
    }

    private async Task<CouncilModelPreset> SaveBenchmarkPresetAsync(
        AdaptiveOllamaBenchmarkReport report,
        IReadOnlyList<OneWireHardwareDescriptor> hardware,
        AdaptiveOllamaBenchmarkOptions options,
        bool userConfirmed,
        CancellationToken cancellationToken)
    {
        try
        {
            var ranked = report.Models
                .Where(model => model.BestScore > 0)
                .OrderByDescending(model => model.BestScore)
                .Take(Math.Min(options.MaxModels, 8))
                .ToList();
            var gpu = hardware.FirstOrDefault(item => item.IsOnline && item.Kind is OneWireHardwareKind.Gpu or OneWireHardwareKind.Accelerator);
            var cpu = hardware.FirstOrDefault(item => item.IsOnline && item.Kind == OneWireHardwareKind.Cpu);
            var lane = gpu ?? cpu;
            var routes = new List<OneWireCouncilModelRoute>();
            foreach (var model in ranked)
            {
                var profile = model.Profiles.First(item => string.Equals(item.ProfileName, model.BestProfile, StringComparison.Ordinal));
                routes.Add(new OneWireCouncilModelRoute
                {
                    ModelName = new ProviderModelIdentity().CreateSelectionKey("Ollama", report.Endpoint, model.ModelName),
                    ProviderKind = ProviderModelKinds.Ollama,
                    ProviderName = "Ollama",
                    ProviderEndpoint = report.Endpoint,
                    ProviderModelName = model.ModelName,
                    HardwareKind = lane?.Kind ?? OneWireHardwareKind.Cpu,
                    HardwareIndex = lane?.Index ?? 0,
                    HardwareName = lane?.Name ?? "CPU",
                    MinOutputTokens = Math.Min(256, profile.OutputTokens),
                    MaxOutputTokens = profile.OutputTokens,
                    MinContextTokens = Math.Min(2048, profile.ContextTokens),
                    MaxContextTokens = profile.ContextTokens,
                    OllamaNumGpu = lane?.Kind == OneWireHardwareKind.Cpu ? 0 : profile.OllamaNumGpu,
                    MaxConcurrentModelsOnLane = 1,
                    IsEnabled = true
                });
            }

            var presetName = $"{options.PresetName} {DateTimeOffset.Now:yyyy-MM-dd HHmm}";
            presetName = presetName[..Math.Min(presetName.Length, 160)];
            var preset = new CouncilModelPreset
            {
                Name = presetName,
                Description = $"User-approved adaptive Ollama benchmark {report.RunId}. New preset only; existing presets were not overwritten.",
                ModelNamesJson = JsonSerializer.Serialize(routes.Select(route => route.ModelName).ToList(), jsonOptions),
                ModelRoutesJson = JsonSerializer.Serialize(routes, jsonOptions),
                AllowParallelHardwareRoads = false,
                MaxOutputTokens = routes.Max(route => route.MaxOutputTokens),
                MaxContextTokens = routes.Max(route => route.MaxContextTokens),
                MaxParallelModels = 1,
                OllamaNumGpu = routes[0].OllamaNumGpu,
                IncludeMemory = false,
                GenerateArtifacts = false,
                CreateProjectPerRun = false,
                IsDefault = options.MakeDefault,
                IsUserApproved = true
            };
            return await modelPresets.SavePresetAsync(preset, userConfirmed, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving the adaptive Ollama benchmark preset failed; existing presets were left unchanged.");
            throw;
        }
    }

    private string BuildHardwareSummary(IReadOnlyList<OneWireHardwareDescriptor> hardware)
    {
        try
        {
            return string.Join(
                "; ",
                hardware.Where(item => item.IsOnline).Select(item =>
                    item.DedicatedMemoryBytes is > 0
                        ? $"{item.Kind}:{item.Name} ({item.DedicatedMemoryBytes.Value / 1024d / 1024d / 1024d:F1} GiB)"
                        : $"{item.Kind}:{item.Name}"));
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Building the adaptive benchmark hardware summary failed.");
            return "Hardware inventory unavailable";
        }
    }

    private string DisplayModelName(OllamaBenchmarkModelInfo model)
    {
        try
        {
            return string.IsNullOrWhiteSpace(model.Model) ? model.Name.Trim() : model.Model.Trim();
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Resolving an installed Ollama model name failed.");
            return string.Empty;
        }
    }
}
