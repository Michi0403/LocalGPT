using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>Seeds database-owned Council seed sampling defaults introduced by LocalGPT 5.4.0.</summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20261009170000_AddCouncilCurationAndSeedSamplingPolicy")]
public partial class AddCouncilCurationAndSeedSamplingPolicy : Migration
{
    /// <summary>Adds missing supplied-team random-range participant defaults without overwriting user-edited runtime variables.</summary>
    /// <param name="migrationBuilder">Migration builder used to update persisted runtime policy.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT OR IGNORE INTO "SystemVariables" ("Name", "ValueString", "DataType", "LastUpdated")
            VALUES
            ('CouncilSeedRoleMinimumAiParticipants','2','System.Int32',CURRENT_TIMESTAMP),
            ('CouncilSeedRoleMaximumAiParticipants','3','System.Int32',CURRENT_TIMESTAMP);
            """);
    }

    /// <summary>Removes only the 5.4.0 supplied-team sampling defaults when the migration is rolled back.</summary>
    /// <param name="migrationBuilder">Migration builder used to revert persisted runtime policy.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM "SystemVariables"
            WHERE "Name" IN ('CouncilSeedRoleMinimumAiParticipants', 'CouncilSeedRoleMaximumAiParticipants');
            """);
    }
}
