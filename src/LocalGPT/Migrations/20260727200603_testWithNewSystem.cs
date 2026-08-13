using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations
{
    /// <summary>
    /// Defines the Entity Framework Core migration testWithNewSystem, applying and reverting the schema changes represented by this versioned database step.
    /// </summary>
    /// <inheritdoc />
    public partial class testWithNewSystem : Migration
    {
        /// <summary>
        /// Applies the schema changes defined by the <see cref="testWithNewSystem"/> Entity Framework Core migration to move the database forward.
        /// </summary>
        /// <inheritdoc />
        /// <param name="migrationBuilder">Migration builder value supplied to the test with new system operation and used when producing its result.</param>
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

        /// <summary>
        /// Reverts the schema changes defined by the <see cref="testWithNewSystem"/> Entity Framework Core migration to return the database to its preceding shape.
        /// </summary>
        /// <inheritdoc />
        /// <param name="migrationBuilder">Migration builder value supplied to the test with new system operation and used when producing its result.</param>
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
