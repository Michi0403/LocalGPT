using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Journals every 1-Wire packet. Log levels (category <c>LocalGPT.Services.OneWire.OneWireTrafficJournal</c>):
/// Information writes one metadata line per packet (Warning for rejected or failed packets), Debug adds the decrypted
/// payload according to <c>OneWire:Traffic:PayloadMode</c>, Trace adds the raw frame as it crossed the wire. A bounded
/// in-memory copy feeds the live traffic view. Journaling never throws into a transport.
/// </summary>
/// <param name="options">Live 1-Wire options; the <c>Traffic</c> section controls the journal.</param>
/// <param name="codec">Envelope codec used to serialize the plaintext view of an envelope.</param>
/// <param name="formatter">Applies the payload mode and length bounds.</param>
/// <param name="logger">Logger whose category carries the traffic log lines.</param>
public sealed class OneWireTrafficJournal(
    IOptionsMonitor<OneWireOptions> options,
    IOneWireEnvelopeCodec codec,
    IOneWireTrafficPayloadFormatter formatter,
    ILogger<OneWireTrafficJournal> logger) : IOneWireTrafficJournal
{
    /// <summary>Recent packets for the live view, oldest first.</summary>
    private readonly ConcurrentQueue<OneWireTrafficEntry> entries = new();

    /// <summary>Serializes rotation and append operations for the dedicated disk journal.</summary>
    private readonly object fileSync = new();

    /// <summary>Total packets journaled since start; also the sequence of the latest entry.</summary>
    private long recorded;

    /// <inheritdoc />
    public event Action? Changed;

    /// <inheritdoc />
    public OneWireTrafficOptions CurrentOptions => options.CurrentValue.Traffic ?? new OneWireTrafficOptions();

    /// <inheritdoc />
    public string CapturePlaintext(OneWireEnvelope envelope)
    {
        try
        {
            var traffic = CurrentOptions;
            if (!traffic.Enabled || string.Equals(traffic.PayloadMode, OneWireTrafficPayloadModes.None, StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            return codec.Serialize(envelope, seal: false);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Capturing the plaintext of an outgoing 1-Wire envelope failed; the packet is journaled without payload.");
            return string.Empty;
        }
    }

    /// <inheritdoc />
    public void Record(OneWireTrafficRecord record)
    {
        try
        {
            var traffic = CurrentOptions;
            if (!traffic.Enabled ||
                (!traffic.IncludeDiscovery && string.Equals(record.Transport, OneWireTrafficTransports.UdpDiscovery, StringComparison.Ordinal)))
                return;

            var envelope = record.Envelope;
            var plaintext = !string.IsNullOrEmpty(record.PlaintextJson)
                ? record.PlaintextJson
                : envelope is not null && !string.Equals(traffic.PayloadMode, OneWireTrafficPayloadModes.None, StringComparison.OrdinalIgnoreCase)
                    ? codec.Serialize(envelope, seal: false)
                    : string.Empty;
            var payload = formatter.Format(plaintext, traffic);
            var rawFrame = traffic.IncludeRawFrame ? record.RawFrame ?? string.Empty : string.Empty;
            var received = string.Equals(record.Direction, OneWireTrafficDirections.Received, StringComparison.Ordinal);
            var failed = !string.IsNullOrWhiteSpace(record.Error);
            var sequence = Interlocked.Increment(ref recorded);
            var entry = new OneWireTrafficEntry(
                sequence,
                DateTime.UtcNow,
                record.Direction,
                record.Transport,
                FirstNonEmpty(record.PeerId, received ? envelope?.SourcePeerId : envelope?.TargetPeerId),
                record.RemoteEndpoint ?? string.Empty,
                FirstNonEmpty(record.MessageType, envelope?.MessageType.ToString()),
                envelope?.MessageId ?? Guid.Empty,
                envelope?.CorrelationId ?? Guid.Empty,
                envelope?.CapabilityKey ?? string.Empty,
                envelope?.SecurityMode.ToString() ?? string.Empty,
                string.IsNullOrEmpty(record.RawFrame) ? 0 : Encoding.UTF8.GetByteCount(record.RawFrame),
                failed
                    ? received ? OneWireTrafficOutcomes.Rejected : OneWireTrafficOutcomes.Failed
                    : received ? OneWireTrafficOutcomes.Accepted : OneWireTrafficOutcomes.Delivered,
                record.Error ?? string.Empty,
                formatter.Bound(payload, traffic.MaximumStoredCharacters),
                formatter.Bound(rawFrame, traffic.MaximumStoredCharacters));

            WriteLog(entry, payload, rawFrame, traffic);
            WriteFile(entry, traffic);
            entries.Enqueue(entry);
            var capacity = Math.Max(10, traffic.JournalCapacity);
            while (entries.Count > capacity)
            {
                if (!entries.TryDequeue(out _))
                    break;
            }

            Changed?.Invoke();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Journaling a 1-Wire {Direction} {Transport} packet failed; the transport continues.", record?.Direction, record?.Transport);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<OneWireTrafficEntry> Query(OneWireTrafficQuery query)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(query);
            var text = query.Text?.Trim() ?? string.Empty;
            // AsEnumerable keeps LINQ's Reverse; on a bare array C# 14 binds the in-place span overload instead.
            var matches = entries.ToArray()
                .AsEnumerable()
                .Reverse()
                .Where(entry => string.IsNullOrWhiteSpace(query.Direction) || string.Equals(entry.Direction, query.Direction, StringComparison.OrdinalIgnoreCase))
                .Where(entry => string.IsNullOrWhiteSpace(query.Transport) || string.Equals(entry.Transport, query.Transport, StringComparison.OrdinalIgnoreCase))
                .Where(entry => !query.ProblemsOnly || !string.IsNullOrWhiteSpace(entry.Error))
                .Where(entry => text.Length == 0 || Matches(entry, text));
            return (query.Maximum > 0 ? matches.Take(query.Maximum) : matches).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Querying the 1-Wire traffic journal failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public long TotalRecorded()
    {
        try
        {
            return Interlocked.Read(ref recorded);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the 1-Wire traffic journal total failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        try
        {
            entries.Clear();
            logger.LogInformation("The 1-Wire traffic view was cleared; log files are unchanged.");
            Changed?.Invoke();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Clearing the 1-Wire traffic journal failed.");
            throw;
        }
    }

    /// <summary>Writes the metadata line, the payload (Debug) and the raw frame (Trace) of one packet.</summary>
    /// <param name="entry">The journaled packet.</param>
    /// <param name="payload">The formatted payload, unbounded.</param>
    /// <param name="rawFrame">The raw frame when captured, unbounded.</param>
    /// <param name="traffic">The traffic options in effect.</param>
    private void WriteLog(OneWireTrafficEntry entry, string payload, string rawFrame, OneWireTrafficOptions traffic)
    {
        try
        {
            var level = string.IsNullOrWhiteSpace(entry.Error) ? LogLevel.Information : LogLevel.Warning;
            logger.Log(
                level,
                "1-Wire #{Sequence} {Direction} {Transport} {MessageType} message {MessageId} correlation {CorrelationId} capability {CapabilityKey} peer {PeerId} remote {RemoteEndpoint}: {FrameBytes} bytes, security {SecurityMode}, {Outcome} {Error}",
                entry.Sequence, entry.Direction, entry.Transport, entry.MessageType, entry.MessageId, entry.CorrelationId,
                entry.CapabilityKey, entry.PeerId, entry.RemoteEndpoint, entry.FrameBytes, entry.SecurityMode, entry.Outcome, entry.Error);
            if (payload.Length > 0 && logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug(
                    "1-Wire #{Sequence} {Direction} payload ({PayloadMode}): {Payload}",
                    entry.Sequence, entry.Direction, traffic.PayloadMode, formatter.Bound(payload, traffic.MaximumLoggedCharacters));
            if (rawFrame.Length > 0 && logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace(
                    "1-Wire #{Sequence} {Direction} raw frame: {RawFrame}",
                    entry.Sequence, entry.Direction, formatter.Bound(rawFrame, traffic.MaximumLoggedCharacters));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Writing the 1-Wire traffic log lines failed.");
        }
    }

    /// <summary>Appends the bounded packet as JSON Lines to the dedicated per-user disk log, independently of ILogger category filtering.</summary>
    /// <param name="entry">Captured packet with the same bounded payload/raw frame used by the UI.</param>
    /// <param name="traffic">Live file logging and rotation policy.</param>
    private void WriteFile(OneWireTrafficEntry entry, OneWireTrafficOptions traffic)
    {
        if (!traffic.FileLoggingEnabled)
            return;
        try
        {
            var path = string.IsNullOrWhiteSpace(traffic.FilePath)
                ? LocalGptApplicationDataPaths.ResolveUserPath("OneWire-traffic.jsonl")
                : Path.IsPathRooted(traffic.FilePath)
                    ? Path.GetFullPath(traffic.FilePath)
                    : LocalGptApplicationDataPaths.ResolveUserPath(traffic.FilePath);
            var line = JsonSerializer.Serialize(entry) + Environment.NewLine;
            lock (fileSync)
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);
                var limit = Math.Max(1024L * 1024, traffic.MaximumFileBytes);
                if (File.Exists(path) && new FileInfo(path).Length + Encoding.UTF8.GetByteCount(line) > limit)
                    File.Move(path, path + ".previous", overwrite: true);
                File.AppendAllText(path, line, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
        }
        catch (Exception exception)
        {
            // A disk failure must never prevent receipt display or transport delivery.
            logger.LogWarning(exception, "The dedicated 1-Wire traffic file could not be updated; in-memory journaling continues.");
        }
    }

    /// <summary>Matches a packet against free search text.</summary>
    /// <param name="entry">The packet.</param>
    /// <param name="text">The trimmed search text.</param>
    /// <returns><see langword="true"/> when any searchable field contains the text.</returns>
    private bool Matches(OneWireTrafficEntry entry, string text)
    {
        try
        {
            return entry.PeerId.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                   entry.MessageType.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                   entry.MessageId.ToString().Contains(text, StringComparison.OrdinalIgnoreCase) ||
                   entry.CorrelationId.ToString().Contains(text, StringComparison.OrdinalIgnoreCase) ||
                   entry.CapabilityKey.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                   entry.RemoteEndpoint.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                   entry.Error.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                   entry.Payload.Contains(text, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Matching a 1-Wire traffic entry failed.");
            throw;
        }
    }

    /// <summary>Returns the first non-empty value.</summary>
    /// <param name="first">Preferred value.</param>
    /// <param name="second">Fallback value.</param>
    /// <returns>The first non-empty value, or empty.</returns>
    private string FirstNonEmpty(string? first, string? second)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(first) ? first : second ?? string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Choosing a 1-Wire traffic field value failed.");
            throw;
        }
    }
}
