using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Represents a translate JSON text function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="translator">Structured text translation service dependency used by the translate JSON text function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class TranslateJsonTextFunction(
    IStructuredTextTranslationService translator,
    ILogger<TranslateJsonTextFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the translate JSON text function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="TranslateJsonTextFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.text.json.translate",
        "POST",
        "/api/dxai/functions/localgpt.text.json.translate/invoke",
        "Turns standalone JSON objects or arrays into a readable Markdown tree while preserving normalized raw JSON.",
        "JSON parameters: text required; includeRawJson optional boolean; maximumDocuments optional integer from 1 to 100.",
        "Read-only and local. It parses supplied text only, does not execute JSON, and does not persist input.",
        /// <summary>
        /// Stores the internal parameter schema JSON state used by <see cref="TranslateJsonTextFunction"/> while executing its surrounding workflow.
        /// </summary>
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {
          "type":"object",
          "required":["text"],
          "properties":{
            "text":{"type":"string","maxLength":1000000},
            "includeRawJson":{"type":"boolean"},
            "maximumDocuments":{"type":"integer","minimum":1,"maximum":100}
          },
          "additionalProperties":false
        }
        """);

    /// <summary>
    /// Performs invoke for <see cref="TranslateJsonTextFunction"/>, keeping the operation consistent with the state and invariants of the surrounding translate JSON text function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var parameters = request.Parameters.ValueKind == JsonValueKind.Object
                ? request.Parameters.Deserialize<StructuredJsonTranslationRequest>(JsonOptions) ?? new StructuredJsonTranslationRequest()
                : new StructuredJsonTranslationRequest();
            if (string.IsNullOrWhiteSpace(parameters.Text))
            {
                return Task.FromResult(new DxAiFunctionInvocationResult
                {
                    Succeeded = false,
                    Status = "InvalidParameters",
                    Error = "Parameter 'text' is required."
                });
            }

            var result = translator.TranslateJson(parameters);
            logger.LogInformation(
                "JSON translation DXFunction completed with {DocumentCount} translated document(s).",
                result.Documents.Count);
            return Task.FromResult(new DxAiFunctionInvocationResult
            {
                Succeeded = result.Succeeded,
                Status = result.Status,
                Value = result,
                Error = result.Succeeded ? string.Empty : string.Join(" ", result.Warnings)
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "JSON translation DXFunction failed; supplied text was omitted from logs.");
            return Task.FromResult(new DxAiFunctionInvocationResult
            {
                Succeeded = false,
                Status = "Failed",
                Error = "JSON translation failed. Review LocalGPT application logs."
            });
        }
    }

    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
}

/// <summary>
/// Represents an inspect JSON text function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="translator">Structured text translation service dependency used by the inspect JSON text function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class InspectJsonTextFunction(
    IStructuredTextTranslationService translator,
    ILogger<InspectJsonTextFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the inspect JSON text function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="InspectJsonTextFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.text.json.inspect",
        "POST",
        "/api/dxai/functions/localgpt.text.json.inspect/invoke",
        "Detects valid standalone JSON objects or arrays and reports their root kind, position and normalized structure.",
        "JSON parameters: text required; maximumDocuments optional integer from 1 to 100.",
        "Read-only and local. It does not execute or persist supplied JSON.",
        /// <summary>
        /// Stores the internal parameter schema JSON state used by <see cref="InspectJsonTextFunction"/> while executing its surrounding workflow.
        /// </summary>
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {
          "type":"object",
          "required":["text"],
          "properties":{
            "text":{"type":"string","maxLength":1000000},
            "maximumDocuments":{"type":"integer","minimum":1,"maximum":100}
          },
          "additionalProperties":false
        }
        """);

    /// <summary>
    /// Performs invoke for <see cref="InspectJsonTextFunction"/>, keeping the operation consistent with the state and invariants of the surrounding inspect JSON text function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            if (request.Parameters.ValueKind != JsonValueKind.Object ||
                !request.Parameters.TryGetProperty("text", out var textElement) ||
                textElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(textElement.GetString()))
            {
                return Task.FromResult(new DxAiFunctionInvocationResult
                {
                    Succeeded = false,
                    Status = "InvalidParameters",
                    Error = "Parameter 'text' is required."
                });
            }

            var maximumDocuments = request.Parameters.TryGetProperty("maximumDocuments", out var maximumElement) &&
                                   maximumElement.TryGetInt32(out var parsedMaximum)
                ? parsedMaximum
                : 20;
            var result = translator.TranslateJson(new StructuredJsonTranslationRequest
            {
                Text = textElement.GetString() ?? string.Empty,
                IncludeRawJson = false,
                MaximumDocuments = maximumDocuments
            });
            var value = new
            {
                result.Succeeded,
                result.Status,
                Documents = result.Documents.Select(document => new
                {
                    document.Index,
                    document.RootKind,
                    document.StartIndex,
                    document.Length,
                    document.NormalizedJson
                }).ToList(),
                result.Warnings
            };
            logger.LogInformation(
                "JSON inspection DXFunction completed with {DocumentCount} valid document(s).",
                result.Documents.Count);
            return Task.FromResult(new DxAiFunctionInvocationResult
            {
                Succeeded = result.Succeeded,
                Status = result.Status,
                Value = value,
                Error = result.Succeeded ? string.Empty : string.Join(" ", result.Warnings)
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "JSON inspection DXFunction failed; supplied text was omitted from logs.");
            return Task.FromResult(new DxAiFunctionInvocationResult
            {
                Succeeded = false,
                Status = "Failed",
                Error = "JSON inspection failed. Review LocalGPT application logs."
            });
        }
    }
}
