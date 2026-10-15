using System.Formats.Tar;
using System.Globalization;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Filters;

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
            if (args.Length == 0) throw new ArgumentException("A command is required: capabilities, tar, deb, sha256, pdf-merge, pdf-verify, or pdf-optimize.");
            var command = args[0].ToLowerInvariant();
            var values = Parse(args.Skip(1).ToArray());
            switch (command)
            {
                case "capabilities":
                    if (values.Count != 0) throw new ArgumentException("The capabilities command does not accept options.");
                    Console.WriteLine("pdf-merge-input-list");
                    break;
                case "tar": CreateTarGz(Required(values, "source"), Required(values, "output"), Optional(values, "root", string.Empty), Multi(values, "executable")); break;
                case "deb": CreateDeb(values); break;
                case "sha256": CreateSha256(Required(values, "directory"), Required(values, "output")); break;
                case "pdf-merge": MergePdf(values); break;
                case "pdf-verify": VerifyPdf(values); break;
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
    /// <param name="values">Parsed command-line values containing repeated input paths or a UTF-8 input-list manifest, and the output path.</param>
    private static void MergePdf(Dictionary<string, List<string>> values)
    {
        var output = Path.GetFullPath(Required(values, "output"));
        var directInputs = Multi(values, "input");
        var inputLists = Multi(values, "input-list");
        if (inputLists.Count > 1) throw new ArgumentException("Only one --input-list manifest is supported per PDF merge.");
        if (directInputs.Count > 0 && inputLists.Count > 0) throw new ArgumentException("Use either --input or --input-list, not both.");
        var inputPaths = inputLists.Count == 1
            ? File.ReadAllLines(Path.GetFullPath(inputLists[0]), Encoding.UTF8)
            : directInputs.ToArray();
        if (inputPaths.Length == 0 || inputPaths.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("At least one non-empty PDF input path is required.");
        var inputs = inputPaths.Select(Path.GetFullPath).ToArray();
        foreach (var input in inputs)
        {
            if (!File.Exists(input)) throw new FileNotFoundException("PDF input was not found.", input);
            if (string.Equals(input, output, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                throw new ArgumentException("The PDF output must not overwrite one of the input chunks.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var temporary = output + ".tmp-" + Guid.NewGuid().ToString("N") + ".pdf";
        try
        {
            long expectedPages = 0;
            using (var destination = new PdfDocument())
            {
                // Documentation PDFs are distributable artifacts even when Visual Studio builds Debug.
                destination.Options.Layout = PdfWriterLayout.Compact;
                destination.Options.CompressContentStreams = true;
                destination.Options.FlateEncodeMode = PdfFlateEncodeMode.BestCompression;
                foreach (var input in inputs)
                {
                    using var source = PdfReader.Open(input, PdfDocumentOpenMode.Import);
                    if (source.Pages.Count == 0) throw new InvalidDataException($"PDF merge input contains no pages: {input}");
                    expectedPages += source.Pages.Count;
                    foreach (var page in source.Pages) destination.AddPage(page);
                }
                var savedBytes = CompressImportedPdfStreams(destination);
                if (savedBytes > 0) Console.WriteLine($"PDFsharp merged-stream compression saved {savedBytes} byte(s) before compact serialization.");
                destination.Save(temporary);
            }
            // Verify a fully written, closed file rather than trusting its signature or save call.
            // All imported pages must be represented by a document that PDFsharp can reopen.
            var verifiedPages = ReadPdfPageCount(temporary);
            if (verifiedPages != expectedPages)
                throw new InvalidDataException($"Merged PDF page count mismatch: expected {expectedPages}, found {verifiedPages}. The existing output was not replaced.");
            var verifiedBytes = new FileInfo(temporary).Length;
            CommitTemporaryFile(temporary, output);
            Console.WriteLine($"PDFsharp merge verified {inputs.Length} parts, {verifiedPages} pages, {verifiedBytes} bytes.");
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        Console.WriteLine(output);
    }

    /// <summary>
    /// Validates a complete PDF via the PDFsharp import reader and reports its page count and size.
    /// </summary>
    private static void VerifyPdf(Dictionary<string, List<string>> values)
    {
        var input = Path.GetFullPath(Required(values, "input"));
        var pageCount = ReadPdfPageCount(input);
        var expected = Optional(values, "expected-pages", string.Empty);
        if (!string.IsNullOrWhiteSpace(expected))
        {
            if (!long.TryParse(expected, NumberStyles.None, CultureInfo.InvariantCulture, out var expectedCount) || expectedCount < 1)
                throw new ArgumentException("--expected-pages must be a positive integer.");
            if (pageCount != expectedCount) throw new InvalidDataException($"PDF page count mismatch: expected {expectedCount}, found {pageCount}.");
        }
        Console.WriteLine($"pages={pageCount};bytes={new FileInfo(input).Length};file={input}");
    }

    /// <summary>
    /// Parses a PDF's cross references and page tree rather than trusting its byte length.
    /// </summary>
    private static int ReadPdfPageCount(string path)
    {
        if (!LooksLikePdf(path)) throw new InvalidDataException($"PDF is missing its signature or final EOF marker: {path}");
        using var document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        var pages = document.Pages.Count;
        if (pages < 1) throw new InvalidDataException($"PDF contains no pages: {path}");
        return pages;
    }

    /// <summary>
    /// Losslessly compresses unfiltered imported PDF streams when deflating actually saves space.
    /// Already filtered streams (including browser JPEGs) and decode parameters are left untouched.
    /// </summary>
    /// <param name="document">Merged PDF document containing imported page resources.</param>
    /// <returns>The total number of stream bytes removed, excluding dictionary serialization overhead.</returns>
    private static long CompressImportedPdfStreams(PdfDocument document)
    {
        var encoder = new FlateDecode();
        long savedBytes = 0;
        foreach (var item in document.Internals.GetAllObjects())
        {
            if (item is not PdfDictionary dictionary || dictionary.Stream is null ||
                dictionary.Elements.ContainsKey("/Filter") || dictionary.Elements.ContainsKey("/DecodeParms")) continue;

            var raw = dictionary.Stream.Value;
            if (raw.Length < 256) continue;
            var compressed = encoder.Encode(raw, PdfFlateEncodeMode.BestCompression);
            // Account for the additional /Filter name so incompressible data is not inflated.
            if (compressed.Length + 40 >= raw.Length) continue;
            dictionary.Stream.Value = compressed;
            dictionary.Elements["/Filter"] = new PdfName("/FlateDecode");
            savedBytes += raw.Length - compressed.Length;
        }
        return savedBytes;
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
        var expectedPages = ReadPdfPageCount(input);
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
                if (RunTool(qpdf, qpdfArgs) == 0 && HasMatchingPdfPages(qpdfOut, expectedPages)) candidates.Add((qpdfOut, "qpdf"));
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
                if (RunTool(ghostscript, gsArgs) == 0 && HasMatchingPdfPages(gsOut, expectedPages)) candidates.Add((gsOut, smallestBeforeGs.Mode == "qpdf" ? "qpdf+ghostscript" : "ghostscript"));
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
    /// Rejects lossy optimizer results that are unreadable or silently lose pages.
    /// </summary>
    private static bool HasMatchingPdfPages(string candidate, int expectedPages)
    {
        try
        {
            var actualPages = ReadPdfPageCount(candidate);
            if (actualPages == expectedPages) return true;
            Console.Error.WriteLine($"PDF optimizer output rejected: expected {expectedPages} pages, found {actualPages} in '{candidate}'.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"PDF optimizer output rejected: '{candidate}' could not be read ({ex.Message}).");
        }
        return false;
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
    /// Performs a bounded-memory PDF signature and final EOF check, even for multi-gigabyte outputs.
    /// </summary>
    /// <param name="path">Path to the candidate PDF file to validate.</param>
    /// <returns><see langword="true"/> when the file has a PDF header and an EOF marker near its end.</returns>
    private static bool LooksLikePdf(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length < 1024) return false;
        using var stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[5];
        stream.ReadExactly(header);
        if (!header.SequenceEqual("%PDF-"u8)) return false;
        var tailLength = (int)Math.Min(info.Length, 65536L);
        var tail = new byte[tailLength];
        stream.Seek(-tailLength, SeekOrigin.End);
        stream.ReadExactly(tail);
        return tail.AsSpan().LastIndexOf("%%EOF"u8) >= 0;
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
