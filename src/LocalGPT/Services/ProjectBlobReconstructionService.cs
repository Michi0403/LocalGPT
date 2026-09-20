using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Reconstructs ordered base64 chunks into a hash-verified archive that enters the normal quarantine workflow.</summary>
public sealed class ProjectBlobReconstructionService(
    IChatUploadWorkspaceService workspaces,
    LocalGptCatalogService catalog,
    ILogger<ProjectBlobReconstructionService> logger) : IProjectBlobReconstructionService
{
    private readonly string Root = LocalGptApplicationDataPaths.ResolveUserPath("BlobReconstruction");
    private readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<ProjectBlobSessionSnapshot> StartAsync(ProjectBlobManifest manifest, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(manifest);
            if (manifest.Files.Count == 0 || manifest.Files.Count > catalog.MaxFiles) throw new InvalidDataException("Blob manifest file count is outside the configured bounds.");
            var total = manifest.Files.Sum(file => file.Length);
            if (total > catalog.MaxTotalFileBytes) throw new InvalidDataException("Blob manifest exceeds MaxTotalFileBytes.");
            foreach (var file in manifest.Files)
            {
                ValidateRelativePath(file.RelativePath);
                if (file.Length < 0 || file.Length > catalog.MaxSingleFileBytes) throw new InvalidDataException($"{file.RelativePath}: invalid declared length.");
                if (file.ChunkCount <= 0 || file.ChunkCount > 10000) throw new InvalidDataException($"{file.RelativePath}: invalid chunk count.");
                if (!IsSha256(file.Sha256)) throw new InvalidDataException($"{file.RelativePath}: invalid SHA-256.");
            }
            var canonical = CanonicalManifest(manifest);
            var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
            if (!string.IsNullOrWhiteSpace(manifest.ManifestSha256) && !digest.Equals(manifest.ManifestSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Blob manifest SHA-256 does not match the canonical manifest.");
            manifest.ManifestSha256 = digest;

            var id = Guid.NewGuid();
            var dir = Path.Combine(Root, id.ToString("N"));
            Directory.CreateDirectory(Path.Combine(dir, "chunks"));
            await File.WriteAllTextAsync(Path.Combine(dir, "manifest.json"), JsonSerializer.Serialize(manifest, catalog.JsonOptions), cancellationToken).ConfigureAwait(false);
            var snapshot = new ProjectBlobSessionSnapshot { SessionId = id, Name = manifest.Name, DeclaredFileCount = manifest.Files.Count, DeclaredBytes = total, CreatedAtUtc = DateTimeOffset.UtcNow };
            await SaveSnapshotAsync(dir, snapshot, cancellationToken).ConfigureAwait(false);
            return snapshot;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Starting blob reconstruction failed.");
            throw;
        }
    }

    public async Task<ProjectBlobSessionSnapshot> AddChunkAsync(Guid sessionId, string relativePath, int chunkIndex, string base64Data, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateRelativePath(relativePath);
            await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var (dir, manifest, snapshot) = await LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
                if (snapshot.Finalized) throw new InvalidOperationException("Blob reconstruction session is already finalized.");
                var file = manifest.Files.SingleOrDefault(item => string.Equals(Normalize(item.RelativePath), Normalize(relativePath), StringComparison.OrdinalIgnoreCase))
                    ?? throw new KeyNotFoundException("The chunk path is not declared in the manifest.");
                if (chunkIndex < 0 || chunkIndex >= file.ChunkCount) throw new ArgumentOutOfRangeException(nameof(chunkIndex));
                byte[] bytes;
                try { bytes = Convert.FromBase64String(base64Data); } catch (FormatException ex) { throw new InvalidDataException("Chunk data is not valid base64.", ex); }
                if (bytes.Length > Math.Min(catalog.MaxSingleFileBytes, 8 * 1024 * 1024)) throw new InvalidDataException("One blob chunk exceeds the bounded chunk size.");
                var chunkDir = Path.Combine(dir, "chunks", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(relativePath)))));
                Directory.CreateDirectory(chunkDir);
                var chunkPath = Path.Combine(chunkDir, chunkIndex.ToString("D6") + ".chunk");
                if (File.Exists(chunkPath)) throw new InvalidOperationException("Duplicate chunk index received for this file.");
                await File.WriteAllBytesAsync(chunkPath, bytes, cancellationToken).ConfigureAwait(false);
                snapshot.ReceivedChunkCount++;
                await SaveSnapshotAsync(dir, snapshot, cancellationToken).ConfigureAwait(false);
                return snapshot;
            }
            finally { Gate.Release(); }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Adding blob reconstruction chunk failed for session {SessionId}.", sessionId);
            throw;
        }
    }

    public async Task<ProjectBlobSessionSnapshot> FinalizeAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var (dir, manifest, snapshot) = await LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
                if (snapshot.Finalized) return snapshot;
                var assembled = Path.Combine(dir, "assembled");
                if (Directory.Exists(assembled)) Directory.Delete(assembled, true);
                Directory.CreateDirectory(assembled);
                foreach (var file in manifest.Files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var chunkDir = Path.Combine(dir, "chunks", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(file.RelativePath)))));
                    var chunks = Directory.Exists(chunkDir) ? Directory.EnumerateFiles(chunkDir, "*.chunk").OrderBy(path => path, StringComparer.Ordinal).ToList() : [];
                    if (chunks.Count != file.ChunkCount) throw new InvalidDataException($"{file.RelativePath}: expected {file.ChunkCount} chunks but received {chunks.Count}.");
                    var destination = Path.GetFullPath(Path.Combine(assembled, Normalize(file.RelativePath)));
                    if (!destination.StartsWith(Path.GetFullPath(assembled) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Reconstruction path escaped the session root.");
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    var output = File.Create(destination);
                    await using var configuredOutput = output.ConfigureAwait(false);
                    foreach (var chunk in chunks)
                    {
                        var input = File.OpenRead(chunk);
                        await using var configuredInput = input.ConfigureAwait(false);
                        await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
                    }
                    await output.FlushAsync(cancellationToken).ConfigureAwait(false);
                    if (output.Length != file.Length) throw new InvalidDataException($"{file.RelativePath}: reconstructed byte length does not match the manifest.");
                    output.Position = 0;
                    var digest = Convert.ToHexString(await SHA256.HashDataAsync(output, cancellationToken).ConfigureAwait(false));
                    if (!digest.Equals(file.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"{file.RelativePath}: reconstructed SHA-256 does not match the manifest.");
                }

                var archivePath = Path.Combine(dir, "reconstructed.zip");
                if (File.Exists(archivePath)) File.Delete(archivePath);
                ZipFile.CreateFromDirectory(assembled, archivePath, CompressionLevel.Optimal, includeBaseDirectory: false);
                var archiveBytes = await File.ReadAllBytesAsync(archivePath, cancellationToken).ConfigureAwait(false);
                var result = await workspaces.CreateWorkspaceAsync(manifest.Name, [new ChatUploadWorkspaceInputFile(
                    string.IsNullOrWhiteSpace(manifest.Name) ? "reconstructed-project.zip" : SanitizeArchiveName(manifest.Name) + ".zip",
                    "application/zip", archiveBytes.LongLength, archiveBytes)], cancellationToken).ConfigureAwait(false);
                snapshot.Finalized = true;
                snapshot.WorkspaceName = result.WorkspaceName;
                await SaveSnapshotAsync(dir, snapshot, cancellationToken).ConfigureAwait(false);
                return snapshot;
            }
            finally { Gate.Release(); }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Finalizing blob reconstruction failed for session {SessionId}.", sessionId);
            throw;
        }
    }

    private async Task<(string Dir, ProjectBlobManifest Manifest, ProjectBlobSessionSnapshot Snapshot)> LoadAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var dir = Path.Combine(Root, sessionId.ToString("N"));
        if (!Directory.Exists(dir)) throw new KeyNotFoundException("Blob reconstruction session was not found.");
        var manifest = JsonSerializer.Deserialize<ProjectBlobManifest>(await File.ReadAllTextAsync(Path.Combine(dir, "manifest.json"), cancellationToken).ConfigureAwait(false), catalog.JsonOptions) ?? throw new InvalidDataException("Blob reconstruction manifest is invalid.");
        var snapshot = JsonSerializer.Deserialize<ProjectBlobSessionSnapshot>(await File.ReadAllTextAsync(Path.Combine(dir, "state.json"), cancellationToken).ConfigureAwait(false), catalog.JsonOptions) ?? throw new InvalidDataException("Blob reconstruction state is invalid.");
        return (dir, manifest, snapshot);
    }

    private async Task SaveSnapshotAsync(string dir, ProjectBlobSessionSnapshot snapshot, CancellationToken cancellationToken)
    {
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "state.json"), JsonSerializer.Serialize(snapshot, catalog.JsonOptions), cancellationToken).ConfigureAwait(false);
            logger.LogDebug("Saved blob reconstruction state for session {SessionId}.", snapshot.SessionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Saving blob reconstruction state failed for session {SessionId}.", snapshot.SessionId);
            throw;
        }
    }

    private string CanonicalManifest(ProjectBlobManifest manifest)
    {
        try
        {
            var result = string.Join("\n", manifest.Files.OrderBy(file => Normalize(file.RelativePath), StringComparer.Ordinal).Select(file =>
                $"{Normalize(file.RelativePath)}|{file.Length}|{file.ChunkCount}|{file.Sha256.ToUpperInvariant()}|{file.ContentType.Trim()}"));
            logger.LogDebug("Built canonical blob manifest for {FileCount} file(s).", manifest.Files.Count);
            return result;
        }
        catch (Exception ex) { logger.LogError(ex, "Building canonical blob manifest failed."); throw; }
    }

    private bool IsSha256(string value)
    {
        try
        {
            var result = value?.Length == 64 && value.All(Uri.IsHexDigit);
            logger.LogDebug("Validated one SHA-256 manifest field: {Valid}.", result);
            return result;
        }
        catch (Exception ex) { logger.LogError(ex, "Validating SHA-256 manifest field failed."); throw; }
    }

    private string Normalize(string path)
    {
        try
        {
            var result = path.Replace('\\', '/').TrimStart('/');
            logger.LogDebug("Normalized one bounded blob path.");
            return result;
        }
        catch (Exception ex) { logger.LogError(ex, "Normalizing blob path failed."); throw; }
    }

    private void ValidateRelativePath(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path)) throw new InvalidDataException("A safe relative path is required.");
            var normalized = Normalize(path);
            if (normalized.Split('/').Any(segment => segment is "" or "." or "..")) throw new InvalidDataException("Blob path contains an unsafe segment.");
            logger.LogDebug("Validated one bounded blob relative path.");
        }
        catch (Exception ex) { logger.LogError(ex, "Validating blob relative path failed."); throw; }
    }

    private string SanitizeArchiveName(string name)
    {
        try
        {
            var invalid = Path.GetInvalidFileNameChars().ToHashSet();
            var value = new string(name.Trim().Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            var result = value[..Math.Min(80, value.Length)];
            logger.LogDebug("Sanitized one reconstructed archive name.");
            return result;
        }
        catch (Exception ex) { logger.LogError(ex, "Sanitizing reconstructed archive name failed."); throw; }
    }

}
