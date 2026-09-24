using System.Security.Cryptography;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Owns LocalGPT's reviewed direct/GitHub-first local-AI source catalog and delegates executable installation to the managed Python runtime.</summary>
public sealed class LocalAiAcquisitionService(
    IHttpClientFactory httpClientFactory,
    ILocalAiRuntimeService runtime,
    ILogger<LocalAiAcquisitionService> logger) : ILocalAiAcquisitionService
{
    private const long MaximumSourceBytes = 2_147_483_648;

    /// <inheritdoc />
    public IReadOnlyList<LocalAiKnownModelDefinition> GetKnownModels()
    {
        try
        {
            return BuildCatalog()
                .OrderByDescending(item => item.CanInstall)
                .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading the reviewed direct local-AI source catalog failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<LocalAiSourceDownloadResult> DownloadSourceAsync(string sourceKey, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Explicit human confirmation is required before downloading local-AI source code.");
            var source = RequireSource(sourceKey);
            if (!Uri.TryCreate(source.SourceArchiveUri, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("The reviewed local-AI source does not use HTTPS.");

            var sourceRoot = LocalGptApplicationDataPaths.ResolveUserPath("LocalAiRuntime", "Sources", source.Key);
            Directory.CreateDirectory(sourceRoot);
            var finalPath = Path.Combine(sourceRoot, source.SourceFileName);
            var temporaryPath = finalPath + ".part-" + Guid.NewGuid().ToString("N");
            try
            {
                using var client = httpClientFactory.CreateClient("LocalGPTLocalAiSources");
                using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentLength is > MaximumSourceBytes)
                    throw new InvalidOperationException("The local-AI source archive exceeds LocalGPT's 2 GiB direct-download limit.");

                var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                await using var configuredInputAsyncDisposal = input.ConfigureAwait(false);
                var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
                await using var configuredOutputAsyncDisposal = output.ConfigureAwait(false);
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                var buffer = new byte[128 * 1024];
                long total = 0;
                while (true)
                {
                    var read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                        break;
                    total += read;
                    if (total > MaximumSourceBytes)
                        throw new InvalidOperationException("The local-AI source archive exceeded LocalGPT's 2 GiB direct-download limit while streaming.");
                    hash.AppendData(buffer, 0, read);
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }
                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
                File.Move(temporaryPath, finalPath, overwrite: true);
                var digest = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
                logger.LogInformation("Downloaded reviewed local-AI source {SourceKey} with {Bytes} bytes; URL and local path were omitted from logs.", source.Key, total);
                return new LocalAiSourceDownloadResult
                {
                    SourceKey = source.Key,
                    LocalPath = finalPath,
                    Bytes = total,
                    Sha256 = digest,
                    DownloadedAtUtc = DateTime.UtcNow
                };
            }
            finally
            {
                try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); } catch { }
            }
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Downloading a reviewed local-AI source archive was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Downloading a reviewed local-AI source archive failed; source identity, URL and local path were omitted from logs.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<LocalAiModelInstallation> InstallKnownModelAsync(LocalAiKnownModelInstallRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.UserConfirmed)
                throw new InvalidOperationException("Explicit human confirmation is required before downloading and installing a known local-AI model.");
            var source = RequireSource(request.SourceKey);
            if (!source.CanInstall)
                throw new NotSupportedException("This reviewed project is available for direct source download, but this LocalGPT release does not yet expose an executable runtime adapter for it.");
            var variant = source.Variants.FirstOrDefault(item => item.Key.Equals(request.Variant?.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException("Select one reviewed model variant from the catalog entry.", nameof(request));
            var archive = await DownloadSourceAsync(source.Key, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            if (source.Adapter.Equals("openai-whisper", StringComparison.OrdinalIgnoreCase))
                return await runtime.InstallOpenAiWhisperAsync(archive.LocalPath, variant.Key, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            throw new NotSupportedException("The selected direct local-AI project does not have an executable installer adapter in this release.");
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Installing a reviewed direct local-AI model was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Installing a reviewed direct local-AI model failed; model identity, URL and local paths were omitted from logs.");
            throw;
        }
    }

    private LocalAiKnownModelDefinition RequireSource(string sourceKey)
    {
        try
        {
            return BuildCatalog().FirstOrDefault(item => item.Key.Equals(sourceKey?.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new KeyNotFoundException("The requested local-AI source is not in LocalGPT's reviewed direct-download catalog.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving a reviewed local-AI source failed; source identity was omitted from logs.");
            throw;
        }
    }

    private List<LocalAiKnownModelDefinition> BuildCatalog()
    {
        try
        {
            return
            [
                new LocalAiKnownModelDefinition
                {
                    Key = "openai-whisper",
                    DisplayName = "OpenAI Whisper",
                    Publisher = "OpenAI",
                    Description = "Speech recognition installed from the upstream GitHub source archive into LocalGPT's managed Python environment. Model weights are then obtained by the Whisper Python package itself, without Hugging Face Hub.",
                    RepositoryUri = "https://github.com/openai/whisper",
                    SourceArchiveUri = "https://github.com/openai/whisper/archive/refs/heads/main.zip",
                    SourceFileName = "openai-whisper-main.zip",
                    Adapter = "openai-whisper",
                    Capabilities = [LocalAiCapability.SpeechRecognition],
                    CanInstall = true,
                    RuntimePrerequisites = "A working Python binding and managed environment are required. FFmpeg must be available to the LocalGPT process for audio decoding.",
                    Variants =
                    [
                        new() { Key = "tiny", DisplayName = "tiny", Hint = "Smallest/faster model for constrained hardware." },
                        new() { Key = "base", DisplayName = "base", Hint = "Compact general-purpose speech model." },
                        new() { Key = "small", DisplayName = "small", Hint = "Higher accuracy with more memory/compute." },
                        new() { Key = "medium", DisplayName = "medium", Hint = "Large general-purpose model; substantial RAM/VRAM use." },
                        new() { Key = "large-v3", DisplayName = "large-v3", Hint = "High-accuracy large model; substantial download and compute requirements." },
                        new() { Key = "turbo", DisplayName = "turbo", Hint = "Optimized large-family variant where supported by the upstream package." }
                    ]
                },
                new LocalAiKnownModelDefinition
                {
                    Key = "openai-clip",
                    DisplayName = "OpenAI CLIP",
                    Publisher = "OpenAI",
                    Description = "Reviewed upstream vision-language source. Direct source download is available; executable ImageUnderstanding/Embeddings binding is intentionally not claimed until LocalGPT has a dedicated adapter.",
                    RepositoryUri = "https://github.com/openai/CLIP",
                    SourceArchiveUri = "https://github.com/openai/CLIP/archive/refs/heads/main.zip",
                    SourceFileName = "openai-clip-main.zip",
                    Adapter = "openai-clip",
                    Capabilities = [LocalAiCapability.ImageUnderstanding, LocalAiCapability.Embeddings],
                    CanInstall = false,
                    RuntimePrerequisites = "Source download only in this release."
                },
                new LocalAiKnownModelDefinition
                {
                    Key = "segment-anything",
                    DisplayName = "Segment Anything",
                    Publisher = "Meta AI Research",
                    Description = "Reviewed upstream image-segmentation project available as a direct GitHub source archive. LocalGPT does not pretend it is executable until a bounded segmentation adapter is implemented.",
                    RepositoryUri = "https://github.com/facebookresearch/segment-anything",
                    SourceArchiveUri = "https://github.com/facebookresearch/segment-anything/archive/refs/heads/main.zip",
                    SourceFileName = "segment-anything-main.zip",
                    Adapter = "segment-anything",
                    Capabilities = [LocalAiCapability.ImageUnderstanding],
                    CanInstall = false,
                    RuntimePrerequisites = "Source download only in this release."
                },
                new LocalAiKnownModelDefinition
                {
                    Key = "real-esrgan",
                    DisplayName = "Real-ESRGAN",
                    Publisher = "Xintao Wang / contributors",
                    Description = "Reviewed upstream image-restoration project available as a direct GitHub source archive. It remains source-only until LocalGPT exposes a bounded restoration DXFunction adapter.",
                    RepositoryUri = "https://github.com/xinntao/Real-ESRGAN",
                    SourceArchiveUri = "https://github.com/xinntao/Real-ESRGAN/archive/refs/heads/master.zip",
                    SourceFileName = "real-esrgan-master.zip",
                    Adapter = "real-esrgan",
                    Capabilities = [LocalAiCapability.ImageEditing],
                    CanInstall = false,
                    RuntimePrerequisites = "Source download only in this release."
                }
            ];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Constructing the reviewed direct local-AI source catalog failed.");
            throw;
        }
    }
}
