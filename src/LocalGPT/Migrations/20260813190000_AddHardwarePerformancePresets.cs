using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>
/// Adds durable hardware-spooler performance profiles so measured benchmark token/hardware settings can be
/// selected independently from Council membership presets.
/// </summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260813190000_AddHardwarePerformancePresets")]
public partial class AddHardwarePerformancePresets : Migration
{
    /// <summary>
    /// Applies the schema changes defined by the <see cref="AddHardwarePerformancePresets"/> Entity Framework Core migration to move the database forward.
    /// </summary>
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "HardwarePerformancePresets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                ModelRoutesJson = table.Column<string>(type: "TEXT", nullable: false),
                ResourceLoadPercent = table.Column<int>(type: "INTEGER", nullable: false),
                SourceRunId = table.Column<Guid>(type: "TEXT", nullable: true),
                SourceKind = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                IsUserApproved = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HardwarePerformancePresets", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_HardwarePerformancePresets_Name",
            table: "HardwarePerformancePresets",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_HardwarePerformancePresets_SourceRunId",
            table: "HardwarePerformancePresets",
            column: "SourceRunId");

        migrationBuilder.CreateIndex(
            name: "IX_HardwarePerformancePresets_IsArchived_IsDefault_UpdatedAtUtc",
            table: "HardwarePerformancePresets",
            columns: new[] { "IsArchived", "IsDefault", "UpdatedAtUtc" });
    }

    /// <summary>
    /// Reverts the schema changes defined by the <see cref="AddHardwarePerformancePresets"/> Entity Framework Core migration to return the database to its preceding shape.
    /// </summary>
    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "HardwarePerformancePresets");
    }
}
