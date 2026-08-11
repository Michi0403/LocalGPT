using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>
/// Represents an add human question flow.
/// </summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260731152000_AddHumanQuestionFlow")]
public partial class AddHumanQuestionFlow : Migration
{
    /// <summary>
    /// Runs the up operation.
    /// </summary>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "GateMode",
            table: "HumanCollaborationRequests",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "None");

        migrationBuilder.AddColumn<string>(
            name: "QuestionScope",
            table: "HumanCollaborationRequests",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "Member");

        migrationBuilder.AddColumn<string>(
            name: "RequestedCouncilPhase",
            table: "HumanCollaborationRequests",
            type: "TEXT",
            maxLength: 120,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<int>(
            name: "RequestedCouncilRound",
            table: "HumanCollaborationRequests",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "TargetMembersText",
            table: "HumanCollaborationRequests",
            type: "TEXT",
            maxLength: 1600,
            nullable: false,
            defaultValue: "");

        migrationBuilder.Sql(
            "UPDATE HumanCollaborationRequests SET GateMode = 'Completion' WHERE RequiredBeforeCompletion = 1 AND GateMode = 'None';");

        migrationBuilder.CreateIndex(
            name: "IX_HumanCollaborationRequests_CouncilRunId_Status_GateMode",
            table: "HumanCollaborationRequests",
            columns: new[] { "CouncilRunId", "Status", "GateMode" });
    }

    /// <summary>
    /// Runs the down operation.
    /// </summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_HumanCollaborationRequests_CouncilRunId_Status_GateMode",
            table: "HumanCollaborationRequests");

        migrationBuilder.DropColumn(name: "GateMode", table: "HumanCollaborationRequests");
        migrationBuilder.DropColumn(name: "QuestionScope", table: "HumanCollaborationRequests");
        migrationBuilder.DropColumn(name: "RequestedCouncilPhase", table: "HumanCollaborationRequests");
        migrationBuilder.DropColumn(name: "RequestedCouncilRound", table: "HumanCollaborationRequests");
        migrationBuilder.DropColumn(name: "TargetMembersText", table: "HumanCollaborationRequests");
    }
}
