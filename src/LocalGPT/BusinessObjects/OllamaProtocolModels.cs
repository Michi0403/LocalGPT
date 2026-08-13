using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents the input contract for Ollama chat, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class OllamaChatRequest
{
    /// <summary>
    /// Gets or sets the model value that forms part of the Ollama chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model value exposed by <see cref="OllamaChatRequest"/>.</value>
    public string Model { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether stream applies to the Ollama chat state.
    /// </summary>
    /// <value>The stream value exposed by <see cref="OllamaChatRequest"/>.</value>
    public bool Stream { get; set; }
    /// <summary>
    /// Gets or sets the keep alive value that forms part of the Ollama chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The keep alive value exposed by <see cref="OllamaChatRequest"/>.</value>
    public string KeepAlive { get; set; } = "10m";
    /// <summary>
    /// Gets or sets the messages collection maintained or exposed by this Ollama chat instance for downstream processing.
    /// </summary>
    /// <value>The messages value exposed by <see cref="OllamaChatRequest"/>.</value>
    public List<OllamaChatMessage> Messages { get; set; } = [];
    /// <summary>
    /// Gets or sets the tools collection maintained or exposed by this Ollama chat instance for downstream processing.
    /// </summary>
    /// <value>The tools value exposed by <see cref="OllamaChatRequest"/>.</value>
    public List<OllamaToolDefinition>? Tools { get; set; }
    /// <summary>
    /// Gets or sets the options value that forms part of the Ollama chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The options value exposed by <see cref="OllamaChatRequest"/>.</value>
    public OllamaRequestOptions? Options { get; set; }
}

/// <summary>
/// Carries the configurable Ollama request settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed class OllamaRequestOptions
{
    /// <summary>
    /// Gets or sets the num predict value that forms part of the Ollama request state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The num predict value exposed by <see cref="OllamaRequestOptions"/>.</value>
    [JsonPropertyName("num_predict")]
    public int NumPredict { get; set; }

    /// <summary>
    /// Gets or sets the num ctx value that forms part of the Ollama request state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The num ctx value exposed by <see cref="OllamaRequestOptions"/>.</value>
    [JsonPropertyName("num_ctx")]
    public int? NumCtx { get; set; }

    /// <summary>
    /// Gets or sets the num GPU value that forms part of the Ollama request state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The num GPU value exposed by <see cref="OllamaRequestOptions"/>.</value>
    [JsonPropertyName("num_gpu")]
    public int? NumGpu { get; set; }

    /// <summary>
    /// Gets or sets the temperature value that forms part of the Ollama request state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The temperature value exposed by <see cref="OllamaRequestOptions"/>.</value>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }
}

/// <summary>
/// Represents an Ollama chat message application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OllamaChatMessage
{
    /// <summary>
    /// Gets or sets the role value that forms part of the Ollama chat message state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The role value exposed by <see cref="OllamaChatMessage"/>.</value>
    public string Role { get; set; } = "user";
    /// <summary>
    /// Gets or sets the content value that forms part of the Ollama chat message state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content value exposed by <see cref="OllamaChatMessage"/>.</value>
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the thinking value that forms part of the Ollama chat message state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The thinking value exposed by <see cref="OllamaChatMessage"/>.</value>
    public string? Thinking { get; set; }

    /// <summary>
    /// Gets or sets the images collection maintained or exposed by this Ollama chat message instance for downstream processing.
    /// </summary>
    /// <value>The images value exposed by <see cref="OllamaChatMessage"/>.</value>
    [JsonPropertyName("images")]
    public List<string>? Images { get; set; }

    /// <summary>
    /// Gets or sets the tool calls collection maintained or exposed by this Ollama chat message instance for downstream processing.
    /// </summary>
    /// <value>The tool calls value exposed by <see cref="OllamaChatMessage"/>.</value>
    [JsonPropertyName("tool_calls")]
    public List<OllamaToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// Gets or sets the tool name value that forms part of the Ollama chat message state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The tool name value exposed by <see cref="OllamaChatMessage"/>.</value>
    [JsonPropertyName("tool_name")]
    public string? ToolName { get; set; }
}

/// <summary>
/// Represents an Ollama tool definition application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OllamaToolDefinition
{
    /// <summary>
    /// Gets or sets the type value that forms part of the Ollama tool definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The type value exposed by <see cref="OllamaToolDefinition"/>.</value>
    public string Type { get; set; } = "function";
    /// <summary>
    /// Gets or sets the function value that forms part of the Ollama tool definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The function value exposed by <see cref="OllamaToolDefinition"/>.</value>
    public OllamaToolFunctionDefinition Function { get; set; } = new();
}

/// <summary>
/// Represents an Ollama tool function definition application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OllamaToolFunctionDefinition
{
    /// <summary>
    /// Gets or sets the name value that forms part of the Ollama tool function definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="OllamaToolFunctionDefinition"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the Ollama tool function definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="OllamaToolFunctionDefinition"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parameters value that forms part of the Ollama tool function definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parameters value exposed by <see cref="OllamaToolFunctionDefinition"/>.</value>
    public JsonElement Parameters { get; set; }
}

/// <summary>
/// Represents an Ollama tool call application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OllamaToolCall
{
    /// <summary>
    /// Gets or sets the function value that forms part of the Ollama tool call state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The function value exposed by <see cref="OllamaToolCall"/>.</value>
    public OllamaToolFunctionCall Function { get; set; } = new();
}

/// <summary>
/// Represents an Ollama tool function call application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OllamaToolFunctionCall
{
    /// <summary>
    /// Gets or sets the name value that forms part of the Ollama tool function call state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="OllamaToolFunctionCall"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the arguments value that forms part of the Ollama tool function call state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The arguments value exposed by <see cref="OllamaToolFunctionCall"/>.</value>
    public JsonElement Arguments { get; set; }
}

/// <summary>
/// Represents the outcome of Ollama chat, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class OllamaChatResponse
{
    /// <summary>
    /// Gets or sets the message value that forms part of the Ollama chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The message value exposed by <see cref="OllamaChatResponse"/>.</value>
    public OllamaChatMessage? Message { get; set; }
}
