using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Routes upload evidence through LocalGPT-native processors first and connected PublisherStudio capabilities only where required.</summary>
/// <param name="peers">One wire peer registry dependency used by the upload file processing capability workflow to provide the corresponding application capability.</param>
/// <param name="connections">One wire connection registry dependency used by the upload file processing capability workflow to provide the corresponding application capability.</param>
/// <param name="catalog">Local gpt catalog service dependency used by the upload file processing capability workflow to provide the corresponding application capability.</param>
/// <param name="localAiRuntime">Local ai runtime service dependency used by the upload file processing capability workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class UploadFileProcessingCapabilityService(
    IOneWirePeerRegistry peers,
    IOneWireConnectionRegistry connections,
    LocalGptCatalogService catalog,
    ILocalAiRuntimeService localAiRuntime,
    ILogger<UploadFileProcessingCapabilityService> logger) : IUploadFileProcessingCapabilityService
{
    // Compatibility fallbacks are used only with older PublisherStudio peers that advertise the capability key
    // but predate the format-family metadata extension. Current peers are routed from their advertised contract.
    /// <summary>
    /// Stores the in-memory publisher document compatibility extensions collection maintained internally by <see cref="UploadFileProcessingCapabilityService"/> for its current workflow state.
    /// </summary>
    private readonly HashSet<string> publisherDocumentCompatibilityExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".docx", ".xlsx", ".xlsm", ".xls"
    };

    /// <summary>
    /// Stores the in-memory publisher media compatibility extensions collection maintained internally by <see cref="UploadFileProcessingCapabilityService"/> for its current workflow state.
    /// </summary>
    private readonly HashSet<string> publisherMediaCompatibilityExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".m4v", ".webm", ".ogv", ".ogg", ".mov", ".mkv", ".avi", ".mxf", ".mts", ".m2ts", ".vob",
        ".mp3", ".m4a", ".aac", ".wav", ".flac", ".oga", ".opus"
    };

    /// <summary>
    /// Stores the in-memory local speech extensions collection maintained internally by <see cref="UploadFileProcessingCapabilityService"/> for its current workflow state.
    /// </summary>
    private readonly HashSet<string> localSpeechExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wav", ".mp3", ".m4a", ".aac", ".flac", ".ogg", ".oga", ".opus", ".webm", ".mp4", ".m4v", ".mov", ".mkv"
    };

    /// <summary>
    /// Stores the in-memory local image extensions collection maintained internally by <see cref="UploadFileProcessingCapabilityService"/> for its current workflow state.
    /// </summary>
    private readonly HashSet<string> localImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp", ".tif", ".tiff"
    };

    /// <summary>
    /// Performs evaluate as part of the upload file processing capability service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public IReadOnlyList<UploadFileProcessingRoute> Evaluate(IReadOnlyList<ProjectIngestionFileEvidence> files)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(files);
            var publisherPeers = peers.GetPeers()
                .Where(peer => connections.IsConnected(peer.PeerId)
                    && (peer.Application.Contains("PublisherStudio", StringComparison.OrdinalIgnoreCase)
                        || peer.Application.Contains("BlazorPublisher", StringComparison.OrdinalIgnoreCase)
                        || peer.PeerId.Contains("publisher", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            var formatCapabilities = publisherPeers
                .Select(peer => FindOnlineCapability(peer, "publisher.file.formats"))
                .Where(capability => capability is not null)
                .Cast<OneWireCapabilityDescriptor>()
                .ToList();
            var mediaRuntimeAvailable = publisherPeers.Any(peer =>
                FindOnlineCapability(peer, "publisher.file.formats") is not null
                && FindOnlineCapability(peer, "publisher.media.capabilities") is not null);

            var routes = files.Select(file => Route(file, formatCapabilities, mediaRuntimeAvailable)).ToList();
            logger.LogDebug(
                "Evaluated {RouteCount} upload processor route(s); publisher format contracts={PublisherContractCount}; publisher media={PublisherMedia}.",
                routes.Count,
                formatCapabilities.Count,
                mediaRuntimeAvailable);
            return routes;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Evaluating upload processor capabilities failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs route as part of the upload file processing capability service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="file">File value supplied to the upload file processing capability operation and used when producing its result.</param>
    /// <param name="publisherFormatCapabilities">One wire capability descriptor dependency used by the upload file processing capability workflow to provide the corresponding application capability.</param>
    /// <param name="publisherMediaRuntimeAvailable">Value indicating whether publisher media runtime available should apply to this operation.</param>
    /// <returns>The upload file processing route produced by the operation.</returns>
    private UploadFileProcessingRoute Route(
        ProjectIngestionFileEvidence file,
        IReadOnlyList<OneWireCapabilityDescriptor> publisherFormatCapabilities,
        bool publisherMediaRuntimeAvailable)
    {
        try
        {
            var extension = Path.GetExtension(file.RelativePath);
            if (catalog.TextExtensions.Contains(extension) || catalog.SourceExtensions.Contains(extension))
                return Available(file, "LocalGPT", "localgpt.native.text", "LocalGPT can process this text/source format directly.");
            if (file.IsArchive && extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                return Available(file, "LocalGPT", "localgpt.native.archive", "LocalGPT can inspect this bounded ZIP/source archive directly.");
            if (localImageExtensions.Contains(extension))
                return Available(file, "LocalGPT", "localgpt.vision.ocr", "LocalGPT can inspect the image directly and can use the local vision OCR workflow when an OCR model is available.");
            if (localSpeechExtensions.Contains(extension) && localAiRuntime.GetInstalledModels().Any(model => model.Capabilities.Contains(LocalAiCapability.SpeechRecognition)))
                return Available(file, "LocalGPT", "localai.audio.transcribe.workspace", "An installed LocalGPT speech-recognition model can transcribe this admitted audio/media file locally; managed Python/OpenAI Whisper is preferred when available, while compatible Hugging Face ASR models are also supported.");

            var publisherRoute = ReadPublisherFormatRoute(extension, publisherFormatCapabilities, publisherMediaRuntimeAvailable);
            if (publisherRoute is not null)
            {
                return publisherRoute.Value.Available
                    ? Available(file, "PublisherStudio", publisherRoute.Value.CapabilityKey, publisherRoute.Value.Reason)
                    : Unavailable(file, "PublisherStudio", publisherRoute.Value.CapabilityKey, publisherRoute.Value.Reason);
            }

            if (publisherDocumentCompatibilityExtensions.Contains(extension))
            {
                return publisherFormatCapabilities.Count > 0
                    ? Available(file, "PublisherStudio", "publisher.file.formats", "A connected legacy PublisherStudio advertises its maintained file-format capability; exact family metadata is not available from that peer version.")
                    : Unavailable(file, "PublisherStudio", "publisher.file.formats", "This Office format requires a connected PublisherStudio advertising publisher.file.formats.");
            }
            if (publisherMediaCompatibilityExtensions.Contains(extension))
            {
                return publisherFormatCapabilities.Count > 0 && publisherMediaRuntimeAvailable
                    ? Available(file, "PublisherStudio", "publisher.media.capabilities", "A connected legacy PublisherStudio advertises file formats and an online media runtime; exact family metadata is not available from that peer version.")
                    : Unavailable(file, "PublisherStudio", "publisher.media.capabilities", "This media format requires a connected PublisherStudio with publisher.file.formats and an online publisher.media.capabilities runtime.");
            }

            return Unavailable(file, "QuarantineOnly", string.Empty, "No low-hassle LocalGPT-native or connected PublisherStudio processing route is currently advertised for this file type.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Routing quarantined upload file {RelativePath} failed.", file.RelativePath);
            throw;
        }
    }

    /// <summary>
    /// Reads publisher format route as part of the upload file processing capability service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="extension">Extension value supplied to the upload file processing capability operation and used when producing its result.</param>
    /// <param name="capabilities">One wire capability descriptor dependency used by the upload file processing capability workflow to provide the corresponding application capability.</param>
    /// <param name="publisherMediaRuntimeAvailable">Value indicating whether publisher media runtime available should apply to this operation.</param>
    /// <returns>The bool available string capability key string reason produced by the operation.</returns>
    private (bool Available, string CapabilityKey, string Reason)? ReadPublisherFormatRoute(
        string extension,
        IReadOnlyList<OneWireCapabilityDescriptor> capabilities,
        bool publisherMediaRuntimeAvailable)
    {
        try
        {
            foreach (var capability in capabilities)
            {
                if (string.IsNullOrWhiteSpace(capability.ParameterSchemaJson))
                    continue;
                try
                {
                    using var document = JsonDocument.Parse(capability.ParameterSchemaJson);
                    if (!document.RootElement.TryGetProperty("x-publisher-format-families", out var families)
                        || families.ValueKind != JsonValueKind.Array)
                        continue;
                    foreach (var family in families.EnumerateArray())
                    {
                        if (!family.TryGetProperty("extensions", out var extensions) || extensions.ValueKind != JsonValueKind.Array)
                            continue;
                        var matches = extensions.EnumerateArray().Any(value => string.Equals(value.GetString(), extension, StringComparison.OrdinalIgnoreCase));
                        if (!matches)
                            continue;

                        var requiresMedia = family.TryGetProperty("requiresMediaRuntime", out var requiresElement) && requiresElement.ValueKind == JsonValueKind.True;
                        var runtimeAvailable = !requiresMedia
                            || (family.TryGetProperty("runtimeAvailable", out var runtimeElement) && runtimeElement.ValueKind == JsonValueKind.True && publisherMediaRuntimeAvailable);
                        var directImport = !family.TryGetProperty("directImportAvailable", out var directElement) || directElement.ValueKind == JsonValueKind.True;
                        var key = family.TryGetProperty("key", out var keyElement) ? keyElement.GetString() ?? string.Empty : string.Empty;
                        var label = family.TryGetProperty("displayName", out var labelElement) ? labelElement.GetString() ?? key : key;
                        if (!directImport)
                            return (false, "publisher.file.formats", $"PublisherStudio advertises {label}, but direct import is not currently available.");
                        if (requiresMedia && !runtimeAvailable)
                            return (false, "publisher.media.capabilities", $"PublisherStudio advertises {label}, but its required media runtime is offline.");
                        return (true, requiresMedia ? "publisher.media.capabilities" : "publisher.file.formats", $"Connected PublisherStudio advertises {label} support for {extension} through its live 1-Wire format contract.");
                    }
                }
                catch (JsonException exception)
                {
                    logger.LogDebug(exception, "Ignored malformed PublisherStudio format metadata from one connected peer.");
                }
            }
            return null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading PublisherStudio file-format contract for extension {Extension} failed.", extension);
            throw;
        }
    }

    /// <summary>
    /// Finds online capability as part of the upload file processing capability service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="peer">Peer value supplied to the upload file processing capability operation and used when producing its result.</param>
    /// <param name="key">Key value supplied to the upload file processing capability operation and used when producing its result.</param>
    /// <returns>The one wire capability descriptor produced by the operation.</returns>
    private OneWireCapabilityDescriptor? FindOnlineCapability(OneWirePeerAdvertisement peer, string key)
    {
        try
        {
            return peer.Capabilities.FirstOrDefault(capability =>
                capability.IsEnabled
                && capability.IsOnline
                && string.Equals(capability.Key, key, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading online 1-Wire capability {CapabilityKey} from peer {PeerId} failed.", key, peer.PeerId);
            throw;
        }
    }

    /// <summary>
    /// Performs available as part of the upload file processing capability service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="file">File value supplied to the upload file processing capability operation and used when producing its result.</param>
    /// <param name="processor">Processor value supplied to the upload file processing capability operation and used when producing its result.</param>
    /// <param name="capability">Capability value supplied to the upload file processing capability operation and used when producing its result.</param>
    /// <param name="reason">Reason value supplied to the upload file processing capability operation and used when producing its result.</param>
    /// <returns>The upload file processing route produced by the operation.</returns>
    private UploadFileProcessingRoute Available(ProjectIngestionFileEvidence file, string processor, string capability, string reason)
    {
        try
        {
            return new UploadFileProcessingRoute
            {
                RelativePath = file.RelativePath,
                Processor = processor,
                CapabilityKey = capability,
                IsAvailable = true,
                Reason = reason
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building an available upload-processing route failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs unavailable as part of the upload file processing capability service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="file">File value supplied to the upload file processing capability operation and used when producing its result.</param>
    /// <param name="processor">Processor value supplied to the upload file processing capability operation and used when producing its result.</param>
    /// <param name="capability">Capability value supplied to the upload file processing capability operation and used when producing its result.</param>
    /// <param name="reason">Reason value supplied to the upload file processing capability operation and used when producing its result.</param>
    /// <returns>The upload file processing route produced by the operation.</returns>
    private UploadFileProcessingRoute Unavailable(ProjectIngestionFileEvidence file, string processor, string capability, string reason)
    {
        try
        {
            return new UploadFileProcessingRoute
            {
                RelativePath = file.RelativePath,
                Processor = processor,
                CapabilityKey = capability,
                IsAvailable = false,
                Reason = reason
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building an unavailable upload-processing route failed.");
            throw;
        }
    }
}
