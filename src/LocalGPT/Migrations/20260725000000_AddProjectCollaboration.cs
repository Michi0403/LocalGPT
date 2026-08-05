using System;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260725000000_AddProjectCollaboration")]
public partial class AddProjectCollaboration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LocalGptProjects",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Purpose = table.Column<string>(type: "TEXT", nullable: false),
                RootPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                CurrentVersion = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                RecommendGit = table.Column<bool>(type: "INTEGER", nullable: false),
                IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LocalGptProjects", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "LocalGptProjectTopics",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                IsUserApproved = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LocalGptProjectTopics", x => x.Id);
                table.ForeignKey(
                    name: "FK_LocalGptProjectTopics_LocalGptProjects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "LocalGptProjects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "LocalGptProjectVersions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                Version = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Notes = table.Column<string>(type: "TEXT", nullable: false),
                PathSnapshot = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                IsCurrent = table.Column<bool>(type: "INTEGER", nullable: false),
                IsUserConfirmed = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LocalGptProjectVersions", x => x.Id);
                table.ForeignKey(
                    name: "FK_LocalGptProjectVersions_LocalGptProjects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "LocalGptProjects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "LocalGptProjectTopicKnowledgeLinks",
            columns: table => new
            {
                ProjectTopicId = table.Column<Guid>(type: "TEXT", nullable: false),
                KnowledgeEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                LinkedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                LinkReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                LinkedByHuman = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_LocalGptProjectTopicKnowledgeLinks",
                    x => new { x.ProjectTopicId, x.KnowledgeEntryId });
                table.ForeignKey(
                    name: "FK_LocalGptProjectTopicKnowledgeLinks_CouncilKnowledgeEntries_KnowledgeEntryId",
                    column: x => x.KnowledgeEntryId,
                    principalTable: "CouncilKnowledgeEntries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_LocalGptProjectTopicKnowledgeLinks_LocalGptProjectTopics_ProjectTopicId",
                    column: x => x.ProjectTopicId,
                    principalTable: "LocalGptProjectTopics",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LocalGptProjects_IsArchived_UpdatedAtUtc",
            table: "LocalGptProjects",
            columns: new[] { "IsArchived", "UpdatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_LocalGptProjects_Name",
            table: "LocalGptProjects",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_LocalGptProjectTopics_ProjectId_Name",
            table: "LocalGptProjectTopics",
            columns: new[] { "ProjectId", "Name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_LocalGptProjectTopics_ProjectId_Status",
            table: "LocalGptProjectTopics",
            columns: new[] { "ProjectId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_LocalGptProjectVersions_ProjectId_IsCurrent",
            table: "LocalGptProjectVersions",
            columns: new[] { "ProjectId", "IsCurrent" });

        migrationBuilder.CreateIndex(
            name: "IX_LocalGptProjectVersions_ProjectId_Version",
            table: "LocalGptProjectVersions",
            columns: new[] { "ProjectId", "Version" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_LocalGptProjectTopicKnowledgeLinks_KnowledgeEntryId",
            table: "LocalGptProjectTopicKnowledgeLinks",
            column: "KnowledgeEntryId");

        migrationBuilder.CreateIndex(
            name: "IX_LocalGptProjectTopicKnowledgeLinks_LinkedAtUtc",
            table: "LocalGptProjectTopicKnowledgeLinks",
            column: "LinkedAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LocalGptProjectTopicKnowledgeLinks");
        migrationBuilder.DropTable(name: "LocalGptProjectVersions");
        migrationBuilder.DropTable(name: "LocalGptProjectTopics");
        migrationBuilder.DropTable(name: "LocalGptProjects");
    }
}
