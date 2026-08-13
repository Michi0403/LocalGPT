using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Exposes the transport-neutral provider benchmark service as an approval-gated DXFunction so Council workflows can benchmark exact provider/endpoint/model identities across configured hosts instead of falling back to a loopback-only implementation.
/// </summary>
/// <param name="providerModels">Provider runtime catalog used to resolve exact selection keys into benchmark references.</param>
/// <param name="benchmarks">Provider-qualified benchmark service shared with the Chat configuration UI.</param>
/// <param name="logger">Logger used for bounded diagnostics; prompts and generated provider output are intentionally omitted.</param>
public sealed class RunProviderModelBenchmarkFunction(
    IProviderModelRuntimeService providerModels,
    IProviderModelBenchmarkService benchmarks,
    ILogger<RunProviderModelBenchmarkFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Stores the web-compatible JSON options used to bind the function request without introducing controller or UI dependencies into the benchmark service.</summary>
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Gets the descriptor value that forms part of the run provider model benchmark function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The registry metadata used for discovery, policy and deferred human approval.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.models.benchmark.provider",
        "POST",
        "/api/dxai/functions/localgpt.models.benchmark.provider/invoke",
        "Runs the same provider-qualified Benchmark Council used by Chat configuration against exact selected provider/endpoint/model identities. It supports all currently discovered models, evenly-spaced token stepping, a user-selected reviewer pool, and provider endpoints on configured local or LAN AI hosts.",
        "Provide modelSelectionKeys, or explicitly set allDiscoveredModels=true. Optional reviewerSelectionKeys select exact reviewers; otherwise LocalGPT ranks the available reviewer pool and prefers capable reviewers such as gpt-oss:20b. Configure profileSteps/profileMode, token bounds, task count, timeout, reviewer count and optional early stopping. Every successful approved run creates or updates a selectable hardware-spooler performance profile. Use performancePresetName to choose its user-visible base name.",
        "Requires fresh human approval because it consumes AI compute and normally stores the measured performance profile. It never downloads models, changes provider-global settings, or changes Council membership.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false,
        Source: "ProviderModelBenchmarkDxAiFunction",
        ParameterSchemaJson: """
            {
              "type": "object",
              "properties": {
                "modelSelectionKeys": { "type": "array", "items": { "type": "string" } },
                "allDiscoveredModels": { "type": "boolean" },
                "reviewerSelectionKeys": { "type": "array", "items": { "type": "string" } },
                "profileSteps": { "type": "integer", "minimum": 1 },
                "profileMode": { "type": "string", "enum": ["Adaptive", "EvenlySpaced"] },
                "minimumContextTokens": { "type": "integer", "minimum": 1 },
                "minimumOutputTokens": { "type": "integer", "minimum": 1 },
                "maximumContextTokens": { "type": "integer", "minimum": 1 },
                "maximumOutputTokens": { "type": "integer", "minimum": 1 },
                "tasksPerProfile": { "type": "integer", "minimum": 1 },
                "timeoutSeconds": { "type": "integer", "minimum": 1 },
                "reviewersPerRecommendation": { "type": "integer", "minimum": 0 },
                "includeCpuSafeControl": { "type": "boolean" },
                "stopWhenImprovementStalls": { "type": "boolean" },
                "improvementThresholdPercent": { "type": "number", "minimum": 0 },
                "performancePresetName": { "type": "string", "maxLength": 160 }
              },
              "additionalProperties": false
            }
            """,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: true);

    /// <summary>Runs an approval-gated provider benchmark by resolving the caller's exact provider identities and delegating all measurement behavior to <see cref="IProviderModelBenchmarkService"/>.</summary>
    /// <param name="request">DXFunction invocation containing the benchmark options and current human-approval state.</param>
    /// <param name="cancellationToken">Cancels discovery or the running benchmark without applying recommendations.</param>
    /// <returns>A structured function result containing the complete provider benchmark report or a bounded validation error.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.UserConfirmed)
            {
                return new DxAiFunctionInvocationResult
                {
                    Succeeded = false,
                    Status = "HumanConfirmationRequired",
                    Error = "Fresh human confirmation is required before provider models are benchmarked."
                };
            }

            var options = BindRequest(request.Parameters);
            var candidates = await providerModels.GetCandidatesAsync(cancellationToken).ConfigureAwait(false);
            var targets = ResolveTargets(candidates, options, out var missingTargets);
            if (missingTargets.Count > 0)
            {
                return new DxAiFunctionInvocationResult
                {
                    Succeeded = false,
                    Status = "InvalidParameters",
                    Error = $"The benchmark target selection contains unavailable provider-qualified identity/identities: {string.Join(", ", missingTargets)}. Refresh provider discovery and keep endpoint identity exact."
                };
            }

            if (targets.Count == 0)
            {
                return new DxAiFunctionInvocationResult
                {
                    Succeeded = false,
                    Status = "InvalidParameters",
                    Error = "Select at least one provider-qualified model, or explicitly set allDiscoveredModels=true."
                };
            }

            var reviewers = ResolveReviewers(candidates, options, out var missingReviewers);
            if (missingReviewers.Count > 0)
            {
                return new DxAiFunctionInvocationResult
                {
                    Succeeded = false,
                    Status = "InvalidParameters",
                    Error = $"The benchmark reviewer selection contains unavailable provider-qualified identity/identities: {string.Join(", ", missingReviewers)}."
                };
            }

            var report = await benchmarks.RunAsync(new ProviderModelBenchmarkRequest
            {
                RunId = Guid.NewGuid(),
                Targets = targets,
                CouncilReviewers = reviewers,
                MaxProfilesPerModel = Math.Max(1, options.ProfileSteps),
                ProfileMode = options.ProfileMode,
                MinimumContextTokens = options.MinimumContextTokens,
                MinimumOutputTokens = options.MinimumOutputTokens,
                MaximumContextTokens = options.MaximumContextTokens,
                MaximumOutputTokens = options.MaximumOutputTokens,
                MaxTasks = options.TasksPerProfile,
                MaxSecondsPerCall = options.TimeoutSeconds,
                MaxCouncilReviewers = Math.Max(0, options.ReviewersPerRecommendation),
                IncludeCouncilReview = options.ReviewersPerRecommendation > 0,
                IncludeCpuSafeControl = options.IncludeCpuSafeControl,
                StopWhenImprovementStalls = options.StopWhenImprovementStalls,
                ImprovementThresholdPercent = options.ImprovementThresholdPercent
            }, cancellationToken).ConfigureAwait(false);

            var successfulTargets = report.Targets.Count(target => string.IsNullOrWhiteSpace(target.Error));
            if (successfulTargets > 0)
            {
                var presetName = string.IsNullOrWhiteSpace(options.PerformancePresetName)
                    ? $"Benchmark performance · {DateTimeOffset.Now:yyyy-MM-dd HHmm}"
                    : options.PerformancePresetName;
                await benchmarks.SavePerformancePresetAsync(
                    report, presetName, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            }
            return new DxAiFunctionInvocationResult
            {
                Succeeded = successfulTargets > 0,
                Status = successfulTargets > 0 ? "Completed" : "NoSuccessfulModel",
                Value = report,
                Error = successfulTargets > 0
                    ? null
                    : "No selected provider-qualified model completed the benchmark. Review the returned report and LocalGPT logs."
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Provider-qualified benchmark DXFunction was cancelled; no benchmark performance profile was stored or applied.");
            throw;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Provider benchmark DXFunction parameters were invalid JSON; parameter values were omitted.");
            return new DxAiFunctionInvocationResult
            {
                Succeeded = false,
                Status = "InvalidParameters",
                Error = "Provider benchmark parameters are not valid JSON."
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Provider benchmark DXFunction failed; model prompts and generated text were omitted.");
            return new DxAiFunctionInvocationResult
            {
                Succeeded = false,
                Status = "Failed",
                Error = "The provider-qualified benchmark failed. Review LocalGPT logs for technical details."
            };
        }
    }

    /// <summary>Binds a DXFunction JSON payload to the maintained provider benchmark request contract while preserving defaults for omitted optional settings.</summary>
    /// <param name="parameters">JSON object supplied by the caller.</param>
    /// <returns>The bound provider benchmark function request.</returns>
    private ProviderModelBenchmarkFunctionRequest BindRequest(JsonElement parameters)
    {
        try
        {
            if (parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                return new ProviderModelBenchmarkFunctionRequest();
            if (parameters.ValueKind != JsonValueKind.Object)
                throw new JsonException("Provider benchmark parameters must be a JSON object.");

            return JsonSerializer.Deserialize<ProviderModelBenchmarkFunctionRequest>(parameters.GetRawText(), jsonOptions)
                ?? new ProviderModelBenchmarkFunctionRequest();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Binding provider benchmark DXFunction parameters failed; parameter content was omitted.");
            throw;
        }
    }

    /// <summary>Resolves the target pool without same-name substitution, preserving the provider/endpoint/model identity selected by the caller.</summary>
    /// <param name="candidates">Current provider model catalog.</param>
    /// <param name="options">Bound function request containing explicit keys or the all-discovered opt-in.</param>
    /// <param name="missing">Receives explicit selection keys that are not present in current provider discovery.</param>
    /// <returns>The provider model references to benchmark.</returns>
    private List<ProviderModelReference> ResolveTargets(
        IReadOnlyList<MultiModelCouncilModelCandidate> candidates,
        ProviderModelBenchmarkFunctionRequest options,
        out List<string> missing)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(candidates);
            ArgumentNullException.ThrowIfNull(options);
            var explicitKeys = NormalizeKeys(options.ModelSelectionKeys);
            missing = explicitKeys
                .Where(key => !candidates.Any(candidate => candidate.SelectionKey.Equals(key, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (missing.Count > 0)
                return [];

            IEnumerable<MultiModelCouncilModelCandidate> selected = explicitKeys.Count > 0
                ? candidates.Where(candidate => explicitKeys.Contains(candidate.SelectionKey, StringComparer.OrdinalIgnoreCase))
                : options.AllDiscoveredModels
                    ? candidates.Where(candidate => candidate.IsInstalled && candidate.SupportsBenchmark)
                    : [];
            return selected
                .Where(candidate => candidate.SupportsBenchmark)
                .GroupBy(candidate => candidate.SelectionKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First().ToReference())
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving provider benchmark target identities failed; exact selection values were omitted.");
            throw;
        }
    }

    /// <summary>Resolves an exact reviewer pool, or returns every available benchmark-capable candidate so the benchmark service can apply its maintained quality-first default ranking.</summary>
    /// <param name="candidates">Current provider model catalog.</param>
    /// <param name="options">Bound function request containing optional reviewer keys.</param>
    /// <param name="missing">Receives explicit reviewer keys that are not present in current provider discovery.</param>
    /// <returns>The reviewer references from which the benchmark service selects the configured reviewer count.</returns>
    private List<ProviderModelReference> ResolveReviewers(
        IReadOnlyList<MultiModelCouncilModelCandidate> candidates,
        ProviderModelBenchmarkFunctionRequest options,
        out List<string> missing)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(candidates);
            ArgumentNullException.ThrowIfNull(options);
            if (options.ReviewersPerRecommendation <= 0)
            {
                missing = [];
                return [];
            }

            var explicitKeys = NormalizeKeys(options.ReviewerSelectionKeys);
            missing = explicitKeys
                .Where(key => !candidates.Any(candidate => candidate.SelectionKey.Equals(key, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (missing.Count > 0)
                return [];

            IEnumerable<MultiModelCouncilModelCandidate> selected = explicitKeys.Count > 0
                ? candidates.Where(candidate => explicitKeys.Contains(candidate.SelectionKey, StringComparer.OrdinalIgnoreCase))
                : candidates.Where(candidate => candidate.IsInstalled && candidate.SupportsBenchmark);
            return selected
                .Where(candidate => candidate.SupportsBenchmark)
                .GroupBy(candidate => candidate.SelectionKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First().ToReference())
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving provider benchmark reviewer identities failed; exact selection values were omitted.");
            throw;
        }
    }

    /// <summary>Normalizes provider selection keys while preserving all distinct identities and without imposing a product-specific collection ceiling.</summary>
    /// <param name="values">Potentially null, blank or duplicated selection keys.</param>
    /// <returns>A trimmed case-insensitive distinct key list.</returns>
    private List<string> NormalizeKeys(IEnumerable<string>? values)
    {
        try
        {
            return (values ?? [])
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing provider benchmark selection keys failed; selection values were omitted.");
            throw;
        }
    }
}
