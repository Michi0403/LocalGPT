using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Normalizes editable per-model CPU/GPU road settings without coupling the UI to the council
/// scheduler. The returned routes remain transport-compatible 1-Wire business objects.
/// </summary>
public interface ICouncilHardwareRoadConfigurationService
{
    /// <summary>Creates an automatic road from the persisted Council policy or explicit run budgets, without overriding saved routes.</summary>
    /// <param name="modelName">Provider-qualified model selection key.</param>
    /// <param name="requestedMaxOutputTokens">Explicit run output budget, or null to use the persisted Council default.</param>
    /// <param name="requestedMaxContextTokens">Explicit run context budget, or null to use the persisted Council default.</param>
    /// <returns>A new, normalized road with no assumed provider hardware or measured model capacity.</returns>
    OneWireCouncilModelRoute CreateDefaultRoute(string modelName, int? requestedMaxOutputTokens = null, int? requestedMaxContextTokens = null);

    /// <summary>Largest editable Council road output budget from the persisted runtime policy.</summary>
    /// <value>Persisted Council maximum output token budget.</value>
    int MaximumRoadOutputTokens { get; }

    /// <summary>Largest editable Council road context budget from the persisted runtime policy.</summary>
    /// <value>Persisted Council maximum context token budget.</value>
    int MaximumRoadContextTokens { get; }

    /// <summary>
    /// Performs synchronize as part of the council hardware road configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="modelNames">String dependency used by the council hardware road configuration workflow to provide the corresponding application capability.</param>
    /// <param name="existingRoutes">One wire council model route dependency used by the council hardware road configuration workflow to provide the corresponding application capability.</param>
    /// <param name="requestedMaxOutputTokens">Current run output budget for newly discovered, unconfigured routes.</param>
    /// <param name="requestedMaxContextTokens">Current run context budget for newly discovered, unconfigured routes.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<OneWireCouncilModelRoute> Synchronize(
        IEnumerable<string> modelNames,
        IEnumerable<OneWireCouncilModelRoute>? existingRoutes,
        int? requestedMaxOutputTokens = null,
        int? requestedMaxContextTokens = null);

    /// <summary>Refreshes only an unchanged automatically generated route when the operator changes the run budget.</summary>
    /// <param name="route">Existing auto road, left intact if it contains independent configuration.</param>
    /// <param name="previousMaxOutputTokens">Previous Council output ceiling.</param>
    /// <param name="previousMaxContextTokens">Previous Council context ceiling.</param>
    /// <param name="requestedMaxOutputTokens">New Council output ceiling.</param>
    /// <param name="requestedMaxContextTokens">New Council context ceiling.</param>
    /// <returns>Whether the old road was an unmodified generated default and was refreshed.</returns>
    bool TryRefreshAutomaticRoadBudget(OneWireCouncilModelRoute route,
        int previousMaxOutputTokens, int previousMaxContextTokens,
        int requestedMaxOutputTokens, int requestedMaxContextTokens);

    /// <summary>Copies one road and its mutable capability lists independently of the original.</summary>
    /// <param name="route">The original road.</param>
    /// <returns>An independent road copy.</returns>
    OneWireCouncilModelRoute CloneRoute(OneWireCouncilModelRoute route);

    /// <summary>Expands generic preset roads over available Ollama providers while retaining explicit provider bindings and per-road limits.</summary>
    /// <param name="modelNames">Preset model selections.</param>
    /// <param name="existingRoutes">Saved road templates.</param>
    /// <param name="candidates">Current provider inventory.</param>
    /// <param name="requestedMaxOutputTokens">Preset output ceiling for missing model routes.</param>
    /// <param name="requestedMaxContextTokens">Preset context ceiling for missing model routes.</param>
    /// <returns>Provider-qualified road copies for the editable selection.</returns>
    IReadOnlyList<OneWireCouncilModelRoute> ResolvePresetRoutes(
        IEnumerable<string> modelNames, IEnumerable<OneWireCouncilModelRoute> existingRoutes,
        IReadOnlyList<MultiModelCouncilModelCandidate> candidates,
        int? requestedMaxOutputTokens = null, int? requestedMaxContextTokens = null);

    /// <summary>
    /// Performs normalize as part of the council hardware road configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="route">Route value supplied to the council hardware road configuration operation and used when producing its result.</param>
    /// <returns>The one wire council model route produced by the operation.</returns>
    OneWireCouncilModelRoute Normalize(OneWireCouncilModelRoute route);

    /// <summary>
    /// Normalizes load percent as part of the council hardware road configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the council hardware road configuration operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    int NormalizeLoadPercent(int value);

    /// <summary>
    /// Performs interpolate as part of the council hardware road configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="minimum">Minimum value supplied to the council hardware road configuration operation and used when producing its result.</param>
    /// <param name="maximum">Maximum value supplied to the council hardware road configuration operation and used when producing its result.</param>
    /// <param name="loadPercent">Load percent value supplied to the council hardware road configuration operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    int Interpolate(int minimum, int maximum, int loadPercent);
}
