using LocalGPT.Interfaces;
using System.Text;

namespace LocalGPT.Services;

/// <summary>
/// Extracts the distinct compiler error lines (MSBuild/Roslyn/NuGet canonical <c>: error CODE:</c> format) from build output,
/// bounded by the runtime values <c>BuildDiagnosticMaximumLines</c> and <c>BuildDiagnosticMaximumCharacters</c>.
/// </summary>
/// <param name="runtimePolicy">Database runtime policy that bounds the returned diagnostics.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class BuildDiagnosticSummaryService(
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<BuildDiagnosticSummaryService> logger) : IBuildDiagnosticSummaryService
{
    /// <inheritdoc />
    public string Summarize(string standardOutput, string standardError)
    {
        try
        {
            var maximumLines = Math.Max(1, runtimePolicy.GetInt("BuildDiagnosticMaximumLines"));
            var maximumCharacters = Math.Max(200, runtimePolicy.GetInt("BuildDiagnosticMaximumCharacters"));
            var lines = SplitLines(standardError).Concat(SplitLines(standardOutput)).ToList();
            if (lines.Count == 0)
                return string.Empty;

            // MSBuild repeats every error in its closing summary; keep each distinct line once, in first-seen order.
            var errors = lines
                .Where(IsErrorLine)
                .Distinct(StringComparer.Ordinal)
                .Take(maximumLines)
                .ToList();
            var selected = errors.Count > 0 ? errors : lines.TakeLast(maximumLines).ToList();
            var builder = new StringBuilder();
            foreach (var line in selected)
            {
                if (builder.Length + line.Length + 1 > maximumCharacters)
                    break;
                builder.AppendLine(line);
            }

            logger.LogDebug(
                "Summarized build output into {SelectedLines} line(s) ({Kind}) from {TotalLines} captured line(s).",
                selected.Count, errors.Count > 0 ? "error lines" : "output tail", lines.Count);
            return builder.ToString().TrimEnd();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Summarizing build diagnostics failed.");
            throw;
        }
    }

    /// <summary>Splits captured process output into trimmed, non-empty lines.</summary>
    /// <param name="text">Captured output; may be empty.</param>
    /// <returns>The non-empty lines.</returns>
    private IEnumerable<string> SplitLines(string text)
    {
        try
        {
            return string.IsNullOrWhiteSpace(text)
                ? []
                : text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Splitting build output into lines failed.");
            throw;
        }
    }

    /// <summary>Recognizes a canonical compiler error line such as <c>Program.cs(4,9): error CS0103: ...</c> or <c>error MSB1009: ...</c>.</summary>
    /// <param name="line">One trimmed output line.</param>
    /// <returns><see langword="true"/> for an error line.</returns>
    private bool IsErrorLine(string line)
    {
        try
        {
            return line.Contains(": error ", StringComparison.OrdinalIgnoreCase) ||
                   line.StartsWith("error ", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Classifying a build output line failed.");
            throw;
        }
    }
}
