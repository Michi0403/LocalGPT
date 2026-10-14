using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Serves generated Local AI media from the bounded per-user artifact store.</summary>
/// <param name="artifacts">Local ai artifact service dependency used by the local AI artifact workflow to provide the corresponding application capability.</param>
[ApiController]
public sealed class LocalAiArtifactController(ILocalAiArtifactService artifacts) : ControllerBase
{
    /// <summary>
    /// Returns the get projection for the local AI artifact API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="artifactId">Identifier of the artifact to use for this operation.</param>
    /// <param name="fileName">File name value supplied to the local AI artifact operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("/__artifacts/media/{artifactId}/{fileName}")]
    public IActionResult Get(string artifactId, string fileName)
    {
        var artifact = artifacts.Resolve(artifactId, fileName);
        if (artifact is null)
            return NotFound();
        Response.Headers["Cache-Control"] = "private, no-store, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
        Response.Headers["Content-Disposition"] = $"inline; filename*=UTF-8''{Uri.EscapeDataString(artifact.FileName)}";
        return PhysicalFile(artifact.FullPath, artifact.ContentType, enableRangeProcessing: true);
    }
}
