using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Represents an one wire envelope codec application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OneWireEnvelopeCodec : IOneWireEnvelopeCodec
{
    /// <summary>
    /// Stores the logger used by <see cref="OneWireEnvelopeCodec"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<OneWireEnvelopeCodec> logger;
    /// <summary>
    /// Stores the internal serializer options state used by <see cref="OneWireEnvelopeCodec"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions serializerOptions;

    /// <summary>
    /// Initializes a new <see cref="OneWireEnvelopeCodec"/> instance and captures the dependencies or initial state required by its one wire envelope codec workflow.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public OneWireEnvelopeCodec(ILogger<OneWireEnvelopeCodec> logger)
    {
        this.logger = logger;
        serializerOptions = CreateOptions();
    }

    /// <summary>
    /// Gets the JSON options value that forms part of the one wire envelope codec state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The JSON options value exposed by <see cref="OneWireEnvelopeCodec"/>.</value>
    public JsonSerializerOptions JsonOptions => serializerOptions;

    /// <summary>
    /// Performs serialize for <see cref="OneWireEnvelopeCodec"/>, keeping the operation consistent with the state and invariants of the surrounding one wire envelope codec workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire envelope codec operation and used when producing its result.</param>
    /// <param name="seal">Value indicating whether seal should apply to this operation.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Performs deserialize and validate for <see cref="OneWireEnvelopeCodec"/>, keeping the operation consistent with the state and invariants of the surrounding one wire envelope codec workflow.
    /// </summary>
    /// <param name="json">Json value supplied to the one wire envelope codec operation and used when producing its result.</param>
    /// <returns>The one wire envelope produced by the operation.</returns>
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
    /// Performs validate for <see cref="OneWireEnvelopeCodec"/>, keeping the operation consistent with the state and invariants of the surrounding one wire envelope codec workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire envelope codec operation and used when producing its result.</param>
    /// <param name="error">Error value supplied to the one wire envelope codec operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Validates payload shape for <see cref="OneWireEnvelopeCodec"/>, keeping the operation consistent with the state and invariants of the surrounding one wire envelope codec workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire envelope codec operation and used when producing its result.</param>
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
    /// Builds integrity bytes for <see cref="OneWireEnvelopeCodec"/>, keeping the operation consistent with the state and invariants of the surrounding one wire envelope codec workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire envelope codec operation and used when producing its result.</param>
    /// <returns>The byte produced by the operation.</returns>
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
    /// Computes crc32 for <see cref="OneWireEnvelopeCodec"/>, keeping the operation consistent with the state and invariants of the surrounding one wire envelope codec workflow.
    /// </summary>
    /// <param name="data">Data value supplied to the one wire envelope codec operation and used when producing its result.</param>
    /// <returns>The uint produced by the operation.</returns>
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
    /// Creates options for <see cref="OneWireEnvelopeCodec"/>, keeping the operation consistent with the state and invariants of the surrounding one wire envelope codec workflow.
    /// </summary>
    /// <returns>The JSON serializer options produced by the operation.</returns>
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
