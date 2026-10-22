using LocalGPT.BusinessObjects;
using Microsoft.Extensions.AI;
using LocalGPT.WireProtocol;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Model-assisted, schema-validated goal decomposition using the existing provider-qualified runtime.</summary>
public sealed partial class GoalExecutionService
{
    /// <summary>Reads a model plan only from configured, reachable provider identities and validates it later against live catalogs.</summary>
    /// <param name="goalText">Original requested outcome.</param>
    /// <param name="request">Original non-escalating caller constraints.</param>
    /// <param name="configuredTeams">Current persisted teams.</param>
    /// <param name="functions">Registered executable DXFunctions.</param>
    /// <param name="skills">Online organic capabilities.</param>
    /// <param name="models">Selected participant keys, or auto-selected installed keys.</param>
    /// <param name="cancellationToken">Cancels planning.</param>
    /// <returns>Untrusted proposal, or null if model planning cannot be completed.</returns>
    /// <param name="recoveryContext">Optional recorded failure evidence for bounded semantic replanning.</param>
    private async Task<GoalSemanticProposal?> TrySemanticPlanAsync(
        string goalText, GoalExecutionRequest request,
        IReadOnlyCollection<OrganicCouncilTeamDefinition> configuredTeams,
        IReadOnlyCollection<DxaichatFunctionInfo> functions,
        IReadOnlyCollection<OneWireSkillDescriptor> skills,
        IReadOnlyCollection<string> models,
        CancellationToken cancellationToken, string? recoveryContext = null)
    {
        // Types of directory entries are inferred from the existing directory contract, not redefined.
        try
        {
            var candidates = await providerModels.GetCandidatesAsync(cancellationToken).ConfigureAwait(false);
            var chosen = candidates.FirstOrDefault(candidate => candidate.IsConfigured &&
                (candidate.IsInstalled || !candidate.IsLocal) &&
                (models.Contains(candidate.SelectionKey, StringComparer.OrdinalIgnoreCase)
                    || models.Contains(candidate.ModelName, StringComparer.OrdinalIgnoreCase)));
            if (chosen is null) return null;
            var policy = runtimePolicy.GetJson<ProviderModelRuntimeParameters>(LocalGptRuntimeValue.ProviderModelRuntimeParametersJson);
            using var client = providerModels.CreateChatClient(chosen.ToReference(), policy.DefaultSessionKeepAlive,
                request.CouncilRequest.MaxContextTokens,
                TimeSpan.FromSeconds(Math.Max(10, runtimePolicy.GetInt(LocalGptRuntimeValue.GoalPlannerTimeoutSeconds))),
                request.CouncilRequest.OllamaNumGpu, enableAutomaticTools: false, throwOnFailure: true);
            // Present only a bounded, goal-relevant catalog to the planner. The proposal is
            // still checked against the entire live directory in GoalExecutionService.PlanAsync;
            // this is an inference budget, not an authorization filter.
            var goalTerms = Tokens(goalText);
            var relevantTeams = configuredTeams
                .OrderByDescending(team => string.Equals(team.Key, request.CouncilRequest.CouncilTeamKey,
                    StringComparison.OrdinalIgnoreCase) ? int.MaxValue : Match(goalTerms, team.Key, team.DisplayName, team.Purpose))
                .Take(16).ToList();
            var relevantFunctions = functions
                .OrderByDescending(function => request.RequiredFunctions.Contains(function.Name, StringComparer.OrdinalIgnoreCase)
                    ? int.MaxValue : Match(goalTerms, function.Name, function.Purpose))
                .Take(48).ToList();
            var connected = wirePeers.GetPeers()
                .Where(peer => wireConnections.IsConnected(peer.PeerId))
                .SelectMany(peer => peer.Capabilities)
                .Where(capability => capability.IsEnabled && capability.IsOnline)
                .OrderByDescending(capability => request.RequiredCapabilities.Contains(capability.Key, StringComparer.OrdinalIgnoreCase)
                    ? int.MaxValue : Match(goalTerms, capability.Key, capability.DisplayName, capability.Description))
                .Take(48).ToList();
            var catalog = JsonSerializer.Serialize(new
            {
                goal = goalText,
                previousFailure = recoveryContext,
                explicitTeam = request.PreserveSelectedTeam ? request.CouncilRequest.CouncilTeamKey : null,
                requiredFunctions = request.RequiredFunctions,
                requiredCapabilities = request.RequiredCapabilities,
                teams = relevantTeams.Select(team => new
                {
                    team.Key, team.DisplayName, team.Purpose,
                    roles = team.Roles.Select(role => role.Role),
                    functions = team.AllowedAutomaticFunctions,
                    capabilities = team.PreferredCapabilities
                }),
                functions = relevantFunctions.Select(function => new { function.Name, function.Purpose }),
                skills = skills.OrderByDescending(skill => request.RequiredCapabilities.Contains(skill.Key, StringComparer.OrdinalIgnoreCase)
                    ? int.MaxValue : Match(goalTerms, skill.Key)).Take(32).Select(skill => skill.Key),
                connectedCapabilities = connected.Select(capability => new { capability.Key, capability.DisplayName, capability.Description }),
                participants = models
            });
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(10,
                runtimePolicy.GetInt(LocalGptRuntimeValue.GoalPlannerTimeoutSeconds))));
            var response = await client.GetResponseAsync(
                [new ChatMessage(ChatRole.System,
                    runtimePolicy.GetString(LocalGptRuntimeValue.GoalSemanticPlannerSystemPrompt)),
                 new ChatMessage(ChatRole.User, catalog)],
                new ChatOptions
                {
                    MaxOutputTokens = Math.Max(256, runtimePolicy.GetInt(LocalGptRuntimeValue.GoalPlannerOutputTokens)),
                    Temperature = 0f
                }, timeout.Token).ConfigureAwait(false);
            var responseText = response.Text ?? string.Empty;
            // Some compatible model gateways return HTML-encoded JSON (for example
            // {&quot;steps&quot;:...}); decode the transport representation before parsing.
            // Preserve literal ampersands in otherwise valid JSON strings.
            var objectStart = responseText.IndexOf('{');
            if (objectStart >= 0)
            {
                var objectBody = responseText[(objectStart + 1)..].TrimStart();
                if (objectBody.StartsWith("&quot;", StringComparison.OrdinalIgnoreCase) ||
                    objectBody.StartsWith("&#34;", StringComparison.OrdinalIgnoreCase) ||
                    objectBody.StartsWith("&#x22;", StringComparison.OrdinalIgnoreCase) ||
                    objectBody.StartsWith("&amp;quot;", StringComparison.OrdinalIgnoreCase))
                {
                    responseText = System.Net.WebUtility.HtmlDecode(responseText);
                    if (responseText.Contains("&quot;", StringComparison.OrdinalIgnoreCase))
                        responseText = System.Net.WebUtility.HtmlDecode(responseText);
                }
            }
            var start = responseText.IndexOf('{');
            var end = responseText.LastIndexOf('}');
            if (start < 0 || end <= start) return null;
            var json = responseText.Substring(start, end - start + 1);
            // An LLM sometimes emits illustrative ellipses in JSON values, e.g. "teams": ... .
            // Reject that incomplete response as ordinary failed planning *before* invoking the
            // strict JSON parser. Deterministic catalog routing will handle the request.
            if (ContainsInvalidBarePeriod(json))
            {
                logger.LogWarning("Goal {GoalId} semantic model returned incomplete JSON; using catalog fallback.",
                    request.CouncilRequest.RunId);
                return null;
            }
            GoalSemanticProposal? proposal;
            try
            {
                proposal = JsonSerializer.Deserialize<GoalSemanticProposal>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                logger.LogWarning("Goal {GoalId} semantic model returned malformed JSON; using catalog fallback.",
                    request.CouncilRequest.RunId);
                return null;
            }
            return proposal is { Steps.Count: > 0, Functions: not null, Capabilities: not null } ? proposal : null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Inference failure must not remove the deterministic capability-based fallback.
            logger.LogWarning(exception, "Semantic planner unavailable for goal {GoalId}; using live catalog fallback.",
                request.CouncilRequest.RunId);
            return null;
        }
    }

    /// <summary>Detects unquoted model placeholders without changing legitimate quoted text.</summary>
    /// <param name="text">Untrusted JSON candidate.</param>
    /// <returns>True when a non-numeric period appears outside a JSON string.</returns>
    private bool ContainsInvalidBarePeriod(string text)
    {
        try
        {
            var quoted = false;
            var escaped = false;
            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                if (quoted)
                {
                    if (escaped) escaped = false;
                    else if (character == '\\') escaped = true;
                    else if (character == '"') quoted = false;
                    continue;
                }
                if (character == '"') { quoted = true; continue; }
                // Bare periods are legal only inside numeric JSON literals, e.g. 1.25.
                // An LLM's placeholder "." or "..." is not a JSON value.
                if (character == '.' && (index == 0 || index + 1 >= text.Length ||
                    !char.IsDigit(text[index - 1]) || !char.IsDigit(text[index + 1])))
                    return true;
            }
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to inspect semantic-plan response syntax.");
            return true;
        }
    }

}
