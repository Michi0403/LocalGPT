using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LocalGPT.Mcp;

/// <summary>
/// Implements the LocalGPT MCP Streamable HTTP JSON-RPC boundary without duplicating the underlying project,
/// knowledge, toolchain or DX-function business logic. LocalGPT remains authoritative for approvals and data policy.
/// </summary>
public sealed class LocalGptMcpEndpoint(
    IOptionsMonitor<McpGatewayOptions> optionMonitor,
    LocalGptMcpGatewayAccessor accessor,
    IDxAiFunctionRegistry functions,
    IDataProtectionProvider dataProtectionProvider,
    ILogger<LocalGptMcpEndpoint> logger)
{
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };
    private readonly IDataProtector ApprovalStateProtector = dataProtectionProvider.CreateProtector("LocalGPT.Mcp.ApprovalMrtr.v1");

    /// <summary>Handles one MCP Streamable HTTP POST containing a JSON-RPC request or notification.</summary>
    public async Task HandlePostAsync(HttpContext context)
    {
        var options = optionMonitor.CurrentValue;
        if (!TryAuthorize(context, options, out var denial))
        {
            context.Response.StatusCode = denial;
            return;
        }

        context.Response.Headers.CacheControl = "no-store";

        try
        {
            if (context.Request.ContentLength is long length && length > Math.Clamp(options.MaxRequestBodyBytes, 1024, 64 * 1024 * 1024))
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                return;
            }

            using var body = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted).ConfigureAwait(false);
            if (body.RootElement.ValueKind != JsonValueKind.Object)
            {
                await WriteErrorAsync(context, null, -32600, "Invalid Request").ConfigureAwait(false);
                return;
            }

            var request = body.RootElement;
            var hasId = request.TryGetProperty("id", out var id);
            var idNode = hasId ? JsonNode.Parse(id.GetRawText()) : null;
            if (!request.TryGetProperty("jsonrpc", out var jsonrpc) || jsonrpc.GetString() != "2.0" ||
                !request.TryGetProperty("method", out var methodElement) || string.IsNullOrWhiteSpace(methodElement.GetString()))
            {
                await WriteErrorAsync(context, idNode, -32600, "Invalid Request").ConfigureAwait(false);
                return;
            }

            var method = methodElement.GetString()!;
            request.TryGetProperty("params", out var parameters);
            var protocol = ResolveProtocol(context, method, parameters, options);
            if (protocol.ErrorCode is int protocolError)
            {
                await WriteErrorAsync(
                    context,
                    idNode,
                    protocolError,
                    protocol.ErrorMessage ?? "Unsupported MCP protocol request.",
                    protocol.ErrorData,
                    StatusCodes.Status400BadRequest).ConfigureAwait(false);
                return;
            }

            SetProtocolResponseHeader(context, protocol.ProtocolVersion);
            if (!hasId)
            {
                await HandleNotificationAsync(method, parameters, protocol, context.RequestAborted).ConfigureAwait(false);
                context.Response.StatusCode = StatusCodes.Status202Accepted;
                return;
            }

            var result = await DispatchAsync(method, parameters, protocol, options, context.RequestAborted).ConfigureAwait(false);
            if (result.ErrorCode is int errorCode)
            {
                await WriteErrorAsync(context, idNode, errorCode, result.ErrorMessage ?? "MCP request failed.", result.ErrorData).ConfigureAwait(false);
                return;
            }

            await WriteResultAsync(context, idNode, result.Result, protocol, method, options).ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "LocalGPT MCP rejected malformed JSON; request content was omitted from logs.");
            if (!context.Response.HasStarted)
                await WriteErrorAsync(context, null, -32700, "Parse error").ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("LocalGPT MCP request ended because the client disconnected or cancelled the request.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "LocalGPT MCP request failed; request parameters and returned data were omitted from logs.");
            if (!context.Response.HasStarted)
                await WriteErrorAsync(context, null, -32603, "Internal error. Review LocalGPT logs.").ConfigureAwait(false);
        }
    }

    /// <summary>Handles GET on a stateless MCP endpoint. LocalGPT deliberately does not open a server-initiated SSE channel.</summary>
    public Task HandleGetAsync(HttpContext context)
    {
        var options = optionMonitor.CurrentValue;
        if (!TryAuthorize(context, options, out var denial))
        {
            context.Response.StatusCode = denial;
            return Task.CompletedTask;
        }

        context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        context.Response.Headers.Allow = "POST";
        return Task.CompletedTask;
    }

    /// <summary>Rejects DELETE because the LocalGPT MCP gateway is stateless and owns no transport sessions.</summary>
    public Task HandleDeleteAsync(HttpContext context)
    {
        var options = optionMonitor.CurrentValue;
        if (!TryAuthorize(context, options, out var denial))
        {
            context.Response.StatusCode = denial;
            return Task.CompletedTask;
        }

        context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        context.Response.Headers.Allow = "POST";
        return Task.CompletedTask;
    }

    private McpProtocolContext ResolveProtocol(HttpContext context, string method, JsonElement parameters, McpGatewayOptions options)
    {
        try
        {
            if (method.Equals("initialize", StringComparison.Ordinal))
            {
                if (!options.EnableLegacyProtocol)
                    return ProtocolError(-32022, "Legacy MCP initialize compatibility is disabled.", options);
                return new McpProtocolContext(false, NormalizeLegacyProtocolVersion(options.LegacyProtocolVersion), null, null, null);
            }

            var modernVersion = GetModernMetaString(parameters, "io.modelcontextprotocol/protocolVersion");
            var headerVersion = context.Request.Headers["MCP-Protocol-Version"].ToString().Trim();
            var wantsModern = method.Equals("server/discover", StringComparison.Ordinal) ||
                              string.Equals(modernVersion, NormalizeModernProtocolVersion(options.ProtocolVersion), StringComparison.Ordinal) ||
                              string.Equals(headerVersion, NormalizeModernProtocolVersion(options.ProtocolVersion), StringComparison.Ordinal);

            if (!wantsModern)
            {
                if (options.EnableLegacyProtocol)
                    return new McpProtocolContext(false, NormalizeLegacyProtocolVersion(options.LegacyProtocolVersion), null, null, null);
                if (!options.EnableModernProtocol)
                    return ProtocolError(-32022, "No MCP protocol mode is enabled.", options);
                wantsModern = true;
            }

            if (!options.EnableModernProtocol)
                return ProtocolError(-32022, "Modern MCP is disabled by LocalGPT gateway policy.", options);

            var configuredModern = NormalizeModernProtocolVersion(options.ProtocolVersion);
            if (parameters.ValueKind != JsonValueKind.Object ||
                !parameters.TryGetProperty("_meta", out var metadata) || metadata.ValueKind != JsonValueKind.Object)
                return ProtocolError(-32602, "Modern MCP requests require params._meta protocol metadata.", options);

            if (!metadata.TryGetProperty("io.modelcontextprotocol/protocolVersion", out var metadataProtocol) ||
                metadataProtocol.ValueKind != JsonValueKind.String ||
                !string.Equals(metadataProtocol.GetString(), configuredModern, StringComparison.Ordinal))
                return ProtocolError(-32022, $"Modern MCP requires protocol version {configuredModern}.", options);

            if (!metadata.TryGetProperty("io.modelcontextprotocol/clientCapabilities", out var clientCapabilities) ||
                clientCapabilities.ValueKind != JsonValueKind.Object)
                return ProtocolError(-32602, "Modern MCP requests require params._meta io.modelcontextprotocol/clientCapabilities.", options);

            if (options.RequireModernRoutingHeaders)
            {
                if (!string.Equals(headerVersion, configuredModern, StringComparison.Ordinal))
                    return ProtocolError(-32020, $"MCP-Protocol-Version must be {configuredModern}.", options);

                var methodHeader = context.Request.Headers["Mcp-Method"].ToString();
                if (!string.Equals(methodHeader, method, StringComparison.Ordinal))
                    return ProtocolError(-32020, "Mcp-Method must match the JSON-RPC method.", options);

                var subject = GetRoutingSubject(method, parameters);
                if (!string.IsNullOrWhiteSpace(subject))
                {
                    var nameHeader = context.Request.Headers["Mcp-Name"].ToString();
                    if (!string.Equals(nameHeader, subject, StringComparison.Ordinal))
                        return ProtocolError(-32020, "Mcp-Name must match the addressed MCP tool, prompt, or resource.", options);
                }
            }

            var clientInfo = metadata.TryGetProperty("io.modelcontextprotocol/clientInfo", out var suppliedClientInfo) && suppliedClientInfo.ValueKind == JsonValueKind.Object
                ? suppliedClientInfo.GetRawText()
                : null;
            return new McpProtocolContext(true, configuredModern, clientCapabilities.GetRawText(), clientInfo, null);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "LocalGPT MCP protocol negotiation failed; request metadata was omitted from logs.");
            return ProtocolError(-32602, "LocalGPT could not validate the MCP request protocol metadata.", options);
        }
    }

    private async Task<McpDispatchResult> DispatchAsync(
        string method,
        JsonElement parameters,
        McpProtocolContext protocol,
        McpGatewayOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            if (protocol.IsModern && method.Equals("initialize", StringComparison.Ordinal))
                return Error(-32601, "Modern MCP does not use initialize; call server/discover.");
            if (!protocol.IsModern && method.Equals("server/discover", StringComparison.Ordinal))
                return Error(-32601, "server/discover requires modern MCP request metadata.");

            return method switch
            {
                "server/discover" when protocol.IsModern => Discover(options),
                "initialize" when !protocol.IsModern => Initialize(parameters, options),
                "ping" when !protocol.IsModern => Success(new JsonObject()),
                "tools/list" => options.ExposeTools ? ListTools(parameters, options) : MethodDisabled("tools"),
                "tools/call" => options.ExposeTools ? await CallToolAsync(parameters, protocol, options, cancellationToken).ConfigureAwait(false) : MethodDisabled("tools"),
                "resources/list" => options.ExposeResources ? ListResources(options) : MethodDisabled("resources"),
                "resources/templates/list" => options.ExposeResources ? ListResourceTemplates(options) : MethodDisabled("resources"),
                "resources/read" => options.ExposeResources ? await ReadResourceAsync(parameters, options, cancellationToken).ConfigureAwait(false) : MethodDisabled("resources"),
                "prompts/list" => options.ExposePrompts ? ListPrompts() : MethodDisabled("prompts"),
                "prompts/get" => options.ExposePrompts ? GetPrompt(parameters) : MethodDisabled("prompts"),
                _ => Error(-32601, "Method not found")
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "LocalGPT MCP dispatch failed for method {Method}; request data was omitted from logs.", method);
            return Error(-32603, "LocalGPT MCP dispatch failed. Review LocalGPT logs.");
        }
    }

    private McpDispatchResult Discover(McpGatewayOptions options)
    {
        try
        {
            return Success(new JsonObject
            {
                ["supportedVersions"] = BuildSupportedVersions(options, includeLegacy: false),
                ["capabilities"] = BuildCapabilities(options),
                ["instructions"] = BuildServerInstructions(true)
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the LocalGPT modern MCP discovery response failed.");
            return Error(-32603, "LocalGPT could not build MCP discovery metadata.");
        }
    }

    private McpDispatchResult Initialize(JsonElement parameters, McpGatewayOptions options)
    {
        try
        {
            var requested = parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty("protocolVersion", out var requestedVersion)
                ? requestedVersion.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(requested))
                return Error(-32602, "initialize requires protocolVersion.");
            var protocolVersion = NormalizeLegacyProtocolVersion(options.LegacyProtocolVersion);

            return Success(new JsonObject
            {
                ["protocolVersion"] = protocolVersion,
                ["capabilities"] = BuildCapabilities(options),
                ["serverInfo"] = BuildServerInfo(),
                ["instructions"] = BuildServerInstructions(false)
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the LocalGPT legacy MCP initialize response failed.");
            return Error(-32603, "LocalGPT could not initialize the legacy MCP compatibility boundary.");
        }
    }

    private JsonObject BuildCapabilities(McpGatewayOptions options)
    {
        var capabilities = new JsonObject();
        if (options.ExposeTools)
            capabilities["tools"] = new JsonObject { ["listChanged"] = false };
        if (options.ExposeResources)
            capabilities["resources"] = new JsonObject { ["subscribe"] = false, ["listChanged"] = false };
        if (options.ExposePrompts)
            capabilities["prompts"] = new JsonObject { ["listChanged"] = false };
        return capabilities;
    }

    private JsonObject BuildServerInfo() => new()
    {
        ["name"] = "LocalGPT",
        ["version"] = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "0.0.0"
    };

    private string BuildServerInstructions(bool modern)
    {
        var approval = modern
            ? "Consequential tools remain governed by LocalGPT Human Collaboration. A modern client that advertises elicitation may receive resultType=input_required; client input never authorizes the operation by itself."
            : "Consequential tools remain governed by LocalGPT Human Collaboration. If a tool returns _meta.localgptOperationId, approve the pending operation in LocalGPT and retry tools/call with the same operation id.";
        return $"LocalGPT exposes only the projects, knowledge, regex, toolchains, artifacts, prompts and DX functions enabled in Install → MCP gateway. {approval}";
    }

    private McpDispatchResult ListTools(JsonElement parameters, McpGatewayOptions options)
    {
        try
        {
            var all = accessor.FilterFunctions(functions.GetFunctions());
            var offset = ParseCursor(parameters);
            if (offset < 0 || offset > all.Count)
                return Error(-32602, "tools/list cursor is invalid.");

            var pageSize = Math.Clamp(options.MaxListItems, 1, 10000);
            var page = all.Skip(offset).Take(pageSize).ToArray();
            var tools = new JsonArray();
            foreach (var function in page)
            {
                JsonNode inputSchema;
                try
                {
                    inputSchema = JsonNode.Parse(function.ParameterSchemaJson) ?? new JsonObject { ["type"] = "object" };
                }
                catch (JsonException)
                {
                    inputSchema = new JsonObject { ["type"] = "object" };
                }

                tools.Add(new JsonObject
                {
                    ["name"] = function.Name,
                    ["title"] = function.Name,
                    ["description"] = BuildToolDescription(function),
                    ["inputSchema"] = inputSchema,
                    ["annotations"] = new JsonObject
                    {
                        ["readOnlyHint"] = function.IsReadOnly,
                        ["destructiveHint"] = !function.IsReadOnly,
                        ["idempotentHint"] = function.IsReadOnly,
                        ["openWorldHint"] = !function.IsReadOnly
                    }
                });
            }

            var result = new JsonObject { ["tools"] = tools };
            if (offset + page.Length < all.Count)
                result["nextCursor"] = (offset + page.Length).ToString(System.Globalization.CultureInfo.InvariantCulture);
            return Success(result);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing LocalGPT MCP tools failed.");
            return Error(-32603, "LocalGPT could not list MCP tools.");
        }
    }

    private async Task<McpDispatchResult> CallToolAsync(
        JsonElement parameters,
        McpProtocolContext protocol,
        McpGatewayOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            if (parameters.ValueKind != JsonValueKind.Object || !parameters.TryGetProperty("name", out var nameElement) || string.IsNullOrWhiteSpace(nameElement.GetString()))
                return Error(-32602, "tools/call requires a tool name.");

            var name = nameElement.GetString()!;
            var descriptor = accessor.FilterFunctions(functions.GetFunctions()).FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (descriptor is null)
                return Error(-32602, "The requested tool is not exposed by the LocalGPT MCP gateway policy.");

            JsonElement arguments;
            if (parameters.TryGetProperty("arguments", out var suppliedArguments) && suppliedArguments.ValueKind == JsonValueKind.Object)
                arguments = suppliedArguments.Clone();
            else
            {
                using var empty = JsonDocument.Parse("{}");
                arguments = empty.RootElement.Clone();
            }

            if (!accessor.IsFunctionInvocationAllowed(descriptor, arguments, out var policyDenial))
                return Error(-32602, policyDenial);

            Guid? protectedOperationId = null;
            if (protocol.IsModern && parameters.TryGetProperty("requestState", out var requestStateElement) && requestStateElement.ValueKind == JsonValueKind.String)
            {
                var stateResult = TryReadApprovalRequestState(requestStateElement.GetString(), descriptor.Name, out var approvalState);
                if (!stateResult)
                    return Error(-32602, "The MCP approval requestState is invalid, expired, or belongs to another LocalGPT tool.");
                protectedOperationId = approvalState!.OperationId;
            }

            var operationIdText = GetMetaString(parameters, "localgptOperationId", "localgpt.operationId", "localgpt/operationId");
            if (!TryParseOptionalGuid(operationIdText, out var operationId))
                return Error(-32602, "The MCP _meta LocalGPT operation id is not a valid GUID.");
            if (protectedOperationId is Guid protectedId && operationId is Guid suppliedOperationId && protectedId != suppliedOperationId)
                return Error(-32602, "The MCP requestState and _meta LocalGPT operation ids do not match.");
            operationId ??= protectedOperationId;

            var conversationIdText = GetMetaString(parameters, "localgptConversationId", "localgpt.conversationId", "localgpt/conversationId");
            if (!TryParseOptionalGuid(conversationIdText, out var conversationId))
                return Error(-32602, "The MCP _meta LocalGPT conversation id is not a valid GUID.");
            var projectIdText = GetArgumentString(arguments, "projectId", "project_id", "localGptProjectId");
            if (!TryParseOptionalGuid(projectIdText, out var projectId))
                return Error(-32602, "The LocalGPT projectId tool argument is not a valid GUID.");
            var projectVersionIdText = GetArgumentString(arguments, "projectVersionId", "project_version_id");
            if (!TryParseOptionalGuid(projectVersionIdText, out var projectVersionId))
                return Error(-32602, "The LocalGPT projectVersionId tool argument is not a valid GUID.");

            var invocation = new DxAiFunctionInvocationRequest
            {
                Parameters = arguments,
                UserConfirmed = false,
                AutomaticInvocation = options.AllowAutomaticFunctionInvocation && descriptor.SupportsAutomaticInvocation,
                RequestedBy = "MCP client",
                ApplicationVersion = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? string.Empty,
                OperationId = operationId,
                ConversationId = conversationId,
                ProjectId = projectId,
                ProjectVersionId = projectVersionId
            };
            var result = await functions.InvokeAsync(descriptor.Name, invocation, cancellationToken).ConfigureAwait(false);

            if (protocol.IsModern && options.EnableApprovalMultiRoundTrip &&
                result.Status.Equals("HumanApprovalPending", StringComparison.OrdinalIgnoreCase) &&
                SupportsElicitation(protocol))
                return Success(CreateApprovalInputRequired(descriptor.Name, result.OperationId));

            var text = accessor.SerializeInvocationResult(result, descriptor);
            JsonNode? structured = null;
            try
            {
                structured = JsonNode.Parse(text);
            }
            catch (JsonException exception)
            {
                logger.LogDebug(exception, "LocalGPT MCP invocation result for {ToolName} was not structured JSON.", descriptor.Name);
            }

            var approvalPending = result.Status.Equals("HumanApprovalPending", StringComparison.OrdinalIgnoreCase);
            return Success(new JsonObject
            {
                ["content"] = CreateTextContent(text),
                ["structuredContent"] = structured,
                ["isError"] = !result.Succeeded && !approvalPending,
                ["_meta"] = new JsonObject
                {
                    ["localgptOperationId"] = result.OperationId.ToString("D"),
                    ["localgptStatus"] = result.Status,
                    ["localgptApprovalRetry"] = approvalPending,
                    ["localgptApprovalInstruction"] = approvalPending
                        ? "Approve this pending operation in LocalGPT Human Collaboration and retry tools/call with this same localgptOperationId. MCP client input does not grant approval."
                        : "No approval retry is pending for this result."
                }
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Invoking a LocalGPT MCP tool failed; tool arguments were omitted from logs.");
            return Error(-32603, "LocalGPT MCP tool invocation failed. Review LocalGPT logs.");
        }
    }

    private JsonObject CreateApprovalInputRequired(string functionName, Guid operationId)
    {
        var state = new ApprovalRetryState
        {
            FunctionName = functionName,
            OperationId = operationId,
            IssuedAtUtc = DateTimeOffset.UtcNow
        };
        var requestState = ApprovalStateProtector.Protect(JsonSerializer.Serialize(state, JsonOptions));
        return new JsonObject
        {
            ["resultType"] = "input_required",
            ["inputRequests"] = new JsonObject
            {
                ["localgptApproval"] = new JsonObject
                {
                    ["method"] = "elicitation/create",
                    ["params"] = new JsonObject
                    {
                        ["mode"] = "form",
                        ["message"] = $"LocalGPT queued {functionName} as operation {operationId:D}. Approve or decline that exact operation in LocalGPT Human Collaboration, then confirm here so the client can retry. This form does not authorize the operation by itself.",
                        ["requestedSchema"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["decisionRecordedInLocalGpt"] = new JsonObject
                                {
                                    ["type"] = "boolean",
                                    ["title"] = "I recorded my decision in LocalGPT"
                                }
                            },
                            ["required"] = RequiredArray("decisionRecordedInLocalGpt")
                        }
                    }
                }
            },
            ["requestState"] = requestState,
            ["_meta"] = new JsonObject
            {
                ["localgptOperationId"] = operationId.ToString("D"),
                ["localgptApprovalAuthority"] = "LocalGPT Human Collaboration"
            }
        };
    }

    private bool TryReadApprovalRequestState(string? protectedState, string functionName, out ApprovalRetryState? state)
    {
        state = null;
        if (string.IsNullOrWhiteSpace(protectedState))
            return false;

        try
        {
            var json = ApprovalStateProtector.Unprotect(protectedState);
            state = JsonSerializer.Deserialize<ApprovalRetryState>(json, JsonOptions);
            return state is not null && state.OperationId != Guid.Empty &&
                   state.FunctionName.Equals(functionName, StringComparison.OrdinalIgnoreCase);
        }
        catch (CryptographicException exception)
        {
            logger.LogWarning(exception, "LocalGPT MCP rejected an invalid approval requestState; protected contents were omitted from logs.");
            return false;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "LocalGPT MCP rejected malformed protected approval state.");
            return false;
        }
    }

    private bool SupportsElicitation(McpProtocolContext protocol)
    {
        if (!protocol.IsModern || string.IsNullOrWhiteSpace(protocol.ClientCapabilitiesJson))
            return false;
        try
        {
            using var document = JsonDocument.Parse(protocol.ClientCapabilitiesJson);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!property.Name.Equals("elicitation", StringComparison.OrdinalIgnoreCase) &&
                    !property.Name.Equals("io.modelcontextprotocol/elicitation", StringComparison.OrdinalIgnoreCase))
                    continue;
                return property.Value.ValueKind != JsonValueKind.False && property.Value.ValueKind != JsonValueKind.Null;
            }
            return false;
        }
        catch (JsonException exception)
        {
            logger.LogDebug(exception, "LocalGPT MCP client capability metadata could not be inspected for elicitation support.");
            return false;
        }
    }

    private McpDispatchResult ListResources(McpGatewayOptions options)
    {
        try
        {
            var resources = new JsonArray
            {
                Resource("localgpt://gateway/status", "LocalGPT MCP gateway policy", "Effective non-secret gateway exposure policy.")
            };
            if (options.ExposeProjects) resources.Add(Resource("localgpt://projects", "LocalGPT projects", "Database-backed project metadata allowed by the MCP project allow-list."));
            if (options.ExposeKnowledge) resources.Add(Resource("localgpt://knowledge", "LocalGPT knowledge", "Council knowledge metadata/content according to gateway policy."));
            if (options.ExposeRegex) resources.Add(Resource("localgpt://regex", "LocalGPT regex knowledge", "Reusable regex records used by LocalGPT project and knowledge recognition."));
            if (options.ExposeToolchains) resources.Add(Resource("localgpt://toolchains", "LocalGPT toolchains", "Configured compiler/runtime/toolchain inventory with path/environment redaction policy."));
            return Success(new JsonObject { ["resources"] = resources });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing LocalGPT MCP resources failed.");
            return Error(-32603, "LocalGPT could not list MCP resources.");
        }
    }

    private McpDispatchResult ListResourceTemplates(McpGatewayOptions options)
    {
        try
        {
            var templates = new JsonArray();
            if (options.ExposeProjects)
            {
                templates.Add(ResourceTemplate("localgpt://projects/{projectId}", "LocalGPT project", "One database-backed project and structural counts."));
                templates.Add(ResourceTemplate("localgpt://projects/{projectId}/revisions", "Project revisions", "Persisted project revisions and structure metadata."));
                if (options.ExposeArtifacts)
                    templates.Add(ResourceTemplate("localgpt://projects/{projectId}/artifacts", "Project artifacts", "Project artifact metadata/content according to exposure policy."));
            }
            if (options.ExposeKnowledge)
                templates.Add(ResourceTemplate("localgpt://knowledge/{knowledgeId}", "Knowledge entry", "One Council knowledge record."));
            return Success(new JsonObject { ["resourceTemplates"] = templates });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing LocalGPT MCP resource templates failed.");
            return Error(-32603, "LocalGPT could not list MCP resource templates.");
        }
    }

    private async Task<McpDispatchResult> ReadResourceAsync(JsonElement parameters, McpGatewayOptions options, CancellationToken cancellationToken)
    {
        try
        {
            if (parameters.ValueKind != JsonValueKind.Object || !parameters.TryGetProperty("uri", out var uriElement) || string.IsNullOrWhiteSpace(uriElement.GetString()))
                return Error(-32602, "resources/read requires a URI.");
            var raw = uriElement.GetString()!;
            if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) || !uri.Scheme.Equals("localgpt", StringComparison.OrdinalIgnoreCase))
                return Error(-32602, "Only localgpt:// resources are supported.");

            string content;
            if (uri.Host.Equals("gateway", StringComparison.OrdinalIgnoreCase) && uri.AbsolutePath.Equals("/status", StringComparison.OrdinalIgnoreCase))
                content = accessor.GetGatewayStatus();
            else if (uri.Host.Equals("projects", StringComparison.OrdinalIgnoreCase))
                content = await ReadProjectResourceAsync(uri, cancellationToken).ConfigureAwait(false);
            else if (uri.Host.Equals("knowledge", StringComparison.OrdinalIgnoreCase))
                content = await ReadKnowledgeResourceAsync(uri, cancellationToken).ConfigureAwait(false);
            else if (uri.Host.Equals("regex", StringComparison.OrdinalIgnoreCase))
                content = await accessor.ListRegexAsync(GetQueryValue(uri, "q"), cancellationToken).ConfigureAwait(false);
            else if (uri.Host.Equals("toolchains", StringComparison.OrdinalIgnoreCase))
                content = await accessor.ListToolchainsAsync(GetQueryValue(uri, "language"), cancellationToken).ConfigureAwait(false);
            else
                return Error(-32602, "The requested LocalGPT resource is not exposed.");

            return Success(new JsonObject
            {
                ["contents"] = new JsonArray(new JsonObject
                {
                    ["uri"] = raw,
                    ["mimeType"] = "application/json",
                    ["text"] = content
                })
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading a LocalGPT MCP resource failed; resource URI was omitted from logs.");
            return Error(-32603, "LocalGPT could not read the requested MCP resource.");
        }
    }

    private async Task<string> ReadProjectResourceAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
                return await accessor.ListProjectsAsync(GetQueryValue(uri, "q"), cancellationToken).ConfigureAwait(false);
            if (!Guid.TryParse(segments[0], out var projectId))
                return "{\"error\":\"invalid_project_id\"}";
            if (segments.Length == 1)
                return await accessor.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
            if (segments[1].Equals("revisions", StringComparison.OrdinalIgnoreCase))
                return await accessor.ListProjectRevisionsAsync(projectId, cancellationToken).ConfigureAwait(false);
            if (segments[1].Equals("artifacts", StringComparison.OrdinalIgnoreCase))
                return await accessor.ListProjectArtifactsAsync(projectId, cancellationToken).ConfigureAwait(false);
            return "{\"error\":\"project_resource_not_found\"}";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading a LocalGPT MCP project resource failed; URI data was omitted from logs.");
            return "{\"error\":\"project_resource_failed\"}";
        }
    }

    private async Task<string> ReadKnowledgeResourceAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
                return await accessor.SearchKnowledgeAsync(GetQueryValue(uri, "q"), cancellationToken).ConfigureAwait(false);
            return Guid.TryParse(segments[0], out var knowledgeId)
                ? await accessor.GetKnowledgeAsync(knowledgeId, cancellationToken).ConfigureAwait(false)
                : "{\"error\":\"invalid_knowledge_id\"}";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading a LocalGPT MCP knowledge resource failed; URI data was omitted from logs.");
            return "{\"error\":\"knowledge_resource_failed\"}";
        }
    }

    private McpDispatchResult ListPrompts()
    {
        try
        {
            var prompts = new JsonArray
            {
                PromptDescriptor("project-review", "Review a LocalGPT project using its persisted structure, revisions, knowledge and toolchains.", [("projectId", true), ("focus", false)]),
                PromptDescriptor("project-implement", "Implement a requested change through LocalGPT project/workspace tools and return artifact/download plus fallback local path when policy permits.", [("projectId", true), ("request", true)]),
                PromptDescriptor("knowledge-research", "Research an engineering question against LocalGPT Council knowledge and project data.", [("question", true)]),
                PromptDescriptor("speech-setup", "Prepare LocalGPT speech-to-text prerequisites and choose an installed SpeechRecognition provider without bypassing install or approval policy.", [])
            };
            return Success(new JsonObject { ["prompts"] = prompts });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing LocalGPT MCP prompts failed.");
            return Error(-32603, "LocalGPT could not list MCP prompts.");
        }
    }

    private McpDispatchResult GetPrompt(JsonElement parameters)
    {
        try
        {
            if (parameters.ValueKind != JsonValueKind.Object || !parameters.TryGetProperty("name", out var nameElement) || string.IsNullOrWhiteSpace(nameElement.GetString()))
                return Error(-32602, "prompts/get requires a prompt name.");
            var name = nameElement.GetString()!;
            var args = parameters.TryGetProperty("arguments", out var supplied) && supplied.ValueKind == JsonValueKind.Object ? supplied : default;
            string text = name switch
            {
                "project-review" => $"Review LocalGPT project {Argument(args, "projectId", "<projectId>")}. Read the project, revision, knowledge and toolchain resources first. Focus: {Argument(args, "focus", "correctness, regressions and maintainability")}. Do not mutate files unless the user explicitly asks for changes.",
                "project-implement" => $"Implement this request for LocalGPT project {Argument(args, "projectId", "<projectId>")}: {Argument(args, "request", "<request>")}. Use the project's database identity, workspace/toolchain and structure knowledge. Keep human approvals authoritative. Produce real workspace/project files and, when LocalGPT exposes them, return a directly usable download URL plus the local system path as fallback.",
                "knowledge-research" => $"Research this question using LocalGPT MCP knowledge/project resources before answering: {Argument(args, "question", "<question>")}. Distinguish persisted evidence from inference.",
                "speech-setup" => "Inspect LocalGPT Local AI status and the installed-model inventory for SpeechRecognition. Prefer an already usable managed Python/OpenAI Whisper model, but accept compatible Hugging Face/Transformers ASR or future SpeechRecognition adapters. Only guide Python environment, PyTorch, Whisper or FFmpeg setup when the selected provider actually needs them. Never claim speech-to-text is ready until the installed-model inventory exposes SpeechRecognition.",
                _ => string.Empty
            };
            if (string.IsNullOrWhiteSpace(text))
                return Error(-32602, "Unknown LocalGPT prompt.");
            return Success(new JsonObject
            {
                ["description"] = name,
                ["messages"] = new JsonArray(new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = new JsonObject { ["type"] = "text", ["text"] = text }
                })
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading a LocalGPT MCP prompt failed; prompt arguments were omitted from logs.");
            return Error(-32603, "LocalGPT could not render the requested MCP prompt.");
        }
    }

    private Task HandleNotificationAsync(string method, JsonElement parameters, McpProtocolContext protocol, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!method.Equals("notifications/initialized", StringComparison.Ordinal) &&
                !method.Equals("notifications/cancelled", StringComparison.Ordinal) &&
                !method.StartsWith("notifications/", StringComparison.Ordinal))
                logger.LogDebug("LocalGPT MCP ignored JSON-RPC notification {Method} for protocol {ProtocolVersion}.", method, protocol.ProtocolVersion);
            return Task.CompletedTask;
        }
        catch
        {
            throw;
        }
    }

    private bool TryAuthorize(HttpContext context, McpGatewayOptions options, out int denialStatus)
    {
        try
        {
            denialStatus = StatusCodes.Status403Forbidden;
            if (!Program.McpEnabled || !IsAllowedEndpoint(context))
            {
                denialStatus = StatusCodes.Status404NotFound;
                return false;
            }

            var remote = context.Connection.RemoteIpAddress;
            if (!options.AllowRemoteClients && remote is not null && !IPAddress.IsLoopback(remote))
                return false;

            var host = context.Request.Host.Host;
            var allowedHostPolicy = string.IsNullOrWhiteSpace(options.AllowedHosts) ? "localhost;127.0.0.1;::1;[::1]" : options.AllowedHosts;
            var allowedHosts = Split(allowedHostPolicy);
            if (allowedHosts.Count > 0 && !allowedHosts.Contains("*") && !allowedHosts.Contains(host, StringComparer.OrdinalIgnoreCase))
                return false;

            if (context.Request.Headers.TryGetValue("Origin", out var originValues) && !string.IsNullOrWhiteSpace(originValues.ToString()))
            {
                var allowedOrigins = Split(options.AllowedOrigins);
                if (allowedOrigins.Count == 0 || !allowedOrigins.Contains("*") && !allowedOrigins.Contains(originValues.ToString(), StringComparer.OrdinalIgnoreCase))
                    return false;
            }

            var requireApiKey = options.RequireApiKey || options.AllowRemoteClients;
            if (!requireApiKey)
                return true;
            var configuredKey = Environment.GetEnvironmentVariable("LOCALGPT_MCP_API_KEY");
            if (string.IsNullOrWhiteSpace(configuredKey))
                configuredKey = options.ApiKey;
            if (string.IsNullOrWhiteSpace(configuredKey))
                return false;

            var supplied = string.Empty;
            if (!string.IsNullOrWhiteSpace(options.ApiKeyHeader) && context.Request.Headers.TryGetValue(options.ApiKeyHeader, out var header))
                supplied = header.ToString();
            if (string.IsNullOrWhiteSpace(supplied) && options.AcceptBearerToken && context.Request.Headers.TryGetValue("Authorization", out var authorization))
            {
                const string prefix = "Bearer ";
                var value = authorization.ToString();
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    supplied = value[prefix.Length..].Trim();
            }
            if (string.IsNullOrWhiteSpace(supplied))
            {
                denialStatus = StatusCodes.Status401Unauthorized;
                return false;
            }
            var left = Encoding.UTF8.GetBytes(configuredKey);
            var right = Encoding.UTF8.GetBytes(supplied);
            if (left.Length != right.Length || !CryptographicOperations.FixedTimeEquals(left, right))
            {
                denialStatus = StatusCodes.Status401Unauthorized;
                return false;
            }
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "LocalGPT MCP admission policy evaluation failed; credentials and request values were omitted from logs.");
            denialStatus = StatusCodes.Status403Forbidden;
            return false;
        }
    }

    private bool IsAllowedEndpoint(HttpContext context)
    {
        try
        {
            var isDedicated = Program.McpDedicatedListenerEnabled && Program.McpPort > 0 && context.Connection.LocalPort == Program.McpPort;
            var isPrimary = Program.McpExposeOnPrimaryEndpoint && context.Connection.LocalPort == Program.Port;
            return isDedicated || isPrimary;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "LocalGPT MCP endpoint ownership validation failed.");
            return false;
        }
    }

    private JsonObject Resource(string uri, string name, string description) => new()
    {
        ["uri"] = uri,
        ["name"] = name,
        ["description"] = description,
        ["mimeType"] = "application/json"
    };

    private JsonObject ResourceTemplate(string uriTemplate, string name, string description) => new()
    {
        ["uriTemplate"] = uriTemplate,
        ["name"] = name,
        ["description"] = description,
        ["mimeType"] = "application/json"
    };

    private JsonObject PromptDescriptor(string name, string description, IEnumerable<(string Name, bool Required)> arguments)
    {
        var values = new JsonArray();
        foreach (var argument in arguments)
            values.Add(new JsonObject { ["name"] = argument.Name, ["required"] = argument.Required });
        return new JsonObject { ["name"] = name, ["description"] = description, ["arguments"] = values };
    }

    private string BuildToolDescription(DxaichatFunctionInfo function)
    {
        var confirmation = function.RequiresHumanConfirmation || !function.IsReadOnly ? " LocalGPT human approval policy may gate execution." : string.Empty;
        return $"{function.Purpose}{confirmation} Safety: {function.SafetyNotes}".Trim();
    }

    private string Argument(JsonElement arguments, string name, string fallback) =>
        arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!
            : fallback;

    private string? GetMetaString(JsonElement parameters, params string[] names)
    {
        if (parameters.ValueKind != JsonValueKind.Object || !parameters.TryGetProperty("_meta", out var metadata) || metadata.ValueKind != JsonValueKind.Object)
            return null;
        return GetArgumentString(metadata, names);
    }

    private string? GetModernMetaString(JsonElement parameters, string name)
    {
        if (parameters.ValueKind != JsonValueKind.Object || !parameters.TryGetProperty("_meta", out var metadata) || metadata.ValueKind != JsonValueKind.Object)
            return null;
        return metadata.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private string? GetArgumentString(JsonElement arguments, params string[] names)
    {
        if (arguments.ValueKind != JsonValueKind.Object)
            return null;
        foreach (var property in arguments.EnumerateObject())
        {
            if (!names.Any(name => property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                continue;
            return property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : property.Value.GetRawText().Trim('"');
        }
        return null;
    }

    private bool TryParseOptionalGuid(string? value, out Guid? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;
        if (!Guid.TryParse(value, out var guid))
            return false;
        parsed = guid;
        return true;
    }

    private string? GetQueryValue(Uri uri, string key)
    {
        var query = uri.Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = pair.IndexOf('=');
            var name = separator < 0 ? pair : pair[..separator];
            if (!Uri.UnescapeDataString(name).Equals(key, StringComparison.OrdinalIgnoreCase))
                continue;
            return separator < 0 ? string.Empty : Uri.UnescapeDataString(pair[(separator + 1)..].Replace('+', ' '));
        }
        return null;
    }

    private string? GetRoutingSubject(string method, JsonElement parameters)
    {
        if (parameters.ValueKind != JsonValueKind.Object)
            return null;
        if (method.Equals("tools/call", StringComparison.Ordinal) || method.Equals("prompts/get", StringComparison.Ordinal))
            return parameters.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString() : null;
        if (method.Equals("resources/read", StringComparison.Ordinal))
            return parameters.TryGetProperty("uri", out var uri) && uri.ValueKind == JsonValueKind.String ? uri.GetString() : null;
        return null;
    }

    private int ParseCursor(JsonElement parameters)
    {
        if (parameters.ValueKind != JsonValueKind.Object || !parameters.TryGetProperty("cursor", out var cursor) || cursor.ValueKind == JsonValueKind.Null)
            return 0;
        var text = cursor.ValueKind == JsonValueKind.String ? cursor.GetString() : cursor.GetRawText();
        return int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var offset) && offset >= 0 ? offset : -1;
    }

    private string NormalizeModernProtocolVersion(string? value) => string.Equals(value?.Trim(), "2026-07-28", StringComparison.Ordinal) ? "2026-07-28" : "2026-07-28";
    private string NormalizeLegacyProtocolVersion(string? value) => string.Equals(value?.Trim(), "2025-11-25", StringComparison.Ordinal) ? "2025-11-25" : "2025-11-25";

    private List<string> Split(string? value) => string.IsNullOrWhiteSpace(value)
        ? []
        : value.Split([';', ',', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private JsonArray BuildSupportedVersions(McpGatewayOptions options, bool includeLegacy = true)
    {
        var versions = new JsonArray();
        if (options.EnableModernProtocol)
            versions.Add(NormalizeModernProtocolVersion(options.ProtocolVersion));
        if (includeLegacy && options.EnableLegacyProtocol)
            versions.Add(NormalizeLegacyProtocolVersion(options.LegacyProtocolVersion));
        return versions;
    }

    private JsonArray RequiredArray(string name)
    {
        var values = new JsonArray();
        values.Add(name);
        return values;
    }

    private JsonArray CreateTextContent(string text)
    {
        return new JsonArray
        {
            new JsonObject { ["type"] = "text", ["text"] = text }
        };
    }

    private McpProtocolContext ProtocolError(int code, string message, McpGatewayOptions options) =>
        new(false, string.Empty, null, null, new McpDispatchResult(null, code, message, new JsonObject
        {
            ["supportedVersions"] = BuildSupportedVersions(options)
        }));

    private void SetProtocolResponseHeader(HttpContext context, string protocolVersion)
    {
        if (!string.IsNullOrWhiteSpace(protocolVersion))
            context.Response.Headers["MCP-Protocol-Version"] = protocolVersion;
    }

    private async Task WriteResultAsync(
        HttpContext context,
        JsonNode? id,
        JsonNode? result,
        McpProtocolContext protocol,
        string method,
        McpGatewayOptions options)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";
        var preparedResult = result ?? new JsonObject();
        if (protocol.IsModern && preparedResult is JsonObject modernResult)
        {
            if (!modernResult.ContainsKey("resultType"))
                modernResult["resultType"] = "complete";

            JsonObject metadata;
            if (modernResult["_meta"] is JsonObject existingMetadata)
                metadata = existingMetadata;
            else
            {
                metadata = new JsonObject();
                modernResult["_meta"] = metadata;
            }
            metadata["io.modelcontextprotocol/serverInfo"] = BuildServerInfo();

            if (IsCacheHintMethod(method))
            {
                modernResult["ttlMs"] = Math.Clamp(options.CacheTtlMs, 0, 86_400_000);
                modernResult["cacheScope"] = NormalizeCacheScope(options.CacheScope);
            }
        }

        var envelope = new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["result"] = preparedResult };
        await context.Response.WriteAsync(envelope.ToJsonString(JsonOptions), context.RequestAborted).ConfigureAwait(false);
    }

    private async Task WriteErrorAsync(
        HttpContext context,
        JsonNode? id,
        int code,
        string message,
        JsonNode? data = null,
        int httpStatusCode = StatusCodes.Status200OK)
    {
        context.Response.StatusCode = httpStatusCode;
        context.Response.ContentType = "application/json";
        var error = new JsonObject { ["code"] = code, ["message"] = message };
        if (data is not null) error["data"] = data;
        var envelope = new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["error"] = error };
        await context.Response.WriteAsync(envelope.ToJsonString(JsonOptions), context.RequestAborted).ConfigureAwait(false);
    }

    private bool IsCacheHintMethod(string method) => method is
        "server/discover" or
        "tools/list" or
        "prompts/list" or
        "resources/list" or
        "resources/templates/list" or
        "resources/read";

    private string NormalizeCacheScope(string? value) => value?.Trim().Equals("public", StringComparison.OrdinalIgnoreCase) == true ? "public" : "private";

    private McpDispatchResult Success(JsonNode? result) => new(result, null, null, null);
    private McpDispatchResult Error(int code, string message, JsonNode? data = null) => new(null, code, message, data);
    private McpDispatchResult MethodDisabled(string domain) => Error(-32601, $"The LocalGPT MCP {domain} domain is disabled by gateway policy.");

    private sealed record McpDispatchResult(JsonNode? Result, int? ErrorCode, string? ErrorMessage, JsonNode? ErrorData);
    private sealed record McpProtocolContext(bool IsModern, string ProtocolVersion, string? ClientCapabilitiesJson, string? ClientInfoJson, McpDispatchResult? ProtocolError)
    {
        public int? ErrorCode => ProtocolError?.ErrorCode;
        public string? ErrorMessage => ProtocolError?.ErrorMessage;
        public JsonNode? ErrorData => ProtocolError?.ErrorData;
    }
    private sealed class ApprovalRetryState
    {
        public string FunctionName { get; set; } = string.Empty;
        public Guid OperationId { get; set; }
        public DateTimeOffset IssuedAtUtc { get; set; }
    }
}
