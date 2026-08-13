namespace LocalGPT.BusinessObjects;

/// <summary>Application-host settings. The loopback endpoint remains authoritative while RemoteEndpoint adds optional LAN/VPN access.</summary>
public sealed class LocalGptHostOptions
{
    /// <summary>
    /// Gets or sets the port value that forms part of the LocalGPT host state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The port value exposed by <see cref="LocalGptHostOptions"/>.</value>
    public int Port { get; set; } = 5000;
    /// <summary>
    /// Gets or sets the remote endpoint that identifies the network or application endpoint associated with this LocalGPT host state.
    /// </summary>
    /// <value>The remote endpoint value exposed by <see cref="LocalGptHostOptions"/>.</value>
    public RemoteWebEndpointOptions RemoteEndpoint { get; set; } = new();
}

/// <summary>Optional second Kestrel endpoint for LAN/VPN/browser access. The historical loopback endpoint remains authoritative.</summary>
public sealed class RemoteWebEndpointOptions
{
    /// <summary>
    /// Defines the section name constant used by <see cref="RemoteWebEndpointOptions"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string SectionName = "LocalGPT:RemoteEndpoint";

    /// <summary>Explicit opt-in. A command-line/environment/configured remote port also opts in.</summary>
    /// <value>The enabled value exposed by <see cref="RemoteWebEndpointOptions"/>.</value>
    public bool Enabled { get; set; }

    /// <summary>IP address to bind. Use 0.0.0.0 for all IPv4 interfaces or :: for all IPv6 interfaces.</summary>
    /// <value>The address value exposed by <see cref="RemoteWebEndpointOptions"/>.</value>
    public string Address { get; set; } = "0.0.0.0";

    /// <summary>Secondary listener port. Zero keeps the endpoint disabled.</summary>
    /// <value>The port value exposed by <see cref="RemoteWebEndpointOptions"/>.</value>
    public int Port { get; set; }

    /// <summary>Optional PFX/PKCS#12 certificate path. When set, the secondary endpoint uses HTTPS.</summary>
    /// <value>The certificate path value exposed by <see cref="RemoteWebEndpointOptions"/>.</value>
    public string CertificatePath { get; set; } = string.Empty;

    /// <summary>Optional PFX password. Prefer an environment variable over persisting a reusable production secret.</summary>
    /// <value>The certificate password value exposed by <see cref="RemoteWebEndpointOptions"/>.</value>
    public string CertificatePassword { get; set; } = string.Empty;
}
