namespace LocalGPT.Components.Shared;

public sealed record WorkbenchNavItem(
    string Key,
    string Label,
    string? Description = null,
    string? Badge = null);
