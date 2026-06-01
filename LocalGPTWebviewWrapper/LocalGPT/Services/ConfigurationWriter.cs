using LocalGPT.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LocalGPT.Services
{
    public class ConfigurationWriter : IConfigurationWriter
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _cfg;
        private readonly ILogger<ConfigurationWriter> _logger;

        public ConfigurationWriter(IWebHostEnvironment env, IConfiguration cfg, ILogger<ConfigurationWriter> logger)
        {
            _env = env;
            _cfg = cfg;
            _logger = logger;
        }

        public async Task SaveAsync(BusinessObjects.ConfigurationRoot root, CancellationToken ct = default)
        {
            try
            {
                var file = Path.Combine(_env.ContentRootPath, "appsettings.json");
                var serializerOptions = new JsonSerializerOptions { WriteIndented = true };
                JsonObject settings;

                if (File.Exists(file))
                {
                    await using var readStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    settings = await JsonNode.ParseAsync(readStream, cancellationToken: ct) as JsonObject ?? new JsonObject();
                }
                else
                {
                    settings = new JsonObject();
                }

                SetSection(settings, nameof(root.LoggingCore), root.LoggingCore, serializerOptions);
                SetSection(settings, nameof(root.PythonCore), root.PythonCore, serializerOptions);
                SetSection(settings, nameof(root.ConnectionStringsCore), root.ConnectionStringsCore, serializerOptions);
                SetSection(settings, nameof(root.AICore), root.AICore, serializerOptions);

                var tempFile = file + ".tmp";
                await File.WriteAllTextAsync(tempFile, settings.ToJsonString(serializerOptions), ct);
                File.Copy(tempFile, file, overwrite: true);
                File.Delete(tempFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConfigurationWriter.SaveAsync {Message}", ex.Message);
                throw;
            }
        }

        private static void SetSection<T>(JsonObject settings, string sectionName, T? value, JsonSerializerOptions serializerOptions)
        {
            if (value is null)
                return;

            settings[sectionName] = JsonSerializer.SerializeToNode(value, serializerOptions);
        }
    }
}
