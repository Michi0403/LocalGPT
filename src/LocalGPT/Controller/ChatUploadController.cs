using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Receives browser drag/drop batches for the Local Chat quarantine workflow without adding a permanent upload control to the page.</summary>
/// <param name="uploads">Bounded chat upload workspace service.</param>
/// <param name="catalog">Database-backed LocalGPT upload policy catalog.</param>
/// <param name="logger">Logger used for bounded operational diagnostics.</param>
[ApiController]
[Route("api/chat-upload")]
public sealed class ChatUploadController(
    IChatUploadWorkspaceService uploads,
    LocalGptCatalogService catalog,
    ILogger<ChatUploadController> logger) : ControllerBase
{
    /// <summary>Streams an externally dropped browser file batch into one bounded quarantine workspace.</summary>
    /// <param name="cancellationToken">Cancellation token tied to the HTTP request.</param>
    /// <returns>The created workspace identity and bounded file count; file content is never echoed.</returns>
    [HttpPost("drop")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue, ValueCountLimit = int.MaxValue)]
    public async Task<IResult> DropAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!Request.HasFormContentType)
                return Results.BadRequest(new { Error = "Workspace drop requires multipart file content." });

            var form = await Request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
            var configuredMaximumFiles = Math.Max(1, catalog.MaxFiles);
            var files = form.Files
                .Where(file => file is not null && !string.IsNullOrWhiteSpace(file.FileName))
                .Take(configuredMaximumFiles)
                .ToList();
            if (files.Count == 0)
                return Results.BadRequest(new { Error = "No dropped files were supplied." });

            long totalBytes = 0;
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (file.Length < 0 || file.Length > catalog.MaxSingleFileBytes)
                    return Results.BadRequest(new { Error = $"{file.FileName}: file exceeds the configured single-file upload limit." });
                totalBytes = checked(totalBytes + file.Length);
                if (totalBytes > catalog.MaxTotalFileBytes)
                    return Results.BadRequest(new { Error = "Dropped file batch exceeds the configured total upload limit." });
            }

            var inputs = files.Select(file => new ChatUploadWorkspaceStreamInput(
                file.FileName,
                file.ContentType ?? string.Empty,
                file.Length,
                file.OpenReadStream)).ToList();
            var workspace = await uploads.CreateWorkspaceFromStreamsAsync(string.Empty, inputs, cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Accepted browser workspace drop into quarantine {WorkspaceName} with {FileCount} file(s) and {TotalBytes} declared byte(s).",
                workspace.WorkspaceName,
                workspace.FileCount,
                totalBytes);
            return Results.Ok(new
            {
                workspace.WorkspaceName,
                FileCount = workspace.FileCount,
                WarningCount = workspace.Warnings.Count
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OverflowException exception)
        {
            logger.LogWarning(exception, "A browser workspace drop declared an invalid aggregate byte count.");
            return Results.BadRequest(new { Error = "Dropped file batch size is invalid." });
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            logger.LogWarning(exception, "A browser workspace drop was rejected before promotion; file content was omitted from diagnostics.");
            return Results.BadRequest(new { Error = exception.Message });
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "A browser workspace drop could not be persisted to bounded quarantine; file content was omitted from diagnostics.");
            return Results.BadRequest(new { Error = "The dropped files could not be written to the quarantine workspace." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Browser workspace drop failed; file content was omitted from diagnostics.");
            return Results.InternalServerError(new { Error = "The dropped files could not be quarantined. Review local logs for details." });
        }
    }
}
