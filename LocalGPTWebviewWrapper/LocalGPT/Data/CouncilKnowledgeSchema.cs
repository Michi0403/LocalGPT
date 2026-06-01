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
                    "IsPinned" INTEGER NOT NULL,
                    "IsArchived" INTEGER NOT NULL
                );
                """,
                cancellationToken);

            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_UpdatedAtUtc" ON "CouncilKnowledgeEntries" ("UpdatedAtUtc");""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_IsPinned_UpdatedAtUtc" ON "CouncilKnowledgeEntries" ("IsPinned", "UpdatedAtUtc");""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeEntries_Scope" ON "CouncilKnowledgeEntries" ("Scope");""",
                cancellationToken);
        }
    }
}
