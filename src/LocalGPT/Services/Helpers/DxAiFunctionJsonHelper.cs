using System.Reflection;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Helpers;

/// <summary>
/// Coordinates DevExpress AI function JSON behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
internal sealed class DxAiFunctionJsonService(ILogger<DxAiFunctionJsonService> logger) : IDxAiFunctionJsonService
{
    /// <summary>
    /// Gets the options value that forms part of the DevExpress AI function JSON state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The options value exposed by <see cref="DxAiFunctionJsonService"/>.</value>
    public JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Performs deserialize as part of the DevExpress AI function JSON service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="DxAiFunctionJsonService"/>.</typeparam>
    /// <param name="element">Element value supplied to the DevExpress AI function JSON operation and used when producing its result.</param>
    /// <returns>The t produced by the operation.</returns>
    public T Deserialize<T>(JsonElement element) where T : new()
    {
        var binding = Bind<T>(element);
        if (binding.Succeeded)
            return binding.Value;

        throw new JsonException(binding.Error);
    }

    /// <summary>
    /// Performs bind as part of the DevExpress AI function JSON service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="DxAiFunctionJsonService"/>.</typeparam>
    /// <param name="element">Element value supplied to the DevExpress AI function JSON operation and used when producing its result.</param>
    /// <returns>The DevExpress AI function parameter binding t produced by the operation.</returns>
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

    /// <summary>
    /// Performs invalid parameters as part of the DevExpress AI function JSON service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="error">Error value supplied to the DevExpress AI function JSON operation and used when producing its result.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
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

    /// <summary>
    /// Performs success as part of the DevExpress AI function JSON service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the DevExpress AI function JSON operation and used when producing its result.</param>
    /// <param name="status">Status value supplied to the DevExpress AI function JSON operation and used when producing its result.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
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

    /// <summary>
    /// Runs the failed operation.
    /// </summary>
    private DxAiFunctionParameterBinding<T> Failed<T>(string error) where T : new() =>
        /// <summary>
        /// Gets the new value that forms part of the DevExpress AI function JSON state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The new value exposed by <see cref="DxAiFunctionJsonService"/>.</value>
        new()
        {
            Succeeded = false,
            Value = new T(),
            Error = error
        };

    /// <summary>
    /// Builds parameter error as part of the DevExpress AI function JSON service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="exception">Exception value supplied to the DevExpress AI function JSON operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
