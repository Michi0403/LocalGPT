using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Formatting;

public sealed class ChatProtocolResolver(
    IChatProtocolProfileCatalog catalog,
    ILogger<ChatProtocolResolver> logger) : IChatProtocolResolver
{
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
