using LocalGPT.BusinessObjects;

namespace LocalGPT.Mcp;

/// <summary>
/// Owns MCP gateway draft cloning and normalization so UI components only coordinate presentation and persistence.
/// </summary>
public sealed class LocalGptMcpConfigurationPolicy
{
    /// <summary>Creates a detached MCP gateway option copy for editable UI state.</summary>
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
    public McpGatewayOptions NormalizeDraft(McpGatewayOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var result = Clone(source);
        result.Address = string.IsNullOrWhiteSpace(result.Address) ? "127.0.0.1" : result.Address.Trim();
        result.Port = result.Port is >= 1 and <= 65535 ? result.Port : McpGatewayOptions.DefaultPort;
        result.Path = NormalizePath(result.Path);
        result.CertificatePath = result.CertificatePath?.Trim() ?? string.Empty;
        result.CertificatePassword ??= string.Empty;
        result.ApiKey = result.ApiKey?.Trim() ?? string.Empty;
        result.ApiKeyHeader = string.IsNullOrWhiteSpace(result.ApiKeyHeader) ? "X-LocalGPT-MCP-Key" : result.ApiKeyHeader.Trim();
        result.ProtocolVersion = string.IsNullOrWhiteSpace(result.ProtocolVersion) ? "2026-07-28" : result.ProtocolVersion.Trim();
        result.LegacyProtocolVersion = string.IsNullOrWhiteSpace(result.LegacyProtocolVersion) ? "2025-11-25" : result.LegacyProtocolVersion.Trim();
        if (!result.EnableModernProtocol && !result.EnableLegacyProtocol)
            result.EnableModernProtocol = true;
        result.CacheTtlMs = Math.Clamp(result.CacheTtlMs, 0, 86_400_000);
        result.CacheScope = string.Equals(result.CacheScope, "public", StringComparison.OrdinalIgnoreCase) ? "public" : "private";
        result.AllowedHosts = string.IsNullOrWhiteSpace(result.AllowedHosts) ? "localhost;127.0.0.1;::1;[::1]" : result.AllowedHosts.Trim();
        result.AllowedOrigins = result.AllowedOrigins?.Trim() ?? string.Empty;
        if (result.AllowRemoteClients)
            result.RequireApiKey = true;
        if (result.Enabled && !result.DedicatedListenerEnabled && !result.ExposeOnPrimaryEndpoint)
            result.DedicatedListenerEnabled = true;
        result.AllowedProjectIds = result.AllowedProjectIds?.Trim() ?? string.Empty;
        result.ToolIncludePrefixes = result.ToolIncludePrefixes?.Trim() ?? string.Empty;
        result.ToolExcludePrefixes = result.ToolExcludePrefixes?.Trim() ?? string.Empty;
        result.MaxRequestBodyBytes = Math.Clamp(result.MaxRequestBodyBytes, 1024, 64 * 1024 * 1024);
        result.MaxListItems = Math.Clamp(result.MaxListItems, 1, 10000);
        result.MaxResultCharacters = Math.Clamp(result.MaxResultCharacters, 1024, 8_000_000);
        return result;
    }

    /// <summary>Creates a cryptographically random 256-bit API key suitable for MCP client admission.</summary>
    public string GenerateApiKey()
    {
        return Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    }

    /// <summary>Normalizes an MCP HTTP route and keeps it under the MCP route namespace.</summary>
    public string NormalizePath(string? value)
    {
        var path = string.IsNullOrWhiteSpace(value) ? "/mcp" : value.Trim();
        if (!path.StartsWith('/'))
            path = "/" + path;
        if (path.Length > 1)
            path = path.TrimEnd('/');
        if (path.Equals("/", StringComparison.Ordinal))
            return "/mcp";
        if (!path.Equals("/mcp", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/mcp/", StringComparison.OrdinalIgnoreCase))
            path = "/mcp" + path;
        return path;
    }
}
