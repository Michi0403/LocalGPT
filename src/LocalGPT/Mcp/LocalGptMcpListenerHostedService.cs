using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net;

namespace LocalGPT.Mcp;

/// <summary>
/// Owns the optional dedicated MCP Kestrel host. The listener is deliberately isolated from the primary LocalGPT
/// WebApplication so an MCP bind/TLS failure can never abort the Blazor, installer, API, or remote-web listeners.
/// MCP requests still resolve their application services from the authoritative LocalGPT service provider.
/// </summary>
internal sealed class LocalGptMcpListenerHostedService(
    IServiceScopeFactory applicationScopeFactory,
    McpDedicatedListenerSettings settings,
    ILogger<LocalGptMcpListenerHostedService> logger) : BackgroundService
{
    /// <summary>Starts the isolated MCP transport host and contains all optional-listener startup failures.</summary>
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
    public bool Enabled { get; } = enabled;
    public bool ExposeOnPrimaryEndpoint { get; } = exposeOnPrimaryEndpoint;
    public bool AllowPrimaryFallback { get; } = allowPrimaryFallback;
    public string Address { get; } = address;
    public int Port { get; } = port;
    public string Path { get; } = path;
    public string CertificatePath { get; } = certificatePath;
    public string CertificatePassword { get; } = certificatePassword;
}
