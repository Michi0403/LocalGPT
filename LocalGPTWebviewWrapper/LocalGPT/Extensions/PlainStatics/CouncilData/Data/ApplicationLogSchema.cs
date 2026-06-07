using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Extensions.PlainStatics.CouncilData.Data
{
    public static class ApplicationLogSchema
    {
        public static async Task EnsureCreatedAsync(LocalGptMemoryDbContext db, CancellationToken cancellationToken = default)
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "ApplicationLogs" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_ApplicationLogs" PRIMARY KEY AUTOINCREMENT,
                    "TimestampUtc" TEXT NOT NULL,
                    "Level" TEXT NOT NULL,
                    "LogLevelValue" INTEGER NOT NULL,
                    "Category" TEXT NOT NULL,
                    "EventId" INTEGER NOT NULL,
                    "EventName" TEXT NULL,
                    "Message" TEXT NOT NULL,
                    "Exception" TEXT NULL,
                    "MachineName" TEXT NOT NULL,
                    "ProcessId" INTEGER NOT NULL,
                    "ThreadId" INTEGER NOT NULL
                );
                """,
                cancellationToken);

            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_ApplicationLogs_TimestampUtc" ON "ApplicationLogs" ("TimestampUtc");""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_ApplicationLogs_LogLevelValue" ON "ApplicationLogs" ("LogLevelValue");""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX IF NOT EXISTS "IX_ApplicationLogs_LogLevelValue_TimestampUtc" ON "ApplicationLogs" ("LogLevelValue", "TimestampUtc");""",
                cancellationToken);
        }
    }
}
