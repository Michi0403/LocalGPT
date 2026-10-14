namespace LocalGPT.BusinessObjects;

/// <summary>Declares a persisted, user-configurable DevExpress layout surface for a Razor component section.</summary>
/// <param name="sectionName">Section name value supplied to the local GPT UI layout surface attribute operation and used when producing its result.</param>
/// <param name="displayName">Display name value supplied to the local GPT UI layout surface attribute operation and used when producing its result.</param>
/// <param name="route">Route value supplied to the local GPT UI layout surface attribute operation and used when producing its result.</param>
/// <param name="defaultColSpanMd">Default col span md value supplied to the local GPT UI layout surface attribute operation and used when producing its result.</param>
/// <param name="defaultInnerLayout">Default inner layout value supplied to the local GPT UI layout surface attribute operation and used when producing its result.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class LocalGptUiLayoutSurfaceAttribute(
    string sectionName,
    string displayName,
    string route = "",
    int defaultColSpanMd = 12,
    string defaultInnerLayout = "Content") : Attribute
{
    /// <summary>Gets the stable section name used as part of the persisted layout key.</summary>
    /// <value>The section name value exposed by <see cref="LocalGptUiLayoutSurfaceAttribute"/>.</value>
    public string SectionName { get; } = sectionName;
    /// <summary>
    /// Gets the display name value that forms part of the local GPT UI layout surface attribute state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="LocalGptUiLayoutSurfaceAttribute"/>.</value>
    public string DisplayName { get; } = displayName;
    /// <summary>Gets the route associated with the surface when the owning component is routable.</summary>
    /// <value>The route value exposed by <see cref="LocalGptUiLayoutSurfaceAttribute"/>.</value>
    public string Route { get; } = route;
    /// <summary>
    /// Gets the default col span md value that forms part of the local GPT UI layout surface attribute state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default col span md value exposed by <see cref="LocalGptUiLayoutSurfaceAttribute"/>.</value>
    public int DefaultColSpanMd { get; } = defaultColSpanMd;
    /// <summary>
    /// Gets the default inner layout value that forms part of the local GPT UI layout surface attribute state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default inner layout value exposed by <see cref="LocalGptUiLayoutSurfaceAttribute"/>.</value>
    public string DefaultInnerLayout { get; } = defaultInnerLayout;
    /// <summary>Gets or sets whether an operator may hide this source-declared surface.</summary>
    /// <value>The can hide value exposed by <see cref="LocalGptUiLayoutSurfaceAttribute"/>.</value>
    public bool CanHide { get; set; } = true;
    /// <summary>Gets or sets the default DevExpress layout component used as the outer envelope.</summary>
    /// <value>The default layout type value exposed by <see cref="LocalGptUiLayoutSurfaceAttribute"/>.</value>
    public string DefaultLayoutType { get; set; } = "FormLayout";
}

/// <summary>Serializable user-owned layout state for one page or nested editor surface.</summary>
public sealed class LocalGptUiLayoutSurface
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this local GPT UI layout surface instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="LocalGptUiLayoutSurface"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the assembly name value that forms part of the local GPT UI layout surface state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The assembly name value exposed by <see cref="LocalGptUiLayoutSurface"/>.</value>
    public string AssemblyName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the component name value that forms part of the local GPT UI layout surface state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The component name value exposed by <see cref="LocalGptUiLayoutSurface"/>.</value>
    public string ComponentName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the section name value that forms part of the local GPT UI layout surface state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The section name value exposed by <see cref="LocalGptUiLayoutSurface"/>.</value>
    public string SectionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the local GPT UI layout surface state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="LocalGptUiLayoutSurface"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the route associated with the surface, when applicable.</summary>
    /// <value>The route value exposed by <see cref="LocalGptUiLayoutSurface"/>.</value>
    public string Route { get; set; } = string.Empty;
    /// <summary>Gets or sets whether source policy permits the surface to be hidden by an operator override.</summary>
    /// <value>The can hide value exposed by <see cref="LocalGptUiLayoutSurface"/>.</value>
    public bool CanHide { get; set; } = true;
    /// <summary>Gets or sets whether the surface should render when source policy permits hiding.</summary>
    /// <value>The visible value exposed by <see cref="LocalGptUiLayoutSurface"/>.</value>
    public bool Visible { get; set; } = true;
    /// <summary>Gets or sets the relative ordering hint for future multi-surface orchestration.</summary>
    /// <value>The order value exposed by <see cref="LocalGptUiLayoutSurface"/>.</value>
    public int Order { get; set; }
    /// <summary>
    /// Gets or sets the col span md value that forms part of the local GPT UI layout surface state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The col span md value exposed by <see cref="LocalGptUiLayoutSurface"/>.</value>
    public int ColSpanMd { get; set; } = 12;
    /// <summary>Gets or sets the DevExpress outer layout type: FormLayout, GridLayout, StackLayout, Tabs, Splitter, Carousel, or Content.</summary>
    /// <value>The layout type value exposed by <see cref="LocalGptUiLayoutSurface"/>.</value>
    public string LayoutType { get; set; } = "FormLayout";
    /// <summary>Gets or sets the legacy nested presentation strategy retained for persisted 5.0.2 layout documents.</summary>
    /// <value>The inner layout value exposed by <see cref="LocalGptUiLayoutSurface"/>.</value>
    public string InnerLayout { get; set; } = "Content";
    /// <summary>Gets or sets the orientation used by StackLayout and Splitter envelopes.</summary>
    /// <value>The orientation value exposed by <see cref="LocalGptUiLayoutSurface"/>.</value>
    public string Orientation { get; set; } = "Vertical";
    /// <summary>Gets or sets the stack item length used when a stack presentation is selected.</summary>
    /// <value>The stack length value exposed by <see cref="LocalGptUiLayoutSurface"/>.</value>
    public string StackLength { get; set; } = "1fr";
    /// <summary>Gets or sets the optional CSS class appended to the layout surface.</summary>
    /// <value>The CSS class value exposed by <see cref="LocalGptUiLayoutSurface"/>.</value>
    public string CssClass { get; set; } = string.Empty;
    /// <summary>Gets or sets the time the persisted override was last updated.</summary>
    /// <value>The updated at UTC value exposed by <see cref="LocalGptUiLayoutSurface"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Durable JSON document that stores operator layout overrides independently from application business configuration.</summary>
public sealed class LocalGptUiLayoutDocument
{
    /// <summary>
    /// Gets or sets the version value that forms part of the local GPT UI layout state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The version value exposed by <see cref="LocalGptUiLayoutDocument"/>.</value>
    public int Version { get; set; } = 2;
    /// <summary>
    /// Gets or sets the surfaces collection maintained or exposed by this local GPT UI layout instance for downstream processing.
    /// </summary>
    /// <value>The surfaces value exposed by <see cref="LocalGptUiLayoutDocument"/>.</value>
    public List<LocalGptUiLayoutSurface> Surfaces { get; set; } = [];
}
