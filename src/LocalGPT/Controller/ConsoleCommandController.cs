using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes the bounded cross-platform LocalGPT console feed and explicitly confirmed command execution endpoint.</summary>
/// <param name="console">Shared console command service.</param>
/// <param name="logger">Logger used for operational diagnostics.</param>
[ApiController]
[Route("api/console")]
public sealed class ConsoleCommandController(IConsoleCommandService console, ILogger<ConsoleCommandController> logger) : ControllerBase
{
    /// <summary>Returns recent bounded console output suitable for the ASCII console surface.</summary>
    /// <param name="take">Take value supplied to the console command operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("history")]
    public IResult History([FromQuery] int take = 120)
    {
        try { return Results.Ok(console.GetRecentOutput(take)); }
        catch (Exception exception) { logger.LogError(exception, "Reading console output history failed."); return Results.InternalServerError("Console history could not be read. Review local logs for details."); }
    }

    /// <summary>Executes one read-only or explicitly confirmed command through the common console engine.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("execute")]
    [HumanApprovalRequired("console.execute", "Run local console command", "Execute the exact reviewed local terminal command with bounded output and no elevation.", "High", "Local machine operator")]
    public async Task<IResult> Execute([FromBody] LocalConsoleCommandRequest request, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await console.ExecuteAsync(request, cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or PlatformNotSupportedException or DirectoryNotFoundException) { return Results.BadRequest(new { Error = exception.Message }); }
        catch (Exception exception) { logger.LogError(exception, "Console command execution failed; command details were omitted."); return Results.InternalServerError("The local console operation failed. Review local logs for details."); }
    }
}
