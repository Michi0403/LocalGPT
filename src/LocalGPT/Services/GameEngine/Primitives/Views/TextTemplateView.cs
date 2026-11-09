using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Views;

/// <summary>
/// View <c>text-template</c>: renders the multi-line ruleset text <c>templateKey</c> with session tokens inside a framed page.
/// Missing rows are padded with the declared side border before the bottom border.
/// </summary>
/// <param name="parameters">Reads the view parameters.</param>
/// <param name="tokens">Builds session tokens.</param>
/// <param name="texts">Reads and formats the template.</param>
/// <param name="textUtility">Fits rows to the frame width.</param>
/// <param name="formatter">Formats template rows.</param>
/// <param name="logger">Receives view diagnostics.</param>
public sealed class TextTemplateView(
    IGameParameterReader parameters,
    IGameTokenService tokens,
    IGameTextService texts,
    IGameTextUtilityService textUtility,
    IGameTemplateFormatter formatter,
    ILogger<TextTemplateView> logger) : IGameViewPrimitive
{
    /// <inheritdoc />
    public string Key => "text-template";

    /// <inheritdoc />
    public GameViewFrame Render(GameEngineContext context)
    {
        try
        {
            var session = context.Session;
            var viewParameters = parameters.Parse(context.Ruleset.ViewParametersJson);
            var width = session.FrameWidth;
            var values = tokens.Build(session);
            var top = parameters.ReadString(viewParameters, "topBorder", "┌─┐");
            var bottom = parameters.ReadString(viewParameters, "bottomBorder", "└─┘");
            var padding = parameters.ReadString(viewParameters, "paddingLine", "│");
            var lines = new List<string> { Border(top, width) };
            var template = texts.Get(context.Ruleset, parameters.ReadString(viewParameters, "templateKey", "view.template"))
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n');
            foreach (var line in template)
                lines.Add(textUtility.Fit(FormatLine(line, values), width));
            lines.Add(Border(bottom, width));
            while (lines.Count < session.FrameHeight)
                lines.Insert(lines.Count - 1, textUtility.Fit(padding, width));
            return new GameViewFrame { Text = string.Join(Environment.NewLine, lines.Take(session.FrameHeight)) };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Rendering the text template view of session {SessionId} failed.", context.Session.Id);
            throw;
        }
    }

    /// <summary>
    /// Builds a border row from a three-character pattern: start, repeated middle, end.
    /// </summary>
    /// <param name="pattern">The border pattern.</param>
    /// <param name="width">The frame width.</param>
    /// <returns>The border row.</returns>
    private string Border(string pattern, int width)
    {
        try
        {
            if (pattern.Length < 3)
                return textUtility.Fit(pattern, width);
            return pattern[0] + new string(pattern[1], Math.Max(0, width - 2)) + pattern[2];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building a text template border failed.");
            throw;
        }
    }

    /// <summary>
    /// Formats one template row.
    /// </summary>
    /// <param name="line">The template row.</param>
    /// <param name="values">The session tokens.</param>
    /// <returns>The formatted row.</returns>
    private string FormatLine(string line, IReadOnlyDictionary<string, object?> values)
    {
        try
        {
            return formatter.Format(line, values);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting a text template row failed.");
            throw;
        }
    }
}
