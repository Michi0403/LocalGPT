using System;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>
/// Represents an add database first project architecture.
/// </summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260726010000_AddDatabaseFirstProjectArchitecture")]
public partial class AddDatabaseFirstProjectArchitecture : Migration
{
    /// <summary>
    /// Runs the up operation.
    /// </summary>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
        CREATE TABLE IF NOT EXISTS "LocalGptProjectRevisions" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_LocalGptProjectRevisions" PRIMARY KEY,
            "ProjectId" TEXT NOT NULL,
            "ParentRevisionId" TEXT NULL,
            "CreatedAtUtc" TEXT NOT NULL,
            "UpdatedAtUtc" TEXT NOT NULL,
            "BranchName" TEXT NOT NULL,
            "RevisionName" TEXT NOT NULL,
            "Summary" TEXT NOT NULL,
            "ProjectStructureJson" TEXT NOT NULL,
            "CreatedBy" TEXT NOT NULL,
            "IsCurrent" INTEGER NOT NULL,
            "IsUserApproved" INTEGER NOT NULL,
            CONSTRAINT "FK_LocalGptProjectRevisions_LocalGptProjects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "LocalGptProjects" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_LocalGptProjectRevisions_LocalGptProjectRevisions_ParentRevisionId" FOREIGN KEY ("ParentRevisionId") REFERENCES "LocalGptProjectRevisions" ("Id") ON DELETE RESTRICT
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_LocalGptProjectRevisions_ProjectId_BranchName_RevisionName" ON "LocalGptProjectRevisions" ("ProjectId", "BranchName", "RevisionName");
        CREATE INDEX IF NOT EXISTS "IX_LocalGptProjectRevisions_ProjectId_IsCurrent_UpdatedAtUtc" ON "LocalGptProjectRevisions" ("ProjectId", "IsCurrent", "UpdatedAtUtc");
        CREATE INDEX IF NOT EXISTS "IX_LocalGptProjectRevisions_ParentRevisionId" ON "LocalGptProjectRevisions" ("ParentRevisionId");

        CREATE TABLE IF NOT EXISTS "LocalGptProjectRequirements" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_LocalGptProjectRequirements" PRIMARY KEY,
            "ProjectId" TEXT NOT NULL,
            "RevisionId" TEXT NULL,
            "CreatedAtUtc" TEXT NOT NULL,
            "UpdatedAtUtc" TEXT NOT NULL,
            "Name" TEXT NOT NULL,
            "Description" TEXT NOT NULL,
            "RequirementType" TEXT NOT NULL,
            "Status" TEXT NOT NULL,
            "Priority" TEXT NOT NULL,
            "RequiredCapability" TEXT NOT NULL,
            "SourceKind" TEXT NOT NULL,
            "CouncilRating" INTEGER NOT NULL,
            "IsUserApproved" INTEGER NOT NULL,
            CONSTRAINT "FK_LocalGptProjectRequirements_LocalGptProjects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "LocalGptProjects" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_LocalGptProjectRequirements_LocalGptProjectRevisions_RevisionId" FOREIGN KEY ("RevisionId") REFERENCES "LocalGptProjectRevisions" ("Id") ON DELETE RESTRICT
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_LocalGptProjectRequirements_ProjectId_Name" ON "LocalGptProjectRequirements" ("ProjectId", "Name");
        CREATE INDEX IF NOT EXISTS "IX_LocalGptProjectRequirements_ProjectId_Status_Priority" ON "LocalGptProjectRequirements" ("ProjectId", "Status", "Priority");
        CREATE INDEX IF NOT EXISTS "IX_LocalGptProjectRequirements_RevisionId" ON "LocalGptProjectRequirements" ("RevisionId");

        CREATE TABLE IF NOT EXISTS "LocalGptProjectRequirementLinks" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_LocalGptProjectRequirementLinks" PRIMARY KEY,
            "RequirementId" TEXT NOT NULL,
            "LinkedAtUtc" TEXT NOT NULL,
            "TargetKind" TEXT NOT NULL,
            "TargetName" TEXT NOT NULL,
            "TargetId" TEXT NOT NULL,
            "TargetTable" TEXT NOT NULL,
            "LinkPurpose" TEXT NOT NULL,
            "CouncilReviewStatus" TEXT NOT NULL,
            "IsUserApproved" INTEGER NOT NULL,
            CONSTRAINT "FK_LocalGptProjectRequirementLinks_LocalGptProjectRequirements_RequirementId" FOREIGN KEY ("RequirementId") REFERENCES "LocalGptProjectRequirements" ("Id") ON DELETE RESTRICT
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_LocalGptProjectRequirementLinks_RequirementId_TargetKind_TargetName" ON "LocalGptProjectRequirementLinks" ("RequirementId", "TargetKind", "TargetName");

        CREATE TABLE IF NOT EXISTS "LocalGptProjectArtifacts" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_LocalGptProjectArtifacts" PRIMARY KEY,
            "ProjectId" TEXT NOT NULL,
            "RevisionId" TEXT NULL,
            "RequirementId" TEXT NULL,
            "CreatedAtUtc" TEXT NOT NULL,
            "UpdatedAtUtc" TEXT NOT NULL,
            "ArtifactKind" TEXT NOT NULL,
            "Name" TEXT NOT NULL,
            "Value" TEXT NOT NULL,
            "DataType" TEXT NOT NULL,
            "Flags" TEXT NOT NULL,
            "Description" TEXT NOT NULL,
            "CouncilReviewStatus" TEXT NOT NULL,
            "IsSensitive" INTEGER NOT NULL,
            "IsUserApproved" INTEGER NOT NULL,
            CONSTRAINT "FK_LocalGptProjectArtifacts_LocalGptProjects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "LocalGptProjects" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_LocalGptProjectArtifacts_LocalGptProjectRevisions_RevisionId" FOREIGN KEY ("RevisionId") REFERENCES "LocalGptProjectRevisions" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_LocalGptProjectArtifacts_LocalGptProjectRequirements_RequirementId" FOREIGN KEY ("RequirementId") REFERENCES "LocalGptProjectRequirements" ("Id") ON DELETE RESTRICT
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_LocalGptProjectArtifacts_ProjectId_ArtifactKind_Name" ON "LocalGptProjectArtifacts" ("ProjectId", "ArtifactKind", "Name");
        CREATE INDEX IF NOT EXISTS "IX_LocalGptProjectArtifacts_ProjectId_IsUserApproved_UpdatedAtUtc" ON "LocalGptProjectArtifacts" ("ProjectId", "IsUserApproved", "UpdatedAtUtc");
        CREATE INDEX IF NOT EXISTS "IX_LocalGptProjectArtifacts_RevisionId" ON "LocalGptProjectArtifacts" ("RevisionId");
        CREATE INDEX IF NOT EXISTS "IX_LocalGptProjectArtifacts_RequirementId" ON "LocalGptProjectArtifacts" ("RequirementId");

        CREATE TABLE IF NOT EXISTS "ProjectDocumentImports" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_ProjectDocumentImports" PRIMARY KEY,
            "ProjectId" TEXT NOT NULL,
            "RevisionId" TEXT NULL,
            "ImportedAtUtc" TEXT NOT NULL,
            "SourceName" TEXT NOT NULL,
            "SourceUri" TEXT NOT NULL,
            "ContentHash" TEXT NOT NULL,
            "ContentType" TEXT NOT NULL,
            "EncodingName" TEXT NOT NULL,
            "ExtractedText" TEXT NOT NULL,
            "Status" TEXT NOT NULL,
            "SafetyNotes" TEXT NOT NULL,
            "IsUserApproved" INTEGER NOT NULL,
            CONSTRAINT "FK_ProjectDocumentImports_LocalGptProjects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "LocalGptProjects" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_ProjectDocumentImports_LocalGptProjectRevisions_RevisionId" FOREIGN KEY ("RevisionId") REFERENCES "LocalGptProjectRevisions" ("Id") ON DELETE RESTRICT
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProjectDocumentImports_ProjectId_ContentHash" ON "ProjectDocumentImports" ("ProjectId", "ContentHash");
        CREATE INDEX IF NOT EXISTS "IX_ProjectDocumentImports_RevisionId" ON "ProjectDocumentImports" ("RevisionId");

        CREATE TABLE IF NOT EXISTS "CouncilModelPresets" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_CouncilModelPresets" PRIMARY KEY,
            "CreatedAtUtc" TEXT NOT NULL,
            "UpdatedAtUtc" TEXT NOT NULL,
            "Name" TEXT NOT NULL,
            "Description" TEXT NOT NULL,
            "ModelNamesJson" TEXT NOT NULL,
            "MaxOutputTokens" INTEGER NOT NULL,
            "MaxContextTokens" INTEGER NOT NULL,
            "MaxParallelModels" INTEGER NOT NULL,
            "OllamaNumGpu" INTEGER NULL,
            "IncludeMemory" INTEGER NOT NULL,
            "GenerateArtifacts" INTEGER NOT NULL,
            "CreateProjectPerRun" INTEGER NOT NULL,
            "IsDefault" INTEGER NOT NULL,
            "IsArchived" INTEGER NOT NULL,
            "IsUserApproved" INTEGER NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_CouncilModelPresets_Name" ON "CouncilModelPresets" ("Name");
        CREATE INDEX IF NOT EXISTS "IX_CouncilModelPresets_IsArchived_IsDefault_UpdatedAtUtc" ON "CouncilModelPresets" ("IsArchived", "IsDefault", "UpdatedAtUtc");

        CREATE TABLE IF NOT EXISTS "SqliteEditorFieldOverrides" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_SqliteEditorFieldOverrides" PRIMARY KEY,
            "UpdatedAtUtc" TEXT NOT NULL,
            "TableName" TEXT NOT NULL,
            "ColumnName" TEXT NOT NULL,
            "EditorKind" TEXT NOT NULL,
            "InputMask" TEXT NOT NULL,
            "FormatString" TEXT NOT NULL,
            "NullText" TEXT NOT NULL,
            "IsSensitive" INTEGER NOT NULL,
            "RequireHumanApproval" INTEGER NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_SqliteEditorFieldOverrides_TableName_ColumnName" ON "SqliteEditorFieldOverrides" ("TableName", "ColumnName");

        CREATE TABLE IF NOT EXISTS "CouncilKnowledgeUserRatings" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_CouncilKnowledgeUserRatings" PRIMARY KEY,
            "KnowledgeEntryId" TEXT NOT NULL,
            "CreatedAtUtc" TEXT NOT NULL,
            "UpdatedAtUtc" TEXT NOT NULL,
            "Rating" INTEGER NOT NULL,
            "AccuracyStatus" TEXT NOT NULL,
            "Notes" TEXT NOT NULL,
            "ApprovedForCouncilUse" INTEGER NOT NULL,
            "RatedBy" TEXT NOT NULL,
            CONSTRAINT "FK_CouncilKnowledgeUserRatings_CouncilKnowledgeEntries_KnowledgeEntryId" FOREIGN KEY ("KnowledgeEntryId") REFERENCES "CouncilKnowledgeEntries" ("Id") ON DELETE RESTRICT
        );
        CREATE INDEX IF NOT EXISTS "IX_CouncilKnowledgeUserRatings_KnowledgeEntryId_UpdatedAtUtc" ON "CouncilKnowledgeUserRatings" ("KnowledgeEntryId", "UpdatedAtUtc");
        """);
    }

    /// <summary>
    /// Runs the down operation.
    /// </summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CouncilKnowledgeUserRatings");
        migrationBuilder.DropTable(name: "SqliteEditorFieldOverrides");
        migrationBuilder.DropTable(name: "CouncilModelPresets");
        migrationBuilder.DropTable(name: "ProjectDocumentImports");
        migrationBuilder.DropTable(name: "LocalGptProjectRequirementLinks");
        migrationBuilder.DropTable(name: "LocalGptProjectArtifacts");
        migrationBuilder.DropTable(name: "LocalGptProjectRequirements");
        migrationBuilder.DropTable(name: "LocalGptProjectRevisions");
    }
}
