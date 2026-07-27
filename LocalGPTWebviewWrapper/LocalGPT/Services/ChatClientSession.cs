using DevExpress.AIIntegration.Blazor.Chat;
using Microsoft.Extensions.AI;

namespace LocalGPT.Services;

public class ChatClientSession
{
    public string Name { get; set; }
    public string Provider { get; }
    public string ModelName { get; }
    public string Endpoint { get; }
    public IChatClient Client { get; }
    public List<BlazorChatMessage> Messages { get; set; }

    public ChatClientSession(IChatClient client, string name, string? provider = null, string? modelName = null, string? endpoint = null)
    {
        Name = name;
        Provider = string.IsNullOrWhiteSpace(provider) ? InferProvider(name) : provider;
        ModelName = string.IsNullOrWhiteSpace(modelName) ? InferModel(name) : modelName;
        Endpoint = endpoint?.Trim() ?? string.Empty;
        Client = client ?? throw new ArgumentNullException(nameof(client));
        Messages = [];
    }

    private static string InferProvider(string name) => name.Split('—', 2, StringSplitOptions.TrimEntries)[0];
    private static string InferModel(string name)
    {
        var parts = name.Split('—', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? parts[1] : name;
    }
}
