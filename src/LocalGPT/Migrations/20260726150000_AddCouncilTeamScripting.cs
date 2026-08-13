using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>
/// Defines the Entity Framework Core migration AddCouncilTeamScripting, applying and reverting the schema changes represented by this versioned database step.
/// </summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260726150000_AddCouncilTeamScripting")]
public partial class AddCouncilTeamScripting : Migration
{
    /// <summary>
    /// Applies the schema changes defined by the <see cref="AddCouncilTeamScripting"/> Entity Framework Core migration to move the database forward.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder value supplied to the add council team scripting operation and used when producing its result.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
        CREATE TABLE IF NOT EXISTS "CouncilTeamConfigurations" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_CouncilTeamConfigurations" PRIMARY KEY,
            "Key" TEXT NOT NULL,
            "DisplayName" TEXT NOT NULL,
            "Purpose" TEXT NOT NULL,
            "RolesJson" TEXT NOT NULL,
            "PreferredCapabilitiesJson" TEXT NOT NULL,
            "ArchitectureContractsJson" TEXT NOT NULL,
            "WorkflowStepsJson" TEXT NOT NULL,
            "ExpertPreparationPromptTemplate" TEXT NOT NULL,
            "LeaderSynthesisPromptTemplate" TEXT NOT NULL,
            "MainRoundInstructionTemplate" TEXT NOT NULL,
            "SeedVersion" INTEGER NOT NULL DEFAULT 1,
            "IsSystemSeed" INTEGER NOT NULL DEFAULT 1,
            "IsUserModified" INTEGER NOT NULL DEFAULT 0,
            "IsEnabled" INTEGER NOT NULL DEFAULT 1,
            "CreatedAtUtc" TEXT NOT NULL,
            "UpdatedAtUtc" TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_CouncilTeamConfigurations_Key" ON "CouncilTeamConfigurations" ("Key");
        CREATE INDEX IF NOT EXISTS "IX_CouncilTeamConfigurations_IsEnabled_UpdatedAtUtc" ON "CouncilTeamConfigurations" ("IsEnabled", "UpdatedAtUtc");
        """);
    }

    /// <summary>
    /// Reverts the schema changes defined by the <see cref="AddCouncilTeamScripting"/> Entity Framework Core migration to return the database to its preceding shape.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder value supplied to the add council team scripting operation and used when producing its result.</param>
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "CouncilTeamConfigurations");
}
