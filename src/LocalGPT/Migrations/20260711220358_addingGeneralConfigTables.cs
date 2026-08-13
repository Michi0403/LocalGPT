using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations
{
    /// <summary>
    /// Defines the Entity Framework Core migration addingGeneralConfigTables, applying and reverting the schema changes represented by this versioned database step.
    /// </summary>
    /// <inheritdoc />
    public partial class addingGeneralConfigTables : Migration
    {
        /// <summary>
        /// Applies the schema changes defined by the <see cref="addingGeneralConfigTables"/> Entity Framework Core migration to move the database forward.
        /// </summary>
        /// <inheritdoc />
        /// <param name="migrationBuilder">Migration builder value supplied to the adding general config tables operation and used when producing its result.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Prompts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prompts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegexPatterns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Pattern = table.Column<string>(type: "TEXT", nullable: false),
                    Flags = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegexPatterns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemVariables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ValueString = table.Column<string>(type: "TEXT", nullable: false),
                    DataType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemVariables", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_Key_Language",
                table: "Prompts",
                columns: new[] { "Key", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegexPatterns_Name",
                table: "RegexPatterns",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemVariables_Name",
                table: "SystemVariables",
                column: "Name",
                unique: true);
        }

        /// <summary>
        /// Reverts the schema changes defined by the <see cref="addingGeneralConfigTables"/> Entity Framework Core migration to return the database to its preceding shape.
        /// </summary>
        /// <inheritdoc />
        /// <param name="migrationBuilder">Migration builder value supplied to the adding general config tables operation and used when producing its result.</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Prompts");

            migrationBuilder.DropTable(
                name: "RegexPatterns");

            migrationBuilder.DropTable(
                name: "SystemVariables");
        }
    }
}
