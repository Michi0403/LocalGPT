using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LocalGPT.Migrations;

[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20261029123000_AddCouncilRoleAssignments")]
public sealed class AddCouncilRoleAssignments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "CouncilRoleIndexes", columns: table => new
        {
            TeamKey = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
            RoleName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            Responsibility = table.Column<string>(type: "TEXT", nullable: false),
            SelectionMode = table.Column<int>(type: "INTEGER", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_CouncilRoleIndexes", x => new { x.TeamKey, x.RoleName }));
        migrationBuilder.CreateTable(name: "CouncilRoleModelBindings", columns: table => new
        {
            TeamKey = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
            RoleName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            ModelKey = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_CouncilRoleModelBindings", x => new { x.TeamKey, x.RoleName, x.ModelKey });
            table.ForeignKey(name: "FK_CouncilRoleModelBindings_CouncilRoleIndexes_TeamKey_RoleName",
                columns: x => new { x.TeamKey, x.RoleName }, principalTable: "CouncilRoleIndexes",
                principalColumns: new[] { "TeamKey", "RoleName" }, onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex(name: "IX_CouncilRoleIndexes_RoleName", table: "CouncilRoleIndexes", column: "RoleName");
        migrationBuilder.CreateIndex(name: "IX_CouncilRoleModelBindings_ModelKey", table: "CouncilRoleModelBindings", column: "ModelKey");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CouncilRoleModelBindings");
        migrationBuilder.DropTable(name: "CouncilRoleIndexes");
    }
}
