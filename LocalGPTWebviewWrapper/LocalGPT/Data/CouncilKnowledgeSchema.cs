using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Data
{
    public static class CouncilKnowledgeSchema
    {
        public static async Task EnsureCreatedAsync(LocalGptMemoryDbContext db, CancellationToken cancellationToken = default)
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "CouncilKnowledgeEntries" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_CouncilKnowledgeEntries" PRIMARY KEY,
                    "CreatedAtUtc" TEXT NOT NULL,
                    "UpdatedAtUtc" TEXT NOT NULL,
                    "Topic" TEXT NOT NULL,
                    "Scope" TEXT NOT NULL,
                    "Content" TEXT NOT NULL,
                    "Source" TEXT NOT NULL,
                    "HelpfulSources" TEXT NOT NULL,
                    "Tags" TEXT NOT NULL,
                    "Confidence" INTEGER NOT NULL,
                    "VerificationStatus" TEXT NOT NULL DEFAULT 'NeedsVerification',
                    "ReviewStatus" TEXT NOT NULL DEFAULT 'NeedsUserReview',
                    "ExpiresAtUtc" TEXT NULL,
                    "LastVerifiedAtUtc" TEXT NULL,
                    "LastUsedAtUtc" TEXT NULL,
                    "SupersededByKnowledgeId" TEXT NULL,
                    "StalenessReason" TEXT NOT NULL DEFAULT '',
                    "StalenessDetectedAtUtc" TEXT NULL,
                    "StalenessDetectedBy" TEXT NOT NULL DEFAULT '',
                    "SourceHash" TEXT NOT NULL DEFAULT '',
                    "SourceDateUtc" TEXT NULL,
                    "IsUserApproved" INTEGER NOT NULL DEFAULT 0,
                    "IsPinned" INTEGER NOT NULL,
                    "IsArchived" INTEGER NOT NULL
                );
                """,
                cancellationToken);

            await TryAddColumnAsync(
                db,
                """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "IsUserApproved" INTEGER NOT NULL DEFAULT 0;""",
                cancellationToken);
            await TryAddColumnAsync(
                db,
                """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "VerificationStatus" TEXT NOT NULL DEFAULT 'NeedsVerification';""",
                cancellationToken);
            await TryAddColumnAsync(
                db,
                """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "ReviewStatus" TEXT NOT NULL DEFAULT 'NeedsUserReview';""",
                cancellationToken);
            await TryAddColumnAsync(
                db,
                """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "ExpiresAtUtc" TEXT NULL;""",
                cancellationToken);
            await TryAddColumnAsync(
                db,
                """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "LastVerifiedAtUtc" TEXT NULL;""",
                cancellationToken);
            await TryAddColumnAsync(
                db,
                """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "LastUsedAtUtc" TEXT NULL;""",
                cancellationToken);
            await TryAddColumnAsync(
                db,
                """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "SupersededByKnowledgeId" TEXT NULL;""",
                cancellationToken);
            await TryAddColumnAsync(
                db,
                """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "StalenessReason" TEXT NOT NULL DEFAULT '';""",
                cancellationToken);
            await TryAddColumnAsync(
                db,
                """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "StalenessDetectedAtUtc" TEXT NULL;""",
                cancellationToken);
            await TryAddColumnAsync(
                db,
                """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "StalenessDetectedBy" TEXT NOT NULL DEFAULT '';""",
                cancellationToken);
            await TryAddColumnAsync(
                db,
                """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "SourceHash" TEXT NOT NULL DEFAULT '';""",
                cancellationToken);
            await TryAddColumnAsync(
                db,
                """ALTER TABLE "CouncilKnowledgeEntries" ADD COLUMN "SourceDateUtc" TEXT NULL;""",
                cancellationToken);

            await db.Database.ExecuteSqlRawAsync(
                """
                UPDATE "CouncilKnowledgeEntries"
                SET "VerificationStatus" =
                    CASE
                        WHEN "IsArchived" = 1 THEN 'Archived'
                        WHEN "Source" LIKE 'LocalGPT % seed' THEN 'SourceBacked'
                        WHEN "Source" = 'LocalGPT SQL seed' THEN 'SourceBacked'
                        WHEN "IsUserApproved" = 1 THEN 'UserVerified'
                        WHEN "Source" LIKE 'AI Council %' THEN 'ModelSuggested'
                        ELSE 'NeedsVerification'
                    END
                WHERE "VerificationStatus" IS NULL OR trim("VerificationStatus") = '';
                """,
                cancellationToken);

            await db.Database.ExecuteSqlRawAsync(
                """
                UPDATE "CouncilKnowledgeEntries"
                SET "ReviewStatus" =
                    CASE
                        WHEN "IsArchived" = 1 THEN 'Archived'
                        WHEN "SupersededByKnowledgeId" IS NOT NULL AND trim("SupersededByKnowledgeId") <> '' THEN 'Superseded'
                        WHEN "ExpiresAtUtc" IS NOT NULL AND trim("ExpiresAtUtc") <> '' AND "ExpiresAtUtc" <= datetime('now') THEN 'Expired'
                        WHEN "VerificationStatus" IN ('SourceBacked', 'UserVerified') THEN 'Current'
                        WHEN "VerificationStatus" IN ('ModelSuggested', 'NeedsVerification') THEN 'NeedsUserReview'
                        ELSE 'NeedsUserReview'
                    END
                WHERE "ReviewStatus" IS NULL OR trim("ReviewStatus") = '' OR "ReviewStatus" IN ('NeedsVerification', 'NeedsUserReview');
                """,
                cancellationToken);

            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_UpdatedAtUtc" ON "CouncilKnowledgeEntries" ("UpdatedAtUtc");""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_IsUserApproved_UpdatedAtUtc" ON "CouncilKnowledgeEntries" ("IsUserApproved", "UpdatedAtUtc");""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_IsPinned_UpdatedAtUtc" ON "CouncilKnowledgeEntries" ("IsPinned", "UpdatedAtUtc");""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_VerificationStatus" ON "CouncilKnowledgeEntries" ("VerificationStatus");""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_ReviewStatus" ON "CouncilKnowledgeEntries" ("ReviewStatus");""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_ExpiresAtUtc" ON "CouncilKnowledgeEntries" ("ExpiresAtUtc");""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_LastVerifiedAtUtc" ON "CouncilKnowledgeEntries" ("LastVerifiedAtUtc");""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_LastUsedAtUtc" ON "CouncilKnowledgeEntries" ("LastUsedAtUtc");""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_SupersededByKnowledgeId" ON "CouncilKnowledgeEntries" ("SupersededByKnowledgeId");""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_Scope" ON "CouncilKnowledgeEntries" ("Scope");""",
                cancellationToken);
        }

        private static async Task TryAddColumnAsync(LocalGptMemoryDbContext db, string sql, CancellationToken cancellationToken)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            }
            catch (Exception ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase))
            {
            }
        }
    }
}
