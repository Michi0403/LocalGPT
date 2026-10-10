using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>Persists resumable per-file learning evidence without altering tracked-file identity.</summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20261010210000_AddTrackedFileLearningChecklist")]
public partial class AddTrackedFileLearningChecklist : Migration
{
    /// <summary>Adds the learning checklist to each existing project/revision tracked file.</summary>
    /// <param name="migrationBuilder">EF migration operations.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "LearningStatus", table: "LocalGptProjectTrackedFiles", type: "TEXT", maxLength: 32, nullable: false, defaultValue: "Pending");
        migrationBuilder.AddColumn<long>(name: "LearningReadCharacters", table: "LocalGptProjectTrackedFiles", type: "INTEGER", nullable: false, defaultValue: 0L);
        migrationBuilder.AddColumn<bool>(name: "LearningReadComplete", table: "LocalGptProjectTrackedFiles", type: "INTEGER", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>(name: "LearningReviewedHash", table: "LocalGptProjectTrackedFiles", type: "TEXT", maxLength: 128, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "LearningEvidence", table: "LocalGptProjectTrackedFiles", type: "TEXT", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "LearningBlockReason", table: "LocalGptProjectTrackedFiles", type: "TEXT", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<DateTime>(name: "LearningUpdatedAtUtc", table: "LocalGptProjectTrackedFiles", type: "TEXT", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "LearningCompletedAtUtc", table: "LocalGptProjectTrackedFiles", type: "TEXT", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_LocalGptProjectTrackedFiles_ProjectId_RevisionId_LearningStatus", table: "LocalGptProjectTrackedFiles", columns: new[] { "ProjectId", "RevisionId", "LearningStatus" });
    }

    /// <summary>Removes only checklist fields while keeping project-file tracking intact.</summary>
    /// <param name="migrationBuilder">EF migration operations.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_LocalGptProjectTrackedFiles_ProjectId_RevisionId_LearningStatus", table: "LocalGptProjectTrackedFiles");
        migrationBuilder.DropColumn(name: "LearningStatus", table: "LocalGptProjectTrackedFiles");
        migrationBuilder.DropColumn(name: "LearningReadCharacters", table: "LocalGptProjectTrackedFiles");
        migrationBuilder.DropColumn(name: "LearningReadComplete", table: "LocalGptProjectTrackedFiles");
        migrationBuilder.DropColumn(name: "LearningReviewedHash", table: "LocalGptProjectTrackedFiles");
        migrationBuilder.DropColumn(name: "LearningEvidence", table: "LocalGptProjectTrackedFiles");
        migrationBuilder.DropColumn(name: "LearningBlockReason", table: "LocalGptProjectTrackedFiles");
        migrationBuilder.DropColumn(name: "LearningUpdatedAtUtc", table: "LocalGptProjectTrackedFiles");
        migrationBuilder.DropColumn(name: "LearningCompletedAtUtc", table: "LocalGptProjectTrackedFiles");
    }
}
