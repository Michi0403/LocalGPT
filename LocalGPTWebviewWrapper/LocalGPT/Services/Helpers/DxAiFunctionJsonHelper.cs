using System.Text.Json;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services.Helpers;

/// <summary>
/// Pure JSON/result helpers shared by DI-backed DXAI function handlers.
/// This helper owns no mutable runtime state.
/// </summary>
internal static class DxAiFunctionJsonHelper
{
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    internal static T Deserialize<T>(JsonElement element) where T : new() =>
        element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? new T()
            : element.Deserialize<T>(Options) ?? new T();

    internal static DxAiFunctionInvocationResult Success(object? value = null, string status = "Completed") => new()
    {
        Succeeded = true,
        Status = status,
        Value = value
    };
}
