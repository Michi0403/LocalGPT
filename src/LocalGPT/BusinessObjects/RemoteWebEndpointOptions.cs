namespace LocalGPT.BusinessObjects;

/// <summary>Application-host settings. The loopback endpoint remains authoritative while RemoteEndpoint adds optional LAN/VPN access.</summary>
public sealed class LocalGptHostOptions
{
    /// <summary>
    /// Gets or sets port.
    /// </summary>
    public int Port { get; set; } = 5000;
    /// <summary>
    /// Gets or sets remote endpoint.
    /// </summary>
    public RemoteWebEndpointOptions RemoteEndpoint { get; set; } = new();
}

/// <summary>Optional second Kestrel endpoint for LAN/VPN/browser access. The historical loopback endpoint remains authoritative.</summary>
public sealed class RemoteWebEndpointOptions
{
    /// <summary>
    /// Stores section name.
    /// </summary>
    public const string SectionName = "LocalGPT:RemoteEndpoint";

    /// <summary>Explicit opt-in. A command-line/environment/configured remote port also opts in.</summary>
    public bool Enabled { get; set; }

    /// <summary>IP address to bind. Use 0.0.0.0 for all IPv4 interfaces or :: for all IPv6 interfaces.</summary>
    public string Address { get; set; } = "0.0.0.0";

    /// <summary>Secondary listener port. Zero keeps the endpoint disabled.</summary>
    public int Port { get; set; }

    /// <summary>Optional PFX/PKCS#12 certificate path. When set, the secondary endpoint uses HTTPS.</summary>
    public string CertificatePath { get; set; } = string.Empty;

    /// <summary>Optional PFX password. Prefer an environment variable over persisting a reusable production secret.</summary>
    public string CertificatePassword { get; set; } = string.Empty;
}
