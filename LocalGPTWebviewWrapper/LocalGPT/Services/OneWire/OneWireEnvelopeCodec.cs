using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalGPT.Services.OneWire;

public sealed class OneWireEnvelopeCodec : IOneWireEnvelopeCodec
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateOptions();

    public string Serialize(OneWireEnvelope envelope, bool seal = true)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (seal)
        {
            envelope.NormalizeInteractionKind();
            ValidatePayloadShape(envelope);
            var integrity = BuildIntegrityBytes(envelope);
            envelope.Hash = Convert.ToHexString(SHA256.HashData(integrity));
            envelope.ErrorCheck = ComputeCrc32(integrity).ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
        }
        return JsonSerializer.Serialize(envelope, SerializerOptions);
    }

    public OneWireEnvelope DeserializeAndValidate(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        if (Encoding.UTF8.GetByteCount(json) > OneWireProtocol.MaximumMessageBytes)
            throw new InvalidDataException("The 1-Wire message exceeds the supported size limit.");
        var envelope = JsonSerializer.Deserialize<OneWireEnvelope>(json, SerializerOptions)
            ?? throw new JsonException("The 1-Wire envelope is empty.");
        if (!Validate(envelope, out var error))
            throw new InvalidDataException(error);
        return envelope;
    }

    public bool Validate(OneWireEnvelope envelope, out string error)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(envelope);
            if (!OneWireProtocol.IsCompatible(envelope.ProtocolVersion))
                throw new InvalidDataException($"Unsupported 1-Wire protocol version '{envelope.ProtocolVersion}'.");
            if (envelope.MessageId == Guid.Empty || envelope.CorrelationId == Guid.Empty)
                throw new InvalidDataException("MessageId and CorrelationId are required.");
            if (envelope.ExpiresUtc is { } expires && expires < DateTimeOffset.UtcNow)
                throw new InvalidDataException("The 1-Wire message has expired.");
            ValidatePayloadShape(envelope);

            var integrity = BuildIntegrityBytes(envelope);
            var expectedHash = Convert.ToHexString(SHA256.HashData(integrity));
            var expectedCrc = ComputeCrc32(integrity).ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(envelope.Hash) || !CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(expectedHash), Encoding.ASCII.GetBytes(envelope.Hash.Trim().ToUpperInvariant())))
                throw new InvalidDataException("The 1-Wire hash check failed.");
            if (!string.Equals(expectedCrc, envelope.ErrorCheck, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The 1-Wire transmission error check failed.");

            error = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException)
        {
            error = ex.Message;
            return false;
        }
    }

    private static void ValidatePayloadShape(OneWireEnvelope envelope)
    {
        if (!string.IsNullOrWhiteSpace(envelope.EncryptedPayload) && envelope.Properties is not null)
            throw new InvalidDataException("EncryptedPayload and public Properties are mutually exclusive.");
        if (envelope.Properties is not null && envelope.Properties.Count > 128)
            throw new InvalidDataException("The 1-Wire property count exceeds the supported limit.");
    }

    private static byte[] BuildIntegrityBytes(OneWireEnvelope envelope)
    {
        var orderedProperties = envelope.Properties is null
            ? null
            : new SortedDictionary<string, JsonElement>(envelope.Properties, StringComparer.Ordinal);
        var integrityView = new
        {
            envelope.ProtocolVersion,
            envelope.MessageId,
            envelope.CorrelationId,
            envelope.ReplyToMessageId,
            envelope.MessageType,
            envelope.SourcePeerId,
            envelope.TargetPeerId,
            envelope.CreatedUtc,
            envelope.ExpiresUtc,
            envelope.Sequence,
            envelope.ExecutionMode,
            envelope.Controller,
            envelope.Method,
            envelope.Route,
            envelope.CapabilityKey,
            Organs = envelope.Organs.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            Skills = envelope.Skills.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            Properties = orderedProperties,
            envelope.EncryptedPayload,
            envelope.SecurityMode,
            envelope.SecurityKeyId,
            envelope.EncryptionNonce,
            envelope.AuthenticationTag,
            envelope.Signature,
            envelope.UserConfirmed,
            envelope.ApprovalMode,
            envelope.WorkOrderKey,
            envelope.NotBeforeUtc,
            envelope.WorkflowJson,
            envelope.Error,
            envelope.RequiresHumanInteractionOnTargetSystem,
            envelope.RequiresAutomatedInteractionOnTargetSystem,
            envelope.InteractionKind,
            envelope.InteractionValueJson,
            envelope.InteractionValueContentType
        };
        return JsonSerializer.SerializeToUtf8Bytes(integrityView, SerializerOptions);
    }

    private static uint ComputeCrc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) == 1 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }
        return ~crc;
    }

    internal static JsonSerializerOptions CreateOptions() => new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
}
