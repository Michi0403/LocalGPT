using System.Text.Json;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;

namespace LocalGPT.Services.Council.Skills;

/// <summary>
/// Accepts optional model self-reports as untrusted evidence. Self-reported skills are persisted disabled for
/// automatic authority decisions until the current user approves them in the organic-skill registry.
/// </summary>
/// <param name="skillRegistry">Organic skill registry service dependency used by the model capability self assessment workflow to provide the corresponding application capability.</param>
/// <param name="runtimePolicy">Local gpt runtime policy data service dependency used by the model capability self assessment workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ModelCapabilitySelfAssessmentService(
    IOrganicSkillRegistryService skillRegistry,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<ModelCapabilitySelfAssessmentService> logger) : IModelCapabilitySelfAssessmentService
{
    /// <summary>
    /// Performs capture and strip as part of the model capability self assessment service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="modelName">Model name value supplied to the model capability self assessment operation and used when producing its result.</param>
    /// <param name="visibleContent">Visible content value supplied to the model capability self assessment operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public async Task<string> CaptureAndStripAsync(string modelName, string visibleContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(visibleContent))
            return visibleContent;

        var matches = runtimePolicy.GetPattern(LocalGptRuntimePattern.ModelCapabilitySelfAssessment).Matches(visibleContent);
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

        return runtimePolicy.GetPattern(LocalGptRuntimePattern.ModelCapabilitySelfAssessment).Replace(visibleContent, string.Empty).Trim();
    }
}
