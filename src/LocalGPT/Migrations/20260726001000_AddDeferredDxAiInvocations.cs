using System;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>
/// Represents an add deferred DevExpress ai invocations.
/// </summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260726001000_AddDeferredDxAiInvocations")]
public partial class AddDeferredDxAiInvocations : Migration
{
    /// <summary>
    /// Runs the up operation.
    /// </summary>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DeferredDxAiInvocations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ApprovalRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                CouncilRunId = table.Column<Guid>(type: "TEXT", nullable: true),
                OperationId = table.Column<Guid>(type: "TEXT", nullable: false),
                CorrelationId = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                FunctionName = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                ParametersJson = table.Column<string>(type: "TEXT", maxLength: 64000, nullable: false),
                ConfirmationSummaryHash = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                RequestedBy = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                ConversationId = table.Column<Guid>(type: "TEXT", nullable: true),
                ProjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                ProjectVersionId = table.Column<Guid>(type: "TEXT", nullable: true),
                ApplicationVersion = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ResultStatus = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                ResultSummary = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastAttemptAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DeferredDxAiInvocations", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DeferredDxAiInvocations_ApprovalRequestId",
            table: "DeferredDxAiInvocations",
            column: "ApprovalRequestId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DeferredDxAiInvocations_CouncilRunId_Status_CreatedAtUtc",
            table: "DeferredDxAiInvocations",
            columns: new[] { "CouncilRunId", "Status", "CreatedAtUtc" });
    }

    /// <summary>
    /// Runs the down operation.
    /// </summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DeferredDxAiInvocations");
    }
}
