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
    ILogger<OneWireHttpController> logger) : ControllerBase
{
    /// <summary>
    /// Runs the profile operation.
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<OneWireProtocolProfile>> Profile(CancellationToken cancellationToken)
    {
        try
        {
            var options = configuredOptions.Value;
            var peer = dispatcher.GetLocalAdvertisement();
            return Ok(new OneWireProtocolProfile
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
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not return the LocalGPT 1-Wire HTTP/JSON profile.");
            return Problem(ex.Message);
        }
    }

    /// <summary>
    /// Runs the dispatch operation.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(OneWireProtocol.MaximumMessageBytes)]
    public async Task<IActionResult> Dispatch([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        try
        {
            var json = body.GetRawText();
            if (Encoding.UTF8.GetByteCount(json) > OneWireProtocol.MaximumMessageBytes)
                return BadRequest(new { Error = "The 1-Wire HTTP/JSON envelope is too large." });
            var envelope = codec.DeserializeAndValidate(json);
            await security.UnprotectIncomingAsync(envelope, cancellationToken).ConfigureAwait(false);
            var remoteAddress = HttpContext.Connection.RemoteIpAddress;
            var context = dispatchContextFactory.CreateExternal(
                envelope.SourcePeerId,
                Guid.NewGuid(),
                transportSecurityPolicy.IsLoopback(remoteAddress),
                "http-json");
            var response = await dispatcher.DispatchAsync(envelope, context, cancellationToken).ConfigureAwait(false);
            if (response is null) return Accepted(new { envelope.CorrelationId, Status = "AcceptedWithoutImmediateResponse" });
            await security.ProtectOutgoingAsync(response, cancellationToken).ConfigureAwait(false);
            if (transportSecurityPolicy.RequiresProtectedTransport(response.MessageType) &&
                !transportSecurityPolicy.IsProtected(response))
            {
                throw new CryptographicException("The HTTP/JSON response requires an MFA-verified peer before application data can be returned.");
            }
            return Content(codec.Serialize(response), "application/json", Encoding.UTF8);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Cancelled a LocalGPT 1-Wire HTTP/JSON request at the caller's request.");
            return StatusCode(499);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or CryptographicException or FormatException or ArgumentException)
        {
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
    /// Runs the work operation.
    /// </summary>
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
            await security.ProtectOutgoingAsync(response, cancellationToken).ConfigureAwait(false);
            if (transportSecurityPolicy.RequiresProtectedTransport(response.MessageType) &&
                !transportSecurityPolicy.IsProtected(response))
            {
                throw new CryptographicException("The HTTP/JSON work response requires an MFA-verified peer before application data can be returned.");
            }
            return Content(codec.Serialize(response), "application/json", Encoding.UTF8);
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
}
