namespace LocalGPT.Interfaces;

/// <summary>
/// Turns raw compiler output into the bounded list of error lines a Council role needs to repair a failed build, so build
/// results carry the actual diagnostics instead of only a status.
/// </summary>
public interface IBuildDiagnosticSummaryService
{
    /// <summary>Extracts the distinct compiler error lines of one build, bounded by the database runtime policy.</summary>
    /// <param name="standardOutput">Captured standard output of the build.</param>
    /// <param name="standardError">Captured standard error of the build.</param>
    /// <returns>The error lines, or the tail of the output when no error line is recognized; empty when there is no output.</returns>
    string Summarize(string standardOutput, string standardError);
}
