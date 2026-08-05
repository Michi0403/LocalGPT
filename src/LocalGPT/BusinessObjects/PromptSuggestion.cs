using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Describes one reusable Chat prompt and its optional Council-team ownership.
/// </summary>
[DocumentationUpdated("2.1.22")]
public sealed class PromptSuggestion
{
    /// <summary>Gets the stable prompt key used by direct Council starter routes.</summary>
    public string Key { get; }

    /// <summary>Gets or sets the visible prompt title.</summary>
    public string Title { get; set; }

    /// <summary>Gets or sets the compact prompt description.</summary>
    [Column(TypeName = "TEXT")]
    public string Text { get; set; }

    /// <summary>Gets or sets the full text submitted to the selected chat or Council session.</summary>
    [Column(TypeName = "TEXT")]
    public string PromptMessage { get; set; }

    /// <summary>Gets the Council-team keys for which this prompt is recommended.</summary>
    public IReadOnlyList<string> TeamKeys { get; }

    /// <summary>Gets whether the prompt is intended to create a fresh AI Council run rather than a normal single-model reply.</summary>
    public bool StartsCouncilDirectly { get; }

    /// <summary>
    /// Creates one prompt suggestion.
    /// </summary>
    /// <param name="title">Visible prompt title.</param>
    /// <param name="text">Compact prompt description.</param>
    /// <param name="promptMessage">Full submitted prompt text.</param>
    /// <param name="key">Optional stable route key; a title-derived key is used when omitted.</param>
    /// <param name="teamKeys">Optional Council-team keys associated with the prompt.</param>
    /// <param name="startsCouncilDirectly">Whether invoking the prompt should select AI Council and create a fresh Council chat.</param>
    public PromptSuggestion(
        string title,
        string text,
        string promptMessage,
        string? key = null,
        IEnumerable<string>? teamKeys = null,
        bool startsCouncilDirectly = false)
    {
        Title = title;
        Text = text;
        PromptMessage = promptMessage;
        Key = NormalizeKey(string.IsNullOrWhiteSpace(key) ? title : key);
        TeamKeys = (teamKeys ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        StartsCouncilDirectly = startsCouncilDirectly;
    }

    /// <summary>Returns whether this prompt is generic or assigned to the requested Council team.</summary>
    /// <param name="teamKey">Selected Council-team key.</param>
    /// <returns>True when the prompt should be displayed for the selected team.</returns>
    public bool IsAvailableForTeam(string? teamKey) =>
        TeamKeys.Count == 0 || (!string.IsNullOrWhiteSpace(teamKey) && TeamKeys.Contains(teamKey.Trim(), StringComparer.OrdinalIgnoreCase));

    /// <summary>Normalizes a prompt key for route and lookup use.</summary>
    /// <param name="value">Raw key or title.</param>
    /// <returns>A lowercase dash-separated key.</returns>
    private string NormalizeKey(string value) =>
        string.Join('-', value.Trim().ToLowerInvariant().Split([' ', '_', '/', '\\', ':'], StringSplitOptions.RemoveEmptyEntries));
}
