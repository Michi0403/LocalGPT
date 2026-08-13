using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>
/// Defines the Entity Framework Core migration AddCouncilRuntimeClasses, applying and reverting the schema changes represented by this versioned database step.
/// </summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260802010000_AddCouncilRuntimeClasses")]
public partial class AddCouncilRuntimeClasses : Migration
{
    /// <summary>
    /// Applies the schema changes defined by the <see cref="AddCouncilRuntimeClasses"/> Entity Framework Core migration to move the database forward.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder value supplied to the add council runtime classes operation and used when producing its result.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
        CREATE TABLE IF NOT EXISTS "CouncilRuntimeClassConfigurations" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_CouncilRuntimeClassConfigurations" PRIMARY KEY,
            "Key" TEXT NOT NULL,
            "Namespace" TEXT NOT NULL,
            "DisplayName" TEXT NOT NULL,
            "Kind" TEXT NOT NULL,
            "Description" TEXT NOT NULL,
            "FieldsJson" TEXT NOT NULL,
            "InputBindingsJson" TEXT NOT NULL,
            "RecommendedDxFunctionsJson" TEXT NOT NULL,
            "SourceReferencesJson" TEXT NOT NULL,
            "IsEnabled" INTEGER NOT NULL DEFAULT 1,
            "IsSystemSeed" INTEGER NOT NULL DEFAULT 0,
            "IsUserModified" INTEGER NOT NULL DEFAULT 0,
            "SeedVersion" INTEGER NOT NULL DEFAULT 0,
            "CreatedAtUtc" TEXT NOT NULL,
            "UpdatedAtUtc" TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_CouncilRuntimeClassConfigurations_Key"
            ON "CouncilRuntimeClassConfigurations" ("Key");
        CREATE INDEX IF NOT EXISTS "IX_CouncilRuntimeClassConfigurations_Namespace_Kind_IsEnabled"
            ON "CouncilRuntimeClassConfigurations" ("Namespace", "Kind", "IsEnabled");
        """);
    }

    /// <summary>
    /// Reverts the schema changes defined by the <see cref="AddCouncilRuntimeClasses"/> Entity Framework Core migration to return the database to its preceding shape.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder value supplied to the add council runtime classes operation and used when producing its result.</param>
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "CouncilRuntimeClassConfigurations");
}
