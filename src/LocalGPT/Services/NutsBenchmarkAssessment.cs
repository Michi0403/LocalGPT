using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Validates NUTS scenarios and scores model answers independently of model reviewers.</summary>
public sealed class NutsBenchmarkAssessment(
    ICouncilTeamConfigurationService teams,
    IProviderModelRuntimeService providerModels,
    ILogger<NutsBenchmarkAssessment> logger)
{
    internal const int MaximumScenarioCharacters = 262_144;
    private const int MaximumCases = 256;
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal NutsBenchmarkScenario Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > MaximumScenarioCharacters)
            throw new ArgumentException($"Enter scenario JSON up to {MaximumScenarioCharacters:N0} characters.");

        try
        {
            return JsonSerializer.Deserialize<NutsBenchmarkScenario>(json, JsonOptions)
                ?? throw new ArgumentException("Enter a scenario JSON object.");
        }
        catch (JsonException)
        {
            throw new ArgumentException("The scenario JSON could not be read. Check its structure and field values.");
        }
    }

    internal IReadOnlyList<ProviderModelBenchmarkTaskDefinition> Prepare(NutsBenchmarkScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (string.IsNullOrWhiteSpace(scenario.Name) || scenario.Name.Length > 120 || scenario.Cases is not { Count: > 0 and <= MaximumCases })
            throw new ArgumentException($"Provide a scenario name and between 1 and {MaximumCases} cases.");

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var size = 0;
        foreach (var scenarioCase in scenario.Cases)
        {
            if (scenarioCase is null || string.IsNullOrWhiteSpace(scenarioCase.Name)
                || scenarioCase.Name.Length > 120 || !names.Add(scenarioCase.Name.Trim())
                || string.IsNullOrWhiteSpace(scenarioCase.TeamKey) || scenarioCase.TeamKey.Length > 120
                || string.IsNullOrWhiteSpace(scenarioCase.Role) || scenarioCase.Role.Length > 120
                || string.IsNullOrWhiteSpace(scenarioCase.Prompt) || scenarioCase.Prompt.Length > 12_000
                || scenarioCase.Expected is not { Count: > 0 and <= 32 }
                || scenarioCase.Expected.Any(pair => string.IsNullOrWhiteSpace(pair.Key)
                    || pair.Key.Length > 120 || pair.Value.ValueKind == JsonValueKind.Undefined))
                throw new ArgumentException("Every case needs a unique name, Council team key, role, prompt and 1–32 expected JSON properties.");

            size += scenarioCase.Prompt.Length + scenarioCase.Expected.Sum(pair => pair.Key.Length + pair.Value.GetRawText().Length);
            if (size > MaximumScenarioCharacters)
                throw new ArgumentException("The combined case prompts and answer keys exceed the scenario size limit.");
        }

        return scenario.Cases.Select(scenarioCase => new ProviderModelBenchmarkTaskDefinition
        {
            Name = scenarioCase.Name.Trim(),
            Prompt = $"{scenarioCase.Prompt.Trim()}\n\nReturn one JSON object without Markdown or surrounding prose.",
            ExpectJson = true
        }).ToList();
    }

    internal void Record(NutsBenchmarkScenario scenario, ProviderModelBenchmarkReport report)
    {
        report.NutsScenarioName = scenario.Name.Trim();
        var roleCases = scenario.Cases.GroupBy(item => (Team: item.TeamKey.Trim().ToUpperInvariant(), Role: item.Role.Trim().ToUpperInvariant())).ToList();
        foreach (var target in report.Targets)
        foreach (var profile in target.Profiles)
        {
            var results = profile.Tasks.ToDictionary(task => task.TaskName, StringComparer.OrdinalIgnoreCase);
            foreach (var role in roleCases)
            {
                var passed = 0;
                var gaps = new List<string>();
                var unmeasured = new List<string>();
                foreach (var item in role)
                {
                    var name = item.Name.Trim();
                    if (!results.TryGetValue(name, out var result) || !string.IsNullOrEmpty(result.Error) || result.ResponseTextTruncated)
                        unmeasured.Add(name);
                    else if (Matches(result.ResponseText, item.Expected))
                        passed++;
                    else
                        gaps.Add(name);
                }

                var definition = role.First();
                report.NutsRoleEvidence.Add(new NutsRoleEvidence(target.Model.SelectionKey, target.Model.DisplayName,
                    profile.ProfileName, definition.TeamKey.Trim(), definition.Role.Trim(), passed, gaps, unmeasured));
            }
        }

        report.NutsRoleRecommendations = report.NutsRoleEvidence
            .Where(evidence => evidence.Passed >= 2 && evidence.CapabilityGaps.Count == 0 && evidence.UnmeasuredCases.Count == 0)
            .GroupBy(evidence => (evidence.TeamKey.ToUpperInvariant(), evidence.Role.ToUpperInvariant()))
            .Select(group => group.OrderBy(evidence => evidence.ModelSelectionKey, StringComparer.OrdinalIgnoreCase).First())
            .ToList();
    }

    /// <summary>Checks role keys against the current Council configuration before provider calls begin.</summary>
    internal async Task ValidateRolesAsync(NutsBenchmarkScenario scenario, CancellationToken cancellationToken)
    {
        try
        {
            var configured = (await teams.GetTeamsAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
                .ToDictionary(team => team.Key, StringComparer.OrdinalIgnoreCase);
            if (scenario.Cases.Any(item => !configured.TryGetValue(item.TeamKey.Trim(), out var team)
                || !team.Roles.Any(role => role.Role.Equals(item.Role.Trim(), StringComparison.OrdinalIgnoreCase)
                    && role.HumanParticipationMode != HumanParticipationMode.HumanOnly)))
                throw new ArgumentException("Every case must reference an enabled Council team and an existing AI role. Check the team keys and role names in Council Teams.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not ArgumentException)
        {
            logger.LogError(exception, "Checking NUTS scenario roles failed.");
            throw;
        }
    }

    /// <summary>Saves one recommended model through the ordinary Council role settings after explicit selection.</summary>
    internal async Task ApplyAsync(ProviderModelBenchmarkReport report, NutsRoleEvidence recommendation, bool userConfirmed,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!userConfirmed || !report.NutsRoleRecommendations.Contains(recommendation))
                throw new InvalidOperationException("Choose an exact completed NUTS recommendation.");

            var available = await providerModels.GetCandidatesAsync(cancellationToken).ConfigureAwait(false);
            if (!available.Any(model => model.IsInstalled && model.SelectionKey.Equals(recommendation.ModelSelectionKey, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("The recommended model is unavailable on its measured host.");

            var team = await teams.FindTeamAsync(recommendation.TeamKey, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The recommended team is unavailable.");
            var role = team.Roles.SingleOrDefault(role => role.Role.Equals(recommendation.Role, StringComparison.OrdinalIgnoreCase));
            if (role is null || role.HumanParticipationMode == HumanParticipationMode.HumanOnly
                || role.AiSelectionMode != CouncilRoleAiSelectionMode.AllSelected || role.AssignedModelKeys.Count > 0)
                throw new InvalidOperationException("The role already has a selection policy or is unavailable. Existing assignments were kept.");

            role.AiSelectionMode = CouncilRoleAiSelectionMode.AssignedModels;
            role.AssignedModelKeys = [recommendation.ModelSelectionKey];
            await teams.SaveAsync(new SaveCouncilTeamConfigurationRequest
            {
                Team = team,
                IsEnabled = team.IsEnabled,
                UserConfirmed = userConfirmed
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Applying a NUTS role recommendation failed for benchmark {RunId}.", report.RunId);
            throw;
        }
    }

    private bool Matches(string response, Dictionary<string, JsonElement> expected)
    {
        try
        {
            using var document = JsonDocument.Parse(response);
            var answer = document.RootElement;
            return answer.ValueKind == JsonValueKind.Object
                && answer.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count()
                    == answer.EnumerateObject().Count()
                && expected.All(pair => answer.TryGetProperty(pair.Key, out var value) && JsonElement.DeepEquals(value, pair.Value));
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
