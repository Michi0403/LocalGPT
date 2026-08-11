using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>
/// Represents an add feature persistence records.
/// </summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260803000000_AddFeaturePersistenceRecords")]
public partial class AddFeaturePersistenceRecords : Migration
{
    /// <summary>
    /// Runs the up operation.
    /// </summary>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CouncilPromptStarterConfigurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Key = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                Summary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                PromptMessage = table.Column<string>(type: "TEXT", nullable: false),
                TeamKeysJson = table.Column<string>(type: "TEXT", nullable: false),
                StartsCouncilDirectly = table.Column<bool>(type: "INTEGER", nullable: false),
                IsBuiltIn = table.Column<bool>(type: "INTEGER", nullable: false),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_CouncilPromptStarterConfigurations", x => x.Id));

        migrationBuilder.CreateTable(
            name: "LocalizationCatalogRegistrations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                CultureName = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                CatalogPath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                StringCount = table.Column<int>(type: "INTEGER", nullable: false),
                MissingBaselineKeyCount = table.Column<int>(type: "INTEGER", nullable: false),
                IsUserOverride = table.Column<bool>(type: "INTEGER", nullable: false),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_LocalizationCatalogRegistrations", x => x.Id));

        migrationBuilder.CreateTable(
            name: "DocumentationBuildRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Version = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                GeneratedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                HtmlAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                PdfAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                DocumentationMode = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                PdfMode = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                ToolSource = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                OutputRoot = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                Warning = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_DocumentationBuildRecords", x => x.Id));

        migrationBuilder.CreateTable(
            name: "EmbeddedFirmwarePlanRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                PlanKey = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                ProjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                DeviceName = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                BoardProfileKey = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                PlanJson = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmbeddedFirmwarePlanRecords", x => x.Id);
                table.ForeignKey("FK_EmbeddedFirmwarePlanRecords_LocalGptProjects_ProjectId", x => x.ProjectId, "LocalGptProjects", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CouncilGameSessionRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SessionKey = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                ConversationId = table.Column<Guid>(type: "TEXT", nullable: true),
                GameKey = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                TeamKey = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                SnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CouncilGameSessionRecords", x => x.Id);
                table.ForeignKey("FK_CouncilGameSessionRecords_ChatMemoryConversations_ConversationId", x => x.ConversationId, "ChatMemoryConversations", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_CouncilPromptStarterConfigurations_Key", "CouncilPromptStarterConfigurations", "Key", unique: true);
        migrationBuilder.CreateIndex("IX_CouncilPromptStarterConfigurations_IsEnabled_Title", "CouncilPromptStarterConfigurations", new[] { "IsEnabled", "Title" });
        migrationBuilder.CreateIndex("IX_LocalizationCatalogRegistrations_CultureName", "LocalizationCatalogRegistrations", "CultureName", unique: true);
        migrationBuilder.CreateIndex("IX_LocalizationCatalogRegistrations_IsEnabled_DisplayName", "LocalizationCatalogRegistrations", new[] { "IsEnabled", "DisplayName" });
        migrationBuilder.CreateIndex("IX_DocumentationBuildRecords_Version_GeneratedAtUtc", "DocumentationBuildRecords", new[] { "Version", "GeneratedAtUtc" });
        migrationBuilder.CreateIndex("IX_EmbeddedFirmwarePlanRecords_PlanKey", "EmbeddedFirmwarePlanRecords", "PlanKey", unique: true);
        migrationBuilder.CreateIndex("IX_EmbeddedFirmwarePlanRecords_ProjectId_UpdatedAtUtc", "EmbeddedFirmwarePlanRecords", new[] { "ProjectId", "UpdatedAtUtc" });
        migrationBuilder.CreateIndex("IX_CouncilGameSessionRecords_SessionKey", "CouncilGameSessionRecords", "SessionKey", unique: true);
        migrationBuilder.CreateIndex("IX_CouncilGameSessionRecords_GameKey_Status_UpdatedAtUtc", "CouncilGameSessionRecords", new[] { "GameKey", "Status", "UpdatedAtUtc" });
        migrationBuilder.CreateIndex("IX_CouncilGameSessionRecords_ConversationId", "CouncilGameSessionRecords", "ConversationId");
    }

    /// <summary>
    /// Runs the down operation.
    /// </summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("CouncilGameSessionRecords");
        migrationBuilder.DropTable("CouncilPromptStarterConfigurations");
        migrationBuilder.DropTable("DocumentationBuildRecords");
        migrationBuilder.DropTable("EmbeddedFirmwarePlanRecords");
        migrationBuilder.DropTable("LocalizationCatalogRegistrations");
    }
}
