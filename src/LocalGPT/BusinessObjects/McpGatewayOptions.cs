namespace LocalGPT.BusinessObjects;

/// <summary>
/// Controls the LocalGPT Model Context Protocol gateway. The gateway is opt-in and can be isolated on a dedicated
/// listener so MCP clients do not automatically receive the rest of the LocalGPT web application surface.
/// </summary>
public sealed class McpGatewayOptions
{
    /// <summary>Configuration section used by the MCP gateway.</summary>
    public const string SectionName = "LocalGPT:McpGateway";

    /// <summary>Default dedicated TCP port used when the MCP gateway is enabled.</summary>
    public const int DefaultPort = 51142;

    /// <summary>Gets or sets whether MCP is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets whether a dedicated Kestrel listener is created for MCP traffic.</summary>
    public bool DedicatedListenerEnabled { get; set; } = true;

    /// <summary>Gets or sets whether the MCP path is also accepted on the authoritative LocalGPT web listener.</summary>
    public bool ExposeOnPrimaryEndpoint { get; set; }

    /// <summary>Gets or sets the IP address used by the dedicated MCP listener.</summary>
    public string Address { get; set; } = "127.0.0.1";

    /// <summary>Gets or sets the dedicated MCP listener port.</summary>
    public int Port { get; set; } = DefaultPort;

    /// <summary>Gets or sets the Streamable HTTP endpoint path.</summary>
    public string Path { get; set; } = "/mcp";

    /// <summary>Gets or sets an optional PFX/PKCS#12 certificate path for the dedicated listener.</summary>
    public string CertificatePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional PFX password.</summary>
    public string CertificatePassword { get; set; } = string.Empty;

    /// <summary>Gets or sets whether non-loopback clients may call the MCP endpoint.</summary>
    public bool AllowRemoteClients { get; set; }

    /// <summary>Gets or sets whether an API key must be supplied for MCP requests.</summary>
    public bool RequireApiKey { get; set; }

    /// <summary>Gets or sets the API key. Prefer LOCALGPT_MCP_API_KEY for reusable deployments.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the request header that carries the LocalGPT MCP API key.</summary>
    public string ApiKeyHeader { get; set; } = "X-LocalGPT-MCP-Key";

    /// <summary>Gets or sets whether the same API key may also be supplied as an Authorization Bearer token.</summary>
    public bool AcceptBearerToken { get; set; } = true;

    /// <summary>Gets or sets whether the stateless MCP 2026-era request envelope is accepted.</summary>
    public bool EnableModernProtocol { get; set; } = true;

    /// <summary>Gets or sets the current stateless MCP protocol revision advertised through server/discover.</summary>
    public string ProtocolVersion { get; set; } = "2026-07-28";

    /// <summary>Gets or sets whether initialize-handshake clients may use the same endpoint in stateless compatibility mode.</summary>
    public bool EnableLegacyProtocol { get; set; } = true;

    /// <summary>Gets or sets the legacy initialize-handshake revision offered to down-level clients.</summary>
    public string LegacyProtocolVersion { get; set; } = "2025-11-25";

    /// <summary>Gets or sets whether modern Streamable HTTP routing headers are required and validated against the JSON-RPC body.</summary>
    public bool RequireModernRoutingHeaders { get; set; } = true;

    /// <summary>Gets or sets whether modern MCP approval waits are surfaced through standard multi-round-trip input_required results.</summary>
    public bool EnableApprovalMultiRoundTrip { get; set; } = true;

    /// <summary>Gets or sets the cache freshness hint, in milliseconds, emitted on modern discovery/list/read results.</summary>
    public int CacheTtlMs { get; set; }

    /// <summary>Gets or sets the modern MCP cache scope. Use private for authorization-dependent LocalGPT data.</summary>
    public string CacheScope { get; set; } = "private";

    /// <summary>Gets or sets the semicolon/comma/newline separated HTTP Host values accepted by the MCP gateway.</summary>
    public string AllowedHosts { get; set; } = "localhost;127.0.0.1;::1;[::1]";

    /// <summary>Gets or sets the explicitly allowed browser origins. Empty means cross-origin browser requests are rejected.</summary>
    public string AllowedOrigins { get; set; } = string.Empty;

    /// <summary>Gets or sets the maximum MCP request body size in bytes.</summary>
    public int MaxRequestBodyBytes { get; set; } = 4 * 1024 * 1024;

    /// <summary>Gets or sets whether MCP tools are registered.</summary>
    public bool ExposeTools { get; set; } = true;

    /// <summary>Gets or sets whether MCP resources are registered.</summary>
    public bool ExposeResources { get; set; } = true;

    /// <summary>Gets or sets whether MCP prompt templates are registered.</summary>
    public bool ExposePrompts { get; set; } = true;

    /// <summary>Gets or sets whether read-only DX AI functions may be surfaced through the MCP bridge.</summary>
    public bool ExposeReadOnlyFunctions { get; set; } = true;

    /// <summary>Gets or sets whether mutating DX AI functions may be surfaced through the MCP bridge.</summary>
    public bool ExposeMutatingFunctions { get; set; }

    /// <summary>Gets or sets whether confirmation-gated DX AI functions are visible to MCP clients.</summary>
    public bool ExposeConfirmationRequiredFunctions { get; set; } = true;

    /// <summary>Gets or sets whether MCP bridge calls may use the LocalGPT automatic-invocation path.</summary>
    public bool AllowAutomaticFunctionInvocation { get; set; }

    /// <summary>Gets or sets whether project metadata/resources are exposed.</summary>
    public bool ExposeProjects { get; set; } = true;

    /// <summary>Gets or sets whether Council knowledge is exposed.</summary>
    public bool ExposeKnowledge { get; set; } = true;

    /// <summary>Gets or sets whether full Council knowledge content is returned instead of metadata-only records.</summary>
    public bool ExposeKnowledgeContent { get; set; }

    /// <summary>Gets or sets whether reusable regex knowledge is exposed.</summary>
    public bool ExposeRegex { get; set; } = true;

    /// <summary>Gets or sets whether detected/configured toolchains are exposed.</summary>
    public bool ExposeToolchains { get; set; } = true;

    /// <summary>Gets or sets whether project artifacts and artifact metadata are exposed.</summary>
    public bool ExposeArtifacts { get; set; } = true;

    /// <summary>Gets or sets whether persisted artifact values/content are returned instead of metadata-only records.</summary>
    public bool ExposeArtifactValues { get; set; }

    /// <summary>Gets or sets whether artifacts marked sensitive can be returned to MCP clients.</summary>
    public bool ExposeSensitiveArtifacts { get; set; }

    /// <summary>Gets or sets whether configured toolchain environment-variable payloads can be returned.</summary>
    public bool ExposeToolchainEnvironmentVariables { get; set; }

    /// <summary>Gets or sets whether Council-related DX functions can be bridged through MCP.</summary>
    public bool ExposeCouncil { get; set; } = true;

    /// <summary>Gets or sets whether runtime-policy information can be bridged through MCP.</summary>
    public bool ExposeRuntimePolicy { get; set; }

    /// <summary>Gets or sets whether diagnostic functions can be bridged through MCP.</summary>
    public bool ExposeDiagnostics { get; set; }

    /// <summary>Gets or sets whether absolute local file-system paths may be returned to MCP clients.</summary>
    public bool ExposeSystemPaths { get; set; }

    /// <summary>Gets or sets whether archived projects/knowledge are included in MCP list operations.</summary>
    public bool IncludeArchivedProjects { get; set; }

    /// <summary>Gets or sets a GUID allow-list for projects. Empty permits all projects allowed by the other gateway rules.</summary>
    public string AllowedProjectIds { get; set; } = string.Empty;

    /// <summary>Gets or sets optional function-name/route prefixes that are allowed. Empty permits all otherwise allowed functions.</summary>
    public string ToolIncludePrefixes { get; set; } = string.Empty;

    /// <summary>Gets or sets function-name/route prefixes that are always excluded.</summary>
    public string ToolExcludePrefixes { get; set; } = string.Empty;

    /// <summary>Gets or sets the maximum number of records returned by list/search operations.</summary>
    public int MaxListItems { get; set; } = 100;

    /// <summary>Gets or sets the maximum number of characters returned by one MCP helper result.</summary>
    public int MaxResultCharacters { get; set; } = 200000;

    /// <summary>Gets or sets whether generated artifact/download links can be returned when existing LocalGPT tools provide them.</summary>
    public bool ReturnDownloadUrls { get; set; } = true;
}
