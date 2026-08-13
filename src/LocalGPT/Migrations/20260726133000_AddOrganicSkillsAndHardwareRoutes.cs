using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>
/// Defines the Entity Framework Core migration AddOrganicSkillsAndHardwareRoutes, applying and reverting the schema changes represented by this versioned database step.
/// </summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260726133000_AddOrganicSkillsAndHardwareRoutes")]
public partial class AddOrganicSkillsAndHardwareRoutes : Migration
{
    /// <summary>
    /// Applies the schema changes defined by the <see cref="AddOrganicSkillsAndHardwareRoutes"/> Entity Framework Core migration to move the database forward.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder value supplied to the add organic skills and hardware routes operation and used when producing its result.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ModelRoutesJson",
            table: "CouncilModelPresets",
            type: "TEXT",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<bool>(
            name: "AllowParallelHardwareRoads",
            table: "CouncilModelPresets",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);

        migrationBuilder.Sql("""
        CREATE TABLE IF NOT EXISTS "OrganicSkills" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_OrganicSkills" PRIMARY KEY,
            "Key" TEXT NOT NULL,
            "DisplayName" TEXT NOT NULL,
            "Description" TEXT NOT NULL,
            "SourcePeerId" TEXT NOT NULL,
            "OrgansJson" TEXT NOT NULL,
            "CapabilityKeysJson" TEXT NOT NULL,
            "UiActivationKeysJson" TEXT NOT NULL,
            "IsOnline" INTEGER NOT NULL,
            "IsEnabled" INTEGER NOT NULL,
            "IsUserApproved" INTEGER NOT NULL,
            "CreatedAtUtc" TEXT NOT NULL,
            "UpdatedAtUtc" TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_OrganicSkills_Key" ON "OrganicSkills" ("Key");
        CREATE INDEX IF NOT EXISTS "IX_OrganicSkills_IsEnabled_IsOnline_UpdatedAtUtc" ON "OrganicSkills" ("IsEnabled", "IsOnline", "UpdatedAtUtc");

        CREATE TABLE IF NOT EXISTS "ProjectOrganicSkillLinks" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_ProjectOrganicSkillLinks" PRIMARY KEY,
            "ProjectId" TEXT NOT NULL,
            "SkillId" TEXT NOT NULL,
            "IsRequired" INTEGER NOT NULL,
            "IsEnabled" INTEGER NOT NULL,
            "Notes" TEXT NOT NULL,
            "UpdatedAtUtc" TEXT NOT NULL,
            CONSTRAINT "FK_ProjectOrganicSkillLinks_LocalGptProjects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "LocalGptProjects" ("Id") ON DELETE CASCADE,
            CONSTRAINT "FK_ProjectOrganicSkillLinks_OrganicSkills_SkillId" FOREIGN KEY ("SkillId") REFERENCES "OrganicSkills" ("Id") ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProjectOrganicSkillLinks_ProjectId_SkillId" ON "ProjectOrganicSkillLinks" ("ProjectId", "SkillId");
        CREATE INDEX IF NOT EXISTS "IX_ProjectOrganicSkillLinks_ProjectId_IsEnabled_IsRequired" ON "ProjectOrganicSkillLinks" ("ProjectId", "IsEnabled", "IsRequired");
        CREATE INDEX IF NOT EXISTS "IX_ProjectOrganicSkillLinks_SkillId" ON "ProjectOrganicSkillLinks" ("SkillId");

        CREATE TABLE IF NOT EXISTS "CouncilMemberOrganicSkillLinks" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_CouncilMemberOrganicSkillLinks" PRIMARY KEY,
            "MemberKey" TEXT NOT NULL,
            "SkillId" TEXT NOT NULL,
            "Proficiency" INTEGER NOT NULL,
            "IsSelfRevealed" INTEGER NOT NULL,
            "IsEnabled" INTEGER NOT NULL,
            "Evidence" TEXT NOT NULL,
            "DxFunctionsJson" TEXT NOT NULL,
            "ControllerMethodsJson" TEXT NOT NULL,
            "OrganicCapabilitiesJson" TEXT NOT NULL,
            "UpdatedAtUtc" TEXT NOT NULL,
            CONSTRAINT "FK_CouncilMemberOrganicSkillLinks_OrganicSkills_SkillId" FOREIGN KEY ("SkillId") REFERENCES "OrganicSkills" ("Id") ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_CouncilMemberOrganicSkillLinks_MemberKey_SkillId" ON "CouncilMemberOrganicSkillLinks" ("MemberKey", "SkillId");
        CREATE INDEX IF NOT EXISTS "IX_CouncilMemberOrganicSkillLinks_MemberKey_IsEnabled_Proficiency" ON "CouncilMemberOrganicSkillLinks" ("MemberKey", "IsEnabled", "Proficiency");
        CREATE INDEX IF NOT EXISTS "IX_CouncilMemberOrganicSkillLinks_SkillId" ON "CouncilMemberOrganicSkillLinks" ("SkillId");
        """);
    }

    /// <summary>
    /// Reverts the schema changes defined by the <see cref="AddOrganicSkillsAndHardwareRoutes"/> Entity Framework Core migration to return the database to its preceding shape.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder value supplied to the add organic skills and hardware routes operation and used when producing its result.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CouncilMemberOrganicSkillLinks");
        migrationBuilder.DropTable(name: "ProjectOrganicSkillLinks");
        migrationBuilder.DropTable(name: "OrganicSkills");
        migrationBuilder.DropColumn(name: "AllowParallelHardwareRoads", table: "CouncilModelPresets");
        migrationBuilder.DropColumn(name: "ModelRoutesJson", table: "CouncilModelPresets");
    }
}
