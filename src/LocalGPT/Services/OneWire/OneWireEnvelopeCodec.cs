using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Represents an one wire envelope codec.
/// </summary>
public sealed class OneWireEnvelopeCodec : IOneWireEnvelopeCodec
{
    private readonly ILogger<OneWireEnvelopeCodec> logger;
    private readonly JsonSerializerOptions serializerOptions;

    /// <summary>
    /// Runs the one wire envelope codec operation.
    /// </summary>
    public OneWireEnvelopeCodec(ILogger<OneWireEnvelopeCodec> logger)
    {
        this.logger = logger;
        serializerOptions = CreateOptions();
    }

    /// <summary>
    /// Gets or sets JSON options.
    /// </summary>
    public JsonSerializerOptions JsonOptions => serializerOptions;

    /// <summary>
    /// Runs the serialize operation.
    /// </summary>
    public string Serialize(OneWireEnvelope envelope, bool seal = true)
    {
    try
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
            return JsonSerializer.Serialize(envelope, serializerOptions);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireEnvelopeCodec)}.{nameof(Serialize)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireEnvelopeCodec)}.{nameof(Serialize)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the deserialize and validate operation.
    /// </summary>
    public OneWireEnvelope DeserializeAndValidate(string json)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(json);
            if (Encoding.UTF8.GetByteCount(json) > OneWireProtocol.MaximumMessageBytes)
                throw new InvalidDataException("The 1-Wire message exceeds the supported size limit.");
            var envelope = JsonSerializer.Deserialize<OneWireEnvelope>(json, serializerOptions)
                ?? throw new JsonException("The 1-Wire envelope is empty.");
            if (!Validate(envelope, out var error))
                throw new InvalidDataException(error);
            return envelope;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireEnvelopeCodec)}.{nameof(DeserializeAndValidate)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireEnvelopeCodec)}.{nameof(DeserializeAndValidate)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the validate operation.
    /// </summary>
    public bool Validate(OneWireEnvelope envelope, out string error)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireEnvelopeCodec)}.{nameof(Validate)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireEnvelopeCodec)}.{nameof(Validate)} failed.");
        throw;
    }
}

    /// <summary>
    /// Validates payload shape.
    /// </summary>
    private void ValidatePayloadShape(OneWireEnvelope envelope)
    {
    try
    {
            if (!string.IsNullOrWhiteSpace(envelope.EncryptedPayload) && envelope.Properties is not null)
                throw new InvalidDataException("EncryptedPayload and public Properties are mutually exclusive.");
            if (envelope.Properties is not null && envelope.Properties.Count > 128)
                throw new InvalidDataException("The 1-Wire property count exceeds the supported limit.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireEnvelopeCodec)}.{nameof(ValidatePayloadShape)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireEnvelopeCodec)}.{nameof(ValidatePayloadShape)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds integrity bytes.
    /// </summary>
    private byte[] BuildIntegrityBytes(OneWireEnvelope envelope)
    {
    try
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
            return JsonSerializer.SerializeToUtf8Bytes(integrityView, serializerOptions);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireEnvelopeCodec)}.{nameof(BuildIntegrityBytes)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireEnvelopeCodec)}.{nameof(BuildIntegrityBytes)} failed.");
        throw;
    }
}

    /// <summary>
    /// Computes crc32.
    /// </summary>
    private uint ComputeCrc32(ReadOnlySpan<byte> data)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireEnvelopeCodec)}.{nameof(ComputeCrc32)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireEnvelopeCodec)}.{nameof(ComputeCrc32)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates options.
    /// </summary>
    private JsonSerializerOptions CreateOptions()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };
            logger.LogTrace($"Created the LocalGPT 1-Wire JSON serializer options.");
            return options;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not create the LocalGPT 1-Wire JSON serializer options.");
            throw;
        }
    }
}
