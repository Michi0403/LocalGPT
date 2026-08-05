using System;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260726000000_AddHumanCollaboration")]
public partial class AddHumanCollaboration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "HumanCollaborationRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                CouncilRunId = table.Column<Guid>(type: "TEXT", nullable: true),
                CorrelationId = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                OperationKey = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                ParameterFingerprint = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                RequestKind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                RiskLevel = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Source = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                RequestedBy = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                RequestedRole = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                SuggestedResponsesText = table.Column<string>(type: "TEXT", maxLength: 1600, nullable: false),
                ResponsePrompt = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                PrefillText = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                UserResponse = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                DecisionReason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                DecisionBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                DecisionByProfileId = table.Column<Guid>(type: "TEXT", nullable: true),
                RequestedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                DecidedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                ConsumedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                EarliestCouncilRound = table.Column<int>(type: "INTEGER", nullable: false),
                RequiredBeforeCompletion = table.Column<bool>(type: "INTEGER", nullable: false),
                IsSensitive = table.Column<bool>(type: "INTEGER", nullable: false),
                AllowFreeText = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HumanCollaborationRequests", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "HumanCouncilParticipantProfiles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                RoleName = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                Expertise = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                WorkingStyle = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: false),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                ProfileVersion = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HumanCouncilParticipantProfiles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "HumanCouncilContributions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                CouncilRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                HumanDisplayName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                HumanRole = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                Content = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                EarliestCouncilRound = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                SubmittedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                InjectedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                EvaluatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                Evaluation = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                EvaluationVerdict = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                EvaluatedAfterRound = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HumanCouncilContributions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_HumanCollaborationRequests_CorrelationId_OperationKey_RequestedAtUtc",
            table: "HumanCollaborationRequests",
            columns: new[] { "CorrelationId", "OperationKey", "RequestedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_HumanCollaborationRequests_CouncilRunId_Status",
            table: "HumanCollaborationRequests",
            columns: new[] { "CouncilRunId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_HumanCollaborationRequests_Status_UpdatedAtUtc",
            table: "HumanCollaborationRequests",
            columns: new[] { "Status", "UpdatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_HumanCouncilContributions_CouncilRunId_Status_EarliestCouncilRound",
            table: "HumanCouncilContributions",
            columns: new[] { "CouncilRunId", "Status", "EarliestCouncilRound" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "HumanCollaborationRequests");
        migrationBuilder.DropTable(name: "HumanCouncilContributions");
        migrationBuilder.DropTable(name: "HumanCouncilParticipantProfiles");
    }
}
