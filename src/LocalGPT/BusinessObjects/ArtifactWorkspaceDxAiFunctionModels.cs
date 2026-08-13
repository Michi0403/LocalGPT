namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents optional parameters for listing generated artifact workspaces.
/// </summary>
public sealed class ArtifactWorkspaceListParameters
{
    /// <summary>
    /// Gets or sets an optional caller-requested result count; null or non-positive values use the database-backed MaxFiles policy.
    /// </summary>
    /// <value>The take value exposed by <see cref="ArtifactWorkspaceListParameters"/>.</value>
    public int? Take { get; set; }
}

/// <summary>
/// Represents parameters for listing generated artifact workspace files.
/// </summary>
public sealed class ArtifactWorkspaceFilesParameters
{
    /// <summary>
    /// Gets or sets the workspace name value that forms part of the artifact workspace files parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workspace name value exposed by <see cref="ArtifactWorkspaceFilesParameters"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional caller-requested result count; null or non-positive values use the database-backed MaxFiles policy.
    /// </summary>
    /// <value>The take value exposed by <see cref="ArtifactWorkspaceFilesParameters"/>.</value>
    public int? Take { get; set; }
}

/// <summary>
/// Represents parameters for reading one generated artifact workspace text file.
/// </summary>
public sealed class ArtifactWorkspaceFileReadParameters
{
    /// <summary>
    /// Gets or sets the workspace name value that forms part of the artifact workspace file read parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workspace name value exposed by <see cref="ArtifactWorkspaceFileReadParameters"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relative path used by this artifact workspace file read parameters instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path value exposed by <see cref="ArtifactWorkspaceFileReadParameters"/>.</value>
    public string RelativePath { get; set; } = string.Empty;
}

/// <summary>
/// Represents parameters for writing one reviewed generated artifact workspace text file.
/// </summary>
public sealed class ArtifactWorkspaceFileWriteParameters
{
    /// <summary>
    /// Gets or sets the workspace name value that forms part of the artifact workspace file write parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workspace name value exposed by <see cref="ArtifactWorkspaceFileWriteParameters"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relative path used by this artifact workspace file write parameters instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path value exposed by <see cref="ArtifactWorkspaceFileWriteParameters"/>.</value>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exact reviewed source text. Empty text is valid and creates an empty file.
    /// </summary>
    /// <value>The content value exposed by <see cref="ArtifactWorkspaceFileWriteParameters"/>.</value>
    public string? Content { get; set; }
}

/// <summary>
/// Represents parameters for refreshing one generated artifact workspace ZIP.
/// </summary>
public sealed class ArtifactWorkspaceZipParameters
{
    /// <summary>
    /// Gets or sets the workspace name value that forms part of the artifact workspace ZIP parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workspace name value exposed by <see cref="ArtifactWorkspaceZipParameters"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;
}
