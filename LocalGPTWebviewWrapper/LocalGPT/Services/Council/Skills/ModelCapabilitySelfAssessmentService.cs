using System.Text.Json;
using System.Text.RegularExpressions;
using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;

namespace LocalGPT.Services.Council.Skills;

/// <summary>
/// Accepts optional model self-reports as untrusted evidence. Self-reported skills are persisted disabled for
/// automatic authority decisions until the current user approves them in the organic-skill registry.
/// </summary>
public sealed partial class ModelCapabilitySelfAssessmentService(
    IOrganicSkillRegistryService skillRegistry,
    ILogger<ModelCapabilitySelfAssessmentService> logger) : IModelCapabilitySelfAssessmentService
{
    [GeneratedRegex("<localgpt-self-assessment>(?<json>[\\s\\S]*?)</localgpt-self-assessment>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AssessmentRegex();

    public async Task<string> CaptureAndStripAsync(string modelName, string visibleContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(visibleContent))
            return visibleContent;

        var matches = AssessmentRegex().Matches(visibleContent);
        foreach (Match match in matches)
        {
            try
            {
                var assessment = JsonSerializer.Deserialize<LocalGPT.WireProtocol.OneWireModelSelfAssessment>(match.Groups["json"].Value, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (assessment is null)
                    continue;
                assessment.ModelName = string.IsNullOrWhiteSpace(assessment.ModelName) ? modelName : assessment.ModelName.Trim();
                assessment.MemberKey = string.IsNullOrWhiteSpace(assessment.MemberKey) ? modelName : assessment.MemberKey.Trim();
                await skillRegistry.RecordUntrustedSelfAssessmentAsync(assessment, cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                logger.LogDebug(ex, "Ignored malformed capability self-assessment from {ModelName}.", modelName);
            }
        }

        return AssessmentRegex().Replace(visibleContent, string.Empty).Trim();
    }
}
