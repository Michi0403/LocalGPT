using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LocalGPT.Services;

/// <summary>
/// Converts chat messages to Ollama message format.
/// </summary>
public static partial class Conversation 
{
    /// <summary>
    /// Converts conversation history to Ollama messages array.
    /// </summary>
    public static async Task<List<OllamaMessage>> ToOllamaMessagesAsync(
        IEnumerable<string> roles,
        IEnumerable<string> contents,
        CancellationToken ct = default)
    {
        var messages = new List<OllamaMessage>();
        foreach ((var role, var content) in roles.Zip(contents))
        {
            messages.Add(new OllamaMessage { Role = role.Value, Content = content.Value });
        }
        return messages;
    }
    
    /// <summary>
    /// Measures total character count across all request components.
    /// </summary>
    public static int MeasureRequestCharacters(
        string systemPrompt,
        IEnumerable<string> userMessages,
        IEnumerable<string> tools)
    {
        int count = 0;
        if (!string.IsNullOrEmpty(systemPrompt)) count += systemPrompt.Length;
        foreach (var msg in userMessages) count += msg.Length;
        foreach (var tool in tools) count += tool.Length;
        return count;
    }
    
    /// <summary>
    /// Gets available discovery broker tools from configuration.
    /// </summary>
    public static List<string> GetDiscoveryBrokerTools(
        string? toolsPath = null,
        CancellationToken ct = default)
    {
        var tools = new List<string>();
        return tools;
    }
}

public sealed partial class OllamaContextBudgetExceeded : Exception 
{
    public OllamaContextBudgetExceeded(string message) : base(message) { }
}
