namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents optional parameters for listing generated artifact workspaces.
/// </summary>
public sealed class ArtifactWorkspaceListParameters
{
    /// <summary>
    /// Gets or sets an optional caller-requested result count; null or non-positive values use the database-backed MaxFiles policy.
    /// </summary>
    public int? Take { get; set; }
}

/// <summary>
/// Represents parameters for listing generated artifact workspace files.
/// </summary>
public sealed class ArtifactWorkspaceFilesParameters
{
    /// <summary>
    /// Gets or sets the generated workspace name.
    /// </summary>
    public string WorkspaceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional caller-requested result count; null or non-positive values use the database-backed MaxFiles policy.
    /// </summary>
    public int? Take { get; set; }
}

/// <summary>
/// Represents parameters for reading one generated artifact workspace text file.
/// </summary>
public sealed class ArtifactWorkspaceFileReadParameters
{
    /// <summary>
    /// Gets or sets the generated workspace name.
    /// </summary>
    public string WorkspaceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the workspace-relative source path.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;
}

/// <summary>
/// Represents parameters for writing one reviewed generated artifact workspace text file.
/// </summary>
public sealed class ArtifactWorkspaceFileWriteParameters
{
    /// <summary>
    /// Gets or sets the generated workspace name.
    /// </summary>
    public string WorkspaceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the workspace-relative source path.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exact reviewed source text. Empty text is valid and creates an empty file.
    /// </summary>
    public string? Content { get; set; }
}

/// <summary>
/// Represents parameters for refreshing one generated artifact workspace ZIP.
/// </summary>
public sealed class ArtifactWorkspaceZipParameters
{
    /// <summary>
    /// Gets or sets the generated workspace name.
    /// </summary>
    public string WorkspaceName { get; set; } = string.Empty;
}
