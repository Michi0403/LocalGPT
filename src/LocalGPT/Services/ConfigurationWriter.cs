using LocalGPT.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LocalGPT.Services
{
    public sealed class ConfigurationWriter(ILogger<ConfigurationWriter> logger) : IConfigurationWriter
    {
        public async Task SaveAsync(BusinessObjects.ConfigurationRoot root, CancellationToken ct = default)
        {
            try
            {
                var directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LocalGPT");
                Directory.CreateDirectory(directory);
                var file = Path.Combine(directory, "appsettings.user.json");
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

                SetSection(settings, nameof(root.LoggingCore), root.LoggingCore, serializerOptions);
                SetSection(settings, nameof(root.PythonCore), root.PythonCore, serializerOptions);
                SetSection(settings, nameof(root.ConnectionStringsCore), root.ConnectionStringsCore, serializerOptions);
                SetSection(settings, nameof(root.AICore), root.AICore, serializerOptions);
                SetSection(settings, nameof(root.LocalGPT), root.LocalGPT, serializerOptions);

                var tempFile = file + ".tmp";
                await File.WriteAllTextAsync(tempFile, settings.ToJsonString(serializerOptions), ct).ConfigureAwait(false);
                File.Move(tempFile, file, overwrite: true);
                logger.LogInformation("Saved durable LocalGPT user configuration to {ConfigurationFile}.", file);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not save LocalGPT user configuration.");
                throw;
            }
        }

        private void SetSection<T>(
            JsonObject settings,
            string sectionName,
            T? value,
            JsonSerializerOptions serializerOptions)
        {
            if (value is null)
                return;

            settings[sectionName] = JsonSerializer.SerializeToNode(value, serializerOptions);
        }
    }
}
