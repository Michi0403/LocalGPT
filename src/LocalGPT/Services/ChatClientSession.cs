using LocalGPT.BusinessObjects;
using DevExpress.AIIntegration.Blazor.Chat;
using Microsoft.Extensions.AI;

namespace LocalGPT.Services;

/// <summary>
/// Represents a chat client session.
/// </summary>
public class ChatClientSession
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets provider.
    /// </summary>
    public string Provider { get; }
    /// <summary>
    /// Gets or sets model name.
    /// </summary>
    public string ModelName { get; }
    /// <summary>
    /// Gets or sets endpoint.
    /// </summary>
    public string Endpoint { get; }
    /// <summary>
    /// Gets or sets selection key.
    /// </summary>
    public string SelectionKey => new ProviderModelIdentity().CreateSelectionKey(Provider, Endpoint, ModelName);
    /// <summary>
    /// Gets or sets client.
    /// </summary>
    public IChatClient Client { get; }
    /// <summary>
    /// Gets or sets messages.
    /// </summary>
    public List<BlazorChatMessage> Messages { get; set; }

    /// <summary>
    /// Runs the chat client session operation.
    /// </summary>
    public ChatClientSession(IChatClient client, string name, string? provider = null, string? modelName = null, string? endpoint = null)
    {
        Name = name;
        Provider = string.IsNullOrWhiteSpace(provider) ? InferProvider(name) : provider;
        ModelName = string.IsNullOrWhiteSpace(modelName) ? InferModel(name) : modelName;
        Endpoint = endpoint?.Trim() ?? string.Empty;
        Client = client ?? throw new ArgumentNullException(nameof(client));
        Messages = [];
    }

    /// <summary>
    /// Runs the infer provider operation.
    /// </summary>
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
    /// <summary>
    /// Runs the infer model operation.
    /// </summary>
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
