namespace LocalGPT.BusinessObjects;

/// <summary>Names the 1-Wire transports the traffic journal distinguishes.</summary>
public sealed class OneWireTrafficTransports
{
    /// <summary>The line-delimited JSON TCP link.</summary>
    public const string Tcp = "tcp";
    /// <summary>The HTTP/JSON request/response endpoint.</summary>
    public const string HttpJson = "http-json";
    /// <summary>The UDP discovery beacon.</summary>
    public const string UdpDiscovery = "udp-discovery";
}
