using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>
/// Defines the Entity Framework Core migration FixProjectTrackedFileRevisionIdentity, applying and reverting the schema changes represented by this versioned database step.
/// </summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260727193000_FixProjectTrackedFileRevisionIdentity")]
public partial class FixProjectTrackedFileRevisionIdentity : Migration
{
    /// <summary>
    /// Applies the schema changes defined by the <see cref="FixProjectTrackedFileRevisionIdentity"/> Entity Framework Core migration to move the database forward.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder value supplied to the fix project tracked file revision identity operation and used when producing its result.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_LocalGptProjectTrackedFiles_ProjectId_ProjectRelativePath",
            table: "LocalGptProjectTrackedFiles");

        migrationBuilder.CreateIndex(
            name: "IX_LocalGptProjectTrackedFiles_ProjectId_RevisionId_ProjectRelativePath",
            table: "LocalGptProjectTrackedFiles",
            columns: new[] { "ProjectId", "RevisionId", "ProjectRelativePath" },
            unique: true);
    }

    /// <summary>
    /// Reverts the schema changes defined by the <see cref="FixProjectTrackedFileRevisionIdentity"/> Entity Framework Core migration to return the database to its preceding shape.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder value supplied to the fix project tracked file revision identity operation and used when producing its result.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_LocalGptProjectTrackedFiles_ProjectId_RevisionId_ProjectRelativePath",
            table: "LocalGptProjectTrackedFiles");

        migrationBuilder.CreateIndex(
            name: "IX_LocalGptProjectTrackedFiles_ProjectId_ProjectRelativePath",
            table: "LocalGptProjectTrackedFiles",
            columns: new[] { "ProjectId", "ProjectRelativePath" },
            unique: true);
    }
}
