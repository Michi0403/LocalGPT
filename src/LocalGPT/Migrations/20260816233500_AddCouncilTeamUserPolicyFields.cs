using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>
/// Adds the persisted Council-team policy fields introduced after the original team-scripting schema.
/// Existing installations receive backward-compatible defaults so their configured teams remain usable.
/// </summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260816233500_AddCouncilTeamUserPolicyFields")]
public partial class AddCouncilTeamUserPolicyFields : Migration
{
    /// <summary>Adds the missing persisted Council team columns while preserving every existing team row.</summary>
    /// <param name="migrationBuilder">Migration builder used to advance the LocalGPT SQLite schema.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AllowedAutomaticFunctionsJson",
            table: "CouncilTeamConfigurations",
            type: "TEXT",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<bool>(
            name: "IsDeleted",
            table: "CouncilTeamConfigurations",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "AllMembersReadinessPreflightMode",
            table: "CouncilTeamConfigurations",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<bool>(
            name: "IncludeAllMembersReadinessPreflightInWorkflowContext",
            table: "CouncilTeamConfigurations",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "AllMembersReadinessPreflightMaxOutputTokens",
            table: "CouncilTeamConfigurations",
            type: "INTEGER",
            nullable: false,
            defaultValue: 192);

        migrationBuilder.AddColumn<string>(
            name: "AllMembersReadinessPreflightPromptTemplate",
            table: "CouncilTeamConfigurations",
            type: "TEXT",
            nullable: false,
            defaultValue: "");
    }

    /// <summary>Returns the Council team table to its pre-policy schema by removing the six additive user-policy columns introduced for configurable automatic functions, deletion tombstones and readiness preflight settings.</summary>
    /// <param name="migrationBuilder">Migration builder used to return the LocalGPT SQLite schema to the preceding revision.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AllowedAutomaticFunctionsJson", table: "CouncilTeamConfigurations");
        migrationBuilder.DropColumn(name: "IsDeleted", table: "CouncilTeamConfigurations");
        migrationBuilder.DropColumn(name: "AllMembersReadinessPreflightMode", table: "CouncilTeamConfigurations");
        migrationBuilder.DropColumn(name: "IncludeAllMembersReadinessPreflightInWorkflowContext", table: "CouncilTeamConfigurations");
        migrationBuilder.DropColumn(name: "AllMembersReadinessPreflightMaxOutputTokens", table: "CouncilTeamConfigurations");
        migrationBuilder.DropColumn(name: "AllMembersReadinessPreflightPromptTemplate", table: "CouncilTeamConfigurations");
    }
}
