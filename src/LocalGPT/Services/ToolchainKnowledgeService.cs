using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Reads compiler/runtime discovery profiles and exact-version context from the user-maintainable LocalGPT knowledge base.</summary>
/// <param name="knowledge">Council knowledge service dependency used by the toolchain knowledge workflow to provide the corresponding application capability.</param>
/// <param name="regexPatterns">Regex pattern service dependency used by the toolchain knowledge workflow to provide the corresponding application capability.</param>
/// <param name="humanCollaboration">Human collaboration service dependency used by the toolchain knowledge workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ToolchainKnowledgeService(
    ICouncilKnowledgeService knowledge,
    IRegexPatternService regexPatterns,
    IHumanCollaborationService humanCollaboration,
    ILogger<ToolchainKnowledgeService> logger) : IToolchainKnowledgeService
{
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="ToolchainKnowledgeService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Retrieves profiles as part of the toolchain knowledge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<ToolchainKnowledgeProfile>> GetProfilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var blockRegex = await regexPatterns.GetRegexAsync("builtin.toolchain-knowledge-block").ConfigureAwait(false);
            if (blockRegex is null)
            {
                logger.LogWarning("Toolchain knowledge block regex is not available; no knowledge-backed toolchain profiles can be loaded.");
                return [];
            }

            var entries = await knowledge.GetEntriesAsync(includeArchived: false, take: 500, cancellationToken).ConfigureAwait(false);
            var profiles = new List<ToolchainKnowledgeProfile>();
            foreach (var entry in entries.OrderByDescending(item => item.IsUserApproved).ThenByDescending(item => item.UpdatedAtUtc))
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (System.Text.RegularExpressions.Match match in blockRegex.Matches(entry.Content ?? string.Empty))
                {
                    var json = match.Groups["json"].Value;
                    if (string.IsNullOrWhiteSpace(json))
                        continue;
                    try
                    {
                        var profile = JsonSerializer.Deserialize<ToolchainKnowledgeProfile>(json, jsonOptions);
                        if (profile is null || string.IsNullOrWhiteSpace(profile.Key) || profile.ExecutableNames.Count == 0)
                            continue;
                        profile.Key = profile.Key.Trim().ToLowerInvariant();
                        profile.DisplayName = string.IsNullOrWhiteSpace(profile.DisplayName) ? profile.Key : profile.DisplayName.Trim();
                        profile.Language = string.IsNullOrWhiteSpace(profile.Language) ? "Other" : profile.Language.Trim();
                        profile.ValidationArguments = string.IsNullOrWhiteSpace(profile.ValidationArguments) ? "--version" : profile.ValidationArguments.Trim();
                        profile.VersionRegexPatternName = string.IsNullOrWhiteSpace(profile.VersionRegexPatternName) ? "builtin.toolchain-version-token" : profile.VersionRegexPatternName.Trim();
                        profile.MaximumSearchDepth = Math.Clamp(profile.MaximumSearchDepth, 0, 5);
                        profile.KnowledgeEntryId = entry.Id;
                        profiles.Add(profile);
                    }
                    catch (JsonException exception)
                    {
                        logger.LogWarning(exception, "Ignored malformed toolchain profile JSON from knowledge entry {KnowledgeEntryId}; profile content was omitted.", entry.Id);
                    }
                }
            }

            var result = profiles
                .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(item => item.Language, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            logger.LogInformation("Loaded {ProfileCount} knowledge-backed toolchain discovery profile(s).", result.Count);
            return result;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Loading toolchain knowledge profiles was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading toolchain knowledge profiles failed.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves profile as part of the toolchain knowledge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<ToolchainKnowledgeProfile?> GetProfileAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return (await GetProfilesAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(item => string.Equals(item.Key, key.Trim(), StringComparison.OrdinalIgnoreCase));
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Loading toolchain profile {ProfileKey} was cancelled.", key);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading toolchain profile {ProfileKey} failed.", key);
            throw;
        }
    }

    /// <summary>
    /// Performs extract version as part of the toolchain knowledge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<string> ExtractVersionAsync(string profileKey, string probeOutput, CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await GetProfileAsync(profileKey, cancellationToken).ConfigureAwait(false);
            var patternName = profile?.VersionRegexPatternName;
            if (string.IsNullOrWhiteSpace(patternName))
                patternName = "builtin.toolchain-version-token";
            var regex = await regexPatterns.GetRegexAsync(patternName).ConfigureAwait(false)
                ?? await regexPatterns.GetRegexAsync("builtin.toolchain-version-token").ConfigureAwait(false);
            if (regex is null)
                return string.Empty;
            var match = regex.Match(probeOutput ?? string.Empty);
            if (!match.Success)
                return string.Empty;
            var version = match.Groups["version"].Success ? match.Groups["version"].Value : match.Value;
            return version.Length <= 160 ? version.Trim() : version[..160].Trim();
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Extracting toolchain version for {ProfileKey} was cancelled.", profileKey);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Extracting toolchain version for {ProfileKey} failed; probe output was omitted.", profileKey);
            throw;
        }
    }

    /// <summary>
    /// Retrieves version knowledge as part of the toolchain knowledge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<ToolchainVersionKnowledgeResult> GetVersionKnowledgeAsync(string profileKey, string version, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(profileKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);
            var normalizedProfile = profileKey.Trim().ToLowerInvariant();
            var normalizedVersion = version.Trim();
            var entries = await knowledge.GetEntriesAsync(includeArchived: false, take: 500, cancellationToken).ConfigureAwait(false);
            var match = entries
                .Where(item => item.IsUserApproved || item.IsPinned)
                .FirstOrDefault(item => HasVersionContext(item, normalizedProfile, normalizedVersion));
            return new ToolchainVersionKnowledgeResult
            {
                ProfileKey = normalizedProfile,
                Version = normalizedVersion,
                HasKnowledge = match is not null,
                KnowledgeEntryId = match?.Id,
                Status = match is null ? "MissingVersionKnowledge" : "KnowledgeAvailable"
            };
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Checking version knowledge for {ProfileKey} was cancelled.", profileKey);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Checking version knowledge for {ProfileKey} failed.", profileKey);
            throw;
        }
    }

    /// <summary>
    /// Performs request missing version knowledge as part of the toolchain knowledge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<ToolchainVersionKnowledgeResult> RequestMissingVersionKnowledgeAsync(ToolchainKnowledgeGapRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var existing = await GetVersionKnowledgeAsync(request.ProfileKey, request.Version, cancellationToken).ConfigureAwait(false);
            if (existing.HasKnowledge)
                return existing;

            var profile = await GetProfileAsync(request.ProfileKey, cancellationToken).ConfigureAwait(false);
            var title = $"Toolchain knowledge needed: {profile?.DisplayName ?? request.ProfileKey} {request.Version}";
            var description = $"LocalGPT detected {profile?.DisplayName ?? request.ProfileKey} version {request.Version}, but the local Knowledge Database has no approved context for that exact version. Provide one of: a Markdown file, a Knowledge Database article, or a text blob describing the version, supported project/build context, validation notes, and important compatibility constraints. No online lookup is performed automatically. {Bound(request.Context, 800)}";
            var gate = await humanCollaboration.AuthorizeOrEnqueueAsync(
                new HumanApprovalRequestSpec(
                    $"toolchain-knowledge:{request.ProfileKey}:{request.Version}",
                    "toolchain.knowledge.request",
                    title,
                    description,
                    "Low",
                    nameof(ToolchainKnowledgeService),
                    "CurrentUser",
                    "Toolchain knowledge provider",
                    RequiredBeforeCompletion: false,
                    IsSensitive: false,
                    RequestKind: "Guidance",
                    SuggestedResponsesText: "Provide Markdown file\nCreate/edit Knowledge Database article\nPaste text blob\nSkip for now",
                    ResponsePrompt: "Choose how to provide the missing toolchain-version knowledge, then attach or enter the requested content.",
                    AllowFreeText: true,
                    QuestionScope: "Member",
                    GateMode: "None"),
                directHumanConfirmation: false,
                cancellationToken).ConfigureAwait(false);
            existing.HumanRequestId = gate.RequestId;
            existing.Status = gate.IsAuthorized ? "KnowledgeRequestSatisfied" : "KnowledgeRequestedFromUser";
            logger.LogInformation("Requested local knowledge for toolchain profile {ProfileKey} version {Version}; request {RequestId}.", request.ProfileKey, request.Version, gate.RequestId);
            return existing;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Requesting missing toolchain knowledge was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Requesting missing toolchain knowledge failed.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether version context as part of the toolchain knowledge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="entry">Entry value supplied to the toolchain knowledge operation and used when producing its result.</param>
    /// <param name="profileKey">Profile key value supplied to the toolchain knowledge operation and used when producing its result.</param>
    /// <param name="version">Version value supplied to the toolchain knowledge operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool HasVersionContext(CouncilKnowledgeEntry entry, string profileKey, string version)
    {
        try
        {
            var tags = entry.Tags ?? string.Empty;
            var topic = entry.Topic ?? string.Empty;
            var content = entry.Content ?? string.Empty;
            var profileTagged = tags.Contains($"toolchain:{profileKey}", StringComparison.OrdinalIgnoreCase)
                || topic.Contains(profileKey, StringComparison.OrdinalIgnoreCase)
                || content.Contains($"\"key\":\"{profileKey}\"", StringComparison.OrdinalIgnoreCase)
                || content.Contains($"\"key\": \"{profileKey}\"", StringComparison.OrdinalIgnoreCase);
            var versionTagged = tags.Contains($"version:{version}", StringComparison.OrdinalIgnoreCase)
                || topic.Contains(version, StringComparison.OrdinalIgnoreCase)
                || content.Contains(version, StringComparison.OrdinalIgnoreCase);
            return profileTagged && versionTagged;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Matching knowledge context for toolchain profile {ProfileKey} failed; content omitted from logs.", profileKey);
            throw;
        }
    }

    /// <summary>
    /// Performs bound as part of the toolchain knowledge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the toolchain knowledge operation and used when producing its result.</param>
    /// <param name="maximum">Maximum value supplied to the toolchain knowledge operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Bound(string? value, int maximum)
    {
        try
        {
            var text = value?.Trim() ?? string.Empty;
            return text.Length <= maximum ? text : text[..maximum];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Bounding toolchain knowledge request text failed; content omitted from logs.");
            throw;
        }
    }

}
