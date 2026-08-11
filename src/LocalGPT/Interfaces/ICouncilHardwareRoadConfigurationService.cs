namespace LocalGPT.Interfaces;

/// <summary>
/// Normalizes editable per-model CPU/GPU road settings without coupling the UI to the council
/// scheduler. The returned routes remain transport-compatible 1-Wire business objects.
/// </summary>
public interface ICouncilHardwareRoadConfigurationService
{
    /// <summary>
    /// Runs the synchronize operation.
    /// </summary>
    IReadOnlyList<OneWireCouncilModelRoute> Synchronize(
        IEnumerable<string> modelNames,
        IEnumerable<OneWireCouncilModelRoute>? existingRoutes);

    /// <summary>
    /// Runs the normalize operation.
    /// </summary>
    OneWireCouncilModelRoute Normalize(OneWireCouncilModelRoute route);

    /// <summary>
    /// Normalizes load percent.
    /// </summary>
    int NormalizeLoadPercent(int value);

    /// <summary>
    /// Runs the interpolate operation.
    /// </summary>
    int Interpolate(int minimum, int maximum, int loadPercent);
}
