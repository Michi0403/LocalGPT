using System.Formats.Tar;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

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
            if (args.Length == 0) throw new ArgumentException("A command is required: tar, deb, or sha256.");
            var command = args[0].ToLowerInvariant();
            var values = Parse(args.Skip(1).ToArray());
            switch (command)
            {
                case "tar": CreateTarGz(Required(values, "source"), Required(values, "output"), Optional(values, "root", string.Empty), Multi(values, "executable")); break;
                case "deb": CreateDeb(values); break;
                case "sha256": CreateSha256(Required(values, "directory"), Required(values, "output")); break;
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
            using var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.SequentialScan);
            using var gzip = new GZipStream(file, CompressionLevel.SmallestSize, leaveOpen: false);
            using var tar = new TarWriter(gzip, TarEntryFormat.Pax, leaveOpen: false);
            WriteTree(tar, sourceDirectory, rootName, executableSet);
            File.Move(temp, outputPath, true);
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
                using var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.SequentialScan);
                stream.Write(Encoding.ASCII.GetBytes("!<arch>\n"));
                WriteArMember(stream, "debian-binary", new MemoryStream(Encoding.ASCII.GetBytes("2.0\n"), writable: false));
                using (var member = File.OpenRead(controlTar)) WriteArMember(stream, "control.tar.gz", member);
                using (var member = File.OpenRead(dataTar)) WriteArMember(stream, "data.tar.gz", member);
                File.Move(temp, output, true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            Console.WriteLine(output);
        }
        finally { try { Directory.Delete(work, true); } catch { } }
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
