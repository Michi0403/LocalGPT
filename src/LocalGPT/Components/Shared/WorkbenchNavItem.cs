namespace LocalGPT.Components.Shared;

/// <summary>
/// Represents a workbench nav item.
/// </summary>
public sealed record WorkbenchNavItem(
    string Key,
    string Label,
    string? Description = null,
    string? Badge = null);
