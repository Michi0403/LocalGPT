using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations
{
    /// <inheritdoc />
    public partial class testWithNewSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProjectWorkspaceRoots_ProjectId",
                table: "ProjectWorkspaceRoots",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBuildVerifications_RevisionId",
                table: "ProjectBuildVerifications",
                column: "RevisionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectWorkspaceRoots_ProjectId",
                table: "ProjectWorkspaceRoots");

            migrationBuilder.DropIndex(
                name: "IX_ProjectBuildVerifications_RevisionId",
                table: "ProjectBuildVerifications");
        }
    }
}
