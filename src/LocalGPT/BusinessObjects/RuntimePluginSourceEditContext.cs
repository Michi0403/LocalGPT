namespace LocalGPT.BusinessObjects;

/// <summary>
/// Current InteractiveServer editing selection for a single runtime plugin draft. This is
/// transient UI state, not a second persisted source of truth: projects, revisions and
/// tracked files remain owned by the Project services and the publication artifact.
/// </summary>
public sealed class RuntimePluginSourceEditContext
{
    /// <summary>Changes whenever a selection is mutated, invalidating stale asynchronous reads.</summary>
    /// <value>The selection version value exposed by <see cref="RuntimePluginSourceEditContext"/>.</value>
    public long SelectionVersion { get; set; }
    /// <summary>Identifies the currently edited runtime-plugin Business Object.</summary>
    /// <value>The plugin identifier value exposed by <see cref="RuntimePluginSourceEditContext"/>.</value>
    public Guid PluginId { get; set; }
    /// <summary>Selected approved project.</summary>
    /// <value>The project identifier value exposed by <see cref="RuntimePluginSourceEditContext"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>Selected project revision, or Empty for the unversioned tracked-file view.</summary>
    /// <value>The revision identifier value exposed by <see cref="RuntimePluginSourceEditContext"/>.</value>
    public Guid RevisionId { get; set; }
    /// <summary>Selected, curator-reviewed source file in that project revision.</summary>
    /// <value>The tracked file identifier value exposed by <see cref="RuntimePluginSourceEditContext"/>.</value>
    public Guid TrackedFileId { get; set; }
}
