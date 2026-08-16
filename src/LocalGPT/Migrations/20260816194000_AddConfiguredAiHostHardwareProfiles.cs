using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>Adds durable physical-host hardware definitions used by configured provider endpoints and benchmarks.</summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260816194000_AddConfiguredAiHostHardwareProfiles")]
public partial class AddConfiguredAiHostHardwareProfiles : Migration
{
    /// <summary>Creates the durable configured physical-host hardware table and lookup indexes.</summary>
    /// <param name="migrationBuilder">EF Core migration builder receiving the schema operations.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ConfiguredAiHostHardwareProfiles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                HostKey = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                HostName = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                OperatingSystem = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                Architecture = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                CpuName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                SystemMemoryBytes = table.Column<long>(type: "INTEGER", nullable: true),
                GpusJson = table.Column<string>(type: "TEXT", nullable: false),
                ProviderEndpointsJson = table.Column<string>(type: "TEXT", nullable: false),
                SourceKind = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                Confidence = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                IsUserConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastDetectedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_ConfiguredAiHostHardwareProfiles", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_ConfiguredAiHostHardwareProfiles_HostKey",
            table: "ConfiguredAiHostHardwareProfiles",
            column: "HostKey",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_ConfiguredAiHostHardwareProfiles_UpdatedAtUtc",
            table: "ConfiguredAiHostHardwareProfiles",
            column: "UpdatedAtUtc");
    }

    /// <summary>Rolls back LocalGPT host-hardware persistence by dropping the table created for configured physical AI hosts.</summary>
    /// <param name="migrationBuilder">EF Core migration builder receiving the rollback operation.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "ConfiguredAiHostHardwareProfiles");
}
