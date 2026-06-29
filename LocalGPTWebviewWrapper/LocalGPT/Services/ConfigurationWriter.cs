using LocalGPT.Extensions.PlainStatics;
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
                    settings = await JsonNode.ParseAsync(readStream, cancellationToken: ct).ConfigureAwait(false) as JsonObject ?? new JsonObject();
                }
                else
                {
                    settings = new JsonObject();
                }

                JsonFunctions.SetSection(settings, nameof(root.LoggingCore), root.LoggingCore, serializerOptions, _logger);
                JsonFunctions.SetSection(settings, nameof(root.PythonCore), root.PythonCore, serializerOptions, _logger);
                JsonFunctions.SetSection(settings, nameof(root.ConnectionStringsCore), root.ConnectionStringsCore, serializerOptions, _logger);
                JsonFunctions.SetSection(settings, nameof(root.AICore), root.AICore, serializerOptions,_logger);

                var tempFile = file + ".tmp";
                await File.WriteAllTextAsync(tempFile, settings.ToJsonString(serializerOptions), ct).ConfigureAwait(false);
                File.Copy(tempFile, file, overwrite: true);
                File.Delete(tempFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in ConfigurationWriter.SaveAsync {ex.ToString()} root {root.ToString()}");
                
            }
        }


    }
}
