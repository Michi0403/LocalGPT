using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Reads full or cropped Council ASCII display cells for continuity-aware rendering.</summary>
public sealed class GetCouncilGameDisplayFunction(ICouncilGameSessionService games, CouncilGameDxParameterReader parameters, ILogger<GetCouncilGameDisplayFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the DXFunction descriptor.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.game.display.get", "POST", "/api/dxai/functions/localgpt.game.display.get/invoke",
        "Reads the current fixed-cell Council ASCII display, including its live dimensions, so a renderer can continue an existing scene without redrawing blindly.",
        "sessionId optional; when omitted LocalGPT resolves the current conversation game when available, otherwise the current active game. x, y, width and height optional. Zero width/height means through the display edge.",
        "Read-only display inspection; it does not read the shell command buffer or private conversation content.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "CouncilGameDisplayDxAiFunctions",
        ParameterSchemaJson: """{"type":"object","properties":{"sessionId":{"type":"string"},"x":{"type":"integer","minimum":0},"y":{"type":"integer","minimum":0},"width":{"type":"integer","minimum":0,"maximum":240},"height":{"type":"integer","minimum":0,"maximum":100}},"additionalProperties":false}""");

    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionId = parameters.Guid(request.Parameters, "sessionId");
            if (sessionId == Guid.Empty)
            {
                var active = await games.GetActiveAsync(request.ConversationId, cancellationToken).ConfigureAwait(false);
                if (active is null)
                    return new() { Succeeded = false, Status = "NotFound", Error = "No active game is available." };
                sessionId = active.Id;
            }
            var result = await games.GetDisplayAsync(new ReadCouncilGameDisplayRequest
            {
                SessionId = sessionId, X = parameters.Integer(request.Parameters, "x", 0), Y = parameters.Integer(request.Parameters, "y", 0), Width = parameters.Integer(request.Parameters, "width", 0), Height = parameters.Integer(request.Parameters, "height", 0)
            }, cancellationToken).ConfigureAwait(false);
            return new() { Succeeded = true, Status = "Completed", Value = result };
        }
        catch (Exception ex) { logger.LogWarning(ex, "Council display read was rejected."); return new() { Succeeded = false, Status = "Rejected", Error = ex.Message }; }
    }
}

/// <summary>Writes simple text into a Council ASCII display.</summary>
public sealed class WriteCouncilGameDisplayTextFunction(ICouncilGameSessionService games, CouncilGameDxParameterReader parameters, ILogger<WriteCouncilGameDisplayTextFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the DXFunction descriptor.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.game.display.text.write", "POST", "/api/dxai/functions/localgpt.game.display.text.write/invoke",
        "Writes clipped single- or multi-line text at display-cell coordinates. Prefer this over a full frame when only labels, reactions or small text areas changed.",
        "rendererName, x, y and text required; sessionId and turn optional and resolve to the current conversation game/turn when available, otherwise the current active game/turn.", "Display-only mutation; one renderer owns a turn and authoritative game state is unchanged.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, IsCoordinationOnly: true, Source: "CouncilGameDisplayDxAiFunctions",
        ParameterSchemaJson: """{"type":"object","required":["rendererName","x","y","text"],"properties":{"sessionId":{"type":"string"},"turn":{"type":"integer"},"rendererName":{"type":"string"},"x":{"type":"integer"},"y":{"type":"integer"},"text":{"type":"string"}},"additionalProperties":false}""");
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { var resolved = await parameters.ResolveSessionAsync(games, request, cancellationToken).ConfigureAwait(false); if (resolved is null) return new(){Succeeded=false,Status="NotFound",Error="No active game is available."}; var value = await games.WriteTextAsync(new() { SessionId = resolved.Value.SessionId, Turn = resolved.Value.Turn, RendererName = parameters.String(request.Parameters,"rendererName"), X = parameters.Integer(request.Parameters,"x",0), Y = parameters.Integer(request.Parameters,"y",0), Text = parameters.String(request.Parameters,"text") }, cancellationToken).ConfigureAwait(false); return new() { Succeeded=true, Status="Completed", Value=value }; }
        catch(Exception ex) { logger.LogWarning(ex,"Council display text write was rejected."); return new(){Succeeded=false,Status="Rejected",Error=ex.Message}; }
    }
}

/// <summary>Writes one Council ASCII display cell.</summary>
public sealed class SetCouncilGameDisplayCellFunction(ICouncilGameSessionService games, CouncilGameDxParameterReader parameters, ILogger<SetCouncilGameDisplayCellFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the DXFunction descriptor.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.game.display.cell.set", "POST", "/api/dxai/functions/localgpt.game.display.cell.set/invoke",
        "Changes one display cell for tiny reactions, cursor-like markers or surgical frame edits.", "rendererName, x, y and glyph required; sessionId and turn optional and resolve to the current conversation game/turn when available, otherwise the current active game/turn.", "Display-only mutation; clipped to the configured terminal cells.",
        IsReadOnly:false, AvailableToAi:true, RequiresHumanConfirmation:false, SupportsDirectInvocation:true, SupportsAutomaticInvocation:true, IsCoordinationOnly:true, Source:"CouncilGameDisplayDxAiFunctions",
        ParameterSchemaJson:"""{"type":"object","required":["rendererName","x","y","glyph"],"properties":{"sessionId":{"type":"string"},"turn":{"type":"integer"},"rendererName":{"type":"string"},"x":{"type":"integer"},"y":{"type":"integer"},"glyph":{"type":"string","minLength":1}},"additionalProperties":false}""");
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request,CancellationToken cancellationToken=default)
    {
        try { var resolved=await parameters.ResolveSessionAsync(games,request,cancellationToken).ConfigureAwait(false); if(resolved is null)return new(){Succeeded=false,Status="NotFound",Error="No active game is available."}; var value=await games.SetCellAsync(new(){SessionId=resolved.Value.SessionId,Turn=resolved.Value.Turn,RendererName=parameters.String(request.Parameters,"rendererName"),X=parameters.Integer(request.Parameters,"x",0),Y=parameters.Integer(request.Parameters,"y",0),Glyph=parameters.String(request.Parameters,"glyph"," ")},cancellationToken).ConfigureAwait(false); return new(){Succeeded=true,Status="Completed",Value=value}; }
        catch(Exception ex){logger.LogWarning(ex,"Council display cell write was rejected.");return new(){Succeeded=false,Status="Rejected",Error=ex.Message};}
    }
}

/// <summary>Fills a Council ASCII display rectangle with one glyph.</summary>
public sealed class FillCouncilGameDisplayRegionFunction(ICouncilGameSessionService games, CouncilGameDxParameterReader parameters, ILogger<FillCouncilGameDisplayRegionFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the DXFunction descriptor.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.game.display.region.fill", "POST", "/api/dxai/functions/localgpt.game.display.region.fill/invoke",
        "Fills a clipped rectangular cell region with one glyph; useful for clears, panels, floors and repeated texture without generating every character.", "rendererName, x, y, width, height and glyph required; sessionId and turn optional and resolve to the current conversation game/turn when available, otherwise the current active game/turn.", "Display-only mutation; width/height are bounded.",
        IsReadOnly:false,AvailableToAi:true,RequiresHumanConfirmation:false,SupportsDirectInvocation:true,SupportsAutomaticInvocation:true,IsCoordinationOnly:true,Source:"CouncilGameDisplayDxAiFunctions",
        ParameterSchemaJson:"""{"type":"object","required":["rendererName","x","y","width","height","glyph"],"properties":{"sessionId":{"type":"string"},"turn":{"type":"integer"},"rendererName":{"type":"string"},"x":{"type":"integer"},"y":{"type":"integer"},"width":{"type":"integer","minimum":1,"maximum":240},"height":{"type":"integer","minimum":1,"maximum":100},"glyph":{"type":"string","minLength":1}},"additionalProperties":false}""");
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request,CancellationToken cancellationToken=default)
    {
        try{var resolved=await parameters.ResolveSessionAsync(games,request,cancellationToken).ConfigureAwait(false);if(resolved is null)return new(){Succeeded=false,Status="NotFound",Error="No active game is available."};var value=await games.FillRegionAsync(new(){SessionId=resolved.Value.SessionId,Turn=resolved.Value.Turn,RendererName=parameters.String(request.Parameters,"rendererName"),X=parameters.Integer(request.Parameters,"x",0),Y=parameters.Integer(request.Parameters,"y",0),Width=Math.Clamp(parameters.Integer(request.Parameters,"width",1),1,240),Height=Math.Clamp(parameters.Integer(request.Parameters,"height",1),1,100),Glyph=parameters.String(request.Parameters,"glyph"," ")},cancellationToken).ConfigureAwait(false);return new(){Succeeded=true,Status="Completed",Value=value};}
        catch(Exception ex){logger.LogWarning(ex,"Council display fill was rejected.");return new(){Succeeded=false,Status="Rejected",Error=ex.Message};}
    }
}

/// <summary>Blits a multiline ASCII block into a Council display.</summary>
public sealed class BlitCouncilGameDisplayRegionFunction(ICouncilGameSessionService games, CouncilGameDxParameterReader parameters, ILogger<BlitCouncilGameDisplayRegionFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the DXFunction descriptor.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.game.display.region.blit", "POST", "/api/dxai/functions/localgpt.game.display.region.blit/invoke",
        "Blits a multiline ASCII sprite, panel or art block at display-cell coordinates. Prefer this for medium changes and reusable generated shapes.", "rendererName, x, y and text required; sessionId and turn optional and resolve to the current conversation game/turn when available, otherwise the current active game/turn.", "Display-only mutation; content is clipped to the live display dimensions.",
        IsReadOnly:false,AvailableToAi:true,RequiresHumanConfirmation:false,SupportsDirectInvocation:true,SupportsAutomaticInvocation:true,IsCoordinationOnly:true,Source:"CouncilGameDisplayDxAiFunctions",
        ParameterSchemaJson:"""{"type":"object","required":["rendererName","x","y","text"],"properties":{"sessionId":{"type":"string"},"turn":{"type":"integer"},"rendererName":{"type":"string"},"x":{"type":"integer"},"y":{"type":"integer"},"text":{"type":"string"}},"additionalProperties":false}""");
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request,CancellationToken cancellationToken=default)
    {
        try{var resolved=await parameters.ResolveSessionAsync(games,request,cancellationToken).ConfigureAwait(false);if(resolved is null)return new(){Succeeded=false,Status="NotFound",Error="No active game is available."};var value=await games.BlitRegionAsync(new(){SessionId=resolved.Value.SessionId,Turn=resolved.Value.Turn,RendererName=parameters.String(request.Parameters,"rendererName"),X=parameters.Integer(request.Parameters,"x",0),Y=parameters.Integer(request.Parameters,"y",0),Text=parameters.String(request.Parameters,"text")},cancellationToken).ConfigureAwait(false);return new(){Succeeded=true,Status="Completed",Value=value};}
        catch(Exception ex){logger.LogWarning(ex,"Council display blit was rejected.");return new(){Succeeded=false,Status="Rejected",Error=ex.Message};}
    }
}

/// <summary>Submits a pregenerated ASCII movie for local non-blocking playback.</summary>
public sealed class SubmitCouncilGameAnimationFunction(ICouncilGameSessionService games, CouncilGameDxParameterReader parameters, ILogger<SubmitCouncilGameAnimationFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the DXFunction descriptor.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.game.animation.submit", "POST", "/api/dxai/functions/localgpt.game.animation.submit/invoke",
        "Submits 2-12 complete pregenerated ASCII frames. Generate the frames fluently in one normal text-generation step, then let the browser play them locally so chat/council flow never waits on per-frame AI calls.", "rendererName and frames required; sessionId and turn optional and resolve to the current conversation game/turn when available, otherwise the current active game/turn; delayMilliseconds and caption optional.", "Display presentation only; does not mutate authoritative world state or transcript history.",
        IsReadOnly:false,AvailableToAi:true,RequiresHumanConfirmation:false,SupportsDirectInvocation:true,SupportsAutomaticInvocation:true,IsCoordinationOnly:true,Source:"CouncilGameDisplayDxAiFunctions",
        ParameterSchemaJson:"""{"type":"object","required":["rendererName","frames"],"properties":{"sessionId":{"type":"string"},"turn":{"type":"integer"},"rendererName":{"type":"string"},"frames":{"type":"array","minItems":2,"maxItems":12,"items":{"type":"string"}},"delayMilliseconds":{"type":"integer","minimum":250,"maximum":5000},"caption":{"type":"string"}},"additionalProperties":false}""");
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request,CancellationToken cancellationToken=default)
    {
        try{var resolved=await parameters.ResolveSessionAsync(games,request,cancellationToken).ConfigureAwait(false);if(resolved is null)return new(){Succeeded=false,Status="NotFound",Error="No active game is available."};var value=await games.SubmitAnimationAsync(new(){SessionId=resolved.Value.SessionId,Turn=resolved.Value.Turn,RendererName=parameters.String(request.Parameters,"rendererName"),Frames=parameters.Strings(request.Parameters,"frames"),DelayMilliseconds=parameters.Integer(request.Parameters,"delayMilliseconds",650),Caption=parameters.String(request.Parameters,"caption")},cancellationToken).ConfigureAwait(false);return new(){Succeeded=true,Status="Completed",Value=value};}
        catch(Exception ex){logger.LogWarning(ex,"Council ASCII animation was rejected.");return new(){Succeeded=false,Status="Rejected",Error=ex.Message};}
    }
}
