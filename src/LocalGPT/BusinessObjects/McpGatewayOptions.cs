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

    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the mcp gateway state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets whether a dedicated isolated Kestrel host is created for MCP traffic.</summary>
    /// <value>The dedicated listener enabled value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool DedicatedListenerEnabled { get; set; } = true;

    /// <summary>Gets or sets whether the MCP path is also accepted on the authoritative LocalGPT web listener.</summary>
    /// <value>The expose on primary endpoint value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeOnPrimaryEndpoint { get; set; }

    /// <summary>Gets or sets the IP address used by the dedicated MCP listener.</summary>
    /// <value>The address value exposed by <see cref="McpGatewayOptions"/>.</value>
    public string Address { get; set; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets the port value that forms part of the mcp gateway state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The port value exposed by <see cref="McpGatewayOptions"/>.</value>
    public int Port { get; set; } = DefaultPort;

    /// <summary>
    /// Gets or sets the path used by this mcp gateway instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path value exposed by <see cref="McpGatewayOptions"/>.</value>
    public string Path { get; set; } = "/mcp";

    /// <summary>Gets or sets an optional PFX/PKCS#12 certificate path for the dedicated listener.</summary>
    /// <value>The certificate path value exposed by <see cref="McpGatewayOptions"/>.</value>
    public string CertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the certificate password value that forms part of the mcp gateway state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The certificate password value exposed by <see cref="McpGatewayOptions"/>.</value>
    public string CertificatePassword { get; set; } = string.Empty;

    /// <summary>Gets or sets whether non-loopback clients may call the MCP endpoint.</summary>
    /// <value>The allow remote clients value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool AllowRemoteClients { get; set; }

    /// <summary>Gets or sets whether an API key must be supplied for MCP requests.</summary>
    /// <value>The require API key value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool RequireApiKey { get; set; }

    /// <summary>Gets or sets the API key. Prefer LOCALGPT_MCP_API_KEY for reusable deployments.</summary>
    /// <value>The API key value exposed by <see cref="McpGatewayOptions"/>.</value>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the request header that carries the LocalGPT MCP API key.</summary>
    /// <value>The API key header value exposed by <see cref="McpGatewayOptions"/>.</value>
    public string ApiKeyHeader { get; set; } = "X-LocalGPT-MCP-Key";

    /// <summary>Gets or sets whether the same API key may also be supplied as an Authorization Bearer token.</summary>
    /// <value>The accept bearer token value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool AcceptBearerToken { get; set; } = true;

    /// <summary>Gets or sets whether the stateless MCP 2026-era request envelope is accepted.</summary>
    /// <value>The enable modern protocol value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool EnableModernProtocol { get; set; } = true;

    /// <summary>Gets or sets the current stateless MCP protocol revision advertised through server/discover.</summary>
    /// <value>The protocol version value exposed by <see cref="McpGatewayOptions"/>.</value>
    public string ProtocolVersion { get; set; } = "2026-07-28";

    /// <summary>Gets or sets whether initialize-handshake clients may use the same endpoint in stateless compatibility mode.</summary>
    /// <value>The enable legacy protocol value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool EnableLegacyProtocol { get; set; } = true;

    /// <summary>Gets or sets the legacy initialize-handshake revision offered to down-level clients.</summary>
    /// <value>The legacy protocol version value exposed by <see cref="McpGatewayOptions"/>.</value>
    public string LegacyProtocolVersion { get; set; } = "2025-11-25";

    /// <summary>Gets or sets whether modern Streamable HTTP routing headers are required and validated against the JSON-RPC body.</summary>
    /// <value>The require modern routing headers value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool RequireModernRoutingHeaders { get; set; } = true;

    /// <summary>Gets or sets whether modern MCP approval waits are surfaced through standard multi-round-trip input_required results.</summary>
    /// <value>The enable approval multi round trip value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool EnableApprovalMultiRoundTrip { get; set; } = true;

    /// <summary>Gets or sets the cache freshness hint, in milliseconds, emitted on modern discovery/list/read results.</summary>
    /// <value>The cache ttl ms value exposed by <see cref="McpGatewayOptions"/>.</value>
    public int CacheTtlMs { get; set; }

    /// <summary>Gets or sets the modern MCP cache scope. Use private for authorization-dependent LocalGPT data.</summary>
    /// <value>The cache scope value exposed by <see cref="McpGatewayOptions"/>.</value>
    public string CacheScope { get; set; } = "private";

    /// <summary>Gets or sets the semicolon/comma/newline separated HTTP Host values accepted by the MCP gateway.</summary>
    /// <value>The allowed hosts value exposed by <see cref="McpGatewayOptions"/>.</value>
    public string AllowedHosts { get; set; } = "localhost;127.0.0.1;::1;[::1]";

    /// <summary>Gets or sets the explicitly allowed browser origins. Empty means cross-origin browser requests are rejected.</summary>
    /// <value>The allowed origins value exposed by <see cref="McpGatewayOptions"/>.</value>
    public string AllowedOrigins { get; set; } = string.Empty;

    /// <summary>Gets or sets the maximum MCP request body size in bytes.</summary>
    /// <value>The max request body bytes value exposed by <see cref="McpGatewayOptions"/>.</value>
    public int MaxRequestBodyBytes { get; set; } = 4 * 1024 * 1024;

    /// <summary>
    /// Gets or sets a value indicating whether expose tools applies to the mcp gateway state.
    /// </summary>
    /// <value>The expose tools value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeTools { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether expose resources applies to the mcp gateway state.
    /// </summary>
    /// <value>The expose resources value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeResources { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether expose prompts applies to the mcp gateway state.
    /// </summary>
    /// <value>The expose prompts value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposePrompts { get; set; } = true;

    /// <summary>Gets or sets whether read-only DX AI functions may be surfaced through the MCP bridge.</summary>
    /// <value>The expose read only functions value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeReadOnlyFunctions { get; set; } = true;

    /// <summary>Gets or sets whether mutating DX AI functions may be surfaced through the MCP bridge.</summary>
    /// <value>The expose mutating functions value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeMutatingFunctions { get; set; }

    /// <summary>Gets or sets whether confirmation-gated DX AI functions are visible to MCP clients.</summary>
    /// <value>The expose confirmation required functions value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeConfirmationRequiredFunctions { get; set; } = true;

    /// <summary>Gets or sets whether MCP bridge calls may use the LocalGPT automatic-invocation path.</summary>
    /// <value>The allow automatic function invocation value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool AllowAutomaticFunctionInvocation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether expose projects applies to the mcp gateway state.
    /// </summary>
    /// <value>The expose projects value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeProjects { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether expose knowledge applies to the mcp gateway state.
    /// </summary>
    /// <value>The expose knowledge value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeKnowledge { get; set; } = true;

    /// <summary>Gets or sets whether full Council knowledge content is returned instead of metadata-only records.</summary>
    /// <value>The expose knowledge content value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeKnowledgeContent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether expose regex applies to the mcp gateway state.
    /// </summary>
    /// <value>The expose regex value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeRegex { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether expose toolchains applies to the mcp gateway state.
    /// </summary>
    /// <value>The expose toolchains value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeToolchains { get; set; } = true;

    /// <summary>Gets or sets whether project artifacts and artifact metadata are exposed.</summary>
    /// <value>The expose artifacts value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeArtifacts { get; set; } = true;

    /// <summary>Gets or sets whether persisted artifact values/content are returned instead of metadata-only records.</summary>
    /// <value>The expose artifact values value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeArtifactValues { get; set; }

    /// <summary>Gets or sets whether artifacts marked sensitive can be returned to MCP clients.</summary>
    /// <value>The expose sensitive artifacts value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeSensitiveArtifacts { get; set; }

    /// <summary>Gets or sets whether configured toolchain environment-variable payloads can be returned.</summary>
    /// <value>The expose toolchain environment variables value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeToolchainEnvironmentVariables { get; set; }

    /// <summary>Gets or sets whether Council-related DX functions can be bridged through MCP.</summary>
    /// <value>The expose council value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeCouncil { get; set; } = true;

    /// <summary>Gets or sets whether runtime-policy information can be bridged through MCP.</summary>
    /// <value>The expose runtime policy value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeRuntimePolicy { get; set; }

    /// <summary>Gets or sets whether diagnostic functions can be bridged through MCP.</summary>
    /// <value>The expose diagnostics value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeDiagnostics { get; set; }

    /// <summary>Gets or sets whether absolute local file-system paths may be returned to MCP clients.</summary>
    /// <value>The expose system paths value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ExposeSystemPaths { get; set; }

    /// <summary>Gets or sets whether archived projects/knowledge are included in MCP list operations.</summary>
    /// <value>The include archived projects value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool IncludeArchivedProjects { get; set; }

    /// <summary>Gets or sets a GUID allow-list for projects. Empty permits all projects allowed by the other gateway rules.</summary>
    /// <value>The allowed project identifiers value exposed by <see cref="McpGatewayOptions"/>.</value>
    public string AllowedProjectIds { get; set; } = string.Empty;

    /// <summary>Gets or sets optional function-name/route prefixes that are allowed. Empty permits all otherwise allowed functions.</summary>
    /// <value>The tool include prefixes value exposed by <see cref="McpGatewayOptions"/>.</value>
    public string ToolIncludePrefixes { get; set; } = string.Empty;

    /// <summary>Gets or sets function-name/route prefixes that are always excluded.</summary>
    /// <value>The tool exclude prefixes value exposed by <see cref="McpGatewayOptions"/>.</value>
    public string ToolExcludePrefixes { get; set; } = string.Empty;

    /// <summary>Gets or sets the maximum number of records returned by list/search operations.</summary>
    /// <value>The max list items value exposed by <see cref="McpGatewayOptions"/>.</value>
    public int MaxListItems { get; set; } = 100;

    /// <summary>Gets or sets the maximum number of characters returned by one MCP helper result.</summary>
    /// <value>The max result characters value exposed by <see cref="McpGatewayOptions"/>.</value>
    public int MaxResultCharacters { get; set; } = 200000;

    /// <summary>Gets or sets whether generated artifact/download links can be returned when existing LocalGPT tools provide them.</summary>
    /// <value>The return download urls value exposed by <see cref="McpGatewayOptions"/>.</value>
    public bool ReturnDownloadUrls { get; set; } = true;
}
