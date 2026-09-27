namespace LocalGPT.BusinessObjects;

/// <summary>Declares a persisted, user-configurable DevExpress layout surface for a Razor component section.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class LocalGptUiLayoutSurfaceAttribute(
    string sectionName,
    string displayName,
    string route = "",
    int defaultColSpanMd = 12,
    string defaultInnerLayout = "Content") : Attribute
{
    /// <summary>Gets the stable section name used as part of the persisted layout key.</summary>
    public string SectionName { get; } = sectionName;
    /// <summary>Gets the human-readable layout surface name.</summary>
    public string DisplayName { get; } = displayName;
    /// <summary>Gets the route associated with the surface when the owning component is routable.</summary>
    public string Route { get; } = route;
    /// <summary>Gets the default medium breakpoint column span.</summary>
    public int DefaultColSpanMd { get; } = defaultColSpanMd;
    /// <summary>Gets the default inner layout strategy.</summary>
    public string DefaultInnerLayout { get; } = defaultInnerLayout;
    /// <summary>Gets or sets whether an operator may hide this source-declared surface.</summary>
    public bool CanHide { get; set; } = true;
    /// <summary>Gets or sets the default DevExpress layout component used as the outer envelope.</summary>
    public string DefaultLayoutType { get; set; } = "FormLayout";
}

/// <summary>Serializable user-owned layout state for one page or nested editor surface.</summary>
public sealed class LocalGptUiLayoutSurface
{
    /// <summary>Gets or sets the stable assembly/component/section layout key.</summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>Gets or sets the owning assembly name.</summary>
    public string AssemblyName { get; set; } = string.Empty;
    /// <summary>Gets or sets the fully qualified Razor component type name.</summary>
    public string ComponentName { get; set; } = string.Empty;
    /// <summary>Gets or sets the stable section name within the component.</summary>
    public string SectionName { get; set; } = string.Empty;
    /// <summary>Gets or sets the operator-facing display name.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the route associated with the surface, when applicable.</summary>
    public string Route { get; set; } = string.Empty;
    /// <summary>Gets or sets whether source policy permits the surface to be hidden by an operator override.</summary>
    public bool CanHide { get; set; } = true;
    /// <summary>Gets or sets whether the surface should render when source policy permits hiding.</summary>
    public bool Visible { get; set; } = true;
    /// <summary>Gets or sets the relative ordering hint for future multi-surface orchestration.</summary>
    public int Order { get; set; }
    /// <summary>Gets or sets the DevExpress 12-column medium-breakpoint span.</summary>
    public int ColSpanMd { get; set; } = 12;
    /// <summary>Gets or sets the DevExpress outer layout type: FormLayout, GridLayout, StackLayout, Tabs, Splitter, Carousel, or Content.</summary>
    public string LayoutType { get; set; } = "FormLayout";
    /// <summary>Gets or sets the legacy nested presentation strategy retained for persisted 5.0.2 layout documents.</summary>
    public string InnerLayout { get; set; } = "Content";
    /// <summary>Gets or sets the orientation used by StackLayout and Splitter envelopes.</summary>
    public string Orientation { get; set; } = "Vertical";
    /// <summary>Gets or sets the stack item length used when a stack presentation is selected.</summary>
    public string StackLength { get; set; } = "1fr";
    /// <summary>Gets or sets the optional CSS class appended to the layout surface.</summary>
    public string CssClass { get; set; } = string.Empty;
    /// <summary>Gets or sets the time the persisted override was last updated.</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Durable JSON document that stores operator layout overrides independently from application business configuration.</summary>
public sealed class LocalGptUiLayoutDocument
{
    /// <summary>Gets or sets the document schema version.</summary>
    public int Version { get; set; } = 2;
    /// <summary>Gets or sets persisted layout surface overrides.</summary>
    public List<LocalGptUiLayoutSurface> Surfaces { get; set; } = [];
}
