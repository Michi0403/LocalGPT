using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Owns presentation-only color normalization and semantic styling for the shared ASCII surface.</summary>
public sealed partial class CouncilGameSessionService
{
    /// <summary>
    /// Defines the maximum ascii style runs constant used by <see cref="CouncilGameSessionService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MaximumAsciiStyleRuns = 4096;

    /// <summary>
    /// Retrieves ascii palette as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public CouncilAsciiPaletteSnapshot GetAsciiPalette()
    {
        try
        {
            return new CouncilAsciiPaletteSnapshot
            {
                Modes =
                [
                    new() { Mode = CouncilAsciiColorMode.TerminalDefault, Description = "Keep LocalGPT's existing black/green terminal appearance for unstyled cells. Explicit style runs may still select indexed colors.", DefaultForegroundColor = 46, DefaultBackgroundColor = 0 },
                    new() { Mode = CouncilAsciiColorMode.Ansi16, Description = "Use standard ANSI 16-color indexes 0-15. LocalGPT defaults to bright green 10 on black 0.", DefaultForegroundColor = 10, DefaultBackgroundColor = 0 },
                    new() { Mode = CouncilAsciiColorMode.Indexed256, Description = "Use xterm-compatible indexed colors 0-255. LocalGPT defaults to green 46 on black 0.", DefaultForegroundColor = 46, DefaultBackgroundColor = 0 }
                ],
                Ansi16 =
                [
                    new() { Index = 0, Name = "black" }, new() { Index = 1, Name = "red" }, new() { Index = 2, Name = "green" }, new() { Index = 3, Name = "yellow" },
                    new() { Index = 4, Name = "blue" }, new() { Index = 5, Name = "magenta" }, new() { Index = 6, Name = "cyan" }, new() { Index = 7, Name = "white" },
                    new() { Index = 8, Name = "bright-black" }, new() { Index = 9, Name = "bright-red" }, new() { Index = 10, Name = "bright-green" }, new() { Index = 11, Name = "bright-yellow" },
                    new() { Index = 12, Name = "bright-blue" }, new() { Index = 13, Name = "bright-magenta" }, new() { Index = 14, Name = "bright-cyan" }, new() { Index = 15, Name = "bright-white" }
                ],
                Indexed256Minimum = 0,
                Indexed256Maximum = 255,
                AuthoringRules =
                [
                    "Keep canonical ASCII text plain. Never emit raw ANSI escape sequences or HTML into frame text.",
                    "Use style metadata only where color adds meaning; unstyled TerminalDefault cells retain the LocalGPT black/green theme.",
                    "For 0.8B-2B models prefer one styled text/blit/fill operation over many individual cell writes.",
                    "Read localgpt.ascii.surface.get and localgpt.game.display.palette.get before inventing dimensions, palette indexes or layout rules.",
                    "Use Project requirements/profile/build data as design truth, then narrow knowledge and tested database regex evidence only when parsing is actually needed."
                ]
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the LocalGPT ASCII palette description failed.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes ascii color mode as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="mode">Mode value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The council ascii color mode produced by the operation.</returns>
    private CouncilAsciiColorMode NormalizeAsciiColorMode(CouncilAsciiColorMode mode)
    {
        try
        {
            return Enum.IsDefined(typeof(CouncilAsciiColorMode), mode) ? mode : CouncilAsciiColorMode.TerminalDefault;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Normalizing an ASCII color mode failed.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes ascii color index as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="mode">Mode value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="value">Value value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="foreground">Value indicating whether foreground should apply to this operation.</param>
    /// <returns>The int produced by the operation.</returns>
    private int NormalizeAsciiColorIndex(CouncilAsciiColorMode mode, int value, bool foreground)
    {
        try
        {
            var normalizedMode = NormalizeAsciiColorMode(mode);
            if (normalizedMode == CouncilAsciiColorMode.Ansi16)
                return value is >= 0 and <= 15 ? value : foreground ? 10 : 0;
            if (normalizedMode == CouncilAsciiColorMode.Indexed256)
                return value is >= 0 and <= 255 ? value : foreground ? 46 : 0;
            return value is >= 0 and <= 255 ? value : foreground ? 46 : 0;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Normalizing an ASCII palette index failed.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes ascii style as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="style">Style value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="sessionMode">Session mode value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The council ascii text style produced by the operation.</returns>
    private CouncilAsciiTextStyle? NormalizeAsciiStyle(CouncilAsciiTextStyle? style, CouncilAsciiColorMode sessionMode)
    {
        try
        {
            if (style is null)
                return null;
            var effectiveMode = style.ColorMode ?? (sessionMode == CouncilAsciiColorMode.TerminalDefault
                ? CouncilAsciiColorMode.Indexed256
                : sessionMode);
            effectiveMode = NormalizeAsciiColorMode(effectiveMode);
            return new CouncilAsciiTextStyle
            {
                ColorMode = effectiveMode,
                ForegroundColor = style.ForegroundColor is int foreground ? NormalizeAsciiColorIndex(effectiveMode, foreground, true) : null,
                BackgroundColor = style.BackgroundColor is int background ? NormalizeAsciiColorIndex(effectiveMode, background, false) : null,
                Bold = style.Bold,
                Dim = style.Dim,
                Invert = style.Invert
            };
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Normalizing ASCII text style metadata failed.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes ascii style runs as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runs">Council ascii style run dependency used by the council game session workflow to provide the corresponding application capability.</param>
    /// <param name="frameWidth">Frame width value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="frameHeight">Frame height value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="sessionMode">Session mode value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<CouncilAsciiStyleRun> NormalizeAsciiStyleRuns(
        IEnumerable<CouncilAsciiStyleRun>? runs,
        int frameWidth,
        int frameHeight,
        CouncilAsciiColorMode sessionMode)
    {
        try
        {
            var result = new List<CouncilAsciiStyleRun>();
            if (runs is null || frameWidth <= 0 || frameHeight <= 0)
                return result;
            foreach (var run in runs)
            {
                if (result.Count >= MaximumAsciiStyleRuns || run is null || run.Length <= 0 || run.Y < 0 || run.Y >= frameHeight)
                    continue;
                var start = Math.Max(0, run.X);
                var end = Math.Min(frameWidth, (int)Math.Clamp((long)run.X + run.Length, int.MinValue, int.MaxValue));
                if (end <= start)
                    continue;
                var style = NormalizeAsciiStyle(run.Style, sessionMode);
                if (style is null)
                    continue;
                result.Add(new CouncilAsciiStyleRun { Y = run.Y, X = start, Length = end - start, Style = style });
            }
            return result;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Normalizing bounded ASCII style runs failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs clone ascii style runs as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runs">Council ascii style run dependency used by the council game session workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<CouncilAsciiStyleRun> CloneAsciiStyleRuns(IEnumerable<CouncilAsciiStyleRun>? runs)
    {
        try
        {
            return (runs ?? [])
                .Select(run => new CouncilAsciiStyleRun
                {
                    Y = run.Y,
                    X = run.X,
                    Length = run.Length,
                    Style = new CouncilAsciiTextStyle
                    {
                        ColorMode = run.Style.ColorMode,
                        ForegroundColor = run.Style.ForegroundColor,
                        BackgroundColor = run.Style.BackgroundColor,
                        Bold = run.Style.Bold,
                        Dim = run.Style.Dim,
                        Invert = run.Style.Invert
                    }
                })
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Cloning ASCII style runs failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs crop ascii style runs as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runs">Council ascii style run dependency used by the council game session workflow to provide the corresponding application capability.</param>
    /// <param name="x">X value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="width">Width value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="height">Height value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<CouncilAsciiStyleRun> CropAsciiStyleRuns(
        IEnumerable<CouncilAsciiStyleRun>? runs,
        int x,
        int y,
        int width,
        int height)
    {
        try
        {
            var result = new List<CouncilAsciiStyleRun>();
            var right = x + width;
            var bottom = y + height;
            foreach (var run in runs ?? [])
            {
                if (run.Y < y || run.Y >= bottom)
                    continue;
                var start = Math.Max(run.X, x);
                var end = Math.Min(run.X + run.Length, right);
                if (end <= start)
                    continue;
                result.Add(new CouncilAsciiStyleRun
                {
                    Y = run.Y - y,
                    X = start - x,
                    Length = end - start,
                    Style = NormalizeAsciiStyle(run.Style, CouncilAsciiColorMode.Indexed256) ?? new()
                });
                if (result.Count >= MaximumAsciiStyleRuns)
                    break;
            }
            return result;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Cropping ASCII style runs failed.");
            throw;
        }
    }

    /// <summary>
    /// Applies ascii style range as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="x">X value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="length">Length value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="style">Style value supplied to the council game session operation and used when producing its result.</param>
    private void ApplyAsciiStyleRange(
        CouncilGameSessionState session,
        int y,
        int x,
        int length,
        CouncilAsciiTextStyle? style)
    {
        try
        {
            if (length <= 0 || y < 0 || y >= session.FrameHeight)
                return;
            var start = Math.Max(0, x);
            var end = Math.Min(session.FrameWidth, (int)Math.Clamp((long)x + length, int.MinValue, int.MaxValue));
            if (end <= start)
                return;

            var preserved = new List<CouncilAsciiStyleRun>();
            foreach (var run in session.FrameStyleRuns)
            {
                if (run.Y != y || run.X >= end || run.X + run.Length <= start)
                {
                    preserved.Add(run);
                    continue;
                }
                if (run.X < start)
                    preserved.Add(new CouncilAsciiStyleRun { Y = run.Y, X = run.X, Length = start - run.X, Style = run.Style });
                var runEnd = run.X + run.Length;
                if (runEnd > end)
                    preserved.Add(new CouncilAsciiStyleRun { Y = run.Y, X = end, Length = runEnd - end, Style = run.Style });
            }
            var normalizedStyle = NormalizeAsciiStyle(style, session.AsciiColorMode);
            if (normalizedStyle is not null)
                preserved.Add(new CouncilAsciiStyleRun { Y = y, X = start, Length = end - start, Style = normalizedStyle });
            session.FrameStyleRuns = NormalizeAsciiStyleRuns(preserved, session.FrameWidth, session.FrameHeight, session.AsciiColorMode);
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Applying ASCII style metadata to a display range failed.");
            throw;
        }
    }

    /// <summary>
    /// Resolves frame style runs as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="frame">Frame value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<CouncilAsciiStyleRun> ResolveFrameStyleRuns(CouncilGameSessionState session, string frame)
    {
        try
        {
            if (session.FrameStyleRuns.Count > 0)
                return CloneAsciiStyleRuns(session.FrameStyleRuns);
            return BuildSemanticAsciiStyleRuns(session, frame);
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Resolving ASCII frame presentation failed.");
            throw;
        }
    }

    /// <summary>
    /// Builds the semantic presentation of a frame from the <c>style</c> rule rows of the session's ruleset, so colour
    /// cues (titles, HUD tokens, glyph classes) are game content rather than renderer code.
    /// </summary>
    /// <param name="session">Session whose ruleset and frame size apply.</param>
    /// <param name="frame">Frame text to style.</param>
    /// <returns>The bounded style runs of the frame.</returns>
    private List<CouncilAsciiStyleRun> BuildSemanticAsciiStyleRuns(CouncilGameSessionState session, string frame)
    {
        try
        {
            var lines = NormalizeFrame(frame, session.FrameWidth, session.FrameHeight)
                .Split(Environment.NewLine, StringSplitOptions.None)
                .Take(session.FrameHeight)
                .ToList();
            var runs = engine.BuildStyleRuns(session.Ruleset, lines);
            return NormalizeAsciiStyleRuns(runs, session.FrameWidth, session.FrameHeight, CouncilAsciiColorMode.TerminalDefault);
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Building semantic ASCII style runs failed for {GameKey}.", session.GameKey);
            throw;
        }
    }
}
