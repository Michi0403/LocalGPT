using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Formatting;

public sealed class ChatProtocolResolver : IChatProtocolResolver
{
    private readonly IReadOnlyList<IChatProtocolProfile> profiles;

    public ChatProtocolResolver(IEnumerable<IChatProtocolProfile>? profiles = null)
    {
        this.profiles = (profiles ?? ChatProtocolProfileCatalog.CreateDefaults())
            .OrderByDescending(profile => profile.Priority)
            .ToList();
    }

    public ChatResponseProtocol Resolve(OllamaCoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.ResponseProtocol != ChatResponseProtocol.Auto)
            return options.ResponseProtocol;

        var model = options.ModelName ?? string.Empty;
        return profiles.FirstOrDefault(profile => profile.MatchesModel(model))?.Protocol
            ?? ChatResponseProtocol.Auto;
    }
}
