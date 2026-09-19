using System.Text;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services
{
    /// <summary>Provides fixed-cell read/write display operations shared by AI functions, controllers and the /Chat console.</summary>
    public sealed partial class CouncilGameSessionService
    {
        /// <summary>Returns the bidirectional display functions available to Council renderers for this session surface.</summary>
        private IReadOnlyList<string> GetSupportedDisplayMethods()
        {
            try
            {
                return
                [
                    "localgpt.game.display.get",
                    "localgpt.game.display.palette.get",
                    "localgpt.game.display.text.write",
                    "localgpt.game.display.cell.set",
                    "localgpt.game.display.region.fill",
                    "localgpt.game.display.region.blit",
                    "localgpt.game.frame.submit",
                    "localgpt.game.animation.submit"
                ];
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Building the Council ASCII display-function list failed.");
                throw;
            }
        }

        /// <inheritdoc />
        public Task<CouncilGameDisplaySnapshot> GetDisplayAsync(ReadCouncilGameDisplayRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                ThrowIfDisposed();
                ArgumentNullException.ThrowIfNull(request);
                cancellationToken.ThrowIfCancellationRequested();
                if (!sessions.TryGetValue(request.SessionId, out var session))
                    throw new KeyNotFoundException($"Council game session {request.SessionId} was not found.");

                lock (session.SyncRoot)
                {
                    var cells = ToCells(session.FrameText, session.FrameWidth, session.FrameHeight);
                    var x = Math.Clamp(request.X, 0, Math.Max(0, session.FrameWidth - 1));
                    var y = Math.Clamp(request.Y, 0, Math.Max(0, session.FrameHeight - 1));
                    var width = request.Width <= 0 ? session.FrameWidth - x : Math.Min(request.Width, session.FrameWidth - x);
                    var height = request.Height <= 0 ? session.FrameHeight - y : Math.Min(request.Height, session.FrameHeight - y);
                    width = Math.Max(1, width);
                    height = Math.Max(1, height);
                    var lines = new string[height];
                    for (var row = 0; row < height; row++)
                        lines[row] = new string(cells[y + row], x, width);
                    var activeStyleRuns = ResolveFrameStyleRuns(session, session.FrameText);
                    return Task.FromResult(new CouncilGameDisplaySnapshot
                    {
                        SessionId = session.Id,
                        Turn = session.Turn,
                        FrameWidth = session.FrameWidth,
                        FrameHeight = session.FrameHeight,
                        X = x,
                        Y = y,
                        Width = width,
                        Height = height,
                        Text = string.Join(Environment.NewLine, lines),
                        AsciiColorMode = session.AsciiColorMode,
                        DefaultForegroundColor = session.DefaultForegroundColor,
                        DefaultBackgroundColor = session.DefaultBackgroundColor,
                        StyleRuns = CropAsciiStyleRuns(activeStyleRuns, x, y, width, height),
                        RendererName = session.FrameRenderer,
                        SupportedMethods = GetSupportedDisplayMethods()
                    });
                }
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Reading a Council ASCII display surface failed for session {GameSessionId}.", request?.SessionId);
                throw;
            }
        }

        /// <inheritdoc />
        public Task<CouncilGameSessionSnapshot> WriteTextAsync(WriteCouncilGameTextRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                return MutateDisplayAsync(request.SessionId, request.Turn, request.RendererName, (session, cells) =>
                {
                    var lines = NormalizeInputLines(request.Text);
                    for (var row = 0; row < lines.Length; row++)
                    {
                        WriteLine(cells, request.X, request.Y + row, lines[row]);
                        ApplyAsciiStyleRange(session, request.Y + row, request.X, lines[row].Length, request.Style);
                    }
                }, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Writing text to a Council ASCII display surface failed for session {GameSessionId}.", request?.SessionId);
                throw;
            }
        }

        /// <inheritdoc />
        public Task<CouncilGameSessionSnapshot> SetCellAsync(SetCouncilGameCellRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                return MutateDisplayAsync(request.SessionId, request.Turn, request.RendererName, (session, cells) =>
                {
                    if (request.Y >= 0 && request.Y < cells.Length && request.X >= 0 && request.X < cells[request.Y].Length)
                    {
                        cells[request.Y][request.X] = FirstGlyph(request.Glyph);
                        ApplyAsciiStyleRange(session, request.Y, request.X, 1, request.Style);
                    }
                }, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Writing one cell to a Council ASCII display surface failed for session {GameSessionId}.", request?.SessionId);
                throw;
            }
        }

        /// <inheritdoc />
        public Task<CouncilGameSessionSnapshot> FillRegionAsync(FillCouncilGameRegionRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                return MutateDisplayAsync(request.SessionId, request.Turn, request.RendererName, (session, cells) =>
                {
                    if (request.Width <= 0 || request.Height <= 0) return;
                    var glyph = FirstGlyph(request.Glyph);
                    var y0 = Math.Max(0, request.Y);
                    var y1 = Math.Min(cells.Length, SafeAdd(request.Y, request.Height));
                    for (var y = y0; y < y1; y++)
                    {
                        var x0 = Math.Max(0, request.X);
                        var x1 = Math.Min(cells[y].Length, SafeAdd(request.X, request.Width));
                        for (var x = x0; x < x1; x++) cells[y][x] = glyph;
                        ApplyAsciiStyleRange(session, y, x0, x1 - x0, request.Style);
                    }
                }, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Filling a Council ASCII display region failed for session {GameSessionId}.", request?.SessionId);
                throw;
            }
        }

        /// <inheritdoc />
        public Task<CouncilGameSessionSnapshot> BlitRegionAsync(BlitCouncilGameRegionRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                return MutateDisplayAsync(request.SessionId, request.Turn, request.RendererName, (session, cells) =>
                {
                    var lines = NormalizeInputLines(request.Text);
                    for (var row = 0; row < lines.Length; row++)
                    {
                        WriteLine(cells, request.X, request.Y + row, lines[row]);
                        ApplyAsciiStyleRange(session, request.Y + row, request.X, lines[row].Length, request.Style);
                    }
                }, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Blitting text into a Council ASCII display region failed for session {GameSessionId}.", request?.SessionId);
                throw;
            }
        }

        /// <inheritdoc />
        public Task<CouncilGameSessionSnapshot> SubmitAnimationAsync(SubmitCouncilGameAnimationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                ThrowIfDisposed();
                ArgumentNullException.ThrowIfNull(request);
                cancellationToken.ThrowIfCancellationRequested();
                if (request.Frames is null || request.Frames.Count is < 2 or > 12)
                    throw new ArgumentOutOfRangeException(nameof(request), "ASCII animations require two through twelve pregenerated frames.");
                if (!sessions.TryGetValue(request.SessionId, out var session))
                    throw new KeyNotFoundException($"Council game session {request.SessionId} was not found.");

                lock (session.SyncRoot)
                {
                    ClaimRenderer(session, request.Turn, request.RendererName);
                    if (request.AsciiColorMode is CouncilAsciiColorMode requestedMode)
                        session.AsciiColorMode = NormalizeAsciiColorMode(requestedMode);
                    if (request.DefaultForegroundColor is int requestedForeground)
                        session.DefaultForegroundColor = NormalizeAsciiColorIndex(session.AsciiColorMode, requestedForeground, true);
                    if (request.DefaultBackgroundColor is int requestedBackground)
                        session.DefaultBackgroundColor = NormalizeAsciiColorIndex(session.AsciiColorMode, requestedBackground, false);
                    session.AnimationFrames = request.Frames.Select(frame => NormalizeFrame(frame, session.FrameWidth, session.FrameHeight)).ToList();
                    session.AnimationFrameStyleRuns = session.AnimationFrames.Select((_, index) =>
                        index < request.FrameStyleRuns.Count
                            ? NormalizeAsciiStyleRuns(request.FrameStyleRuns[index], session.FrameWidth, session.FrameHeight, session.AsciiColorMode)
                            : new List<CouncilAsciiStyleRun>()).ToList();
                    session.AnimationDelayMilliseconds = Math.Clamp(request.DelayMilliseconds, 250, 5000);
                    session.AnimationSubtitle = string.IsNullOrWhiteSpace(request.Subtitle) ? string.Empty : request.Subtitle.Trim();
                    session.AnimationSubtitleStyle = NormalizeAsciiStyle(request.SubtitleStyle, session.AsciiColorMode);
                    if (session.AnimationSubtitle.Length > 360)
                        session.AnimationSubtitle = session.AnimationSubtitle[..360];
                    session.AnimationSubtitleHoldMilliseconds = Math.Clamp(request.SubtitleHoldMilliseconds, 500, 10000);
                    session.FrameText = session.AnimationFrames[0];
                    session.FrameStyleRuns = session.AnimationFrameStyleRuns.Count > 0
                        ? CloneAsciiStyleRuns(session.AnimationFrameStyleRuns[0])
                        : [];
                    session.FrameCaption = string.IsNullOrWhiteSpace(request.Caption) ? BuildCaption(session) : request.Caption.Trim();
                    session.FrameRenderer = request.RendererName.Trim();
                    session.UpdatedAtUtc = DateTime.UtcNow;
                }
                Notify(session.Id);
                logger.LogInformation("Accepted {FrameCount} pregenerated ASCII animation frames for session {GameSessionId}, turn {Turn}; frame content was omitted.", request.Frames.Count, session.Id, request.Turn);
                return Task.FromResult(ToSnapshot(session));
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Submitting a pregenerated Council ASCII animation failed for session {GameSessionId}.", request?.SessionId);
                throw;
            }
        }

        private Task<CouncilGameSessionSnapshot> MutateDisplayAsync(Guid sessionId, long turn, string rendererName, Action<CouncilGameSessionState, char[][]> mutation, CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfDisposed();
                cancellationToken.ThrowIfCancellationRequested();
                ArgumentNullException.ThrowIfNull(mutation);
                if (!sessions.TryGetValue(sessionId, out var session))
                    throw new KeyNotFoundException($"Council game session {sessionId} was not found.");
                lock (session.SyncRoot)
                {
                    ClaimRenderer(session, turn, rendererName);
                    var cells = ToCells(session.FrameText, session.FrameWidth, session.FrameHeight);
                    mutation(session, cells);
                    session.FrameText = string.Join(Environment.NewLine, cells.Select(row => new string(row)));
                    session.FrameRenderer = rendererName.Trim();
                    session.AnimationFrames.Clear();
                    session.AnimationFrameStyleRuns.Clear();
                    session.AnimationSubtitleStyle = null;
                    session.AnimationDelayMilliseconds = 650;
                    session.UpdatedAtUtc = DateTime.UtcNow;
                }
                Notify(session.Id);
                return Task.FromResult(ToSnapshot(session));
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Mutating a Council ASCII display surface failed for session {GameSessionId}, turn {Turn}.", sessionId, turn);
                throw;
            }
        }

        private void ClaimRenderer(CouncilGameSessionState session, long turn, string rendererName)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(session);
                if (turn != session.Turn)
                    throw new InvalidOperationException($"Display turn {turn} does not match authoritative turn {session.Turn}.");
                if (string.IsNullOrWhiteSpace(rendererName))
                    throw new ArgumentException("RendererName is required so one Council member owns display mutations for the turn.", nameof(rendererName));
                if (session.FrameOwnerTurn == turn
                    && !string.IsNullOrWhiteSpace(session.FrameOwner)
                    && !session.FrameOwner.Equals(rendererName, StringComparison.OrdinalIgnoreCase)
                    && !session.FrameOwner.Equals("LocalGPT deterministic preview renderer", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Turn {turn} already has one frame owner: {session.FrameOwner}.");
                session.FrameOwnerTurn = turn;
                session.FrameOwner = rendererName.Trim();
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Claiming Council ASCII renderer ownership failed for turn {Turn}.", turn);
                throw;
            }
        }

        private char[][] ToCells(string frame, int width, int height)
        {
            try
            {
                return NormalizeFrame(frame, width, height)
                    .Split(Environment.NewLine, StringSplitOptions.None)
                    .Select(line => line.ToCharArray())
                    .ToArray();
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Converting a Council ASCII frame into terminal cells failed for {Width}x{Height}.", width, height);
                throw;
            }
        }

        private string[] NormalizeInputLines(string? text)
        {
            try
            {
                return (text ?? string.Empty)
                    .Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Replace("\r", "\n", StringComparison.Ordinal)
                    .Split('\n');
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Normalizing Council ASCII text lines failed.");
                throw;
            }
        }

        private void WriteLine(char[][] cells, int x, int y, string text)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(cells);
                if (y < 0 || y >= cells.Length || string.IsNullOrEmpty(text)) return;
                var sourceStart = x < 0 ? (int)Math.Min(text.Length, -(long)x) : 0;
                var destination = Math.Max(0, x);
                for (var index = sourceStart; index < text.Length && destination + index - sourceStart < cells[y].Length; index++)
                    cells[y][destination + index - sourceStart] = text[index];
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Writing a clipped line into Council ASCII terminal cells failed at {X},{Y}.", x, y);
                throw;
            }
        }

        private char FirstGlyph(string? value)
        {
            try
            {
                return string.IsNullOrEmpty(value) ? ' ' : value[0];
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Resolving a Council ASCII glyph failed.");
                throw;
            }
        }

        private int SafeAdd(int left, int right)
        {
            try
            {
                return (int)Math.Clamp((long)left + right, int.MinValue, int.MaxValue);
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Calculating clipped Council ASCII display coordinates failed.");
                throw;
            }
        }
    }
}
