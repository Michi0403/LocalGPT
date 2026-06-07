using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalGPT.Extensions
{
    public static class StringExtensions
    {
        private static JsonSerializerOptions jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            PropertyNamingPolicy = null,
            IgnoreReadOnlyFields = false,
            IgnoreReadOnlyProperties = false,
            IncludeFields = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowTrailingCommas = true,
            Converters = {
        new JsonStringEnumConverter()
    },
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString

        };
        public static string TrimEndpoint(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                return "unknown endpoint";

            return endpoint
                .Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
                .TrimEnd('/');
        }
        public static string ToJsonString(this object obj, JsonSerializerOptions? jsonOptions = null)
        {
            try
            {
                if (jsonOptions == null)
                {

                    return JsonSerializer.Serialize(obj, jsonSerializerOptions);
                }
                else
                {
                    return JsonSerializer.Serialize(obj, jsonOptions);
                }

            }
            catch (Exception ex)
            {


                return $"Serialization failed: {ex.Message}";
            }
        }
    }
}