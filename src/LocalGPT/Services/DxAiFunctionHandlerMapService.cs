using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Builds the scoped DXAI handler directory after the registry has been created.
/// This keeps lazy cycle breaking in the registry while moving validation behavior
/// behind an injected service instead of a static helper.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class DxAiFunctionHandlerMapService(
    ILogger<DxAiFunctionHandlerMapService> logger)
{
    /// <summary>
    /// Performs build as part of the DevExpress AI function handler map service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="handlers">Devexpress ai function handler dependency used by the DevExpress AI function handler map workflow to provide the corresponding application capability.</param>
    /// <returns>The i read only dictionary string i DevExpress AI function handler produced by the operation.</returns>
    public IReadOnlyDictionary<string, IDxAiFunctionHandler> Build(
        IEnumerable<IDxAiFunctionHandler> handlers)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(handlers);
            var result = new Dictionary<string, IDxAiFunctionHandler>(StringComparer.OrdinalIgnoreCase);
            foreach (var handler in handlers)
            {
                ArgumentNullException.ThrowIfNull(handler);
                var descriptor = handler.Descriptor
                    ?? throw new InvalidOperationException($"DXAIFunction handler {handler.GetType().FullName} returned no descriptor.");
                ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.Name);
                if (!descriptor.Name.All(character => char.IsLetterOrDigit(character) || character is '.' or '_' or '-'))
                    throw new InvalidOperationException($"DXAIFunction name '{descriptor.Name}' contains unsupported characters.");
                if (!descriptor.Method.Equals("GET", StringComparison.OrdinalIgnoreCase)
                    && !descriptor.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"DXAIFunction {descriptor.Name} declares unsupported method {descriptor.Method}.");
                }

                try
                {
                    using var schema = JsonDocument.Parse(
                        string.IsNullOrWhiteSpace(descriptor.ParameterSchemaJson)
                            ? "{}"
                            : descriptor.ParameterSchemaJson);
                    if (schema.RootElement.ValueKind != JsonValueKind.Object)
                        throw new JsonException("The root schema must be an object.");
                }
                catch (JsonException exception)
                {
                    throw new InvalidOperationException(
                        $"DXAIFunction {descriptor.Name} has invalid parameter-schema JSON.",
                        exception);
                }

                if (!result.TryAdd(descriptor.Name, handler))
                {
                    throw new InvalidOperationException(
                        $"Duplicate DXAIFunction name '{descriptor.Name}'. Function names are stable database-link identifiers and must be unique.");
                }
            }

            logger.LogDebug($"Built the scoped DXAIFunction handler map with {result.Count} entries.");
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not build the scoped DXAIFunction handler map: {exception.Message}");
            throw;
        }
    }
}
