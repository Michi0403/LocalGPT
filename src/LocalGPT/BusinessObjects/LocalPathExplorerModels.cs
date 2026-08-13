namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents the input contract for local path browse, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class LocalPathBrowseRequest
{
    /// <summary>
    /// Gets or sets the path used by this local path browse instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path value exposed by <see cref="LocalPathBrowseRequest"/>.</value>
    public string Path { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether files applies to the local path browse state.
    /// </summary>
    /// <value>The include files value exposed by <see cref="LocalPathBrowseRequest"/>.</value>
    public bool IncludeFiles { get; set; } = true;
    /// <summary>
    /// Gets or sets the max entries value that forms part of the local path browse state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max entries value exposed by <see cref="LocalPathBrowseRequest"/>.</value>
    public int MaxEntries { get; set; } = 250;
}

/// <summary>
/// Represents local path state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class LocalPathEntry
{
    /// <summary>
    /// Gets or sets the name value that forms part of the local path state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="LocalPathEntry"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the full path used by this local path instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The full path value exposed by <see cref="LocalPathEntry"/>.</value>
    public string FullPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the entry kind value that forms part of the local path state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The entry kind value exposed by <see cref="LocalPathEntry"/>.</value>
    public string EntryKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the size bytes value that forms part of the local path state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The size bytes value exposed by <see cref="LocalPathEntry"/>.</value>
    public long? SizeBytes { get; set; }
    /// <summary>
    /// Gets or sets the modified at UTC associated with this local path state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The modified at UTC value exposed by <see cref="LocalPathEntry"/>.</value>
    public DateTime? ModifiedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether enter applies to the local path state.
    /// </summary>
    /// <value>The can enter value exposed by <see cref="LocalPathEntry"/>.</value>
    public bool CanEnter { get; set; }
}

/// <summary>
/// Represents the outcome of local path browse, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class LocalPathBrowseResult
{
    /// <summary>
    /// Gets or sets the requested path used by this local path browse instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The requested path value exposed by <see cref="LocalPathBrowseResult"/>.</value>
    public string RequestedPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the current path used by this local path browse instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The current path value exposed by <see cref="LocalPathBrowseResult"/>.</value>
    public string CurrentPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parent path used by this local path browse instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The parent path value exposed by <see cref="LocalPathBrowseResult"/>.</value>
    public string ParentPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether exists applies to the local path browse state.
    /// </summary>
    /// <value>The exists value exposed by <see cref="LocalPathBrowseResult"/>.</value>
    public bool Exists { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether directory applies to the local path browse state.
    /// </summary>
    /// <value>The is directory value exposed by <see cref="LocalPathBrowseResult"/>.</value>
    public bool IsDirectory { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether file applies to the local path browse state.
    /// </summary>
    /// <value>The is file value exposed by <see cref="LocalPathBrowseResult"/>.</value>
    public bool IsFile { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether readable applies to the local path browse state.
    /// </summary>
    /// <value>The is readable value exposed by <see cref="LocalPathBrowseResult"/>.</value>
    public bool IsReadable { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether writable applies to the local path browse state.
    /// </summary>
    /// <value>The is writable value exposed by <see cref="LocalPathBrowseResult"/>.</value>
    public bool IsWritable { get; set; }
    /// <summary>
    /// Gets or sets the entries collection maintained or exposed by this local path browse instance for downstream processing.
    /// </summary>
    /// <value>The entries value exposed by <see cref="LocalPathBrowseResult"/>.</value>
    public List<LocalPathEntry> Entries { get; set; } = [];
    /// <summary>
    /// Gets or sets the suggested roots collection maintained or exposed by this local path browse instance for downstream processing.
    /// </summary>
    /// <value>The suggested roots value exposed by <see cref="LocalPathBrowseResult"/>.</value>
    public List<string> SuggestedRoots { get; set; } = [];
    /// <summary>
    /// Gets or sets the warnings collection maintained or exposed by this local path browse instance for downstream processing.
    /// </summary>
    /// <value>The warnings value exposed by <see cref="LocalPathBrowseResult"/>.</value>
    public List<string> Warnings { get; set; } = [];
}
