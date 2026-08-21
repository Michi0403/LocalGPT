using System.Diagnostics;
using System.Text;

namespace LocalGPT.Services;

/// <summary>
/// Watches one actively generated provider stream for sustained exact token-cycle repetition so a runaway model can be
/// stopped before it monopolizes a benchmark host road or Council member slot until the much larger request timeout.
/// The detector is deliberately conservative: it requires a substantial generated tail, repeated periodic agreement
/// across multiple time-spaced samples, and several complete cycles before it reports a failure.
/// </summary>
internal sealed class ProviderStreamRepetitionWatchdog
{
    /// <summary>Maximum recent provider text retained for repetition analysis.</summary>
    private const int MaximumBufferedCharacters = 32_768;

    /// <summary>Minimum generated character count required before repetition analysis may classify a stream.</summary>
    private const int MinimumObservedCharacters = 1_024;

    /// <summary>Minimum token count examined before a repeated cycle can be considered pathological.</summary>
    private const int MinimumAnalyzedTokens = 72;

    /// <summary>Maximum repeated token-cycle length considered by the bounded detector.</summary>
    private const int MaximumPeriodTokens = 512;

    /// <summary>Largest token-cycle length that retains the historical short-loop thresholds.</summary>
    private const int ShortPeriodMaximumTokens = 32;

    /// <summary>Minimum number of complete repeated cycles required for short token loops.</summary>
    private const int MinimumRepeatedCycles = 6;

    /// <summary>Minimum number of complete repeated cycles required for longer sentence/paragraph loops.</summary>
    private const int MinimumLongPeriodRepeatedCycles = 4;

    /// <summary>Minimum periodic token agreement required before a short-cycle sample is classified as suspicious.</summary>
    private const double MinimumPeriodicAgreement = 0.97d;

    /// <summary>Higher agreement floor used for longer cycles so ordinary prose is not classified as repetition.</summary>
    private const double MinimumLongPeriodAgreement = 0.985d;

    /// <summary>Number of consecutive suspicious time-spaced samples required before the stream is stopped.</summary>
    private const int RequiredSuspiciousSamples = 4;

    /// <summary>Diagnostics sink used when the bounded detector itself encounters an unexpected implementation failure.</summary>
    private readonly ILogger logger;

    /// <summary>Minimum generation time before the first repetition sample is evaluated.</summary>
    private readonly TimeSpan initialObservationDelay = TimeSpan.FromSeconds(4);

    /// <summary>Spacing between repetition samples while provider text continues to arrive.</summary>
    private readonly TimeSpan sampleInterval = TimeSpan.FromSeconds(2);

    /// <summary>Minimum wall-clock duration over which suspicious repetition must remain present.</summary>
    private readonly TimeSpan minimumSuspiciousDuration = TimeSpan.FromSeconds(6);

    /// <summary>Recent provider-generated text retained as a bounded rolling window.</summary>
    private readonly StringBuilder recentText = new();

    /// <summary>Wall-clock generation timer started by the first substantive provider fragment.</summary>
    private readonly Stopwatch generationClock = new();

    /// <summary>Total provider-generated characters observed since this watchdog was created.</summary>
    private long observedCharacters;

    /// <summary>Elapsed generation time at which the previous periodicity sample was evaluated.</summary>
    private TimeSpan lastSampleAt;

    /// <summary>Elapsed generation time at which the current run of suspicious samples began.</summary>
    private TimeSpan? suspiciousSince;

    /// <summary>Number of consecutive suspicious samples observed at the configured sample interval.</summary>
    private int suspiciousSamples;

    /// <summary>Creates a conservative repetition watchdog using the caller's normal LocalGPT diagnostics sink.</summary>
    /// <param name="logger">Diagnostics logger used only when watchdog implementation logic itself fails.</param>
    public ProviderStreamRepetitionWatchdog(ILogger logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Observes one provider-generated fragment and returns a bounded failure when sustained periodic repetition has
    /// crossed every safety threshold. Callers should cancel/dispose only the current provider request before throwing
    /// the returned exception so normal user cancellation and larger workflow cancellation remain distinguishable.
    /// </summary>
    /// <param name="fragment">The provider-generated text fragment. Status-only application messages should not be supplied.</param>
    /// <returns>A repetition failure when the stream is classified as runaway; otherwise <see langword="null"/>.</returns>
    public ProviderStreamRepetitionException? Observe(string? fragment)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fragment))
                return null;

            if (!generationClock.IsRunning)
                generationClock.Start();

            observedCharacters += fragment.Length;
            recentText.Append(fragment);
            if (recentText.Length > MaximumBufferedCharacters)
                recentText.Remove(0, recentText.Length - MaximumBufferedCharacters);

            var elapsed = generationClock.Elapsed;
            if (observedCharacters < MinimumObservedCharacters || elapsed < initialObservationDelay)
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
            if (suspiciousSamples < RequiredSuspiciousSamples || elapsed - suspiciousSince.Value < minimumSuspiciousDuration)
                return null;

            return new ProviderStreamRepetitionException(
                patternPreview,
                periodTokens,
                agreement,
                elapsed.TotalSeconds,
                suspiciousSamples);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Provider stream repetition observation failed; provider content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Finds an exact-ish periodic token cycle in the bounded text tail. Short cycles retain the historical six-cycle
    /// and 97% agreement thresholds; longer sentence/paragraph cycles require four complete cycles and a stricter 98.5%
    /// agreement floor. Broad lexical-diversity heuristics are intentionally avoided so legitimate prose, source code,
    /// tables, and enumerations are not classified merely because they reuse vocabulary.
    /// </summary>
    /// <param name="text">Bounded recent provider text.</param>
    /// <param name="patternPreview">Receives a short human-readable token-cycle preview when repetition is found.</param>
    /// <param name="periodTokens">Receives the detected token-cycle length.</param>
    /// <param name="agreement">Receives the periodic token-agreement ratio from zero through one.</param>
    /// <returns><see langword="true"/> when a sustained-candidate token cycle is present in the current text sample.</returns>
    private bool TryFindRepeatedTokenCycle(
        string text,
        out string patternPreview,
        out int periodTokens,
        out double agreement)
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

            if (tokens.Count < MinimumAnalyzedTokens)
                return false;

            var maximumPeriod = Math.Min(MaximumPeriodTokens, tokens.Count / MinimumLongPeriodRepeatedCycles);
            for (var period = 1; period <= maximumPeriod; period++)
            {
                var isLongPeriod = period > ShortPeriodMaximumTokens;
                var requiredCycles = isLongPeriod ? MinimumLongPeriodRepeatedCycles : MinimumRepeatedCycles;
                var requiredAgreement = isLongPeriod ? MinimumLongPeriodAgreement : MinimumPeriodicAgreement;
                var analyzedTokenCount = Math.Max(MinimumAnalyzedTokens, period * requiredCycles);
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

/// <summary>
/// Identifies a provider request that LocalGPT intentionally terminated because its actively generated output remained
/// trapped in the same short token cycle across several time-spaced watchdog samples.
/// </summary>
internal sealed class ProviderStreamRepetitionException : InvalidOperationException
{
    /// <summary>
    /// Creates a repetition failure containing only bounded diagnostic metadata; the full model output remains in the
    /// normal provider-stream evidence surface instead of being duplicated into exception logs.
    /// </summary>
    /// <param name="patternPreview">Bounded preview of the repeated token cycle retained in memory for optional UI diagnostics.</param>
    /// <param name="periodTokens">Detected cycle length in normalized tokens.</param>
    /// <param name="agreement">Periodic token-agreement ratio from zero through one.</param>
    /// <param name="observedSeconds">Elapsed active-generation time when the watchdog stopped the request.</param>
    /// <param name="suspiciousSamples">Number of consecutive suspicious samples that triggered the stop.</param>
    public ProviderStreamRepetitionException(
        string patternPreview,
        int periodTokens,
        double agreement,
        double observedSeconds,
        int suspiciousSamples)
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

    /// <summary>Gets the bounded repeated token-cycle preview retained in memory but omitted from exception logs.</summary>
    /// <value>The normalized repeated-pattern preview.</value>
    public string PatternPreview { get; }

    /// <summary>Reports how many normalized tokens form one detected repetition cycle.</summary>
    /// <value>The period length in normalized tokens.</value>
    public int PeriodTokens { get; }

    /// <summary>Reports the sampled tail agreement with the detected periodic token cycle.</summary>
    /// <value>A value from zero through one.</value>
    public double Agreement { get; }

    /// <summary>Reports how long active generation had run when LocalGPT terminated the repeated stream.</summary>
    /// <value>The elapsed generation time in seconds.</value>
    public double ObservedSeconds { get; }

    /// <summary>Reports how many time-spaced suspicious samples remained consecutive at termination.</summary>
    /// <value>The number of time-spaced suspicious samples.</value>
    public int SuspiciousSamples { get; }
}
