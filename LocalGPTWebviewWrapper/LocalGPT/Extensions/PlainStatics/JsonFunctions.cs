using System.Text.Json;
using System.Text.Json.Nodes;

namespace LocalGPT.Extensions.PlainStatics
{
    public static class JsonFunctions
    {
        public static void SetSection<T>(JsonObject settings, string sectionName, T? value, JsonSerializerOptions serializerOptions, ILogger logger)
        {
            try
            {
                if (value is null)
                    return;

                settings[sectionName] = JsonSerializer.SerializeToNode(value, serializerOptions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigurationWriter.SaveAsync {ex.ToString()}");
            }

        }
    }
}
