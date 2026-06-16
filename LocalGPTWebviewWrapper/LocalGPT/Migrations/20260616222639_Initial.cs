using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    LogLevelValue = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    EventName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Exception = table.Column<string>(type: "TEXT", nullable: true),
                    MachineName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ProcessId = table.Column<int>(type: "INTEGER", nullable: false),
                    ThreadId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatMemoryConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                    ProviderName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMemoryConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CouncilKnowledgeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Topic = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                    Scope = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                    HelpfulSources = table.Column<string>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    Confidence = table.Column<int>(type: "INTEGER", nullable: false),
                    VerificationStatus = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ReviewStatus = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastVerifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUsedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SupersededByKnowledgeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    StalenessReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    StalenessDetectedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StalenessDetectedBy = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    SourceHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    SourceDateUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsUserApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPinned = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouncilKnowledgeEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NativeCommandLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FeatureName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    RequestedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CommandProfile = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Executable = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    Arguments = table.Column<string>(type: "TEXT", nullable: false),
                    WorkingDirectory = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    ExitCode = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationMilliseconds = table.Column<double>(type: "REAL", nullable: false),
                    StdoutPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    StderrPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    PolicyDecision = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    PolicyReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NativeCommandLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatMemoryMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConversationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Thinking = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMemoryMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMemoryMessages_ChatMemoryConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ChatMemoryConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLogs_LogLevelValue",
                table: "ApplicationLogs",
                column: "LogLevelValue");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLogs_LogLevelValue_TimestampUtc",
                table: "ApplicationLogs",
                columns: new[] { "LogLevelValue", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLogs_TimestampUtc",
                table: "ApplicationLogs",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMemoryConversations_UpdatedAtUtc",
                table: "ChatMemoryConversations",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMemoryMessages_ConversationId_SortOrder",
                table: "ChatMemoryMessages",
                columns: new[] { "ConversationId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMemoryMessages_CreatedAtUtc",
                table: "ChatMemoryMessages",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CouncilKnowledgeEntries_ExpiresAtUtc",
                table: "CouncilKnowledgeEntries",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CouncilKnowledgeEntries_IsPinned_UpdatedAtUtc",
                table: "CouncilKnowledgeEntries",
                columns: new[] { "IsPinned", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CouncilKnowledgeEntries_IsUserApproved_UpdatedAtUtc",
                table: "CouncilKnowledgeEntries",
                columns: new[] { "IsUserApproved", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CouncilKnowledgeEntries_LastUsedAtUtc",
                table: "CouncilKnowledgeEntries",
                column: "LastUsedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CouncilKnowledgeEntries_LastVerifiedAtUtc",
                table: "CouncilKnowledgeEntries",
                column: "LastVerifiedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CouncilKnowledgeEntries_ReviewStatus",
                table: "CouncilKnowledgeEntries",
                column: "ReviewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CouncilKnowledgeEntries_Scope",
                table: "CouncilKnowledgeEntries",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_CouncilKnowledgeEntries_SupersededByKnowledgeId",
                table: "CouncilKnowledgeEntries",
                column: "SupersededByKnowledgeId");

            migrationBuilder.CreateIndex(
                name: "IX_CouncilKnowledgeEntries_UpdatedAtUtc",
                table: "CouncilKnowledgeEntries",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CouncilKnowledgeEntries_VerificationStatus",
                table: "CouncilKnowledgeEntries",
                column: "VerificationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_NativeCommandLogs_Executable",
                table: "NativeCommandLogs",
                column: "Executable");

            migrationBuilder.CreateIndex(
                name: "IX_NativeCommandLogs_PolicyDecision",
                table: "NativeCommandLogs",
                column: "PolicyDecision");

            migrationBuilder.CreateIndex(
                name: "IX_NativeCommandLogs_StartedAtUtc",
                table: "NativeCommandLogs",
                column: "StartedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationLogs");

            migrationBuilder.DropTable(
                name: "ChatMemoryMessages");

            migrationBuilder.DropTable(
                name: "CouncilKnowledgeEntries");

            migrationBuilder.DropTable(
                name: "NativeCommandLogs");

            migrationBuilder.DropTable(
                name: "ChatMemoryConversations");
        }
    }
}
