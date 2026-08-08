using LocalGPT.BusinessObjects;
using DevExpress.AIIntegration.Blazor.Chat;
using Microsoft.Extensions.AI;

namespace LocalGPT.Services;

public class ChatClientSession
{
    public string Name { get; set; }
    public string Provider { get; }
    public string ModelName { get; }
    public string Endpoint { get; }
    public string SelectionKey => new ProviderModelIdentity().CreateSelectionKey(Provider, Endpoint, ModelName);
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

    private string InferProvider(string name) {
    try
    {
        return name.Split('—', 2, StringSplitOptions.TrimEntries)[0];
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method ChatClientSession.InferProvider failed: {__serviceMethodException}");
        throw;
    }
}
    private string InferModel(string name)
    {
    try
    {
            var parts = name.Split('—', 2, StringSplitOptions.TrimEntries);
            return parts.Length == 2 ? parts[1] : name;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method ChatClientSession.InferModel failed: {__serviceMethodException}");
        throw;
    }
}
}
