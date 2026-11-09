using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
/// <param name="ambientContext">Current Council run and actor for the approval request.</param>
/// <param name="logger">Logger for steward diagnostics.</param>
public sealed class ConfigurationStewardService(
    IEnumerable<IConfigurationHealthCheck> checks,
    IConfigurationRepairApplier applier,
    IHumanCollaborationService collaboration,
    ILocalGptVocabularyService vocabulary,
    ILocalGptRuntimePolicyDataService runtimePolicy,
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
