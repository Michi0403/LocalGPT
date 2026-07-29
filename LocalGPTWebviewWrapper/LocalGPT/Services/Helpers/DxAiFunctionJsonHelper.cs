using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Helpers;

internal sealed class DxAiFunctionJsonService(ILogger<DxAiFunctionJsonService> logger) : IDxAiFunctionJsonService
{
    public JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public T Deserialize<T>(JsonElement element) where T : new()
    {
        try
        {
            var value = element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? new T()
                : element.Deserialize<T>(Options) ?? new T();
            logger.LogTrace("Deserialized DXAI function parameters as {ParameterType}.", typeof(T).FullName);
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not deserialize DXAI function parameters as {ParameterType}.", typeof(T).FullName);
            throw;
        }
    }

    public DxAiFunctionInvocationResult Success(object? value = null, string status = "Completed")
    {
        try
        {
            logger.LogTrace("Created a successful DXAI function result with status {Status}.", status);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = status, Value = value };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create a successful DXAI function result with status {Status}.", status);
            throw;
        }
    }
}
