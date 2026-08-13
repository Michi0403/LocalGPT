using System.Text.RegularExpressions;

namespace LocalGPT.Interfaces;

/// <summary>
/// Supplies Council text patterns from the database-backed regex catalog.
/// Runtime text services consume this contract instead of owning regex literals,
/// options, timeouts, or process-wide pattern fields.
/// </summary>
public interface ICouncilTextPatternDataService
{
    /// <summary>
    /// Gets the former thought break pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The former thought break pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex FormerThoughtBreakPattern { get; }
    /// <summary>
    /// Gets the former thought code wrapper pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The former thought code wrapper pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex FormerThoughtCodeWrapperPattern { get; }
    /// <summary>
    /// Gets the former thought opening fence pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The former thought opening fence pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex FormerThoughtOpeningFencePattern { get; }
    /// <summary>
    /// Gets the former thought closing fence pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The former thought closing fence pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex FormerThoughtClosingFencePattern { get; }
    /// <summary>
    /// Gets the former thought presentation wrapper pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The former thought presentation wrapper pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex FormerThoughtPresentationWrapperPattern { get; }
    /// <summary>
    /// Gets the former thought excess line break pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The former thought excess line break pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex FormerThoughtExcessLineBreakPattern { get; }
    /// <summary>
    /// Gets the whitespace pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The whitespace pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex WhitespacePattern { get; }
    /// <summary>
    /// Gets the name cleaner pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name cleaner pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex NameCleanerPattern { get; }
    /// <summary>
    /// Gets the mod identifier cleaner pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The mod identifier cleaner pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex ModIdCleanerPattern { get; }
    /// <summary>
    /// Gets the package part cleaner pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The package part cleaner pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex PackagePartCleanerPattern { get; }
    /// <summary>
    /// Gets the structured field pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The structured field pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex StructuredFieldPattern { get; }
    /// <summary>
    /// Gets the knowledge block pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The knowledge block pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex KnowledgeBlockPattern { get; }
    /// <summary>
    /// Gets the minecraft quoted project name pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minecraft quoted project name pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex MinecraftQuotedProjectNamePattern { get; }
    /// <summary>
    /// Gets the minecraft explicit project name pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minecraft explicit project name pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex MinecraftExplicitProjectNamePattern { get; }
    /// <summary>
    /// Gets the minecraft named project pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minecraft named project pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex MinecraftNamedProjectPattern { get; }
    /// <summary>
    /// Gets the markdown heading project name pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The markdown heading project name pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex MarkdownHeadingProjectNamePattern { get; }
    /// <summary>
    /// Gets the identifier separator pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The identifier separator pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex IdentifierSeparatorPattern { get; }
    /// <summary>
    /// Gets the alpha numeric word pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The alpha numeric word pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex AlphaNumericWordPattern { get; }
    /// <summary>
    /// Gets the integer pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The integer pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex IntegerPattern { get; }
    /// <summary>
    /// Gets the council DevExpress function call pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council DevExpress function call pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex CouncilDxFunctionCallPattern { get; }
    /// <summary>
    /// Gets the missing feature pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The missing feature pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex MissingFeaturePattern { get; }
    /// <summary>
    /// Gets the sensitive name pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sensitive name pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex SensitiveNamePattern { get; }
    /// <summary>
    /// Gets the truncated tail pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The truncated tail pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex TruncatedTailPattern { get; }
    /// <summary>
    /// Gets the target framework pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target framework pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex TargetFrameworkPattern { get; }
    /// <summary>
    /// Gets the package reference pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The package reference pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex PackageReferencePattern { get; }
    /// <summary>
    /// Gets the thinking block pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The thinking block pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex ThinkingBlockPattern { get; }
    /// <summary>
    /// Gets the capability gap block pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The capability gap block pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex CapabilityGapBlockPattern { get; }
    /// <summary>
    /// Gets the helpful source line pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The helpful source line pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex HelpfulSourceLinePattern { get; }
    /// <summary>
    /// Gets the stream status pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The stream status pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex StreamStatusPattern { get; }
    /// <summary>
    /// Gets the minecraft pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minecraft pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex MinecraftPattern { get; }
    /// <summary>
    /// Gets the datapack pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The datapack pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex DatapackPattern { get; }
    /// <summary>
    /// Gets the minecraft skeleton matrix pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minecraft skeleton matrix pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex MinecraftSkeletonMatrixPattern { get; }
    /// <summary>
    /// Gets the minecraft version pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minecraft version pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex MinecraftVersionPattern { get; }
    /// <summary>
    /// Gets the DevExpress document pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The DevExpress document pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex DevExpressDocumentPattern { get; }
    /// <summary>
    /// Gets the blazor frontend pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The blazor frontend pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex BlazorFrontendPattern { get; }
    /// <summary>
    /// Gets the dot net pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The dot net pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex DotNetPattern { get; }
    /// <summary>
    /// Gets the frontend pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frontend pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex FrontendPattern { get; }
    /// <summary>
    /// Gets the logging pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The logging pattern value exposed by <see cref="ICouncilTextPatternDataService"/>.</value>
    Regex LoggingPattern { get; }
    /// <summary>
    /// Performs extract structured field as part of the council text pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="body">Body value supplied to the council text pattern operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the council text pattern operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string? ExtractStructuredField(string body, string name);
}
