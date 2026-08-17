namespace LocalGPT.Extensions;

/// <summary>Internal string mechanics used only behind DI-owned text services.</summary>
internal static class TextFilteringExtensions
{
    /// <summary>
    /// Performs contains text for <see cref="TextFilteringExtensions"/>, keeping the operation consistent with the state and invariants of the surrounding text filtering extensions workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the text filtering extensions operation and used when producing its result.</param>
    /// <param name="fragment">Fragment value supplied to the text filtering extensions operation and used when producing its result.</param>
    /// <param name="comparison">Comparison value supplied to the text filtering extensions operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    internal static bool ContainsText(this string? value, string? fragment, StringComparison comparison) =>
        !string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(fragment) && value.Contains(fragment, comparison);

    /// <summary>
    /// Starts s with text for <see cref="TextFilteringExtensions"/>, keeping the operation consistent with the state and invariants of the surrounding text filtering extensions workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the text filtering extensions operation and used when producing its result.</param>
    /// <param name="prefix">Prefix value supplied to the text filtering extensions operation and used when producing its result.</param>
    /// <param name="comparison">Comparison value supplied to the text filtering extensions operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    internal static bool StartsWithText(this string? value, string? prefix, StringComparison comparison) =>
        !string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(prefix) && value.StartsWith(prefix, comparison);

    /// <summary>
    /// Performs ends with text for <see cref="TextFilteringExtensions"/>, keeping the operation consistent with the state and invariants of the surrounding text filtering extensions workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the text filtering extensions operation and used when producing its result.</param>
    /// <param name="suffix">Suffix value supplied to the text filtering extensions operation and used when producing its result.</param>
    /// <param name="comparison">Comparison value supplied to the text filtering extensions operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    internal static bool EndsWithText(this string? value, string? suffix, StringComparison comparison) =>
        !string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(suffix) && value.EndsWith(suffix, comparison);

    /// <summary>
    /// Performs replace text for <see cref="TextFilteringExtensions"/>, keeping the operation consistent with the state and invariants of the surrounding text filtering extensions workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the text filtering extensions operation and used when producing its result.</param>
    /// <param name="oldValue">Old value value supplied to the text filtering extensions operation and used when producing its result.</param>
    /// <param name="newValue">New value value supplied to the text filtering extensions operation and used when producing its result.</param>
    /// <param name="comparison">Comparison value supplied to the text filtering extensions operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    internal static string ReplaceText(this string? value, string oldValue, string newValue, StringComparison comparison) =>
        (value ?? string.Empty).Replace(oldValue, newValue, comparison);

    /// <summary>
    /// Performs join text for <see cref="TextFilteringExtensions"/>, keeping the operation consistent with the state and invariants of the surrounding text filtering extensions workflow.
    /// </summary>
    /// <param name="values">String dependency used by the text filtering extensions workflow to provide the corresponding application capability.</param>
    /// <param name="separator">Separator value supplied to the text filtering extensions operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    internal static string JoinText(this IEnumerable<string>? values, string separator) =>
        values is null ? string.Empty : string.Join(separator, values);
}
