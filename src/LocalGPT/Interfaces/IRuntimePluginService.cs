using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Persists, materializes, loads, and invokes user-owned runtime extensions.</summary>
public interface IRuntimePluginService
{
    /// <summary>Refreshes the runtime descriptor cache from persistence.</summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);
    /// <summary>Lists persisted runtime extensions.</summary>
    Task<IReadOnlyList<RuntimePluginDefinition>> ListAsync(CancellationToken cancellationToken = default);
    /// <summary>Saves one explicitly confirmed extension definition.</summary>
    Task<RuntimePluginDefinition> SaveAsync(SaveRuntimePluginRequest request, CancellationToken cancellationToken = default);
    /// <summary>Deletes one explicitly confirmed extension definition.</summary>
    Task<bool> DeleteAsync(Guid id, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>Builds/materializes and activates an extension.</summary>
    Task<RuntimePluginBuildResult> BuildAndLoadAsync(Guid id, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>Gets enabled dynamic DXFunction descriptors without database I/O.</summary>
    IReadOnlyList<DxaichatFunctionInfo> GetDescriptors();
    /// <summary>Tries to resolve one enabled dynamic descriptor without database I/O.</summary>
    bool TryGetDescriptor(string functionName, out DxaichatFunctionInfo descriptor);
    /// <summary>Invokes one runtime extension after the central registry security policy has run.</summary>
    Task<DxAiFunctionInvocationResult> InvokeAsync(string functionName, DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default);
    /// <summary>Returns a starter source template for the requested extension kind.</summary>
    string GetTemplate(RuntimePluginKind kind);
}
