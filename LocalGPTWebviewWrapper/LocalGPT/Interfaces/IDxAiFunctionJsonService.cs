using LocalGPT.BusinessObjects;
using System.Text.Json;

namespace LocalGPT.Interfaces;

public interface IDxAiFunctionJsonService
{
    JsonSerializerOptions Options { get; }
    T Deserialize<T>(JsonElement element) where T : new();
    DxAiFunctionParameterBinding<T> Bind<T>(JsonElement element) where T : new();
    DxAiFunctionInvocationResult InvalidParameters(string error);
    DxAiFunctionInvocationResult Success(object? value = null, string status = "Completed");
}
