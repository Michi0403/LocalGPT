using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Provides the shared, non-executing ZIP container/Office/OpenDocument/EPUB inspection boundary for source learning and uploads.</summary>
/// <param name="catalog">Database-backed limits for entries and expanded content.</param>
/// <param name="logger">Structured diagnostics without logging document content.</param>
public sealed class ArchivePackageService(LocalGptCatalogService catalog, ILogger<ArchivePackageService> logger) : IArchivePackageService
{
    /// <summary>Checks the container magic, including renamed ZIPs and Office/OpenDocument packages.</summary>
    /// <inheritdoc />
    public bool IsZipContainer(string path)
    {
        try
        {
            if (!File.Exists(path)) return false;
            using var stream = File.OpenRead(path);
            Span<byte> magic = stackalloc byte[4];
            if (stream.Read(magic) != magic.Length) return false;
            return magic[0] == 0x50 && magic[1] == 0x4b &&
                   ((magic[2] == 3 && magic[3] == 4) ||
                    (magic[2] == 5 && magic[3] == 6) ||
                    (magic[2] == 7 && magic[3] == 8));
        }
        catch (IOException ex)
        {
            logger.LogDebug(ex, "ZIP header could not be inspected.");
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "ZIP header access was denied.");
            return false;
        }
    }

    /// <summary>Classifies a ZIP container using its actual internal format markers.</summary>
    /// <inheritdoc />
    public string IdentifyFamily(string path)
    {
        try
        {
            if (!IsZipContainer(path)) return string.Empty;
            using var archive = ZipFile.OpenRead(path);
            var names = archive.Entries.Take(Math.Max(1, catalog.MaxZipEntries))
                .Select(entry => entry.FullName.Replace('\\', '/')).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (names.Contains("word/document.xml")) return "OfficeWord";
            if (names.Contains("xl/workbook.xml")) return "OfficeSpreadsheet";
            if (names.Contains("ppt/presentation.xml")) return "OfficePresentation";
            if (names.Contains("mimetype") && names.Contains("content.xml"))
            {
                var mimeEntry = archive.GetEntry("mimetype");
                if (mimeEntry is not null && mimeEntry.Length < 256)
                {
                    using var input = new StreamReader(mimeEntry.Open(), Encoding.ASCII, detectEncodingFromByteOrderMarks: false);
                    var mime = input.ReadToEnd();
                    if (mime.Contains("spreadsheet", StringComparison.OrdinalIgnoreCase)) return "OpenSpreadsheet";
                    if (mime.Contains("presentation", StringComparison.OrdinalIgnoreCase)) return "OpenPresentation";
                }
                return "OpenDocument";
            }
            if (names.Contains("META-INF/container.xml")) return "EPUB";
            return "ZipContainer";
        }
        catch (InvalidDataException ex)
        {
            logger.LogWarning(ex, "ZIP container format metadata is malformed.");
            return "InvalidZipContainer";
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "ZIP container format metadata could not be accessed.");
            return "InvalidZipContainer";
        }
    }

    /// <summary>Prevents extraction of symbolic links, devices, sockets and other special Unix entries.</summary>
    /// <inheritdoc />
    public bool IsUnsafeEntry(int externalAttributes)
    {
        try
        {
            var mode = (externalAttributes >> 16) & 0xF000;
            return mode != 0 && mode != 0x8000 && mode != 0x4000;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Determining archive entry type failed.");
            throw;
        }
    }

    /// <summary>Reads only bounded, meaningful text from recognized document parts and ordinary source archives.</summary>
    /// <inheritdoc />
    public async Task<ArchiveSemanticPage> ReadSemanticPageAsync(string path, long characterOffset, int maxCharacters, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsZipContainer(path)) throw new InvalidDataException("The selected file is not a ZIP-compatible container.");
            using var archive = ZipFile.OpenRead(path);
            if (archive.Entries.Count > catalog.MaxZipEntries)
                throw new InvalidDataException("Package exceeds the configured MaxZipEntries limit.");
            // Validate every entry before returning even the first page. Truncation of model context
            // must never allow hidden oversized or linked entries later in the ZIP central directory.
            long totalExpanded = 0;
            foreach (var candidate in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (IsUnsafeEntry(candidate.ExternalAttributes))
                    throw new InvalidDataException("Package contains a linked or special entry.");
                totalExpanded = checked(totalExpanded + candidate.Length);
                if (candidate.Length > catalog.MaxZipEntryBytes || totalExpanded > catalog.MaxExtractedBytes)
                    throw new InvalidDataException("Package exceeds configured expanded-content limits.");
                var normalizedName = candidate.FullName.Replace('\\', '/');
                if (normalizedName.StartsWith('/') || normalizedName.Split('/').Any(segment => segment is ".." or "."))
                    throw new InvalidDataException("Package contains an unsafe entry path.");
            }
            var family = IdentifyFamily(path);
            var remaining = Math.Clamp(maxCharacters, 1, Math.Max(1, catalog.MaxContextCharacters));
            var skip = Math.Max(0L, characterOffset);
            var hasMore = false;
            var text = new StringBuilder();
            long expanded = 0;
            var sharedStrings = new List<string>();
            if (family == "OfficeSpreadsheet")
            {
                var shared = archive.GetEntry("xl/sharedStrings.xml");
                if (shared is not null && shared.Length <= catalog.MaxZipEntryBytes && shared.Length <= catalog.MaxExtractedBytes)
                {
                    var xml = await ReadXmlAsync(shared, cancellationToken).ConfigureAwait(false);
                    sharedStrings = xml.Descendants().Where(item => item.Name.LocalName == "si")
                        .Select(item => string.Concat(item.Descendants().Where(node => node.Name.LocalName == "t").Select(node => node.Value)))
                        .ToList();
                }
            }
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(entry.Name)) continue;
                if (IsUnsafeEntry(entry.ExternalAttributes))
                    throw new InvalidDataException("The package contains an unsafe linked or special entry.");
                expanded = checked(expanded + entry.Length);
                if (entry.Length > catalog.MaxZipEntryBytes || expanded > catalog.MaxExtractedBytes)
                    throw new InvalidDataException("Package exceeds configured expanded-content limits.");
                var name = entry.FullName.Replace('\\', '/');
                if (name.StartsWith('/') || name.Split('/').Any(segment => segment is ".." or "."))
                    throw new InvalidDataException("The package contains an unsafe entry path.");
                if (!IsRelevantPart(family, name)) continue;
                string part;
                if (name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".xhtml", StringComparison.OrdinalIgnoreCase))
                {
                    var document = await ReadXmlAsync(entry, cancellationToken).ConfigureAwait(false);
                    part = family switch
                    {
                        "OfficeSpreadsheet" => ExtractSheet(document, sharedStrings),
                        "OfficeWord" => string.Join("\n", document.Descendants()
                            .Where(item => item.Name.LocalName == "p").Select(item => item.Value).Where(value => !string.IsNullOrWhiteSpace(value))),
                        "OfficePresentation" => string.Join(" ", document.Descendants()
                            .Where(item => item.Name.LocalName == "t").Select(item => item.Value)),
                        "OpenDocument" or "OpenSpreadsheet" or "OpenPresentation" or "EPUB" =>
                            string.Join("\n", document.Descendants().Where(item => item.Name.LocalName is "p" or "h" or "title")
                                .Select(item => item.Value).Where(value => !string.IsNullOrWhiteSpace(value))),
                        _ => document.Root?.Value ?? string.Empty
                    };
                }
                else
                {
                    var source = entry.Open();
                    await using var configuredSource = source.ConfigureAwait(false);
                    using var reader = new StreamReader(source, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                    // Read complete textual parts within the same configured entry budget, so offsets remain stable.
                    // The archive gate has already validated entry counts, sizes and total expanded bytes.
                    var builder = new StringBuilder();
                    var chunk = new char[8192];
                    long readCharacters = 0;
                    int readCount;
                    while ((readCount = await reader.ReadAsync(chunk.AsMemory(), cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        readCharacters += readCount;
                        if (readCharacters > catalog.MaxZipEntryBytes)
                            throw new InvalidDataException("Package text entry exceeded the configured reading budget.");
                        builder.Append(chunk, 0, readCount);
                    }
                    part = builder.ToString();
                }
                if (string.IsNullOrWhiteSpace(part)) continue;
                var section = $"\n[Package part: {name}]\n" + part;
                if (skip >= section.Length)
                {
                    skip -= section.Length;
                    continue;
                }
                // This conversion is safe after the comparison above established skip < section.Length.
                var sectionOffset = (int)skip;
                var count = Math.Min(remaining, section.Length - sectionOffset);
                text.Append(section.AsSpan(sectionOffset, count));
                remaining -= count;
                hasMore = count < section.Length - sectionOffset;
                skip = 0;
                if (remaining <= 0) break;
            }
            logger.LogInformation("Read bounded semantic text from {PackageFamily}; returned {CharacterCount} characters.", family, text.Length);
            return new ArchiveSemanticPage(text.ToString(), hasMore || remaining == 0);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reading ZIP-based package text failed; contents were omitted from diagnostics.");
            throw;
        }
    }

    /// <summary>Reads the first bounded semantic package page for safe document imports and chat upload context.</summary>
    /// <inheritdoc />
    public async Task<string> ReadSemanticTextAsync(string path, int maxCharacters, CancellationToken cancellationToken = default)
    {
        try
        {
            var page = await ReadSemanticPageAsync(path, 0, maxCharacters, cancellationToken).ConfigureAwait(false);
            return page.Text;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reading initial archive semantic text page failed.");
            throw;
        }
    }

    /// <summary>Restricts semantic extraction to document body parts or source-text parts in ordinary ZIPs.</summary>
    /// <param name="family">Detected package family.</param>
    /// <param name="name">Normalized ZIP entry path.</param>
    /// <returns>Whether the part carries relevant readable content.</returns>
    private bool IsRelevantPart(string family, string name)
    {
        try
        {
            if (family == "OfficeWord")
                return name.Equals("word/document.xml", StringComparison.OrdinalIgnoreCase) ||
                       (name.StartsWith("word/header", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) ||
                       (name.StartsWith("word/footer", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) ||
                       name.Equals("word/footnotes.xml", StringComparison.OrdinalIgnoreCase);
            if (family == "OfficeSpreadsheet")
                return name.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
            if (family == "OfficePresentation")
                return name.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
            if (family is "OpenDocument" or "OpenSpreadsheet" or "OpenPresentation")
                return name.Equals("content.xml", StringComparison.OrdinalIgnoreCase);
            if (family == "EPUB") return name.EndsWith(".xhtml", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
            return name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".py", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".java", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".toml", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Classifying ZIP package part failed.");
            throw;
        }
    }

    /// <summary>Parses XML with external entities disabled and a bounded document character budget.</summary>
    /// <param name="entry">Archive entry containing untrusted XML.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed XML structure.</returns>
    private async Task<XDocument> ReadXmlAsync(ZipArchiveEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            if (entry.Length > catalog.MaxZipEntryBytes || entry.Length > catalog.MaxExtractedBytes)
                throw new InvalidDataException("Package XML entry exceeds configured bounds.");
            var stream = entry.Open();
            await using var configuredStream = stream.ConfigureAwait(false);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                Async = true,
                MaxCharactersInDocument = Math.Max(1L, catalog.MaxZipEntryBytes)
            });
            return await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is XmlException or InvalidDataException)
        {
            logger.LogWarning(ex, "Package XML could not be parsed safely.");
            throw;
        }
    }

    /// <summary>Resolves shared-string, inline-string, formula and numeric worksheet cell values into a readable row sequence.</summary>
    /// <param name="document">Parsed worksheet XML.</param>
    /// <param name="sharedStrings">Workbook shared-string table.</param>
    /// <returns>Text with one line per worksheet row.</returns>
    private string ExtractSheet(XDocument document, IReadOnlyList<string> sharedStrings)
    {
        try
        {
            var builder = new StringBuilder();
            foreach (var row in document.Descendants().Where(item => item.Name.LocalName == "row"))
            {
                var cells = new List<string>();
                foreach (var cell in row.Elements().Where(item => item.Name.LocalName == "c"))
                {
                    var value = cell.Elements().FirstOrDefault(item => item.Name.LocalName == "v")?.Value ??
                                string.Concat(cell.Descendants().Where(item => item.Name.LocalName == "t").Select(item => item.Value));
                    if ((string?)cell.Attribute("t") == "s" && int.TryParse(value, out var index) && index >= 0 && index < sharedStrings.Count)
                        value = sharedStrings[index];
                    var formula = cell.Elements().FirstOrDefault(item => item.Name.LocalName == "f")?.Value;
                    cells.Add(string.IsNullOrEmpty(formula) ? value : $"{value} (formula: {formula})");
                }
                builder.AppendLine(string.Join("\t", cells));
            }
            return builder.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reading worksheet cells failed.");
            throw;
        }
    }
}
