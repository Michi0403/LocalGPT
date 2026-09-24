using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>Adds database-backed runtime extension definitions for scripts and compiled plugins.</summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260924170000_AddRuntimePlugins")]
public partial class AddRuntimePlugins : Migration
{
    /// <summary>Creates the persisted runtime extension table.</summary>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RuntimePluginDefinitions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                FunctionName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Purpose = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                SafetyNotes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                ParameterSchemaJson = table.Column<string>(type: "TEXT", nullable: false),
                Kind = table.Column<int>(type: "INTEGER", nullable: false),
                SourceCode = table.Column<string>(type: "TEXT", nullable: false),
                PackagePayloadBase64 = table.Column<string>(type: "TEXT", nullable: false),
                EntryAssemblyName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                EntryTypeName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                CompilerInstallationId = table.Column<Guid>(type: "TEXT", nullable: true),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                AvailableToAi = table.Column<bool>(type: "INTEGER", nullable: false),
                IsReadOnly = table.Column<bool>(type: "INTEGER", nullable: false),
                RequiresHumanConfirmation = table.Column<bool>(type: "INTEGER", nullable: false),
                SupportsAutomaticInvocation = table.Column<bool>(type: "INTEGER", nullable: false),
                ContentHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                LastBuildStatus = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                LastBuildMessage = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                LastLoadedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_RuntimePluginDefinitions", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_RuntimePluginDefinitions_FunctionName",
            table: "RuntimePluginDefinitions",
            column: "FunctionName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RuntimePluginDefinitions_IsEnabled_AvailableToAi_UpdatedAtUtc",
            table: "RuntimePluginDefinitions",
            columns: new[] { "IsEnabled", "AvailableToAi", "UpdatedAtUtc" });
    }

    /// <summary>Removes the runtime extension persistence table.</summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "RuntimePluginDefinitions");
    }
}
