using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services
{

    public class ConfigurationWriter : IConfigurationWriter
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _cfg;
        private readonly ILogger<ConfigurationWriter> _logger;

        public ConfigurationWriter(IWebHostEnvironment env, IConfiguration cfg, ILogger<ConfigurationWriter> logger)
        {
            _env = env; _cfg = cfg; _logger = logger;
        }

        public async Task SaveAsync(BusinessObjects.ConfigurationRoot root, CancellationToken ct = default)
        {
            try
            {
                var file = Path.Combine(_env.ContentRootPath, "appsettings.json");
                using var fs = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                using var doc = await JsonDocument.ParseAsync(fs, cancellationToken: ct);
                fs.Seek(0, SeekOrigin.Begin);

                // Merge: simplest approach is rewrite with our object (you can replace with a deep-merge if you prefer)
                var newJson = JsonSerializer.Serialize(new { Configuration = root }, new JsonSerializerOptions { WriteIndented = true });
                await using var sw = new StreamWriter(fs);
                sw.Write(newJson);
                sw.Flush();
                fs.SetLength(fs.Position);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in ConfigurationWriterSaveAsync {ex.ToString()}");
                throw;
            }

        }
    }
}
