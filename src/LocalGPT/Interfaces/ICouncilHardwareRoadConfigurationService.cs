using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Normalizes editable per-model CPU/GPU road settings without coupling the UI to the council
/// scheduler. The returned routes remain transport-compatible 1-Wire business objects.
/// </summary>
public interface ICouncilHardwareRoadConfigurationService
{
    /// <summary>
    /// Performs synchronize as part of the council hardware road configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="modelNames">String dependency used by the council hardware road configuration workflow to provide the corresponding application capability.</param>
    /// <param name="existingRoutes">One wire council model route dependency used by the council hardware road configuration workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<OneWireCouncilModelRoute> Synchronize(
        IEnumerable<string> modelNames,
        IEnumerable<OneWireCouncilModelRoute>? existingRoutes);

    /// <summary>Copies one road and its mutable capability lists independently of the original.</summary>
    /// <param name="route">The original road.</param>
    /// <returns>An independent road copy.</returns>
    OneWireCouncilModelRoute CloneRoute(OneWireCouncilModelRoute route);

    /// <summary>Expands generic preset roads over available Ollama providers while retaining explicit provider bindings and per-road limits.</summary>
    /// <param name="modelNames">Preset model selections.</param>
    /// <param name="existingRoutes">Saved road templates.</param>
    /// <param name="candidates">Current provider inventory.</param>
    /// <returns>Provider-qualified road copies for the editable selection.</returns>
    IReadOnlyList<OneWireCouncilModelRoute> ResolvePresetRoutes(
        IEnumerable<string> modelNames, IEnumerable<OneWireCouncilModelRoute> existingRoutes,
        IReadOnlyList<MultiModelCouncilModelCandidate> candidates);

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
