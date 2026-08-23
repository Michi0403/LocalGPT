using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>
/// Adds structured Council-knowledge-to-regex relationships so aliases, classifications, extraction rules and other
/// recognition semantics no longer need to be encoded only in free-form knowledge tags or content.
/// </summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260823145500_AddKnowledgeRegexRelationships")]
public partial class AddKnowledgeRegexRelationships : Migration
{
    /// <summary>Adds the composite knowledge/regex relationship table plus indexes used by enabled-role and recency queries.</summary>
    /// <param name="migrationBuilder">Migration builder used to advance the LocalGPT SQLite schema.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CouncilKnowledgeRegexPatternLinks",
            columns: table => new
            {
                KnowledgeEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                RegexPatternId = table.Column<int>(type: "INTEGER", nullable: false),
                LinkPurpose = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                Meaning = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                LinkedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                LinkedByHuman = table.Column<bool>(type: "INTEGER", nullable: false),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_CouncilKnowledgeRegexPatternLinks",
                    x => new { x.KnowledgeEntryId, x.RegexPatternId });
                table.ForeignKey(
                    name: "FK_CouncilKnowledgeRegexPatternLinks_CouncilKnowledgeEntries_KnowledgeEntryId",
                    column: x => x.KnowledgeEntryId,
                    principalTable: "CouncilKnowledgeEntries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CouncilKnowledgeRegexPatternLinks_RegexPatterns_RegexPatternId",
                    column: x => x.RegexPatternId,
                    principalTable: "RegexPatterns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CouncilKnowledgeRegexPatternLinks_RegexPatternId",
            table: "CouncilKnowledgeRegexPatternLinks",
            column: "RegexPatternId");
        migrationBuilder.CreateIndex(
            name: "IX_CouncilKnowledgeRegexPatternLinks_IsEnabled_LinkPurpose",
            table: "CouncilKnowledgeRegexPatternLinks",
            columns: new[] { "IsEnabled", "LinkPurpose" });
        migrationBuilder.CreateIndex(
            name: "IX_CouncilKnowledgeRegexPatternLinks_LinkedAtUtc",
            table: "CouncilKnowledgeRegexPatternLinks",
            column: "LinkedAtUtc");
    }

    /// <summary>Reverts this migration by dropping only the knowledge/regex relationship table and its owned indexes.</summary>
    /// <param name="migrationBuilder">Migration builder used to return the schema to the preceding revision.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CouncilKnowledgeRegexPatternLinks");
    }
}
