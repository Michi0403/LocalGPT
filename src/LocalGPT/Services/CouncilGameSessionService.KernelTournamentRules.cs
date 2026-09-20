using System.Globalization;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LocalGPT.Services;

/// <summary>Resolves the database-backed, copyable Kernel Creature Tournament rule contract.</summary>
public sealed partial class CouncilGameSessionService
{
    /// <summary>Resolves the explicit/team-assigned tournament rules class before a session starts.</summary>
    private async Task<CouncilKernelTournamentRules> ResolveKernelTournamentRulesAsync(
        StartCouncilGameRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            const string starterKey = "games.ascii.kernel-tournament.rules";
            using var scope = scopeFactory.CreateScope();
            var runtimeClasses = scope.ServiceProvider.GetRequiredService<ICouncilRuntimeClassService>();
            var definitions = await runtimeClasses.GetDefinitionsAsync(includeDisabled: false, cancellationToken).ConfigureAwait(false);
            var byKey = definitions
                .Where(definition => !string.IsNullOrWhiteSpace(definition.Key))
                .ToDictionary(definition => definition.Key, StringComparer.OrdinalIgnoreCase);

            CouncilRuntimeClassDefinition? selected = null;
            var explicitKey = request.TournamentRuntimeClassKey?.Trim() ?? string.Empty;
            if (explicitKey.Length > 0 && byKey.TryGetValue(explicitKey, out var explicitDefinition) && IsKernelTournamentRulesDefinition(explicitDefinition))
                selected = explicitDefinition;

            if (selected is null && !string.IsNullOrWhiteSpace(request.TeamKey))
            {
                var teams = scope.ServiceProvider.GetRequiredService<ICouncilTeamConfigurationService>();
                var team = await teams.FindTeamAsync(request.TeamKey, cancellationToken).ConfigureAwait(false);
                if (team is not null)
                {
                    selected = team.Roles
                        .SelectMany(role => role.RuntimeClassKeys ?? [])
                        .Where(key => !string.IsNullOrWhiteSpace(key))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Select(key => byKey.TryGetValue(key, out var definition) ? definition : null)
                        .Where(definition => definition is not null && IsKernelTournamentRulesDefinition(definition))
                        .Cast<CouncilRuntimeClassDefinition>()
                        .OrderBy(definition => definition.IsSystemSeed ? 1 : 0)
                        .FirstOrDefault();
                }
            }

            if (selected is null && byKey.TryGetValue(starterKey, out var starter) && IsKernelTournamentRulesDefinition(starter))
                selected = starter;
            if (selected is null)
                throw new InvalidOperationException($"No enabled Kernel Creature Tournament rules class is available for team '{request.TeamKey}'.");

            var fields = selected.Fields.ToDictionary(field => field.Name, StringComparer.OrdinalIgnoreCase);
            var startingHealth = ReadTournamentRule(fields, "startingHealth", 100, 25, 500);
            var minimumDamage = ReadTournamentRule(fields, "minimumDamage", 7, 1, 100);
            var maximumDamage = ReadTournamentRule(fields, "maximumDamage", 18, minimumDamage, 150);
            return new CouncilKernelTournamentRules
            {
                RuntimeClassKey = selected.Key,
                StartingHealth = startingHealth,
                MinimumDamage = minimumDamage,
                MaximumDamage = maximumDamage,
                GuardReduction = ReadTournamentRule(fields, "guardReduction", 7, 0, maximumDamage - 1),
                RecoveryAmount = ReadTournamentRule(fields, "recoveryAmount", 6, 0, startingHealth),
                MaximumExchangesPerMatch = ReadTournamentRule(fields, "maximumExchangesPerMatch", 12, 1, 50),
                CreaturesPerTrainer = ReadTournamentRule(fields, "creaturesPerTrainer", 3, 1, 5),
                MaximumCreatureSwitchesPerFight = ReadTournamentRule(fields, "maximumCreatureSwitchesPerFight", 3, 0, 12),
                RestRecoveryPerExchange = ReadTournamentRule(fields, "restRecoveryPerExchange", 3, 0, startingHealth),
                TrainerFocusBonus = ReadTournamentRule(fields, "trainerFocusBonus", 2, 0, maximumDamage),
                TrainerBraceReduction = ReadTournamentRule(fields, "trainerBraceReduction", 2, 0, maximumDamage),
                AnimationFrameDelayMilliseconds = ReadTournamentRule(fields, "animationFrameDelayMilliseconds", 320, 250, 5000),
                SubtitleHoldMilliseconds = ReadTournamentRule(fields, "subtitleHoldMilliseconds", 1500, 500, 10000)
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving Kernel Creature Tournament rules failed.");
            throw;
        }
    }

    /// <summary>Recognizes copied tournament rule classes by their persisted field contract rather than by key alone.</summary>
    private bool IsKernelTournamentRulesDefinition(CouncilRuntimeClassDefinition definition)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(definition);
            if (definition.Kind != RuntimeClassKind.State)
                return false;
            var fields = definition.Fields.Select(field => field.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            // Keep 4.5.8 user-copied rule definitions valid. The 4.5.9 team/switch fields are
            // optional extensions and ResolveKernelTournamentRules falls back to seeded defaults when absent.
            return fields.Contains("startingHealth")
                && fields.Contains("minimumDamage")
                && fields.Contains("maximumDamage")
                && fields.Contains("guardReduction")
                && fields.Contains("recoveryAmount")
                && fields.Contains("maximumExchangesPerMatch")
                && fields.Contains("animationFrameDelayMilliseconds")
                && fields.Contains("subtitleHoldMilliseconds");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Inspecting a Kernel Creature Tournament rule contract failed.");
            throw;
        }
    }

    /// <summary>Reads one bounded integer from a persisted runtime-class default value.</summary>
    private int ReadTournamentRule(
        IReadOnlyDictionary<string, RuntimeClassFieldDefinition> fields,
        string name,
        int fallback,
        int minimum,
        int maximum)
    {
        try
        {
            if (!fields.TryGetValue(name, out var field)
                || !int.TryParse(field.DefaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return Math.Clamp(fallback, minimum, maximum);
            return Math.Clamp(value, minimum, maximum);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading Kernel Creature Tournament rule {RuleName} failed.", name);
            throw;
        }
    }
}
