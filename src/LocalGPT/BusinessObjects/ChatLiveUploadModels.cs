namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents one browser-selected file sent to a Council that is already running.
/// </summary>
public sealed class LiveCouncilUploadFile
{
    /// <summary>Gets or sets the original browser file name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the browser-provided MIME type.</summary>
    public string ContentType { get; set; } = "application/octet-stream";
    /// <summary>Gets or sets the original file size in bytes.</summary>
    public long SizeBytes { get; set; }
    /// <summary>Gets or sets the file bytes transferred through Blazor JavaScript interop.</summary>
    public byte[] Data { get; set; } = [];
}
