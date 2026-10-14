using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net;

namespace LocalGPT.Mcp;

/// <summary>
/// Owns the optional dedicated MCP Kestrel host. The listener is deliberately isolated from the primary LocalGPT
/// WebApplication so an MCP bind/TLS failure can never abort the Blazor, installer, API, or remote-web listeners.
/// MCP requests still resolve their application services from the authoritative LocalGPT service provider.
/// </summary>
/// <param name="applicationScopeFactory">Service scope factory dependency used by the local GPT mcp listener workflow to provide the corresponding application capability.</param>
/// <param name="settings">Settings containing the caller-supplied values that control this operation.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
internal sealed class LocalGptMcpListenerHostedService(
    IServiceScopeFactory applicationScopeFactory,
    McpDedicatedListenerSettings settings,
    ILogger<LocalGptMcpListenerHostedService> logger) : BackgroundService
{
    /// <summary>Starts the isolated MCP transport host and contains all optional-listener startup failures.</summary>
    /// <param name="stoppingToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Ensure BackgroundService.StartAsync can return to the parent host before any optional transport bind occurs.
        // Even a synchronous Kestrel configuration failure therefore cannot become a LocalGPT startup failure.
        await Task.Delay(1, stoppingToken).ConfigureAwait(false);

        if (!settings.Enabled)
            return;

        WebApplication? listener = null;
        try
        {
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
            {
                Args = Array.Empty<string>(),
                ApplicationName = typeof(LocalGptMcpListenerHostedService).Assembly.GetName().Name,
                ContentRootPath = AppContext.BaseDirectory
            });
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options => ConfigureListener(options, settings));

            listener = builder.Build();
            listener.MapPost(settings.Path, HandlePostAsync);
            listener.MapGet(settings.Path, HandleGetAsync);
            listener.MapDelete(settings.Path, HandleDeleteAsync);

            await listener.StartAsync(stoppingToken).ConfigureAwait(false);
            Program.SetMcpDedicatedListenerState(true, settings.Port);

            logger.LogInformation(
                "LocalGPT dedicated MCP host started at {Scheme}://{Address}:{Port}{Path}. The listener is isolated from the primary LocalGPT web host.",
                string.IsNullOrWhiteSpace(settings.CertificatePath) ? "http" : "https",
                settings.Address,
                settings.Port,
                settings.Path);

            await listener.WaitForShutdownAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal parent-host shutdown.
        }
        catch (Exception exception)
        {
            Program.SetMcpDedicatedListenerState(false, 0);
            var keepPrimaryEndpoint = settings.ExposeOnPrimaryEndpoint || settings.AllowPrimaryFallback;
            Program.SetMcpPrimaryExposureForRun(keepPrimaryEndpoint);
            Program.SetMcpEnabledForRun(keepPrimaryEndpoint);

            if (keepPrimaryEndpoint)
            {
                logger.LogWarning(
                    exception,
                    "The optional dedicated MCP host could not start at {Address}:{Port}. LocalGPT remains active and MCP continues on the primary loopback endpoint {PrimaryEndpoint}{Path} for this run.",
                    settings.Address,
                    settings.Port,
                    Program.BaseUrl,
                    settings.Path);
            }
            else
            {
                logger.LogWarning(
                    exception,
                    "The optional dedicated MCP host could not start at {Address}:{Port}. MCP is disabled for this run because the configured remote-access policy cannot safely fall back to the primary loopback endpoint. LocalGPT remains active.",
                    settings.Address,
                    settings.Port);
            }
        }
        finally
        {
            Program.SetMcpDedicatedListenerState(false, 0);
            if (listener is not null)
            {
                try
                {
                    await listener.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Disposing the optional dedicated MCP host failed. LocalGPT shutdown continues.");
                }
            }
        }
    }

    /// <summary>
    /// Handles post as part of the local GPT mcp listener service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="context">Context value supplied to the local GPT mcp listener operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task HandlePostAsync(HttpContext context)
    {
        var scope = applicationScopeFactory.CreateAsyncScope();
        await using var configuredScopeAsyncDisposal = scope.ConfigureAwait(false);
        var originalRequestServices = context.RequestServices;
        context.RequestServices = scope.ServiceProvider;
        try
        {
            var endpoint = scope.ServiceProvider.GetRequiredService<LocalGptMcpEndpoint>();
            await endpoint.HandlePostAsync(context).ConfigureAwait(false);
        }
        finally
        {
            context.RequestServices = originalRequestServices;
        }
    }

    /// <summary>
    /// Handles get as part of the local GPT mcp listener service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="context">Context value supplied to the local GPT mcp listener operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task HandleGetAsync(HttpContext context)
    {
        var scope = applicationScopeFactory.CreateAsyncScope();
        await using var configuredScopeAsyncDisposal = scope.ConfigureAwait(false);
        var originalRequestServices = context.RequestServices;
        context.RequestServices = scope.ServiceProvider;
        try
        {
            var endpoint = scope.ServiceProvider.GetRequiredService<LocalGptMcpEndpoint>();
            await endpoint.HandleGetAsync(context).ConfigureAwait(false);
        }
        finally
        {
            context.RequestServices = originalRequestServices;
        }
    }

    /// <summary>
    /// Handles delete as part of the local GPT mcp listener service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="context">Context value supplied to the local GPT mcp listener operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task HandleDeleteAsync(HttpContext context)
    {
        var scope = applicationScopeFactory.CreateAsyncScope();
        await using var configuredScopeAsyncDisposal = scope.ConfigureAwait(false);
        var originalRequestServices = context.RequestServices;
        context.RequestServices = scope.ServiceProvider;
        try
        {
            var endpoint = scope.ServiceProvider.GetRequiredService<LocalGptMcpEndpoint>();
            await endpoint.HandleDeleteAsync(context).ConfigureAwait(false);
        }
        finally
        {
            context.RequestServices = originalRequestServices;
        }
    }

    /// <summary>
    /// Performs configure listener as part of the local GPT mcp listener service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="kestrel">Kestrel value supplied to the local GPT mcp listener operation and used when producing its result.</param>
    /// <param name="settings">Settings containing the caller-supplied values that control this operation.</param>
    private void ConfigureListener(KestrelServerOptions kestrel, McpDedicatedListenerSettings settings)
    {
        kestrel.Limits.MaxRequestBodySize = null;
        kestrel.Limits.MaxRequestBufferSize = null;

        void Configure(ListenOptions listen)
        {
            if (!string.IsNullOrWhiteSpace(settings.CertificatePath))
                listen.UseHttps(settings.CertificatePath, settings.CertificatePassword);
        }

        if (settings.Address.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase) ||
            settings.Address.Equals("*", StringComparison.OrdinalIgnoreCase))
        {
            kestrel.ListenAnyIP(settings.Port, Configure);
        }
        else if (settings.Address.Equals("::", StringComparison.OrdinalIgnoreCase))
        {
            kestrel.Listen(IPAddress.IPv6Any, settings.Port, Configure);
        }
        else if (IPAddress.TryParse(settings.Address, out var address))
        {
            kestrel.Listen(address, settings.Port, Configure);
        }
        else
        {
            throw new InvalidOperationException("The MCP listener address must be an IP address, 0.0.0.0, ::, or *.");
        }
    }
}

/// <summary>Immutable startup snapshot for the optional dedicated MCP transport host.</summary>
/// <param name="enabled">Value indicating whether enabled should apply to this operation.</param>
/// <param name="exposeOnPrimaryEndpoint">Value indicating whether expose on primary endpoint should apply to this operation.</param>
/// <param name="allowPrimaryFallback">Value indicating whether allow primary fallback should apply to this operation.</param>
/// <param name="address">Address value supplied to the mcp dedicated listener operation and used when producing its result.</param>
/// <param name="port">Port value supplied to the mcp dedicated listener operation and used when producing its result.</param>
/// <param name="path">Path value supplied to the mcp dedicated listener operation and used when producing its result.</param>
/// <param name="certificatePath">Certificate path value supplied to the mcp dedicated listener operation and used when producing its result.</param>
/// <param name="certificatePassword">Certificate password value supplied to the mcp dedicated listener operation and used when producing its result.</param>
internal sealed class McpDedicatedListenerSettings(
    bool enabled,
    bool exposeOnPrimaryEndpoint,
    bool allowPrimaryFallback,
    string address,
    int port,
    string path,
    string certificatePath,
    string certificatePassword)
{
    /// <summary>
    /// Gets a value indicating whether the option is enabled applies to the mcp dedicated listener state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="McpDedicatedListenerSettings"/>.</value>
    public bool Enabled { get; } = enabled;
    /// <summary>
    /// Gets a value indicating whether expose on primary endpoint applies to the mcp dedicated listener state.
    /// </summary>
    /// <value>The expose on primary endpoint value exposed by <see cref="McpDedicatedListenerSettings"/>.</value>
    public bool ExposeOnPrimaryEndpoint { get; } = exposeOnPrimaryEndpoint;
    /// <summary>
    /// Gets a value indicating whether primary fallback applies to the mcp dedicated listener state.
    /// </summary>
    /// <value>The allow primary fallback value exposed by <see cref="McpDedicatedListenerSettings"/>.</value>
    public bool AllowPrimaryFallback { get; } = allowPrimaryFallback;
    /// <summary>
    /// Gets the address that identifies the network or application endpoint associated with this mcp dedicated listener state.
    /// </summary>
    /// <value>The address value exposed by <see cref="McpDedicatedListenerSettings"/>.</value>
    public string Address { get; } = address;
    /// <summary>
    /// Gets the port value that forms part of the mcp dedicated listener state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The port value exposed by <see cref="McpDedicatedListenerSettings"/>.</value>
    public int Port { get; } = port;
    /// <summary>
    /// Gets the path used by this mcp dedicated listener instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path value exposed by <see cref="McpDedicatedListenerSettings"/>.</value>
    public string Path { get; } = path;
    /// <summary>
    /// Gets the certificate path used by this mcp dedicated listener instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The certificate path value exposed by <see cref="McpDedicatedListenerSettings"/>.</value>
    public string CertificatePath { get; } = certificatePath;
    /// <summary>
    /// Gets the certificate password value that forms part of the mcp dedicated listener state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The certificate password value exposed by <see cref="McpDedicatedListenerSettings"/>.</value>
    public string CertificatePassword { get; } = certificatePassword;
}
