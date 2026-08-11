using LocalGPT.BusinessObjects;
using LocalGPT.Controller;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using LocalGPT.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace LocalGPT.Endpoints
{
    /// <summary>
    /// Provides minecraft diagnostic controller operations.
    /// </summary>
    [ApiController]
    [Route("")]
    public class MinecraftDiagnosticController(ILogger<MinecraftDiagnosticController> logger,
        CouncilRuntimeService councilRuntime,
        CouncilTextService councilText,
        LocalGptCatalogService catalog) : ControllerBase
    {
        /// <summary>
        /// Runs the require human confirmation operation.
        /// </summary>
        private IResult? RequireHumanConfirmation(bool userConfirmed, string operation) =>
            userConfirmed
                ? null
                : Results.BadRequest(new
                {
                    Error = "Fresh, specific human confirmation is required for this operation.",
                    Operation = operation
                });

        /// <summary>
        /// Gets minecraft project name file name.
        /// </summary>
        [HttpGet("/__artifacts/minecraft/{projectName}/{fileName}")]
        public IResult GetMinecraftProjectNameFileName(
            string projectName,
            string fileName,
            [FromServices] IMinecraftModWorkspaceService workspaces)
        {
            try
            {
                var safeProjectName = Path.GetFileName(projectName);
                var safeFileName = Path.GetFileName(fileName);
                if (!string.Equals(projectName, safeProjectName, StringComparison.Ordinal) ||
                    !string.Equals(fileName, safeFileName, StringComparison.Ordinal) ||
                    !safeFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest("Invalid Minecraft artifact path.");

                var path = Path.Combine(workspaces.WorkspaceRoot, safeProjectName, "build", safeFileName);
                var fullPath = Path.GetFullPath(path);
                var allowedRoot = Path.GetFullPath(workspaces.WorkspaceRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest("Artifact is outside the Minecraft workspace root.");

                if (!System.IO.File.Exists(fullPath))
                    return Results.NotFound();

                HttpContext.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{safeFileName}\"";
                HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
                return Results.File(fullPath, "application/octet-stream", safeFileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetMinecraftProjectNameFileName");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
        }

        /// <summary>
        /// Gets minecraft datapack version.
        /// </summary>
        [HttpGet("/__diag/minecraft/datapack-version")]
        public IResult GetMinecraftDatapackVersion(
            string? minecraftVersion)
        {
            try
            {
                return Results.Ok(councilRuntime.MinecraftDatapackVersionInfoResolve(minecraftVersion,logger));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetMinecraftDatapackVersion");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }         
        }

        /// <summary>
        /// Gets minecraft dependency version.
        /// </summary>
        [HttpGet("/__diag/minecraft/dependency-version")]
        public IResult GetMinecraftDependencyVersion(
            string? loader,
            string? minecraftVersion,
            string? javaVersion,
            string? gradleVersion)
        {
            try
            {
                return Results.Ok(councilRuntime.ResolveMinecraftDependencyVersionInfo(loader, minecraftVersion,logger, javaVersion, gradleVersion));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetMinecraftDependencyVersion");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }          
        }

        /// <summary>
        /// Gets minecraft workspace smoke.
        /// </summary>
        [HttpGet("/__diag/minecraft/workspace-smoke")]
        [HumanApprovalRequired("diagnostic.minecraft.workspace.create", "Create Minecraft diagnostic workspace", "Create one bounded Minecraft diagnostic workspace from the exact selected loader and versions.", "High", "Minecraft workspace reviewer")]
        public async Task<IResult> GetMinecraftWorkspaceSmoke(
            [FromServices] IMinecraftModWorkspaceService workspaceService,
            string? loader,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "create a Minecraft diagnostic workspace") is { } denied)
                    return denied;

                var request = new MinecraftModBuildRequest
                {
                    ProjectName = $"LivingCitiesSmoke{DateTime.UtcNow:HHmmss}",
                    ModId = "living_cities_smoke",
                    PackageName = "com.localgpt.livingcitiessmoke",
                    Loader = string.IsNullOrWhiteSpace(loader) ? "Fabric" : loader,
                    MinecraftVersion = catalog.DefaultMinecraftVersion,
                    JavaVersion = "25",
                    GradleVersion = "8.14.2",
                    Ide = "Eclipse",
                    IncludeLivingCitiesStarter = true,
                    Description = "Smoke-test the LocalGPT Minecraft Mod Builder with a small Living Cities starter item and report command."
                };

                var workspace = await workspaceService.CreateWorkspaceAsync(request, ct).ConfigureAwait(false);
                return Results.Ok(new
                {
                    workspace.ProjectName,
                    workspace.RootPath,
                    workspace.MainClassPath,
                    workspace.MetadataPath,
                    workspace.BuildFilePath,
                    workspace.ReadmePath,
                    workspace.BuildCommand,
                    workspace.EclipseImportHint
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetMinecraftWorkspaceSmoke");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }          
        }

        /// <summary>
        /// Gets minecraft datapack benchmark.
        /// </summary>
        [HttpGet("/__diag/minecraft/datapack-benchmark")]
        [HumanApprovalRequired("diagnostic.minecraft.datapack.benchmark", "Build Minecraft datapack benchmark", "Create, validate, build, and persist the exact Minecraft datapack benchmark request.", "High", "Minecraft build reviewer", requiredBeforeCompletion: true)]
        public async Task<IResult> GetMinecraftDatapackBenchmark(
            [FromServices] IMinecraftModWorkspaceService workspaceService,
            [FromServices] INativeCommandRunner commandRunner,
            [FromServices] ICouncilKnowledgeService knowledgeService,
            string? minecraftVersion,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "create, validate, and save a Minecraft datapack benchmark") is { } denied)
                    return denied;

                var request = new MinecraftModBuildRequest
                {
                    ProjectName = $"LivingCitiesDatapackCouncil{DateTime.UtcNow:HHmmss}",
                    ModId = "living_cities",
                    PackageName = "com.localgpt.livingcities",
                    Loader = "Datapack",
                    MinecraftVersion = string.IsNullOrWhiteSpace(minecraftVersion) ? catalog.DefaultMinecraftVersion : minecraftVersion,
                    JavaVersion = "25",
                    GradleVersion = "8.14.2",
                    Ide = "Eclipse",
                    IncludeLivingCitiesStarter = true,
                    Description = "Generate and validate the Living Cities 0.1 vanilla datapack benchmark from the provided design plan and early reference zip."
                };

                var workspace = await workspaceService.CreateWorkspaceAsync(request, ct).ConfigureAwait(false);
                var build = await commandRunner.RunAsync(
                    "powershell.exe",
                    "-NoProfile -ExecutionPolicy Bypass -File .\\build-local.ps1",
                    workspace.RootPath,
                    ct,
                    userConfirmed: userConfirmed).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("The approved datapack build did not produce a command result.");
                var files = Directory.GetFiles(workspace.RootPath, "*", SearchOption.AllDirectories)
                    .Select(path => Path.GetRelativePath(workspace.RootPath, path).Replace('\\', '/'))
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var referenceComparison = councilRuntime.BuildDatapackReferenceComparison(workspace.RootPath,logger);
                var knowledgeEntry = await knowledgeService.SaveEntryAsync(new CouncilKnowledgeEntry
                {
                    Topic = "Living Cities datapack benchmark",
                    Scope = "Minecraft Builder",
                    Source = "/__diag/minecraft/datapack-benchmark",
                    Content = string.Join(Environment.NewLine, new[]
                    {
                    "Use this compact entry instead of sending the full Living Cities design document to every model.",
                    "Goal: vanilla Minecraft Java datapack for Living Cities 0.1 with city aggregate simulation, no full-world scans, town hall/admin UI, population, food, security, personalities, chronicle, and quests.",
                    "Reference zip traits: legacy pack_format 61 for 1.21.4, namespace living_cities, singular data/<namespace>/function folders, core/load and core/tick tags, early placeholders that should become real .mcfunction files. Current default generation targets Minecraft Java 26.1 pack_format 101.1 unless the user requests an older version.",
                    $"Latest generated workspace: {workspace.RootPath}",
                    $"Build succeeded: {build.Succeeded}; exit code {build.ExitCode}.",
                    $"Function files: {files.Count(file => file.EndsWith(".mcfunction", StringComparison.OrdinalIgnoreCase))}.",
                    $"Build output: {councilText.TrimForKnowledge(build.StandardOutput ?? string.Empty, 700, logger)}",
                    $"Reference comparison: {referenceComparison?.Summary}",
                    $"Reference placeholders: {referenceComparison?.ReferencePlaceholderCount}; generated placeholders: {referenceComparison?.GeneratedPlaceholderCount}.",
                    $"Root pack.mcmeta: generated={referenceComparison?.GeneratedHasRootPackMcmeta}, reference={referenceComparison?.ReferenceHasRootPackMcmeta}, reference nested={referenceComparison?.ReferenceHasNestedPackMcmeta}.",
                    $"Critical files preserved after reference normalization: {referenceComparison?.PreservedCriticalFileCount}/{referenceComparison?.CriticalFileCount}.",
                    "Validation checks: required files, JSON parse, no .mcfunction.txt placeholders, load/tick tag targets exist, function namespace:path references resolve.",
                    "Before friend testing: verify exact Minecraft Java version/pack_format and run /reload, /datapack list, /function living_cities:ui/townhall in a test world."
                }),
                    HelpfulSources = "Official Minecraft Java datapack/version documentation; exact installed Minecraft version manifest; friend in-game test result.",
                    Tags = "minecraft; datapack; living-cities; benchmark; low-context",
                    Confidence = build.Succeeded ? 78 : 45,
                    IsPinned = true
                }, ct).ConfigureAwait(false);

                return Results.Ok(new
                {
                    workspace.ProjectName,
                    workspace.RootPath,
                    workspace.ReadmePath,
                    workspace.BuildCommand,
                    KnowledgeEntryId = knowledgeEntry.Id,
                    Build = build,
                    ReferenceComparison = referenceComparison,
                    FunctionFileCount = files.Count(file => file.EndsWith(".mcfunction", StringComparison.OrdinalIgnoreCase)),
                    Files = files
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetMinecraftDatapackBenchmark");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }       
        }
    }
}
