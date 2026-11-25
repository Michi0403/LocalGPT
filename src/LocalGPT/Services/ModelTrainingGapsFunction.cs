using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Returns measured benchmark and recovery observations for training review.</summary>
public sealed class ModelTrainingGapsFunction(IProviderModelBenchmarkService benchmarks, ICouncilSpoolerService spooler,
    IDxAiFunctionJsonService json, ILogger<ModelTrainingGapsFunction> logger) : IDxAiFunctionHandler
{
    /// <inheritdoc />
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.training.gaps.list", "POST", "/api/dxai/functions/localgpt.training.gaps.list/invoke",
        "List observable model answer gaps from recent Council steps and measured gaps from one saved NUTS benchmark run. Recoveries and benchmark misses are evidence for review and may require new guidance or a fresh test.",
        "Optional benchmarkRunId (UUID) for exact NUTS evidence.",
        "Read-only summary of model, team, role, case names and run IDs. Omits transcripts and provider failures.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"benchmarkRunId":{"type":"string","format":"uuid"}},"additionalProperties":false}""");

    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var runEvidence = spooler.GetMemberOutcomes(20)
                .Where(outcome => outcome.AnswerGap.HasValue)
                .GroupBy(outcome => new { outcome.RunId, outcome.CouncilTeamKey, outcome.ModelName, outcome.Role, outcome.AnswerGap })
                .Take(100)
                .Select(group => new { group.Key, Observations = group.Count() })
                .ToArray();

            ProviderModelBenchmarkReport? report = null;
            if (request.Parameters.ValueKind == JsonValueKind.Object && request.Parameters.TryGetProperty("benchmarkRunId", out var id))
            {
                if (id.ValueKind != JsonValueKind.String || !id.TryGetGuid(out var runId) || runId == Guid.Empty)
                    return json.InvalidParameters("benchmarkRunId must be a non-empty UUID.");
                report = await benchmarks.LoadStoredEvidenceAsync(runId, cancellationToken).ConfigureAwait(false);
                if (report is null || string.IsNullOrEmpty(report.NutsScenarioName))
                    return json.InvalidParameters("No saved NUTS scenario report was found for that run.");
            }

            return json.Success(new
            {
                BenchmarkRunId = report?.RunId,
                BenchmarkGaps = report?.NutsRoleEvidence.Where(evidence => evidence.CapabilityGaps.Count > 0).Take(100).ToArray(),
                RecoveryObservations = runEvidence
            });
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Reading model training gap evidence was canceled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError("Reading model training gap evidence failed with {ExceptionType}.", exception.GetType().Name);
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Could not read model training gap evidence." };
        }
    }
}
