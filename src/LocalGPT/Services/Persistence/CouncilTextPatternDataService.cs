using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Database boundary for every regular expression used by <see cref="CouncilTextService"/>.
/// Pattern text and flags come from RegexPatterns; the match timeout comes from SystemVariables.
/// </summary>
public sealed class CouncilTextPatternDataService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ISystemVariableDefinitionService systemVariables,
    ILogger<CouncilTextPatternDataService> logger) : ICouncilTextPatternDataService
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly object _sync = new();
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly Dictionary<string, CouncilTextCachedPattern> _cache = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets former thought break pattern.
    /// </summary>
    public Regex FormerThoughtBreakPattern => GetRequired(nameof(FormerThoughtBreakPattern));
    /// <summary>
    /// Gets or sets former thought code wrapper pattern.
    /// </summary>
    public Regex FormerThoughtCodeWrapperPattern => GetRequired(nameof(FormerThoughtCodeWrapperPattern));
    /// <summary>
    /// Gets or sets former thought opening fence pattern.
    /// </summary>
    public Regex FormerThoughtOpeningFencePattern => GetRequired(nameof(FormerThoughtOpeningFencePattern));
    /// <summary>
    /// Gets or sets former thought closing fence pattern.
    /// </summary>
    public Regex FormerThoughtClosingFencePattern => GetRequired(nameof(FormerThoughtClosingFencePattern));
    /// <summary>
    /// Gets or sets former thought presentation wrapper pattern.
    /// </summary>
    public Regex FormerThoughtPresentationWrapperPattern => GetRequired(nameof(FormerThoughtPresentationWrapperPattern));
    /// <summary>
    /// Gets or sets former thought excess line break pattern.
    /// </summary>
    public Regex FormerThoughtExcessLineBreakPattern => GetRequired(nameof(FormerThoughtExcessLineBreakPattern));
    /// <summary>
    /// Gets or sets whitespace pattern.
    /// </summary>
    public Regex WhitespacePattern => GetRequired("builtin.whitespace-pattern");
    /// <summary>
    /// Gets or sets name cleaner pattern.
    /// </summary>
    public Regex NameCleanerPattern => GetRequired("builtin.name-cleaner");
    /// <summary>
    /// Gets or sets mod identifier cleaner pattern.
    /// </summary>
    public Regex ModIdCleanerPattern => GetRequired("builtin.mod-id-cleaner");
    /// <summary>
    /// Gets or sets package part cleaner pattern.
    /// </summary>
    public Regex PackagePartCleanerPattern => GetRequired("builtin.package-part-cleaner");
    /// <summary>
    /// Gets or sets structured field pattern.
    /// </summary>
    public Regex StructuredFieldPattern => GetRequired(nameof(StructuredFieldPattern));
    /// <summary>
    /// Gets or sets knowledge block pattern.
    /// </summary>
    public Regex KnowledgeBlockPattern => GetRequired("builtin.localgpt-knowledge-block");
    /// <summary>
    /// Gets or sets minecraft quoted project name pattern.
    /// </summary>
    public Regex MinecraftQuotedProjectNamePattern => GetRequired(nameof(MinecraftQuotedProjectNamePattern));
    /// <summary>
    /// Gets or sets minecraft explicit project name pattern.
    /// </summary>
    public Regex MinecraftExplicitProjectNamePattern => GetRequired(nameof(MinecraftExplicitProjectNamePattern));
    /// <summary>
    /// Gets or sets minecraft named project pattern.
    /// </summary>
    public Regex MinecraftNamedProjectPattern => GetRequired(nameof(MinecraftNamedProjectPattern));
    /// <summary>
    /// Gets or sets markdown heading project name pattern.
    /// </summary>
    public Regex MarkdownHeadingProjectNamePattern => GetRequired(nameof(MarkdownHeadingProjectNamePattern));
    /// <summary>
    /// Gets or sets identifier separator pattern.
    /// </summary>
    public Regex IdentifierSeparatorPattern => GetRequired(nameof(IdentifierSeparatorPattern));
    /// <summary>
    /// Gets or sets alpha numeric word pattern.
    /// </summary>
    public Regex AlphaNumericWordPattern => GetRequired(nameof(AlphaNumericWordPattern));
    /// <summary>
    /// Gets or sets integer pattern.
    /// </summary>
    public Regex IntegerPattern => GetRequired(nameof(IntegerPattern));
    /// <summary>
    /// Gets or sets council DevExpress function call pattern.
    /// </summary>
    public Regex CouncilDxFunctionCallPattern => GetRequired(nameof(CouncilDxFunctionCallPattern));
    /// <summary>
    /// Gets or sets missing feature pattern.
    /// </summary>
    public Regex MissingFeaturePattern => GetRequired("builtin.missing-feature-pattern");
    /// <summary>
    /// Gets or sets sensitive name pattern.
    /// </summary>
    public Regex SensitiveNamePattern => GetRequired("builtin.sensitive-name-pattern");
    /// <summary>
    /// Gets or sets truncated tail pattern.
    /// </summary>
    public Regex TruncatedTailPattern => GetRequired("builtin.truncated-tail-pattern");
    /// <summary>
    /// Gets or sets target framework pattern.
    /// </summary>
    public Regex TargetFrameworkPattern => GetRequired("builtin.target-framework-pattern");
    /// <summary>
    /// Gets or sets package reference pattern.
    /// </summary>
    public Regex PackageReferencePattern => GetRequired("builtin.package-reference-pattern");
    /// <summary>
    /// Gets or sets thinking block pattern.
    /// </summary>
    public Regex ThinkingBlockPattern => GetRequired("builtin.thinking-block-pattern");
    /// <summary>
    /// Gets or sets capability gap block pattern.
    /// </summary>
    public Regex CapabilityGapBlockPattern => GetRequired("builtin.capability-gap-block-pattern");
    /// <summary>
    /// Gets or sets helpful source line pattern.
    /// </summary>
    public Regex HelpfulSourceLinePattern => GetRequired("builtin.helpful-source-line-pattern");
    /// <summary>
    /// Gets or sets stream status pattern.
    /// </summary>
    public Regex StreamStatusPattern => GetRequired("builtin.stream-status-pattern");
    /// <summary>
    /// Gets or sets minecraft pattern.
    /// </summary>
    public Regex MinecraftPattern => GetRequired("builtin.minecraft-pattern");
    /// <summary>
    /// Gets or sets datapack pattern.
    /// </summary>
    public Regex DatapackPattern => GetRequired("builtin.datapack-pattern");
    /// <summary>
    /// Gets or sets minecraft skeleton matrix pattern.
    /// </summary>
    public Regex MinecraftSkeletonMatrixPattern => GetRequired("builtin.minecraft-skeleton-matrix-pattern");
    /// <summary>
    /// Gets or sets minecraft version pattern.
    /// </summary>
    public Regex MinecraftVersionPattern => GetRequired("builtin.minecraft-version-pattern");
    /// <summary>
    /// Gets or sets dev express document pattern.
    /// </summary>
    public Regex DevExpressDocumentPattern => GetRequired("builtin.dev-express-document-pattern");
    /// <summary>
    /// Gets or sets blazor frontend pattern.
    /// </summary>
    public Regex BlazorFrontendPattern => GetRequired("builtin.blazor-frontend-pattern");
    /// <summary>
    /// Gets or sets dot net pattern.
    /// </summary>
    public Regex DotNetPattern => GetRequired("builtin.dot-net-pattern");
    /// <summary>
    /// Gets or sets frontend pattern.
    /// </summary>
    public Regex FrontendPattern => GetRequired("builtin.frontend-pattern");
    /// <summary>
    /// Gets or sets logging pattern.
    /// </summary>
    public Regex LoggingPattern => GetRequired("builtin.logging-pattern");

    /// <summary>
    /// Runs the extract structured field operation.
    /// </summary>
    public string? ExtractStructuredField(string body, string name)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(body);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            foreach (Match match in StructuredFieldPattern.Matches(body))
            {
                if (match.Groups["name"].Value.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return match.Groups["value"].Value.Trim();
            }

            return null;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                $"{nameof(ExtractStructuredField)} could not extract a structured field; field name and source content were omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Gets required.
    /// </summary>
    private Regex GetRequired(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        lock (_sync)
        {
            try
            {
                databaseInitializer.InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                using var db = dbContextFactory.CreateDbContext();
                var row = db.RegexPatterns.AsNoTracking().SingleOrDefault(item => item.Name == name)
                    ?? throw new KeyNotFoundException($"Required database regex '{name}' was not found.");
                var timeoutMilliseconds = ReadTimeoutMilliseconds(db);
                var flags = row.Flags ?? string.Empty;
                if (_cache.TryGetValue(name, out var cached) &&
                    cached.Pattern.Equals(row.Pattern, StringComparison.Ordinal) &&
                    cached.Flags.Equals(flags, StringComparison.Ordinal) &&
                    cached.TimeoutMilliseconds == timeoutMilliseconds)
                {
                    return cached.Regex;
                }

                var compiled = new Regex(
                    row.Pattern,
                    ParseFlags(flags),
                    TimeSpan.FromMilliseconds(timeoutMilliseconds));
                _cache[name] = new CouncilTextCachedPattern(row.Pattern, flags, timeoutMilliseconds, compiled);
                logger.LogDebug($"Loaded Council text regex {{RegexName}} from the database-backed catalog; pattern content omitted.", name);
                return compiled;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"Council text regex {{RegexName}} could not be loaded from the database-backed catalog.", name);
                throw;
            }
        }
    }

    /// <summary>
    /// Reads timeout milliseconds.
    /// </summary>
    private int ReadTimeoutMilliseconds(LocalGptMemoryDbContext db)
    {
        try
        {
            var definition = systemVariables.RegexMatchTimeoutMilliseconds;
            var raw = db.SystemVariables.AsNoTracking()
                .Where(item => item.Name == definition.Name)
                .Select(item => item.ValueString)
                .SingleOrDefault();
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
                ? parsed
                : definition.DefaultValue;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                $"{nameof(ReadTimeoutMilliseconds)} could not read the database-backed regular-expression timeout.");
            throw;
        }
    }

    /// <summary>
    /// Parses flags.
    /// </summary>
    private RegexOptions ParseFlags(string? flags)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(flags))
                return RegexOptions.CultureInvariant;

            var result = RegexOptions.CultureInvariant;
            foreach (var token in flags.Split([',', '|', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                result |= token.ToLowerInvariant() switch
                {
                    "i" or "ignorecase" => RegexOptions.IgnoreCase,
                    "m" or "multiline" => RegexOptions.Multiline,
                    "s" or "singleline" => RegexOptions.Singleline,
                    "x" or "ignorepatternwhitespace" => RegexOptions.IgnorePatternWhitespace,
                    "n" or "explicitcapture" => RegexOptions.ExplicitCapture,
                    "compiled" => RegexOptions.Compiled,
                    "c" or "cultureinvariant" => RegexOptions.CultureInvariant,
                    "ecmascript" => RegexOptions.ECMAScript,
                    "none" => RegexOptions.None,
                    _ when Enum.TryParse<RegexOptions>(token, true, out var parsed) => parsed,
                    _ => throw new InvalidDataException($"Unknown regular-expression option '{token}' in the database-backed catalog.")
                };
            }

            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                $"{nameof(ParseFlags)} could not parse database-backed regular-expression flags; flag content was omitted from logs.");
            throw;
        }
    }
}
