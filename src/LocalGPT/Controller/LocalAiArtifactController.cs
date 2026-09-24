using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Serves generated Local AI media from the bounded per-user artifact store.</summary>
[ApiController]
public sealed class LocalAiArtifactController(ILocalAiArtifactService artifacts) : ControllerBase
{
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
