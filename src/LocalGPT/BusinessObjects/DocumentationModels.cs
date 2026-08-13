namespace LocalGPT.BusinessObjects;

/// <summary>
/// Marks a documented type with the LocalGPT version in which its maintained XML documentation was last reviewed.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = true, AllowMultiple = false)]
public sealed class DocumentationUpdatedAttribute : Attribute
{
    /// <summary>Creates documentation-version metadata for a maintained public contract.</summary>
    /// <param name="version">Version value supplied to the documentation updated attribute operation and used when producing its result.</param>
    public DocumentationUpdatedAttribute(string version)
    {
        Version = string.IsNullOrWhiteSpace(version) ? "unknown" : version.Trim();
    }

    /// <summary>Gets the LocalGPT version that last reviewed the annotated contract documentation.</summary>
    /// <value>The version value exposed by <see cref="DocumentationUpdatedAttribute"/>.</value>
    public string Version { get; }
}

/// <summary>
/// Describes the generated LocalGPT documentation artifacts available to the running application.
/// </summary>
[DocumentationUpdated("2.1.20")]
public sealed class LocalGptDocumentationStatus
{
    /// <summary>Gets or sets the LocalGPT application version represented by the documentation.</summary>
    /// <value>The version value exposed by <see cref="LocalGptDocumentationStatus"/>.</value>
    public string Version { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC time at which the documentation status was inspected.</summary>
    /// <value>The inspected at UTC value exposed by <see cref="LocalGptDocumentationStatus"/>.</value>
    public DateTime InspectedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC build time recorded by the generated documentation manifest.</summary>
    /// <value>The generated at UTC value exposed by <see cref="LocalGptDocumentationStatus"/>.</value>
    public DateTime? GeneratedAtUtc { get; set; }

    /// <summary>Gets or sets whether the generated DocFX HTML entry page exists.</summary>
    /// <value>The HTML available value exposed by <see cref="LocalGptDocumentationStatus"/>.</value>
    public bool HtmlAvailable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether PDF available applies to the LocalGPT documentation status state.
    /// </summary>
    /// <value>The PDF available value exposed by <see cref="LocalGptDocumentationStatus"/>.</value>
    public bool PdfAvailable { get; set; }

    /// <summary>Gets or sets whether the compiler-generated XML comment catalog exists.</summary>
    /// <value>The XML comments available value exposed by <see cref="LocalGptDocumentationStatus"/>.</value>
    public bool XmlCommentsAvailable { get; set; }

    /// <summary>Gets or sets the number of XML-comment members currently available through the documentation API.</summary>
    /// <value>The comment count value exposed by <see cref="LocalGptDocumentationStatus"/>.</value>
    public int CommentCount { get; set; }

    /// <summary>Gets or sets the application-relative URL of the generated HTML documentation.</summary>
    /// <value>The HTML URL value exposed by <see cref="LocalGptDocumentationStatus"/>.</value>
    public string HtmlUrl { get; set; } = "/help-docs/index.html";

    /// <summary>Gets or sets the application-relative URL of the generated PDF documentation.</summary>
    /// <value>The PDF URL value exposed by <see cref="LocalGptDocumentationStatus"/>.</value>
    public string PdfUrl { get; set; } = "/api/documentation/pdf";

    /// <summary>Gets or sets the application-relative URL of the searchable XML-comment catalog.</summary>
    /// <value>The comments URL value exposed by <see cref="LocalGptDocumentationStatus"/>.</value>
    public string CommentsUrl { get; set; } = "/api/documentation/comments";

    /// <summary>
    /// Gets or sets the PDF file name used by this LocalGPT documentation status instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The PDF file name value exposed by <see cref="LocalGptDocumentationStatus"/>.</value>
    public string PdfFileName { get; set; } = string.Empty;
}

/// <summary>
/// Represents one compiler-generated XML documentation member after localization adaptation and version enrichment.
/// </summary>
[DocumentationUpdated("2.1.20")]
public sealed class LocalGptDocumentationComment
{
    /// <summary>
    /// Gets or sets the stable member identifier used to identify or correlate this LocalGPT documentation comment instance with related application state.
    /// </summary>
    /// <value>The member identifier value exposed by <see cref="LocalGptDocumentationComment"/>.</value>
    public string MemberId { get; set; } = string.Empty;

    /// <summary>Gets or sets a readable member name derived from the stable identifier.</summary>
    /// <value>The display name value exposed by <see cref="LocalGptDocumentationComment"/>.</value>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the localized summary text, falling back to the maintained XML comment.</summary>
    /// <value>The summary value exposed by <see cref="LocalGptDocumentationComment"/>.</value>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Gets or sets localized extended guidance, falling back to the maintained XML remarks.</summary>
    /// <value>The remarks value exposed by <see cref="LocalGptDocumentationComment"/>.</value>
    public string Remarks { get; set; } = string.Empty;

    /// <summary>Gets or sets the requested UI culture used by the documentation translation adapter.</summary>
    /// <value>The culture value exposed by <see cref="LocalGptDocumentationComment"/>.</value>
    public string Culture { get; set; } = "en-US";

    /// <summary>Gets or sets the LocalGPT version in which the declaring contract documentation was last reviewed.</summary>
    /// <value>The last updated version value exposed by <see cref="LocalGptDocumentationComment"/>.</value>
    public string LastUpdatedVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current version value that forms part of the LocalGPT documentation comment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The current version value exposed by <see cref="LocalGptDocumentationComment"/>.</value>
    public string CurrentVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the comment review version matches the running application version.</summary>
    /// <value>The is current value exposed by <see cref="LocalGptDocumentationComment"/>.</value>
    public bool IsCurrent { get; set; }
}

/// <summary>Describes one safe same-origin documentation view requested by the LocalGPT frontend.</summary>
[DocumentationUpdated("2.3.6")]
public sealed class LocalGptDocumentationViewerRequest
{
    /// <summary>
    /// Gets or sets the URL that identifies the network or application endpoint associated with this LocalGPT documentation viewer state.
    /// </summary>
    /// <value>The URL value exposed by <see cref="LocalGptDocumentationViewerRequest"/>.</value>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title value that forms part of the LocalGPT documentation viewer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title value exposed by <see cref="LocalGptDocumentationViewerRequest"/>.</value>
    public string Title { get; set; } = "LocalGPT documentation";
}

/// <summary>Represents the scoped in-application documentation viewer state for one Blazor circuit.</summary>
[DocumentationUpdated("2.3.6")]
public sealed class LocalGptDocumentationViewerState
{
    /// <summary>
    /// Gets or sets a value indicating whether open applies to the LocalGPT documentation viewer state.
    /// </summary>
    /// <value>The is open value exposed by <see cref="LocalGptDocumentationViewerState"/>.</value>
    public bool IsOpen { get; set; }

    /// <summary>
    /// Gets or sets the URL that identifies the network or application endpoint associated with this LocalGPT documentation viewer state.
    /// </summary>
    /// <value>The URL value exposed by <see cref="LocalGptDocumentationViewerState"/>.</value>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title value that forms part of the LocalGPT documentation viewer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title value exposed by <see cref="LocalGptDocumentationViewerState"/>.</value>
    public string Title { get; set; } = "LocalGPT documentation";

    /// <summary>Gets or sets a monotonic change token used by the viewer host.</summary>
    /// <value>The revision value exposed by <see cref="LocalGptDocumentationViewerState"/>.</value>
    public long Revision { get; set; }
}

/// <summary>Describes the documentation routes and availability exposed to local controllers and 1-Wire peers.</summary>
[DocumentationUpdated("2.3.6")]
public sealed class LocalGptDocumentationProfile
{
    /// <summary>
    /// Gets or sets the status value that forms part of the LocalGPT documentation profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="LocalGptDocumentationProfile"/>.</value>
    public LocalGptDocumentationStatus Status { get; set; } = new();

    /// <summary>
    /// Gets or sets the help route value that forms part of the LocalGPT documentation profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The help route value exposed by <see cref="LocalGptDocumentationProfile"/>.</value>
    public string HelpRoute { get; set; } = "/help";

    /// <summary>
    /// Gets or sets the HTML route value that forms part of the LocalGPT documentation profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The HTML route value exposed by <see cref="LocalGptDocumentationProfile"/>.</value>
    public string HtmlRoute { get; set; } = "/help-docs/index.html";

    /// <summary>
    /// Gets or sets the API route value that forms part of the LocalGPT documentation profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The API route value exposed by <see cref="LocalGptDocumentationProfile"/>.</value>
    public string ApiRoute { get; set; } = "/help-docs/api/index.html";

    /// <summary>
    /// Gets or sets the PDF route value that forms part of the LocalGPT documentation profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The PDF route value exposed by <see cref="LocalGptDocumentationProfile"/>.</value>
    public string PdfRoute { get; set; } = "/api/documentation/pdf";

    /// <summary>Gets or sets whether the frontend uses a focus-managed native modal viewer.</summary>
    /// <value>The supports accessible modal viewer value exposed by <see cref="LocalGptDocumentationProfile"/>.</value>
    public bool SupportsAccessibleModalViewer { get; set; } = true;
}

