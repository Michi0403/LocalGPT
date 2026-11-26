using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;
using Microsoft.Extensions.Options;

namespace LocalGPT.Services.OneWire;

/// <summary>Relays verified generation results to the peer that requested the Council session.</summary>
public sealed class PublisherMediaDeliveryService(ILocalAiArtifactService artifacts, IOptionsMonitor<OneWireOptions> options,
    IServiceScopeFactory scopeFactory, ILocalGptVocabularyService vocabulary, ILogger<PublisherMediaDeliveryService> logger)
{
    internal const string CapabilityKey = "localgpt.media.delivery.read";
    private readonly object gate = new();
    private readonly Dictionary<Guid, PublisherMediaDeliveryRegistration> requests = new();
    private readonly Dictionary<Guid, Guid> runs = new();

    internal void Register(Guid correlationId, string peerId, string projectContextJson)
    {
        if (correlationId == Guid.Empty || string.IsNullOrWhiteSpace(peerId) || string.IsNullOrWhiteSpace(projectContextJson))
            return;

        string? kind;
        try
        {
            using var context = JsonDocument.Parse(projectContextJson);
            kind = context.RootElement.ValueKind == JsonValueKind.Object &&
                context.RootElement.TryGetProperty("publisherMediaKind", out var element) && element.ValueKind == JsonValueKind.String
                ? element.GetString() : null;
        }
        catch (JsonException error)
        {
            logger.LogDebug(error, "Ignored invalid PublisherStudio media context for Council {CorrelationId}.", correlationId);
            return;
        }

        var contentType = kind switch { "picture" => "image/png", "video" => "video/mp4", _ => null };
        if (contentType is null) return;

        lock (gate)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var expired in requests.Where(pair => pair.Value.ExpiresUtc <= now).Select(pair => pair.Key).ToArray())
                requests.Remove(expired);
            foreach (var expired in runs.Where(pair => !requests.ContainsKey(pair.Value)).Select(pair => pair.Key).ToArray())
                runs.Remove(expired);

            requests.TryAdd(correlationId, new PublisherMediaDeliveryRegistration(peerId, contentType,
                now.AddMinutes(Math.Max(1, options.CurrentValue.PublisherMediaDeliveryMinutes))));
        }
    }

    internal void TrackRun(string correlationId, Guid runId)
    {
        try
        {
            if (!Guid.TryParse(correlationId, out var id)) return;
            lock (gate)
            {
                if (requests.ContainsKey(id)) runs.TryAdd(runId, id);
            }
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "Could not associate PublisherStudio media with Council run {RunId}.", runId);
        }
    }

    internal void CompleteCouncil(Guid correlationId)
    {
        lock (gate)
        {
            if (requests.TryGetValue(correlationId, out var delivery))
                delivery.CouncilFinished = true;
        }
    }

    internal void Record(Guid? runId, DxAiFunctionInvocationResult result)
    {
        if (runId is not Guid id || !result.Succeeded ||
            result.Value is not LocalAiRuntimeJobResult { Succeeded: true } job)
            return;

        var contentType = GetGenerationContentType(result.FunctionName);
        if (contentType is null) return;

        try
        {
            var artifact = artifacts.Resolve(job.ArtifactId, job.ArtifactFileName);
            if (artifact?.ContentType != contentType || artifact.Length <= 0) return;
            lock (gate)
            {
                if (runs.TryGetValue(id, out var correlationId) && requests.TryGetValue(correlationId, out var delivery) &&
                    delivery.ExpiresUtc > DateTimeOffset.UtcNow && delivery.ContentType == contentType && delivery.Artifact is null)
                    delivery.Artifact = artifact;
            }
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "Could not associate a generated asset with Council run {RunId}.", runId);
        }
    }

    internal async Task<PublisherMediaReadResult> ReadAsync(string peerId, PublisherMediaReadRequest request, CancellationToken cancellationToken)
    {
        LocalAiArtifactDescriptor? artifact;
        string contentType;
        Guid[] completedRunIds;
        lock (gate)
        {
            if (!requests.TryGetValue(request.CouncilCorrelationId, out var delivery) ||
                delivery.ExpiresUtc <= DateTimeOffset.UtcNow || !string.Equals(delivery.PeerId, peerId, StringComparison.OrdinalIgnoreCase))
                return new("Unavailable");
            artifact = delivery.Artifact;
            contentType = delivery.ContentType;
            if (artifact is null && !delivery.CouncilFinished)
                return new("Waiting");
            completedRunIds = artifact is null
                ? runs.Where(pair => pair.Value == request.CouncilCorrelationId).Select(pair => pair.Key).ToArray()
                : [];
        }

        if (artifact is null)
        {
            var scope = scopeFactory.CreateAsyncScope();
            await using var configuredScope = scope.ConfigureAwait(false);
            var deferred = scope.ServiceProvider.GetRequiredService<IDeferredDxAiInvocationService>();
            var statuses = vocabulary.Get();
            foreach (var runId in completedRunIds)
            {
                var invocations = await deferred.GetForRunAsync(runId, cancellationToken).ConfigureAwait(false);
                if (invocations.Any(item => GetGenerationContentType(item.FunctionName) == contentType &&
                    (item.Status == statuses.DeferredPendingApproval || item.Status == statuses.DeferredExecuting)))
                    return new("Waiting");
            }
            lock (gate)
            {
                if (!requests.TryGetValue(request.CouncilCorrelationId, out var delivery) ||
                    delivery.ExpiresUtc <= DateTimeOffset.UtcNow || !string.Equals(delivery.PeerId, peerId, StringComparison.OrdinalIgnoreCase))
                    return new("Unavailable");
                artifact = delivery.Artifact;
            }
            if (artifact is null) return new("CompletedWithoutFile");
        }
        if (request.Offset < 0 || request.Offset >= artifact.Length ||
            (request.Offset != 0 || !string.IsNullOrEmpty(request.ArtifactId)) && request.ArtifactId != artifact.ArtifactId)
            return new("Unavailable");

        // Leave space for metadata, base64 expansion, and the encrypted envelope.
        var wireLimit = Math.Min(options.CurrentValue.MaximumMessageBytes, OneWireProtocol.MaximumMessageBytes);
        var chunkLimit = (wireLimit - 2048) / 4;
        if (chunkLimit <= 0 || request.Count <= 0) return new("Unavailable");
        var count = (int)Math.Min(Math.Min(request.Count, chunkLimit), artifact.Length - request.Offset);
        var data = new byte[count];
        try
        {
            var file = new FileStream(artifact.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            await using (file.ConfigureAwait(false))
            {
                if (file.Length != artifact.Length) return new("Unavailable");
                file.Position = request.Offset;
                await file.ReadExactlyAsync(data, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (IOException error)
        {
            logger.LogDebug(error, "Generated artifact {ArtifactId} is no longer readable.", artifact.ArtifactId);
            return new("Unavailable");
        }

        return new("Ready", artifact.ArtifactId, artifact.FileName, artifact.ContentType, artifact.Length, request.Offset, data);
    }

    private string? GetGenerationContentType(string functionName) => functionName switch
    {
        "localai.image.generate" or "localai.image.edit.workspace" => "image/png",
        "localai.video.generate" or "localai.video.from-image.workspace" => "video/mp4",
        _ => null
    };
}
