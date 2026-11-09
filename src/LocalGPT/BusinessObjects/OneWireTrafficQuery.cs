namespace LocalGPT.BusinessObjects;

/// <summary>Filters the in-app 1-Wire traffic view.</summary>
public sealed class OneWireTrafficQuery
{
    /// <summary>Gets or sets text matched (case-insensitive) against peer, type, ids, capability, error and payload.</summary>
    /// <value>The search text; empty matches every packet.</value>
    public string Text { get; set; } = string.Empty;
    /// <summary>Gets or sets the direction to show.</summary>
    /// <value>Received, Sent, or empty for both.</value>
    public string Direction { get; set; } = string.Empty;
    /// <summary>Gets or sets the transport to show.</summary>
    /// <value>A transport name, or empty for all.</value>
    public string Transport { get; set; } = string.Empty;
    /// <summary>Gets or sets whether only rejected or failed packets are shown.</summary>
    /// <value><see langword="true"/> to show problems only.</value>
    public bool ProblemsOnly { get; set; }
    /// <summary>Gets or sets the maximum number of packets returned, newest first.</summary>
    /// <value>The result bound; values below 1 return the whole journal.</value>
    public int Maximum { get; set; } = 500;
}
