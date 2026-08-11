using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents an ollama chat request.
/// </summary>
public sealed class OllamaChatRequest
{
    /// <summary>
    /// Gets or sets model.
    /// </summary>
    public string Model { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets stream.
    /// </summary>
    public bool Stream { get; set; }
    /// <summary>
    /// Gets or sets keep alive.
    /// </summary>
    public string KeepAlive { get; set; } = "10m";
    /// <summary>
    /// Gets or sets messages.
    /// </summary>
    public List<OllamaChatMessage> Messages { get; set; } = [];
    /// <summary>
    /// Gets or sets tools.
    /// </summary>
    public List<OllamaToolDefinition>? Tools { get; set; }
    /// <summary>
    /// Gets or sets options.
    /// </summary>
    public OllamaRequestOptions? Options { get; set; }
}

/// <summary>
/// Represents an ollama request options.
/// </summary>
public sealed class OllamaRequestOptions
{
    /// <summary>
    /// Gets or sets num predict.
    /// </summary>
    [JsonPropertyName("num_predict")]
    public int NumPredict { get; set; }

    /// <summary>
    /// Gets or sets num ctx.
    /// </summary>
    [JsonPropertyName("num_ctx")]
    public int? NumCtx { get; set; }

    /// <summary>
    /// Gets or sets num gpu.
    /// </summary>
    [JsonPropertyName("num_gpu")]
    public int? NumGpu { get; set; }

    /// <summary>
    /// Gets or sets temperature.
    /// </summary>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }
}

/// <summary>
/// Represents an ollama chat message.
/// </summary>
public sealed class OllamaChatMessage
{
    /// <summary>
    /// Gets or sets role.
    /// </summary>
    public string Role { get; set; } = "user";
    /// <summary>
    /// Gets or sets content.
    /// </summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets thinking.
    /// </summary>
    public string? Thinking { get; set; }

    /// <summary>
    /// Gets or sets images.
    /// </summary>
    [JsonPropertyName("images")]
    public List<string>? Images { get; set; }

    /// <summary>
    /// Gets or sets tool calls.
    /// </summary>
    [JsonPropertyName("tool_calls")]
    public List<OllamaToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// Gets or sets tool name.
    /// </summary>
    [JsonPropertyName("tool_name")]
    public string? ToolName { get; set; }
}

/// <summary>
/// Represents an ollama tool definition.
/// </summary>
public sealed class OllamaToolDefinition
{
    /// <summary>
    /// Gets or sets type.
    /// </summary>
    public string Type { get; set; } = "function";
    /// <summary>
    /// Gets or sets function.
    /// </summary>
    public OllamaToolFunctionDefinition Function { get; set; } = new();
}

/// <summary>
/// Represents an ollama tool function definition.
/// </summary>
public sealed class OllamaToolFunctionDefinition
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets parameters.
    /// </summary>
    public JsonElement Parameters { get; set; }
}

/// <summary>
/// Represents an ollama tool call.
/// </summary>
public sealed class OllamaToolCall
{
    /// <summary>
    /// Gets or sets function.
    /// </summary>
    public OllamaToolFunctionCall Function { get; set; } = new();
}

/// <summary>
/// Represents an ollama tool function call.
/// </summary>
public sealed class OllamaToolFunctionCall
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets arguments.
    /// </summary>
    public JsonElement Arguments { get; set; }
}

/// <summary>
/// Represents an ollama chat response.
/// </summary>
public sealed class OllamaChatResponse
{
    /// <summary>
    /// Gets or sets message.
    /// </summary>
    public OllamaChatMessage? Message { get; set; }
}
