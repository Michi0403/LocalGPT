using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>Turns stale-knowledge reports into durable human review and exact-source refresh operations.</summary>
public sealed class KnowledgeFreshnessReviewService(
    ICouncilKnowledgeService knowledge,
    IRemoteKnowledgeImportService remoteImporter,
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILocalGptVocabularyService vocabulary,
    ILogger<KnowledgeFreshnessReviewService> logger) : IKnowledgeFreshnessReviewService
{
    private const string ReviewOperationPrefix = "knowledge.freshness.review:";
    private const string RefreshOperationPrefix = "knowledge.freshness.refresh:";
    private const int MaximumApprovalSourceUrlLength = 1200;
    private readonly Regex UrlRegex = new(@"https?://[^\s<>""']+", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<KnowledgeFreshnessReviewResult> ReportAsync(KnowledgeFreshnessReportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.KnowledgeId == Guid.Empty)
                throw new ArgumentException("A stable knowledgeId is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.Reason))
                throw new ArgumentException("A concrete staleness reason is required.", nameof(request));

            var entry = await RequireKnowledgeAsync(request.KnowledgeId, cancellationToken).ConfigureAwait(false);
            var now = DateTime.UtcNow;
            entry.StalenessReason = Compact(request.Reason, 500);
            entry.StalenessDetectedAtUtc = now;
            entry.StalenessDetectedBy = Compact(request.DetectedBy, 160, "AI Council");
            entry.VerificationStatus = "StaleReported";
            entry.ReviewStatus = "NeedsUserReview";
            entry.IsUserApproved = false;
            await knowledge.SaveEntryAsync(entry, cancellationToken).ConfigureAwait(false);

            var sourceUrl = NormalizeSourceUrl(request.SourceUrl, entry);
            var fingerprint = Fingerprint(entry.Id, sourceUrl);
            var suggested = string.IsNullOrWhiteSpace(sourceUrl)
                ? "Keep current knowledge\nReject as outdated"
                : "Keep current knowledge\nReview exact source first\nRefresh exact source\nReject as outdated";
            var operationKey = ReviewOperationPrefix + entry.Id.ToString("N");
            var collaborationRequest = await EnsurePendingRequestAsync(new HumanCollaborationRequest
            {
                CouncilRunId = request.CouncilRunId,
                CorrelationId = $"knowledge-freshness:{entry.Id:N}:{now:yyyyMMddHHmmss}",
                OperationKey = operationKey,
                ParameterFingerprint = fingerprint,
                RequestKind = vocabulary.Get().HumanRequestGuidance,
                Title = $"Review possibly outdated knowledge: {Compact(entry.Topic, 90, "Untitled knowledge")}",
                Description = string.IsNullOrWhiteSpace(sourceUrl)
                    ? $"An AI reported this persisted knowledge as possibly outdated. Reason: {entry.StalenessReason}"
                    : $"An AI reported this persisted knowledge as possibly outdated. Reason: {entry.StalenessReason}\nExact source: {sourceUrl}",
                RiskLevel = "Low",
                Status = vocabulary.Get().HumanStatusPending,
                Source = string.IsNullOrWhiteSpace(sourceUrl) ? "Council knowledge" : "External URL (exact value in description)",
                RequestedBy = entry.StalenessDetectedBy,
                RequestedRole = "Knowledge reviewer",
                SuggestedResponsesText = suggested,
                ResponsePrompt = "Choose whether to keep this knowledge, inspect or refresh this exact source, or reject the entry as outdated.",
                AllowFreeText = true,
                RequestedAtUtc = now,
                UpdatedAtUtc = now,
                EarliestCouncilRound = 0,
                RequiredBeforeCompletion = false,
                IsSensitive = false
            }, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Knowledge entry {KnowledgeId} was marked for freshness review; source content was omitted.", entry.Id);
            return new KnowledgeFreshnessReviewResult
            {
                KnowledgeId = entry.Id,
                CollaborationRequestId = collaborationRequest.Id,
                Status = "ReviewQueued",
                SourceUrl = sourceUrl,
                Message = "The stale-knowledge report is visible in Chat, the ASCII console, and Approvals & team."
            };
    
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Knowledge freshness method ReportAsync was canceled.");
            else
                logger.LogError(exception, "Knowledge freshness method ReportAsync failed.");
            throw;
        }
    }

    public async Task<KnowledgeFreshnessReviewResult> RefreshApprovedSourceAsync(KnowledgeSourceRefreshRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.UserConfirmed)
                throw new InvalidOperationException("Fresh human confirmation for this exact source URL is required before refresh.");
            var entry = await RequireKnowledgeAsync(request.KnowledgeId, cancellationToken).ConfigureAwait(false);
            var sourceUrl = NormalizeSourceUrl(request.SourceUrl, entry);
            if (string.IsNullOrWhiteSpace(sourceUrl))
                throw new InvalidOperationException("This knowledge entry has no usable public http/https source URL to refresh.");
            if (IsClearlyLocalSource(sourceUrl))
                throw new InvalidOperationException("Local/private-network sources stay on LocalGPT's local/LAN path and are not fetched by the public remote importer.");

            var remoteResult = await remoteImporter.ImportAsync(new RemoteKnowledgeImportRequest
            {
                SourceUrl = sourceUrl,
                SourceKind = "Auto",
                SaveToKnowledge = true,
                PreviewOnly = false,
                UserConfirmed = true,
                Topics = string.IsNullOrWhiteSpace(entry.Topic) ? [] : [entry.Topic]
            }, cancellationToken).ConfigureAwait(false);

            if (remoteResult.ImportedKnowledgeCount <= 0)
                return new KnowledgeFreshnessReviewResult
                {
                    KnowledgeId = entry.Id,
                    Status = "RefreshInspectedNoReplacement",
                    SourceUrl = sourceUrl,
                    RemoteResult = remoteResult,
                    Message = "The exact source was inspected, but no replacement knowledge was imported; the original entry remains under review."
                };

            entry.ReviewStatus = "Superseded";
            entry.VerificationStatus = "SupersededBySourceRefresh";
            entry.IsArchived = true;
            entry.StalenessReason = string.Empty;
            entry.StalenessDetectedAtUtc = null;
            entry.StalenessDetectedBy = string.Empty;
            entry.LastVerifiedAtUtc = DateTime.UtcNow;
            await knowledge.SaveEntryAsync(entry, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Approved exact-source refresh completed for knowledge entry {KnowledgeId}; URL content was omitted.", entry.Id);
            return new KnowledgeFreshnessReviewResult
            {
                KnowledgeId = entry.Id,
                Status = "Refreshed",
                SourceUrl = sourceUrl,
                RemoteResult = remoteResult,
                Message = "Replacement knowledge was imported and the stale entry was archived as superseded."
            };
    
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Knowledge freshness method RefreshApprovedSourceAsync was canceled.");
            else
                logger.LogError(exception, "Knowledge freshness method RefreshApprovedSourceAsync failed.");
            throw;
        }
    }

    public async Task ApplyHumanDecisionAsync(HumanCollaborationRequest request, HumanDecisionSubmission submission, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(submission);
            if (request.OperationKey.StartsWith(ReviewOperationPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var knowledgeId = ParseKnowledgeId(request.OperationKey, ReviewOperationPrefix);
                var entry = await RequireKnowledgeAsync(knowledgeId, cancellationToken).ConfigureAwait(false);
                var response = (submission.Response ?? string.Empty).Trim();
                if (response.Contains("refresh", StringComparison.OrdinalIgnoreCase))
                {
                    var sourceUrl = ResolveRequestSourceUrl(request, entry);
                    if (string.IsNullOrWhiteSpace(sourceUrl))
                        throw new InvalidOperationException("No exact public source URL is available for this refresh request.");
                    await QueueRefreshApprovalAsync(entry, sourceUrl, request.CouncilRunId, cancellationToken).ConfigureAwait(false);
                }
                else if (response.Contains("review", StringComparison.OrdinalIgnoreCase) || response.Contains("inspect", StringComparison.OrdinalIgnoreCase))
                {
                    await InspectAndQueueFollowUpAsync(entry, ResolveRequestSourceUrl(request, entry), request.CouncilRunId, cancellationToken).ConfigureAwait(false);
                }
                else if (response.Contains("reject", StringComparison.OrdinalIgnoreCase) || response.Contains("outdated", StringComparison.OrdinalIgnoreCase) || response.Contains("archive", StringComparison.OrdinalIgnoreCase))
                {
                    entry.ReviewStatus = "Deprecated";
                    entry.VerificationStatus = "RejectedAsOutdated";
                    entry.IsArchived = true;
                    entry.IsUserApproved = false;
                    await knowledge.SaveEntryAsync(entry, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    entry.ReviewStatus = "Reviewed";
                    entry.VerificationStatus = "UserVerified";
                    entry.LastVerifiedAtUtc = DateTime.UtcNow;
                    entry.StalenessReason = string.Empty;
                    entry.StalenessDetectedAtUtc = null;
                    entry.StalenessDetectedBy = string.Empty;
                    entry.IsUserApproved = true;
                    await knowledge.SaveEntryAsync(entry, cancellationToken).ConfigureAwait(false);
                }
                return;
            }

            if (request.OperationKey.StartsWith(RefreshOperationPrefix, StringComparison.OrdinalIgnoreCase) && submission.Approved == true)
            {
                var knowledgeId = ParseKnowledgeId(request.OperationKey, RefreshOperationPrefix);
                var entry = await RequireKnowledgeAsync(knowledgeId, cancellationToken).ConfigureAwait(false);
                await RefreshApprovedSourceAsync(new KnowledgeSourceRefreshRequest
                {
                    KnowledgeId = knowledgeId,
                    SourceUrl = ResolveRequestSourceUrl(request, entry),
                    UserConfirmed = true
                }, cancellationToken).ConfigureAwait(false);
            }
    
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Knowledge freshness method ApplyHumanDecisionAsync was canceled.");
            else
                logger.LogError(exception, "Knowledge freshness method ApplyHumanDecisionAsync failed.");
            throw;
        }
    }

    private async Task InspectAndQueueFollowUpAsync(CouncilKnowledgeEntry entry, string sourceUrl, Guid? councilRunId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
            throw new InvalidOperationException("No exact public source URL is available to inspect.");
        if (IsClearlyLocalSource(sourceUrl))
            throw new InvalidOperationException("Local/private-network sources must be reviewed through the existing local/LAN path.");

        RemoteKnowledgeImportResult preview;
        try
        {
            preview = await remoteImporter.ImportAsync(new RemoteKnowledgeImportRequest
            {
                SourceUrl = sourceUrl,
                SourceKind = "Auto",
                PreviewOnly = true,
                SaveToKnowledge = false,
                UserConfirmed = false
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException or HttpRequestException)
        {
            logger.LogWarning(exception, "Exact-source freshness inspection was rejected; URL content was omitted.");
            throw;
        }

        var now = DateTime.UtcNow;
        await EnsurePendingRequestAsync(new HumanCollaborationRequest
        {
            CouncilRunId = councilRunId,
            CorrelationId = $"knowledge-freshness-preview:{entry.Id:N}:{now:yyyyMMddHHmmss}",
            OperationKey = ReviewOperationPrefix + entry.Id.ToString("N"),
            ParameterFingerprint = Fingerprint(entry.Id, sourceUrl + "|preview|" + now.ToString("yyyyMMddHH")),
            RequestKind = vocabulary.Get().HumanRequestGuidance,
            Title = $"Source review complete: {Compact(entry.Topic, 90, "Untitled knowledge")}",
            Description = $"Exact source preview returned {preview.DownloadedFileCount} file/page item(s), {preview.MatchedFileCount} matching the configured policy, and {preview.Warnings.Count} warning(s). No knowledge was changed. Exact source: {sourceUrl}",
            RiskLevel = "Low",
            Status = vocabulary.Get().HumanStatusPending,
            Source = "External URL (exact value in description)",
            RequestedBy = "Knowledge freshness reviewer",
            RequestedRole = "Knowledge reviewer",
            SuggestedResponsesText = "Keep current knowledge\nRefresh exact source\nReject as outdated",
            ResponsePrompt = "The source was inspected without saving. Choose the next action for this exact knowledge entry.",
            AllowFreeText = true,
            RequestedAtUtc = now,
            UpdatedAtUtc = now,
            IsSensitive = false
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task QueueRefreshApprovalAsync(CouncilKnowledgeEntry entry, string sourceUrl, Guid? councilRunId, CancellationToken cancellationToken)
    {
        try
        {
            if (IsClearlyLocalSource(sourceUrl))
                throw new InvalidOperationException("Local/private-network sources stay on LocalGPT's local/LAN path and do not use public-source approval/import.");
            var now = DateTime.UtcNow;
            await EnsurePendingRequestAsync(new HumanCollaborationRequest
            {
                CouncilRunId = councilRunId,
                CorrelationId = $"knowledge-refresh:{entry.Id:N}:{now:yyyyMMddHHmmss}",
                OperationKey = RefreshOperationPrefix + entry.Id.ToString("N"),
                ParameterFingerprint = Fingerprint(entry.Id, sourceUrl),
                RequestKind = vocabulary.Get().HumanRequestApproval,
                Title = $"Refresh knowledge from exact source: {Compact(entry.Topic, 80, "Untitled knowledge")}",
                Description = $"Allow LocalGPT to fetch and import replacement evidence from this exact external URL only: {sourceUrl}",
                RiskLevel = "High",
                Status = vocabulary.Get().HumanStatusPending,
                Source = "External URL (exact value in description)",
                RequestedBy = "Knowledge freshness reviewer",
                RequestedRole = "Knowledge curator",
                ResponsePrompt = "Approve only if this exact external URL may be fetched and saved to Council knowledge.",
                AllowFreeText = true,
                RequestedAtUtc = now,
                UpdatedAtUtc = now,
                RequiredBeforeCompletion = false,
                IsSensitive = true,
                ConsumeApproval = true,
                ApprovalReuseScope = HumanApprovalReuseScope.ExactRequestOnce
            }, cancellationToken).ConfigureAwait(false);
    
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Knowledge freshness method QueueRefreshApprovalAsync was canceled.");
            else
                logger.LogError(exception, "Knowledge freshness method QueueRefreshApprovalAsync failed.");
            throw;
        }
    }

    private async Task<HumanCollaborationRequest> EnsurePendingRequestAsync(HumanCollaborationRequest candidate, CancellationToken cancellationToken)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var existing = await db.HumanCollaborationRequests
                .Where(item => item.Status == vocabulary.Get().HumanStatusPending
                    && item.OperationKey == candidate.OperationKey
                    && item.ParameterFingerprint == candidate.ParameterFingerprint)
                .OrderByDescending(item => item.UpdatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
                return existing;
            db.HumanCollaborationRequests.Add(candidate);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return candidate;
    
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Knowledge freshness method EnsurePendingRequestAsync was canceled.");
            else
                logger.LogError(exception, "Knowledge freshness method EnsurePendingRequestAsync failed.");
            throw;
        }
    }

    private async Task<CouncilKnowledgeEntry> RequireKnowledgeAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return await knowledge.GetEntryAsync(id, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Council knowledge entry '{id}' was not found.");
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Knowledge lookup for freshness review was canceled.");
            else
                logger.LogError(exception, "Knowledge lookup for freshness review failed for {KnowledgeId}.", id);
            throw;
        }
    }

    private Guid ParseKnowledgeId(string operationKey, string prefix)
    {
        try
        {
            return Guid.TryParseExact(operationKey[prefix.Length..], "N", out var id)
                ? id
                : throw new InvalidOperationException("The freshness request does not contain a valid knowledge identifier.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing the stable knowledge identifier from a freshness request failed.");
            throw;
        }
    }

    private string ResolveRequestSourceUrl(HumanCollaborationRequest request, CouncilKnowledgeEntry entry)
    {
        try
        {
            var matches = UrlRegex.Matches(request.Description ?? string.Empty);
            if (matches.Count > 0)
            {
                var exactSource = matches[^1].Value.TrimEnd('.', ',', ';', ')', ']', '}');
                var normalized = NormalizeSourceUrl(exactSource, entry);
                if (!string.IsNullOrWhiteSpace(normalized))
                    return normalized;
            }

            return NormalizeSourceUrl(request.Source, entry);
    
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Knowledge freshness method ResolveRequestSourceUrl was canceled.");
            else
                logger.LogError(exception, "Knowledge freshness method ResolveRequestSourceUrl failed.");
            throw;
        }
    }

    private string NormalizeSourceUrl(string? explicitUrl, CouncilKnowledgeEntry entry)
    {
        try
        {
            foreach (var value in new[] { explicitUrl, entry.HelpfulSources, entry.Source })
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                var match = UrlRegex.Match(value);
                var candidate = match.Success ? match.Value.TrimEnd('.', ',', ';', ')', ']', '}') : value.Trim();
                if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
                {
                    var absoluteUrl = uri.AbsoluteUri;
                    if (absoluteUrl.Length > MaximumApprovalSourceUrlLength)
                        throw new InvalidOperationException($"The exact source URL exceeds the {MaximumApprovalSourceUrlLength}-character durable approval limit.");
                    return absoluteUrl;
                }
            }
            return string.Empty;
    
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Knowledge freshness method NormalizeSourceUrl was canceled.");
            else
                logger.LogError(exception, "Knowledge freshness method NormalizeSourceUrl failed.");
            throw;
        }
    }

    private bool IsClearlyLocalSource(string sourceUrl)
    {
        try
        {
            if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
                return true;
            if (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
                return true;
            if (!IPAddress.TryParse(uri.Host, out var address))
                return false;
            if (IPAddress.IsLoopback(address) || address.AddressFamily == AddressFamily.InterNetworkV6 && (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal))
                return true;
            var bytes = address.GetAddressBytes();
            return address.AddressFamily == AddressFamily.InterNetwork && (bytes[0] == 10 || bytes[0] == 127 || bytes[0] == 192 && bytes[1] == 168 || bytes[0] == 172 && bytes[1] is >= 16 and <= 31 || bytes[0] == 169 && bytes[1] == 254);
    
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Knowledge freshness method IsClearlyLocalSource was canceled.");
            else
                logger.LogError(exception, "Knowledge freshness method IsClearlyLocalSource failed.");
            throw;
        }
    }

    private string Fingerprint(Guid knowledgeId, string sourceUrl)
    {
        try
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{knowledgeId:N}|{sourceUrl}"))).ToLowerInvariant();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating an exact-source freshness fingerprint failed for {KnowledgeId}.", knowledgeId);
            throw;
        }
    }

    private string Compact(string? value, int maximum, string fallback = "")
    {
        try
        {
            var normalized = string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (normalized.Length == 0)
                normalized = fallback;
            return normalized.Length <= maximum ? normalized : normalized[..maximum];
    
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Knowledge freshness method Compact was canceled.");
            else
                logger.LogError(exception, "Knowledge freshness method Compact failed.");
            throw;
        }
    }
}
