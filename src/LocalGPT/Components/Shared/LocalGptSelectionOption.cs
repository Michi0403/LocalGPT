namespace LocalGPT.Components.Shared;

/// <summary>
/// Describes one value/label pair used by DevExpress Blazor selection editors.
/// </summary>
/// <typeparam name="TValue">The strongly typed value stored by the editor.</typeparam>
internal sealed record LocalGptSelectionOption<TValue>(TValue Value, string Label);
