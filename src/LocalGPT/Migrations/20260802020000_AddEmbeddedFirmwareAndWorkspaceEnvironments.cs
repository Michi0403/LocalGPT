using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations
{
    /// <summary>
    /// Defines the Entity Framework Core migration AddEmbeddedFirmwareAndWorkspaceEnvironments, applying and reverting the schema changes represented by this versioned database step.
    /// </summary>
    [DbContext(typeof(LocalGptMemoryDbContext))]
    [Migration("20260802020000_AddEmbeddedFirmwareAndWorkspaceEnvironments")]
    public partial class AddEmbeddedFirmwareAndWorkspaceEnvironments : Migration
    {
        /// <summary>
        /// Applies the schema changes defined by the <see cref="AddEmbeddedFirmwareAndWorkspaceEnvironments"/> Entity Framework Core migration to move the database forward.
        /// </summary>
        /// <param name="migrationBuilder">Migration builder value supplied to the add embedded firmware and workspace environments operation and used when producing its result.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "AccessPolicyJson", table: "ProjectWorkspaceRoots", type: "TEXT", nullable: false, defaultValue: "[]");
            migrationBuilder.AddColumn<string>(name: "BuildArguments", table: "ProjectWorkspaceRoots", type: "TEXT", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "DefaultSubdirectoriesJson", table: "ProjectWorkspaceRoots", type: "TEXT", nullable: false, defaultValue: "[\"src\",\"docs\",\"tests\",\"artifacts\"]");
            migrationBuilder.AddColumn<string>(name: "EnvironmentKind", table: "ProjectWorkspaceRoots", type: "TEXT", maxLength: 80, nullable: false, defaultValue: "LocalHost");
            migrationBuilder.AddColumn<string>(name: "EnvironmentRootPath", table: "ProjectWorkspaceRoots", type: "TEXT", maxLength: 2048, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "EnvironmentVariablesJson", table: "ProjectWorkspaceRoots", type: "TEXT", nullable: false, defaultValue: "{}");
            migrationBuilder.AddColumn<string>(name: "ExpectedStructureRegex", table: "ProjectWorkspaceRoots", type: "TEXT", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<DateTime>(name: "LastPermissionCheckedAtUtc", table: "ProjectWorkspaceRoots", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "LastPermissionReadAccess", table: "ProjectWorkspaceRoots", type: "INTEGER", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "LastPermissionStatus", table: "ProjectWorkspaceRoots", type: "TEXT", maxLength: 80, nullable: false, defaultValue: "NotChecked");
            migrationBuilder.AddColumn<string>(name: "LastPermissionSummary", table: "ProjectWorkspaceRoots", type: "TEXT", maxLength: 4000, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<bool>(name: "LastPermissionWriteAccess", table: "ProjectWorkspaceRoots", type: "INTEGER", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<Guid>(name: "PreferredCompilerInstallationId", table: "ProjectWorkspaceRoots", type: "TEXT", nullable: true);
        }

        /// <summary>
        /// Reverts the schema changes defined by the <see cref="AddEmbeddedFirmwareAndWorkspaceEnvironments"/> Entity Framework Core migration to return the database to its preceding shape.
        /// </summary>
        /// <param name="migrationBuilder">Migration builder value supplied to the add embedded firmware and workspace environments operation and used when producing its result.</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AccessPolicyJson", table: "ProjectWorkspaceRoots");
            migrationBuilder.DropColumn(name: "BuildArguments", table: "ProjectWorkspaceRoots");
            migrationBuilder.DropColumn(name: "DefaultSubdirectoriesJson", table: "ProjectWorkspaceRoots");
            migrationBuilder.DropColumn(name: "EnvironmentKind", table: "ProjectWorkspaceRoots");
            migrationBuilder.DropColumn(name: "EnvironmentRootPath", table: "ProjectWorkspaceRoots");
            migrationBuilder.DropColumn(name: "EnvironmentVariablesJson", table: "ProjectWorkspaceRoots");
            migrationBuilder.DropColumn(name: "ExpectedStructureRegex", table: "ProjectWorkspaceRoots");
            migrationBuilder.DropColumn(name: "LastPermissionCheckedAtUtc", table: "ProjectWorkspaceRoots");
            migrationBuilder.DropColumn(name: "LastPermissionReadAccess", table: "ProjectWorkspaceRoots");
            migrationBuilder.DropColumn(name: "LastPermissionStatus", table: "ProjectWorkspaceRoots");
            migrationBuilder.DropColumn(name: "LastPermissionSummary", table: "ProjectWorkspaceRoots");
            migrationBuilder.DropColumn(name: "LastPermissionWriteAccess", table: "ProjectWorkspaceRoots");
            migrationBuilder.DropColumn(name: "PreferredCompilerInstallationId", table: "ProjectWorkspaceRoots");
        }
    }
}
