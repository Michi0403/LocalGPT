using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents the persisted semantic relationship between one Council knowledge entry and one reusable regex pattern.
/// The link explains why the pattern belongs to the knowledge instead of forcing aliases, identifiers, or recognition
/// rules into free-form tags and content.
/// </summary>
public sealed class CouncilKnowledgeRegexPatternLink
{
    /// <summary>
    /// Gets or sets the stable knowledge entry identifier used to identify the knowledge side of the relationship.
    /// </summary>
    /// <value>The Council knowledge entry identifier.</value>
    public Guid KnowledgeEntryId { get; set; }

    /// <summary>
    /// References the Council knowledge note whose meaning is constrained or recognized by this pattern link.
    /// </summary>
    /// <value>The related Council knowledge entry when loaded.</value>
    public CouncilKnowledgeEntry? KnowledgeEntry { get; set; }

    /// <summary>
    /// Gets or sets the stable regex pattern identifier used to identify the recognition rule side of the relationship.
    /// </summary>
    /// <value>The regex pattern identifier.</value>
    public int RegexPatternId { get; set; }

    /// <summary>
    /// References the reusable regular-expression rule assigned a semantic role for the linked knowledge note.
    /// </summary>
    /// <value>The related regex pattern when loaded.</value>
    public RegexPattern? RegexPattern { get; set; }

    /// <summary>
    /// Gets or sets the user-visible purpose of the pattern for this particular knowledge entry.
    /// </summary>
    /// <value>A short role such as Alias, Classification, Extraction, Validation, or Routing.</value>
    [Required, MaxLength(96)]
    public string LinkPurpose { get; set; } = "Classification";

    /// <summary>
    /// Gets or sets the human-readable semantic meaning captured by this relationship.
    /// </summary>
    /// <value>An explanation of what a successful pattern match means for the linked knowledge.</value>
    [MaxLength(1000)]
    public string Meaning { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp at which the relationship was most recently created or refreshed.
    /// </summary>
    /// <value>The relationship update timestamp.</value>
    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether a human explicitly created or confirmed this relationship.
    /// </summary>
    /// <value><see langword="true"/> when the relationship was explicitly human-confirmed.</value>
    public bool LinkedByHuman { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the relationship is active for knowledge recognition workflows.
    /// </summary>
    /// <value><see langword="true"/> when the relationship is active.</value>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets a concise user-facing label for selection lists and relationship diagnostics.
    /// </summary>
    /// <value>The related regex name followed by the semantic relationship purpose.</value>
    public string DisplayName => $"{RegexPattern?.Name ?? $"Regex #{RegexPatternId}"} · {LinkPurpose}";
}

/// <summary>
/// Represents a request to create or update one persisted knowledge-to-regex relationship.
/// </summary>
public sealed class SaveKnowledgeRegexPatternLinkRequest
{
    /// <summary>Identifies the Council knowledge note that will receive the explicit recognition relationship.</summary>
    /// <value>The knowledge entry identifier.</value>
    public Guid KnowledgeEntryId { get; set; }

    /// <summary>Identifies the reusable regular-expression rule that will be assigned meaning for the knowledge note.</summary>
    /// <value>The regex pattern identifier.</value>
    public int RegexPatternId { get; set; }

    /// <summary>Gets or sets the semantic role of the regex for the linked knowledge.</summary>
    /// <value>The short relationship role.</value>
    public string LinkPurpose { get; set; } = "Classification";

    /// <summary>Gets or sets the explanation of what a successful match means for the linked knowledge.</summary>
    /// <value>The human-readable semantic meaning.</value>
    public string Meaning { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the relationship should participate in recognition workflows.</summary>
    /// <value><see langword="true"/> when enabled.</value>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the user explicitly confirmed this persisted relationship change.</summary>
    /// <value><see langword="true"/> when the write is explicitly confirmed.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents one successful recognition test produced by a knowledge-to-regex relationship without persisting test input.
/// </summary>
/// <param name="RegexPatternId">Identifier of the reusable regular-expression rule that matched the transient input.</param>
/// <param name="RegexPatternName">Human-readable name of the matching regular-expression rule.</param>
/// <param name="LinkPurpose">Semantic role assigned to the rule for the selected knowledge note.</param>
/// <param name="Meaning">Human-authored explanation of what the match means for the selected knowledge note.</param>
public sealed record KnowledgeRegexRecognitionMatch(
    int RegexPatternId,
    string RegexPatternName,
    string LinkPurpose,
    string Meaning)
{
    /// <summary>Formats the matching rule, its semantic role, and optional explanation into the label shown by the recognition tester.</summary>
    /// <value>The pattern name, semantic purpose, and optional meaning.</value>
    public string DisplayName => string.IsNullOrWhiteSpace(Meaning)
        ? $"{RegexPatternName} · {LinkPurpose}"
        : $"{RegexPatternName} · {LinkPurpose}: {Meaning}";
}
