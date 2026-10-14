using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Mcp;

/// <summary>
/// Owns MCP gateway draft cloning and normalization so UI components only coordinate presentation and persistence.
/// </summary>
/// <param name="runtimePolicy">Local gpt runtime policy data service dependency used by the local GPT mcp configuration policy workflow to provide the corresponding application capability.</param>
public sealed class LocalGptMcpConfigurationPolicy(ILocalGptRuntimePolicyDataService runtimePolicy)
{
    /// <summary>
    /// Gets the parameters value that forms part of the local GPT mcp configuration policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parameters value exposed by <see cref="LocalGptMcpConfigurationPolicy"/>.</value>
    private McpGatewayRuntimeParameters Parameters => runtimePolicy.GetJson<McpGatewayRuntimeParameters>(LocalGptRuntimeValue.McpGatewayRuntimeParametersJson);
    /// <summary>Creates a detached MCP gateway option copy for editable UI state.</summary>
    /// <param name="source">Source value supplied to the local GPT mcp configuration policy operation and used when producing its result.</param>
    /// <returns>The mcp gateway options produced by the operation.</returns>
    public McpGatewayOptions Clone(McpGatewayOptions? source)
    {
        source ??= new McpGatewayOptions();
        return new McpGatewayOptions
        {
            Enabled = source.Enabled,
            DedicatedListenerEnabled = source.DedicatedListenerEnabled,
            ExposeOnPrimaryEndpoint = source.ExposeOnPrimaryEndpoint,
            Address = source.Address,
            Port = source.Port,
            Path = source.Path,
            CertificatePath = source.CertificatePath,
            CertificatePassword = source.CertificatePassword,
            AllowRemoteClients = source.AllowRemoteClients,
            RequireApiKey = source.RequireApiKey,
            ApiKey = source.ApiKey,
            ApiKeyHeader = source.ApiKeyHeader,
            AcceptBearerToken = source.AcceptBearerToken,
            EnableModernProtocol = source.EnableModernProtocol,
            ProtocolVersion = source.ProtocolVersion,
            EnableLegacyProtocol = source.EnableLegacyProtocol,
            LegacyProtocolVersion = source.LegacyProtocolVersion,
            RequireModernRoutingHeaders = source.RequireModernRoutingHeaders,
            EnableApprovalMultiRoundTrip = source.EnableApprovalMultiRoundTrip,
            CacheTtlMs = source.CacheTtlMs,
            CacheScope = source.CacheScope,
            AllowedHosts = source.AllowedHosts,
            AllowedOrigins = source.AllowedOrigins,
            MaxRequestBodyBytes = source.MaxRequestBodyBytes,
            ExposeTools = source.ExposeTools,
            ExposeResources = source.ExposeResources,
            ExposePrompts = source.ExposePrompts,
            ExposeReadOnlyFunctions = source.ExposeReadOnlyFunctions,
            ExposeMutatingFunctions = source.ExposeMutatingFunctions,
            ExposeConfirmationRequiredFunctions = source.ExposeConfirmationRequiredFunctions,
            AllowAutomaticFunctionInvocation = source.AllowAutomaticFunctionInvocation,
            ExposeProjects = source.ExposeProjects,
            ExposeKnowledge = source.ExposeKnowledge,
            ExposeKnowledgeContent = source.ExposeKnowledgeContent,
            ExposeRegex = source.ExposeRegex,
            ExposeToolchains = source.ExposeToolchains,
            ExposeArtifacts = source.ExposeArtifacts,
            ExposeArtifactValues = source.ExposeArtifactValues,
            ExposeSensitiveArtifacts = source.ExposeSensitiveArtifacts,
            ExposeToolchainEnvironmentVariables = source.ExposeToolchainEnvironmentVariables,
            ExposeCouncil = source.ExposeCouncil,
            ExposeRuntimePolicy = source.ExposeRuntimePolicy,
            ExposeDiagnostics = source.ExposeDiagnostics,
            ExposeSystemPaths = source.ExposeSystemPaths,
            IncludeArchivedProjects = source.IncludeArchivedProjects,
            AllowedProjectIds = source.AllowedProjectIds,
            ToolIncludePrefixes = source.ToolIncludePrefixes,
            ToolExcludePrefixes = source.ToolExcludePrefixes,
            MaxListItems = source.MaxListItems,
            MaxResultCharacters = source.MaxResultCharacters,
            ReturnDownloadUrls = source.ReturnDownloadUrls
        };
    }

    /// <summary>Normalizes and bounds an editable MCP gateway draft before persistence or preview.</summary>
    /// <param name="source">Source value supplied to the local GPT mcp configuration policy operation and used when producing its result.</param>
    /// <returns>The mcp gateway options produced by the operation.</returns>
    public McpGatewayOptions NormalizeDraft(McpGatewayOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var parameters = Parameters;
        var result = Clone(source);
        result.Address = string.IsNullOrWhiteSpace(result.Address) ? parameters.DefaultAddress : result.Address.Trim();
        result.Port = result.Port >= parameters.MinimumPort && result.Port <= parameters.MaximumPort ? result.Port : parameters.DefaultPort;
        result.Path = NormalizePath(result.Path);
        result.CertificatePath = result.CertificatePath?.Trim() ?? string.Empty;
        result.CertificatePassword ??= string.Empty;
        result.ApiKey = result.ApiKey?.Trim() ?? string.Empty;
        result.ApiKeyHeader = string.IsNullOrWhiteSpace(result.ApiKeyHeader) ? parameters.DefaultApiKeyHeader : result.ApiKeyHeader.Trim();
        result.ProtocolVersion = string.IsNullOrWhiteSpace(result.ProtocolVersion) ? parameters.ModernProtocolVersion : result.ProtocolVersion.Trim();
        result.LegacyProtocolVersion = string.IsNullOrWhiteSpace(result.LegacyProtocolVersion) ? parameters.LegacyProtocolVersion : result.LegacyProtocolVersion.Trim();
        if (parameters.EnableModernProtocolWhenNoneSelected && !result.EnableModernProtocol && !result.EnableLegacyProtocol)
            result.EnableModernProtocol = true;
        result.CacheTtlMs = Math.Clamp(result.CacheTtlMs, parameters.MinimumCacheTtlMilliseconds, parameters.MaximumCacheTtlMilliseconds);
        result.CacheScope = string.Equals(result.CacheScope, parameters.PublicCacheScope, StringComparison.OrdinalIgnoreCase)
            ? parameters.PublicCacheScope
            : parameters.PrivateCacheScope;
        result.AllowedHosts = string.IsNullOrWhiteSpace(result.AllowedHosts) ? parameters.DefaultAllowedHosts : result.AllowedHosts.Trim();
        result.AllowedOrigins = result.AllowedOrigins?.Trim() ?? string.Empty;
        if (parameters.RequireApiKeyForRemoteClients && result.AllowRemoteClients)
            result.RequireApiKey = true;
        if (parameters.EnableDedicatedListenerWhenNoEndpointSelected && result.Enabled && !result.DedicatedListenerEnabled && !result.ExposeOnPrimaryEndpoint)
            result.DedicatedListenerEnabled = true;
        result.AllowedProjectIds = result.AllowedProjectIds?.Trim() ?? string.Empty;
        result.ToolIncludePrefixes = result.ToolIncludePrefixes?.Trim() ?? string.Empty;
        result.ToolExcludePrefixes = result.ToolExcludePrefixes?.Trim() ?? string.Empty;
        result.MaxRequestBodyBytes = Math.Clamp(result.MaxRequestBodyBytes, parameters.MinimumRequestBodyBytes, parameters.MaximumRequestBodyBytes);
        result.MaxListItems = Math.Clamp(result.MaxListItems, parameters.MinimumListItems, parameters.MaximumListItems);
        result.MaxResultCharacters = Math.Clamp(result.MaxResultCharacters, parameters.MinimumResultCharacters, parameters.MaximumResultCharacters);
        return result;
    }

    /// <summary>Creates a cryptographically random API key using the database-backed MCP key-size policy.</summary>
    /// <returns>The string produced by the operation.</returns>
    public string GenerateApiKey()
    {
        return Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(Parameters.ApiKeyBytes));
    }

    /// <summary>Normalizes an MCP HTTP route and keeps it under the MCP route namespace.</summary>
    /// <param name="value">Value value supplied to the local GPT mcp configuration policy operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizePath(string? value)
    {
        var rootPath = Parameters.RootPath;
        var path = string.IsNullOrWhiteSpace(value) ? rootPath : value.Trim();
        if (!path.StartsWith('/'))
            path = "/" + path;
        if (path.Length > 1)
            path = path.TrimEnd('/');
        if (path.Equals("/", StringComparison.Ordinal))
            return rootPath;
        var rootedPrefix = rootPath.EndsWith("/", StringComparison.Ordinal) ? rootPath : rootPath + "/";
        if (!path.Equals(rootPath, StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith(rootedPrefix, StringComparison.OrdinalIgnoreCase))
            path = rootPath + path;
        return path;
    }
}
