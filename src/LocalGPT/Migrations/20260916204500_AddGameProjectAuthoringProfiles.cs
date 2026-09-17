using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>
/// Adds the first-class Game-project authoring profile owned by the existing LocalGPT Project system.
/// </summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260916204500_AddGameProjectAuthoringProfiles")]
public partial class AddGameProjectAuthoringProfiles : Migration
{
    /// <summary>Creates the one-to-one Project-owned game-authoring table and its identity/update indexes without moving editable design state into the runtime session schema.</summary>
    /// <param name="migrationBuilder">Migration builder used to apply the schema change.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LocalGptGameProjectProfiles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                GameKey = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                RuntimeProfile = table.Column<int>(type: "INTEGER", nullable: false),
                DefaultTeamKey = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                DefaultControlMode = table.Column<int>(type: "INTEGER", nullable: false),
                DirectorMode = table.Column<int>(type: "INTEGER", nullable: false),
                GameDirectorModelName = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                CreatureDirectorCount = table.Column<int>(type: "INTEGER", nullable: false),
                AutoplayDelayMilliseconds = table.Column<int>(type: "INTEGER", nullable: false),
                FrameWidth = table.Column<int>(type: "INTEGER", nullable: false),
                FrameHeight = table.Column<int>(type: "INTEGER", nullable: false),
                MapSeed = table.Column<int>(type: "INTEGER", nullable: false),
                ScenarioPrompt = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LocalGptGameProjectProfiles", x => x.Id);
                table.ForeignKey(
                    name: "FK_LocalGptGameProjectProfiles_LocalGptProjects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "LocalGptProjects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LocalGptGameProjectProfiles_ProjectId",
            table: "LocalGptGameProjectProfiles",
            column: "ProjectId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_LocalGptGameProjectProfiles_UpdatedAtUtc",
            table: "LocalGptGameProjectProfiles",
            column: "UpdatedAtUtc");
    }

    /// <summary>Reverts the Game-project authoring persistence boundary by dropping the dedicated profile table created by this migration.</summary>
    /// <param name="migrationBuilder">Migration builder used to revert the schema change.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LocalGptGameProjectProfiles");
    }
}
