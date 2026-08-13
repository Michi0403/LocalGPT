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
/// <param name="dbContextFactory">Local gpt memory database context dependency used by the council text pattern workflow to provide the corresponding application capability.</param>
/// <param name="databaseInitializer">Database initialization service dependency used by the council text pattern workflow to provide the corresponding application capability.</param>
/// <param name="systemVariables">System variable definition service dependency used by the council text pattern workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CouncilTextPatternDataService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ISystemVariableDefinitionService systemVariables,
    ILogger<CouncilTextPatternDataService> logger) : ICouncilTextPatternDataService
{
    /// <summary>
    /// Stores the internal sync state used by <see cref="CouncilTextPatternDataService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object _sync = new();
    /// <summary>
    /// Stores the in-memory cache collection maintained internally by <see cref="CouncilTextPatternDataService"/> for its current workflow state.
    /// </summary>
    private readonly Dictionary<string, CouncilTextCachedPattern> _cache = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the former thought break pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The former thought break pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex FormerThoughtBreakPattern => GetRequired(nameof(FormerThoughtBreakPattern));
    /// <summary>
    /// Gets the former thought code wrapper pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The former thought code wrapper pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex FormerThoughtCodeWrapperPattern => GetRequired(nameof(FormerThoughtCodeWrapperPattern));
    /// <summary>
    /// Gets the former thought opening fence pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The former thought opening fence pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex FormerThoughtOpeningFencePattern => GetRequired(nameof(FormerThoughtOpeningFencePattern));
    /// <summary>
    /// Gets the former thought closing fence pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The former thought closing fence pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex FormerThoughtClosingFencePattern => GetRequired(nameof(FormerThoughtClosingFencePattern));
    /// <summary>
    /// Gets the former thought presentation wrapper pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The former thought presentation wrapper pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex FormerThoughtPresentationWrapperPattern => GetRequired(nameof(FormerThoughtPresentationWrapperPattern));
    /// <summary>
    /// Gets the former thought excess line break pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The former thought excess line break pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex FormerThoughtExcessLineBreakPattern => GetRequired(nameof(FormerThoughtExcessLineBreakPattern));
    /// <summary>
    /// Gets the whitespace pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The whitespace pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex WhitespacePattern => GetRequired("builtin.whitespace-pattern");
    /// <summary>
    /// Gets the name cleaner pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name cleaner pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex NameCleanerPattern => GetRequired("builtin.name-cleaner");
    /// <summary>
    /// Gets the mod identifier cleaner pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The mod identifier cleaner pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex ModIdCleanerPattern => GetRequired("builtin.mod-id-cleaner");
    /// <summary>
    /// Gets the package part cleaner pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The package part cleaner pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex PackagePartCleanerPattern => GetRequired("builtin.package-part-cleaner");
    /// <summary>
    /// Gets the structured field pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The structured field pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex StructuredFieldPattern => GetRequired(nameof(StructuredFieldPattern));
    /// <summary>
    /// Gets the knowledge block pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The knowledge block pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex KnowledgeBlockPattern => GetRequired("builtin.localgpt-knowledge-block");
    /// <summary>
    /// Gets the minecraft quoted project name pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minecraft quoted project name pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex MinecraftQuotedProjectNamePattern => GetRequired(nameof(MinecraftQuotedProjectNamePattern));
    /// <summary>
    /// Gets the minecraft explicit project name pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minecraft explicit project name pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex MinecraftExplicitProjectNamePattern => GetRequired(nameof(MinecraftExplicitProjectNamePattern));
    /// <summary>
    /// Gets the minecraft named project pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minecraft named project pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex MinecraftNamedProjectPattern => GetRequired(nameof(MinecraftNamedProjectPattern));
    /// <summary>
    /// Gets the markdown heading project name pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The markdown heading project name pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex MarkdownHeadingProjectNamePattern => GetRequired(nameof(MarkdownHeadingProjectNamePattern));
    /// <summary>
    /// Gets the identifier separator pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The identifier separator pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex IdentifierSeparatorPattern => GetRequired(nameof(IdentifierSeparatorPattern));
    /// <summary>
    /// Gets the alpha numeric word pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The alpha numeric word pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex AlphaNumericWordPattern => GetRequired(nameof(AlphaNumericWordPattern));
    /// <summary>
    /// Gets the integer pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The integer pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex IntegerPattern => GetRequired(nameof(IntegerPattern));
    /// <summary>
    /// Gets the council DevExpress function call pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council DevExpress function call pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex CouncilDxFunctionCallPattern => GetRequired(nameof(CouncilDxFunctionCallPattern));
    /// <summary>
    /// Gets the missing feature pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The missing feature pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex MissingFeaturePattern => GetRequired("builtin.missing-feature-pattern");
    /// <summary>
    /// Gets the sensitive name pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sensitive name pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex SensitiveNamePattern => GetRequired("builtin.sensitive-name-pattern");
    /// <summary>
    /// Gets the truncated tail pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The truncated tail pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex TruncatedTailPattern => GetRequired("builtin.truncated-tail-pattern");
    /// <summary>
    /// Gets the target framework pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target framework pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex TargetFrameworkPattern => GetRequired("builtin.target-framework-pattern");
    /// <summary>
    /// Gets the package reference pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The package reference pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex PackageReferencePattern => GetRequired("builtin.package-reference-pattern");
    /// <summary>
    /// Gets the thinking block pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The thinking block pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex ThinkingBlockPattern => GetRequired("builtin.thinking-block-pattern");
    /// <summary>
    /// Gets the capability gap block pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The capability gap block pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex CapabilityGapBlockPattern => GetRequired("builtin.capability-gap-block-pattern");
    /// <summary>
    /// Gets the helpful source line pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The helpful source line pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex HelpfulSourceLinePattern => GetRequired("builtin.helpful-source-line-pattern");
    /// <summary>
    /// Gets the stream status pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The stream status pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex StreamStatusPattern => GetRequired("builtin.stream-status-pattern");
    /// <summary>
    /// Gets the minecraft pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minecraft pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex MinecraftPattern => GetRequired("builtin.minecraft-pattern");
    /// <summary>
    /// Gets the datapack pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The datapack pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex DatapackPattern => GetRequired("builtin.datapack-pattern");
    /// <summary>
    /// Gets the minecraft skeleton matrix pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minecraft skeleton matrix pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex MinecraftSkeletonMatrixPattern => GetRequired("builtin.minecraft-skeleton-matrix-pattern");
    /// <summary>
    /// Gets the minecraft version pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minecraft version pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex MinecraftVersionPattern => GetRequired("builtin.minecraft-version-pattern");
    /// <summary>
    /// Gets the DevExpress document pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The DevExpress document pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex DevExpressDocumentPattern => GetRequired("builtin.dev-express-document-pattern");
    /// <summary>
    /// Gets the blazor frontend pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The blazor frontend pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex BlazorFrontendPattern => GetRequired("builtin.blazor-frontend-pattern");
    /// <summary>
    /// Gets the dot net pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The dot net pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex DotNetPattern => GetRequired("builtin.dot-net-pattern");
    /// <summary>
    /// Gets the frontend pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frontend pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex FrontendPattern => GetRequired("builtin.frontend-pattern");
    /// <summary>
    /// Gets the logging pattern value that forms part of the council text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The logging pattern value exposed by <see cref="CouncilTextPatternDataService"/>.</value>
    public Regex LoggingPattern => GetRequired("builtin.logging-pattern");

    /// <summary>
    /// Performs extract structured field as part of the council text pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="body">Body value supplied to the council text pattern operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the council text pattern operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Retrieves required as part of the council text pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the council text pattern operation and used when producing its result.</param>
    /// <returns>The regex produced by the operation.</returns>
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
    /// Reads timeout milliseconds as part of the council text pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="db">Database value supplied to the council text pattern operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
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
    /// Parses flags as part of the council text pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="flags">Flags value supplied to the council text pattern operation and used when producing its result.</param>
    /// <returns>The regex options produced by the operation.</returns>
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
