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
    private readonly object _sync = new();
    private readonly Dictionary<string, CachedPattern> _cache = new(StringComparer.Ordinal);

    public Regex FormerThoughtBreakPattern => GetRequired(nameof(FormerThoughtBreakPattern));
    public Regex FormerThoughtCodeWrapperPattern => GetRequired(nameof(FormerThoughtCodeWrapperPattern));
    public Regex FormerThoughtOpeningFencePattern => GetRequired(nameof(FormerThoughtOpeningFencePattern));
    public Regex FormerThoughtClosingFencePattern => GetRequired(nameof(FormerThoughtClosingFencePattern));
    public Regex FormerThoughtPresentationWrapperPattern => GetRequired(nameof(FormerThoughtPresentationWrapperPattern));
    public Regex FormerThoughtExcessLineBreakPattern => GetRequired(nameof(FormerThoughtExcessLineBreakPattern));
    public Regex WhitespacePattern => GetRequired("builtin.whitespace-pattern");
    public Regex NameCleanerPattern => GetRequired("builtin.name-cleaner");
    public Regex ModIdCleanerPattern => GetRequired("builtin.mod-id-cleaner");
    public Regex PackagePartCleanerPattern => GetRequired("builtin.package-part-cleaner");
    public Regex StructuredFieldPattern => GetRequired(nameof(StructuredFieldPattern));
    public Regex KnowledgeBlockPattern => GetRequired("builtin.localgpt-knowledge-block");
    public Regex MinecraftQuotedProjectNamePattern => GetRequired(nameof(MinecraftQuotedProjectNamePattern));
    public Regex MinecraftExplicitProjectNamePattern => GetRequired(nameof(MinecraftExplicitProjectNamePattern));
    public Regex MinecraftNamedProjectPattern => GetRequired(nameof(MinecraftNamedProjectPattern));
    public Regex MarkdownHeadingProjectNamePattern => GetRequired(nameof(MarkdownHeadingProjectNamePattern));
    public Regex IdentifierSeparatorPattern => GetRequired(nameof(IdentifierSeparatorPattern));
    public Regex AlphaNumericWordPattern => GetRequired(nameof(AlphaNumericWordPattern));
    public Regex IntegerPattern => GetRequired(nameof(IntegerPattern));
    public Regex CouncilDxFunctionCallPattern => GetRequired(nameof(CouncilDxFunctionCallPattern));
    public Regex MissingFeaturePattern => GetRequired("builtin.missing-feature-pattern");
    public Regex SensitiveNamePattern => GetRequired("builtin.sensitive-name-pattern");
    public Regex TruncatedTailPattern => GetRequired("builtin.truncated-tail-pattern");
    public Regex TargetFrameworkPattern => GetRequired("builtin.target-framework-pattern");
    public Regex PackageReferencePattern => GetRequired("builtin.package-reference-pattern");
    public Regex ThinkingBlockPattern => GetRequired("builtin.thinking-block-pattern");
    public Regex CapabilityGapBlockPattern => GetRequired("builtin.capability-gap-block-pattern");
    public Regex HelpfulSourceLinePattern => GetRequired("builtin.helpful-source-line-pattern");
    public Regex StreamStatusPattern => GetRequired("builtin.stream-status-pattern");
    public Regex MinecraftPattern => GetRequired("builtin.minecraft-pattern");
    public Regex DatapackPattern => GetRequired("builtin.datapack-pattern");
    public Regex MinecraftSkeletonMatrixPattern => GetRequired("builtin.minecraft-skeleton-matrix-pattern");
    public Regex MinecraftVersionPattern => GetRequired("builtin.minecraft-version-pattern");
    public Regex DevExpressDocumentPattern => GetRequired("builtin.dev-express-document-pattern");
    public Regex BlazorFrontendPattern => GetRequired("builtin.blazor-frontend-pattern");
    public Regex DotNetPattern => GetRequired("builtin.dot-net-pattern");
    public Regex FrontendPattern => GetRequired("builtin.frontend-pattern");
    public Regex LoggingPattern => GetRequired("builtin.logging-pattern");

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
                _cache[name] = new CachedPattern(row.Pattern, flags, timeoutMilliseconds, compiled);
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

    private sealed record CachedPattern(string Pattern, string Flags, int TimeoutMilliseconds, Regex Regex);

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
