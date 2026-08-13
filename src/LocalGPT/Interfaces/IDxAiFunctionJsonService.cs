using LocalGPT.BusinessObjects;
using System.Text.Json;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for DevExpress AI function JSON behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IDxAiFunctionJsonService
{
    /// <summary>
    /// Gets the options value that forms part of the DevExpress AI function JSON state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The options value exposed by <see cref="IDxAiFunctionJsonService"/>.</value>
    JsonSerializerOptions Options { get; }
    /// <summary>
    /// Performs deserialize as part of the DevExpress AI function JSON service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="IDxAiFunctionJsonService"/>.</typeparam>
    /// <param name="element">Element value supplied to the DevExpress AI function JSON operation and used when producing its result.</param>
    /// <returns>The t produced by the operation.</returns>
    T Deserialize<T>(JsonElement element) where T : new();
    /// <summary>
    /// Performs bind as part of the DevExpress AI function JSON service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="IDxAiFunctionJsonService"/>.</typeparam>
    /// <param name="element">Element value supplied to the DevExpress AI function JSON operation and used when producing its result.</param>
    /// <returns>The DevExpress AI function parameter binding t produced by the operation.</returns>
    DxAiFunctionParameterBinding<T> Bind<T>(JsonElement element) where T : new();
    /// <summary>
    /// Performs invalid parameters as part of the DevExpress AI function JSON service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="error">Error value supplied to the DevExpress AI function JSON operation and used when producing its result.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    DxAiFunctionInvocationResult InvalidParameters(string error);
    /// <summary>
    /// Performs success as part of the DevExpress AI function JSON service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the DevExpress AI function JSON operation and used when producing its result.</param>
    /// <param name="status">Status value supplied to the DevExpress AI function JSON operation and used when producing its result.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    DxAiFunctionInvocationResult Success(object? value = null, string status = "Completed");
}
