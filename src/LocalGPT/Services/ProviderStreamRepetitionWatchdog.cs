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
    /// <summary>
    /// Stores the logger used by <see cref="ProviderStreamRepetitionWatchdog"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger logger;
    /// <summary>
    /// Stores the internal enabled state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private readonly bool enabled;
    /// <summary>
    /// Stores the internal maximum buffered characters state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private readonly int maximumBufferedCharacters;
    /// <summary>
    /// Stores the internal minimum observed characters state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private readonly int minimumObservedCharacters;
    /// <summary>
    /// Stores the internal minimum analyzed tokens state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private readonly int minimumAnalyzedTokens;
    /// <summary>
    /// Stores the internal maximum period tokens state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private readonly int maximumPeriodTokens;
    /// <summary>
    /// Stores the internal short period maximum tokens state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private readonly int shortPeriodMaximumTokens;
    /// <summary>
    /// Stores the internal minimum repeated cycles state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private readonly int minimumRepeatedCycles;
    /// <summary>
    /// Stores the internal minimum long period repeated cycles state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private readonly int minimumLongPeriodRepeatedCycles;
    /// <summary>
    /// Stores the internal minimum periodic agreement state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private readonly double minimumPeriodicAgreement;
    /// <summary>
    /// Stores the internal minimum long period agreement state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private readonly double minimumLongPeriodAgreement;
    /// <summary>
    /// Stores the internal required suspicious samples state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private readonly int requiredSuspiciousSamples;
    /// <summary>
    /// Stores the internal initial observation delay state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private readonly TimeSpan initialObservationDelay;
    /// <summary>
    /// Stores the internal sample interval state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private readonly TimeSpan sampleInterval;
    /// <summary>
    /// Stores the internal minimum suspicious duration state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private readonly TimeSpan minimumSuspiciousDuration;
    /// <summary>
    /// Stores the internal recent text state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private readonly StringBuilder recentText = new();
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to generation clock state owned by <see cref="ProviderStreamRepetitionWatchdog"/>.
    /// </summary>
    private readonly Stopwatch generationClock = new();
    /// <summary>
    /// Stores the internal observed characters state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private long observedCharacters;
    /// <summary>
    /// Stores the internal last sample at state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private TimeSpan lastSampleAt;
    /// <summary>
    /// Stores the internal suspicious since state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private TimeSpan? suspiciousSince;
    /// <summary>
    /// Stores the internal suspicious samples state used by <see cref="ProviderStreamRepetitionWatchdog"/> while executing its surrounding workflow.
    /// </summary>
    private int suspiciousSamples;

    /// <summary>Creates a repetition watchdog from database-backed operator policy.</summary>
    /// <param name="catalog">Local gpt catalog service dependency used by the provider stream repetition watchdog workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
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
    /// <param name="fragment">Fragment value supplied to the provider stream repetition watchdog operation and used when producing its result.</param>
    /// <returns>The provider stream repetition exception produced by the operation.</returns>
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

    /// <summary>
    /// Attempts to find repeated token cycle for <see cref="ProviderStreamRepetitionWatchdog"/>, keeping the operation consistent with the state and invariants of the surrounding provider stream repetition watchdog workflow.
    /// </summary>
    /// <param name="text">Text value supplied to the provider stream repetition watchdog operation and used when producing its result.</param>
    /// <param name="patternPreview">Pattern preview value supplied to the provider stream repetition watchdog operation and used when producing its result.</param>
    /// <param name="periodTokens">Period tokens value supplied to the provider stream repetition watchdog operation and used when producing its result.</param>
    /// <param name="agreement">Agreement value supplied to the provider stream repetition watchdog operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// <summary>
    /// Initializes a new <see cref="ProviderStreamRepetitionException"/> instance and captures the dependencies or initial state required by its provider stream repetition exception workflow.
    /// </summary>
    /// <param name="patternPreview">Pattern preview value supplied to the provider stream repetition exception operation and used when producing its result.</param>
    /// <param name="periodTokens">Period tokens value supplied to the provider stream repetition exception operation and used when producing its result.</param>
    /// <param name="agreement">Agreement value supplied to the provider stream repetition exception operation and used when producing its result.</param>
    /// <param name="observedSeconds">Observed seconds value supplied to the provider stream repetition exception operation and used when producing its result.</param>
    /// <param name="suspiciousSamples">Suspicious samples value supplied to the provider stream repetition exception operation and used when producing its result.</param>
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

    /// <summary>
    /// Gets the pattern preview value that forms part of the provider stream repetition exception state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The pattern preview value exposed by <see cref="ProviderStreamRepetitionException"/>.</value>
    public string PatternPreview { get; }
    /// <summary>
    /// Gets the period tokens value that forms part of the provider stream repetition exception state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The period tokens value exposed by <see cref="ProviderStreamRepetitionException"/>.</value>
    public int PeriodTokens { get; }
    /// <summary>
    /// Gets the agreement value that forms part of the provider stream repetition exception state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The agreement value exposed by <see cref="ProviderStreamRepetitionException"/>.</value>
    public double Agreement { get; }
    /// <summary>
    /// Gets the observed seconds value that forms part of the provider stream repetition exception state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The observed seconds value exposed by <see cref="ProviderStreamRepetitionException"/>.</value>
    public double ObservedSeconds { get; }
    /// <summary>
    /// Gets the suspicious samples value that forms part of the provider stream repetition exception state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The suspicious samples value exposed by <see cref="ProviderStreamRepetitionException"/>.</value>
    public int SuspiciousSamples { get; }
}
