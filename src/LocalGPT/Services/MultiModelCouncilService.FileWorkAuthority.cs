using System.Security.Cryptography;
using System.Text.Json;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Collects technical evidence before a source manifest is proposed, using the existing local knowledge and public-source inspection routes.</summary>
public sealed partial class MultiModelCouncilService
{
    /// <summary>Builds a run-owned evidence snapshot independently of architect or reviewer output.</summary>
    /// <param name="result">Owning Council result and project identity.</param>
    /// <param name="request">Original request and presentation callbacks.</param>
    /// <param name="researchPolicy">Resolved automatic-function policy of the configured planning role.</param>
    /// <param name="goal">Original user goal.</param>
    /// <param name="targets">Explicit normalized user targets.</param>
    /// <param name="excludeDevExpress">Whether the user explicitly excluded DevExpress.</param>
    /// <param name="round">Current logical round.</param>
    /// <param name="phase">Configured planning phase.</param>
    /// <param name="cancellationToken">Stops local reads and remote inspection.</param>
    /// <returns>Server-generated requirements and attributed observations; missing evidence remains explicit.</returns>
    private async Task<object> BuildFileWorkAuthorityAsync(MultiModelCouncilResult result, MultiModelCouncilRequest request,
        CouncilAutomaticFunctionPolicyResolution researchPolicy, string goal, IReadOnlyList<string> targets,
        bool excludeDevExpress, int round, string phase, CancellationToken cancellationToken)
    {
        try
        {
            var observations = new List<object>();
            var instructions = runtimePolicy.GetString("CouncilFileWorkTechnicalAuthorityInstruction");
            if (targets.Count == 1)
            {
                var channel = targets[0][3..];
                // The default installation flag is not proof of compatibility or successful validation.
                var installations = await projectMaintenance.GetCompilerInstallationsAsync(cancellationToken).ConfigureAwait(false);
                var sdkInventory = installations.Where(item => string.Equals(item.KnowledgeProfileKey, "dotnet-sdk", StringComparison.OrdinalIgnoreCase))
                    .Select(item => new { item.Id, item.Name, item.Version, item.IsEnabled, item.IsDefaultForLanguage,
                        item.LastValidatedAtUtc, item.LastValidationSucceeded,
                        MatchesRequestedChannel = item.Version.StartsWith(channel + ".", StringComparison.OrdinalIgnoreCase) }).ToList();
                observations.Add(new { Kind = "RecordedLocalSdkInventory", Items = sdkInventory,
                    MatchingValidatedSdk = sdkInventory.Any(item => item.IsEnabled && item.LastValidationSucceeded && item.MatchesRequestedChannel),
                    InvalidDefault = sdkInventory.Any(item => item.IsEnabled && item.IsDefaultForLanguage && !item.LastValidationSucceeded) });

                var entries = await knowledgeService.GetEntriesAsync(includeArchived: false,
                    take: runtimePolicy.GetJson<ServiceQueryRuntimeParameters>("ServiceQueryRuntimeParametersJson").KnowledgeMaximum,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                var references = entries.Where(entry => entry.IsUserApproved && entry.ReviewStatus == "Current" &&
                        entry.VerificationStatus == "SourceBacked" && string.IsNullOrWhiteSpace(entry.StalenessReason) &&
                        (entry.Topic.Contains(".NET", StringComparison.OrdinalIgnoreCase) || entry.Topic.Contains("Blazor", StringComparison.OrdinalIgnoreCase)))
                    .OrderByDescending(entry => entry.Topic.Contains("source map", StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(entry => entry.LastVerifiedAtUtc).Take(4)
                    .Select(entry => new { entry.Id, entry.Topic, entry.Source, entry.SourceHash, entry.LastVerifiedAtUtc,
                        ReferenceExcerpt = entry.Content[..Math.Min(480, entry.Content.Length)] }).ToList();
                observations.Add(new { Kind = "ApprovedLocalKnowledgeReferences", Items = references });

                const string inspect = "localgpt.knowledge.remote.inspect";
                if (researchPolicy.Enabled && (!researchPolicy.IsStrict ||
                    researchPolicy.AutomaticFunctionAllowList?.Contains(inspect, StringComparer.OrdinalIgnoreCase) == true))
                {
                    var releaseUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        runtimePolicy.GetString("CouncilFileWorkDotNetReleaseMetadataUrl"), channel);
                    observations.Add(await InspectFileWorkTechnicalSourceAsync(result, request, round, phase, releaseUrl,
                        channel, releaseMetadata: true, cancellationToken).ConfigureAwait(false));
                    observations.Add(await InspectFileWorkTechnicalSourceAsync(result, request, round, phase,
                        runtimePolicy.GetString("CouncilFileWorkDotNetDocumentationUrl"), channel,
                        releaseMetadata: false, cancellationToken).ConfigureAwait(false));
                }
                else observations.Add(new { Kind = "UpstreamResearch", Status = "NotPermittedByPlanningRole" });
            }
            var snapshot = new { Kind = "CoordinatorTechnicalEvidence", CapturedAtUtc = DateTime.UtcNow,
                GoalSha256 = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(goal))),
                RequiredDotNetTargets = targets, ExcludeDevExpress = excludeDevExpress, Instruction = instructions,
                Observations = observations };
            // Record the exact server-generated snapshot separately from model content in the existing spool.
            if (targets.Count == 1)
            {
                var receipt = JsonSerializer.Serialize(snapshot);
                var step = new MultiModelCouncilStep { ModelName = "LocalGPT: technical evidence gate", Round = round,
                    Phase = phase, Role = "Technical evidence", CouncilMembers = result.ModelNames.ToList(),
                    Content = receipt, VisibleContent = receipt, FileWorkReceiptJson = receipt,
                    StartedAtUtc = snapshot.CapturedAtUtc, CompletedAtUtc = DateTime.UtcNow };
                MultiModelCouncilServiceAddOrderedStep(result, step, logger);
                request.StepCompleted?.Invoke(step);
                request.ProgressMessage?.Invoke("Technical evidence snapshot recorded before source planning.");
            }
            return snapshot;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Technical evidence collection failed for Council {RunId}; user requirements remain authoritative.", result.RunId);
            return new { Kind = "CoordinatorTechnicalEvidence", RequiredDotNetTargets = targets, ExcludeDevExpress = excludeDevExpress,
                Status = "EvidenceCollectionFailed", Instruction = runtimePolicy.GetString("CouncilFileWorkTechnicalAuthorityInstruction") };
        }
    }

    /// <summary>Uses a real registered inspection receipt and evaluates only the bounded cached bytes.</summary>
    /// <param name="result">Owning Council result.</param>
    /// <param name="request">Original Council request.</param>
    /// <param name="round">Current round.</param>
    /// <param name="phase">Current workflow phase.</param>
    /// <param name="url">Configured official source URL, never a model-generated URL.</param>
    /// <param name="channel">Requested .NET release channel.</param>
    /// <param name="releaseMetadata">Whether this source must contain matching release metadata.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>Attributed metadata or an explicit unavailable/invalid status, without inferring a release.</returns>
    private async Task<object> InspectFileWorkTechnicalSourceAsync(MultiModelCouncilResult result, MultiModelCouncilRequest request,
        int round, string phase, string url, string channel, bool releaseMetadata, CancellationToken cancellationToken)
    {
        var kind = releaseMetadata ? "UpstreamDotNetReleaseMetadata" : "UpstreamDotNetDocumentationReference";
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                return new { Kind = kind, SourceUrl = url, Status = "InvalidConfiguredUrl" };
            var outcome = await InvokeFileWorkFunctionAsync(result, request, round, phase, "localgpt.knowledge.remote.inspect",
                new { sourceUrl = url, sourceKind = "Web", maxFiles = 1, maxLinkedPages = 1 }, cancellationToken).ConfigureAwait(false);
            if (!outcome.Succeeded || outcome.HandlerDispatched != true || outcome.DescriptorReadOnly != true ||
                outcome.Value is not RemoteKnowledgeImportResult inspected)
                return new { Kind = kind, SourceUrl = url, outcome.OperationId, outcome.Status };
            var file = inspected.Files.SingleOrDefault(item => string.Equals(item.SourceUrl, uri.AbsoluteUri, StringComparison.Ordinal));
            // This is a small metadata/documentation read; the importer still owns network and file-size limits.
            if (file is null || file.Length <= 0 || file.Length > 8 * 1024 * 1024 || string.IsNullOrWhiteSpace(inspected.CacheRoot))
                return new { Kind = kind, SourceUrl = url, outcome.OperationId, Status = "MissingOrOversizedSource" };
            var root = Path.GetFullPath(inspected.CacheRoot);
            var path = Path.GetFullPath(Path.Combine(root, file.RelativePath));
            var relative = Path.GetRelativePath(root, path);
            if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                !File.Exists(path) || new FileInfo(path).Length != file.Length)
                return new { Kind = kind, SourceUrl = url, outcome.OperationId, Status = "CachedFileMismatch" };
            // The bytes are evidence, not instructions. Do not send a complete upstream repository to a model.
            var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            if (bytes.LongLength != file.Length)
                return new { Kind = kind, SourceUrl = url, outcome.OperationId, Status = "CachedFileChanged" };
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            if (!releaseMetadata)
            {
                var evidencePath = await PreserveFileWorkTechnicalEvidenceAsync(bytes, hash, cancellationToken).ConfigureAwait(false);
                var text = System.Text.Encoding.UTF8.GetString(bytes);
                return new { Kind = kind, SourceUrl = url, outcome.OperationId, Status = "ReferenceRetrieved", Sha256 = hash, EvidencePath = evidencePath,
                    CachedRelativePath = relative, inspected.ResolvedRevision, ReferenceExcerpt = text[..Math.Min(1200, text.Length)] };
            }
            using var json = JsonDocument.Parse(bytes);
            var metadata = json.RootElement;
            if (!metadata.TryGetProperty("channel-version", out var version) || version.GetString() != channel ||
                !metadata.TryGetProperty("latest-sdk", out var sdk) ||
                !(sdk.GetString()?.StartsWith(channel + ".", StringComparison.Ordinal) ?? false))
                return new { Kind = kind, SourceUrl = url, outcome.OperationId, Status = "ReleaseChannelMismatch", Sha256 = hash };
            var releaseEvidencePath = await PreserveFileWorkTechnicalEvidenceAsync(bytes, hash, cancellationToken).ConfigureAwait(false);
            return new { Kind = kind, SourceUrl = url, outcome.OperationId, Status = "MatchingReleaseMetadataRetrieved", Sha256 = hash, EvidencePath = releaseEvidencePath,
                CachedRelativePath = relative, inspected.ResolvedRevision, Channel = channel, LatestSdk = sdk.GetString(),
                LatestRelease = ReadFileWorkReleaseField(metadata, "latest-release"),
                SupportPhase = ReadFileWorkReleaseField(metadata, "support-phase"),
                ReleaseType = ReadFileWorkReleaseField(metadata, "release-type") };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Technical source inspection failed for Council {RunId} ({Kind}).", result.RunId, kind);
            return new { Kind = kind, SourceUrl = url, Status = "SourceUnavailableOrInvalid" };
        }
    }

    /// <summary>Retains exact source bytes under the existing LearningBase remote-source storage, independent of replaceable URL caches.</summary>
    /// <param name="bytes">Bounded retrieved bytes.</param>
    /// <param name="hash">SHA-256 identity computed from the bytes.</param>
    /// <param name="cancellationToken">Cancels the filesystem write or verification.</param>
    /// <returns>LearningBase-relative evidence path; no knowledge entry or approval has been created.</returns>
    private async Task<string> PreserveFileWorkTechnicalEvidenceAsync(byte[] bytes, string hash, CancellationToken cancellationToken)
    {
        var relative = Path.Combine("RemoteSources", "TechnicalEvidence", hash + ".bin");
        var directory = LocalGptApplicationDataPaths.ResolveUserPath("LearningBase", "RemoteSources", "TechnicalEvidence");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, hash + ".bin");
        if (!File.Exists(path))
        {
            var temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                await File.WriteAllBytesAsync(temporary, bytes, cancellationToken).ConfigureAwait(false);
                try { File.Move(temporary, path, overwrite: false); }
                catch (IOException) when (File.Exists(path)) { } // Another Council preserved the same content.
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        if (new FileInfo(path).Length != bytes.LongLength ||
            !string.Equals(Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false))), hash, StringComparison.Ordinal))
            throw new IOException("The retained technical evidence does not match the observed content hash.");
        return relative;
    }

    /// <summary>Reads an optional string from the official release metadata without inventing a missing value.</summary>
    /// <param name="metadata">Parsed matching-channel JSON object.</param>
    /// <param name="key">Official metadata property name.</param>
    /// <returns>Present string or null.</returns>
    private string? ReadFileWorkReleaseField(JsonElement metadata, string key) =>
        metadata.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
