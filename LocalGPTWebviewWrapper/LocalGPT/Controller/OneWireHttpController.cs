using LocalGPT.Interfaces;
using LocalGPT.Services.OneWire;
using Microsoft.AspNetCore.Mvc;
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
    IOneWireWorkSpooler spooler,
    ILogger<OneWireHttpController> logger) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<ActionResult<object>> Profile(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(new
            {
                ProtocolVersion = OneWireProtocol.Version,
                OneWireProtocol.MinimumCompatibleVersion,
                Transport = "http-json",
                PostEnvelope = "/api/onewire/http-json",
                PollWork = "/api/onewire/http-json/work/{correlationId}",
                MaximumMessageBytes = OneWireProtocol.MaximumMessageBytes,
                Security = await security.GetPublicDescriptorAsync(cancellationToken).ConfigureAwait(false),
                Peer = OneWireMessageDispatcher.LocalAdvertisement()
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not return the LocalGPT 1-Wire HTTP/JSON profile.");
            return Problem(ex.Message);
        }
    }

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
            var response = await dispatcher.DispatchAsync(envelope, cancellationToken).ConfigureAwait(false);
            if (response is null) return Accepted(new { envelope.CorrelationId, Status = "AcceptedWithoutImmediateResponse" });
            await security.ProtectOutgoingAsync(response, cancellationToken).ConfigureAwait(false);
            return Content(codec.Serialize(response), "application/json", Encoding.UTF8);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Cancelled a LocalGPT 1-Wire HTTP/JSON request at the caller's request.");
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

    [HttpGet("work/{correlationId:guid}")]
    public ActionResult<object> Work(Guid correlationId)
    {
        try
        {
            var item = spooler.GetSnapshot().FirstOrDefault(candidate => candidate.CorrelationId == correlationId);
            return item is null
                ? NotFound(new { CorrelationId = correlationId, Status = "NotFoundOrNotQueuedYet" })
                : Ok(new { item.Id, item.CorrelationId, item.CapabilityKey, item.Status, item.ResultJson, item.Error, item.CreatedUtc, item.UpdatedUtc });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read LocalGPT 1-Wire HTTP work for correlation {CorrelationId}.", correlationId);
            return Problem(ex.Message);
        }
    }
}
