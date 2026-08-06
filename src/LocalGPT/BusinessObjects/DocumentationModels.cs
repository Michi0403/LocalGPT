namespace LocalGPT.BusinessObjects;

/// <summary>
/// Marks a documented type with the LocalGPT version in which its maintained XML documentation was last reviewed.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = true, AllowMultiple = false)]
public sealed class DocumentationUpdatedAttribute : Attribute
{
    /// <summary>Creates documentation-version metadata for a maintained public contract.</summary>
    public DocumentationUpdatedAttribute(string version)
    {
        Version = string.IsNullOrWhiteSpace(version) ? "unknown" : version.Trim();
    }

    /// <summary>Gets the LocalGPT version that last reviewed the annotated contract documentation.</summary>
    public string Version { get; }
}

/// <summary>
/// Describes the generated LocalGPT documentation artifacts available to the running application.
/// </summary>
[DocumentationUpdated("2.1.20")]
public sealed class LocalGptDocumentationStatus
{
    /// <summary>Gets or sets the LocalGPT application version represented by the documentation.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC time at which the documentation status was inspected.</summary>
    public DateTime InspectedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC build time recorded by the generated documentation manifest.</summary>
    public DateTime? GeneratedAtUtc { get; set; }

    /// <summary>Gets or sets whether the generated DocFX HTML entry page exists.</summary>
    public bool HtmlAvailable { get; set; }

    /// <summary>Gets or sets whether the versioned PDF artifact exists.</summary>
    public bool PdfAvailable { get; set; }

    /// <summary>Gets or sets whether the compiler-generated XML comment catalog exists.</summary>
    public bool XmlCommentsAvailable { get; set; }

    /// <summary>Gets or sets the number of XML-comment members currently available through the documentation API.</summary>
    public int CommentCount { get; set; }

    /// <summary>Gets or sets the application-relative URL of the generated HTML documentation.</summary>
    public string HtmlUrl { get; set; } = "/help-docs/index.html";

    /// <summary>Gets or sets the application-relative URL of the generated PDF documentation.</summary>
    public string PdfUrl { get; set; } = "/api/documentation/pdf";

    /// <summary>Gets or sets the application-relative URL of the searchable XML-comment catalog.</summary>
    public string CommentsUrl { get; set; } = "/api/documentation/comments";

    /// <summary>Gets or sets the versioned PDF file name.</summary>
    public string PdfFileName { get; set; } = string.Empty;
}

/// <summary>
/// Represents one compiler-generated XML documentation member after localization adaptation and version enrichment.
/// </summary>
[DocumentationUpdated("2.1.20")]
public sealed class LocalGptDocumentationComment
{
    /// <summary>Gets or sets the stable XML documentation member identifier.</summary>
    public string MemberId { get; set; } = string.Empty;

    /// <summary>Gets or sets a readable member name derived from the stable identifier.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the localized summary text, falling back to the maintained XML comment.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Gets or sets localized extended guidance, falling back to the maintained XML remarks.</summary>
    public string Remarks { get; set; } = string.Empty;

    /// <summary>Gets or sets the requested UI culture used by the documentation translation adapter.</summary>
    public string Culture { get; set; } = "en-US";

    /// <summary>Gets or sets the LocalGPT version in which the declaring contract documentation was last reviewed.</summary>
    public string LastUpdatedVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets the current LocalGPT application version.</summary>
    public string CurrentVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the comment review version matches the running application version.</summary>
    public bool IsCurrent { get; set; }
}

/// <summary>Describes one safe same-origin documentation view requested by the LocalGPT frontend.</summary>
[DocumentationUpdated("2.3.6")]
public sealed class LocalGptDocumentationViewerRequest
{
    /// <summary>Gets or sets the application-relative documentation URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the accessible dialog and iframe title.</summary>
    public string Title { get; set; } = "LocalGPT documentation";
}

/// <summary>Represents the scoped in-application documentation viewer state for one Blazor circuit.</summary>
[DocumentationUpdated("2.3.6")]
public sealed class LocalGptDocumentationViewerState
{
    /// <summary>Gets or sets whether the native modal dialog is open.</summary>
    public bool IsOpen { get; set; }

    /// <summary>Gets or sets the application-relative documentation URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the accessible dialog and iframe title.</summary>
    public string Title { get; set; } = "LocalGPT documentation";

    /// <summary>Gets or sets a monotonic change token used by the viewer host.</summary>
    public long Revision { get; set; }
}

/// <summary>Describes the documentation routes and availability exposed to local controllers and 1-Wire peers.</summary>
[DocumentationUpdated("2.3.6")]
public sealed class LocalGptDocumentationProfile
{
    /// <summary>Gets or sets the current generated-documentation status.</summary>
    public LocalGptDocumentationStatus Status { get; set; } = new();

    /// <summary>Gets or sets the in-application help route.</summary>
    public string HelpRoute { get; set; } = "/help";

    /// <summary>Gets or sets the HTML documentation route.</summary>
    public string HtmlRoute { get; set; } = "/help-docs/index.html";

    /// <summary>Gets or sets the API reference route.</summary>
    public string ApiRoute { get; set; } = "/help-docs/api/index.html";

    /// <summary>Gets or sets the inline PDF controller route.</summary>
    public string PdfRoute { get; set; } = "/api/documentation/pdf";

    /// <summary>Gets or sets whether the frontend uses a focus-managed native modal viewer.</summary>
    public bool SupportsAccessibleModalViewer { get; set; } = true;
}

