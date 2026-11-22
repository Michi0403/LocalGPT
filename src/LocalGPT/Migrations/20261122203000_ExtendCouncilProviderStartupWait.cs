using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LocalGPT.Migrations;

/// <summary>Updates the original Council startup allowance on existing installations.</summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20261122203000_ExtendCouncilProviderStartupWait")]
public sealed class ExtendCouncilProviderStartupWait : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "SystemVariables"
            SET "ValueString" = json_set("ValueString", '$.maximumProviderStartupWaitSeconds', 300),
                "LastUpdated" = CURRENT_TIMESTAMP
            WHERE "Name" = 'CouncilExecutionRuntimeParametersJson'
              AND CASE WHEN json_valid("ValueString")
                  THEN json_extract("ValueString", '$.maximumProviderStartupWaitSeconds') = 120
                  ELSE 0 END;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // A five-minute value may have been operator-selected. Keep the saved preference on downgrade.
    }
}
