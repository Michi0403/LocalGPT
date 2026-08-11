using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Provides DevExpress ai function registry operations.
/// </summary>
public sealed class DxAiFunctionRegistry(
    IServiceProvider serviceProvider,
    IHumanCollaborationService humanCollaboration,
    IDeferredDxAiInvocationService deferredInvocations,
    IAmbientLocalGptContext ambientContext,
    IHumanApprovalExecutionContext approvalExecutionContext,
    ILocalGptVocabularyService vocabulary,
    DxAiFunctionHandlerMapService handlerMapService,
    ILogger<DxAiFunctionRegistry> logger) : IDxAiFunctionRegistry
{
    // Resolve handlers only after the scoped registry has been constructed. One handler intentionally
    // references this registry to publish the complete function directory; eager IEnumerable resolution
    // would therefore create a constructor cycle during service-provider validation.
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly Lazy<IReadOnlyDictionary<string, IDxAiFunctionHandler>> handlersByName = new(
        () => handlerMapService.Build(serviceProvider.GetServices<IDxAiFunctionHandler>()),
        System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Gets functions.
    /// </summary>
    public IReadOnlyList<DxaichatFunctionInfo> GetFunctions()
    {
    try
    {
            var functions = handlersByName.Value.Values
                .Select(handler => handler.Descriptor)
                .OrderBy(function => function.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            logger.LogDebug("Discovered {FunctionCount} DI-backed DXAIFunction handler(s).", functions.Count);
            return functions;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionRegistry)}.{nameof(GetFunctions)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionRegistry)}.{nameof(GetFunctions)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(
        string functionName,
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        ArgumentNullException.ThrowIfNull(request);
        var operationId = request.OperationId ?? Guid.NewGuid();
        request.OperationId = operationId;
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["OperationId"] = operationId,
            ["Operation"] = "InvokeDxAiFunction",
            ["FunctionName"] = functionName,
            ["RequestedBy"] = string.IsNullOrWhiteSpace(request.RequestedBy) ? "CurrentUser" : request.RequestedBy
        });

        if (!handlersByName.Value.TryGetValue(functionName, out var handler))
        {
            logger.LogWarning("Rejected unknown DXAIFunction {FunctionName}.", functionName);
            return new DxAiFunctionInvocationResult
            {
                FunctionName = functionName,
                OperationId = operationId,
                Status = "NotFound",
                Error = "No DI-backed DXAIFunction handler is registered with this name."
            };
        }

        var descriptor = handler.Descriptor;
        if (request.AutomaticInvocation &&
            (descriptor.RequiresHumanConfirmation
                ? !descriptor.SupportsDeferredApprovalRequest || !descriptor.SupportsDirectInvocation
                : !descriptor.SupportsAutomaticInvocation ||
                  (!descriptor.IsReadOnly && !descriptor.IsCoordinationOnly)))
        {
            logger.LogWarning("Rejected automatic invocation of DXAIFunction {FunctionName}; it is neither automatic-safe nor eligible for an exact deferred approval request.", functionName);
            return new DxAiFunctionInvocationResult
            {
                FunctionName = functionName,
                OperationId = operationId,
                Status = "AutomaticInvocationDenied",
                Error = "This function cannot be invoked automatically. Present its proposed action to the current user instead."
            };
        }
        if (!descriptor.SupportsDirectInvocation)
        {
            logger.LogWarning("Rejected direct invocation of DXAIFunction {FunctionName}; the descriptor is discovery-only.", functionName);
            return new DxAiFunctionInvocationResult
            {
                FunctionName = functionName,
                OperationId = operationId,
                Status = "DiscoveryOnly",
                Error = "This function is discoverable but cannot be invoked through the generic dispatcher."
            };
        }
        IDisposable? approvalScope = null;
        if (descriptor.RequiresHumanConfirmation)
        {
            var parameterFingerprint = BuildInvocationFingerprint(functionName, request);
            var correlationId = string.IsNullOrWhiteSpace(request.ConfirmationSummaryHash)
                ? $"dxai:{functionName}:{parameterFingerprint}"
                : $"dxai:{functionName}:{request.ConfirmationSummaryHash.Trim()}";
            var gate = await humanCollaboration.AuthorizeOrEnqueueAsync(
                /// <summary>
                /// Runs the human approval request spec operation.
                /// </summary>
                new HumanApprovalRequestSpec(
                    correlationId,
                    $"dxai.function.{functionName}",
                    $"Approve DXAI function: {functionName}",
                    BuildApprovalDescription(descriptor, request),
                    descriptor.IsReadOnly ? "Medium" : "High",
                    nameof(DxAiFunctionRegistry),
                    string.IsNullOrWhiteSpace(request.RequestedBy) ? ambientContext.Current.ActorDisplayName : request.RequestedBy,
                    "Function-call security reviewer",
                    ambientContext.Current.CouncilRunId,
                    ambientContext.Current.CouncilRound + 1,
                    RequiredBeforeCompletion: descriptor.ApprovalRequiredBeforeCompletion,
                    IsSensitive: !descriptor.IsReadOnly,
                    ParameterFingerprint: parameterFingerprint),
                directHumanConfirmation: request.UserConfirmed,
                cancellationToken).ConfigureAwait(false);

            if (gate.IsDeclined)
            {
                return new DxAiFunctionInvocationResult
                {
                    FunctionName = functionName,
                    OperationId = operationId,
                    Status = "HumanApprovalDeclined",
                    Error = string.IsNullOrWhiteSpace(gate.DecisionReason)
                        ? "The local human declined this exact function invocation."
                        : gate.DecisionReason,
                    Value = new { gate.RequestId, gate.CorrelationId }
                };
            }

            if (!gate.IsAuthorized)
            {
                if (request.AutomaticInvocation &&
                    descriptor.SupportsDeferredApprovalRequest &&
                    gate.RequestId is Guid pendingApprovalRequestId)
                {
                    await deferredInvocations.QueueAsync(
                        functionName,
                        request,
                        pendingApprovalRequestId,
                        gate.CorrelationId,
                        ambientContext.Current.CouncilRunId,
                        cancellationToken).ConfigureAwait(false);
                }

                logger.LogInformation("DXAIFunction {FunctionName} is queued for non-blocking human review as request {RequestId}.", functionName, gate.RequestId);
                return new DxAiFunctionInvocationResult
                {
                    FunctionName = functionName,
                    OperationId = operationId,
                    Status = "HumanApprovalPending",
                    Error = "This exact function invocation is waiting in the Human Collaboration Inbox. Other council work may continue; an approved deferred invocation can run immediately from the Human Collaboration Inbox or on a council heartbeat.",
                    Value = new
                    {
                        gate.RequestId,
                        gate.CorrelationId,
                        RetryAfterApproval = true,
                        DeferredExecutionAvailable = request.AutomaticInvocation && descriptor.SupportsDeferredApprovalRequest
                    }
                };
            }

            request.UserConfirmed = true;
            if (gate.RequestId is Guid approvalRequestId && !ambientContext.Current.HasHumanApproval(vocabulary.Get()))
            {
                var profile = await humanCollaboration.GetProfileAsync(cancellationToken).ConfigureAwait(false);
                approvalScope = approvalExecutionContext.PushHumanApproval(
                    profile.Id,
                    profile.DisplayName,
                    approvalRequestId,
                    $"DXAI function {functionName}",
                    gate.CorrelationId,
                    ambientContext.Current.CouncilRunId,
                    ambientContext.Current.CouncilRound,
                    ambientContext.Current.Phase);
            }
        }

        try
        {
            var result = await handler.InvokeAsync(request, cancellationToken).ConfigureAwait(false);
            result.FunctionName = functionName;
            result.OperationId = operationId;
            logger.LogInformation(
                "DXAIFunction {FunctionName} completed with status {FunctionStatus} and success={Succeeded}.",
                functionName,
                result.Status,
                result.Succeeded);
            return result;
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "DXAIFunction {FunctionName} was cancelled.", functionName);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "DXAIFunction {FunctionName} received invalid JSON parameters; parameter content was omitted from logs.", functionName);
            return new DxAiFunctionInvocationResult
            {
                FunctionName = functionName,
                OperationId = operationId,
                Status = "InvalidParameters",
                Error = "The function parameters could not be parsed."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DXAIFunction {FunctionName} failed; request and generated payload content were omitted from logs.", functionName);
            return new DxAiFunctionInvocationResult
            {
                FunctionName = functionName,
                OperationId = operationId,
                Status = "Failed",
                Error = "The function failed. Review LocalGPT application logs using the operation ID."
            };
        }
        finally
        {
            approvalScope?.Dispose();
        }
    }

    /// <summary>
    /// Builds approval description.
    /// </summary>
    private string BuildApprovalDescription(DxaichatFunctionInfo descriptor, DxAiFunctionInvocationRequest request)
    {
    try
    {
            var builder = new StringBuilder()
                .Append(descriptor.Purpose)
                .Append(' ')
                .Append(descriptor.SafetyNotes)
                .AppendLine()
                .Append("Exact request summary: ");

            if (request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                builder.Append("no parameters");
            else if (request.Parameters.ValueKind == JsonValueKind.Object)
            {
                var parts = new List<string>();
                foreach (var property in request.Parameters.EnumerateObject().Take(24))
                    parts.Add($"{property.Name}={SummarizeApprovalValue(property.Name, property.Value)}");
                builder.Append(string.Join("; ", parts));
            }
            else
            {
                builder.Append(SummarizeApprovalValue("parameters", request.Parameters));
            }

            var text = builder.ToString();
            return text.Length <= 1900 ? text : text[..1900] + "...";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionRegistry)}.{nameof(BuildApprovalDescription)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionRegistry)}.{nameof(BuildApprovalDescription)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the summarize approval value operation.
    /// </summary>
    private string SummarizeApprovalValue(string name, JsonElement value)
    {
    try
    {
            var sensitiveName = name.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("key", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("connectionstring", StringComparison.OrdinalIgnoreCase);
            if (sensitiveName)
                return "<redacted sensitive value>";

            return value.ValueKind switch
            {
                JsonValueKind.String => QuoteAndTrim(value.GetString(), 180),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.GetRawText(),
                JsonValueKind.Null => "null",
                JsonValueKind.Array => $"array[{value.GetArrayLength()}]",
                JsonValueKind.Object when name.Equals("values", StringComparison.OrdinalIgnoreCase) =>
                    "{" + string.Join(", ", value.EnumerateObject().Take(24).Select(item =>
                        $"{item.Name}:{(item.Name.Contains("secret", StringComparison.OrdinalIgnoreCase) || item.Name.Contains("password", StringComparison.OrdinalIgnoreCase) || item.Name.Contains("token", StringComparison.OrdinalIgnoreCase) ? "<redacted>" : SummarizeApprovalValue(item.Name, item.Value))}")) + "}",
                JsonValueKind.Object => $"object[{value.EnumerateObject().Count()}]",
                _ => value.ValueKind.ToString()
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionRegistry)}.{nameof(SummarizeApprovalValue)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionRegistry)}.{nameof(SummarizeApprovalValue)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the quote and trim operation.
    /// </summary>
    private string QuoteAndTrim(string? value, int maxLength)
    {
    try
    {
            var normalized = (value ?? string.Empty)
                .Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal)
                .Trim();
            if (normalized.Length > maxLength)
                normalized = normalized[..maxLength] + "...";
            return $"\"{normalized}\"";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionRegistry)}.{nameof(QuoteAndTrim)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionRegistry)}.{nameof(QuoteAndTrim)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds invocation fingerprint.
    /// </summary>
    private string BuildInvocationFingerprint(string functionName, DxAiFunctionInvocationRequest request)
    {
    try
    {
            var canonical = new StringBuilder()
                .Append(functionName).Append('|')
                .Append(request.Parameters.ValueKind == JsonValueKind.Undefined ? "{}" : request.Parameters.GetRawText()).Append('|')
                .Append(request.ProjectId).Append('|')
                .Append(request.ProjectVersionId).Append('|')
                .Append(request.ConversationId)
                .ToString();
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionRegistry)}.{nameof(BuildInvocationFingerprint)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionRegistry)}.{nameof(BuildInvocationFingerprint)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents a list code generation reviews function.
/// </summary>
public sealed class ListCodeGenerationReviewsFunction(
    IDxAiFunctionJsonService json,
    ICodeGenerationWorkflowService workflow,
    ILogger<ListCodeGenerationReviewsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "codegen.review.list",
        "POST",
        "/api/dxai/functions/codegen.review.list/invoke",
        "List recent user-controlled code-generation change reviews, optionally filtered by LocalGPT project.",
        "JSON parameters: projectId optional GUID; take optional positive integer. No artificial review-list ceiling is imposed by the workflow service.",
        "Read-only database metadata. Source payload content is represented by paths, sizes, and hashes rather than returned as executable authority.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {
          "type": "object",
          "properties": {
            "projectId": {
              "type": "string",
              "format": "uuid",
              "description": "Optional LocalGPT project GUID."
            },
            "take": {
              "type": "integer",
              "minimum": 1
            }
          },
          "additionalProperties": false
        }
        """);

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<CodeGenerationReviewListParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            var reviews = await workflow.ListReviewsAsync(parameters.ProjectId, parameters.Take, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("DXAIFunction listed {ReviewCount} change review(s).", reviews.Count);
            return json.Success(reviews);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ListCodeGenerationReviewsFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ListCodeGenerationReviewsFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}


}

/// <summary>
/// Represents a get code generation review function.
/// </summary>
public sealed class GetCodeGenerationReviewFunction(
    IDxAiFunctionJsonService json,
    ICodeGenerationWorkflowService workflow,
    ILogger<GetCodeGenerationReviewFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "codegen.review.get",
        "POST",
        "/api/dxai/functions/codegen.review.get/invoke",
        "Read one code-generation change review before presenting its heartbeat/decision summary to the user.",
        "JSON parameters: reviewId required GUID.",
        "Read-only. The returned review hash binds the exact reviewed payload and must be echoed by a later explicit user decision.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {
          "type": "object",
          "properties": {
            "reviewId": {
              "type": "string",
              "format": "uuid"
            }
          },
          "required": [
            "reviewId"
          ],
          "additionalProperties": false
        }
        """);

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<CodeGenerationReviewGetParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            var review = await workflow.GetReviewAsync(parameters.ReviewId, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("DXAIFunction loaded review {ReviewId}; found={Found}.", parameters.ReviewId, review is not null);
            return new DxAiFunctionInvocationResult
            {
                Succeeded = review is not null,
                Status = review is null ? "NotFound" : "Completed",
                Value = review,
                Error = review is null ? "The review was not found." : null
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GetCodeGenerationReviewFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GetCodeGenerationReviewFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

}

/// <summary>
/// Represents a create code generation review function.
/// </summary>
public sealed class CreateCodeGenerationReviewFunction(
    IDxAiFunctionJsonService json,
    ICodeGenerationWorkflowService workflow,
    ILogger<CreateCodeGenerationReviewFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "codegen.review.create",
        "POST",
        "/api/dxai/functions/codegen.review.create/invoke",
        "Create a database-backed change review containing the exact proposed files, CodeDOM types, output targets, current project-state summary, council summary, safety summary, and immutable review hash.",
        "JSON parameters follow CreateCodeGenerationReviewRequest. goal is required. For exact generation provide files with relativePath/content and one or more outputs; for existing-project maintenance also provide projectId plus the approved projectRevisionId. currentProjectState, councilSummary, changeSummary, safetySummary, projectTopicId, councilRunId, and codeDomTypes are optional context. Do not invent a nested summaries object. Output kinds include SourceFiles, ClassLibrary, ConsoleApplication, Solution, LocalGptAddon, CSharpScript, and JavaScriptModule.",
        "Coordination-only review metadata. It does not write a project workspace, build, execute, load, or integrate generated code. The actual codegen.review.execute step remains separately approval-gated.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        IsCoordinationOnly: true,
        ParameterSchemaJson: """
        {
          "type": "object",
          "description": "CreateCodeGenerationReviewRequest with exact reviewed files, CodeDOM types, and output targets.",
          "properties": {
            "projectId": {
              "type": [
                "string",
                "null"
              ],
              "format": "uuid"
            },
            "projectRevisionId": {
              "type": [
                "string",
                "null"
              ],
              "format": "uuid",
              "description": "Optional approved/scanned project revision whose user-approved tracked files are cloned before reviewed changes are applied."
            },
            "projectTopicId": {
              "type": [
                "string",
                "null"
              ],
              "format": "uuid"
            },
            "councilRunId": {
              "type": [
                "string",
                "null"
              ],
              "format": "uuid"
            },
            "title": {
              "type": "string"
            },
            "goal": {
              "type": "string"
            },
            "currentProjectState": {
              "type": "string"
            },
            "councilSummary": {
              "type": "string"
            },
            "changeSummary": {
              "type": "string"
            },
            "safetySummary": {
              "type": "string"
            },
            "files": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "relativePath": {
                    "type": "string"
                  },
                  "content": {
                    "type": "string"
                  },
                  "purpose": {
                    "type": "string"
                  }
                },
                "required": [
                  "relativePath",
                  "content"
                ]
              }
            },
            "codeDomTypes": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "relativePath": {
                    "type": "string"
                  },
                  "namespace": {
                    "type": "string"
                  },
                  "typeName": {
                    "type": "string"
                  },
                  "methodName": {
                    "type": "string"
                  },
                  "methodResult": {
                    "type": "string"
                  },
                  "summary": {
                    "type": "string"
                  }
                }
              }
            },
            "outputs": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "kind": {
                    "type": "string",
                    "enum": [
                      "SourceFiles",
                      "ClassLibrary",
                      "ConsoleApplication",
                      "Solution",
                      "LocalGptAddon",
                      "CSharpScript",
                      "JavaScriptModule"
                    ]
                  },
                  "name": {
                    "type": "string"
                  },
                  "relativeDirectory": {
                    "type": "string"
                  },
                  "targetFramework": {
                    "type": "string"
                  },
                  "rootNamespace": {
                    "type": "string"
                  },
                  "description": {
                    "type": "string"
                  }
                }
              }
            }
          },
          "required": [
            "goal"
          ],
          "additionalProperties": false
        }
        """);

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<CreateCodeGenerationReviewRequest>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            var review = await workflow.CreateReviewAsync(parameters, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("DXAIFunction created review {ReviewId} with hash prefix {HashPrefix}.", review.Id, review.ReviewHash[..Math.Min(12, review.ReviewHash.Length)]);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = review.Status, Value = review };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CreateCodeGenerationReviewFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CreateCodeGenerationReviewFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents an execute code generation review function.
/// </summary>
public sealed class ExecuteCodeGenerationReviewFunction(
    IDxAiFunctionJsonService json,
    ICodeGenerationWorkflowService workflow,
    ILogger<ExecuteCodeGenerationReviewFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "codegen.review.execute",
        "POST",
        "/api/dxai/functions/codegen.review.execute/invoke",
        "Write and optionally build the exact source/addon/solution payload previously shown in a code-generation change review.",
        "JSON parameters: reviewId plus ExecuteCodeGenerationReviewRequest fields expectedReviewHash, userConfirmed, buildAfterGeneration, userConfirmedBuild, and decisionNote.",
        "One-use approval. The exact review hash and fresh human confirmation are mandatory. Files are restricted to a LocalGPT artifact workspace. Scripts and generated programs are never executed or loaded automatically.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {
          "type": "object",
          "properties": {
            "reviewId": {
              "type": "string",
              "format": "uuid"
            },
            "request": {
              "type": "object",
              "properties": {
                "expectedReviewHash": {
                  "type": "string"
                },
                "userConfirmed": {
                  "type": "boolean"
                },
                "buildAfterGeneration": {
                  "type": "boolean"
                },
                "userConfirmedBuild": {
                  "type": "boolean"
                },
                "decisionNote": {
                  "type": "string"
                }
              },
              "required": [
                "expectedReviewHash"
              ]
            }
          },
          "required": [
            "reviewId",
            "request"
          ],
          "additionalProperties": false
        }
        """,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: true);

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<CodeGenerationReviewExecuteParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            parameters.Request.UserConfirmed = request.UserConfirmed;
            if (request.UserConfirmed && parameters.Request.BuildAfterGeneration)
                parameters.Request.UserConfirmedBuild = true;
            if (string.IsNullOrWhiteSpace(parameters.Request.ExpectedReviewHash))
                parameters.Request.ExpectedReviewHash = request.ConfirmationSummaryHash ?? string.Empty;
            var result = await workflow.ExecuteReviewAsync(parameters.ReviewId, parameters.Request, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("DXAIFunction executed review {ReviewId} with status {Status}.", parameters.ReviewId, result.Status);
            return new DxAiFunctionInvocationResult { Succeeded = result.Status is CodeGenerationReviewStatuses.Generated or CodeGenerationReviewStatuses.BuildPassed, Status = result.Status, Value = result };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ExecuteCodeGenerationReviewFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ExecuteCodeGenerationReviewFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

}

/// <summary>
/// Represents a reject code generation review function.
/// </summary>
public sealed class RejectCodeGenerationReviewFunction(
    IDxAiFunctionJsonService json,
    ICodeGenerationWorkflowService workflow,
    ILogger<RejectCodeGenerationReviewFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "codegen.review.reject",
        "POST",
        "/api/dxai/functions/codegen.review.reject/invoke",
        "Reject a pending code-generation change review without writing or building its payload.",
        "JSON parameters: reviewId plus RejectCodeGenerationReviewRequest fields expectedReviewHash, userConfirmed, and decisionNote.",
        "Requires fresh human confirmation and the exact review hash. Rejection does not delete project files or private knowledge.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {
          "type": "object",
          "properties": {
            "reviewId": {
              "type": "string",
              "format": "uuid"
            },
            "request": {
              "type": "object",
              "properties": {
                "expectedReviewHash": {
                  "type": "string"
                },
                "userConfirmed": {
                  "type": "boolean"
                },
                "decisionNote": {
                  "type": "string"
                }
              },
              "required": [
                "expectedReviewHash"
              ]
            }
          },
          "required": [
            "reviewId",
            "request"
          ],
          "additionalProperties": false
        }
        """,
        SupportsDeferredApprovalRequest: true);

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<CodeGenerationReviewRejectParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            parameters.Request.UserConfirmed = request.UserConfirmed;
            if (string.IsNullOrWhiteSpace(parameters.Request.ExpectedReviewHash))
                parameters.Request.ExpectedReviewHash = request.ConfirmationSummaryHash ?? string.Empty;
            var review = await workflow.RejectReviewAsync(parameters.ReviewId, parameters.Request, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("DXAIFunction rejected review {ReviewId}.", parameters.ReviewId);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = review.Status, Value = review };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RejectCodeGenerationReviewFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RejectCodeGenerationReviewFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

}

/// <summary>
/// Represents a list local gpt projects function.
/// </summary>
public sealed class ListLocalGptProjectsFunction(
    ILocalGptProjectService projects,
    ILogger<ListLocalGptProjectsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.projects.list",
        "POST",
        "/api/dxai/functions/localgpt.projects.list/invoke",
        "List LocalGPT project records and their version/topic counts for current project-state awareness.",
        "JSON parameters: includeArchived optional boolean.",
        "Read-only database metadata. Recorded paths are descriptive context and never authorize filesystem access.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {
          "type": "object",
          "properties": {
            "includeArchived": {
              "type": "boolean"
            }
          },
          "additionalProperties": false
        }
        """);

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var parameters = request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? new LocalGptProjectListParameters()
                : request.Parameters.Deserialize<LocalGptProjectListParameters>(JsonOptions) ?? new LocalGptProjectListParameters();
            var values = await projects.GetProjectsAsync(parameters.IncludeArchived, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("DXAIFunction listed {ProjectCount} LocalGPT project record(s).", values.Count);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = values };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ListLocalGptProjectsFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ListLocalGptProjectsFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
}

/// <summary>
/// Represents a get local gpt project function.
/// </summary>
public sealed class GetLocalGptProjectFunction(
    IDxAiFunctionJsonService json,
    ILocalGptProjectService projects,
    ILogger<GetLocalGptProjectFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.project.get",
        "POST",
        "/api/dxai/functions/localgpt.project.get/invoke",
        "Read one LocalGPT project with its approved topics and version history before a council change review is prepared.",
        "JSON parameters: projectId required GUID.",
        "Read-only metadata. The stored project path is not accessed and supplies no write, build, Git, or execution authority.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {
          "type": "object",
          "properties": {
            "projectId": {
              "type": "string",
              "format": "uuid"
            }
          },
          "required": [
            "projectId"
          ],
          "additionalProperties": false
        }
        """);

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<LocalGptProjectGetParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            var value = await projects.GetProjectAsync(parameters.ProjectId, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("DXAIFunction loaded LocalGPT project {ProjectId}; found={Found}.", parameters.ProjectId, value is not null);
            return new DxAiFunctionInvocationResult
            {
                Succeeded = value is not null,
                Status = value is null ? "NotFound" : "Completed",
                Value = value,
                Error = value is null ? "The LocalGPT project was not found." : null
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GetLocalGptProjectFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GetLocalGptProjectFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

}

/// <summary>
/// Represents a list recent application logs function.
/// </summary>
public sealed class ListRecentApplicationLogsFunction(
    IApplicationLogReaderService applicationLogs,
    ILogger<ListRecentApplicationLogsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.logs.recent",
        "POST",
        "/api/dxai/functions/localgpt.logs.recent/invoke",
        "Read a bounded set of recent LocalGPT operational log summaries for live troubleshooting memory.",
        "JSON parameters: minimumLevel optional Trace/Debug/Information/Warning/Error/Critical; take optional integer 1 to 50.",
        "Read-only and bounded. Exception bodies are omitted from function results; prompts, model output, generated source, and secrets must not be logged.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {
          "type": "object",
          "properties": {
            "minimumLevel": {
              "type": "string",
              "enum": [
                "Trace",
                "Debug",
                "Information",
                "Warning",
                "Error",
                "Critical"
              ]
            },
            "take": {
              "type": "integer",
              "minimum": 1,
              "maximum": 50
            }
          },
          "additionalProperties": false
        }
        """);

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var parameters = request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? new RecentApplicationLogListParameters()
                : request.Parameters.Deserialize<RecentApplicationLogListParameters>(JsonOptions) ?? new RecentApplicationLogListParameters();
            var level = Enum.TryParse<LogLevel>(parameters.MinimumLevel, true, out var parsed) ? parsed : LogLevel.Warning;
            var entries = await applicationLogs.GetRecentAsync(level, Math.Clamp(parameters.Take, 1, 50), cancellationToken).ConfigureAwait(false);
            var safeEntries = entries.Select(entry => new
            {
                entry.Id,
                entry.TimestampUtc,
                entry.Level,
                entry.Category,
                entry.EventId,
                entry.EventName,
                Message = Limit(entry.Message, 1200),
                HasTechnicalException = !string.IsNullOrWhiteSpace(entry.Exception)
            }).ToList();
            logger.LogInformation("DXAIFunction returned {LogCount} recent application log summary row(s) at minimum level {MinimumLevel}.", safeEntries.Count, level);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = safeEntries };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ListRecentApplicationLogsFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ListRecentApplicationLogsFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    /// <summary>
    /// Runs the limit operation.
    /// </summary>
    private string Limit(string value, int max) {
    try
    {
        return value.Length <= max ? value : value[..max] + "...";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ListRecentApplicationLogsFunction)}.{nameof(Limit)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ListRecentApplicationLogsFunction)}.{nameof(Limit)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents a list council knowledge function.
/// </summary>
public sealed class ListCouncilKnowledgeFunction(
    ICouncilKnowledgeService knowledge,
    ILogger<ListCouncilKnowledgeFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.knowledge.list",
        "POST",
        "/api/dxai/functions/localgpt.knowledge.list/invoke",
        "List bounded, approved Council knowledge summaries for source-backed project and architecture context.",
        "JSON parameters: includeArchived optional boolean; take optional integer 1 to 30.",
        "Read-only. Knowledge is context, not authority. Results include bounded excerpts and provenance/approval metadata.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {
          "type": "object",
          "properties": {
            "includeArchived": {
              "type": "boolean"
            },
            "take": {
              "type": "integer",
              "minimum": 1,
              "maximum": 30
            }
          },
          "additionalProperties": false
        }
        """);

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var parameters = request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? new CouncilKnowledgeListParameters()
                : request.Parameters.Deserialize<CouncilKnowledgeListParameters>(JsonOptions) ?? new CouncilKnowledgeListParameters();
            var entries = await knowledge.GetEntriesAsync(parameters.IncludeArchived, Math.Clamp(parameters.Take, 1, 30), cancellationToken).ConfigureAwait(false);
            var summaries = entries.Select(entry => new
            {
                entry.Id,
                entry.Topic,
                entry.Scope,
                ContentExcerpt = Limit(entry.Content, 1200),
                entry.Source,
                entry.Tags,
                entry.Confidence,
                entry.VerificationStatus,
                entry.ReviewStatus,
                entry.IsUserApproved,
                entry.IsArchived,
                entry.UpdatedAtUtc
            }).ToList();
            logger.LogInformation("DXAIFunction listed {KnowledgeCount} bounded Council knowledge summary row(s).", summaries.Count);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = summaries };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ListCouncilKnowledgeFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ListCouncilKnowledgeFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    /// <summary>
    /// Runs the limit operation.
    /// </summary>
    private string Limit(string value, int max) {
    try
    {
        return value.Length <= max ? value : value[..max] + "...";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ListCouncilKnowledgeFunction)}.{nameof(Limit)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ListCouncilKnowledgeFunction)}.{nameof(Limit)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents a list chat memory conversations function.
/// </summary>
public sealed class ListChatMemoryConversationsFunction(
    IChatMemoryService memory,
    ILogger<ListChatMemoryConversationsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.memory.conversations",
        "POST",
        "/api/dxai/functions/localgpt.memory.conversations/invoke",
        "List recent LocalGPT conversation metadata so the user and model can select an existing cooperation thread.",
        "JSON parameters: take optional integer 1 to 50.",
        "Read-only metadata only. Message bodies and hidden reasoning are not returned by this automatic function.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {
          "type": "object",
          "properties": {
            "take": {
              "type": "integer",
              "minimum": 1,
              "maximum": 50
            }
          },
          "additionalProperties": false
        }
        """);

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var parameters = request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? new ChatMemoryConversationListParameters()
                : request.Parameters.Deserialize<ChatMemoryConversationListParameters>(JsonOptions) ?? new ChatMemoryConversationListParameters();
            var entries = await memory.GetConversationsAsync(Math.Clamp(parameters.Take, 1, 50), cancellationToken).ConfigureAwait(false);
            logger.LogInformation("DXAIFunction listed {ConversationCount} chat-memory conversation summary row(s).", entries.Count);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = entries };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ListChatMemoryConversationsFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ListChatMemoryConversationsFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
}


/// <summary>
/// Represents a request human collaboration function.
/// </summary>
public sealed class RequestHumanCollaborationFunction(ILocalGptVocabularyService vocabulary,

    IHumanCollaborationService collaboration,
    IAmbientLocalGptContext ambientContext,
    ILogger<RequestHumanCollaborationFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "human.collaboration.request",
        "POST",
        "/api/dxai/functions/human.collaboration.request/invoke",
        "Ask the local human participant for bounded feedback or guidance, with an explicit Council scope and execution gate.",
        "JSON parameters: kind Feedback or Guidance; title and description required. questionScope is Member, SelectedMembers, or Consensus; use Consensus only when all participating members explicitly agreed on the same question. gate is None, NextPhase, NextRound, or Completion. targetMembers identifies affected models. Use a blocking gate only when that boundary genuinely cannot be crossed without the answer.",
        "Coordination-only. This function may create a persistent inbox question and pause only its declared Council boundary. It cannot approve operations, create trusted human identity, or authorize tools and side effects.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {
          "type": "object",
          "properties": {
            "kind": { "type": "string", "enum": ["Feedback", "Guidance"] },
            "title": { "type": "string", "maxLength": 240 },
            "description": { "type": "string", "maxLength": 2000 },
            "requestedRole": { "type": "string", "maxLength": 160 },
            "responsePrompt": { "type": "string", "maxLength": 500 },
            "suggestedResponses": {
              "type": "array",
              "items": { "type": "string", "maxLength": 200 },
              "maxItems": 8
            },
            "prefillText": { "type": "string", "maxLength": 2000 },
            "allowFreeText": { "type": "boolean" },
            "questionScope": { "type": "string", "enum": ["Member", "SelectedMembers", "Consensus"] },
            "gate": { "type": "string", "enum": ["None", "NextPhase", "NextRound", "Completion"] },
            "targetMembers": {
              "type": "array",
              "items": { "type": "string", "maxLength": 160 },
              "maxItems": 16
            },
            "requiredBeforeCompletion": { "type": "boolean", "description": "Backward-compatible alias for gate=Completion." }
          },
          "required": ["title", "description"],
          "additionalProperties": false
        }
        """,
        IsCoordinationOnly: true);

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
    try
    {
            var parameters = request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? new HumanCollaborationRequestParameters()
                : request.Parameters.Deserialize<HumanCollaborationRequestParameters>(JsonOptions) ?? new HumanCollaborationRequestParameters();
            if (string.IsNullOrWhiteSpace(parameters.Title) || string.IsNullOrWhiteSpace(parameters.Description))
                throw new JsonException("title and description are required.");

            var kind = string.Equals(parameters.Kind, vocabulary.Get().HumanRequestGuidance, StringComparison.OrdinalIgnoreCase)
                ? vocabulary.Get().HumanRequestGuidance
                : vocabulary.Get().HumanRequestFeedback;
            var ambient = ambientContext.Current;
            var questionScope = NormalizeQuestionScope(parameters.QuestionScope);
            var gateMode = NormalizeGateMode(parameters.Gate, parameters.RequiredBeforeCompletion);
            var targetMembers = (parameters.TargetMembers ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(16)
                .ToArray();
            if (questionScope == "SelectedMembers" && targetMembers.Length == 0)
                questionScope = "Member";
            if (questionScope == "Member" && targetMembers.Length == 0 && !string.IsNullOrWhiteSpace(ambient.ActorDisplayName))
                targetMembers = [ambient.ActorDisplayName];
            var suggestions = (parameters.SuggestedResponses ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToArray();
            var earliestCouncilRound = gateMode == "NextRound"
                ? Math.Max(0, ambient.CouncilRound + 1)
                : Math.Max(0, ambient.CouncilRound);
            var fingerprintSource = JsonSerializer.Serialize(new
            {
                kind,
                parameters.Title,
                parameters.Description,
                parameters.RequestedRole,
                parameters.ResponsePrompt,
                Suggestions = suggestions,
                parameters.PrefillText,
                parameters.AllowFreeText,
                questionScope,
                gateMode,
                TargetMembers = targetMembers,
                parameters.RequiredBeforeCompletion,
                ambient.CouncilRunId,
                ambient.CouncilRound,
                ambient.Phase
            }, JsonOptions);
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintSource))).ToLowerInvariant();
            var gate = await collaboration.AuthorizeOrEnqueueAsync(
                /// <summary>
                /// Runs the human approval request spec operation.
                /// </summary>
                new HumanApprovalRequestSpec(
                    $"human-question:{ambient.CouncilRunId?.ToString("N") ?? "general"}:{fingerprint}",
                    "human.collaboration.request",
                    parameters.Title,
                    parameters.Description,
                    "Low",
                    nameof(RequestHumanCollaborationFunction),
                    ambient.ActorDisplayName,
                    string.IsNullOrWhiteSpace(parameters.RequestedRole) ? "Human collaborator" : parameters.RequestedRole,
                    ambient.CouncilRunId,
                    earliestCouncilRound,
                    parameters.RequiredBeforeCompletion,
                    IsSensitive: false,
                    RequestKind: kind,
                    SuggestedResponsesText: string.Join('\n', suggestions),
                    ResponsePrompt: parameters.ResponsePrompt ?? string.Empty,
                    PrefillText: parameters.PrefillText ?? string.Empty,
                    AllowFreeText: parameters.AllowFreeText,
                    ParameterFingerprint: fingerprint,
                    QuestionScope: questionScope,
                    GateMode: gateMode,
                    TargetMembersText: string.Join('\n', targetMembers),
                    RequestedCouncilRound: Math.Max(0, ambient.CouncilRound),
                    RequestedCouncilPhase: ambient.Phase),
                directHumanConfirmation: false,
                cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "DXAIFunction queued human {RequestKind} request {RequestId} for council run {CouncilRunId}; question content was omitted from logs.",
                kind,
                gate.RequestId,
                ambient.CouncilRunId);
            return new DxAiFunctionInvocationResult
            {
                Succeeded = gate.RequestId is not null,
                Status = gate.Status,
                Value = new
                {
                    gate.RequestId,
                    gate.CorrelationId,
                    RequestKind = kind,
                    QuestionScope = questionScope,
                    Gate = gateMode,
                    TargetMembers = targetMembers,
                    EntersNextHeartbeat = ambient.CouncilRunId is not null,
                    BlocksNextPhase = gateMode == "NextPhase",
                    BlocksNextRound = gateMode == "NextRound",
                    BlocksCompletion = gateMode == "Completion",
                    BlocksUnrelatedWork = false
                },
                Error = gate.RequestId is null ? gate.Message : null
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RequestHumanCollaborationFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RequestHumanCollaborationFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}


    /// <summary>
    /// Normalizes question scope.
    /// </summary>
    private string NormalizeQuestionScope(string? value)
    {
    try
    {
            if (string.Equals(value, "Consensus", StringComparison.OrdinalIgnoreCase))
                return "Consensus";
            if (string.Equals(value, "SelectedMembers", StringComparison.OrdinalIgnoreCase))
                return "SelectedMembers";
            return "Member";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RequestHumanCollaborationFunction)}.{nameof(NormalizeQuestionScope)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RequestHumanCollaborationFunction)}.{nameof(NormalizeQuestionScope)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes gate mode.
    /// </summary>
    private string NormalizeGateMode(string? value, bool requiredBeforeCompletion)
    {
    try
    {
            if (string.Equals(value, "NextPhase", StringComparison.OrdinalIgnoreCase))
                return "NextPhase";
            if (string.Equals(value, "NextRound", StringComparison.OrdinalIgnoreCase))
                return "NextRound";
            if (string.Equals(value, "Completion", StringComparison.OrdinalIgnoreCase) || requiredBeforeCompletion)
                return "Completion";
            return "None";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RequestHumanCollaborationFunction)}.{nameof(NormalizeGateMode)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RequestHumanCollaborationFunction)}.{nameof(NormalizeGateMode)} failed.");
        throw;
    }
}
}
