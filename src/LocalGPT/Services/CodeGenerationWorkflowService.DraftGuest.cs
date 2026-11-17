using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Validates the isolated file-transfer protocol without granting the guest access to host paths.</summary>
public sealed partial class CodeGenerationWorkflowService
{
    /// <summary>Decodes exact UTF-8 source bytes from one completed guest's bounded stdout envelope.</summary>
    /// <param name="assignment">Server-reserved file assignment, including canonical path.</param>
    /// <param name="guest">Actual completed isolated execution receipt.</param>
    /// <returns>Candidate source when the envelope matches, otherwise null.</returns>
    private string? ExtractGuestFileSource(CodeGenerationDraftAssignment assignment, SessionSandboxRunSnapshot guest)
    {
        try
        {
            if (guest.Status != "Completed" || guest.ExitCode != 0 || guest.OutputTruncated || !guest.CompletedUtc.HasValue)
                return null;
            using var document = JsonDocument.Parse(guest.StandardOutput);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("assignmentId", out var id) ||
                id.ValueKind != JsonValueKind.String || !id.TryGetGuid(out var actualId) || actualId != assignment.Id ||
                !root.TryGetProperty("relativePath", out var path) || path.ValueKind != JsonValueKind.String ||
                !string.Equals(path.GetString(), assignment.RelativePath, StringComparison.Ordinal) ||
                !root.TryGetProperty("contentBase64", out var content) || content.ValueKind != JsonValueKind.String)
                return null;
            var bytes = Convert.FromBase64String(content.GetString() ?? string.Empty);
            var source = new UTF8Encoding(false, true).GetString(bytes);
            return string.IsNullOrWhiteSpace(source) || source.Contains('\0') ? null : source;
        }
        catch (Exception exception) when (exception is JsonException or FormatException or DecoderFallbackException or InvalidOperationException)
        {
            logger.LogInformation(exception, "Guest {GuestRunId} returned an invalid file-transfer envelope for {AssignmentId}.", guest.RunId, assignment.Id);
            return null;
        }
    }
}
