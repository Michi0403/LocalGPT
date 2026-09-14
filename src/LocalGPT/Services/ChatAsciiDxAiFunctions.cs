using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Exposes the current circuit-scoped ASCII chat presentation state through the normal DXFunction catalog so Council teams and individual models can explicitly inspect the terminal capability without scraping browser state.
/// </summary>
/// <param name="asciiExperience">Circuit-scoped ASCII presentation state shared with the /chat renderer and provider prompt builders.</param>
/// <param name="json">DXFunction JSON service used to serialize the bounded capability result.</param>
/// <param name="logger">Logger used for bounded diagnostics; conversation content is intentionally omitted.</param>
public sealed class GetChatAsciiSurfaceFunction(
    IChatAsciiExperienceState asciiExperience,
    IDxAiFunctionJsonService json,
    ILogger<GetChatAsciiSurfaceFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Declares the read-only <c>localgpt.ascii.surface.get</c> function contract exposed to the DXFunction catalog.</summary>
    /// <value>The read-only DXFunction descriptor discovered by the normal registry/catalog synchronization pipeline.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.ascii.surface.get",
        "POST",
        "/api/dxai/functions/localgpt.ascii.surface.get/invoke",
        "Returns whether the current /chat ASCII terminal is open, whether contextual ASCII fun is enabled, and the bounded art/sequence/game conventions available to this model or Council member.",
        "No parameters.",
        "Read-only circuit-local presentation metadata. It does not expose chat content, execute a command, mutate the conversation, or open the terminal.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "ChatAsciiDxAiFunctions",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""");

    /// <summary>Returns the current ASCII presentation capability snapshot for the invoking chat circuit.</summary>
    /// <param name="request">DXFunction invocation request; no parameters are consumed.</param>
    /// <param name="cancellationToken">Cancellation token supplied by the normal DXFunction execution pipeline.</param>
    /// <returns>A bounded structured capability snapshot that models can use to decide whether optional ASCII output is appropriate.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            var result = new
            {
                surfaceOpen = asciiExperience.IsSurfaceOpen,
                funModeEnabled = asciiExperience.IsFunModeEnabled,
                conversationId = asciiExperience.ConversationId,
                canonicalConversationShared = true,
                supportsAsciiArt = true,
                supportsAsciiSequence = true,
                supportsCouncilGameRuntime = true,
                supportsBidirectionalCouncilDisplay = true,
                supportsPregeneratedCouncilAnimations = true,
                displayFunctions = new[]
                {
                    "localgpt.game.display.get", "localgpt.game.display.text.write", "localgpt.game.display.cell.set",
                    "localgpt.game.display.region.fill", "localgpt.game.display.region.blit", "localgpt.game.frame.submit",
                    "localgpt.game.animation.submit"
                },
                reusableEvidenceFunctions = new[] { "localgpt.regex.list", "localgpt.regex.get", "localgpt.regex.test", "localgpt.knowledge.list" },
                asciiFence = "ascii",
                sequenceFence = "ascii-sequence",
                sequenceFrameSeparator = "--- frame ---",
                minimumSequenceFrames = 2,
                maximumSequenceFrames = 12,
                recommendedMaximumColumns = 80,
                recommendedMaximumRowsPerFrame = 28,
                councilDisplayDimensionsAreSessionSpecific = true,
                note = asciiExperience.IsSurfaceOpen
                    ? asciiExperience.IsFunModeEnabled
                        ? "The shared terminal is open and Crazy ASCII mode is enabled. Keep the natural-language answer authoritative, but include a contextual visual reaction on every assistant/Council-facing turn. Prefer a lightweight smiley, divider, text decoration or small art most turns; use larger art or a bounded pregenerated animation selectively. Presentation must never rewrite or block the canonical transcript."
                        : "The shared terminal is open. Keep normal chat authoritative; ASCII art, direct display operations or bounded animation are available when they materially improve the interaction."
                    : "The terminal is closed. Keep normal chat output authoritative; do not emit ASCII decoration solely for a hidden terminal."
            };
            return Task.FromResult(json.Success(result));
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Reading the ASCII chat presentation capability was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the ASCII chat presentation capability failed.");
            return Task.FromResult(new DxAiFunctionInvocationResult
            {
                Succeeded = false,
                Status = "Failed",
                Error = "The ASCII chat presentation capability could not be read. Review LocalGPT logs."
            });
        }
    }
}
