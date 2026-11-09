using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LocalGPT.Migrations;

/// <summary>
/// 7.0.0 generic game engine: game project profiles name engine primitives instead of a hardcoded runtime profile,
/// and own their declarative rules (stats, archetypes, actions, rules, levels, texts, aliases).
/// </summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20261108200000_AddGameEngineRules")]
public sealed class AddGameEngineRules : Migration
{
    /// <summary>Adds the engine primitive columns and the rule tables and maps the former runtime profile.</summary>
    /// <param name="migrationBuilder">Builder that records the schema operations.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ViewKey", table: "LocalGptGameProjectProfiles", type: "TEXT", maxLength: 80, nullable: false, defaultValue: "first-person-raycast");
        migrationBuilder.AddColumn<string>(name: "ViewParametersJson", table: "LocalGptGameProjectProfiles", type: "TEXT", nullable: false, defaultValue: "{}");
        migrationBuilder.AddColumn<string>(name: "TurnModelKey", table: "LocalGptGameProjectProfiles", type: "TEXT", maxLength: 40, nullable: false, defaultValue: "actions");
        migrationBuilder.AddColumn<string>(name: "MapGeneratorKey", table: "LocalGptGameProjectProfiles", type: "TEXT", maxLength: 80, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "MapParametersJson", table: "LocalGptGameProjectProfiles", type: "TEXT", nullable: false, defaultValue: "{}");
        migrationBuilder.AddColumn<string>(name: "AutoplayKey", table: "LocalGptGameProjectProfiles", type: "TEXT", maxLength: 80, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "AutoplayParametersJson", table: "LocalGptGameProjectProfiles", type: "TEXT", nullable: false, defaultValue: "{}");
        migrationBuilder.AddColumn<string>(name: "PlayerArchetypeKey", table: "LocalGptGameProjectProfiles", type: "TEXT", maxLength: 80, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "RulesetTemplateKey", table: "LocalGptGameProjectProfiles", type: "TEXT", maxLength: 160, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<bool>(name: "IsSystemSeed", table: "LocalGptGameProjectProfiles", type: "INTEGER", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "IsUserModified", table: "LocalGptGameProjectProfiles", type: "INTEGER", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<int>(name: "SeedVersion", table: "LocalGptGameProjectProfiles", type: "INTEGER", nullable: false, defaultValue: 0);
        migrationBuilder.CreateTable(name: "GameProjectStats", columns: table => new
        {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
            StatKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
            DisplayName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
            MinimumValue = table.Column<int>(type: "INTEGER", nullable: false),
            MaximumValue = table.Column<int>(type: "INTEGER", nullable: false),
            InitialValue = table.Column<int>(type: "INTEGER", nullable: false),
            SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_GameProjectStats", x => x.Id);
            table.ForeignKey(name: "FK_GameProjectStats_LocalGptGameProjectProfiles_ProfileId", column: x => x.ProfileId,
                principalTable: "LocalGptGameProjectProfiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex(name: "IX_GameProjectStats_ProfileId_StatKey", table: "GameProjectStats", columns: new[] { "ProfileId", "StatKey" }, unique: true);
        migrationBuilder.CreateTable(name: "GameProjectArchetypes", columns: table => new
        {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
            ArchetypeKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
            DisplayName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
            Role = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
            Glyph = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
            BehaviourKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
            ParametersJson = table.Column<string>(type: "TEXT", nullable: false),
            SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_GameProjectArchetypes", x => x.Id);
            table.ForeignKey(name: "FK_GameProjectArchetypes_LocalGptGameProjectProfiles_ProfileId", column: x => x.ProfileId,
                principalTable: "LocalGptGameProjectProfiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex(name: "IX_GameProjectArchetypes_ProfileId_ArchetypeKey", table: "GameProjectArchetypes", columns: new[] { "ProfileId", "ArchetypeKey" }, unique: true);
        migrationBuilder.CreateTable(name: "GameProjectActions", columns: table => new
        {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
            ActionKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
            DisplayName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
            KeyboardKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
            GamepadButton = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
            PrimitiveKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
            ParametersJson = table.Column<string>(type: "TEXT", nullable: false),
            AiSelectable = table.Column<bool>(type: "INTEGER", nullable: false),
            SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_GameProjectActions", x => x.Id);
            table.ForeignKey(name: "FK_GameProjectActions_LocalGptGameProjectProfiles_ProfileId", column: x => x.ProfileId,
                principalTable: "LocalGptGameProjectProfiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex(name: "IX_GameProjectActions_ProfileId_ActionKey", table: "GameProjectActions", columns: new[] { "ProfileId", "ActionKey" }, unique: true);
        migrationBuilder.CreateTable(name: "GameProjectRules", columns: table => new
        {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
            RuleKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
            Trigger = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
            PrimitiveKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
            ParametersJson = table.Column<string>(type: "TEXT", nullable: false),
            IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
            SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_GameProjectRules", x => x.Id);
            table.ForeignKey(name: "FK_GameProjectRules_LocalGptGameProjectProfiles_ProfileId", column: x => x.ProfileId,
                principalTable: "LocalGptGameProjectProfiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex(name: "IX_GameProjectRules_ProfileId_RuleKey", table: "GameProjectRules", columns: new[] { "ProfileId", "RuleKey" }, unique: true);
        migrationBuilder.CreateTable(name: "GameProjectLevels", columns: table => new
        {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
            LevelIndex = table.Column<int>(type: "INTEGER", nullable: false),
            LevelNumber = table.Column<int>(type: "INTEGER", nullable: false),
            Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
            Difficulty = table.Column<int>(type: "INTEGER", nullable: false),
            MapWidth = table.Column<int>(type: "INTEGER", nullable: false),
            MapHeight = table.Column<int>(type: "INTEGER", nullable: false),
            ParametersJson = table.Column<string>(type: "TEXT", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_GameProjectLevels", x => x.Id);
            table.ForeignKey(name: "FK_GameProjectLevels_LocalGptGameProjectProfiles_ProfileId", column: x => x.ProfileId,
                principalTable: "LocalGptGameProjectProfiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex(name: "IX_GameProjectLevels_ProfileId_LevelIndex", table: "GameProjectLevels", columns: new[] { "ProfileId", "LevelIndex" }, unique: true);
        migrationBuilder.CreateTable(name: "GameProjectTexts", columns: table => new
        {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
            TextKey = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
            Text = table.Column<string>(type: "TEXT", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_GameProjectTexts", x => x.Id);
            table.ForeignKey(name: "FK_GameProjectTexts_LocalGptGameProjectProfiles_ProfileId", column: x => x.ProfileId,
                principalTable: "LocalGptGameProjectProfiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex(name: "IX_GameProjectTexts_ProfileId_TextKey", table: "GameProjectTexts", columns: new[] { "ProfileId", "TextKey" }, unique: true);
        migrationBuilder.CreateTable(name: "GameProjectAliases", columns: table => new
        {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
            AliasKind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
            Alias = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
            Target = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_GameProjectAliases", x => x.Id);
            table.ForeignKey(name: "FK_GameProjectAliases_LocalGptGameProjectProfiles_ProfileId", column: x => x.ProfileId,
                principalTable: "LocalGptGameProjectProfiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex(name: "IX_GameProjectAliases_ProfileId_AliasKind_Alias", table: "GameProjectAliases", columns: new[] { "ProfileId", "AliasKind", "Alias" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_LocalGptGameProjectProfiles_GameKey", table: "LocalGptGameProjectProfiles", column: "GameKey");
        // Existing user projects keep their behaviour: the former Corridor profile (0) uses the rules of the seeded
        // ascii-arena game (the former fixed corridor world of project games), the former Story profile (1) those of
        // the seeded green-dragon game.
        migrationBuilder.Sql("UPDATE \"LocalGptGameProjectProfiles\" SET \"ViewKey\" = CASE \"RuntimeProfile\" WHEN 1 THEN 'text-template' ELSE 'first-person-raycast' END, \"RulesetTemplateKey\" = CASE \"RuntimeProfile\" WHEN 1 THEN 'green-dragon' ELSE 'ascii-arena' END;");
        migrationBuilder.DropColumn(name: "RuntimeProfile", table: "LocalGptGameProjectProfiles");

        // Council teams: the game-specific execution mode became the generic game-engine step.
        migrationBuilder.Sql("UPDATE \"CouncilTeamConfigurations\" SET \"WorkflowStepsJson\" = REPLACE(\"WorkflowStepsJson\", '\"SystemKernelTournamentResolution\"', '\"SystemGameEngineResolution\"') WHERE \"WorkflowStepsJson\" LIKE '%SystemKernelTournamentResolution%';");
        // The lineup step was recognized by its key; it now carries the declarative GameEngineInitializes flag.
        migrationBuilder.Sql("UPDATE \"CouncilTeamConfigurations\" SET \"WorkflowStepsJson\" = REPLACE(\"WorkflowStepsJson\", '\"key\":\"arena-lineup-engine\"', '\"key\":\"arena-lineup-engine\",\"gameEngineInitializes\":true') WHERE \"WorkflowStepsJson\" LIKE '%\"key\":\"arena-lineup-engine\"%' AND \"WorkflowStepsJson\" NOT LIKE '%gameEngineInitializes%';");
        // User-edited tournament teams keep the bounded battle context and the active-pair limit as step settings.
        const string battleContext =
            "\"limitToActiveGameEntrants\":true," +
            "\"contextStepKeys\":[\"trainer-creature-selection\",\"creature-introduction\",\"arena-lineup-engine\",\"trainer-round-command\",\"fight-round\",\"arena-round-engine\"]," +
            "\"contextIdentityStepKeys\":[\"trainer-creature-selection\",\"creature-introduction\"]," +
            "\"contextIdentityStepLimit\":8,\"contextRecentStepLimit\":8";
        foreach (var battleStep in new[] { "trainer-round-command", "fight-round" })
        {
            var stepKey = "\"key\":\"" + battleStep + "\"";
            migrationBuilder.Sql(
                "UPDATE \"CouncilTeamConfigurations\" SET \"WorkflowStepsJson\" = REPLACE(\"WorkflowStepsJson\", '" + stepKey + "', '" + stepKey + "," + battleContext + "') " +
                "WHERE \"Key\" = 'kernel-creature-tournament' AND \"WorkflowStepsJson\" NOT LIKE '%" + stepKey + ",\"limitToActiveGameEntrants\"%';");
        }
        // The first experimental arena preset used a third-party franchise key; an untouched system seed row is renamed
        // to LocalGPT's own team key and refreshed by the team seeding (formerly done in C# on every start).
        migrationBuilder.Sql("UPDATE \"CouncilTeamConfigurations\" SET \"Key\" = 'kernel-creature-tournament', \"SeedVersion\" = 0 WHERE \"Key\" = 'pokemon-tournament' AND \"IsSystemSeed\" = 1 AND \"IsUserModified\" = 0 AND NOT EXISTS (SELECT 1 FROM \"CouncilTeamConfigurations\" AS modern WHERE modern.\"Key\" = 'kernel-creature-tournament');");
    }

    /// <summary>Restores the runtime profile column and removes the rule tables and engine primitive columns.</summary>
    /// <param name="migrationBuilder">Builder that records the schema operations.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "RuntimeProfile", table: "LocalGptGameProjectProfiles", type: "INTEGER", nullable: false, defaultValue: 0);
        migrationBuilder.Sql("UPDATE \"LocalGptGameProjectProfiles\" SET \"RuntimeProfile\" = CASE \"ViewKey\" WHEN 'text-template' THEN 1 ELSE 0 END;");
        migrationBuilder.Sql("UPDATE \"CouncilTeamConfigurations\" SET \"WorkflowStepsJson\" = REPLACE(\"WorkflowStepsJson\", '\"SystemGameEngineResolution\"', '\"SystemKernelTournamentResolution\"') WHERE \"WorkflowStepsJson\" LIKE '%SystemGameEngineResolution%';");
        migrationBuilder.DropTable(name: "GameProjectAliases");
        migrationBuilder.DropTable(name: "GameProjectTexts");
        migrationBuilder.DropTable(name: "GameProjectLevels");
        migrationBuilder.DropTable(name: "GameProjectRules");
        migrationBuilder.DropTable(name: "GameProjectActions");
        migrationBuilder.DropTable(name: "GameProjectArchetypes");
        migrationBuilder.DropTable(name: "GameProjectStats");
        migrationBuilder.DropIndex(name: "IX_LocalGptGameProjectProfiles_GameKey", table: "LocalGptGameProjectProfiles");
        migrationBuilder.DropColumn(name: "ViewKey", table: "LocalGptGameProjectProfiles");
        migrationBuilder.DropColumn(name: "ViewParametersJson", table: "LocalGptGameProjectProfiles");
        migrationBuilder.DropColumn(name: "TurnModelKey", table: "LocalGptGameProjectProfiles");
        migrationBuilder.DropColumn(name: "MapGeneratorKey", table: "LocalGptGameProjectProfiles");
        migrationBuilder.DropColumn(name: "MapParametersJson", table: "LocalGptGameProjectProfiles");
        migrationBuilder.DropColumn(name: "AutoplayKey", table: "LocalGptGameProjectProfiles");
        migrationBuilder.DropColumn(name: "AutoplayParametersJson", table: "LocalGptGameProjectProfiles");
        migrationBuilder.DropColumn(name: "PlayerArchetypeKey", table: "LocalGptGameProjectProfiles");
        migrationBuilder.DropColumn(name: "RulesetTemplateKey", table: "LocalGptGameProjectProfiles");
        migrationBuilder.DropColumn(name: "IsSystemSeed", table: "LocalGptGameProjectProfiles");
        migrationBuilder.DropColumn(name: "IsUserModified", table: "LocalGptGameProjectProfiles");
        migrationBuilder.DropColumn(name: "SeedVersion", table: "LocalGptGameProjectProfiles");
    }
}
