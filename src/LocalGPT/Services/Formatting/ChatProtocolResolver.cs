using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Formatting;

/// <summary>
/// Resolves chat protocol choices from the available runtime state and returns the application-appropriate result to callers.
/// </summary>
/// <param name="catalog">Chat protocol profile catalog dependency used by the chat protocol workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ChatProtocolResolver(
    IChatProtocolProfileCatalog catalog,
    ILogger<ChatProtocolResolver> logger) : IChatProtocolResolver
{
    /// <summary>
    /// Performs resolve for <see cref="ChatProtocolResolver"/>, keeping the operation consistent with the state and invariants of the surrounding chat protocol workflow.
    /// </summary>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <returns>The chat response protocol produced by the operation.</returns>
    public ChatResponseProtocol Resolve(OllamaCoreOptions options)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(options);
            if (options.ResponseProtocol != ChatResponseProtocol.Auto)
            {
                logger.LogTrace($"Used explicitly configured chat protocol {options.ResponseProtocol}.");
                return options.ResponseProtocol;
            }

            var model = options.ModelName ?? string.Empty;
            var protocol = catalog.Profiles.FirstOrDefault(profile => profile.MatchesModel(model))?.Protocol
                ?? ChatResponseProtocol.Auto;
            logger.LogTrace($"Resolved chat protocol {protocol} for model {model}.");
            return protocol;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve the chat protocol: {exception.Message}");
            throw;
        }
    }
}
