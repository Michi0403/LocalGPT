using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

public sealed class OllamaChatRequest
{
    public string Model { get; set; } = string.Empty;
    public bool Stream { get; set; }
    public string KeepAlive { get; set; } = "10m";
    public List<OllamaChatMessage> Messages { get; set; } = [];
    public List<OllamaToolDefinition>? Tools { get; set; }
    public OllamaRequestOptions? Options { get; set; }
}

public sealed class OllamaRequestOptions
{
    [JsonPropertyName("num_predict")]
    public int NumPredict { get; set; }

    [JsonPropertyName("num_ctx")]
    public int? NumCtx { get; set; }

    [JsonPropertyName("num_gpu")]
    public int? NumGpu { get; set; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }
}

public sealed class OllamaChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public string? Thinking { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<OllamaToolCall>? ToolCalls { get; set; }

    [JsonPropertyName("tool_name")]
    public string? ToolName { get; set; }
}

public sealed class OllamaToolDefinition
{
    public string Type { get; set; } = "function";
    public OllamaToolFunctionDefinition Function { get; set; } = new();
}

public sealed class OllamaToolFunctionDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JsonElement Parameters { get; set; }
}

public sealed class OllamaToolCall
{
    public OllamaToolFunctionCall Function { get; set; } = new();
}

public sealed class OllamaToolFunctionCall
{
    public string Name { get; set; } = string.Empty;
    public JsonElement Arguments { get; set; }
}

public sealed class OllamaChatResponse
{
    public OllamaChatMessage? Message { get; set; }
}
