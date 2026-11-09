using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Director;

/// <summary>
/// Default <see cref="IGameDirectorPredictionService"/>. Rule parameters: <c>directorKey</c>, <c>actorKind</c>
/// (a <see cref="CouncilGameActorKind"/> name), <c>runtimeClassKey</c>, <c>confidencePercent</c>, <c>triggerActions</c>
/// with <c>triggeredTextKey</c>/<c>idleTextKey</c> (texts receive the token <c>count</c>), <c>generatedActors</c>
/// (<c>countFromCreatureDirectors</c>, <c>instanceKeyTemplate</c>, <c>archetypes</c>, <c>slotTemplate</c>; templates
/// receive <c>index</c>) and <c>fixedActors</c> (<c>instanceKey</c>, <c>archetype</c>, <c>slot</c>), plus
/// <c>councilRole</c> and <c>assignmentGroup</c> for every actor.
/// </summary>
/// <param name="parameters">Reads rule parameters.</param>
/// <param name="texts">Formats the game's prediction texts.</param>
/// <param name="formatter">Formats key templates.</param>
/// <param name="runtimePolicy">Supplies the creature-director bounds.</param>
/// <param name="logger">Receives prediction diagnostics.</param>
public sealed class GameDirectorPredictionService(
    IGameParameterReader parameters,
    IGameTextService texts,
    IGameTemplateFormatter formatter,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<GameDirectorPredictionService> logger) : IGameDirectorPredictionService
{
    /// <inheritdoc />
    public IReadOnlyList<CouncilGameSubdirectorPrediction> Predict(CouncilGameDirectorContext context)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(context);
            var bounds = runtimePolicy.GetJson<CouncilGameRuntimeParameters>("CouncilGameRuntimeParametersJson");
            var directors = Math.Clamp(context.Session.CreatureDirectorCount, bounds.MinimumCreatureDirectors, bounds.MaximumCreatureDirectors);
            var predictions = new List<CouncilGameSubdirectorPrediction>();
            foreach (var rule in context.Session.Ruleset.Rules
                .Where(item => item.IsEnabled && string.Equals(item.Trigger, "director.prediction", StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.SortOrder))
            {
                predictions.Add(BuildPrediction(context, parameters.Parse(rule.ParametersJson), directors));
            }
            return predictions;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building GameDirector predictions failed for session {GameSessionId}.", context?.Session?.Id);
            throw;
        }
    }

    /// <summary>Builds one prediction from one rule row.</summary>
    /// <param name="context">Immutable session snapshot and the normalized proposal.</param>
    /// <param name="rule">Parsed rule parameters.</param>
    /// <param name="directors">Bounded creature-director count of the session.</param>
    /// <returns>The prediction.</returns>
    private CouncilGameSubdirectorPrediction BuildPrediction(CouncilGameDirectorContext context, JsonElement rule, int directors)
    {
        try
        {
            var actorKind = Enum.TryParse<CouncilGameActorKind>(parameters.ReadString(rule, "actorKind", string.Empty), true, out var parsed)
                ? parsed
                : CouncilGameActorKind.Creature;
            var runtimeClassKey = parameters.ReadString(rule, "runtimeClassKey", string.Empty);
            var triggered = parameters.ReadStrings(rule, "triggerActions")
                .Contains(context.NormalizedAction ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            var textKey = parameters.ReadString(rule, triggered ? "triggeredTextKey" : "idleTextKey", string.Empty);
            var tokens = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["count"] = directors };
            return new CouncilGameSubdirectorPrediction
            {
                DirectorKey = parameters.ReadString(rule, "directorKey", string.Empty),
                ActorKind = actorKind,
                RuntimeClassKey = runtimeClassKey,
                Prediction = textKey.Length == 0 ? string.Empty : texts.Format(context.Session.Ruleset, textKey, tokens),
                ConfidencePercent = Math.Clamp(parameters.ReadInt(rule, "confidencePercent", 50), 0, 100),
                ActorInstances = BuildActors(rule, actorKind, runtimeClassKey, directors)
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building one GameDirector prediction failed.");
            throw;
        }
    }

    /// <summary>Builds the generated and fixed actor descriptors of one prediction.</summary>
    /// <param name="rule">Parsed rule parameters.</param>
    /// <param name="actorKind">Actor kind of the prediction.</param>
    /// <param name="runtimeClassKey">Runtime class of the predicted actors.</param>
    /// <param name="directors">Bounded creature-director count of the session.</param>
    /// <returns>The actor descriptors.</returns>
    private IReadOnlyList<CouncilGameActorRuntimeDescriptor> BuildActors(JsonElement rule, CouncilGameActorKind actorKind, string runtimeClassKey, int directors)
    {
        try
        {
            var councilRole = parameters.ReadString(rule, "councilRole", string.Empty);
            var assignmentGroup = parameters.ReadString(rule, "assignmentGroup", string.Empty);
            var actors = new List<CouncilGameActorRuntimeDescriptor>();
            if (parameters.TryRead(rule, "generatedActors", out var generated) && generated.ValueKind == JsonValueKind.Object)
            {
                var count = parameters.ReadBool(generated, "countFromCreatureDirectors", false)
                    ? directors
                    : Math.Max(0, parameters.ReadInt(generated, "count", 0));
                var archetypes = parameters.ReadStrings(generated, "archetypes");
                var keyTemplate = parameters.ReadString(generated, "instanceKeyTemplate", "{index}");
                var slotTemplate = parameters.ReadString(generated, "slotTemplate", "{index}");
                for (var index = 1; index <= count; index++)
                {
                    var tokens = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["index"] = index };
                    actors.Add(new CouncilGameActorRuntimeDescriptor
                    {
                        InstanceKey = formatter.Format(keyTemplate, tokens),
                        ActorKind = actorKind,
                        RuntimeClassKey = runtimeClassKey,
                        Archetype = archetypes.Count == 0 ? string.Empty : archetypes[(index - 1) % archetypes.Count],
                        CouncilRole = councilRole,
                        CouncilAssignmentGroup = assignmentGroup,
                        CouncilAssignmentSlot = formatter.Format(slotTemplate, tokens)
                    });
                }
            }

            foreach (var item in parameters.ReadObjects(rule, "fixedActors"))
            {
                actors.Add(new CouncilGameActorRuntimeDescriptor
                {
                    InstanceKey = parameters.ReadString(item, "instanceKey", string.Empty),
                    ActorKind = actorKind,
                    RuntimeClassKey = runtimeClassKey,
                    Archetype = parameters.ReadString(item, "archetype", string.Empty),
                    CouncilRole = councilRole,
                    CouncilAssignmentGroup = assignmentGroup,
                    CouncilAssignmentSlot = parameters.ReadString(item, "slot", string.Empty)
                });
            }
            return actors;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building GameDirector actor descriptors failed.");
            throw;
        }
    }
}
