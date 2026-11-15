using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LocalGPT.Services;

/// <summary>Runs the packaged LoRA trainer probe and reports this machine, its backend and whether a model of a given size fits.</summary>
/// <param name="training">Model training service that runs the packaged trainer and owns every tunable value.</param>
/// <param name="json">Devexpress ai function json service used to shape the invocation result.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ModelTrainingProbeFunction(IModelTrainingService training, IDxAiFunctionJsonService json, ILogger<ModelTrainingProbeFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the model training probe function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ModelTrainingProbeFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.training.probe", "POST", "/api/dxai/functions/localgpt.training.probe/invoke",
        "Probes this machine for local LoRA training: operating system, Python libraries, backend, memory, precision support, problems with fixes and, when parametersBillions is given, a memory verdict for 16-bit LoRA and 4-bit QLoRA.",
        "parametersBillions and loraRank are optional and must be given together or not at all; loraRank defaults to the configured training rank.",
        "Read-only. Starts the packaged trainer script in probe mode with a scrubbed environment; it never downloads anything and changes no file.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"parametersBillions":{"type":"number","exclusiveMinimum":0},"loraRank":{"type":"integer","minimum":1}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ModelTrainingProbeFunction"/>, keeping the operation consistent with the state and invariants of the surrounding model training probe function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            double? parametersBillions = request.Parameters.ValueKind == JsonValueKind.Object && request.Parameters.TryGetProperty("parametersBillions", out var billionsElement) && billionsElement.ValueKind == JsonValueKind.Number
                ? billionsElement.GetDouble()
                : null;
            int? loraRank = request.Parameters.ValueKind == JsonValueKind.Object && request.Parameters.TryGetProperty("loraRank", out var rankElement) && rankElement.ValueKind == JsonValueKind.Number && rankElement.TryGetInt32(out var rankValue)
                ? rankValue
                : null;
            var probe = await training.ProbeAsync(parametersBillions, loraRank, cancellationToken).ConfigureAwait(false);
            return json.Success(JsonNode.Parse(probe));
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "The model training probe function was canceled.");
            throw;
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "The model training probe function was refused or failed.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = exception.Message };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The model training probe function failed with {ExceptionType}.", exception.GetType().Name);
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "The model training probe function failed. Review LocalGPT logs." };
        }
    }
}
