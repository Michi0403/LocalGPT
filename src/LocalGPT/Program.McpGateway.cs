using LocalGPT.BusinessObjects;
using LocalGPT.Mcp;
using System.Net;

namespace LocalGPT
{
    public static partial class Program
    {
        private static int runtimeMcpPort;
        private static bool runtimeMcpEnabled;
        private static bool runtimeMcpDedicatedListenerEnabled;
        private static bool runtimeMcpExposeOnPrimaryEndpoint;
        private static string runtimeMcpAddress = "127.0.0.1";
        private static string runtimeMcpPath = "/mcp";

        /// <summary>Gets whether the MCP gateway was enabled when the current host listeners were created.</summary>
        public static bool McpEnabled => System.Threading.Volatile.Read(ref runtimeMcpEnabled);
        /// <summary>Gets the dedicated MCP port selected for this process, or zero when no dedicated listener exists.</summary>
        public static int McpPort => System.Threading.Volatile.Read(ref runtimeMcpPort);
        /// <summary>Gets whether the dedicated MCP-only listener was enabled when Kestrel was configured.</summary>
        public static bool McpDedicatedListenerEnabled => System.Threading.Volatile.Read(ref runtimeMcpDedicatedListenerEnabled);
        /// <summary>Gets whether the MCP route was admitted on the primary LocalGPT loopback listener at startup.</summary>
        public static bool McpExposeOnPrimaryEndpoint => System.Threading.Volatile.Read(ref runtimeMcpExposeOnPrimaryEndpoint);
        /// <summary>Gets the configured dedicated MCP bind address selected for this process.</summary>
        public static string McpAddress => runtimeMcpAddress;
        /// <summary>Gets the normalized MCP Streamable HTTP path.</summary>
        public static string McpPath => runtimeMcpPath;

        /// <summary>Updates the runtime-only dedicated MCP listener state after the isolated MCP host starts or stops.</summary>
        internal static void SetMcpDedicatedListenerState(bool enabled, int port)
        {
            System.Threading.Volatile.Write(ref runtimeMcpDedicatedListenerEnabled, enabled);
            System.Threading.Volatile.Write(ref runtimeMcpPort, enabled ? port : 0);
        }

        /// <summary>Updates primary-endpoint MCP exposure for this process without rewriting persisted configuration.</summary>
        internal static void SetMcpPrimaryExposureForRun(bool enabled) =>
            System.Threading.Volatile.Write(ref runtimeMcpExposeOnPrimaryEndpoint, enabled);

        /// <summary>Updates whether the MCP gateway remains available for this process after listener startup recovery.</summary>
        internal static void SetMcpEnabledForRun(bool enabled) =>
            System.Threading.Volatile.Write(ref runtimeMcpEnabled, enabled);

        private static McpGatewayOptions ResolveMcpGatewayOptions(IConfiguration configuration, string contentRootPath, ILogger logger)
        {
            try
            {
                var options = configuration.GetSection(McpGatewayOptions.SectionName).Get<McpGatewayOptions>() ?? new McpGatewayOptions();
                if (bool.TryParse(Environment.GetEnvironmentVariable("LOCALGPT_MCP_ENABLED"), out var environmentEnabled))
                    options.Enabled = environmentEnabled;
                if (int.TryParse(Environment.GetEnvironmentVariable("LOCALGPT_MCP_PORT"), out var environmentPort))
                    options.Port = environmentPort;
                var environmentAddress = Environment.GetEnvironmentVariable("LOCALGPT_MCP_ADDRESS");
                if (!string.IsNullOrWhiteSpace(environmentAddress)) options.Address = environmentAddress.Trim();
                var environmentPath = Environment.GetEnvironmentVariable("LOCALGPT_MCP_PATH");
                if (!string.IsNullOrWhiteSpace(environmentPath)) options.Path = environmentPath.Trim();
                var environmentCertificatePath = Environment.GetEnvironmentVariable("LOCALGPT_MCP_CERTIFICATE");
                if (!string.IsNullOrWhiteSpace(environmentCertificatePath)) options.CertificatePath = environmentCertificatePath.Trim();
                var environmentCertificatePassword = Environment.GetEnvironmentVariable("LOCALGPT_MCP_CERTIFICATE_PASSWORD");
                if (!string.IsNullOrWhiteSpace(environmentCertificatePassword)) options.CertificatePassword = environmentCertificatePassword;
                options.Path = NormalizeMcpPath(options.Path);
                options.Port = options.Port is >= 1 and <= 65535 ? options.Port : McpGatewayOptions.DefaultPort;
                options.Address = string.IsNullOrWhiteSpace(options.Address) ? "127.0.0.1" : options.Address.Trim();
                options.MaxRequestBodyBytes = Math.Clamp(options.MaxRequestBodyBytes, 1024, 64 * 1024 * 1024);
                options.MaxListItems = Math.Clamp(options.MaxListItems, 1, 10000);
                options.MaxResultCharacters = Math.Clamp(options.MaxResultCharacters, 1024, 8_000_000);
                options.ApiKeyHeader = string.IsNullOrWhiteSpace(options.ApiKeyHeader) ? "X-LocalGPT-MCP-Key" : options.ApiKeyHeader.Trim();
                options.ProtocolVersion = string.IsNullOrWhiteSpace(options.ProtocolVersion) ? "2026-07-28" : options.ProtocolVersion.Trim();
                options.LegacyProtocolVersion = string.IsNullOrWhiteSpace(options.LegacyProtocolVersion) ? "2025-11-25" : options.LegacyProtocolVersion.Trim();
                options.CacheTtlMs = Math.Clamp(options.CacheTtlMs, 0, 86_400_000);
                options.CacheScope = options.CacheScope?.Trim().Equals("public", StringComparison.OrdinalIgnoreCase) == true ? "public" : "private";
                options.AllowedHosts = string.IsNullOrWhiteSpace(options.AllowedHosts) ? "localhost;127.0.0.1;::1;[::1]" : options.AllowedHosts.Trim();
                options.AllowedOrigins = options.AllowedOrigins?.Trim() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(options.CertificatePath))
                {
                    options.CertificatePath = Environment.ExpandEnvironmentVariables(options.CertificatePath.Trim());
                    options.CertificatePath = Path.GetFullPath(Path.IsPathRooted(options.CertificatePath)
                        ? options.CertificatePath
                        : Path.Combine(contentRootPath, options.CertificatePath));
                    if (!File.Exists(options.CertificatePath))
                        throw new FileNotFoundException("The configured MCP TLS certificate does not exist.", options.CertificatePath);
                }

                if (options.Enabled && !options.DedicatedListenerEnabled && !options.ExposeOnPrimaryEndpoint)
                    throw new InvalidOperationException("MCP is enabled but neither a dedicated listener nor primary-endpoint exposure is enabled.");
                if (options.Enabled && !options.EnableModernProtocol && !options.EnableLegacyProtocol)
                    throw new InvalidOperationException("MCP is enabled but both modern and legacy protocol modes are disabled.");
                if (options.EnableModernProtocol && !options.ProtocolVersion.Equals("2026-07-28", StringComparison.Ordinal))
                    throw new InvalidOperationException("This LocalGPT release implements modern MCP protocol revision 2026-07-28. Select that revision in /install.");
                if (options.EnableLegacyProtocol && !options.LegacyProtocolVersion.Equals("2025-11-25", StringComparison.Ordinal))
                    throw new InvalidOperationException("This LocalGPT release implements legacy initialize compatibility for MCP 2025-11-25. Select that revision in /install.");
                if (options.Enabled && options.AllowRemoteClients && !options.RequireApiKey)
                    throw new InvalidOperationException("Remote MCP clients require API-key authentication. Enable RequireApiKey in /install or keep the MCP gateway loopback-only.");
                if (options.Enabled && options.AllowRemoteClients &&
                    string.IsNullOrWhiteSpace(options.ApiKey) && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LOCALGPT_MCP_API_KEY")))
                    throw new InvalidOperationException("Remote MCP clients require an API key, but no key is configured. Set LOCALGPT_MCP_API_KEY or configure a key in /install.");

                System.Threading.Volatile.Write(ref runtimeMcpEnabled, options.Enabled);
                System.Threading.Volatile.Write(ref runtimeMcpDedicatedListenerEnabled, false);
                System.Threading.Volatile.Write(ref runtimeMcpExposeOnPrimaryEndpoint, options.Enabled && options.ExposeOnPrimaryEndpoint);
                runtimeMcpAddress = options.Address;
                runtimeMcpPath = options.Path;
                System.Threading.Volatile.Write(ref runtimeMcpPort, 0);
                return options;
            }
            catch (Exception exception)
            {
                System.Threading.Volatile.Write(ref runtimeMcpEnabled, false);
                System.Threading.Volatile.Write(ref runtimeMcpDedicatedListenerEnabled, false);
                System.Threading.Volatile.Write(ref runtimeMcpExposeOnPrimaryEndpoint, false);
                System.Threading.Volatile.Write(ref runtimeMcpPort, 0);
                logger.LogError(exception, "MCP gateway configuration is invalid; MCP is disabled for this run so LocalGPT can continue starting. Secret values were omitted from logs.");
                return new McpGatewayOptions
                {
                    Enabled = false,
                    DedicatedListenerEnabled = false,
                    ExposeOnPrimaryEndpoint = false
                };
            }
        }

        private static bool IsLoopbackMcpAddress(string addressText)
        {
            if (!IPAddress.TryParse(addressText, out var address))
                return false;
            return IPAddress.IsLoopback(address);
        }

        private static void ValidateMcpPortContract(McpGatewayOptions options, RemoteWebEndpointOptions? remote)
        {
            if (!options.Enabled || !options.DedicatedListenerEnabled)
                return;
            if (options.Port == Port || options.Port == OneWirePort || options.Port == OneWireDiscoveryPort)
                throw new InvalidOperationException($"MCP port {options.Port} conflicts with LocalGPT app/OneWire ports. Choose a separate MCP TCP port in /install.");
            if (remote is not null && remote.Enabled && remote.Port > 0 && options.Port == remote.Port)
                throw new InvalidOperationException($"MCP port {options.Port} conflicts with the configured remote web endpoint. Choose a separate MCP TCP port in /install.");
        }

        private static void MapMcpGateway(WebApplication app, ILogger logger)
        {
            try
            {
                if (!McpEnabled)
                    return;
                var path = NormalizeMcpPath(McpPath);

                app.MapPost(path, async context =>
                {
                    var endpoint = context.RequestServices.GetRequiredService<LocalGptMcpEndpoint>();
                    await endpoint.HandlePostAsync(context).ConfigureAwait(false);
                });
                app.MapGet(path, context => context.RequestServices.GetRequiredService<LocalGptMcpEndpoint>().HandleGetAsync(context));
                app.MapDelete(path, context => context.RequestServices.GetRequiredService<LocalGptMcpEndpoint>().HandleDeleteAsync(context));
                logger.LogInformation("Configured LocalGPT MCP route at {McpPath}; the optional dedicated listener is owned by the isolated MCP host; primary exposure={PrimaryExposure}.", path, McpExposeOnPrimaryEndpoint);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Configuring the LocalGPT MCP gateway failed.");
                throw;
            }
        }

        private static string NormalizeMcpPath(string? value)
        {
            var path = string.IsNullOrWhiteSpace(value) ? "/mcp" : value.Trim();
            if (!path.StartsWith('/')) path = "/" + path;
            if (path.Length > 1) path = path.TrimEnd('/');
            if (path.Equals("/", StringComparison.Ordinal))
                return "/mcp";
            if (!path.Equals("/mcp", StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith("/mcp/", StringComparison.OrdinalIgnoreCase))
                path = "/mcp" + path;
            return path;
        }
    }
}
