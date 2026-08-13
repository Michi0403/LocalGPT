using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Text;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates build debug inventory behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="councilRuntime">Council runtime service dependency used by the build debug inventory workflow to provide the corresponding application capability.</param>
    /// <param name="catalog">Local gpt catalog service dependency used by the build debug inventory workflow to provide the corresponding application capability.</param>
    public sealed class BuildDebugInventoryService(ILogger<BuildDebugInventoryService> logger,
        CouncilRuntimeService councilRuntime,
        LocalGptCatalogService catalog) : IBuildDebugInventoryService
    {


        /// <summary>
        /// Gets the artifact root value that forms part of the build debug inventory state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The artifact root value exposed by <see cref="BuildDebugInventoryService"/>.</value>
        public string ArtifactRoot { get; } = Path.Combine(
            /// <summary>
            /// Retrieves folder path as part of the build debug inventory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
            /// </summary>
            /// <returns>The environment produced by the operation.</returns>
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalGPT",
            "BuildDebugFiles");

        /// <summary>
        /// Performs capture as part of the build debug inventory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="copyFiles">Value indicating whether copy files should apply to this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The build debug inventory produced by the operation.</returns>
        public async Task<BuildDebugInventory> CaptureAsync(bool copyFiles = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var inventory = new BuildDebugInventory
                {
                    ArtifactRoot = ArtifactRoot,
                    CopiedFiles = copyFiles
                };

                var captureRoot = copyFiles
                    ? Path.Combine(ArtifactRoot, DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss"))
                    : null;

                if (captureRoot is not null)
                    Directory.CreateDirectory(captureRoot);

                foreach (var item in EnumerateDebugFiles().OrderByDescending(item => item.File.LastWriteTimeUtc).Take(250))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string? copiedPath = null;
                    if (captureRoot is not null)
                        copiedPath = await councilRuntime.CopyDebugFileAsync(item.File, item.SourceArea, captureRoot, cancellationToken, logger).ConfigureAwait(false);

                    inventory.Files.Add(new BuildDebugFileSummary
                    {
                        Name = item.File.Name,
                        Extension = item.File.Extension,
                        SourcePath = item.File.FullName,
                        CopiedPath = copiedPath,
                        Length = item.File.Length,
                        LastWriteUtc = item.File.LastWriteTimeUtc,
                        SourceArea = item.SourceArea
                    });
                }

                return inventory;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not capture the build/debug inventory; copy-files mode {CopyFiles}.", copyFiles);
                return new BuildDebugInventory
                {
                    ArtifactRoot = ArtifactRoot,
                    CopiedFiles = copyFiles,
                    Warnings = ["Build/debug inventory capture failed. Review LocalGPT application logs for technical details."]
                };
            }
           
        }

        /// <summary>
        /// Builds briefing as part of the build debug inventory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The string produced by the operation.</returns>
        public async Task<string> BuildBriefingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var inventory = await CaptureAsync(copyFiles: false, cancellationToken).ConfigureAwait(false);
                if (!inventory.Succeeded)
                    return string.Join(" ", inventory.Warnings);

                if (inventory.Files.Count == 0)
                    return "No build debug symbol files (.pdb, .pdg, .appxsym) were found in the current LocalGPT output paths.";

                var builder = new StringBuilder()
                    .AppendLine("Build debug symbol files available for LocalGPT diagnostics:")
                    .AppendLine("- These files are not committed to git; use `/__diag/build-debug-files?copy=true` to copy current symbols to LocalAppData for inspection.")
                    .AppendLine("- Use them as build/debug evidence only. Do not confuse loaded symbols or generated references with source-level usage.")
                    .AppendLine("- Recent symbol files:");

                foreach (var file in inventory.Files.Take(20))
                {
                    builder
                        .Append("- ")
                        .Append(file.SourceArea)
                        .Append(": ")
                        .Append(file.Name)
                        .Append(" (")
                        .Append(file.Length)
                        .Append(" bytes, ")
                        .Append(file.LastWriteUtc.ToString("u"))
                        .AppendLine(")");
                }

                return builder.ToString().Trim();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build the build/debug inventory briefing.");
                return "Build/debug inventory briefing failed. Review LocalGPT application logs for technical details.";
            }
           
        }

        /// <summary>
        /// Performs enumerate debug files as part of the build debug inventory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <returns>The collection produced by the operation.</returns>
        private IEnumerable<(FileInfo File, string SourceArea)> EnumerateDebugFiles()
        {
            try
            {
                foreach (var target in councilRuntime.GetSearchRoots(logger))
                {
                    if (!Directory.Exists(target.Path))
                        continue;

                    IEnumerable<string> files;
                    try
                    {
                        files = Directory.EnumerateFiles(target.Path, "*.*", SearchOption.AllDirectories)
                            .Where(path => catalog.DebugExtensions.Contains(Path.GetExtension(path)))
                            .ToList();
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Could not enumerate build debug files under {Path}.", target.Path);
                        continue;
                    }

                    foreach (var file in files)
                    {
                        FileInfo info;
                        try
                        {
                            info = new FileInfo(file);
                        }
                        catch
                        {
                            continue;
                        }

                        if (info.Exists)
                            yield return (info, target.Area);
                    }
                }
            }
            finally
            {
                logger.LogInformation("Finished enumerating build debug files.");
            }
        }
    }
}
