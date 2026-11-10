using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Verifies staged source mutations and distributable archive contents independently of Council statements.</summary>
public sealed class CouncilDevelopmentWorkspaceService : ICouncilDevelopmentWorkspaceService
{
    /// <inheritdoc />
    public async Task<int> RecordStageBaselineAsync(string artifactRoot, string workspaceName, CancellationToken cancellationToken = default)
    {
        var root = ResolveStagedRoot(artifactRoot, workspaceName);
        var baseline = new CouncilRepositoryStageBaseline { WorkspaceName = workspaceName };
        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            CheckNoLinks(root, path);
            baseline.Files.Add(new CouncilStagedSourceFingerprint
            {
                RelativePath = NormalizePath(Path.GetRelativePath(root, path)),
                Sha256 = await FileHashAsync(path, cancellationToken).ConfigureAwait(false)
            });
        }
        baseline.Files.Sort((a, b) => StringComparer.Ordinal.Compare(a.RelativePath, b.RelativePath));
        var destination = BaselinePath(artifactRoot, workspaceName);
        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(output, baseline, cancellationToken: cancellationToken).ConfigureAwait(false);
        return baseline.Files.Count;
    }

    /// <inheritdoc />
    public async Task<CouncilRepositoryArtifactEvidence> VerifyStageArchiveAsync(string artifactRoot, string workspaceName, CancellationToken cancellationToken = default)
    {
        var root = ResolveStagedRoot(artifactRoot, workspaceName);
        var baselinePath = BaselinePath(artifactRoot, workspaceName);
        if (!File.Exists(baselinePath))
            return new() { Diagnostic = "This workspace has no approved repository-stage baseline." };
        await using var input = File.OpenRead(baselinePath);
        var baseline = await JsonSerializer.DeserializeAsync<CouncilRepositoryStageBaseline>(input, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (baseline is null || baseline.WorkspaceName != workspaceName)
            return new() { Diagnostic = "The repository-stage baseline does not match the selected workspace." };

        var original = baseline.Files.ToDictionary(item => item.RelativePath, item => item.Sha256, StringComparer.Ordinal);
        var current = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            CheckNoLinks(root, path);
            var relative = NormalizePath(Path.GetRelativePath(root, path));
            current.Add(relative, await FileHashAsync(path, cancellationToken).ConfigureAwait(false));
        }
        var changed = original.Keys.Union(current.Keys, StringComparer.Ordinal)
            .Where(key => !original.TryGetValue(key, out var former) ||
                          !current.TryGetValue(key, out var latest) ||
                          !string.Equals(former, latest, StringComparison.Ordinal))
            .OrderBy(key => key, StringComparer.Ordinal).ToList();
        if (changed.Count == 0)
            return new() { Diagnostic = "No file bytes changed since approved repository staging." };

        var zipPath = Path.Combine(artifactRoot, workspaceName + "-workspace.zip");
        if (!File.Exists(zipPath))
            return new() { Diagnostic = "The changed workspace has no refreshed source ZIP.", ChangedPaths = changed };
        using var archive = ZipFile.OpenRead(zipPath);
        var prefix = workspaceName + "/";
        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = NormalizePath(entry.FullName);
            if (!name.StartsWith(prefix, StringComparison.Ordinal))
                return new() { Diagnostic = "Source ZIP contains a file outside its expected workspace root.", ChangedPaths = changed };
            if (name.EndsWith("/", StringComparison.Ordinal)) continue;
            var relative = name[prefix.Length..];
            if (relative.Split('/').Any(part => part is "." or "..") || !entries.TryAdd(relative, entry))
                return new() { Diagnostic = "Source ZIP contains an ambiguous or unsafe file path.", ChangedPaths = changed };
        }
        if (entries.Count != current.Count)
            return new() { Diagnostic = "Source ZIP file inventory does not match the current workspace.", ChangedPaths = changed };
        foreach (var item in current)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!entries.TryGetValue(item.Key, out var entry))
                return new() { Diagnostic = "Source ZIP is missing a staged workspace file.", ChangedPaths = changed };
            await using var stream = entry.Open();
            var archivedHash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
            if (!string.Equals(archivedHash, item.Value, StringComparison.Ordinal))
                return new() { Diagnostic = "Source ZIP contains stale bytes; refresh it after the last edit.", ChangedPaths = changed };
        }
        return new()
        {
            Completed = true,
            Diagnostic = "Changed source and the complete matching ZIP were verified. Build and PowerShell checks remain separate evidence.",
            ChangedPaths = changed,
            DownloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(Path.GetFileName(zipPath))}"
        };
    }

    /// <summary>Restricts baseline operations to the approved repository staging namespace.</summary>
    /// <param name="artifactRoot">Artifact root owned by LocalGPT.</param>
    /// <param name="workspaceName">Caller-supplied stage name.</param>
    /// <returns>The canonical staged directory.</returns>
    private string ResolveStagedRoot(string artifactRoot, string workspaceName)
    {
        if (!workspaceName.StartsWith("repository-stage-", StringComparison.Ordinal) ||
            workspaceName.Length != "repository-stage-".Length + 32 ||
            !Guid.TryParseExact(workspaceName["repository-stage-".Length..], "N", out _))
            throw new ArgumentException("An approved repository-stage workspaceName is required.", nameof(workspaceName));
        var path = Path.Combine(artifactRoot, workspaceName);
        if (!Directory.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new DirectoryNotFoundException("The selected staged workspace was not found.");
        return path;
    }

    /// <summary>Resolves the protected baseline path outside the editable workspace.</summary>
    /// <param name="artifactRoot">Artifact root owned by LocalGPT.</param>
    /// <param name="workspaceName">Validated stage name.</param>
    /// <returns>Path to the immutable stage fingerprint file.</returns>
    private string BaselinePath(string artifactRoot, string workspaceName)
    {
        _ = ResolveStagedRoot(artifactRoot, workspaceName);
        return Path.Combine(artifactRoot, workspaceName + ".stage-baseline.json");
    }

    /// <summary>Rejects linked directories and files before calculating or using their bytes.</summary>
    /// <param name="workspaceRoot">Selected workspace root.</param>
    /// <param name="file">Selected file path.</param>
    private void CheckNoLinks(string workspaceRoot, string file)
    {
        var current = file;
        while (!string.Equals(current, workspaceRoot, StringComparison.OrdinalIgnoreCase))
        {
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("The staged workspace contains a symbolic link or reparse point.");
            current = Path.GetDirectoryName(current) ?? throw new InvalidDataException("File path left its staged workspace.");
        }
    }

    /// <summary>Creates stable archive and source-relative paths.</summary>
    /// <param name="path">Path to normalize.</param>
    /// <returns>A forward-slash path.</returns>
    private string NormalizePath(string path) => path.Replace('\\', '/');

    /// <summary>Produces a streaming SHA-256 without keeping source bytes in application logs.</summary>
    /// <param name="path">Path to read.</param>
    /// <param name="cancellationToken">Cancels the stream.</param>
    /// <returns>Uppercase content digest.</returns>
    private async Task<string> FileHashAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
    }
}
