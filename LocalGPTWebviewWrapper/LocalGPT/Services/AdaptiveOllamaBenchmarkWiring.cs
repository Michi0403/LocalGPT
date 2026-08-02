using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Explicit boundary for the still-unverified empirical model tuner. The shipped Reactive ASCII
/// Gameplay preset is usable now; automatic multi-model benchmarking must be completed and tested
/// against a real local Ollama runtime before it is allowed to rewrite persisted routes.
/// </summary>
public sealed class AdaptiveOllamaBenchmarkWiring(
    ILogger<AdaptiveOllamaBenchmarkWiring> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.models.benchmark.autotune", "POST", "/api/dxai/functions/localgpt.models.benchmark.autotune/invoke",
        "Reserved wiring for a confirmed local Ollama benchmark that tests accessible models and persists a copied best-speed preset.",
        "No parameters are currently accepted.",
        "Not implemented in 2.0.3: empirical tuning requires a real local Ollama/GPU test matrix and must not guess or silently rewrite model routes.",
        IsReadOnly: false, AvailableToAi: false, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""");

    public Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return RunEmpiricalBenchmarkAsync(cancellationToken);
        }
        catch (NotImplementedException exception)
        {
            logger.LogWarning(exception, "Adaptive Ollama benchmark wiring was invoked before local-runtime validation was completed.");
            return Task.FromResult(new DxAiFunctionInvocationResult
            {
                Succeeded = false,
                Status = "NotImplemented",
                Error = exception.Message
            });
        }
    }

    private Task<DxAiFunctionInvocationResult> RunEmpiricalBenchmarkAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new NotImplementedException(
            "Adaptive empirical Ollama tuning is intentionally not active in LocalGPT 2.0.3. " +
            "Run and validate the benchmark against the target machine before enabling persistence; " +
            "the preseeded Reactive ASCII Gameplay preset remains the supported low-latency default.");
    }
}
