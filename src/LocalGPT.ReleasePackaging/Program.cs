using System.Formats.Tar;
using System.Globalization;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace LocalGPT.ReleasePackaging;

/// <summary>
/// Represents a program application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Stores the shared read-only reproducible timestamp value used by <see cref="Program"/> across instances of the containing type.
    /// </summary>
    private static readonly DateTimeOffset ReproducibleTimestamp = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Performs main for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the program operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0) throw new ArgumentException("A command is required: tar, deb, sha256, pdf-merge, or pdf-optimize.");
            var command = args[0].ToLowerInvariant();
            var values = Parse(args.Skip(1).ToArray());
            switch (command)
            {
                case "tar": CreateTarGz(Required(values, "source"), Required(values, "output"), Optional(values, "root", string.Empty), Multi(values, "executable")); break;
                case "deb": CreateDeb(values); break;
                case "sha256": CreateSha256(Required(values, "directory"), Required(values, "output")); break;
                case "pdf-merge": MergePdf(values); break;
                case "pdf-optimize": OptimizePdf(values); break;
                default: throw new ArgumentException($"Unknown packaging command '{command}'.");
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }

    /// <summary>
    /// Performs parse for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the program operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private static Dictionary<string, List<string>> Parse(string[] args)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException($"Unexpected argument '{args[i]}'.");
            var key = args[i][2..];
            if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException($"Missing value for --{key}.");
            if (!result.TryGetValue(key, out var list)) result[key] = list = [];
            list.Add(args[++i]);
        }
        return result;
    }

    /// <summary>
    /// Performs required for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
    /// </summary>
    /// <param name="values">Values value supplied to the program operation and used when producing its result.</param>
    /// <param name="key">Key value supplied to the program operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private static string Required(Dictionary<string, List<string>> values, string key) =>
        values.TryGetValue(key, out var list) && list.Count > 0 && !string.IsNullOrWhiteSpace(list[^1]) ? list[^1] : throw new ArgumentException($"--{key} is required.");
    /// <summary>
    /// Performs optional for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
    /// </summary>
    /// <param name="values">Values value supplied to the program operation and used when producing its result.</param>
    /// <param name="key">Key value supplied to the program operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the program operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private static string Optional(Dictionary<string, List<string>> values, string key, string fallback) => values.TryGetValue(key, out var list) && list.Count > 0 ? list[^1] : fallback;
    /// <summary>
    /// Performs multi for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
    /// </summary>
    /// <param name="values">Values value supplied to the program operation and used when producing its result.</param>
    /// <param name="key">Key value supplied to the program operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private static IReadOnlyList<string> Multi(Dictionary<string, List<string>> values, string key) => values.TryGetValue(key, out var list) ? list : [];

    /// <summary>
    /// Creates tar gz for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
    /// </summary>
    /// <param name="sourceDirectory">Source directory value supplied to the program operation and used when producing its result.</param>
    /// <param name="outputPath">Output path value supplied to the program operation and used when producing its result.</param>
    /// <param name="rootName">Root name value supplied to the program operation and used when producing its result.</param>
    /// <param name="executablePaths">String dependency used by the program workflow to provide the corresponding application capability.</param>
    private static void CreateTarGz(string sourceDirectory, string outputPath, string rootName, IReadOnlyList<string> executablePaths)
    {
        sourceDirectory = Path.GetFullPath(sourceDirectory);
        if (!Directory.Exists(sourceDirectory)) throw new DirectoryNotFoundException(sourceDirectory);
        outputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var executableSet = executablePaths.Select(NormalizeArchivePath).ToHashSet(StringComparer.Ordinal);
        var temp = outputPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            // Close the TAR/GZip/FileStream chain before committing the temporary artifact.
            // Windows does not permit File.Move while the source file is still open with FileShare.None.
            using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.SequentialScan))
            using (var gzip = new GZipStream(file, CompressionLevel.SmallestSize, leaveOpen: false))
            using (var tar = new TarWriter(gzip, TarEntryFormat.Pax, leaveOpen: false))
            {
                WriteTree(tar, sourceDirectory, rootName, executableSet);
            }
            CommitTemporaryFile(temp, outputPath);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        Console.WriteLine(outputPath);
    }

    /// <summary>
    /// Writes tree for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
    /// </summary>
    /// <param name="writer">Writer value supplied to the program operation and used when producing its result.</param>
    /// <param name="sourceDirectory">Source directory value supplied to the program operation and used when producing its result.</param>
    /// <param name="rootName">Root name value supplied to the program operation and used when producing its result.</param>
    /// <param name="executableSet">Executable set value supplied to the program operation and used when producing its result.</param>
    private static void WriteTree(TarWriter writer, string sourceDirectory, string rootName, HashSet<string> executableSet)
    {
        var root = Path.GetFullPath(sourceDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (var directory in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal))
        {
            var relative = NormalizeArchivePath(Path.GetRelativePath(root, directory));
            var entryName = Prefix(rootName, relative).TrimEnd('/') + "/";
            var entry = new PaxTarEntry(TarEntryType.Directory, entryName) { ModificationTime = ReproducibleTimestamp, Mode = (UnixFileMode)Convert.ToInt32("755", 8) };
            writer.WriteEntry(entry);
        }
        foreach (var filePath in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal))
        {
            var relative = NormalizeArchivePath(Path.GetRelativePath(root, filePath));
            var entry = new PaxTarEntry(TarEntryType.RegularFile, Prefix(rootName, relative))
            {
                ModificationTime = ReproducibleTimestamp,
                Mode = executableSet.Contains(relative) ? (UnixFileMode)Convert.ToInt32("755", 8) : (UnixFileMode)Convert.ToInt32("644", 8),
                DataStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.SequentialScan)
            };
            try { writer.WriteEntry(entry); }
            finally { entry.DataStream?.Dispose(); }
        }
    }

    /// <summary>
    /// Performs prefix for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
    /// </summary>
    /// <param name="rootName">Root name value supplied to the program operation and used when producing its result.</param>
    /// <param name="relative">Relative value supplied to the program operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private static string Prefix(string rootName, string relative)
    {
        var root = NormalizeArchivePath(rootName).Trim('/');
        return string.IsNullOrEmpty(root) ? relative : $"{root}/{relative}";
    }
    /// <summary>
    /// Normalizes archive path for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the program operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private static string NormalizeArchivePath(string value)
    {
        var normalized = value.Replace('\\', '/').TrimStart('/');
        if (normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Contains("..", StringComparer.Ordinal)) throw new InvalidDataException($"Unsafe archive path '{value}'.");
        return normalized;
    }

    /// <summary>
    /// Creates deb for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
    /// </summary>
    /// <param name="values">Values value supplied to the program operation and used when producing its result.</param>
    private static void CreateDeb(Dictionary<string, List<string>> values)
    {
        var source = Path.GetFullPath(Required(values, "source"));
        var output = Path.GetFullPath(Required(values, "output"));
        var packageName = Required(values, "package").ToLowerInvariant();
        var version = Required(values, "version");
        var architecture = Required(values, "architecture");
        var executable = Required(values, "executable");
        var description = Optional(values, "description", packageName);
        var dependencies = Multi(values, "dependency").Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToArray();
        if (!Directory.Exists(source)) throw new DirectoryNotFoundException(source);
        var work = Path.Combine(Path.GetTempPath(), "release-packaging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var controlRoot = Path.Combine(work, "control"); Directory.CreateDirectory(controlRoot);
            var installedSize = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories).Select(x => new FileInfo(x).Length).Sum() / 1024 + 1;
            var control = new StringBuilder()
                .AppendLine($"Package: {packageName}").AppendLine($"Version: {version}").AppendLine($"Architecture: {architecture}")
                .AppendLine("Maintainer: Michi0403").AppendLine($"Installed-Size: {installedSize.ToString(CultureInfo.InvariantCulture)}")
                .AppendLine("Section: utils").AppendLine("Priority: optional");
            if (dependencies.Length > 0) control.AppendLine("Depends: " + string.Join(", ", dependencies));
            control.AppendLine($"Description: {description}");
            File.WriteAllText(Path.Combine(controlRoot, "control"), control.ToString(), new UTF8Encoding(false));

            var dataRoot = Path.Combine(work, "data");
            var appRoot = Path.Combine(dataRoot, "opt", packageName); Directory.CreateDirectory(appRoot);
            CopyDirectory(source, appRoot);
            var binRoot = Path.Combine(dataRoot, "usr", "bin"); Directory.CreateDirectory(binRoot);
            var launcher = Path.Combine(binRoot, packageName);
            File.WriteAllText(launcher, $"#!/bin/sh\nexec /opt/{packageName}/{executable} \"$@\"\n", new UTF8Encoding(false));

            var controlTar = Path.Combine(work, "control.tar.gz");
            CreateTarGz(controlRoot, controlTar, string.Empty, ["control"]);
            var dataTar = Path.Combine(work, "data.tar.gz");
            var executableRelative = NormalizeArchivePath(Path.Combine("opt", packageName, executable));
            CreateTarGz(dataRoot, dataTar, string.Empty, [NormalizeArchivePath(Path.Combine("usr", "bin", packageName)), executableRelative]);

            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            var temp = output + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                // Close the AR output stream before moving the completed package into place.
                // Keeping this stream open is a deterministic sharing violation on Windows.
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.SequentialScan))
                {
                    stream.Write(Encoding.ASCII.GetBytes("!<arch>\n"));
                    using (var debianBinary = new MemoryStream(Encoding.ASCII.GetBytes("2.0\n"), writable: false))
                    {
                        WriteArMember(stream, "debian-binary", debianBinary);
                    }
                    using (var member = File.OpenRead(controlTar)) WriteArMember(stream, "control.tar.gz", member);
                    using (var member = File.OpenRead(dataTar)) WriteArMember(stream, "data.tar.gz", member);
                }
                CommitTemporaryFile(temp, output);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            Console.WriteLine(output);
        }
        finally { try { Directory.Delete(work, true); } catch { } }
    }

    /// <summary>
    /// Atomically commits a completed temporary artifact after every writer has released the file handle.
    /// </summary>
    /// <param name="temporaryPath">Fully written temporary artifact path.</param>
    /// <param name="destinationPath">Final artifact path to replace.</param>
    private static void CommitTemporaryFile(string temporaryPath, string destinationPath)
    {
        const int maximumAttempts = 20;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.Move(temporaryPath, destinationPath, true);
                return;
            }
            catch (IOException) when (attempt < maximumAttempts)
            {
                // Antivirus/indexer activity can briefly hold a previous artifact on Windows.
                // The temporary source is already closed, so a short bounded retry is safe.
                Thread.Sleep(100);
            }
        }
    }

    /// <summary>
    /// Performs copy directory for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
    /// </summary>
    /// <param name="source">Source value supplied to the program operation and used when producing its result.</param>
    /// <param name="destination">Destination value supplied to the program operation and used when producing its result.</param>
    private static void CopyDirectory(string source, string destination)
    {
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories)) Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file)); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(file, target, true);
        }
    }

    /// <summary>
    /// Writes ar member for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
    /// </summary>
    /// <param name="output">Output value supplied to the program operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the program operation and used when producing its result.</param>
    /// <param name="input">Input value supplied to the program operation and used when producing its result.</param>
    private static void WriteArMember(Stream output, string name, Stream input)
    {
        if (!input.CanSeek) throw new InvalidOperationException("AR member streams must be seekable.");
        var length = input.Length - input.Position;
        var header = string.Concat(
            (name + "/").PadRight(16),
            "0".PadRight(12),
            "0".PadRight(6),
            "0".PadRight(6),
            "100644".PadRight(8),
            length.ToString(CultureInfo.InvariantCulture).PadRight(10),
            "`\n");
        if (header.Length != 60) throw new InvalidDataException("Invalid AR header length.");
        output.Write(Encoding.ASCII.GetBytes(header));
        input.CopyTo(output, 1024 * 1024);
        if ((length & 1) != 0) output.WriteByte((byte)'\n');
    }


    /// <summary>
    /// Merges one or more PDF files in the supplied order using the cross-platform PDFsharp core package.
    /// </summary>
    /// <param name="values">Parsed command-line values containing repeated input paths and the output path.</param>
    private static void MergePdf(Dictionary<string, List<string>> values)
    {
        var output = Path.GetFullPath(Required(values, "output"));
        var inputs = Multi(values, "input").Where(x => !string.IsNullOrWhiteSpace(x)).Select(Path.GetFullPath).ToArray();
        if (inputs.Length == 0) throw new ArgumentException("At least one --input PDF is required.");
        foreach (var input in inputs) if (!File.Exists(input)) throw new FileNotFoundException("PDF input was not found.", input);

        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var temporary = output + ".tmp-" + Guid.NewGuid().ToString("N") + ".pdf";
        try
        {
            using (var destination = new PdfDocument())
            {
                foreach (var input in inputs)
                {
                    using var source = PdfReader.Open(input, PdfDocumentOpenMode.Import);
                    foreach (var page in source.Pages) destination.AddPage(page);
                }
                destination.Save(temporary);
            }
            if (!LooksLikePdf(temporary)) throw new InvalidDataException("Merged PDF output is invalid or empty.");
            CommitTemporaryFile(temporary, output);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        Console.WriteLine(output);
    }

    /// <summary>
    /// Optimizes a PDF with trusted native tools when available, while remaining a safe no-op copy when they are absent.
    /// </summary>
    /// <param name="values">Parsed command-line values containing input/output paths and optional tool overrides.</param>
    private static void OptimizePdf(Dictionary<string, List<string>> values)
    {
        var input = Path.GetFullPath(Required(values, "input"));
        var output = Path.GetFullPath(Required(values, "output"));
        if (!File.Exists(input)) throw new FileNotFoundException("PDF input was not found.", input);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);

        var candidates = new List<(string Path, string Mode)> { (input, "original") };
        var work = Path.Combine(Path.GetTempPath(), "localgpt-pdf-optimize-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var qpdf = FindExecutable(Optional(values, "qpdf", string.Empty), "qpdf");
            if (qpdf is not null)
            {
                var qpdfOut = Path.Combine(work, "qpdf.pdf");
                var qpdfArgs = new[] { "--compress-streams=y", "--decode-level=generalized", "--recompress-flate", "--compression-level=9", "--object-streams=generate", "--optimize-images", "--jpeg-quality=82", input, qpdfOut };
                if (RunTool(qpdf, qpdfArgs) == 0 && LooksLikePdf(qpdfOut)) candidates.Add((qpdfOut, "qpdf"));
            }

            var smallestBeforeGs = candidates.OrderBy(x => new FileInfo(x.Path).Length).First();
            var ghostscript = FindExecutable(Optional(values, "ghostscript", string.Empty), OperatingSystem.IsWindows() ? "gswin64c" : "gs", "gswin32c", "gs");
            if (ghostscript is not null)
            {
                var gsOut = Path.Combine(work, "ghostscript.pdf");
                var gsArgs = new[] {
                    "-dNOPAUSE", "-dBATCH", "-dSAFER", "-sDEVICE=pdfwrite", "-dCompatibilityLevel=1.7",
                    "-dAutoRotatePages=/None", "-dDetectDuplicateImages=true", "-dCompressFonts=true", "-dSubsetFonts=true",
                    "-dCompressPages=true", "-dEmbedAllFonts=true", "-dPDFSETTINGS=/ebook", "-dDownsampleColorImages=true",
                    "-dColorImageResolution=150", "-dAutoFilterColorImages=false", "-dColorImageFilter=/DCTEncode", "-dJPEGQ=85",
                    "-dDownsampleGrayImages=true", "-dGrayImageResolution=150", "-dAutoFilterGrayImages=false", "-dGrayImageFilter=/DCTEncode",
                    "-dDownsampleMonoImages=true", "-dMonoImageResolution=300", $"-sOutputFile={gsOut}", smallestBeforeGs.Path
                };
                if (RunTool(ghostscript, gsArgs) == 0 && LooksLikePdf(gsOut)) candidates.Add((gsOut, smallestBeforeGs.Mode == "qpdf" ? "qpdf+ghostscript" : "ghostscript"));
            }

            var best = candidates.OrderBy(x => new FileInfo(x.Path).Length).First();
            var temporary = output + ".tmp-" + Guid.NewGuid().ToString("N") + ".pdf";
            File.Copy(best.Path, temporary, true);
            CommitTemporaryFile(temporary, output);
            Console.WriteLine($"mode={best.Mode};before={new FileInfo(input).Length};after={new FileInfo(output).Length};output={output}");
        }
        finally { try { Directory.Delete(work, true); } catch { } }
    }

    /// <summary>
    /// Locates a requested command from an explicit path, PATH, and common Homebrew locations.
    /// </summary>
    /// <param name="explicitPath">Optional explicit path to the executable; an empty value falls back to name-based discovery.</param>
    /// <param name="names">Candidate executable names to search in PATH and supported platform-specific locations.</param>
    /// <returns>The resolved absolute executable path, or <see langword="null"/> when no candidate can be found.</returns>
    private static string? FindExecutable(string explicitPath, params string[] names)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(explicitPath)) candidates.Add(explicitPath);
        foreach (var name in names.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (Path.IsPathRooted(name)) candidates.Add(name);
            var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                candidates.Add(Path.Combine(directory, name));
                if (OperatingSystem.IsWindows() && string.IsNullOrEmpty(Path.GetExtension(name))) candidates.Add(Path.Combine(directory, name + ".exe"));
            }
            if (OperatingSystem.IsMacOS())
            {
                candidates.Add(Path.Combine("/opt/homebrew/bin", name));
                candidates.Add(Path.Combine("/usr/local/bin", name));
            }
        }
        foreach (var candidate in candidates)
        {
            try { if (File.Exists(candidate)) return Path.GetFullPath(candidate); } catch { }
        }
        return null;
    }

    /// <summary>
    /// Runs a native helper with argument-list escaping handled by <see cref="ProcessStartInfo.ArgumentList"/>.
    /// </summary>
    /// <param name="executable">Resolved native helper executable to launch.</param>
    /// <param name="arguments">Arguments to pass as individual process argument-list entries.</param>
    /// <returns>The native helper process exit code.</returns>
    private static int RunTool(string executable, IEnumerable<string> arguments)
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {executable}.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        Task.WaitAll(stdout, stderr);
        if (process.ExitCode != 0)
        {
            var detail = string.Join(" | ", new[] { stdout.Result, stderr.Result }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
            if (!string.IsNullOrWhiteSpace(detail)) Console.Error.WriteLine(detail);
        }
        return process.ExitCode;
    }

    /// <summary>
    /// Performs a lightweight PDF signature/size check before a generated artifact is committed.
    /// </summary>
    /// <param name="path">Path to the candidate PDF file to validate.</param>
    /// <returns><see langword="true"/> when the file exists, has a plausible size, and begins with the PDF signature; otherwise <see langword="false"/>.</returns>
    private static bool LooksLikePdf(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length < 1024) return false;
        Span<byte> header = stackalloc byte[5];
        using var stream = File.OpenRead(path);
        return stream.Read(header) == header.Length && header.SequenceEqual("%PDF-"u8);
    }

    /// <summary>
    /// Creates SHA-256 for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
    /// </summary>
    /// <param name="directory">Directory value supplied to the program operation and used when producing its result.</param>
    /// <param name="outputPath">Output path value supplied to the program operation and used when producing its result.</param>
    private static void CreateSha256(string directory, string outputPath)
    {
        directory = Path.GetFullPath(directory); outputPath = Path.GetFullPath(outputPath);
        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);
        var outputFileName = Path.GetFileName(outputPath);
        var lines = new List<string>();
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly).OrderBy(Path.GetFileName, StringComparer.Ordinal))
        {
            if (string.Equals(Path.GetFileName(file), outputFileName, StringComparison.OrdinalIgnoreCase)) continue;
            using var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.SequentialScan);
            var hash = Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
            lines.Add($"{hash}  {Path.GetFileName(file)}");
        }
        File.WriteAllLines(outputPath, lines, new UTF8Encoding(false));
        Console.WriteLine(outputPath);
    }
}
