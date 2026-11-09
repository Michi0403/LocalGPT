using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services.OneWire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Controller;

/// <summary>
/// Transport-neutral JSON adapter for small user-built organic clients such as ESP32 firmware.
/// It uses the same envelope, permissions, MFA trust and work spool as the TCP transport.
/// </summary>
/// <param name="codec">One wire envelope codec dependency used by the one wire HTTP workflow to provide the corresponding application capability.</param>
/// <param name="security">One wire runtime security service dependency used by the one wire HTTP workflow to provide the corresponding application capability.</param>
/// <param name="dispatcher">One wire message dispatcher dependency used by the one wire HTTP workflow to provide the corresponding application capability.</param>
/// <param name="transportSecurityPolicy">One wire transport security policy dependency used by the one wire HTTP workflow to provide the corresponding application capability.</param>
/// <param name="dispatchContextFactory">One wire dispatch context factory dependency used by the one wire HTTP workflow to provide the corresponding application capability.</param>
/// <param name="spooler">One wire work spooler dependency used by the one wire HTTP workflow to provide the corresponding application capability.</param>
/// <param name="capabilities">One wire capability catalog dependency used by the one wire HTTP workflow to provide the corresponding application capability.</param>
/// <param name="configuredOptions">One wire options dependency used by the one wire HTTP workflow to provide the corresponding application capability.</param>
/// <param name="traffic">Journals every HTTP/JSON request received and response sent.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/onewire/http-json")]
public sealed class OneWireHttpController(
    IOneWireEnvelopeCodec codec,
    IOneWireRuntimeSecurityService security,
    IOneWireMessageDispatcher dispatcher,
    IOneWireTransportSecurityPolicy transportSecurityPolicy,
    IOneWireDispatchContextFactory dispatchContextFactory,
    IOneWireWorkSpooler spooler,
    IOneWireCapabilityCatalog capabilities,
    IOptions<OneWireOptions> configuredOptions,
    IOneWireTrafficJournal traffic,
    ILogger<OneWireHttpController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the profile projection for the one wire HTTP API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("profile")]
    public async Task<ActionResult<OneWireProtocolProfile>> Profile(CancellationToken cancellationToken)
    {
        try
        {
            var options = configuredOptions.Value;
            var peer = dispatcher.GetLocalAdvertisement();
            var profile = new OneWireProtocolProfile
            {
                Security = await security.GetPublicDescriptorAsync(cancellationToken).ConfigureAwait(false),
                Peer = peer,
                Capabilities = (await capabilities.GetLocalCapabilitiesAsync(cancellationToken).ConfigureAwait(false)).ToList(),
                Skills = (await capabilities.GetLocalSkillsAsync(cancellationToken).ConfigureAwait(false)).ToList(),
                UiFeatures = (await capabilities.GetLocalUiFeaturesAsync(cancellationToken).ConfigureAwait(false)).ToList(),
                Hardware = (await capabilities.GetLocalHardwareAsync(cancellationToken).ConfigureAwait(false)).ToList(),
                Settings = new OneWirePublicSettings
                {
                    Enabled = options.Enabled,
                    DiscoveryEnabled = options.EnableDiscovery,
                    LanTransportEnabled = options.EnableLanTransport,
                    ListenAddress = options.ListenAddress,
                    ServicePort = options.ServicePort,
                    DiscoveryPort = options.DiscoveryPort,
                    BroadcastIntervalSeconds = options.BroadcastIntervalSeconds,
                    PeerExpirySeconds = options.PeerExpirySeconds,
                    MaximumMessageBytes = options.MaximumMessageBytes
                }
            };
            JournalProfile(profile);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not return the LocalGPT 1-Wire HTTP/JSON profile.");
            return Problem(ex.Message);
        }
    }

    /// <summary>
    /// Returns the dispatch projection for the one wire HTTP API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="body">Body value supplied to the one wire HTTP operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost]
    [RequestSizeLimit(OneWireProtocol.MaximumMessageBytes)]
    public async Task<IActionResult> Dispatch([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var json = string.Empty;
        OneWireEnvelope? received = null;
        var requestJournaled = false;
        try
        {
            json = body.GetRawText();
            if (Encoding.UTF8.GetByteCount(json) > OneWireProtocol.MaximumMessageBytes)
            {
                requestJournaled = JournalHttp(OneWireTrafficDirections.Received, json, null, string.Empty, "The 1-Wire HTTP/JSON envelope is too large.");
                return BadRequest(new { Error = "The 1-Wire HTTP/JSON envelope is too large." });
            }
            var envelope = codec.DeserializeAndValidate(json);
            received = envelope;
            await security.UnprotectIncomingAsync(envelope, cancellationToken).ConfigureAwait(false);
            requestJournaled = JournalHttp(OneWireTrafficDirections.Received, json, envelope, string.Empty, string.Empty);
            var remoteAddress = HttpContext.Connection.RemoteIpAddress;
            var context = dispatchContextFactory.CreateExternal(
                envelope.SourcePeerId,
                Guid.NewGuid(),
                transportSecurityPolicy.IsLoopback(remoteAddress),
                "http-json");
            var response = await dispatcher.DispatchAsync(envelope, context, cancellationToken).ConfigureAwait(false);
            if (response is null) return Accepted(new { envelope.CorrelationId, Status = "AcceptedWithoutImmediateResponse" });
            return await RespondAsync(response, "The HTTP/JSON response requires an MFA-verified peer before application data can be returned.", cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Cancelled a LocalGPT 1-Wire HTTP/JSON request at the caller's request.");
            return StatusCode(499);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or CryptographicException or FormatException or ArgumentException)
        {
            if (!requestJournaled)
                JournalHttp(OneWireTrafficDirections.Received, json, received, string.Empty, ex.Message);
            logger.LogWarning(ex, "Rejected an invalid LocalGPT 1-Wire HTTP/JSON request.");
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LocalGPT 1-Wire HTTP/JSON dispatch failed.");
            return Problem(ex.Message);
        }
    }

    /// <summary>
    /// Returns the work projection for the one wire HTTP API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("work/{correlationId:guid}")]
    public async Task<IActionResult> Work(Guid correlationId, CancellationToken cancellationToken)
    {
        try
        {
            var item = spooler.GetSnapshot().FirstOrDefault(candidate => candidate.CorrelationId == correlationId);
            if (item is null)
                return NotFound(new { CorrelationId = correlationId, Status = "NotFoundOrNotQueuedYet" });

            var response = new OneWireEnvelope
            {
                MessageType = OneWireMessageType.WorkResult,
                CorrelationId = item.CorrelationId,
                ReplyToMessageId = item.Request.MessageId,
                SourcePeerId = "localgpt",
                TargetPeerId = item.SourcePeerId,
                CapabilityKey = item.CapabilityKey,
                Error = item.Error,
                Properties = new Dictionary<string, JsonElement>
                {
                    ["WorkItemId"] = JsonSerializer.SerializeToElement(item.Id),
                    ["Status"] = JsonSerializer.SerializeToElement(item.Status.ToString()),
                    ["ResultJson"] = JsonSerializer.SerializeToElement(item.ResultJson),
                    ["CreatedUtc"] = JsonSerializer.SerializeToElement(item.CreatedUtc),
                    ["UpdatedUtc"] = JsonSerializer.SerializeToElement(item.UpdatedUtc)
                }
            };
            return await RespondAsync(response, "The HTTP/JSON work response requires an MFA-verified peer before application data can be returned.", cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(499);
        }
        catch (Exception ex) when (ex is CryptographicException or InvalidDataException or JsonException or FormatException)
        {
            logger.LogWarning(ex, "Rejected LocalGPT 1-Wire HTTP work polling for correlation {CorrelationId}.", correlationId);
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read LocalGPT 1-Wire HTTP work for correlation {CorrelationId}.", correlationId);
            return Problem(ex.Message);
        }
    }

    /// <summary>Protects, serializes and journals one HTTP/JSON response envelope.</summary>
    /// <param name="response">The response envelope in plaintext.</param>
    /// <param name="protectionRequiredMessage">Error raised when the response type needs protection the peer does not have.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The JSON content result carrying the protected frame.</returns>
    private async Task<IActionResult> RespondAsync(OneWireEnvelope response, string protectionRequiredMessage, CancellationToken cancellationToken)
    {
        var plaintext = traffic.CapturePlaintext(response);
        var frame = string.Empty;
        try
        {
            await security.ProtectOutgoingAsync(response, cancellationToken).ConfigureAwait(false);
            if (transportSecurityPolicy.RequiresProtectedTransport(response.MessageType) &&
                !transportSecurityPolicy.IsProtected(response))
            {
                throw new CryptographicException(protectionRequiredMessage);
            }
            frame = codec.Serialize(response);
            JournalHttp(OneWireTrafficDirections.Sent, frame, response, plaintext, string.Empty);
            return Content(frame, "application/json", Encoding.UTF8);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            JournalHttp(OneWireTrafficDirections.Sent, frame, response, plaintext, exception.Message);
            logger.LogDebug(exception, "Sending a LocalGPT 1-Wire HTTP/JSON response failed for message {MessageId}.", response.MessageId);
            throw;
        }
    }

    /// <summary>
    /// Journals a profile request and the profile returned for it. MVC writes the response frame itself, so the journal keeps
    /// the profile serialized with the web defaults MVC uses as the payload and leaves the raw frame empty.
    /// </summary>
    /// <param name="profile">The profile about to be returned.</param>
    private void JournalProfile(OneWireProtocolProfile profile)
    {
        try
        {
            var remoteEndpoint = $"{HttpContext.Connection.RemoteIpAddress}:{HttpContext.Connection.RemotePort}";
            traffic.Record(new OneWireTrafficRecord
            {
                Direction = OneWireTrafficDirections.Received,
                Transport = OneWireTrafficTransports.HttpJson,
                RemoteEndpoint = remoteEndpoint,
                RawFrame = $"GET {Request.Path}",
                MessageType = "Profile"
            });
            var payload = traffic.CurrentOptions.Enabled ? JsonSerializer.Serialize(profile, JsonSerializerOptions.Web) : string.Empty;
            traffic.Record(new OneWireTrafficRecord
            {
                Direction = OneWireTrafficDirections.Sent,
                Transport = OneWireTrafficTransports.HttpJson,
                RemoteEndpoint = remoteEndpoint,
                PlaintextJson = payload,
                MessageType = "Profile"
            });
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Journaling the LocalGPT 1-Wire HTTP/JSON profile exchange failed.");
        }
    }

    /// <summary>Journals one HTTP/JSON packet with this request's remote endpoint.</summary>
    /// <param name="direction">Received or Sent.</param>
    /// <param name="frame">The frame as it crossed the wire.</param>
    /// <param name="envelope">The envelope, decrypted when received, when one was parsed.</param>
    /// <param name="plaintext">The plaintext captured before protection, for sent packets.</param>
    /// <param name="error">Rejection or failure reason; empty on success.</param>
    /// <returns>Always <see langword="true"/>, so callers can remember that the packet was journaled.</returns>
    private bool JournalHttp(string direction, string frame, OneWireEnvelope? envelope, string plaintext, string error)
    {
        try
        {
            traffic.Record(new OneWireTrafficRecord
            {
                Direction = direction,
                Transport = OneWireTrafficTransports.HttpJson,
                RemoteEndpoint = $"{HttpContext.Connection.RemoteIpAddress}:{HttpContext.Connection.RemotePort}",
                RawFrame = frame,
                Envelope = envelope,
                PlaintextJson = plaintext,
                Error = error
            });
            return true;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Journaling a LocalGPT 1-Wire HTTP/JSON packet failed.");
            return true;
        }
    }
}
