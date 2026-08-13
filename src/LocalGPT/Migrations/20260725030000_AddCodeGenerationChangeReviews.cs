using System;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>
/// Defines the Entity Framework Core migration AddCodeGenerationChangeReviews, applying and reverting the schema changes represented by this versioned database step.
/// </summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260725030000_AddCodeGenerationChangeReviews")]
public partial class AddCodeGenerationChangeReviews : Migration
{
    /// <summary>
    /// Applies the schema changes defined by the <see cref="AddCodeGenerationChangeReviews"/> Entity Framework Core migration to move the database forward.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder value supplied to the add code generation change reviews operation and used when producing its result.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CodeGenerationChangeReviews",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                ProjectTopicId = table.Column<Guid>(type: "TEXT", nullable: true),
                CouncilRunId = table.Column<Guid>(type: "TEXT", nullable: true),
                Title = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                Goal = table.Column<string>(type: "TEXT", nullable: false),
                CurrentProjectState = table.Column<string>(type: "TEXT", nullable: false),
                CouncilSummary = table.Column<string>(type: "TEXT", nullable: false),
                ChangeSummary = table.Column<string>(type: "TEXT", nullable: false),
                SafetySummary = table.Column<string>(type: "TEXT", nullable: false),
                PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                ReviewHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                DecisionNote = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                WorkspaceName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                ZipFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                BuildStatus = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                ApprovalConsumed = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                DecidedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CodeGenerationChangeReviews", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CodeGenerationChangeReviews_CouncilRunId",
            table: "CodeGenerationChangeReviews",
            column: "CouncilRunId");

        migrationBuilder.CreateIndex(
            name: "IX_CodeGenerationChangeReviews_ProjectId_Status_UpdatedAtUtc",
            table: "CodeGenerationChangeReviews",
            columns: new[] { "ProjectId", "Status", "UpdatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_CodeGenerationChangeReviews_ReviewHash",
            table: "CodeGenerationChangeReviews",
            column: "ReviewHash");

        migrationBuilder.CreateIndex(
            name: "IX_CodeGenerationChangeReviews_UpdatedAtUtc",
            table: "CodeGenerationChangeReviews",
            column: "UpdatedAtUtc");
    }

    /// <summary>
    /// Reverts the schema changes defined by the <see cref="AddCodeGenerationChangeReviews"/> Entity Framework Core migration to return the database to its preceding shape.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder value supplied to the add code generation change reviews operation and used when producing its result.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CodeGenerationChangeReviews");
    }
}
