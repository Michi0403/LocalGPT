using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>Adds the user-owned Remote Control connector, action-pipeline, and bounded execution-audit tables without enabling any external endpoint by default.</summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260817135000_AddRemoteControlIntegrationFabric")]
public partial class AddRemoteControlIntegrationFabric : Migration
{
    /// <summary>Creates the additive Remote Control integration tables and their lookup indexes.</summary>
    /// <param name="migrationBuilder">Migration builder used to advance the LocalGPT SQLite schema.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "KnowledgeProfileKey",
            table: "ProjectCompilerInstallations",
            type: "TEXT",
            maxLength: 96,
            nullable: false,
            defaultValue: "");
        migrationBuilder.AddColumn<Guid>(
            name: "KnowledgeEntryId",
            table: "ProjectCompilerInstallations",
            type: "TEXT",
            nullable: true);
        migrationBuilder.AddColumn<Guid>(
            name: "VersionKnowledgeEntryId",
            table: "ProjectCompilerInstallations",
            type: "TEXT",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "ToolchainKind",
            table: "ProjectCompilerInstallations",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "");
        migrationBuilder.AddColumn<string>(
            name: "DetectedPlatform",
            table: "ProjectCompilerInstallations",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateTable(
            name: "RemoteControlConnectorDefinitions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Key = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                Transport = table.Column<int>(type: "INTEGER", nullable: false),
                Method = table.Column<int>(type: "INTEGER", nullable: false),
                UrlTemplate = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                HeadersJson = table.Column<string>(type: "TEXT", nullable: false),
                RequestBodyTemplate = table.Column<string>(type: "TEXT", nullable: false),
                RequestContentType = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                ResponseFormat = table.Column<int>(type: "INTEGER", nullable: false),
                ResponseSelector = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                PollIntervalSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                TimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                MaxPayloadBytes = table.Column<int>(type: "INTEGER", nullable: false),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                NetworkEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                AllowInsecureHttp = table.Column<bool>(type: "INTEGER", nullable: false),
                AllowedHostsJson = table.Column<string>(type: "TEXT", nullable: false),
                WebhookToken = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastAttemptUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                LastSuccessUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                LastStatus = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                LastContentType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                LastPayloadPreview = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                LastError = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_RemoteControlConnectorDefinitions", x => x.Id));

        migrationBuilder.CreateTable(
            name: "RemoteControlPipelineDefinitions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Key = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                ConnectorKey = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                Triggers = table.Column<int>(type: "INTEGER", nullable: false),
                StepsJson = table.Column<string>(type: "TEXT", nullable: false),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastAttemptUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                LastSuccessUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                LastStatus = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                LastError = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_RemoteControlPipelineDefinitions", x => x.Id));

        migrationBuilder.CreateTable(
            name: "RemoteControlExecutionRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ConnectorKey = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                PipelineKey = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                Trigger = table.Column<int>(type: "INTEGER", nullable: false),
                StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                Succeeded = table.Column<bool>(type: "INTEGER", nullable: false),
                HttpStatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                PayloadBytes = table.Column<int>(type: "INTEGER", nullable: false),
                StepCount = table.Column<int>(type: "INTEGER", nullable: false),
                Summary = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                Error = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_RemoteControlExecutionRecords", x => x.Id));

        migrationBuilder.CreateTable(
            name: "UserDxAiFunctionDefinitions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                FunctionName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Purpose = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                SafetyNotes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                ParameterSchemaJson = table.Column<string>(type: "TEXT", nullable: false),
                PipelineKey = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                AvailableToAi = table.Column<bool>(type: "INTEGER", nullable: false),
                IsReadOnly = table.Column<bool>(type: "INTEGER", nullable: false),
                RequiresHumanConfirmation = table.Column<bool>(type: "INTEGER", nullable: false),
                SupportsAutomaticInvocation = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_UserDxAiFunctionDefinitions", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_ProjectCompilerInstallations_KnowledgeProfileKey_Version", table: "ProjectCompilerInstallations", columns: new[] { "KnowledgeProfileKey", "Version" });
        migrationBuilder.CreateIndex(name: "IX_UserDxAiFunctionDefinitions_FunctionName", table: "UserDxAiFunctionDefinitions", column: "FunctionName", unique: true);
        migrationBuilder.CreateIndex(name: "IX_UserDxAiFunctionDefinitions_PipelineKey_IsEnabled", table: "UserDxAiFunctionDefinitions", columns: new[] { "PipelineKey", "IsEnabled" });
        migrationBuilder.CreateIndex(name: "IX_RemoteControlConnectorDefinitions_Key", table: "RemoteControlConnectorDefinitions", column: "Key", unique: true);
        migrationBuilder.CreateIndex(name: "IX_RemoteControlConnectorDefinitions_IsEnabled_NetworkEnabled_Transport_PollIntervalSeconds", table: "RemoteControlConnectorDefinitions", columns: new[] { "IsEnabled", "NetworkEnabled", "Transport", "PollIntervalSeconds" });
        migrationBuilder.CreateIndex(name: "IX_RemoteControlPipelineDefinitions_Key", table: "RemoteControlPipelineDefinitions", column: "Key", unique: true);
        migrationBuilder.CreateIndex(name: "IX_RemoteControlPipelineDefinitions_ConnectorKey_IsEnabled_Triggers", table: "RemoteControlPipelineDefinitions", columns: new[] { "ConnectorKey", "IsEnabled", "Triggers" });
        migrationBuilder.CreateIndex(name: "IX_RemoteControlExecutionRecords_StartedAtUtc", table: "RemoteControlExecutionRecords", column: "StartedAtUtc");
        migrationBuilder.CreateIndex(name: "IX_RemoteControlExecutionRecords_ConnectorKey_StartedAtUtc", table: "RemoteControlExecutionRecords", columns: new[] { "ConnectorKey", "StartedAtUtc" });
        migrationBuilder.CreateIndex(name: "IX_RemoteControlExecutionRecords_PipelineKey_StartedAtUtc", table: "RemoteControlExecutionRecords", columns: new[] { "PipelineKey", "StartedAtUtc" });
    }

    /// <summary>Removes only the additive Remote Control tables and leaves all pre-existing LocalGPT data untouched.</summary>
    /// <param name="migrationBuilder">Migration builder used to return the LocalGPT SQLite schema to the preceding revision.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UserDxAiFunctionDefinitions");
        migrationBuilder.DropTable(name: "RemoteControlExecutionRecords");
        migrationBuilder.DropTable(name: "RemoteControlPipelineDefinitions");
        migrationBuilder.DropTable(name: "RemoteControlConnectorDefinitions");
        migrationBuilder.DropIndex(name: "IX_ProjectCompilerInstallations_KnowledgeProfileKey_Version", table: "ProjectCompilerInstallations");
        migrationBuilder.DropColumn(name: "DetectedPlatform", table: "ProjectCompilerInstallations");
        migrationBuilder.DropColumn(name: "ToolchainKind", table: "ProjectCompilerInstallations");
        migrationBuilder.DropColumn(name: "KnowledgeProfileKey", table: "ProjectCompilerInstallations");
        migrationBuilder.DropColumn(name: "KnowledgeEntryId", table: "ProjectCompilerInstallations");
        migrationBuilder.DropColumn(name: "VersionKnowledgeEntryId", table: "ProjectCompilerInstallations");
    }
}
