namespace LocalGPT.Components.Shared;

/// <summary>
/// Describes one value/label pair used by DevExpress Blazor selection editors.
/// </summary>
/// <typeparam name="TValue">The strongly typed value stored by the editor.</typeparam>
/// <param name="Value">Value value supplied to the local GPT selection option operation and used when producing its result.</param>
/// <param name="Label">Label value supplied to the local GPT selection option operation and used when producing its result.</param>
internal sealed record LocalGptSelectionOption<TValue>(TValue Value, string Label);
