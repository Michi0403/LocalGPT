using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalGPT.Extensions
{
    /// <summary>
    /// Represents a string extensions application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public static class StringExtensions
    {

        /// <summary>
        /// Performs to JSON string for <see cref="StringExtensions"/>, keeping the operation consistent with the state and invariants of the surrounding string extensions workflow.
        /// </summary>
        /// <param name="obj">Obj value supplied to the string extensions operation and used when producing its result.</param>
        /// <param name="jsonOptions">Json options value supplied to the string extensions operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
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
