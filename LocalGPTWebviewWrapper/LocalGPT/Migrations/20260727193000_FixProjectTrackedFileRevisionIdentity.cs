using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260727193000_FixProjectTrackedFileRevisionIdentity")]
public partial class FixProjectTrackedFileRevisionIdentity : Migration
{
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
