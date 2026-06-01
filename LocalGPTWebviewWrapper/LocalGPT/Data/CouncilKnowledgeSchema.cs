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
