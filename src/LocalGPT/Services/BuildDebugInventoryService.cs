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
        public string ArtifactRoot { get; } = LocalGptApplicationDataPaths.ResolveUserPath("BuildDebugFiles");

        /// <summary>How long a symbol-file listing is reused for chat briefings before bin/obj are scanned again.</summary>
        private readonly TimeSpan debugFileListingLifetime = TimeSpan.FromMinutes(5);
        /// <summary>Serializes recursive bin/obj scans so parallel chat requests share one scan instead of starting several.</summary>
        private readonly SemaphoreSlim debugFileScanGate = new(1, 1);
        /// <summary>Most recent bounded symbol-file listing (newest 250 files) reused by briefings.</summary>
        private IReadOnlyList<(FileInfo File, string SourceArea)>? cachedDebugFiles;
        /// <summary>UTC time at which <see cref="cachedDebugFiles"/> was produced.</summary>
        private DateTime cachedDebugFilesAtUtc;

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

                // The recursive bin/obj scan is synchronous file-system work. Never run it on the caller's
                // (possibly Blazor renderer) thread, and let a canceled chat request stop it. Chat briefings reuse
                // a recent listing; an explicit copy request always scans again.
                var debugFiles = await GetDebugFilesAsync(forceRefresh: copyFiles, cancellationToken).ConfigureAwait(false);

                foreach (var item in debugFiles)
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
        /// Returns the newest symbol files, reusing a listing younger than <see cref="debugFileListingLifetime"/> unless a refresh is forced.
        /// Every chat request asks for a briefing; without this reuse each one walked all bin/obj trees again.
        /// </summary>
        /// <param name="forceRefresh">Whether a fresh recursive scan is required (explicit diagnostics copy).</param>
        /// <param name="cancellationToken">Cancellation token that stops waiting for, or performing, the scan.</param>
        /// <returns>The newest symbol files, at most 250.</returns>
        private async Task<IReadOnlyList<(FileInfo File, string SourceArea)>> GetDebugFilesAsync(bool forceRefresh, CancellationToken cancellationToken)
        {
            try
            {
                if (!forceRefresh
                    && Volatile.Read(ref cachedDebugFiles) is { } fresh
                    && DateTime.UtcNow - cachedDebugFilesAtUtc < debugFileListingLifetime)
                {
                    return fresh;
                }

                await debugFileScanGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (!forceRefresh
                        && cachedDebugFiles is { } scannedMeanwhile
                        && DateTime.UtcNow - cachedDebugFilesAtUtc < debugFileListingLifetime)
                    {
                        return scannedMeanwhile;
                    }

                    IReadOnlyList<(FileInfo File, string SourceArea)> scanned = await Task.Run(
                        () => EnumerateDebugFiles(cancellationToken).OrderByDescending(item => item.File.LastWriteTimeUtc).Take(250).ToList(),
                        cancellationToken).ConfigureAwait(false);
                    cachedDebugFilesAtUtc = DateTime.UtcNow;
                    Volatile.Write(ref cachedDebugFiles, scanned);
                    return scanned;
                }
                finally
                {
                    debugFileScanGate.Release();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not list build debug symbol files; force refresh {ForceRefresh}.", forceRefresh);
                throw;
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
        /// <param name="cancellationToken">Cancellation token that stops the recursive scan when the owning chat request is canceled.</param>
        /// <returns>The collection produced by the operation.</returns>
        private IEnumerable<(FileInfo File, string SourceArea)> EnumerateDebugFiles(CancellationToken cancellationToken)
        {
            try
            {
                foreach (var target in councilRuntime.GetSearchRoots(logger))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!Directory.Exists(target.Path))
                        continue;

                    IEnumerable<string> files;
                    try
                    {
                        files = Directory.EnumerateFiles(target.Path, "*.*", SearchOption.AllDirectories)
                            .Where(path =>
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                return catalog.DebugExtensions.Contains(Path.GetExtension(path));
                            })
                            .ToList();
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Could not enumerate build debug files under {Path}.", target.Path);
                        continue;
                    }

                    foreach (var file in files)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
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
