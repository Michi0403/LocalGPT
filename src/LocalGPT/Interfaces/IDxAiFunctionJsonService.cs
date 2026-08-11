using LocalGPT.BusinessObjects;
using System.Text.Json;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the DevExpress ai function JSON service contract.
/// </summary>
public interface IDxAiFunctionJsonService
{
    JsonSerializerOptions Options { get; }
    /// <summary>
    /// Runs the deserialize operation.
    /// </summary>
    T Deserialize<T>(JsonElement element) where T : new();
    /// <summary>
    /// Runs the bind operation.
    /// </summary>
    DxAiFunctionParameterBinding<T> Bind<T>(JsonElement element) where T : new();
    /// <summary>
    /// Runs the invalid parameters operation.
    /// </summary>
    DxAiFunctionInvocationResult InvalidParameters(string error);
    /// <summary>
    /// Runs the success operation.
    /// </summary>
    DxAiFunctionInvocationResult Success(object? value = null, string status = "Completed");
}
