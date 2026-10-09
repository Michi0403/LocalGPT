using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace LocalGPT.Mcp;

/// <summary>
/// Applies the LocalGPT MCP gateway's data-domain allow-list and result-shaping rules. This adapter intentionally
/// sits above the database schema so MCP clients consume stable project/knowledge concepts rather than raw tables.
/// </summary>
public sealed class LocalGptMcpGatewayAccessor(
    IOptionsMonitor<McpGatewayOptions> optionMonitor,
    IDbContextFactory<LocalGptMemoryDbContext> dbFactory,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<LocalGptMcpGatewayAccessor> logger)
{
    private McpGatewayOptions Options => optionMonitor.CurrentValue;
    private McpGatewayRuntimeParameters Parameters => runtimePolicy.GetJson<McpGatewayRuntimeParameters>(LocalGptRuntimeValue.McpGatewayRuntimeParametersJson);
    private int MaxListItems => Math.Clamp(Options.MaxListItems, Parameters.MinimumListItems, Parameters.MaximumListItems);
    private int MaxResultCharacters => Math.Clamp(Options.MaxResultCharacters, Parameters.MinimumResultCharacters, Parameters.MaximumResultCharacters);
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    /// <summary>Returns a non-secret snapshot of the effective gateway policy.</summary>
    public string GetGatewayStatus()
    {
        try
        {
            return Serialize(new
            {
                enabled = Program.McpEnabled,
                endpoint = Program.McpDedicatedListenerEnabled
                    ? $"{(string.IsNullOrWhiteSpace(Options.CertificatePath) ? "http" : "https")}://{Program.McpAddress}:{Program.McpPort}{Program.McpPath}"
                    : Program.McpExposeOnPrimaryEndpoint ? $"{Program.BaseUrl}{Program.McpPath}" : "disabled",
                dedicatedListener = Program.McpDedicatedListenerEnabled,
                exposedOnPrimaryEndpoint = Program.McpExposeOnPrimaryEndpoint,
                remoteClients = Options.AllowRemoteClients,
                apiKeyRequired = Options.RequireApiKey,
                apiKeyHeader = Options.ApiKeyHeader,
                allowedHosts = Split(Options.AllowedHosts),
                allowedOrigins = Split(Options.AllowedOrigins),
                protocol = new
                {
                    modernEnabled = Options.EnableModernProtocol,
                    modernVersion = Options.ProtocolVersion,
                    legacyEnabled = Options.EnableLegacyProtocol,
                    legacyVersion = Options.LegacyProtocolVersion,
                    modernRoutingHeadersRequired = Options.RequireModernRoutingHeaders,
                    approvalMultiRoundTrip = Options.EnableApprovalMultiRoundTrip,
                    cacheTtlMs = Options.CacheTtlMs,
                    cacheScope = Options.CacheScope
                },
                maxRequestBodyBytes = Options.MaxRequestBodyBytes,
                maxListItems = MaxListItems,
                maxResultCharacters = MaxResultCharacters,
                domains = new
                {
                    tools = Options.ExposeTools,
                    resources = Options.ExposeResources,
                    prompts = Options.ExposePrompts,
                    projects = Options.ExposeProjects,
                    knowledge = Options.ExposeKnowledge,
                    knowledgeContent = Options.ExposeKnowledgeContent,
                    regex = Options.ExposeRegex,
                    toolchains = Options.ExposeToolchains,
                    artifacts = Options.ExposeArtifacts,
                    artifactValues = Options.ExposeArtifactValues,
                    sensitiveArtifacts = Options.ExposeSensitiveArtifacts,
                    council = Options.ExposeCouncil,
                    runtimePolicy = Options.ExposeRuntimePolicy,
                    diagnostics = Options.ExposeDiagnostics,
                    systemPaths = Options.ExposeSystemPaths
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LocalGPT MCP gateway data operation failed with code gateway_status_failed; requested data and paths were omitted from logs.");
            return SerializeError("gateway_status_failed", "The LocalGPT MCP data operation failed. Review LocalGPT logs.");
        }
    }

    /// <summary>Filters the LocalGPT DX function catalog according to the MCP gateway's explicit exposure policy.</summary>
    public IReadOnlyList<DxaichatFunctionInfo> FilterFunctions(IEnumerable<DxaichatFunctionInfo> functions)
    {
        try
        {
            var include = Split(Options.ToolIncludePrefixes);
            var exclude = Split(Options.ToolExcludePrefixes);

            return functions
                .Where(function => function.AvailableToAi)
                .Where(function => function.IsReadOnly ? Options.ExposeReadOnlyFunctions : Options.ExposeMutatingFunctions)
                .Where(function => !function.RequiresHumanConfirmation || Options.ExposeConfirmationRequiredFunctions)
                .Where(function => Options.ExposeProjects || !MatchesAnyCategory(function, "project", "workspace", "revision"))
                .Where(function => Options.ExposeKnowledge || !MatchesAnyCategory(function, "knowledge", "learningbase", "learning-base"))
                .Where(function => Options.ExposeRegex || !MatchesCategory(function, "regex"))
                .Where(function => Options.ExposeToolchains || !MatchesAnyCategory(function, "toolchain", "compiler-installation", "compilerinstallation"))
                .Where(function => Options.ExposeArtifacts || !MatchesCategory(function, "artifact"))
                .Where(function => Options.ExposeCouncil || !MatchesCategory(function, "council"))
                .Where(function => Options.ExposeRuntimePolicy || !MatchesAnyCategory(function, "runtimepolicy", "runtime-policy", "runtime_policy"))
                .Where(function => Options.ExposeDiagnostics || !MatchesAnyCategory(function, "diagnostic", "applicationlog", "application-log"))
                .Where(function => include.Count == 0 || MatchesPrefix(function, include))
                .Where(function => exclude.Count == 0 || !MatchesPrefix(function, exclude))
                .OrderBy(function => function.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            throw;
        }
    }


    /// <summary>Checks project allow-list boundaries before an exposed DX function is invoked through MCP.</summary>
    public bool IsFunctionInvocationAllowed(DxaichatFunctionInfo function, JsonElement arguments, out string denialReason)
    {
        try
        {
            denialReason = string.Empty;
            var allowed = ParseProjectAllowList();
            if (allowed.Count == 0)
                return true;

            var projectScoped = MatchesAnyCategory(function, "project", "workspace", "revision", "artifact");
            var projectIds = FindProjectIds(arguments);
            if (projectIds.Any(projectId => !allowed.Contains(projectId)))
            {
                denialReason = "The requested tool arguments reference a project that is outside the MCP gateway allow-list.";
                return false;
            }

            if (projectScoped && projectIds.Count == 0)
            {
                denialReason = "A project allow-list is active. Project/workspace/revision/artifact MCP tools must include an explicit projectId argument.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LocalGPT MCP tool project-allow-list evaluation failed; tool arguments were omitted from logs.");
            denialReason = "The LocalGPT MCP gateway could not verify the project allow-list for this tool call.";
            return false;
        }
    }

    /// <summary>Determines whether a project identifier passes the configured gateway allow-list.</summary>
    public bool IsProjectAllowed(Guid projectId)
    {
        try
        {
            var allowed = ParseProjectAllowList();
            return allowed.Count == 0 || allowed.Contains(projectId);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Lists project metadata without exposing file-system paths unless explicitly enabled.</summary>
    public async Task<string> ListProjectsAsync(string? search, CancellationToken cancellationToken)
    {
        if (!Options.ExposeProjects)
            return Disabled("projects");

        try
        {
            var allowed = ParseProjectAllowList();
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var query = db.LocalGptProjects.AsNoTracking();
            if (!Options.IncludeArchivedProjects)
                query = query.Where(project => !project.IsArchived);
            if (allowed.Count > 0)
                query = query.Where(project => allowed.Contains(project.Id));
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(project => project.Name.Contains(term) || project.Purpose.Contains(term) || project.ProjectType.Contains(term));
            }

            var rows = await query
                .OrderByDescending(project => project.UpdatedAtUtc)
                .Take(MaxListItems)
                .Select(project => new
                {
                    project.Id,
                    project.Name,
                    project.Purpose,
                    project.ProjectType,
                    project.CurrentVersion,
                    project.Status,
                    project.IsArchived,
                    project.CreatedAtUtc,
                    project.UpdatedAtUtc,
                    RootPath = Options.ExposeSystemPaths ? project.RootPath : null,
                    SolutionPath = Options.ExposeSystemPaths ? project.SolutionPath : null
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            return Serialize(rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LocalGPT MCP gateway data operation failed with code project_list_failed; requested data and paths were omitted from logs.");
            return SerializeError("project_list_failed", "The LocalGPT MCP data operation failed. Review LocalGPT logs.");
        }
    }

    /// <summary>Returns one project and compact counts for its structured LocalGPT data domain.</summary>
    public async Task<string> GetProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (!Options.ExposeProjects)
            return Disabled("projects");
        if (!IsProjectAllowed(projectId))
            return Denied("project_not_allowed");

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var project = await db.LocalGptProjects.AsNoTracking()
                .Where(item => item.Id == projectId)
                .Select(item => new
                {
                    item.Id,
                    item.Name,
                    item.Purpose,
                    item.ProjectType,
                    item.CurrentVersion,
                    item.Status,
                    item.RecommendGit,
                    item.IsArchived,
                    item.CreatedAtUtc,
                    item.UpdatedAtUtc,
                    item.SolutionSearchPattern,
                    item.FileIncludePattern,
                    item.FileExcludePattern,
                    RootPath = Options.ExposeSystemPaths ? item.RootPath : null,
                    SolutionPath = Options.ExposeSystemPaths ? item.SolutionPath : null,
                    TopicCount = item.Topics.Count,
                    VersionCount = item.Versions.Count,
                    RevisionCount = item.Revisions.Count,
                    RequirementCount = item.Requirements.Count,
                    ArtifactCount = item.Artifacts.Count,
                    WorkspaceRootCount = item.WorkspaceRoots.Count,
                    TrackedFileCount = item.TrackedFiles.Count
                })
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            return project is null ? NotFound("project", projectId) : Serialize(project);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LocalGPT MCP gateway data operation failed with code project_get_failed; requested data and paths were omitted from logs.");
            return SerializeError("project_get_failed", "The LocalGPT MCP data operation failed. Review LocalGPT logs.");
        }
    }

    /// <summary>Lists persisted project revisions, including structural JSON but redacting source paths by default.</summary>
    public async Task<string> ListProjectRevisionsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (!Options.ExposeProjects)
            return Disabled("projects");
        if (!IsProjectAllowed(projectId))
            return Denied("project_not_allowed");

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var rows = await db.LocalGptProjectRevisions.AsNoTracking()
                .Where(item => item.ProjectId == projectId)
                .OrderByDescending(item => item.CreatedAtUtc)
                .Take(MaxListItems)
                .Select(item => new
                {
                    item.Id,
                    item.ProjectId,
                    item.ParentRevisionId,
                    item.BranchName,
                    item.RevisionName,
                    item.Summary,
                    item.ProjectStructureJson,
                    item.CreatedBy,
                    item.IsCurrent,
                    item.IsUserApproved,
                    item.CompileVerified,
                    item.CouncilVerified,
                    item.ReadyForTesting,
                    item.CreatedAtUtc,
                    item.UpdatedAtUtc,
                    item.SourceSnapshotHash,
                    SnapshotArchivePath = Options.ExposeSystemPaths ? item.SnapshotArchivePath : null,
                    SourceRootPath = Options.ExposeSystemPaths ? item.SourceRootPath : null,
                    SolutionPath = Options.ExposeSystemPaths ? item.SolutionPath : null
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            return Serialize(rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LocalGPT MCP gateway data operation failed with code project_revision_list_failed; requested data and paths were omitted from logs.");
            return SerializeError("project_revision_list_failed", "The LocalGPT MCP data operation failed. Review LocalGPT logs.");
        }
    }

    /// <summary>Lists project artifact metadata and only exposes persisted artifact values when explicitly enabled.</summary>
    public async Task<string> ListProjectArtifactsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (!Options.ExposeProjects || !Options.ExposeArtifacts)
            return Disabled("artifacts");
        if (!IsProjectAllowed(projectId))
            return Denied("project_not_allowed");

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var query = db.LocalGptProjectArtifacts.AsNoTracking().Where(item => item.ProjectId == projectId);
            if (!Options.ExposeSensitiveArtifacts)
                query = query.Where(item => !item.IsSensitive);
            var rows = await query
                .OrderByDescending(item => item.UpdatedAtUtc)
                .Take(MaxListItems)
                .Select(item => new
                {
                    item.Id,
                    item.ProjectId,
                    item.RevisionId,
                    item.RequirementId,
                    item.ArtifactKind,
                    item.Name,
                    Value = Options.ExposeArtifactValues ? item.Value : null,
                    item.DataType,
                    item.Flags,
                    item.Description,
                    item.CouncilReviewStatus,
                    item.IsSensitive,
                    item.IsUserApproved,
                    item.CreatedAtUtc,
                    item.UpdatedAtUtc
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            return Serialize(rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LocalGPT MCP gateway data operation failed with code project_artifact_list_failed; requested data and paths were omitted from logs.");
            return SerializeError("project_artifact_list_failed", "The LocalGPT MCP data operation failed. Review LocalGPT logs.");
        }
    }

    /// <summary>Searches Council knowledge while respecting archived/content exposure settings.</summary>
    public async Task<string> SearchKnowledgeAsync(string? search, CancellationToken cancellationToken)
    {
        if (!Options.ExposeKnowledge)
            return Disabled("knowledge");

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var query = db.CouncilKnowledgeEntries.AsNoTracking();
            if (!Options.IncludeArchivedProjects)
                query = query.Where(item => !item.IsArchived);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(item => item.Topic.Contains(term) || item.Scope.Contains(term) || item.Tags.Contains(term) || item.Content.Contains(term));
            }
            var rows = await query
                .OrderByDescending(item => item.IsPinned)
                .ThenByDescending(item => item.UpdatedAtUtc)
                .Take(MaxListItems)
                .Select(item => new
                {
                    item.Id,
                    item.Topic,
                    item.Scope,
                    Content = Options.ExposeKnowledgeContent ? item.Content : null,
                    item.Source,
                    item.HelpfulSources,
                    item.Tags,
                    item.Confidence,
                    item.VerificationStatus,
                    item.ReviewStatus,
                    item.IsUserApproved,
                    item.IsPinned,
                    item.IsArchived,
                    item.UpdatedAtUtc,
                    item.LastVerifiedAtUtc
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            return Serialize(rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LocalGPT MCP gateway data operation failed with code knowledge_search_failed; requested data and paths were omitted from logs.");
            return SerializeError("knowledge_search_failed", "The LocalGPT MCP data operation failed. Review LocalGPT logs.");
        }
    }

    /// <summary>Returns one Council knowledge record.</summary>
    public async Task<string> GetKnowledgeAsync(Guid knowledgeId, CancellationToken cancellationToken)
    {
        if (!Options.ExposeKnowledge)
            return Disabled("knowledge");

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var row = await db.CouncilKnowledgeEntries.AsNoTracking()
                .Where(item => item.Id == knowledgeId && (Options.IncludeArchivedProjects || !item.IsArchived))
                .Select(item => new
                {
                    item.Id,
                    item.Topic,
                    item.Scope,
                    Content = Options.ExposeKnowledgeContent ? item.Content : null,
                    item.Source,
                    item.HelpfulSources,
                    item.Tags,
                    item.Confidence,
                    item.VerificationStatus,
                    item.ReviewStatus,
                    item.ExpiresAtUtc,
                    item.LastVerifiedAtUtc,
                    item.LastUsedAtUtc,
                    item.SupersededByKnowledgeId,
                    item.StalenessReason,
                    item.SourceDateUtc,
                    item.IsUserApproved,
                    item.IsPinned,
                    item.IsArchived,
                    item.CreatedAtUtc,
                    item.UpdatedAtUtc
                })
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            return row is null ? NotFound("knowledge", knowledgeId) : Serialize(row);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LocalGPT MCP gateway data operation failed with code knowledge_get_failed; requested data and paths were omitted from logs.");
            return SerializeError("knowledge_get_failed", "The LocalGPT MCP data operation failed. Review LocalGPT logs.");
        }
    }

    /// <summary>Lists reusable regex records used by LocalGPT project/knowledge recognition.</summary>
    public async Task<string> ListRegexAsync(string? search, CancellationToken cancellationToken)
    {
        if (!Options.ExposeRegex)
            return Disabled("regex");

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var query = db.RegexPatterns.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(item => item.Name.Contains(term) || item.Pattern.Contains(term));
            }
            var rows = await query.OrderBy(item => item.Name).Take(MaxListItems)
                .Select(item => new { item.Id, item.Name, item.Pattern, item.Flags, item.CreatedOn, item.UpdatedOn })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            return Serialize(rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LocalGPT MCP gateway data operation failed with code regex_list_failed; requested data and paths were omitted from logs.");
            return SerializeError("regex_list_failed", "The LocalGPT MCP data operation failed. Review LocalGPT logs.");
        }
    }

    /// <summary>Lists persisted compiler/runtime/toolchain installations with path and environment redaction controls.</summary>
    public async Task<string> ListToolchainsAsync(string? language, CancellationToken cancellationToken)
    {
        if (!Options.ExposeToolchains)
            return Disabled("toolchains");

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var query = db.ProjectCompilerInstallations.AsNoTracking().Where(item => item.IsEnabled);
            if (!string.IsNullOrWhiteSpace(language))
            {
                var term = language.Trim();
                query = query.Where(item => item.Language.Contains(term) || item.Name.Contains(term) || item.ToolchainKind.Contains(term));
            }
            var rows = await query.OrderBy(item => item.Language).ThenByDescending(item => item.IsDefaultForLanguage).Take(MaxListItems)
                .Select(item => new
                {
                    item.Id,
                    item.Name,
                    item.Language,
                    ExecutablePath = Options.ExposeSystemPaths ? item.ExecutablePath : null,
                    CompilerHomePath = Options.ExposeSystemPaths ? item.CompilerHomePath : null,
                    item.Version,
                    item.Architecture,
                    item.DiscoverySource,
                    item.ToolchainKind,
                    item.DetectedPlatform,
                    item.ValidationArguments,
                    EnvironmentVariablesJson = Options.ExposeToolchainEnvironmentVariables ? item.EnvironmentVariablesJson : null,
                    item.KnowledgeProfileKey,
                    item.KnowledgeEntryId,
                    item.VersionKnowledgeEntryId,
                    item.IsDefaultForLanguage,
                    item.LastValidatedAtUtc,
                    item.LastValidationSucceeded,
                    item.LastValidationMessage
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            return Serialize(rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LocalGPT MCP gateway data operation failed with code toolchain_list_failed; requested data and paths were omitted from logs.");
            return SerializeError("toolchain_list_failed", "The LocalGPT MCP data operation failed. Review LocalGPT logs.");
        }
    }

    /// <summary>Shapes an arbitrary DX function result according to MCP path/download-result policy.</summary>
    public string SerializeInvocationResult(DxAiFunctionInvocationResult result, DxaichatFunctionInfo? function = null)
    {
        try
        {
            var value = result.Value is null ? null : JsonSerializer.SerializeToNode(result.Value, JsonOptions);
            var artifactResult = function is not null && MatchesCategory(function, "artifact");
            var knowledgeResult = function is not null && MatchesAnyCategory(function, "knowledge", "learningbase", "learning-base");
            var toolchainResult = function is not null && MatchesAnyCategory(function, "toolchain", "compiler-installation", "compilerinstallation");
            value = RedactNode(value, artifactResult, knowledgeResult, toolchainResult);
            return Serialize(new
            {
                result.FunctionName,
                result.Succeeded,
                result.Status,
                value,
                result.Error,
                result.OperationId,
                approval = result.Succeeded ? "not-required-or-satisfied" : "LocalGPT remains authoritative for confirmation-gated operations; approve in LocalGPT when requested and retry with the same operationId."
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LocalGPT MCP gateway data operation failed with code function_result_serialization_failed; requested data and paths were omitted from logs.");
            return SerializeError("function_result_serialization_failed", "The LocalGPT MCP data operation failed. Review LocalGPT logs.");
        }
    }

    private JsonNode? RedactNode(JsonNode? node, bool artifactResult, bool knowledgeResult, bool toolchainResult)
    {
        if (node is JsonValue scalar && scalar.TryGetValue<string>(out var scalarText))
        {
            if (!Options.ExposeSystemPaths && LooksLikeSystemPath(scalarText))
                return JsonValue.Create("[local path hidden by LocalGPT MCP gateway]");
            if (!Options.ReturnDownloadUrls && LooksLikeDownloadUrl(scalarText))
                return JsonValue.Create("[download URL hidden by LocalGPT MCP gateway]");
            return node;
        }

        if (node is JsonObject obj)
        {
            var sensitiveArtifact = artifactResult && !Options.ExposeSensitiveArtifacts &&
                obj.Any(pair => NormalizeKey(pair.Key) == "issensitive" && pair.Value is JsonValue flag && flag.TryGetValue<bool>(out var sensitive) && sensitive);
            foreach (var pair in obj.ToList())
            {
                var normalized = NormalizeKey(pair.Key);
                if (!Options.ReturnDownloadUrls && IsDownloadKey(normalized, pair.Value))
                {
                    obj[pair.Key] = "[download URL hidden by LocalGPT MCP gateway]";
                    continue;
                }
                if (toolchainResult && !Options.ExposeToolchainEnvironmentVariables && IsEnvironmentPayloadKey(normalized))
                {
                    obj[pair.Key] = "[toolchain environment hidden by LocalGPT MCP gateway]";
                    continue;
                }
                if (artifactResult && (!Options.ExposeArtifactValues || sensitiveArtifact) && IsPayloadValueKey(normalized))
                {
                    obj[pair.Key] = sensitiveArtifact
                        ? "[sensitive artifact payload hidden by LocalGPT MCP gateway]"
                        : "[artifact payload hidden by LocalGPT MCP gateway]";
                    continue;
                }
                if (knowledgeResult && !Options.ExposeKnowledgeContent && IsKnowledgeContentKey(normalized))
                {
                    obj[pair.Key] = "[knowledge content hidden by LocalGPT MCP gateway]";
                    continue;
                }
                var redactedChild = RedactNode(pair.Value, artifactResult, knowledgeResult, toolchainResult);
                if (!ReferenceEquals(redactedChild, pair.Value))
                    obj[pair.Key] = redactedChild;
            }
            return obj;
        }

        if (node is JsonArray array)
        {
            for (var index = 0; index < array.Count; index++)
            {
                var child = array[index];
                var redactedChild = RedactNode(child, artifactResult, knowledgeResult, toolchainResult);
                if (!ReferenceEquals(redactedChild, child))
                    array[index] = redactedChild;
            }
            return array;
        }

        return node;
    }

    private string NormalizeKey(string value) => value.Replace("_", string.Empty, StringComparison.Ordinal)
        .Replace("-", string.Empty, StringComparison.Ordinal)
        .ToLowerInvariant();

    private bool IsEnvironmentPayloadKey(string normalized) => normalized.Contains("environmentvariable", StringComparison.Ordinal) ||
        normalized is "env" or "envvars" or "environment" or "environmentjson";

    private bool IsPayloadValueKey(string normalized) => normalized is "value" or "content" or "data" or "payload" or "body" or "text" or "bytes";

    private bool IsKnowledgeContentKey(string normalized) => normalized is "content" or "body" or "text" or "value" or "payload";

    private bool IsDownloadKey(string normalized, JsonNode? value)
    {
        if (normalized.Contains("downloadurl", StringComparison.Ordinal) || normalized.Contains("downloaduri", StringComparison.Ordinal))
            return true;
        return normalized is "url" or "uri" && value is JsonValue scalar && scalar.TryGetValue<string>(out var text) && LooksLikeDownloadUrl(text);
    }

    private bool LooksLikeDownloadUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value, UriKind.Absolute, out var uri) || !(uri.Scheme is "http" or "https"))
            return false;
        return uri.AbsolutePath.Contains("download", StringComparison.OrdinalIgnoreCase) ||
               uri.AbsolutePath.Contains("artifact", StringComparison.OrdinalIgnoreCase) ||
               uri.Query.Contains("download", StringComparison.OrdinalIgnoreCase);
    }

    private string Serialize(object? value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        if (json.Length <= MaxResultCharacters)
            return json;
        var budget = Math.Max(256, MaxResultCharacters - 512);
        var preview = json[..Math.Min(json.Length, budget)];
        return JsonSerializer.Serialize(new
        {
            truncated = true,
            maxResultCharacters = MaxResultCharacters,
            originalCharacters = json.Length,
            preview
        }, JsonOptions);
    }

    private string Disabled(string domain) => Serialize(new { error = "mcp_domain_disabled", domain });
    private string Denied(string reason) => Serialize(new { error = "mcp_access_denied", reason });
    private string NotFound(string kind, Guid id) => Serialize(new { error = "not_found", kind, id });
    private string SerializeError(string code, string message) => Serialize(new { error = code, message });

    private HashSet<Guid> ParseProjectAllowList()
    {
        var values = Split(Options.AllowedProjectIds);
        var result = new HashSet<Guid>();
        foreach (var value in values)
        {
            if (!Guid.TryParse(value, out var id) || id == Guid.Empty)
                throw new FormatException("The MCP project allow-list contains a value that is not a non-empty GUID.");
            result.Add(id);
        }
        return result;
    }

    private List<string> Split(string? value) => string.IsNullOrWhiteSpace(value)
        ? []
        : value.Split([';', ',', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private IReadOnlyList<Guid> FindProjectIds(JsonElement arguments)
    {
        var result = new List<Guid>();
        if (arguments.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var property in arguments.EnumerateObject())
        {
            var normalized = property.Name.Replace("_", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
            if (!normalized.Equals("projectid", StringComparison.OrdinalIgnoreCase) &&
                !normalized.Equals("localgptprojectid", StringComparison.OrdinalIgnoreCase))
                continue;

            if (property.Value.ValueKind == JsonValueKind.String && Guid.TryParse(property.Value.GetString(), out var parsed))
                result.Add(parsed);
        }

        return result;
    }

    private bool MatchesPrefix(DxaichatFunctionInfo function, IReadOnlyCollection<string> prefixes) =>
        prefixes.Any(prefix => function.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || function.Route.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private bool MatchesCategory(DxaichatFunctionInfo function, string value) =>
        function.Name.Contains(value, StringComparison.OrdinalIgnoreCase) || function.Route.Contains(value, StringComparison.OrdinalIgnoreCase) || function.Source.Contains(value, StringComparison.OrdinalIgnoreCase);

    private bool MatchesAnyCategory(DxaichatFunctionInfo function, params string[] values) => values.Any(value => MatchesCategory(function, value));

    private bool LooksLikeSystemPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            return false;
        return Path.IsPathFullyQualified(value) || value.StartsWith("\\\\", StringComparison.Ordinal);
    }
}
