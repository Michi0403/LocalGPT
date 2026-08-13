namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents one browser-selected file sent to a Council that is already running.
/// </summary>
public sealed class LiveCouncilUploadFile
{
    /// <summary>
    /// Gets or sets the name value that forms part of the live council upload file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="LiveCouncilUploadFile"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the content type value that forms part of the live council upload file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content type value exposed by <see cref="LiveCouncilUploadFile"/>.</value>
    public string ContentType { get; set; } = "application/octet-stream";
    /// <summary>
    /// Gets or sets the size bytes value that forms part of the live council upload file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The size bytes value exposed by <see cref="LiveCouncilUploadFile"/>.</value>
    public long SizeBytes { get; set; }
    /// <summary>Gets or sets the file bytes transferred through Blazor JavaScript interop.</summary>
    /// <value>The data value exposed by <see cref="LiveCouncilUploadFile"/>.</value>
    public byte[] Data { get; set; } = [];
}
