using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Invokes exactly one configured provider model as a tool-free recovery curator.</summary>
/// <param name="models">Provider-qualified local AI model registry.</param>
/// <param name="policy">Database-owned curator configuration and instructions.</param>
/// <param name="functions">Authoritative registered DXAiFunction parameter contracts.</param>
/// <param name="logger">Redacted service diagnostics.</param>
public sealed class AiHelpCuratorService(
    IProviderModelRuntimeService models,
    ILocalGptRuntimePolicyDataService policy,
    IDxAiFunctionRegistry functions,
    ILogger<AiHelpCuratorService> logger) : IAiHelpCuratorService
{
    /// <summary>Propose one different action, without allowing the model to execute a tool or grant authority.</summary>
    /// <param name="report">Untrusted bounded attempt report.</param>
    /// <param name="failures">Number of failed attempts.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Bounded curator advice; lack of a model produces cannot_resolve.</returns>
    public async Task<AiHelpCuratorAdvice> DiagnoseAsync(AiHelpAttemptReport report, int failures, CancellationToken cancellationToken = default)
    {
        try
        {
            var configuredKey = policy.GetString("HelpEscalatorCuratorModelKey")?.Trim();
            var candidates = await models.GetCandidatesAsync(cancellationToken).ConfigureAwait(false);
            var selected = candidates.FirstOrDefault(model => model.IsConfigured && (model.IsInstalled || !model.IsLocal) &&
                !string.Equals(model.SelectionKey, report.FailedModelSelectionKey, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(configuredKey) &&
                (string.Equals(model.SelectionKey, configuredKey, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(model.ModelName, configuredKey, StringComparison.OrdinalIgnoreCase)));
            // An empty choice may inherit an available configured participant; an explicit invalid
            // model never silently selects a different provider or host.
            if (selected is null && string.IsNullOrWhiteSpace(configuredKey))
                selected = candidates.FirstOrDefault(model => model.IsConfigured && (model.IsInstalled || !model.IsLocal) &&
                    !string.Equals(model.SelectionKey, report.FailedModelSelectionKey, StringComparison.OrdinalIgnoreCase));
            if (selected is null)
                return new AiHelpCuratorAdvice { Type = "cannot_resolve", Reason = "No configured reachable curator model selection was found." };
            var parameters = policy.GetJson<ProviderModelRuntimeParameters>("ProviderModelRuntimeParametersJson");
            var deadline = TimeSpan.FromSeconds(Math.Clamp(policy.GetInt("HelpEscalatorCuratorTimeoutSeconds"), 10, 180));
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(deadline);
            using var client = models.CreateChatClient(selected.ToReference(), parameters.DefaultSessionKeepAlive, 8192,
                deadline, null, enableAutomaticTools: false, throwOnFailure: true);
            // The model receives explicitly identified registered schema facts, never
            // a caller's assertion that some missing parameter was mandatory.
            var registered = string.IsNullOrWhiteSpace(report.FunctionName) ? null : functions.GetFunctions()
                .FirstOrDefault(function => string.Equals(function.Name, report.FunctionName, StringComparison.OrdinalIgnoreCase));
            var requiredParameters = new List<string>();
            var optionalParameters = new List<string>();
            if (registered is not null && !string.IsNullOrWhiteSpace(registered.ParameterSchemaJson))
            {
                using var document = JsonDocument.Parse(registered.ParameterSchemaJson);
                var schema = document.RootElement;
                if (schema.ValueKind == JsonValueKind.Object)
                {
                    if (schema.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array)
                        requiredParameters.AddRange(required.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String)
                            .Select(item => item.GetString()!).Take(100));
                    if (schema.TryGetProperty("properties", out var properties) && properties.ValueKind == JsonValueKind.Object)
                        optionalParameters.AddRange(properties.EnumerateObject().Select(item => item.Name)
                            .Where(name => !requiredParameters.Contains(name, StringComparer.Ordinal))
                            .Take(100));
                }
            }
            var input = JsonSerializer.Serialize(new
            {
                task = report.Task[..Math.Min(2000, report.Task.Length)],
                registeredFunction = registered?.Name,
                registeredRequiredParameters = registered is null ? null : requiredParameters,
                registeredOptionalParameters = registered is null ? null : optionalParameters,
                failedAction = report.Action[..Math.Min(500, report.Action.Length)],
                failures,
                status = report.Status.ToString(),
                claimedErrorSource = report.Error?.Source.ToString(),
                errorCode = (report.Error?.Code ?? string.Empty)[..Math.Min(120, report.Error?.Code?.Length ?? 0)],
                errorMessage = (report.Error?.Message ?? string.Empty)[..Math.Min(1000, report.Error?.Message?.Length ?? 0)],
                previousActionMustChange = true
            });
            var response = await client.GetResponseAsync(
                [new ChatMessage(ChatRole.System, policy.GetString("HelpEscalatorCuratorSystemPrompt")),
                 new ChatMessage(ChatRole.User, input)],
                new ChatOptions { Temperature = 0f, MaxOutputTokens = 350 }, timeout.Token).ConfigureAwait(false);
            var text = System.Net.WebUtility.HtmlDecode(response.Text ?? string.Empty).Trim();
            // Models may wrap a single object in a markdown JSON fence; never parse concatenated objects.
            if (text.StartsWith("```", StringComparison.Ordinal))
            {
                var firstLine = text.IndexOf('\n');
                var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
                if (firstLine >= 0 && lastFence > firstLine)
                    text = text[(firstLine + 1)..lastFence].Trim();
            }
            if (text.Length > 4096) return new AiHelpCuratorAdvice { Type = "cannot_resolve", Reason = "Curator response exceeded the bounded review size." };
            var advice = JsonSerializer.Deserialize<AiHelpCuratorAdvice>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (advice is null || advice.Type is not ("retry" or "ask_user" or "cannot_resolve"))
                return new AiHelpCuratorAdvice { Type = "cannot_resolve", Reason = "Curator returned no valid structured alternative." };
            advice.Instruction = (advice.Instruction ?? string.Empty)[..Math.Min(1600, advice.Instruction?.Length ?? 0)];
            advice.Question = (advice.Question ?? string.Empty)[..Math.Min(1000, advice.Question?.Length ?? 0)];
            advice.Reason = (advice.Reason ?? string.Empty)[..Math.Min(1000, advice.Reason?.Length ?? 0)];
            return advice;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            logger.LogWarning("AI help curator unavailable ({ErrorType}); model output and attempt evidence omitted.", error.GetType().Name);
            return new AiHelpCuratorAdvice { Type = "cannot_resolve", Reason = "Curator request failed; inspect diagnostics." };
        }
    }
}
