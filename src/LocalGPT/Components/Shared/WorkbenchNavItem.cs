namespace LocalGPT.Components.Shared;

/// <summary>
/// Represents a workbench nav item application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Key">Key value supplied to the workbench nav item operation and used when producing its result.</param>
/// <param name="Label">Label value supplied to the workbench nav item operation and used when producing its result.</param>
/// <param name="Description">Description value supplied to the workbench nav item operation and used when producing its result.</param>
/// <param name="Badge">Badge value supplied to the workbench nav item operation and used when producing its result.</param>
public sealed record WorkbenchNavItem(
    string Key,
    string Label,
    string? Description = null,
    string? Badge = null);
