using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Data
{
    public static class NativeCommandLogSchema
    {
        public static async Task EnsureCreatedAsync(LocalGptMemoryDbContext db, CancellationToken cancellationToken = default)
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "NativeCommandLogs" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_NativeCommandLogs" PRIMARY KEY AUTOINCREMENT,
                    "StartedAtUtc" TEXT NOT NULL,
                    "CompletedAtUtc" TEXT NOT NULL,
                    "FeatureName" TEXT NOT NULL,
                    "RequestedBy" TEXT NOT NULL,
                    "Executable" TEXT NOT NULL,
                    "Arguments" TEXT NOT NULL,
                    "WorkingDirectory" TEXT NOT NULL,
                    "ExitCode" INTEGER NOT NULL,
                    "DurationMilliseconds" REAL NOT NULL,
                    "StdoutPath" TEXT NOT NULL,
                    "StderrPath" TEXT NOT NULL,
                    "PolicyDecision" TEXT NOT NULL,
                    "PolicyReason" TEXT NOT NULL
                );
                """,
                cancellationToken);

            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_NativeCommandLogs_StartedAtUtc" ON "NativeCommandLogs" ("StartedAtUtc");""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_NativeCommandLogs_Executable" ON "NativeCommandLogs" ("Executable");""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_NativeCommandLogs_PolicyDecision" ON "NativeCommandLogs" ("PolicyDecision");""",
                cancellationToken);
        }
    }
}
