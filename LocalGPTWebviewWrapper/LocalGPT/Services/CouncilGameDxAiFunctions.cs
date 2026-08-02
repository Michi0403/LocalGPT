using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

public sealed class CouncilGameDxParameterReader(
    ILogger<CouncilGameDxParameterReader> logger)
{
    public string String(JsonElement parameters, string name, string fallback = "")
    {
        try
        {
            return parameters.ValueKind == JsonValueKind.Object &&
                   parameters.TryGetProperty(name, out var value) &&
                   value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? fallback
                : fallback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading Council game string parameter {ParameterName} failed; parameter content was omitted.", name);
            throw;
        }
    }

    public bool Boolean(JsonElement parameters, string name, bool fallback = false)
    {
        try
        {
            return parameters.ValueKind == JsonValueKind.Object &&
                   parameters.TryGetProperty(name, out var value) &&
                   value.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? value.GetBoolean()
                : fallback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading Council game Boolean parameter {ParameterName} failed; parameter content was omitted.", name);
            throw;
        }
    }

    public Guid Guid(JsonElement parameters, string name)
    {
        try
        {
            return System.Guid.TryParse(String(parameters, name), out var value)
                ? value
                : System.Guid.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading Council game GUID parameter {ParameterName} failed; parameter content was omitted.", name);
            throw;
        }
    }

    public long Long(JsonElement parameters, string name, long fallback = 0)
    {
        try
        {
            return parameters.ValueKind == JsonValueKind.Object &&
                   parameters.TryGetProperty(name, out var value) &&
                   value.TryGetInt64(out var result)
                ? result
                : fallback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading Council game Int64 parameter {ParameterName} failed; parameter content was omitted.", name);
            throw;
        }
    }

    public int? NullableInt(JsonElement parameters, string name)
    {
        try
        {
            return parameters.ValueKind == JsonValueKind.Object &&
                   parameters.TryGetProperty(name, out var value) &&
                   value.TryGetInt32(out var result)
                ? result
                : null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading Council game nullable Int32 parameter {ParameterName} failed; parameter content was omitted.", name);
            throw;
        }
    }

    public int Integer(JsonElement parameters, string name, int fallback)
    {
        try
        {
            return NullableInt(parameters, name) ?? fallback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading Council game Int32 parameter {ParameterName} failed; parameter content was omitted.", name);
            throw;
        }
    }
}

public sealed class StartCouncilGameFunction(
    ICouncilGameSessionService games,
    CouncilGameDxParameterReader parameters,
    ILogger<StartCouncilGameFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.game.session.start", "POST", "/api/dxai/functions/localgpt.game.session.start/invoke",
        "Starts a directly playable /Chat ASCII game session. Human and AI players receive the same control contract.",
        "JSON parameters: gameKey ascii-doom or green-dragon; teamKey optional; conversationId optional; controlMode Human, Ai or Shared; autoplayEnabled and autoplayDelayMilliseconds optional.",
        "Starts only an original LocalGPT runtime-class game session. It does not execute the original DOOM engine or include commercial assets.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","required":["gameKey"],"properties":{"gameKey":{"type":"string","enum":["ascii-doom","green-dragon"]},"teamKey":{"type":"string"},"conversationId":{"type":"string"},"controlMode":{"type":"string","enum":["Human","Ai","Shared"]},"autoplayEnabled":{"type":"boolean"},"autoplayDelayMilliseconds":{"type":"integer","minimum":250,"maximum":10000}},"additionalProperties":false}
        """);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var modeText = parameters.String(request.Parameters, "controlMode", "Shared");
            var mode = Enum.TryParse<CouncilGameControlMode>(modeText, true, out var parsed) ? parsed : CouncilGameControlMode.Shared;
            var result = await games.StartAsync(new StartCouncilGameRequest
            {
                GameKey = parameters.String(request.Parameters, "gameKey", "ascii-doom"),
                TeamKey = parameters.String(request.Parameters, "teamKey"),
                ConversationId = parameters.Guid(request.Parameters, "conversationId") is var id && id != Guid.Empty ? id : null,
                ControlMode = mode,
                AutoplayEnabled = parameters.Boolean(request.Parameters, "autoplayEnabled", mode == CouncilGameControlMode.Ai),
                AutoplayDelayMilliseconds = parameters.Integer(request.Parameters, "autoplayDelayMilliseconds", 1200),
                StartedBy = "LocalGPT AI Council"
            }, cancellationToken).ConfigureAwait(false);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = result };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not start a Council game session.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = ex.Message };
        }
    }
}

public sealed class GetCouncilGameFunction(
    ICouncilGameSessionService games,
    CouncilGameDxParameterReader parameters,
    ILogger<GetCouncilGameFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.game.session.get", "POST", "/api/dxai/functions/localgpt.game.session.get/invoke",
        "Reads the authoritative game frame, turn, shared controls and input gate for one /Chat game session.",
        "JSON parameters: sessionId required.", "Read-only game-state inspection.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["sessionId"],"properties":{"sessionId":{"type":"string"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var id = parameters.Guid(request.Parameters, "sessionId");
            if (id == Guid.Empty) return new DxAiFunctionInvocationResult { Succeeded = false, Status = "InvalidParameters", Error = "sessionId is required." };
            var result = await games.GetAsync(id, cancellationToken).ConfigureAwait(false);
            return result is null
                ? new DxAiFunctionInvocationResult { Succeeded = false, Status = "NotFound", Error = "Game session was not found." }
                : new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = result };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read a Council game session.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Game session read failed." };
        }
    }
}

public sealed class ControlCouncilGameFunction(
    ICouncilGameSessionService games,
    CouncilGameDxParameterReader parameters,
    ILogger<ControlCouncilGameFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.game.control", "POST", "/api/dxai/functions/localgpt.game.control/invoke",
        "Lets an AI player use exactly the same move, turn, aim, shoot, duck, use or choice action contract as the human /Chat controls.",
        "JSON parameters: sessionId, action required; expectedTurn, aimX, aimY and actorName optional.",
        "One bounded game control only. The function cannot send operating-system keyboard or gamepad input.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","required":["sessionId","action"],"properties":{"sessionId":{"type":"string"},"action":{"type":"string"},"expectedTurn":{"type":"integer"},"aimX":{"type":"integer"},"aimY":{"type":"integer"},"actorName":{"type":"string"}},"additionalProperties":false}
        """);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var id = parameters.Guid(request.Parameters, "sessionId");
            var action = parameters.String(request.Parameters, "action");
            if (id == Guid.Empty || string.IsNullOrWhiteSpace(action))
                return new DxAiFunctionInvocationResult { Succeeded = false, Status = "InvalidParameters", Error = "sessionId and action are required." };
            var result = await games.ApplyControlAsync(new CouncilGameControlRequest
            {
                SessionId = id,
                Action = action,
                AimX = parameters.NullableInt(request.Parameters, "aimX"),
                AimY = parameters.NullableInt(request.Parameters, "aimY"),
                ExpectedTurn = parameters.Long(request.Parameters, "expectedTurn", -1) is var expected && expected >= 0 ? expected : null,
                Source = "AI",
                ActorName = parameters.String(request.Parameters, "actorName", "AI Player Controller")
            }, cancellationToken).ConfigureAwait(false);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = result };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI game control was rejected.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Rejected", Error = ex.Message };
        }
    }
}

public sealed class SubmitCouncilGameFrameFunction(
    ICouncilGameSessionService games,
    CouncilGameDxParameterReader parameters,
    ILogger<SubmitCouncilGameFrameFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.game.frame.submit", "POST", "/api/dxai/functions/localgpt.game.frame.submit/invoke",
        "Submits one complete fixed-size ASCII frame. Exactly one renderer name may own a Council turn's frame.",
        "JSON parameters: sessionId, turn, rendererName and frameText required; caption optional.",
        "Frame-only mutation. It cannot change authoritative player/world state.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","required":["sessionId","turn","rendererName","frameText"],"properties":{"sessionId":{"type":"string"},"turn":{"type":"integer"},"rendererName":{"type":"string"},"frameText":{"type":"string"},"caption":{"type":"string"}},"additionalProperties":false}
        """);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await games.SubmitFrameAsync(new SubmitCouncilGameFrameRequest
            {
                SessionId = parameters.Guid(request.Parameters, "sessionId"),
                Turn = parameters.Long(request.Parameters, "turn"),
                RendererName = parameters.String(request.Parameters, "rendererName"),
                FrameText = parameters.String(request.Parameters, "frameText"),
                Caption = parameters.String(request.Parameters, "caption")
            }, cancellationToken).ConfigureAwait(false);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = result };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Council game frame submission was rejected.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Rejected", Error = ex.Message };
        }
    }
}

public sealed class SetCouncilGameControlModeFunction(
    ICouncilGameSessionService games,
    CouncilGameDxParameterReader parameters,
    ILogger<SetCouncilGameControlModeFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.game.control-mode.set", "POST", "/api/dxai/functions/localgpt.game.control-mode.set/invoke",
        "Switches a running /Chat game between human, shared and AI autoplay while retaining the same control service.",
        "JSON parameters: sessionId and controlMode required; autoplayEnabled and autoplayDelayMilliseconds optional.",
        "Only changes ownership and timing of game controls. It does not issue an operating-system input event.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["sessionId","controlMode"],"properties":{"sessionId":{"type":"string"},"controlMode":{"type":"string","enum":["Human","Shared","Ai"]},"autoplayEnabled":{"type":"boolean"},"autoplayDelayMilliseconds":{"type":"integer","minimum":250,"maximum":10000}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var id = parameters.Guid(request.Parameters, "sessionId");
            var modeText = parameters.String(request.Parameters, "controlMode");
            if (id == Guid.Empty || !Enum.TryParse<CouncilGameControlMode>(modeText, true, out var mode))
                return new DxAiFunctionInvocationResult { Succeeded = false, Status = "InvalidParameters", Error = "sessionId and a valid controlMode are required." };
            var autoplay = parameters.Boolean(request.Parameters, "autoplayEnabled", mode != CouncilGameControlMode.Human);
            var result = await games.SetControlModeAsync(
                id,
                mode,
                autoplay,
                parameters.Integer(request.Parameters, "autoplayDelayMilliseconds", 1200),
                cancellationToken).ConfigureAwait(false);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = result };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Council game control mode update was rejected.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Rejected", Error = ex.Message };
        }
    }
}

public sealed class SetCouncilGameInputGateFunction(
    ICouncilGameSessionService games,
    CouncilGameDxParameterReader parameters,
    ILogger<SetCouncilGameInputGateFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.game.input-gate.set", "POST", "/api/dxai/functions/localgpt.game.input-gate.set/invoke",
        "Shows or hides the in-chat human control overlay for one game turn without blocking the rest of LocalGPT.",
        "JSON parameters: sessionId and humanInputRequired required; reason optional.",
        "Only changes the per-game input gate.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["sessionId","humanInputRequired"],"properties":{"sessionId":{"type":"string"},"humanInputRequired":{"type":"boolean"},"reason":{"type":"string"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await games.SetInputGateAsync(new SetCouncilGameInputGateRequest
            {
                SessionId = parameters.Guid(request.Parameters, "sessionId"),
                HumanInputRequired = parameters.Boolean(request.Parameters, "humanInputRequired"),
                Reason = parameters.String(request.Parameters, "reason")
            }, cancellationToken).ConfigureAwait(false);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = result };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Council game input gate update was rejected.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Rejected", Error = ex.Message };
        }
    }
}
