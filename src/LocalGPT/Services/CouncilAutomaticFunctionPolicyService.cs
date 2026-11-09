using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Resolves user-owned Council automatic/native function policy without embedding team-specific function lists in runtime orchestration code.</summary>
/// <param name="logger">Logger used for bounded policy diagnostics.</param>
/// <param name="runtimePolicy">Database-backed runtime values that choose the unlisted-step tool mode, the base tool set and the capability-to-function map.</param>
public sealed class CouncilAutomaticFunctionPolicyService(
    ILogger<CouncilAutomaticFunctionPolicyService> logger,
    ILocalGptRuntimePolicyDataService runtimePolicy) : ICouncilAutomaticFunctionPolicyService
{
    /// <summary>Runtime value selecting how steps without a saved tool preference attach native tools.</summary>
    private const string UnlistedStepToolModeKey = "CouncilUnlistedStepNativeToolMode";
    /// <summary>Runtime collection of functions attached to every non-strict step in compact mode.</summary>
    private const string BaseNativeFunctionsKey = "CouncilBaseNativeFunctions";
    /// <summary>Runtime JSON map from requested organic/1-Wire capability keys to the DXFunctions that serve them.</summary>
    private const string CapabilityNativeFunctionsKey = "CouncilCapabilityNativeFunctionsJson";
    /// <summary>Legacy unlisted-step mode that attaches every registered policy-approved function.</summary>
    private const string AllPolicyApprovedMode = "AllPolicyApproved";

    /// <summary>Resolves the effective automatic/native function exposure for one configured workflow step from persisted team and step policy.</summary>
    /// <param name="team">User-editable Council team that owns the team-level allow-list.</param>
    /// <param name="step">Workflow step whose configured policy mode is being evaluated.</param>
    /// <param name="suppressAutomaticFunctions">Forces automatic functions off for execution paths that require tool isolation.</param>
    /// <param name="requestedCapabilities">Organic/1-Wire capabilities the caller requested for this run; their serving functions are attached natively in non-strict steps.</param>
    /// <returns>The effective enabled state, optional exact allow-list, and user-visible policy description.</returns>
    public CouncilAutomaticFunctionPolicyResolution Resolve(
        OrganicCouncilTeamDefinition team,
        CouncilWorkflowStepDefinition step,
        bool suppressAutomaticFunctions = false,
        IReadOnlyCollection<string>? requestedCapabilities = null)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(team);
            ArgumentNullException.ThrowIfNull(step);

            if (suppressAutomaticFunctions || !step.CanUseOrganicFunctions)
            {
                return new CouncilAutomaticFunctionPolicyResolution(
                    false,
                    [],
                    suppressAutomaticFunctions
                        ? "disabled for this workflow revision"
                        : "disabled by the workflow step");
            }

            var mode = step.AutomaticFunctionPolicyMode;
            if (mode == CouncilAutomaticFunctionPolicyMode.Legacy)
            {
                mode = step.AllowedAutomaticFunctions is { Count: > 0 }
                    ? CouncilAutomaticFunctionPolicyMode.ExactAllowList
                    : CouncilAutomaticFunctionPolicyMode.AllPolicyApproved;
            }

            if (mode == CouncilAutomaticFunctionPolicyMode.Disabled)
                return new CouncilAutomaticFunctionPolicyResolution(false, [], "disabled by the workflow step");

            var teamList = Normalize(team.AllowedAutomaticFunctions);
            var stepList = Normalize(step.AllowedAutomaticFunctions);
            // Only the selected workflow step may opt into an exclusive tool list.
            // Team keys and runtime classes never infer or persist strictness.
            var strictTeam = mode == CouncilAutomaticFunctionPolicyMode.StrictTeamAllowList;
            var strictStep = mode == CouncilAutomaticFunctionPolicyMode.StrictStepAllowList;

            // A strict list scopes the tools offered in this step, not authorization or
            // later workflow steps. Central DXFunction / organic-skill policy still decides
            // whether each selected tool can run and whether approval is required.
            // No unrestricted broker is attached to an exclusive list; it would evade
            // the role-step tool-selection contract.
            if (strictTeam || strictStep)
            {
                // Broker entrypoints cannot be present in an exclusive allow-list:
                // exposing dxai.tools.invoke would indirectly grant access to the
                // entire catalog and negate the saved strict policy.
                // The step's own X-Round authority is part of its configuration, not catalog access:
                // a controller step that may revisit earlier work must be offered the tool to do so.
                var allowed = (strictTeam ? teamList : stepList)
                    .Where(name => !IsBrokerTool(name))
                    .Concat(ConfiguredXRoundFunctions(step))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var scope = strictTeam ? "team" : "step";
                return new CouncilAutomaticFunctionPolicyResolution(
                    allowed.Count > 0,
                    allowed,
                    allowed.Count > 0
                        ? $"strict {scope} function allow-list: {string.Join(", ", allowed)}"
                        : $"strict {scope} function allow-list is empty; native tools are disabled",
                    IsStrict: true);
            }

            // In all non-strict workflows the saved names are
            // *preferences*, never an execution boundary. Promote them as native Ollama
            // metadata while the normal broker exposes every other registered,
            // policy-approved DXFunction on demand without sending thousands of schemas.
            IReadOnlyList<string> preferred = mode switch
            {
                CouncilAutomaticFunctionPolicyMode.TeamAllowList => teamList,
                CouncilAutomaticFunctionPolicyMode.ExactAllowList => stepList,
                CouncilAutomaticFunctionPolicyMode.AllPolicyApproved => Normalize(stepList.Concat(teamList)),
                _ => []
            };
            var capabilityFunctions = ResolveCapabilityFunctions(requestedCapabilities);
            if (preferred.Count == 0)
            {
                if (string.Equals(ReadUnlistedStepToolMode(), AllPolicyApprovedMode, StringComparison.OrdinalIgnoreCase))
                {
                    return new CouncilAutomaticFunctionPolicyResolution(
                        true,
                        null,
                        "all registered policy-approved automatic DXFunctions available as native tools; no preference list");
                }

                // Attaching every registered schema (200+) overflowed small model contexts: Ollama cut the
                // oldest prompt content and the requested 1-Wire function was no longer visible. Without a saved
                // preference the step receives the configured base set, the functions serving the requested
                // capabilities and the broker, which still reaches the full policy-approved catalog on demand.
                var compact = ReadBaseNativeFunctions()
                    .Concat(capabilityFunctions)
                    .Concat(AlwaysOfferedFunctions())
                    .Concat(ConfiguredXRoundFunctions(step))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                logger.LogDebug("Workflow step {StepKey} of team {TeamKey} has no tool preference; attaching {Count} compact native tools.", step.Key, team.Key, compact.Count);
                return new CouncilAutomaticFunctionPolicyResolution(
                    true,
                    compact,
                    capabilityFunctions.Count == 0
                        ? "compact native tools (no preference list); full policy-approved catalog remains available through discovery/preview/invoke"
                        : $"compact native tools for requested capabilities: {string.Join(", ", capabilityFunctions)}; full policy-approved catalog remains available through discovery/preview/invoke");
            }

            // Attachments are first-class context in every non-strict Council role, not a
            // privilege of a named team or of the Learning Round. Approval/policy still applies.
            var exposed = preferred
                .Concat(capabilityFunctions)
                .Concat(AlwaysOfferedFunctions())
                .Concat(ConfiguredXRoundFunctions(step))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return new CouncilAutomaticFunctionPolicyResolution(
                true,
                exposed,
                $"preferred native tools: {string.Join(", ", preferred.Concat(capabilityFunctions).Distinct(StringComparer.OrdinalIgnoreCase))}; full policy-approved catalog remains available through discovery/preview/invoke");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving Council automatic-function policy failed for team {TeamKey} and step {StepKey}.", team?.Key, step?.Key);
            throw;
        }
    }

    /// <summary>Returns the attachment, AI-help and broker entrypoints offered in every non-strict Council step.</summary>
    /// <returns>Function names that are always attached in non-strict steps.</returns>
    private IReadOnlyList<string> AlwaysOfferedFunctions()
    {
        try
        {
            return ["chat.upload_workspace_files", "chat.upload_workspace_context", "chat.upload_workspace_file",
                "ai.help.report_attempt", "ai.help.status",
                "dxai.tools.discover", "dxai.tools.preview", "dxai.tools.invoke"];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the always-offered Council native tool list failed.");
            throw;
        }
    }

    /// <summary>Reads how steps without a saved tool preference attach native tools.</summary>
    /// <returns>The configured mode; compact mode when the runtime value is unavailable.</returns>
    private string ReadUnlistedStepToolMode()
    {
        try
        {
            return runtimePolicy.GetString(UnlistedStepToolModeKey).Trim();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Runtime value {Key} is unavailable; steps without a tool preference use the compact native tool set.", UnlistedStepToolModeKey);
            return string.Empty;
        }
    }

    /// <summary>Reads the configured base native functions for steps without a saved tool preference.</summary>
    /// <returns>Configured function names, or an empty list when the runtime collection is unavailable.</returns>
    private IReadOnlyList<string> ReadBaseNativeFunctions()
    {
        try
        {
            return Normalize(runtimePolicy.GetCollection(BaseNativeFunctionsKey));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Runtime collection {Key} is unavailable; no additional base native tools are attached.", BaseNativeFunctionsKey);
            return [];
        }
    }

    /// <summary>Maps the capabilities requested for this run to the DXFunctions that serve them.</summary>
    /// <param name="requestedCapabilities">Requested organic/1-Wire capability keys.</param>
    /// <returns>Serving function names; empty when nothing was requested or the map is unavailable.</returns>
    private IReadOnlyList<string> ResolveCapabilityFunctions(IReadOnlyCollection<string>? requestedCapabilities)
    {
        try
        {
            if (requestedCapabilities is null || requestedCapabilities.Count == 0)
                return [];
            Dictionary<string, List<string>> map;
            try
            {
                map = new Dictionary<string, List<string>>(
                    runtimePolicy.GetJson<Dictionary<string, List<string>>>(CapabilityNativeFunctionsKey),
                    StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Runtime value {Key} is unavailable; requested capabilities are not mapped to native tools.", CapabilityNativeFunctionsKey);
                return [];
            }
            var functions = new List<string>();
            foreach (var capability in requestedCapabilities.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                if (map.TryGetValue(capability.Trim(), out var served))
                    functions.AddRange(served);
                else
                    logger.LogDebug("Requested capability {Capability} has no native function mapping in {Key}; it stays reachable through the broker.", capability, CapabilityNativeFunctionsKey);
            }
            return Normalize(functions);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Mapping requested capabilities to native Council tools failed.");
            throw;
        }
    }

    /// <summary>
    /// Lists the X-Round DXFunctions a step's own configuration grants, so a controller step is offered the native tool
    /// for every control action it is allowed to take (status, revisit, return text, single model, child Council).
    /// </summary>
    /// <param name="step">Workflow step whose X-Round settings are read.</param>
    /// <returns>The granted X-Round function names; empty when the step has no X-Round authority.</returns>
    private IReadOnlyList<string> ConfiguredXRoundFunctions(CouncilWorkflowStepDefinition step)
    {
        try
        {
            if (!step.XFunctionsEnabled)
                return [];
            var functions = new List<string> { "council.x.status" };
            if (step.XCanRevisit)
                functions.Add("council.x.revisit");
            if (step.XCanReturnText)
                functions.Add("council.x.return_text");
            if (step.XCanStartSingleModel)
                functions.Add("council.x.start_single_model");
            if (step.XCanStartCouncil)
                functions.Add("council.x.start_council");
            logger.LogDebug("Workflow step {StepKey} is offered X-Round functions {Functions}.", step.Key, string.Join(", ", functions));
            return functions;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the X-Round functions of workflow step {StepKey} failed.", step?.Key);
            throw;
        }
    }

    /// <summary>Checks whether a function name is one of the unrestricted DXFunction broker entrypoints.</summary>
    /// <param name="name">Registered function name to inspect.</param>
    /// <returns>Whether that name permits catalog-wide discovery or invocation.</returns>
    private bool IsBrokerTool(string name)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return name.Equals("dxai.tools.discover", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("dxai.tools.preview", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("dxai.tools.invoke", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Classifying Council DXFunction broker entrypoint {FunctionName} failed.", name);
            throw;
        }
    }

    /// <summary>Normalizes one user-edited function list while preserving canonical registered names.</summary>
    /// <param name="values">Function names stored by the team or workflow-step configuration.</param>
    /// <returns>A trimmed, case-insensitively distinct and deterministically ordered function-name list.</returns>
    private IReadOnlyList<string> Normalize(IEnumerable<string>? values)
    {
        try
        {
            return (values ?? [])
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing a Council automatic-function allow-list failed.");
            throw;
        }
    }
}
