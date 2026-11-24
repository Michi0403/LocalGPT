using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Reads large PublisherStudio files (website exports, converted video) in chunks after the user approved the request that produced them.
/// It only continues transfers announced in results of work LocalGPT itself sent, reads them from the announcing peer through the
/// advertised read-only <c>publisher.file.read</c> capability, and hands the assembled bytes to the spooler for SHA-256 and MIME verification.
/// </summary>
/// <param name="spooler">Outgoing work spooler that announces transfers and receives chunk results.</param>
/// <param name="connections">Live 1-Wire connections used to send chunk requests.</param>
/// <param name="peers">Peer registry that holds the advertised chunk capability.</param>
/// <param name="options">1-Wire settings; the chunk timeout and maximum file size are read from them.</param>
/// <param name="logger">Structured diagnostics for transfers.</param>
public sealed class PublisherFileTransferHostedService(
    IOneWireWorkSpooler spooler,
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    IOptionsMonitor<OneWireOptions> options,
    ILogger<PublisherFileTransferHostedService> logger) : BackgroundService
{
    /// <summary>Capability PublisherStudio advertises for reading announced files.</summary>
    private readonly string chunkCapabilityKey = "publisher.file.read";

    /// <summary>Announced transfers waiting to be read, in arrival order.</summary>
    private readonly Channel<Guid> offers = Channel.CreateUnbounded<Guid>();

    /// <summary>Chunk requests waiting for their result, by work item.</summary>
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> waiting = new();

    /// <summary>Subscribes to transfer announcements and chunk results.</summary>
    /// <param name="cancellationToken">Cancels startup.</param>
    /// <returns>A task that completes when the service has started.</returns>
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            spooler.PeerFileTransferOffered += OnTransferOffered;
            spooler.ExternalResultApplied += OnResultApplied;
            logger.LogDebug("PublisherStudio file transfer service subscribed to the 1-Wire spooler.");
            return base.StartAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio file transfer service could not start.");
            throw;
        }
    }

    /// <summary>Unsubscribes and stops reading transfers.</summary>
    /// <param name="cancellationToken">Cancels shutdown.</param>
    /// <returns>A task that completes when the service has stopped.</returns>
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            spooler.PeerFileTransferOffered -= OnTransferOffered;
            spooler.ExternalResultApplied -= OnResultApplied;
            offers.Writer.TryComplete();
            logger.LogDebug("PublisherStudio file transfer service unsubscribed from the 1-Wire spooler.");
            return base.StopAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio file transfer service could not stop cleanly.");
            throw;
        }
    }

    /// <summary>Reads announced transfers one after another.</summary>
    /// <param name="stoppingToken">Stops the service.</param>
    /// <returns>A task that runs until the host stops.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var workItemId in offers.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    await ReadTransferAsync(workItemId, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Reading the PublisherStudio file for work {WorkItemId} failed.", workItemId);
                    spooler.FailPeerFile(workItemId, exception.Message);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("PublisherStudio file transfer service stopped.");
        }
    }

    /// <summary>Queues an announced transfer.</summary>
    /// <param name="workItemId">Outgoing work item that announced the file.</param>
    private void OnTransferOffered(Guid workItemId)
    {
        try
        {
            if (!offers.Writer.TryWrite(workItemId))
                logger.LogWarning("The PublisherStudio file transfer for work {WorkItemId} could not be queued.", workItemId);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Queueing the PublisherStudio file transfer for work {WorkItemId} failed.", workItemId);
        }
    }

    /// <summary>Completes the wait of a chunk request.</summary>
    /// <param name="workItemId">Work item whose result arrived.</param>
    private void OnResultApplied(Guid workItemId)
    {
        try
        {
            if (waiting.TryRemove(workItemId, out var completion))
                completion.TrySetResult(true);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Completing the chunk wait for work {WorkItemId} failed.", workItemId);
        }
    }

    /// <summary>Reads every chunk of one announced file and hands the bytes to the spooler for verification.</summary>
    /// <param name="workItemId">Outgoing work item that announced the file.</param>
    /// <param name="cancellationToken">Stops the transfer.</param>
    /// <returns>A task that completes when the file was attached or rejected.</returns>
    private async Task ReadTransferAsync(Guid workItemId, CancellationToken cancellationToken)
    {
        try
        {
            var item = spooler.Get(workItemId) ?? throw new KeyNotFoundException("The work item that announced the file no longer exists.");
            var transferId = item.DocumentTransferId ?? throw new InvalidOperationException("The work item has no pending file transfer.");
            var peerId = item.Request.TargetPeerId;
            var maximumBytes = Math.Max(1, options.CurrentValue.MaximumPeerFileBytes);
            if (item.DocumentTransferLength > maximumBytes)
                throw new InvalidDataException($"The announced file has {item.DocumentTransferLength} bytes; the configured limit OneWire:MaximumPeerFileBytes is {maximumBytes}.");
            var capability = peers.GetPeer(peerId)?.Capabilities.FirstOrDefault(candidate =>
                candidate.IsEnabled && candidate.IsOnline && string.Equals(candidate.Key, chunkCapabilityKey, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"The peer that announced the file does not advertise {chunkCapabilityKey}.");

            using var buffer = new MemoryStream((int)item.DocumentTransferLength);
            while (buffer.Length < item.DocumentTransferLength)
            {
                var chunk = await ReadChunkAsync(peerId, capability, transferId, buffer.Length, cancellationToken).ConfigureAwait(false);
                if (chunk.Length == 0)
                    throw new InvalidDataException("PublisherStudio returned an empty chunk before the announced end of the file.");
                if (buffer.Length + chunk.Length > item.DocumentTransferLength)
                    throw new InvalidDataException("PublisherStudio returned more data than it announced.");
                buffer.Write(chunk);
            }
            spooler.AttachPeerFile(workItemId, buffer.ToArray());
            logger.LogInformation("Read {Bytes} bytes of PublisherStudio file {TransferId} for work {WorkItemId}.", buffer.Length, transferId, workItemId);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
                logger.LogDebug(exception, "Reading the PublisherStudio file for work {WorkItemId} was canceled.", workItemId);
            else
                logger.LogWarning(exception, "Reading the PublisherStudio file for work {WorkItemId} stopped.", workItemId);
            throw;
        }
    }

    /// <summary>Requests one chunk and waits for its verified result.</summary>
    /// <param name="peerId">Peer that announced the file.</param>
    /// <param name="capability">Advertised chunk capability.</param>
    /// <param name="transferId">Announced transfer.</param>
    /// <param name="offset">First byte to read.</param>
    /// <param name="cancellationToken">Stops the request.</param>
    /// <returns>The chunk bytes.</returns>
    private async Task<byte[]> ReadChunkAsync(string peerId, OneWireCapabilityDescriptor capability, string transferId, long offset, CancellationToken cancellationToken)
    {
        try
        {
            var envelope = new OneWireEnvelope
            {
                MessageType = OneWireMessageType.Invoke,
                SourcePeerId = "localgpt",
                TargetPeerId = peerId,
                CapabilityKey = capability.Key,
                Controller = capability.Controller,
                Method = capability.Method,
                Route = capability.Route,
                Organs = capability.Organs.ToList(),
                Skills = capability.Skills.ToList(),
                ExecutionMode = OneWireExecutionMode.Once,
                WorkOrderKey = $"file-read:{transferId}:{offset}",
                Properties = new Dictionary<string, JsonElement> { ["Parameters"] = JsonSerializer.SerializeToElement(new { transferId, offset }) }
            };
            envelope.NormalizeInteractionKind();
            var work = spooler.Enqueue(envelope);
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            waiting[work.Id] = completion;
            try
            {
                if (!await connections.SendAsync(peerId, envelope, cancellationToken).ConfigureAwait(false))
                    throw new InvalidOperationException("PublisherStudio disconnected while the file was being read.");
                var timeoutSeconds = Math.Max(1, options.CurrentValue.PeerFileChunkTimeoutSeconds);
                try
                {
                    await completion.Task.WaitAsync(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    throw new TimeoutException($"PublisherStudio did not return a file chunk within {timeoutSeconds} seconds (OneWire:PeerFileChunkTimeoutSeconds).");
                }
            }
            finally
            {
                waiting.TryRemove(work.Id, out _);
            }

            var result = spooler.Get(work.Id) ?? throw new KeyNotFoundException("The chunk request disappeared.");
            if (result.Status != OneWireWorkStatus.Completed)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.Error) ? $"The chunk request ended as {result.Status}." : result.Error);
            if (JsonNode.Parse(result.ResultJson) is not JsonObject chunk)
                throw new InvalidDataException("PublisherStudio returned an invalid file chunk.");
            string? Read(string name) => chunk.FirstOrDefault(property => string.Equals(property.Key, name, StringComparison.OrdinalIgnoreCase)).Value is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
            var offsetNode = chunk.FirstOrDefault(property => string.Equals(property.Key, "offset", StringComparison.OrdinalIgnoreCase)).Value;
            if (!string.Equals(Read("transferId"), transferId, StringComparison.OrdinalIgnoreCase) ||
                offsetNode is not JsonValue offsetValue || !offsetValue.TryGetValue<long>(out var returnedOffset) || returnedOffset != offset)
                throw new InvalidDataException("PublisherStudio returned a chunk for a different transfer or offset.");
            var bytes = Convert.FromBase64String(Read("documentChunk") ?? string.Empty);
            spooler.CompactResult(work.Id);
            logger.LogDebug("Read {Bytes} bytes at offset {Offset} of PublisherStudio file {TransferId}.", bytes.Length, offset, transferId);
            return bytes;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
                logger.LogDebug(exception, "Reading a PublisherStudio file chunk was canceled.");
            else
                logger.LogWarning(exception, "Reading a PublisherStudio file chunk at offset {Offset} failed.", offset);
            throw;
        }
    }
}
