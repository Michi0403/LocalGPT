using System.Text.RegularExpressions;

namespace LocalGPT.Interfaces;

/// <summary>
/// Supplies Council text patterns from the database-backed regex catalog.
/// Runtime text services consume this contract instead of owning regex literals,
/// options, timeouts, or process-wide pattern fields.
/// </summary>
public interface ICouncilTextPatternDataService
{
    Regex FormerThoughtBreakPattern { get; }
    Regex FormerThoughtCodeWrapperPattern { get; }
    Regex FormerThoughtOpeningFencePattern { get; }
    Regex FormerThoughtClosingFencePattern { get; }
    Regex FormerThoughtPresentationWrapperPattern { get; }
    Regex FormerThoughtExcessLineBreakPattern { get; }
    Regex WhitespacePattern { get; }
    Regex NameCleanerPattern { get; }
    Regex ModIdCleanerPattern { get; }
    Regex PackagePartCleanerPattern { get; }
    Regex StructuredFieldPattern { get; }
    Regex KnowledgeBlockPattern { get; }
    Regex MinecraftQuotedProjectNamePattern { get; }
    Regex MinecraftExplicitProjectNamePattern { get; }
    Regex MinecraftNamedProjectPattern { get; }
    Regex MarkdownHeadingProjectNamePattern { get; }
    Regex IdentifierSeparatorPattern { get; }
    Regex AlphaNumericWordPattern { get; }
    Regex IntegerPattern { get; }
    Regex CouncilDxFunctionCallPattern { get; }
    Regex MissingFeaturePattern { get; }
    Regex SensitiveNamePattern { get; }
    Regex TruncatedTailPattern { get; }
    Regex TargetFrameworkPattern { get; }
    Regex PackageReferencePattern { get; }
    Regex ThinkingBlockPattern { get; }
    Regex CapabilityGapBlockPattern { get; }
    Regex HelpfulSourceLinePattern { get; }
    Regex StreamStatusPattern { get; }
    Regex MinecraftPattern { get; }
    Regex DatapackPattern { get; }
    Regex MinecraftSkeletonMatrixPattern { get; }
    Regex MinecraftVersionPattern { get; }
    Regex DevExpressDocumentPattern { get; }
    Regex BlazorFrontendPattern { get; }
    Regex DotNetPattern { get; }
    Regex FrontendPattern { get; }
    Regex LoggingPattern { get; }
    string? ExtractStructuredField(string body, string name);
}
