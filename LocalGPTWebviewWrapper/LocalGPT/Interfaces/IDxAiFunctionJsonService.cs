using LocalGPT.BusinessObjects;
using System.Text.Json;

namespace LocalGPT.Interfaces;

public interface IDxAiFunctionJsonService
{
    JsonSerializerOptions Options { get; }
    T Deserialize<T>(JsonElement element) where T : new();
    DxAiFunctionInvocationResult Success(object? value = null, string status = "Completed");
}
