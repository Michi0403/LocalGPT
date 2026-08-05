namespace LocalGPT.Interfaces;

/// <summary>
/// Normalizes editable per-model CPU/GPU road settings without coupling the UI to the council
/// scheduler. The returned routes remain transport-compatible 1-Wire business objects.
/// </summary>
public interface ICouncilHardwareRoadConfigurationService
{
    IReadOnlyList<OneWireCouncilModelRoute> Synchronize(
        IEnumerable<string> modelNames,
        IEnumerable<OneWireCouncilModelRoute>? existingRoutes);

    OneWireCouncilModelRoute Normalize(OneWireCouncilModelRoute route);

    int NormalizeLoadPercent(int value);

    int Interpolate(int minimum, int maximum, int loadPercent);
}
