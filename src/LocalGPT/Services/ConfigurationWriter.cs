using LocalGPT.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LocalGPT.Services
{
    /// <summary>
    /// Represents a configuration application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="jsonText">Json text service dependency used by the configuration workflow to provide the corresponding application capability.</param>
    public sealed class ConfigurationWriter(ILogger<ConfigurationWriter> logger, IJsonTextService jsonText) : IConfigurationWriter
    {
        /// <summary>
        /// Performs save for <see cref="ConfigurationWriter"/>, keeping the operation consistent with the state and invariants of the surrounding configuration workflow.
        /// </summary>
        /// <param name="root">Root value supplied to the configuration operation and used when producing its result.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
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
                    var readStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    await using var configuredReadStreamAsyncDisposal = readStream.ConfigureAwait(false);
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
                await File.WriteAllTextAsync(tempFile, jsonText.SerializeNode(settings, serializerOptions), ct).ConfigureAwait(false);
                File.Move(tempFile, file, overwrite: true);
                logger.LogInformation("Saved durable LocalGPT user configuration to {ConfigurationFile}.", file);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not save LocalGPT user configuration.");
                throw;
            }
        }

        /// <summary>
        /// Sets section for <see cref="ConfigurationWriter"/>, keeping the operation consistent with the state and invariants of the surrounding configuration workflow.
        /// </summary>
        /// <typeparam name="T">Type used for t values handled by <see cref="ConfigurationWriter"/>.</typeparam>
        /// <param name="settings">Settings containing the caller-supplied values that control this operation.</param>
        /// <param name="sectionName">Section name value supplied to the configuration operation and used when producing its result.</param>
        /// <param name="value">Value value supplied to the configuration operation and used when producing its result.</param>
        /// <param name="serializerOptions">Serializer options value supplied to the configuration operation and used when producing its result.</param>
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
