using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalGPT.Extensions
{
    /// <summary>
    /// Represents a string extensions.
    /// </summary>
    public static class StringExtensions
    {

        /// <summary>
        /// Runs the to JSON string operation.
        /// </summary>
        public static string ToJsonString(this object obj, JsonSerializerOptions? jsonOptions = null)
        {
            try
            {
                if (jsonOptions == null)
                {
                    jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true,
                        PropertyNamingPolicy = null,
                        IgnoreReadOnlyFields = false,
                        IgnoreReadOnlyProperties = false,
                        IncludeFields = false,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        AllowTrailingCommas = true,
                        Converters = { new JsonStringEnumConverter() },
                        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
                    };

                    return JsonSerializer.Serialize(obj, jsonOptions);
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