using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using System.Security.Cryptography;
using System.Text;

namespace LocalGPT.Services
{
    /// <summary>
    /// Owns Council knowledge projection, normalization, trust labels and briefing eligibility independently from SQLite persistence.
    /// </summary>
    public sealed class CouncilKnowledgeContentService
    {
        /// <summary>
        /// Stores the council text service dependency used by <see cref="CouncilKnowledgeContentService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly CouncilTextService _text;
        /// <summary>
        /// Stores the local GPT catalog service dependency used by <see cref="CouncilKnowledgeContentService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly LocalGptCatalogService _catalog;
        /// <summary>
        /// Stores the logger used by <see cref="CouncilKnowledgeContentService"/> to record operational diagnostics without coupling callers to logging details.
        /// </summary>
        private readonly ILogger<CouncilKnowledgeContentService> serviceLogger;

        /// <summary>Initializes the Council knowledge content service.</summary>
        /// <param name="text">Text-policy service used for bounded prompt formatting.</param>
        /// <param name="catalog">Persisted/runtime catalog used for maintained patterns.</param>
        /// <param name="serviceLogger">Logger for bounded diagnostics.</param>
        public CouncilKnowledgeContentService(
            CouncilTextService text,
            LocalGptCatalogService catalog,
            ILogger<CouncilKnowledgeContentService> serviceLogger)
        {
            _text = text;
            _catalog = catalog;
            this.serviceLogger = serviceLogger;
        }

        /// <summary>
        /// Computes source hash as part of the council knowledge content service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="entry">Entry value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string ComputeSourceHash(CouncilKnowledgeEntry entry, ILogger logger)
        {
            try
            {
                var sourceMaterial = $"{entry.Topic}\n{entry.Scope}\n{entry.Source}\n{entry.HelpfulSources}\n{entry.Content}";
                return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourceMaterial)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ComputeSourceHash entry {entry.ToString()}");
                return string.Empty;
            }

        }

        /// <summary>
        /// Builds council knowledge content as part of the council knowledge content service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string BuildCouncilKnowledgeContent(MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                var builder = new StringBuilder()
             .AppendLine($"Council members: {string.Join(", ", result.ModelNames)}")
             .AppendLine($"Prompt: {_text.TrimForPrompt(result.Prompt, 900, logger)}")
             .AppendLine()
             .AppendLine("Final answer:")
             .AppendLine(_text.TrimForPrompt(result.FinalAnswer, 2400, logger));

                if (result.Warnings.Count > 0)
                {
                    builder.AppendLine().AppendLine("Warnings:");
                    foreach (var warning in result.Warnings.Take(10))
                        builder.AppendLine($"- {warning}");
                }

                if (result.UserPoll is not null)
                {
                    builder.AppendLine().AppendLine("User decision poll:");
                    builder.AppendLine(result.UserPoll.Question);
                    foreach (var option in result.UserPoll.Options)
                        builder.AppendLine($"- {option.Label}: {option.FollowUpPrompt}");
                }

                return builder.ToString().Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "BuildCouncilKnowledgeContent");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds topic as part of the council knowledge content service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="prompt">Prompt value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string BuildTopic(string prompt, ILogger logger)
        {
            try
            {
                var normalized = _catalog.WhitespacePattern.Replace(prompt, " ").Trim();
                if (string.IsNullOrWhiteSpace(normalized))
                    return "AI Council run";

                return normalized.Length <= 120 ? normalized : $"{normalized[..117].TrimEnd()}...";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not derive a knowledge topic from the supplied prompt.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds tags as part of the council knowledge content service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="nonSubstantive">Value indicating whether non substantive should apply to this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string BuildTags(MultiModelCouncilResult result, bool nonSubstantive, ILogger logger)
        {
            try
            {
                var tags = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "council",
                "auto"
            };

                foreach (var model in result.ModelNames)
                    tags.Add(model);
                if (result.Artifacts.Count > 0)
                    tags.Add("artifact");
                if (result.UserPoll is not null)
                    tags.Add("poll");
                if (nonSubstantive)
                {
                    tags.Add("non-substantive");
                    tags.Add("thinking-only");
                }

                return string.Join("; ", tags);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "BuildTags");
                return string.Empty;
            }
        }

        /// <summary>
        /// Determines whether non substantive council knowledge as part of the council knowledge content service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool IsNonSubstantiveCouncilKnowledge(MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                if (result.UserPoll is not null)
                    return false;

                return LooksLikeNonSubstantiveContent(result.FinalAnswer, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "BuildTags");
                return false;
            }
        }

        /// <summary>
        /// Performs looks like non substantive content as part of the council knowledge content service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="content">Content value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool LooksLikeNonSubstantiveContent(string content, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                    return true;

                return content.Contains("returned thinking but no final visible answer", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("did not return a visible answer", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("did not return a substantive consensus answer", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "LooksLikeNonSubstantiveContent");
                return false;
            }
        }

        /// <summary>
        /// Performs extract helpful sources as part of the council knowledge content service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="text">Text value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string ExtractHelpfulSources(string text, ILogger logger)
        {
            try
            {
                var matches =_catalog.HelpfulSourceLinePattern
                .Matches(text)
                .Select(match => match.Groups["line"].Value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList();

                return matches.Count == 0
                    ? "None explicitly requested."
                    : string.Join(Environment.NewLine, matches.Select(item => $"- {item}"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ExtractHelpfulSources text {text.ToString()}");
                return string.Empty;
            }

        }


        /// <summary>
        /// Performs trim or fallback as part of the council knowledge content service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="maxLength">Max length value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="fallback">Fallback value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string TrimOrFallback(string value, int maxLength, string fallback, ILogger logger)
        {
            try
            {
                var trimmed = Trim(value, maxLength, logger);
                return string.IsNullOrWhiteSpace(trimmed) ? fallback : trimmed;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ExtractHelpfulSources value {value.ToString()} maxLength {maxLength.ToString()} fallback {fallback.ToString()}");
                return string.Empty;
            }

        }

        /// <summary>
        /// Performs trim as part of the council knowledge content service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="maxLength">Max length value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string Trim(string value, int maxLength, ILogger logger)
        {
            try
            {
                var trimmed = value?.Trim() ?? string.Empty;
                return trimmed.Length <= maxLength ? trimmed : $"{trimmed[..maxLength].TrimEnd()}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in Trim value {value.ToString()} maxLength {maxLength.ToString()}");
                return string.Empty;
            }
        }


    

        /// <summary>
        /// Performs normalize as part of the council knowledge content service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="entry">Entry value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        public void Normalize(CouncilKnowledgeEntry entry, ILogger logger)
        {
            try
            {
                entry.Topic = TrimOrFallback(entry.Topic, 240, "Untitled knowledge entry", logger);
                entry.Scope = TrimOrFallback(entry.Scope, 120, "AI Council", logger);
                entry.Source = TrimOrFallback(entry.Source, 240, "Manual", logger);
                entry.Tags = Trim(entry.Tags, 400, logger);
                entry.Confidence = Math.Clamp(entry.Confidence, 0, 100);
                entry.VerificationStatus = NormalizeVerificationStatus(entry, logger);
                entry.ReviewStatus = NormalizeReviewStatus(entry, logger);
                entry.StalenessReason = Trim(entry.StalenessReason, 500, logger);
                entry.StalenessDetectedBy = Trim(entry.StalenessDetectedBy, 160, logger);
                entry.SourceHash = Trim(entry.SourceHash, 128, logger);
                if (string.IsNullOrWhiteSpace(entry.SourceHash))
                    entry.SourceHash = ComputeSourceHash(entry, logger);

                if (entry.VerificationStatus is "SourceBacked" or "UserVerified" && entry.LastVerifiedAtUtc is null)
                    entry.LastVerifiedAtUtc = DateTime.UtcNow;

                if (entry.ReviewStatus == "Archived")
                    entry.IsArchived = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in Normalize entry {entry.ToString()}");
            }
        }

        /// <summary>
        /// Builds trust label as part of the council knowledge content service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="entry">Entry value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string BuildTrustLabel(CouncilKnowledgeEntry entry, ILogger logger)
        {
            try
            {
                var trust = entry.VerificationStatus switch
                {
                    "SourceBacked" => "source-backed seed",
                    "UserVerified" => "verified by user",
                    "ModelSuggested" => "model-suggested; treat as hypothesis until user approves",
                    "Archived" => "archived; do not use as active evidence",
                    _ => entry.IsUserApproved
                        ? "verified by user"
                        : "needs verification"
                };

                var review = entry.ReviewStatus switch
                {
                    "Current" => "current",
                    "NeedsUserReview" => "needs user review",
                    "NeedsSourceRefresh" => "needs source refresh",
                    "NeedsDiagnosticVerification" => "needs diagnostic verification",
                    "Expired" => "expired",
                    "Deprecated" => "deprecated",
                    "Superseded" => "superseded",
                    "Archived" => "archived",
                    _ => "needs review"
                };

                return $"{trust}; review: {review}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildTrustLabel entry {entry.ToString()}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Normalizes verification status as part of the council knowledge content service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="entry">Entry value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string NormalizeVerificationStatus(CouncilKnowledgeEntry entry, ILogger logger)
        {
            try
            {
                if (entry.IsArchived)
                    return "Archived";

                var requested = Trim(entry.VerificationStatus, 80, logger).Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
                if (IsKnownVerificationStatus(requested, logger))
                    return requested;

                if (entry.Source.Contains("seed", StringComparison.OrdinalIgnoreCase))
                    return "SourceBacked";

                if (entry.IsUserApproved)
                    return "UserVerified";

                if (entry.Source.StartsWith("AI Council ", StringComparison.OrdinalIgnoreCase))
                    return "ModelSuggested";

                return "NeedsVerification";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in NormalizeVerificationStatus entry {entry.ToString()}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Determines whether known verification status as part of the council knowledge content service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool IsKnownVerificationStatus(string value, ILogger logger)
        {
            try
            {
                return value is "SourceBacked" or "UserVerified" or "ModelSuggested" or "NeedsVerification" or "Archived";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsKnownVerificationStatus value {value.ToString()}");
                return false;
            }
        }

        /// <summary>
        /// Normalizes review status as part of the council knowledge content service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="entry">Entry value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string NormalizeReviewStatus(CouncilKnowledgeEntry entry, ILogger logger)
        {
            try
            {
                if (entry.IsArchived)
                    return "Archived";

                if (entry.SupersededByKnowledgeId is not null)
                    return "Superseded";

                var now = DateTime.UtcNow;
                if (entry.ExpiresAtUtc is not null && entry.ExpiresAtUtc.Value <= now)
                {
                    if (string.IsNullOrWhiteSpace(entry.StalenessReason))
                        entry.StalenessReason = "Knowledge expiry date passed.";
                    entry.StalenessDetectedAtUtc ??= now;
                    entry.StalenessDetectedBy = TrimOrFallback(entry.StalenessDetectedBy, 160, "LocalGPT knowledge lifecycle", logger);
                    return "Expired";
                }

                var requested = Trim(entry.ReviewStatus, 80, logger).Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
                if (requested == "NeedsUserReview" &&
                    entry.IsUserApproved &&
                    entry.VerificationStatus is "SourceBacked" or "UserVerified")
                    return "Current";

                if (IsKnownReviewStatus(requested, logger))
                    return requested;

                return entry.VerificationStatus switch
                {
                    "SourceBacked" or "UserVerified" => "Current",
                    "Archived" => "Archived",
                    _ => "NeedsUserReview"
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in NormalizeReviewStatus entry {entry.ToString()}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Determines whether known review status as part of the council knowledge content service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool IsKnownReviewStatus(string value, ILogger logger)
        {
            try
            {
                return value is "Current" or "NeedsUserReview" or "NeedsSourceRefresh" or "NeedsDiagnosticVerification" or "Expired" or "Deprecated" or "Superseded" or "Archived";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsKnownReviewStatus value {value.ToString()}");
                return false;
            }
        }

        /// <summary>
        /// Determines whether usable for briefing.
        /// </summary>
        /// <param name="entry">Entry value supplied to the council knowledge content operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool IsUsableForBriefing(CouncilKnowledgeEntry entry, ILogger logger)
        {
            try
            {
                if (entry.IsArchived || !entry.IsUserApproved)
                    return false;

                if (entry.ExpiresAtUtc is not null && entry.ExpiresAtUtc.Value <= DateTime.UtcNow)
                    return false;

                return entry.ReviewStatus == "Current" &&
                       entry.VerificationStatus is "SourceBacked" or "UserVerified";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsUsableForBriefing entry {entry.ToString()}");
                return false;
            }
        }
    }
}
