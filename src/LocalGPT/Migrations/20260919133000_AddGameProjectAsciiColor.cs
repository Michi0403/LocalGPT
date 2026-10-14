using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>Adds durable ASCII palette defaults to the existing Project-owned game authoring profile.</summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260919133000_AddGameProjectAsciiColor")]
public partial class AddGameProjectAsciiColor : Migration
{
    /// <summary>Adds presentation metadata without moving game state into persistence.</summary>
    /// <param name="migrationBuilder">Migration builder value supplied to the add game project ascii color operation and used when producing its result.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "AsciiColorMode",
            table: "LocalGptGameProjectProfiles",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "DefaultForegroundColor",
            table: "LocalGptGameProjectProfiles",
            type: "INTEGER",
            nullable: false,
            defaultValue: 46);

        migrationBuilder.AddColumn<int>(
            name: "DefaultBackgroundColor",
            table: "LocalGptGameProjectProfiles",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);
    }

    /// <summary>
    /// Reverts the schema changes defined by the <see cref="AddGameProjectAsciiColor"/> Entity Framework Core migration to return the database to its preceding shape.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder value supplied to the add game project ascii color operation and used when producing its result.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AsciiColorMode", table: "LocalGptGameProjectProfiles");
        migrationBuilder.DropColumn(name: "DefaultForegroundColor", table: "LocalGptGameProjectProfiles");
        migrationBuilder.DropColumn(name: "DefaultBackgroundColor", table: "LocalGptGameProjectProfiles");
    }
}
