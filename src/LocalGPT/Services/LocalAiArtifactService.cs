using LocalGPT.BusinessObjects;
using LocalGptConfigurationRoot = LocalGPT.BusinessObjects.ConfigurationRoot;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;

namespace LocalGPT.Services;

/// <summary>Publishes generated local-AI media under a bounded per-user artifact root.</summary>
public sealed class LocalAiArtifactService(
    IOptionsMonitor<LocalGptConfigurationRoot> options,
    IPlatformRuntimeService platform,
    ILogger<LocalAiArtifactService> logger) : ILocalAiArtifactService
{
    public string ArtifactRoot => ResolveArtifactRoot();

    public async Task<LocalAiArtifactDescriptor> PublishAsync(string sourcePath, string preferredFileName, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Generated local-AI artifact was not found.", sourcePath);

            var root = ResolveArtifactRoot();
            Directory.CreateDirectory(root);
            var artifactId = Guid.NewGuid().ToString("N");
            var directory = Path.Combine(root, artifactId);
            Directory.CreateDirectory(directory);
            var safeFileName = SanitizeFileName(preferredFileName);
            var destination = Path.Combine(directory, safeFileName);
            var input = File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using (input.ConfigureAwait(false))
            {
                var output = File.Open(destination, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                await using (output.ConfigureAwait(false))
                    await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
            }

            var info = new FileInfo(destination);
            logger.LogInformation("Published local-AI artifact {ArtifactId} ({Length} bytes); generated content and filesystem paths were omitted from logs.", artifactId, info.Length);
            return new LocalAiArtifactDescriptor
            {
                ArtifactId = artifactId,
                FileName = safeFileName,
                ContentType = ContentTypeFor(safeFileName),
                FullPath = destination,
                DownloadUrl = $"/__artifacts/media/{artifactId}/{Uri.EscapeDataString(safeFileName)}",
                Length = info.Length,
                CreatedAtUtc = DateTime.UtcNow
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiArtifactService)}.{nameof(PublishAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiArtifactService)}.{nameof(PublishAsync)} failed.");
        throw;
    }
}

    public LocalAiArtifactDescriptor? Resolve(string artifactId, string fileName)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(artifactId) || artifactId.Length != 32 || artifactId.Any(ch => !Uri.IsHexDigit(ch)))
                return null;
            var root = ResolveArtifactRoot();
            var safeFileName = SanitizeFileName(fileName);
            var path = Path.GetFullPath(Path.Combine(root, artifactId, safeFileName));
            if (!platform.IsSameOrDescendantPath(root, path) || !File.Exists(path))
                return null;
            var info = new FileInfo(path);
            return new LocalAiArtifactDescriptor
            {
                ArtifactId = artifactId,
                FileName = safeFileName,
                ContentType = ContentTypeFor(safeFileName),
                FullPath = path,
                DownloadUrl = $"/__artifacts/media/{artifactId}/{Uri.EscapeDataString(safeFileName)}",
                Length = info.Length,
                CreatedAtUtc = info.CreationTimeUtc
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiArtifactService)}.{nameof(Resolve)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiArtifactService)}.{nameof(Resolve)} failed.");
        throw;
    }
}

    private string ResolveArtifactRoot()
    {
    try
    {
            var configured = options.CurrentValue.PythonCore?.ArtifactRoot;
            return string.IsNullOrWhiteSpace(configured)
                ? LocalGptApplicationDataPaths.ResolveUserPath("LocalAiRuntime", "Artifacts")
                : Path.GetFullPath(configured);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiArtifactService)}.{nameof(ResolveArtifactRoot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiArtifactService)}.{nameof(ResolveArtifactRoot)} failed.");
        throw;
    }
}

    private string SanitizeFileName(string fileName)
    {
    try
    {
            var name = Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? "artifact.bin" : fileName.Trim());
            foreach (var invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(name) ? "artifact.bin" : name;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiArtifactService)}.{nameof(SanitizeFileName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiArtifactService)}.{nameof(SanitizeFileName)} failed.");
        throw;
    }
}

    private string ContentTypeFor(string fileName) {
    try
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        ".mp4" => "video/mp4",
        ".webm" => "video/webm",
        ".wav" => "audio/wav",
        ".mp3" => "audio/mpeg",
        ".json" => "application/json",
        ".txt" => "text/plain; charset=utf-8",
        _ => "application/octet-stream"
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalAiArtifactService)}.{nameof(ContentTypeFor)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalAiArtifactService)}.{nameof(ContentTypeFor)} failed.");
        throw;
    }
}
}
