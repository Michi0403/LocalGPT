using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Owns presentation-only color normalization and semantic styling for the shared ASCII surface.</summary>
public sealed partial class CouncilGameSessionService
{
    private const int MaximumAsciiStyleRuns = 4096;

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

    private List<CouncilAsciiStyleRun> ResolveFrameStyleRuns(CouncilGameSessionState session, string frame)
    {
        try
        {
            if (session.FrameStyleRuns.Count > 0)
                return CloneAsciiStyleRuns(session.FrameStyleRuns);
            return BuildSemanticAsciiStyleRuns(session.GameKey, session.RuntimeProfile, frame, session.FrameWidth, session.FrameHeight);
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Resolving ASCII frame presentation failed.");
            throw;
        }
    }

    private List<CouncilAsciiStyleRun> BuildSemanticAsciiStyleRuns(
        string gameKey,
        CouncilGameRuntimeProfile runtimeProfile,
        string frame,
        int frameWidth,
        int frameHeight)
    {
        try
        {
            var normalized = NormalizeFrame(frame, frameWidth, frameHeight);
            var lines = normalized.Split(Environment.NewLine, StringSplitOptions.None);
            var runs = new List<CouncilAsciiStyleRun>();
            var borderStyle = new CouncilAsciiTextStyle { ColorMode = CouncilAsciiColorMode.Indexed256, ForegroundColor = 22, Dim = true };
            var titleStyle = new CouncilAsciiTextStyle { ColorMode = CouncilAsciiColorMode.Indexed256, ForegroundColor = 51, Bold = true };
            var dangerStyle = new CouncilAsciiTextStyle { ColorMode = CouncilAsciiColorMode.Indexed256, ForegroundColor = 196, Bold = true };
            var warningStyle = new CouncilAsciiTextStyle { ColorMode = CouncilAsciiColorMode.Indexed256, ForegroundColor = 226, Bold = true };
            var goodStyle = new CouncilAsciiTextStyle { ColorMode = CouncilAsciiColorMode.Indexed256, ForegroundColor = 46, Bold = true };
            var accentStyle = new CouncilAsciiTextStyle { ColorMode = CouncilAsciiColorMode.Indexed256, ForegroundColor = 45 };
            var purpleStyle = new CouncilAsciiTextStyle { ColorMode = CouncilAsciiColorMode.Indexed256, ForegroundColor = 201, Bold = true };

            for (var row = 0; row < Math.Min(lines.Length, frameHeight); row++)
            {
                var line = lines[row];
                AddCharacterStyleRuns(runs, line, row, "┌┐└┘─│╭╮╰╯═║#+", borderStyle);
                if (row == 0 || line.Contains("DOOM", StringComparison.OrdinalIgnoreCase) || line.Contains("GREEN DRAGON", StringComparison.OrdinalIgnoreCase) || line.Contains("KERNEL CREATURE", StringComparison.OrdinalIgnoreCase))
                    AddNonBlankLineRun(runs, line, row, titleStyle);
                AddTokenStyleRun(runs, line, row, "HP", goodStyle);
                AddTokenStyleRun(runs, line, row, "HEALTH", goodStyle);
                AddTokenStyleRun(runs, line, row, "AMMO", warningStyle);
                AddTokenStyleRun(runs, line, row, "EXIT", accentStyle);
                AddTokenStyleRun(runs, line, row, "EXTRACTION", accentStyle);
                AddTokenStyleRun(runs, line, row, "ENEMY", dangerStyle);
                AddTokenStyleRun(runs, line, row, "MONSTER", dangerStyle);
                AddTokenStyleRun(runs, line, row, "CHAMPION", warningStyle);
                AddTokenStyleRun(runs, line, row, "ROUND", purpleStyle);
                AddTokenStyleRun(runs, line, row, "VS", dangerStyle);
                AddTokenStyleRun(runs, line, row, "[1]", accentStyle);
                AddTokenStyleRun(runs, line, row, "[2]", accentStyle);
                AddTokenStyleRun(runs, line, row, "[3]", accentStyle);
                if (runtimeProfile == CouncilGameRuntimeProfile.Corridor)
                {
                    AddCharacterStyleRuns(runs, line, row, "MZE", dangerStyle);
                    AddCharacterStyleRuns(runs, line, row, "@", warningStyle);
                }
                if (string.Equals(gameKey, "green-dragon", StringComparison.OrdinalIgnoreCase))
                    AddTokenStyleRun(runs, line, row, "DRAGON", new CouncilAsciiTextStyle { ColorMode = CouncilAsciiColorMode.Indexed256, ForegroundColor = 40, Bold = true });
            }
            return NormalizeAsciiStyleRuns(runs, frameWidth, frameHeight, CouncilAsciiColorMode.TerminalDefault);
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Building semantic ASCII style runs failed for {GameKey}.", gameKey);
            throw;
        }
    }

    private void AddTokenStyleRun(List<CouncilAsciiStyleRun> runs, string line, int row, string token, CouncilAsciiTextStyle style)
    {
        try
        {
            var search = 0;
            while (search < line.Length && runs.Count < MaximumAsciiStyleRuns)
            {
                var index = line.IndexOf(token, search, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    break;
                runs.Add(new CouncilAsciiStyleRun { Y = row, X = index, Length = token.Length, Style = style });
                search = index + Math.Max(1, token.Length);
            }
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Styling an ASCII semantic token failed.");
            throw;
        }
    }

    private void AddCharacterStyleRuns(List<CouncilAsciiStyleRun> runs, string line, int row, string characters, CouncilAsciiTextStyle style)
    {
        try
        {
            var start = -1;
            for (var index = 0; index <= line.Length; index++)
            {
                var matches = index < line.Length && characters.IndexOf(line[index]) >= 0;
                if (matches && start < 0)
                    start = index;
                if ((!matches || index == line.Length) && start >= 0)
                {
                    runs.Add(new CouncilAsciiStyleRun { Y = row, X = start, Length = index - start, Style = style });
                    start = -1;
                    if (runs.Count >= MaximumAsciiStyleRuns)
                        return;
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Styling ASCII semantic glyphs failed.");
            throw;
        }
    }

    private void AddNonBlankLineRun(List<CouncilAsciiStyleRun> runs, string line, int row, CouncilAsciiTextStyle style)
    {
        try
        {
            var first = 0;
            while (first < line.Length && char.IsWhiteSpace(line[first])) first++;
            var last = line.Length - 1;
            while (last >= first && char.IsWhiteSpace(line[last])) last--;
            if (last >= first)
                runs.Add(new CouncilAsciiStyleRun { Y = row, X = first, Length = last - first + 1, Style = style });
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Styling an ASCII line failed.");
            throw;
        }
    }
}
