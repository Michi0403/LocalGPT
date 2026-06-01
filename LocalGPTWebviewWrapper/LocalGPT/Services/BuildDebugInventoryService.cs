using System.Security.Cryptography;
using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    public sealed class BuildDebugInventoryService(ILogger<BuildDebugInventoryService> logger) : IBuildDebugInventoryService
    {
        private static readonly HashSet<string> DebugExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdb",
            ".pdg",
            ".appxsym"
        };

        public string ArtifactRoot { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalGPT",
            "BuildDebugFiles");

        public async Task<BuildDebugInventory> CaptureAsync(bool copyFiles = false, CancellationToken cancellationToken = default)
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
                    copiedPath = await CopyDebugFileAsync(item.File, item.SourceArea, captureRoot, cancellationToken);

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

        public async Task<string> BuildBriefingAsync(CancellationToken cancellationToken = default)
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

        private IEnumerable<(FileInfo File, string SourceArea)> EnumerateDebugFiles()
        {
            foreach (var target in GetSearchRoots())
            {
                if (!Directory.Exists(target.Path))
                    continue;

                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(target.Path, "*.*", SearchOption.AllDirectories)
                        .Where(path => DebugExtensions.Contains(Path.GetExtension(path)))
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

        private static IEnumerable<(string Area, string Path)> GetSearchRoots()
        {
            yield return ("runtime", AppContext.BaseDirectory);

            var root = FindRepositoryRoot();
            if (root is null)
                yield break;

            yield return ("LocalGPT bin", Path.Combine(root, "LocalGPTWebviewWrapper", "LocalGPT", "bin"));
            yield return ("LocalGPT obj", Path.Combine(root, "LocalGPTWebviewWrapper", "LocalGPT", "obj"));
            yield return ("WebView2 wrapper bin", Path.Combine(root, "LocalGPTWebviewWrapper", "LocalGPTWebviewWrapper", "bin"));
            yield return ("WebView2 wrapper obj", Path.Combine(root, "LocalGPTWebviewWrapper", "LocalGPTWebviewWrapper", "obj"));
            yield return ("MSIX package bin", Path.Combine(root, "LocalGPTWebviewWrapper", "LocalGPTWebviewWrapper (Package)", "bin"));
            yield return ("MSIX package obj", Path.Combine(root, "LocalGPTWebviewWrapper", "LocalGPTWebviewWrapper (Package)", "obj"));
        }

        private static async Task<string> CopyDebugFileAsync(FileInfo file, string sourceArea, string captureRoot, CancellationToken cancellationToken)
        {
            var area = SanitizeFileName(sourceArea);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(file.FullName)))[..12];
            var destination = Path.Combine(captureRoot, $"{area}-{hash}-{file.Name}");

            await using var read = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            await using var write = File.Open(destination, FileMode.Create, FileAccess.Write, FileShare.None);
            await read.CopyToAsync(write, cancellationToken);
            return destination;
        }

        private static string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
                builder.Append(invalid.Contains(character) || char.IsWhiteSpace(character) ? '-' : character);

            return builder.ToString().Trim('-');
        }

        private static string? FindRepositoryRoot()
        {
            foreach (var start in new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            })
            {
                var directory = new DirectoryInfo(start);
                while (directory is not null)
                {
                    if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) ||
                        Directory.Exists(Path.Combine(directory.FullName, ".git")))
                    {
                        return directory.FullName;
                    }

                    directory = directory.Parent;
                }
            }

            return null;
        }
    }
}
