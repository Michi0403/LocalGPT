using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LocalGPT.Migrations;

/// <summary>Retains exact benchmark evidence for each reviewed adapter and task role.</summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20261126100000_AddModelAdapterRevisions")]
public sealed class AddModelAdapterRevisions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ModelAdapterRevisions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                BenchmarkRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                BaseModelKey = table.Column<string>(type: "TEXT", nullable: false),
                AdapterModelKey = table.Column<string>(type: "TEXT", nullable: false),
                BaseDigest = table.Column<string>(type: "TEXT", nullable: false),
                AdapterDigest = table.Column<string>(type: "TEXT", nullable: false),
                ScenarioSha256 = table.Column<string>(type: "TEXT", nullable: false),
                TeamKey = table.Column<string>(type: "TEXT", nullable: false),
                Role = table.Column<string>(type: "TEXT", nullable: false),
                ProfileName = table.Column<string>(type: "TEXT", nullable: false),
                BasePassed = table.Column<int>(type: "INTEGER", nullable: false),
                AdapterPassed = table.Column<int>(type: "INTEGER", nullable: false),
                CaseCount = table.Column<int>(type: "INTEGER", nullable: false),
                Qualified = table.Column<bool>(type: "INTEGER", nullable: false),
                ReviewedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ModelAdapterRevisions", item => item.Id));
        migrationBuilder.CreateIndex("IX_ModelAdapterRevisions_AdapterModelKey", "ModelAdapterRevisions", "AdapterModelKey");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable("ModelAdapterRevisions");
}
