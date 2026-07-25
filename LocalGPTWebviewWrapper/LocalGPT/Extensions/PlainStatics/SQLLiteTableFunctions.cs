using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Extensions.PlainStatics
{
    public static class SQLLiteTableFunctions
    {
        public static void Normalize(CouncilKnowledgeEntry entry, ILogger logger)
        {
            try
            {
                entry.Topic = SQLLiteFunctions.TrimOrFallback(entry.Topic, 240, "Untitled knowledge entry", logger);
                entry.Scope = SQLLiteFunctions.TrimOrFallback(entry.Scope, 120, "AI Council", logger);
                entry.Source = SQLLiteFunctions.TrimOrFallback(entry.Source, 240, "Manual", logger);
                entry.Tags = SQLLiteFunctions.Trim(entry.Tags, 400, logger);
                entry.Confidence = Math.Clamp(entry.Confidence, 0, 100);
                entry.VerificationStatus = NormalizeVerificationStatus(entry, logger);
                entry.ReviewStatus = NormalizeReviewStatus(entry, logger);
                entry.StalenessReason = SQLLiteFunctions.Trim(entry.StalenessReason, 500, logger);
                entry.StalenessDetectedBy = SQLLiteFunctions.Trim(entry.StalenessDetectedBy, 160, logger);
                entry.SourceHash = SQLLiteFunctions.Trim(entry.SourceHash, 128, logger);
                if (string.IsNullOrWhiteSpace(entry.SourceHash))
                    entry.SourceHash = SQLLiteFunctions.ComputeSourceHash(entry, logger);

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
        public static string BuildTrustLabel(CouncilKnowledgeEntry entry, ILogger logger)
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
        public static string NormalizeVerificationStatus(CouncilKnowledgeEntry entry, ILogger logger)
        {
            try
            {
                if (entry.IsArchived)
                    return "Archived";

                var requested = SQLLiteFunctions.Trim(entry.VerificationStatus, 80, logger).Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
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
        public static bool IsKnownVerificationStatus(string value, ILogger logger)
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
        public static string NormalizeReviewStatus(CouncilKnowledgeEntry entry, ILogger logger)
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
                    entry.StalenessDetectedBy = SQLLiteFunctions.TrimOrFallback(entry.StalenessDetectedBy, 160, "LocalGPT knowledge lifecycle", logger);
                    return "Expired";
                }

                var requested = SQLLiteFunctions.Trim(entry.ReviewStatus, 80, logger).Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
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
        public static bool IsKnownReviewStatus(string value, ILogger logger)
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
        public static bool IsUsableForBriefing(CouncilKnowledgeEntry entry, ILogger logger)
        {
            try
            {
                if (entry.IsArchived)
                    return false;

                if (entry.ExpiresAtUtc is not null && entry.ExpiresAtUtc.Value <= DateTime.UtcNow)
                    return false;

                return entry.ReviewStatus is not "Archived" and not "Deprecated" and not "Superseded" and not "Expired";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsUsableForBriefing entry {entry.ToString()}");
                return false;
            }
        }
    }
}
