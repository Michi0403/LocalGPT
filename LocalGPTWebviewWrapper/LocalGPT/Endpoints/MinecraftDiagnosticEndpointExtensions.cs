using System.IO.Compression;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services;

namespace LocalGPT.Endpoints
{
    public static class MinecraftDiagnosticEndpointExtensions
    {
        public static IEndpointRouteBuilder MapMinecraftDiagnosticEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/__artifacts/minecraft/{projectName}/{fileName}", (
                string projectName,
                string fileName,
                IMinecraftModWorkspaceService workspaces,
                HttpContext httpContext) =>
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

                if (!File.Exists(fullPath))
                    return Results.NotFound();

                httpContext.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{safeFileName}\"";
                httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
                return Results.File(fullPath, "application/octet-stream", safeFileName);
            });

            app.MapGet("/__diag/minecraft/datapack-version", (string? minecraftVersion) =>
            {
                return Results.Ok(MinecraftDatapackVersionCatalog.Resolve(minecraftVersion));
            });

            app.MapGet("/__diag/minecraft/dependency-version", (string? loader, string? minecraftVersion, string? javaVersion, string? gradleVersion) =>
            {
                return Results.Ok(MinecraftDependencyVersionCatalog.Resolve(loader, minecraftVersion, javaVersion, gradleVersion));
            });

            app.MapGet("/__diag/minecraft/workspace-smoke", async (
                IMinecraftModWorkspaceService workspaceService,
                string? loader,
                CancellationToken ct) =>
            {
                var request = new MinecraftModBuildRequest
                {
                    ProjectName = $"LivingCitiesSmoke{DateTime.UtcNow:HHmmss}",
                    ModId = "living_cities_smoke",
                    PackageName = "com.localgpt.livingcitiessmoke",
                    Loader = string.IsNullOrWhiteSpace(loader) ? "Fabric" : loader,
                    MinecraftVersion = MinecraftDatapackVersionCatalog.DefaultMinecraftVersion,
                    JavaVersion = "25",
                    GradleVersion = "8.14.2",
                    Ide = "Eclipse",
                    IncludeLivingCitiesStarter = true,
                    Description = "Smoke-test the LocalGPT Minecraft Mod Builder with a small Living Cities starter item and report command."
                };

                var workspace = await workspaceService.CreateWorkspaceAsync(request, ct);
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
            });

            app.MapGet("/__diag/minecraft/datapack-benchmark", async (
                IMinecraftModWorkspaceService workspaceService,
                INativeCommandRunner commandRunner,
                ICouncilKnowledgeService knowledgeService,
                string? minecraftVersion,
                CancellationToken ct) =>
            {
                var request = new MinecraftModBuildRequest
                {
                    ProjectName = $"LivingCitiesDatapackCouncil{DateTime.UtcNow:HHmmss}",
                    ModId = "living_cities",
                    PackageName = "com.localgpt.livingcities",
                    Loader = "Datapack",
                    MinecraftVersion = string.IsNullOrWhiteSpace(minecraftVersion) ? MinecraftDatapackVersionCatalog.DefaultMinecraftVersion : minecraftVersion,
                    JavaVersion = "25",
                    GradleVersion = "8.14.2",
                    Ide = "Eclipse",
                    IncludeLivingCitiesStarter = true,
                    Description = "Generate and validate the Living Cities 0.1 vanilla datapack benchmark from the provided design plan and early reference zip."
                };

                var workspace = await workspaceService.CreateWorkspaceAsync(request, ct);
                var build = await commandRunner.RunAsync(
                    "powershell.exe",
                    "-NoProfile -ExecutionPolicy Bypass -File .\\build-local.ps1",
                    workspace.RootPath,
                    ct);
                var files = Directory.GetFiles(workspace.RootPath, "*", SearchOption.AllDirectories)
                    .Select(path => Path.GetRelativePath(workspace.RootPath, path).Replace('\\', '/'))
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var referenceComparison = BuildDatapackReferenceComparison(workspace.RootPath);
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
                        $"Build output: {TrimForKnowledge(build.StandardOutput, 700)}",
                        $"Reference comparison: {referenceComparison.Summary}",
                        $"Reference placeholders: {referenceComparison.ReferencePlaceholderCount}; generated placeholders: {referenceComparison.GeneratedPlaceholderCount}.",
                        $"Root pack.mcmeta: generated={referenceComparison.GeneratedHasRootPackMcmeta}, reference={referenceComparison.ReferenceHasRootPackMcmeta}, reference nested={referenceComparison.ReferenceHasNestedPackMcmeta}.",
                        $"Critical files preserved after reference normalization: {referenceComparison.PreservedCriticalFileCount}/{referenceComparison.CriticalFileCount}.",
                        "Validation checks: required files, JSON parse, no .mcfunction.txt placeholders, load/tick tag targets exist, function namespace:path references resolve.",
                        "Before friend testing: verify exact Minecraft Java version/pack_format and run /reload, /datapack list, /function living_cities:ui/townhall in a test world."
                    }),
                    HelpfulSources = "Official Minecraft Java datapack/version documentation; exact installed Minecraft version manifest; friend in-game test result.",
                    Tags = "minecraft; datapack; living-cities; benchmark; low-context",
                    Confidence = build.Succeeded ? 78 : 45,
                    IsPinned = true
                }, ct);

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
            });

            return app;
        }

        private static string TrimForKnowledge(string text, int maxLength)
        {
            var normalized = Regex.Replace(text ?? string.Empty, "\\s+", " ").Trim();
            return normalized.Length <= maxLength
                ? normalized
                : $"{normalized[..maxLength].TrimEnd()}...";
        }

        private static DatapackReferenceComparison BuildDatapackReferenceComparison(string workspaceRoot)
        {
            var generatedZip = Directory.Exists(Path.Combine(workspaceRoot, "build"))
                ? Directory.GetFiles(Path.Combine(workspaceRoot, "build"), "*.zip").Order(StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? string.Empty
                : string.Empty;
            var referenceZip = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "living_cities.zip");

            if (string.IsNullOrWhiteSpace(generatedZip) || !File.Exists(generatedZip))
            {
                return DatapackReferenceComparison.Missing(
                    generatedZip,
                    referenceZip,
                    "Generated benchmark zip was not found.");
            }

            if (!File.Exists(referenceZip))
            {
                return DatapackReferenceComparison.Missing(
                    generatedZip,
                    referenceZip,
                    "Reference living_cities.zip was not found in Downloads.");
            }

            var generatedEntries = ReadZipFileEntries(generatedZip);
            var referenceEntries = ReadZipFileEntries(referenceZip);
            var normalizedReferenceEntries = referenceEntries
                .Select(NormalizeReferenceDatapackEntry)
                .Where(entry => !string.IsNullOrWhiteSpace(entry))
                .ToArray();

            var generatedSet = generatedEntries.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var normalizedReferenceSet = normalizedReferenceEntries.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var criticalFiles = new[]
            {
                "pack.mcmeta",
                "data/minecraft/tags/function/load.json",
                "data/minecraft/tags/function/tick.json",
                "data/living_cities/function/core/load.mcfunction",
                "data/living_cities/function/core/tick.mcfunction",
                "data/living_cities/function/city/create.mcfunction",
                "data/living_cities/function/citizens/register.mcfunction",
                "data/living_cities/function/ui/status.mcfunction"
            };
            var preservedCriticalFiles = criticalFiles
                .Where(file => generatedSet.Contains(file) && normalizedReferenceSet.Contains(file))
                .ToArray();
            var generatedPlaceholders = generatedEntries
                .Where(entry => entry.EndsWith(".mcfunction.txt", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var referencePlaceholders = referenceEntries
                .Where(entry => entry.EndsWith(".mcfunction.txt", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var summary = string.Join(" ", new[]
            {
                $"Generated zip has {generatedEntries.Length} files and {generatedEntries.Count(IsMcFunctionPath)} functions.",
                $"Reference zip has {referenceEntries.Length} files and {referenceEntries.Count(IsMcFunctionPath)} real functions plus {referencePlaceholders.Length} placeholders.",
                "Generated zip has root pack.mcmeta/load/tick tags; reference keeps those under a top-level folder, so it is useful as a design benchmark but less install-ready as a zip."
            });

            return new DatapackReferenceComparison(
                GeneratedZipPath: generatedZip,
                ReferenceZipPath: referenceZip,
                ReferenceExists: true,
                GeneratedFileCount: generatedEntries.Length,
                GeneratedFunctionFileCount: generatedEntries.Count(IsMcFunctionPath),
                GeneratedPlaceholderCount: generatedPlaceholders.Length,
                ReferenceFileCount: referenceEntries.Length,
                ReferenceFunctionFileCount: referenceEntries.Count(IsMcFunctionPath),
                ReferencePlaceholderCount: referencePlaceholders.Length,
                GeneratedHasRootPackMcmeta: generatedSet.Contains("pack.mcmeta"),
                ReferenceHasRootPackMcmeta: referenceEntries.Contains("pack.mcmeta", StringComparer.OrdinalIgnoreCase),
                ReferenceHasNestedPackMcmeta: normalizedReferenceSet.Contains("pack.mcmeta"),
                GeneratedHasLoadTag: generatedSet.Contains("data/minecraft/tags/function/load.json"),
                GeneratedHasTickTag: generatedSet.Contains("data/minecraft/tags/function/tick.json"),
                ReferenceHasLoadTag: normalizedReferenceSet.Contains("data/minecraft/tags/function/load.json"),
                ReferenceHasTickTag: normalizedReferenceSet.Contains("data/minecraft/tags/function/tick.json"),
                CriticalFileCount: criticalFiles.Length,
                PreservedCriticalFileCount: preservedCriticalFiles.Length,
                PreservedCriticalFiles: preservedCriticalFiles,
                ReferencePlaceholderSamples: referencePlaceholders.Take(12).ToArray(),
                Summary: summary);
        }

        private static string[] ReadZipFileEntries(string zipPath)
        {
            using var archive = ZipFile.OpenRead(zipPath);
            return archive.Entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
                .Select(entry => entry.FullName.Replace('\\', '/'))
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string NormalizeReferenceDatapackEntry(string entry)
        {
            var normalized = entry.Replace('\\', '/').TrimStart('/');
            const string nestedPrefix = "living_cities/";
            return normalized.StartsWith(nestedPrefix, StringComparison.OrdinalIgnoreCase)
                ? normalized[nestedPrefix.Length..]
                : normalized;
        }

        private static bool IsMcFunctionPath(string entry) =>
            entry.EndsWith(".mcfunction", StringComparison.OrdinalIgnoreCase);

        public sealed record DatapackReferenceComparison(
            string GeneratedZipPath,
            string ReferenceZipPath,
            bool ReferenceExists,
            int GeneratedFileCount,
            int GeneratedFunctionFileCount,
            int GeneratedPlaceholderCount,
            int ReferenceFileCount,
            int ReferenceFunctionFileCount,
            int ReferencePlaceholderCount,
            bool GeneratedHasRootPackMcmeta,
            bool ReferenceHasRootPackMcmeta,
            bool ReferenceHasNestedPackMcmeta,
            bool GeneratedHasLoadTag,
            bool GeneratedHasTickTag,
            bool ReferenceHasLoadTag,
            bool ReferenceHasTickTag,
            int CriticalFileCount,
            int PreservedCriticalFileCount,
            string[] PreservedCriticalFiles,
            string[] ReferencePlaceholderSamples,
            string Summary)
        {
            public static DatapackReferenceComparison Missing(string generatedZipPath, string referenceZipPath, string summary) =>
                new(
                    GeneratedZipPath: generatedZipPath,
                    ReferenceZipPath: referenceZipPath,
                    ReferenceExists: File.Exists(referenceZipPath),
                    GeneratedFileCount: 0,
                    GeneratedFunctionFileCount: 0,
                    GeneratedPlaceholderCount: 0,
                    ReferenceFileCount: 0,
                    ReferenceFunctionFileCount: 0,
                    ReferencePlaceholderCount: 0,
                    GeneratedHasRootPackMcmeta: false,
                    ReferenceHasRootPackMcmeta: false,
                    ReferenceHasNestedPackMcmeta: false,
                    GeneratedHasLoadTag: false,
                    GeneratedHasTickTag: false,
                    ReferenceHasLoadTag: false,
                    ReferenceHasTickTag: false,
                    CriticalFileCount: 0,
                    PreservedCriticalFileCount: 0,
                    PreservedCriticalFiles: [],
                    ReferencePlaceholderSamples: [],
                    Summary: summary);
        }
    }
}
