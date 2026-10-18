using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LocalGPT.Migrations;

/// <summary>Persists user-configurable Council project creation and completion evidence contracts.</summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20261018120000_AddCouncilWorkflowExecutionContracts")]
public sealed class AddCouncilWorkflowExecutionContracts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ProjectProvisioningMode",
            table: "CouncilTeamConfigurations",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);
        migrationBuilder.AddColumn<string>(
            name: "RequiredCompletedFunctionsJson",
            table: "CouncilTeamConfigurations",
            type: "TEXT",
            nullable: false,
            defaultValue: "[]");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "RequiredCompletedFunctionsJson", table: "CouncilTeamConfigurations");
        migrationBuilder.DropColumn(name: "ProjectProvisioningMode", table: "CouncilTeamConfigurations");
    }
}
