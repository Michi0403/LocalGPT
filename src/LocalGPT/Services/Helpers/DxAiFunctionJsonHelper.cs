using System.Reflection;
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
        var binding = Bind<T>(element);
        if (binding.Succeeded)
            return binding.Value;

        throw new JsonException(binding.Error);
    }

    public DxAiFunctionParameterBinding<T> Bind<T>(JsonElement element) where T : new()
    {
        try
        {
            var value = element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? new T()
                : element.Deserialize<T>(Options) ?? new T();

            var missingGuid = typeof(T)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(property =>
                    property.PropertyType == typeof(Guid) &&
                    property.CanRead &&
                    property.GetValue(value) is Guid guid &&
                    guid == Guid.Empty);
            if (missingGuid is not null)
            {
                var parameterName = JsonNamingPolicy.CamelCase.ConvertName(missingGuid.Name);
                return Failed<T>($"Parameter '{parameterName}' must contain a valid non-empty GUID.");
            }

            logger.LogTrace("Bound DXAI function parameters as {ParameterType}.", typeof(T).FullName);
            return new DxAiFunctionParameterBinding<T> { Succeeded = true, Value = value };
        }
        catch (JsonException exception)
        {
            var error = BuildParameterError(exception);
            logger.LogWarning(
                "Could not bind DXAI function parameters as {ParameterType}: {ParameterError}",
                typeof(T).FullName,
                error);
            return Failed<T>(error);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not bind DXAI function parameters as {ParameterType}.", typeof(T).FullName);
            return Failed<T>("The function parameters could not be bound to the required parameter type.");
        }
    }

    public DxAiFunctionInvocationResult InvalidParameters(string error)
    {
    try
    {
            var message = string.IsNullOrWhiteSpace(error)
                ? "The function parameters are invalid."
                : error.Trim();
            logger.LogInformation("Created an invalid DXAI parameter result; parameter values were omitted.");
            return new DxAiFunctionInvocationResult
            {
                Succeeded = false,
                Status = "InvalidParameters",
                Error = message
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionJsonService)}.{nameof(InvalidParameters)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionJsonService)}.{nameof(InvalidParameters)} failed.");
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

    private DxAiFunctionParameterBinding<T> Failed<T>(string error) where T : new() =>
        new()
        {
            Succeeded = false,
            Value = new T(),
            Error = error
        };

    private string BuildParameterError(JsonException exception)
    {
    try
    {
            var path = exception.Path?.Trim();
            if (!string.IsNullOrWhiteSpace(path))
            {
                var parameterName = path.TrimStart('$', '.');
                if (!string.IsNullOrWhiteSpace(parameterName))
                    return $"Parameter '{parameterName}' has an invalid value or type.";
            }

            return "The function parameters contain invalid JSON values or types.";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionJsonService)}.{nameof(BuildParameterError)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionJsonService)}.{nameof(BuildParameterError)} failed.");
        throw;
    }
}
}
