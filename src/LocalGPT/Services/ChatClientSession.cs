using LocalGPT.BusinessObjects;
using DevExpress.AIIntegration.Blazor.Chat;
using Microsoft.Extensions.AI;

namespace LocalGPT.Services;

/// <summary>
/// Represents a chat client session application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public class ChatClientSession
{
    /// <summary>
    /// Gets or sets the name value that forms part of the chat client session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="ChatClientSession"/>.</value>
    public string Name { get; set; }
    /// <summary>
    /// Gets the provider value that forms part of the chat client session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The provider value exposed by <see cref="ChatClientSession"/>.</value>
    public string Provider { get; }
    /// <summary>
    /// Gets the model name value that forms part of the chat client session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model name value exposed by <see cref="ChatClientSession"/>.</value>
    public string ModelName { get; }
    /// <summary>
    /// Gets the endpoint that identifies the network or application endpoint associated with this chat client session state.
    /// </summary>
    /// <value>The endpoint value exposed by <see cref="ChatClientSession"/>.</value>
    public string Endpoint { get; }
    /// <summary>
    /// Gets the stable selection key used to identify or correlate this chat client session instance with related application state.
    /// </summary>
    /// <value>The selection key value exposed by <see cref="ChatClientSession"/>.</value>
    public string SelectionKey => new ProviderModelIdentity().CreateSelectionKey(Provider, Endpoint, ModelName);
    /// <summary>
    /// Gets the client value that forms part of the chat client session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The client value exposed by <see cref="ChatClientSession"/>.</value>
    public IChatClient Client { get; }
    /// <summary>
    /// Gets or sets the messages collection maintained or exposed by this chat client session instance for downstream processing.
    /// </summary>
    /// <value>The messages value exposed by <see cref="ChatClientSession"/>.</value>
    public List<BlazorChatMessage> Messages { get; set; }

    /// <summary>
    /// Initializes a new <see cref="ChatClientSession"/> instance and captures the dependencies or initial state required by its chat client session workflow.
    /// </summary>
    /// <param name="client">Chat client dependency used by the chat client session workflow to provide the corresponding application capability.</param>
    /// <param name="name">Name value supplied to the chat client session operation and used when producing its result.</param>
    /// <param name="provider">Provider value supplied to the chat client session operation and used when producing its result.</param>
    /// <param name="modelName">Model name value supplied to the chat client session operation and used when producing its result.</param>
    /// <param name="endpoint">Endpoint value supplied to the chat client session operation and used when producing its result.</param>
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
    /// Performs infer provider for <see cref="ChatClientSession"/>, keeping the operation consistent with the state and invariants of the surrounding chat client session workflow.
    /// </summary>
    /// <param name="name">Name value supplied to the chat client session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Performs infer model for <see cref="ChatClientSession"/>, keeping the operation consistent with the state and invariants of the surrounding chat client session workflow.
    /// </summary>
    /// <param name="name">Name value supplied to the chat client session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
