using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Services;

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
    private readonly Lazy<IReadOnlyDictionary<string, IDxAiFunctionHandler>> handlersByName = new(
        () => handlerMapService.Build(serviceProvider.GetServices<IDxAiFunctionHandler>()),
        System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public IReadOnlyList<DxaichatFunctionInfo> GetFunctions()
    {
        var functions = handlersByName.Value.Values
            .Select(handler => handler.Descriptor)
            .OrderBy(function => function.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        logger.LogDebug("Discovered {FunctionCount} DI-backed DXAIFunction handler(s).", functions.Count);
        return functions;
    }

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
                    Error = "This exact function invocation is waiting in the Human Collaboration Inbox. Other council work may continue; an approved deferred invocation can run on the next council heartbeat.",
                    Value = new
                    {
                        gate.RequestId,
                        gate.CorrelationId,
                        RetryAfterApproval = true,
                        DeferredToCouncilHeartbeat = request.AutomaticInvocation && descriptor.SupportsDeferredApprovalRequest
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
#if DEBUG
            logger.LogInformation(exception, "DXAIFunction {FunctionName} was cancelled while debugging.", functionName);
#endif
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

    private string BuildApprovalDescription(DxaichatFunctionInfo descriptor, DxAiFunctionInvocationRequest request)
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

    private string SummarizeApprovalValue(string name, JsonElement value)
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

    private string QuoteAndTrim(string? value, int maxLength)
    {
        var normalized = (value ?? string.Empty)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
        if (normalized.Length > maxLength)
            normalized = normalized[..maxLength] + "...";
        return $"\"{normalized}\"";
    }

    private string BuildInvocationFingerprint(string functionName, DxAiFunctionInvocationRequest request)
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
}

public sealed class ListCodeGenerationReviewsFunction(
    ICodeGenerationWorkflowService workflow,
    ILogger<ListCodeGenerationReviewsFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "codegen.review.list",
        "POST",
        "/api/dxai/functions/codegen.review.list/invoke",
        "List recent user-controlled code-generation change reviews, optionally filtered by LocalGPT project.",
        "JSON parameters: projectId optional GUID; take optional integer from 1 to 100.",
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
              "minimum": 1,
              "maximum": 100
            }
          },
          "additionalProperties": false
        }
        """);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = Deserialize<ListParameters>(request.Parameters);
        var reviews = await workflow.ListReviewsAsync(parameters.ProjectId, parameters.Take, cancellationToken).ConfigureAwait(false);
        logger.LogDebug("DXAIFunction listed {ReviewCount} change review(s).", reviews.Count);
        return Success(reviews);
    }

    private sealed class ListParameters
    {
        public Guid? ProjectId { get; set; }
        public int Take { get; set; } = 20;
    }

    private T Deserialize<T>(JsonElement element) where T : new() =>
        element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? new T()
            : element.Deserialize<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true }) ?? new T();

    private DxAiFunctionInvocationResult Success(object value) => new() { Succeeded = true, Status = "Completed", Value = value };
}

public sealed class GetCodeGenerationReviewFunction(
    ICodeGenerationWorkflowService workflow,
    ILogger<GetCodeGenerationReviewFunction> logger) : IDxAiFunctionHandler
{
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

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = request.Parameters.Deserialize<GetParameters>(new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true })
            ?? throw new JsonException("reviewId is required.");
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

    private sealed class GetParameters
    {
        public Guid ReviewId { get; set; }
    }
}

public sealed class CreateCodeGenerationReviewFunction(
    ICodeGenerationWorkflowService workflow,
    ILogger<CreateCodeGenerationReviewFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "codegen.review.create",
        "POST",
        "/api/dxai/functions/codegen.review.create/invoke",
        "Create a database-backed change review containing the exact proposed files, CodeDOM types, output targets, current project-state summary, council summary, safety summary, and immutable review hash.",
        "JSON parameters follow CreateCodeGenerationReviewRequest. Include goal, summaries, files, CodeDomTypes, and output targets such as SourceFiles, ClassLibrary, ConsoleApplication, Solution, LocalGptAddon, CSharpScript, or JavaScriptModule.",
        "Creates review metadata only. It does not write a project workspace, build, execute, load, or integrate generated code. The current user must explicitly request this review creation.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false,
        Source: "DIHandler",
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
        """,
        SupportsDeferredApprovalRequest: true);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = request.Parameters.Deserialize<CreateCodeGenerationReviewRequest>(new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true })
            ?? throw new JsonException("A code-generation review request is required.");
        var review = await workflow.CreateReviewAsync(parameters, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("DXAIFunction created review {ReviewId} with hash prefix {HashPrefix}.", review.Id, review.ReviewHash[..Math.Min(12, review.ReviewHash.Length)]);
        return new DxAiFunctionInvocationResult { Succeeded = true, Status = review.Status, Value = review };
    }
}

public sealed class ExecuteCodeGenerationReviewFunction(
    ICodeGenerationWorkflowService workflow,
    ILogger<ExecuteCodeGenerationReviewFunction> logger) : IDxAiFunctionHandler
{
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

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = request.Parameters.Deserialize<ExecuteParameters>(new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true })
            ?? throw new JsonException("A review execution request is required.");
        parameters.Request.UserConfirmed = request.UserConfirmed;
        if (request.UserConfirmed && parameters.Request.BuildAfterGeneration)
            parameters.Request.UserConfirmedBuild = true;
        if (string.IsNullOrWhiteSpace(parameters.Request.ExpectedReviewHash))
            parameters.Request.ExpectedReviewHash = request.ConfirmationSummaryHash ?? string.Empty;
        var result = await workflow.ExecuteReviewAsync(parameters.ReviewId, parameters.Request, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("DXAIFunction executed review {ReviewId} with status {Status}.", parameters.ReviewId, result.Status);
        return new DxAiFunctionInvocationResult { Succeeded = result.Status is CodeGenerationReviewStatuses.Generated or CodeGenerationReviewStatuses.BuildPassed, Status = result.Status, Value = result };
    }

    private sealed class ExecuteParameters
    {
        public Guid ReviewId { get; set; }
        public ExecuteCodeGenerationReviewRequest Request { get; set; } = new();
    }
}

public sealed class RejectCodeGenerationReviewFunction(
    ICodeGenerationWorkflowService workflow,
    ILogger<RejectCodeGenerationReviewFunction> logger) : IDxAiFunctionHandler
{
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

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = request.Parameters.Deserialize<RejectParameters>(new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true })
            ?? throw new JsonException("A review rejection request is required.");
        parameters.Request.UserConfirmed = request.UserConfirmed;
        if (string.IsNullOrWhiteSpace(parameters.Request.ExpectedReviewHash))
            parameters.Request.ExpectedReviewHash = request.ConfirmationSummaryHash ?? string.Empty;
        var review = await workflow.RejectReviewAsync(parameters.ReviewId, parameters.Request, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("DXAIFunction rejected review {ReviewId}.", parameters.ReviewId);
        return new DxAiFunctionInvocationResult { Succeeded = true, Status = review.Status, Value = review };
    }

    private sealed class RejectParameters
    {
        public Guid ReviewId { get; set; }
        public RejectCodeGenerationReviewRequest Request { get; set; } = new();
    }
}

public sealed class ListLocalGptProjectsFunction(
    ILocalGptProjectService projects,
    ILogger<ListLocalGptProjectsFunction> logger) : IDxAiFunctionHandler
{
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

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? new ListParameters()
            : request.Parameters.Deserialize<ListParameters>(JsonOptions) ?? new ListParameters();
        var values = await projects.GetProjectsAsync(parameters.IncludeArchived, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("DXAIFunction listed {ProjectCount} LocalGPT project record(s).", values.Count);
        return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = values };
    }

    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    private sealed class ListParameters { public bool IncludeArchived { get; set; } }
}

public sealed class GetLocalGptProjectFunction(
    ILocalGptProjectService projects,
    ILogger<GetLocalGptProjectFunction> logger) : IDxAiFunctionHandler
{
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

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = request.Parameters.Deserialize<GetParameters>(JsonOptions)
            ?? throw new JsonException("projectId is required.");
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

    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    private sealed class GetParameters { public Guid ProjectId { get; set; } }
}

public sealed class ListRecentApplicationLogsFunction(
    IApplicationLogReaderService applicationLogs,
    ILogger<ListRecentApplicationLogsFunction> logger) : IDxAiFunctionHandler
{
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

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? new ListParameters()
            : request.Parameters.Deserialize<ListParameters>(JsonOptions) ?? new ListParameters();
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

    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    private string Limit(string value, int max) => value.Length <= max ? value : value[..max] + "...";
    private sealed class ListParameters
    {
        public string MinimumLevel { get; set; } = "Warning";
        public int Take { get; set; } = 12;
    }
}

public sealed class ListCouncilKnowledgeFunction(
    ICouncilKnowledgeService knowledge,
    ILogger<ListCouncilKnowledgeFunction> logger) : IDxAiFunctionHandler
{
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

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? new ListParameters()
            : request.Parameters.Deserialize<ListParameters>(JsonOptions) ?? new ListParameters();
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

    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    private string Limit(string value, int max) => value.Length <= max ? value : value[..max] + "...";
    private sealed class ListParameters
    {
        public bool IncludeArchived { get; set; }
        public int Take { get; set; } = 8;
    }
}

public sealed class ListChatMemoryConversationsFunction(
    IChatMemoryService memory,
    ILogger<ListChatMemoryConversationsFunction> logger) : IDxAiFunctionHandler
{
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

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? new ListParameters()
            : request.Parameters.Deserialize<ListParameters>(JsonOptions) ?? new ListParameters();
        var entries = await memory.GetConversationsAsync(Math.Clamp(parameters.Take, 1, 50), cancellationToken).ConfigureAwait(false);
        logger.LogInformation("DXAIFunction listed {ConversationCount} chat-memory conversation summary row(s).", entries.Count);
        return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = entries };
    }

    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    private sealed class ListParameters { public int Take { get; set; } = 12; }
}


public sealed class RequestHumanCollaborationFunction(ILocalGptVocabularyService vocabulary,
    
    IHumanCollaborationService collaboration,
    IAmbientLocalGptContext ambientContext,
    ILogger<RequestHumanCollaborationFunction> logger) : IDxAiFunctionHandler
{
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public DxaichatFunctionInfo Descriptor { get; } = new(
        "human.collaboration.request",
        "POST",
        "/api/dxai/functions/human.collaboration.request/invoke",
        "Ask the local human participant for bounded feedback or guidance without pausing unrelated council work.",
        "JSON parameters: kind Feedback or Guidance; title and description required; requestedRole, responsePrompt, suggestedResponses, prefillText, allowFreeText, and requiredBeforeCompletion optional.",
        "Coordination-only. This function may create a persistent inbox question, but it cannot approve operations, create trusted human identity, or authorize tools and side effects.",
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
            "requiredBeforeCompletion": { "type": "boolean" }
          },
          "required": ["title", "description"],
          "additionalProperties": false
        }
        """,
        IsCoordinationOnly: true);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var parameters = request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? new RequestParameters()
            : request.Parameters.Deserialize<RequestParameters>(JsonOptions) ?? new RequestParameters();
        if (string.IsNullOrWhiteSpace(parameters.Title) || string.IsNullOrWhiteSpace(parameters.Description))
            throw new JsonException("title and description are required.");

        var kind = string.Equals(parameters.Kind, vocabulary.Get().HumanRequestGuidance, StringComparison.OrdinalIgnoreCase)
            ? vocabulary.Get().HumanRequestGuidance
            : vocabulary.Get().HumanRequestFeedback;
        var ambient = ambientContext.Current;
        var suggestions = (parameters.SuggestedResponses ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();
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
            parameters.RequiredBeforeCompletion,
            ambient.CouncilRunId
        }, JsonOptions);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintSource))).ToLowerInvariant();
        var gate = await collaboration.AuthorizeOrEnqueueAsync(
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
                ambient.CouncilRound + 1,
                parameters.RequiredBeforeCompletion,
                IsSensitive: false,
                RequestKind: kind,
                SuggestedResponsesText: string.Join('\n', suggestions),
                ResponsePrompt: parameters.ResponsePrompt ?? string.Empty,
                PrefillText: parameters.PrefillText ?? string.Empty,
                AllowFreeText: parameters.AllowFreeText,
                ParameterFingerprint: fingerprint),
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
                EntersNextHeartbeat = ambient.CouncilRunId is not null,
                BlocksUnrelatedWork = false
            },
            Error = gate.RequestId is null ? gate.Message : null
        };
    }

    private sealed class RequestParameters
    {
        public string Kind { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RequestedRole { get; set; } = "Human collaborator";
        public string ResponsePrompt { get; set; } = "Your response";
        public string[]? SuggestedResponses { get; set; }
        public string PrefillText { get; set; } = string.Empty;
        public bool AllowFreeText { get; set; } = true;
        public bool RequiredBeforeCompletion { get; set; }
    }
}
