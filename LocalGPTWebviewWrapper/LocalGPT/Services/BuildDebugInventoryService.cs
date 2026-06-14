using LocalGPT.BusinessObjects;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Text;

namespace LocalGPT.Services
{
    public sealed class BuildDebugInventoryService(ILogger<BuildDebugInventoryService> logger) : IBuildDebugInventoryService
    {


        public string ArtifactRoot { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalGPT",
            "BuildDebugFiles");

        public async Task<BuildDebugInventory?> CaptureAsync(bool copyFiles = false, CancellationToken cancellationToken = default)
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
                        copiedPath = await CouncilChatStaticsGeneral.CopyDebugFileAsync(item.File, item.SourceArea, captureRoot, cancellationToken, logger);

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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CaptureAsync messages {copyFiles.ToString()}");
                return null;
            }
           
        }

        public async Task<string> BuildBriefingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var inventory = await CaptureAsync(copyFiles: false, cancellationToken);
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildBriefingAsync");
                return string.Empty;
            }
           
        }

        private IEnumerable<(FileInfo File, string SourceArea)> EnumerateDebugFiles()
        {
            try
            {
                foreach (var target in CouncilChatStaticsGeneral.GetSearchRoots(logger))
                {
                    if (!Directory.Exists(target.Path))
                        continue;

                    IEnumerable<string> files;
                    try
                    {
                        files = Directory.EnumerateFiles(target.Path, "*.*", SearchOption.AllDirectories)
                            .Where(path => GlobalVariableSlopCollectionToRemove.DebugExtensions.Contains(Path.GetExtension(path)))
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
                logger.LogInformation($"Finished EnumerateDebugFiles");
            }
        }
    }
}