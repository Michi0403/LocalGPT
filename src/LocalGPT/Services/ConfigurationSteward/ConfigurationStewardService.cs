using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LocalGPT.WireProtocol;
using Microsoft.Extensions.AI;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.ConfigurationSteward;

/// <summary>
/// Configuration steward. Runs deterministic health checks and converts a finding into a Human Collaboration approval
/// request whose selectable answers are the finding's options. A repair is applied only after the local human approved
/// one option for the exact fingerprint of the observed data; if the data changed meanwhile, a new decision is asked.
/// </summary>
/// <param name="checks">Registered read-only configuration health checks.</param>
/// <param name="applier">Compare-and-set applier for approved options.</param>
/// <param name="collaboration">Common LocalGPT approval and collaboration store.</param>
/// <param name="vocabulary">Database-owned request kinds and statuses.</param>
/// <param name="runtimePolicy">Database-backed steward thresholds.</param>
/// <param name="hardwareInventory">Read-only local hardware inventory used to assess available system and GPU resources.</param>
/// <param name="hostHardware">Provider-endpoint hardware profiles, preserving user-confirmed remote and local host facts.</param>
/// <param name="performancePresets">Existing measured performance profiles used for run-local resource recommendations.</param>
/// <param name="providerModels">Selected provider-qualified model runtime for optional bounded steward AI advice.</param>
/// <param name="ambientContext">Current Council run and actor for the approval request.</param>
/// <param name="logger">Logger for steward diagnostics.</param>
public sealed class ConfigurationStewardService(
    IEnumerable<IConfigurationHealthCheck> checks,
    IConfigurationRepairApplier applier,
    IHumanCollaborationService collaboration,
    ILocalGptVocabularyService vocabulary,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    IHardwareInventoryService hardwareInventory,
    IConfiguredAiHostHardwareService hostHardware,
    IHardwarePerformancePresetService performancePresets,
    IProviderModelRuntimeService providerModels,
    IAmbientLocalGptContext ambientContext,
    ILogger<ConfigurationStewardService> logger) : IConfigurationStewardService
{
    /// <summary>Runtime value storing <see cref="ConfigurationStewardPolicy"/>.</summary>
    private const string PolicyKey = "ConfigurationStewardPolicyJson";
    /// <summary>Approval operation key of configuration repairs.</summary>
    private const string OperationKey = "localgpt.configuration.repair";

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigurationFinding>> DiagnoseAsync(string? category = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var policy = ReadPolicy();
            var findings = new List<ConfigurationFinding>();
            foreach (var check in checks.Where(item => string.IsNullOrWhiteSpace(category) ||
                string.Equals(item.Category, category.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    findings.AddRange(await check.InspectAsync(policy, cancellationToken).ConfigureAwait(false));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (Exception exception)
                {
                    // One failing check must not hide the others; its failure is itself a finding.
                    logger.LogWarning(exception, "Configuration check {CheckKey} failed; other checks continue.", check.CheckKey);
                    findings.Add(new ConfigurationFinding
                    {
                        Key = "check-failed:" + check.CheckKey,
                        CheckKey = check.CheckKey,
                        Category = check.Category,
                        Severity = "Error",
                        Title = $"Configuration check {check.CheckKey} could not run",
                        Summary = exception.Message,
                        VerificationNote = "The check failed, so its area was not inspected. See the LocalGPT log for details."
                    });
                }
            }
            foreach (var finding in findings)
                finding.Fingerprint = ComputeFingerprint(finding);
            var ordered = findings
                .OrderBy(item => item.Severity switch { "Error" => 0, "Warning" => 1, _ => 2 })
                .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Max(1, policy.MaximumFindings))
                .ToList();
            logger.LogInformation("Configuration steward diagnosis ({Category}) produced {Count} finding(s): {Keys}.",
                string.IsNullOrWhiteSpace(category) ? "all" : category, ordered.Count, ordered.Count == 0 ? "none" : string.Join(", ", ordered.Select(item => item.Key)));
            return ordered;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Configuration steward diagnosis was canceled.");
            else
                logger.LogError(exception, "Configuration steward diagnosis failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ConfigurationRepairOutcome> RequestRepairAsync(string findingKey, string requestedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(findingKey);
            // The finding is recomputed from live data; nothing the caller (or a model) claims about values is used.
            var finding = (await DiagnoseAsync(null, cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(item => string.Equals(item.Key, findingKey.Trim(), StringComparison.OrdinalIgnoreCase));
            if (finding is null)
                return new ConfigurationRepairOutcome { FindingKey = findingKey, Status = ConfigurationRepairOutcome.FindingNotPresent, Message = "The current data no longer shows this finding; nothing to repair." };
            if (!finding.Options.Any(option => option.Changes.Count > 0))
                return new ConfigurationRepairOutcome { FindingKey = finding.Key, Status = ConfigurationRepairOutcome.NoRepairAvailable, Message = "This finding is report-only; the steward offers no automatic repair for it." };

            var policy = ReadPolicy();
            var ambient = ambientContext.Current;
            var recommended = finding.Options.FirstOrDefault(option => option.Key == finding.RecommendedOptionKey);
            var gate = await collaboration.AuthorizeOrEnqueueAsync(new HumanApprovalRequestSpec(
                CorrelationId: "config-repair:" + finding.Fingerprint,
                OperationKey: OperationKey,
                Title: "Configuration steward: " + finding.Title,
                Description: BuildApprovalDescription(finding),
                RiskLevel: string.IsNullOrWhiteSpace(policy.ApprovalRiskLevel) ? "High" : policy.ApprovalRiskLevel,
                Source: nameof(ConfigurationStewardService),
                RequestedBy: string.IsNullOrWhiteSpace(requestedBy) ? ambient.ActorDisplayName : requestedBy,
                RequestedRole: "Configuration steward",
                CouncilRunId: ambient.CouncilRunId,
                IsSensitive: false,
                RequestKind: vocabulary.Get().HumanRequestApproval,
                SuggestedResponsesText: string.Join('\n', finding.Options.Select(option => option.Label)),
                ResponsePrompt: "Choose one option, then approve. Declining keeps the current configuration.",
                PrefillText: recommended?.Label ?? string.Empty,
                AllowFreeText: false,
                ParameterFingerprint: finding.Fingerprint),
                directHumanConfirmation: false,
                cancellationToken).ConfigureAwait(false);

            var outcome = new ConfigurationRepairOutcome { FindingKey = finding.Key, ApprovalRequestId = gate.RequestId, CorrelationId = gate.CorrelationId };
            if (gate.IsDeclined)
            {
                outcome.Status = ConfigurationRepairOutcome.DeclinedByUser;
                outcome.Message = string.IsNullOrWhiteSpace(gate.DecisionReason) ? "The local human declined the repair; nothing changed." : gate.DecisionReason;
                logger.LogInformation("Configuration repair {FindingKey} was declined by the local human.", finding.Key);
                return outcome;
            }
            if (!gate.IsAuthorized)
            {
                outcome.Status = gate.RequestId is null ? ConfigurationRepairOutcome.RequestRejected : ConfigurationRepairOutcome.AwaitingUserDecision;
                outcome.Message = gate.RequestId is null ? gate.Message : "The repair options wait in Approvals & team. Nothing changes until one option is approved.";
                logger.LogInformation("Configuration repair {FindingKey} waits for the local human decision (request {RequestId}).", finding.Key, gate.RequestId);
                return outcome;
            }

            var selected = finding.Options.FirstOrDefault(option =>
                string.Equals(option.Label.Trim(), gate.UserResponse?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(option.Key, gate.UserResponse?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (selected is null)
            {
                outcome.Status = ConfigurationRepairOutcome.NoOptionSelected;
                outcome.Message = "The approval did not name one of the offered options; nothing changed. Request the repair again and choose an option.";
                logger.LogWarning("Configuration repair {FindingKey} was approved without a recognizable option; nothing changed.", finding.Key);
                return outcome;
            }
            if (selected.Changes.Count == 0)
            {
                outcome.Status = ConfigurationRepairOutcome.KeptByUser;
                outcome.SelectedOptionKey = selected.Key;
                outcome.Message = $"Kept as decided: \"{selected.Label}\".";
                logger.LogInformation("Configuration repair {FindingKey}: the local human chose to keep the current state.", finding.Key);
                return outcome;
            }

            var applied = await applier.ApplyAsync(finding, selected, cancellationToken).ConfigureAwait(false);
            applied.ApprovalRequestId = gate.RequestId;
            applied.CorrelationId = gate.CorrelationId;
            return applied;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Configuration repair {FindingKey} was canceled.", findingKey);
            else
                logger.LogError(exception, "Configuration repair {FindingKey} failed.", findingKey);
            throw;
        }
    }

    /// <summary>Builds the approval card description: effect, evidence, limits of the evidence and every option.</summary>
    /// <param name="finding">Finding to describe.</param>
    /// <returns>Description bounded to the approval store limit.</returns>
    private string BuildApprovalDescription(ConfigurationFinding finding)
    {
        try
        {
            var builder = new StringBuilder();
            builder.AppendLine(finding.Summary);
            builder.AppendLine();
            builder.AppendLine("Evidence:");
            foreach (var evidence in finding.Evidence)
                builder.AppendLine($"- {evidence.Source}: {evidence.ObservedValue} (compared with {evidence.Reference}){(string.IsNullOrWhiteSpace(evidence.Note) ? string.Empty : " " + evidence.Note)}");
            builder.AppendLine();
            builder.AppendLine("What the data cannot prove: " + finding.VerificationNote);
            builder.AppendLine();
            builder.AppendLine("Options:");
            foreach (var option in finding.Options)
                builder.AppendLine($"- {option.Label}: {option.Description}");
            var text = builder.ToString();
            return text.Length <= 2000 ? text : text[..1997] + "...";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not build the approval description of finding {FindingKey}.", finding.Key);
            throw;
        }
    }

    /// <summary>Fingerprints the finding key, observed evidence and exact option changes.</summary>
    /// <param name="finding">Finding to fingerprint.</param>
    /// <returns>Lower-case SHA-256 hex.</returns>
    private string ComputeFingerprint(ConfigurationFinding finding)
    {
        try
        {
            var source = JsonSerializer.Serialize(new
            {
                finding.Key,
                Evidence = finding.Evidence.Select(item => new { item.Source, item.ObservedValue }),
                Options = finding.Options.Select(option => new
                {
                    option.Key,
                    option.Label,
                    Changes = option.Changes.Select(change => new { change.TargetKind, change.TargetKey, change.ExpectedCurrentValue, change.NewValue })
                })
            });
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not fingerprint finding {FindingKey}.", finding.Key);
            throw;
        }
    }

    /// <summary>Reads the steward policy, falling back to the documented defaults with a warning.</summary>
    /// <returns>The current policy.</returns>
    /// <summary>
    /// Recommend an effective context for one GoalExecution attempt. Only measured provider benchmarks
    /// may raise automatic context; weak RAM/VRAM discovery never fabricates a safe token capacity.
    /// Small and unknown hosts are served through bounded AI subgoals instead of a hardcoded token wall.
    /// </summary>
    public async Task<GoalResourceAdvice> AdviseGoalResourcesAsync(
        GoalExecutionRequest request, string? contextFailure = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.CouncilRequest);
        var originalContext = request.CouncilRequest.MaxContextTokens;
        var advice = new GoalResourceAdvice
        {
            RecommendedContextTokens = originalContext,
            PreferSubagents = true,
            Reason = "No verified larger-context benchmark; use the existing goal AI and bounded subagent work packets."
        };
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            advice.IsManual = string.Equals(runtimePolicy.GetString("GoalExecutionResourceMode"),
                "Manual", StringComparison.OrdinalIgnoreCase);
            if (advice.IsManual)
            {
                advice.PreferSubagents = false;
                advice.Reason = "Manual run resources are authoritative; GoalExecution will not change their context settings.";
                return advice;
            }
            if (request.CouncilRequest.ModelRoutes.Count > 0)
            {
                advice.Reason = "This run has explicit hardware routes; preserve their configured limits and split failed work instead.";
                return advice;
            }

            var selected = request.CouncilRequest.ModelSelections
                .FirstOrDefault(model => model.IsLocal && !string.IsNullOrWhiteSpace(model.Endpoint));
            var endpoint = selected?.Endpoint ?? string.Empty;
            var localEndpoint = Uri.TryCreate(endpoint, UriKind.Absolute, out var address) &&
                (address.IsLoopback || string.Equals(address.Host, "localhost", StringComparison.OrdinalIgnoreCase));
            long? systemMemory = null;
            long? dedicatedGpuMemory = null;
            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                var host = await hostHardware.GetForEndpointAsync(endpoint, cancellationToken).ConfigureAwait(false);
                systemMemory = host?.SystemMemoryBytes;
                dedicatedGpuMemory = host?.Gpus.Where(gpu => gpu.DedicatedMemoryBytes is > 0)
                    .Max(gpu => gpu.DedicatedMemoryBytes) ?? null;
            }
            if (localEndpoint)
            {
                systemMemory ??= await hardwareInventory.GetSystemMemoryBytesAsync(cancellationToken).ConfigureAwait(false);
                if (dedicatedGpuMemory is null)
                {
                    var devices = await hardwareInventory.GetHardwareAsync(cancellationToken).ConfigureAwait(false);
                    dedicatedGpuMemory = devices.Where(device => device.DedicatedMemoryBytes is > 0)
                        .Select(device => device.DedicatedMemoryBytes).Max();
                }
            }

            // A stored benchmark is a measured per-provider route. No performance profile is
            // applied or persisted here. Manual presets and another host's measurements do not
            // grant permission to raise the current model's context.
            var benchmarks = await performancePresets.GetPresetsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var measuredContext = 0;
            foreach (var preset in benchmarks.Where(item => item.SourceRunId.HasValue &&
                         item.SourceKind.Contains("Benchmark", StringComparison.OrdinalIgnoreCase)))
            {
                List<OneWireCouncilModelRoute>? routes;
                try
                {
                    routes = JsonSerializer.Deserialize<List<OneWireCouncilModelRoute>>(preset.ModelRoutesJson);
                }
                catch (JsonException exception)
                {
                    logger.LogWarning(exception, "Ignoring invalid benchmark route data in preset {PresetId}.", preset.Id);
                    continue;
                }
                foreach (var route in routes ?? [])
                {
                    var sameModel = selected is not null &&
                        (string.Equals(route.ProviderModelName, selected.ModelName, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(route.ModelName, selected.SelectionKey, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(route.ModelName, selected.ModelName, StringComparison.OrdinalIgnoreCase));
                    if (!sameModel || !string.Equals(route.ProviderEndpoint, selected!.Endpoint, StringComparison.OrdinalIgnoreCase))
                        continue;
                    measuredContext = Math.Max(measuredContext, route.MaxContextTokens);
                }
            }
            if (measuredContext > originalContext)
            {
                advice.RecommendedContextTokens = measuredContext;
                advice.PreferSubagents = false;
                advice.Reason = $"Verified benchmark for {selected?.ModelName} on its exact provider host supports " +
                    $"a context of {measuredContext:N0} tokens; saved settings are untouched.";
            }
            else if (!string.IsNullOrWhiteSpace(contextFailure) &&
                     systemMemory is > 0 && dedicatedGpuMemory is > 0)
            {
                var minimumHostGiB = Math.Max(1, runtimePolicy.GetInt("GoalAutoResourceProbeMinimumHostGiB"));
                var minimumGpuGiB = Math.Max(1, runtimePolicy.GetInt("GoalAutoResourceProbeMinimumGpuGiB"));
                var unit = 1024L * 1024 * 1024;
                long required = 0;
                long reportedMax = 0;
                foreach (var field in contextFailure.Split(';', StringSplitOptions.TrimEntries))
                {
                    if (field.StartsWith("required=", StringComparison.OrdinalIgnoreCase))
                        long.TryParse(field[9..], System.Globalization.NumberStyles.Integer,
                            System.Globalization.CultureInfo.InvariantCulture, out required);
                    else if (field.StartsWith("max=", StringComparison.OrdinalIgnoreCase))
                        long.TryParse(field[4..], System.Globalization.NumberStyles.Integer,
                            System.Globalization.CultureInfo.InvariantCulture, out reportedMax);
                }
                // One bounded per-run capacity probe on a verified high-resource host.
                // A model's advertised maximum is necessary, but not evidence of an
                // affordable KV cache; if this probe fails, GoalExecution decomposes.
                if (systemMemory.Value >= minimumHostGiB * unit &&
                    dedicatedGpuMemory.Value >= minimumGpuGiB * unit &&
                    required > originalContext && reportedMax >= required)
                {
                    var policy = runtimePolicy.GetJson<OllamaRequestBudgetPolicy>("OllamaRequestBudgetPolicyJson");
                    var step = Math.Max(256, policy.ContextGrowthStepTokens);
                    var target = ((required + step - 1L) / step) * step;
                    if (target <= reportedMax && target <= int.MaxValue)
                    {
                        advice.RecommendedContextTokens = (int)target;
                        advice.PreferSubagents = false;
                        advice.Reason = $"Verified host RAM/GPU and model-advertised context permit one " +
                            $"run-local capacity probe at {target:N0} tokens; " +
                            "this is not a benchmark guarantee, and failure will delegate to smaller subagents.";
                    }
                }
            }
            else
            {
                var memoryDescription = systemMemory is > 0
                    ? $"host RAM {systemMemory.Value / 1024d / 1024d / 1024d:F1} GiB"
                    : "host RAM unknown";
                var gpuDescription = dedicatedGpuMemory is > 0
                    ? $", GPU memory {dedicatedGpuMemory.Value / 1024d / 1024d / 1024d:F1} GiB"
                    : ", GPU allocation unknown";
                advice.Reason = $"{memoryDescription}{gpuDescription}; no verified larger-context route. " +
                    "Prefer measured settings and small, goal-directed subagent packets.";
            }
            if (!string.IsNullOrWhiteSpace(contextFailure) && advice.PreferSubagents &&
                bool.TryParse(runtimePolicy.GetString("GoalStewardAiEnabled"), out var stewardAiEnabled) && stewardAiEnabled)
            {
                // The steward's AI is a small planning subagent, not a model selector
                // or a second executor. It cannot override observed hardware or
                // the goal's existing approval/receipt boundaries.
                var modelAdvice = await AskStewardAiAsync(request, advice.Reason,
                    contextFailure, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(modelAdvice))
                {
                    advice.AiConsulted = true;
                    advice.Reason += " Steward AI subagent: " + modelAdvice;
                }
            }
            logger.LogInformation("Configuration Steward advised goal {GoalId}: context {Context}, decompose {Decompose}, AI consulted {AiConsulted}, {Reason}",
                request.CouncilRequest.RunId, advice.RecommendedContextTokens,
                advice.PreferSubagents, advice.AiConsulted, advice.Reason);
            return advice;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "Configuration Steward could not inspect hardware for goal {GoalId}; saved run settings remain unchanged.",
                request.CouncilRequest.RunId);
            advice.Reason = "Hardware/benchmark inspection unavailable; retain the current run and use bounded goal subagents.";
            return advice;
        }
    }

    /// <summary>
    /// Optional bounded inference by the existing selected model for one recovery
    /// advisory. The deterministic hardware/approval rules remain authoritative.
    /// </summary>
    private async Task<string?> AskStewardAiAsync(GoalExecutionRequest request,
        string observedHardware, string contextFailure, CancellationToken cancellationToken)
    {
        try
        {
            var selected = request.CouncilRequest.ModelSelections.FirstOrDefault(model =>
                model.IsReachable && (model.IsLocal || model.IsConfigured));
            if (selected is null)
                return null;
            var runtime = runtimePolicy.GetJson<ProviderModelRuntimeParameters>("ProviderModelRuntimeParametersJson");
            var timeoutSeconds = Math.Clamp(runtimePolicy.GetInt("GoalStewardAiTimeoutSeconds"), 5, 120);
            using var client = providerModels.CreateChatClient(selected,
                runtime.DefaultSessionKeepAlive,
                request.CouncilRequest.MaxContextTokens,
                TimeSpan.FromSeconds(timeoutSeconds),
                request.CouncilRequest.OllamaNumGpu,
                enableAutomaticTools: false, throwOnFailure: false);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            var message = JsonSerializer.Serialize(new
            {
                goal = request.GoalText.Length > 1200 ? request.GoalText[..1200] : request.GoalText,
                model = selected.SelectionKey,
                configuredContext = request.CouncilRequest.MaxContextTokens,
                observedHardware,
                contextFailure = contextFailure.Length > 800 ? contextFailure[..800] : contextFailure,
                invariant = "Propose a smaller model task; never write, build, execute or approve from this helper."
            });
            var answer = await client.GetResponseAsync(
                [new ChatMessage(ChatRole.System, runtimePolicy.GetString("GoalStewardAiSystemPrompt")),
                 new ChatMessage(ChatRole.User, message)],
                new ChatOptions { MaxOutputTokens = 256, Temperature = 0f },
                timeout.Token).ConfigureAwait(false);
            var text = answer.Text?.Trim() ?? string.Empty;
            if (!text.StartsWith('{') || !text.EndsWith('}'))
                return null;
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            if (!root.TryGetProperty("strategy", out var strategy) ||
                !string.Equals(strategy.GetString(), "decompose", StringComparison.OrdinalIgnoreCase) ||
                !root.TryGetProperty("reason", out var reason) || reason.ValueKind != JsonValueKind.String)
                return null;
            var advice = reason.GetString()?.Trim();
            return string.IsNullOrWhiteSpace(advice) ? null
                : advice[..Math.Min(280, advice.Length)];
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Steward AI advisory timed out; deterministic goal recovery continues.");
            return null;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "Steward AI advisory was unavailable; deterministic hardware advice and GoalExecution AI remain active.");
            return null;
        }
    }

    private ConfigurationStewardPolicy ReadPolicy()
    {
        try
        {
            return runtimePolicy.GetJson<ConfigurationStewardPolicy>(PolicyKey);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Runtime value {Key} is unavailable; the configuration steward uses its built-in thresholds.", PolicyKey);
            return new ConfigurationStewardPolicy();
        }
    }
}
