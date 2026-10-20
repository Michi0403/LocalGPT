using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LocalGPT.Migrations;

/// <summary>Adds serialized, user-editable recovery and team-follow-on policies to Council team persistence.</summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20261019220000_AddCouncilConfigurableRecoveryAndFollowOn")]
public sealed class AddCouncilConfigurableRecoveryAndFollowOn : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "CompletionRecoveryJson", table: "CouncilTeamConfigurations", type: "TEXT", nullable: false, defaultValue: "{}");
        migrationBuilder.AddColumn<string>(name: "FollowOnPolicyJson", table: "CouncilTeamConfigurations", type: "TEXT", nullable: false, defaultValue: "{}");
        migrationBuilder.AddColumn<string>(name: "CompletionNoticeTemplate", table: "CouncilTeamConfigurations", type: "TEXT", nullable: false, defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CompletionRecoveryJson", table: "CouncilTeamConfigurations");
        migrationBuilder.DropColumn(name: "FollowOnPolicyJson", table: "CouncilTeamConfigurations");
        migrationBuilder.DropColumn(name: "CompletionNoticeTemplate", table: "CouncilTeamConfigurations");
    }
}
