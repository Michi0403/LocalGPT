using System.Diagnostics;
using System.Text;

namespace LocalGPT.Services;

/// <summary>
/// Optionally watches one actively generated provider stream for sustained token-cycle repetition. The feature is
/// operator controlled through the persisted LocalGPT runtime policy and is disabled in the shipped policy so a local
/// model is never terminated by an invisible developer ceiling unless the operator explicitly enables that behavior.
/// </summary>
internal sealed class ProviderStreamRepetitionWatchdog
{
    private readonly ILogger logger;
    private readonly bool enabled;
    private readonly int maximumBufferedCharacters;
    private readonly int minimumObservedCharacters;
    private readonly int minimumAnalyzedTokens;
    private readonly int maximumPeriodTokens;
    private readonly int shortPeriodMaximumTokens;
    private readonly int minimumRepeatedCycles;
    private readonly int minimumLongPeriodRepeatedCycles;
    private readonly double minimumPeriodicAgreement;
    private readonly double minimumLongPeriodAgreement;
    private readonly int requiredSuspiciousSamples;
    private readonly TimeSpan initialObservationDelay;
    private readonly TimeSpan sampleInterval;
    private readonly TimeSpan minimumSuspiciousDuration;
    private readonly StringBuilder recentText = new();
    private readonly Stopwatch generationClock = new();
    private long observedCharacters;
    private TimeSpan lastSampleAt;
    private TimeSpan? suspiciousSince;
    private int suspiciousSamples;

    /// <summary>Creates a repetition watchdog from database-backed operator policy.</summary>
    public ProviderStreamRepetitionWatchdog(LocalGptCatalogService catalog, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        enabled = catalog.ProviderStreamRepetitionWatchdogEnabled;
        maximumBufferedCharacters = Math.Max(1, catalog.ProviderStreamRepetitionMaximumBufferedCharacters);
        minimumObservedCharacters = Math.Max(1, catalog.ProviderStreamRepetitionMinimumObservedCharacters);
        minimumAnalyzedTokens = Math.Max(2, catalog.ProviderStreamRepetitionMinimumAnalyzedTokens);
        maximumPeriodTokens = Math.Max(1, catalog.ProviderStreamRepetitionMaximumPeriodTokens);
        shortPeriodMaximumTokens = Math.Max(1, catalog.ProviderStreamRepetitionShortPeriodMaximumTokens);
        minimumRepeatedCycles = Math.Max(2, catalog.ProviderStreamRepetitionMinimumRepeatedCycles);
        minimumLongPeriodRepeatedCycles = Math.Max(2, catalog.ProviderStreamRepetitionMinimumLongPeriodRepeatedCycles);
        minimumPeriodicAgreement = Math.Clamp(catalog.ProviderStreamRepetitionMinimumPeriodicAgreementBasisPoints / 10_000d, 0d, 1d);
        minimumLongPeriodAgreement = Math.Clamp(catalog.ProviderStreamRepetitionMinimumLongPeriodAgreementBasisPoints / 10_000d, 0d, 1d);
        requiredSuspiciousSamples = Math.Max(1, catalog.ProviderStreamRepetitionRequiredSuspiciousSamples);
        initialObservationDelay = TimeSpan.FromMilliseconds(Math.Max(0, catalog.ProviderStreamRepetitionInitialObservationMilliseconds));
        sampleInterval = TimeSpan.FromMilliseconds(Math.Max(0, catalog.ProviderStreamRepetitionSampleIntervalMilliseconds));
        minimumSuspiciousDuration = TimeSpan.FromMilliseconds(Math.Max(0, catalog.ProviderStreamRepetitionMinimumSuspiciousDurationMilliseconds));
    }

    /// <summary>Observes one provider-generated fragment and returns a failure only when the operator enabled the watchdog.</summary>
    public ProviderStreamRepetitionException? Observe(string? fragment)
    {
        try
        {
            if (!enabled || string.IsNullOrWhiteSpace(fragment))
                return null;

            if (!generationClock.IsRunning)
                generationClock.Start();

            observedCharacters += fragment.Length;
            recentText.Append(fragment);
            if (recentText.Length > maximumBufferedCharacters)
                recentText.Remove(0, recentText.Length - maximumBufferedCharacters);

            var elapsed = generationClock.Elapsed;
            if (observedCharacters < minimumObservedCharacters || elapsed < initialObservationDelay)
                return null;
            if (lastSampleAt != TimeSpan.Zero && elapsed - lastSampleAt < sampleInterval)
                return null;

            lastSampleAt = elapsed;
            if (!TryFindRepeatedTokenCycle(recentText.ToString(), out var patternPreview, out var periodTokens, out var agreement))
            {
                suspiciousSamples = 0;
                suspiciousSince = null;
                return null;
            }

            suspiciousSamples++;
            suspiciousSince ??= elapsed;
            if (suspiciousSamples < requiredSuspiciousSamples || elapsed - suspiciousSince.Value < minimumSuspiciousDuration)
                return null;

            return new ProviderStreamRepetitionException(patternPreview, periodTokens, agreement, elapsed.TotalSeconds, suspiciousSamples);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Provider stream repetition observation failed; provider content was omitted.");
            throw;
        }
    }

    private bool TryFindRepeatedTokenCycle(string text, out string patternPreview, out int periodTokens, out double agreement)
    {
        try
        {
            patternPreview = string.Empty;
            periodTokens = 0;
            agreement = 0d;
            var tokens = new List<string>();
            var token = new StringBuilder();
            foreach (var character in text)
            {
                if (char.IsLetterOrDigit(character) || character == '_')
                {
                    token.Append(char.ToLowerInvariant(character));
                    continue;
                }
                if (token.Length > 0)
                {
                    tokens.Add(token.ToString());
                    token.Clear();
                }
            }
            if (token.Length > 0)
                tokens.Add(token.ToString());
            if (tokens.Count < minimumAnalyzedTokens)
                return false;

            var maximumPeriod = Math.Min(maximumPeriodTokens, tokens.Count / minimumLongPeriodRepeatedCycles);
            for (var period = 1; period <= maximumPeriod; period++)
            {
                var isLongPeriod = period > shortPeriodMaximumTokens;
                var requiredCycles = isLongPeriod ? minimumLongPeriodRepeatedCycles : minimumRepeatedCycles;
                var requiredAgreement = isLongPeriod ? minimumLongPeriodAgreement : minimumPeriodicAgreement;
                var analyzedTokenCount = Math.Max(minimumAnalyzedTokens, period * requiredCycles);
                if (tokens.Count < analyzedTokenCount)
                    continue;

                var start = tokens.Count - analyzedTokenCount;
                var comparisons = analyzedTokenCount - period;
                var matches = 0;
                for (var index = start + period; index < tokens.Count; index++)
                {
                    if (tokens[index].Equals(tokens[index - period], StringComparison.Ordinal))
                        matches++;
                }

                var currentAgreement = comparisons <= 0 ? 0d : matches / (double)comparisons;
                if (currentAgreement < requiredAgreement)
                    continue;

                periodTokens = period;
                agreement = currentAgreement;
                patternPreview = string.Join(' ', tokens.Skip(tokens.Count - period).Take(period));
                if (patternPreview.Length > 180)
                    patternPreview = patternPreview[..180] + "…";
                return true;
            }
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Provider stream repetition periodicity analysis failed; provider content was omitted.");
            throw;
        }
    }
}

/// <summary>Identifies a provider request intentionally stopped by an explicitly enabled repetition watchdog.</summary>
internal sealed class ProviderStreamRepetitionException : InvalidOperationException
{
    public ProviderStreamRepetitionException(string patternPreview, int periodTokens, double agreement, double observedSeconds, int suspiciousSamples)
        : base(
            $"Provider stream repetition watchdog stopped runaway generation after {observedSeconds:0.0}s because a " +
            $"{periodTokens}-token cycle remained at {agreement:P1} periodic agreement across {suspiciousSamples} consecutive samples. " +
            "The repeated output itself remains in provider-stream evidence and is omitted from exception text.")
    {
        PatternPreview = patternPreview;
        PeriodTokens = periodTokens;
        Agreement = agreement;
        ObservedSeconds = observedSeconds;
        SuspiciousSamples = suspiciousSamples;
    }

    public string PatternPreview { get; }
    public int PeriodTokens { get; }
    public double Agreement { get; }
    public double ObservedSeconds { get; }
    public int SuspiciousSamples { get; }
}
