using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalGPT.Migrations;

/// <summary>
/// Represents an add project maintenance workspaces.
/// </summary>
[DbContext(typeof(LocalGptMemoryDbContext))]
[Migration("20260727190000_AddProjectMaintenanceWorkspaces")]
public partial class AddProjectMaintenanceWorkspaces : Migration
{
    /// <summary>
    /// Runs the up operation.
    /// </summary>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
        ALTER TABLE "LocalGptProjects" ADD COLUMN "ProjectType" TEXT NOT NULL DEFAULT 'DotNetSolution';
        ALTER TABLE "LocalGptProjects" ADD COLUMN "SolutionPath" TEXT NOT NULL DEFAULT '';
        ALTER TABLE "LocalGptProjects" ADD COLUMN "SolutionSearchPattern" TEXT NOT NULL DEFAULT '(?i)\.(sln|slnx)$';
        ALTER TABLE "LocalGptProjects" ADD COLUMN "FileIncludePattern" TEXT NOT NULL DEFAULT '(?s).*';
        ALTER TABLE "LocalGptProjects" ADD COLUMN "FileExcludePattern" TEXT NOT NULL DEFAULT '(?i)(^|[\\/])(bin|obj|node_modules|\.git|\.vs|artifacts|security|secrets?)([\\/]|$)|(^|[\\/])(\.env(?:\..*)?|[^\\/]+\.(?:pfx|p12|key|pem))$';

        ALTER TABLE "LocalGptProjectRevisions" ADD COLUMN "CompileVerified" INTEGER NOT NULL DEFAULT 0;
        ALTER TABLE "LocalGptProjectRevisions" ADD COLUMN "CouncilVerified" INTEGER NOT NULL DEFAULT 0;
        ALTER TABLE "LocalGptProjectRevisions" ADD COLUMN "ReadyForTesting" INTEGER NOT NULL DEFAULT 0;
        ALTER TABLE "LocalGptProjectRevisions" ADD COLUMN "ApprovedForTestingAtUtc" TEXT NULL;
        ALTER TABLE "LocalGptProjectRevisions" ADD COLUMN "SourceSnapshotHash" TEXT NOT NULL DEFAULT '';
        ALTER TABLE "LocalGptProjectRevisions" ADD COLUMN "SnapshotArchivePath" TEXT NOT NULL DEFAULT '';
        ALTER TABLE "LocalGptProjectRevisions" ADD COLUMN "SourceRootPath" TEXT NOT NULL DEFAULT '';
        ALTER TABLE "LocalGptProjectRevisions" ADD COLUMN "SolutionPath" TEXT NOT NULL DEFAULT '';

        ALTER TABLE "CodeGenerationChangeReviews" ADD COLUMN "ProjectRevisionId" TEXT NULL;
        CREATE INDEX IF NOT EXISTS "IX_CodeGenerationChangeReviews_ProjectRevisionId" ON "CodeGenerationChangeReviews" ("ProjectRevisionId");

        CREATE TABLE IF NOT EXISTS "ProjectWorkspaceRoots" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_ProjectWorkspaceRoots" PRIMARY KEY,
            "CreatedAtUtc" TEXT NOT NULL,
            "UpdatedAtUtc" TEXT NOT NULL,
            "Name" TEXT NOT NULL,
            "RootPath" TEXT NOT NULL,
            "ScopeKind" TEXT NOT NULL,
            "ProjectId" TEXT NULL,
            "ProjectTypePattern" TEXT NOT NULL,
            "SolutionPattern" TEXT NOT NULL,
            "Priority" INTEGER NOT NULL,
            "IsDefault" INTEGER NOT NULL,
            "IsEnabled" INTEGER NOT NULL,
            "LastResolvedAtUtc" TEXT NULL,
            "LastResolutionStatus" TEXT NOT NULL,
            CONSTRAINT "FK_ProjectWorkspaceRoots_LocalGptProjects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "LocalGptProjects" ("Id") ON DELETE RESTRICT
        );
        CREATE INDEX IF NOT EXISTS "IX_ProjectWorkspaceRoots_ScopeKind_ProjectId_Priority" ON "ProjectWorkspaceRoots" ("ScopeKind", "ProjectId", "Priority");
        CREATE INDEX IF NOT EXISTS "IX_ProjectWorkspaceRoots_RootPath" ON "ProjectWorkspaceRoots" ("RootPath");

        CREATE TABLE IF NOT EXISTS "ProjectCompilerInstallations" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_ProjectCompilerInstallations" PRIMARY KEY,
            "CreatedAtUtc" TEXT NOT NULL,
            "UpdatedAtUtc" TEXT NOT NULL,
            "Name" TEXT NOT NULL,
            "Language" TEXT NOT NULL,
            "ExecutablePath" TEXT NOT NULL,
            "CompilerHomePath" TEXT NOT NULL,
            "Version" TEXT NOT NULL,
            "Architecture" TEXT NOT NULL,
            "DiscoverySource" TEXT NOT NULL,
            "ValidationArguments" TEXT NOT NULL,
            "EnvironmentVariablesJson" TEXT NOT NULL,
            "IsEnabled" INTEGER NOT NULL,
            "IsDefaultForLanguage" INTEGER NOT NULL,
            "LastValidatedAtUtc" TEXT NULL,
            "LastValidationSucceeded" INTEGER NOT NULL,
            "LastValidationMessage" TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProjectCompilerInstallations_ExecutablePath" ON "ProjectCompilerInstallations" ("ExecutablePath");
        CREATE INDEX IF NOT EXISTS "IX_ProjectCompilerInstallations_Language_IsDefaultForLanguage_IsEnabled" ON "ProjectCompilerInstallations" ("Language", "IsDefaultForLanguage", "IsEnabled");

        CREATE TABLE IF NOT EXISTS "LocalGptProjectTrackedFiles" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_LocalGptProjectTrackedFiles" PRIMARY KEY,
            "ProjectId" TEXT NOT NULL,
            "RevisionId" TEXT NULL,
            "StableFileKey" TEXT NOT NULL,
            "AbsolutePath" TEXT NOT NULL,
            "ProjectRelativePath" TEXT NOT NULL,
            "WorkspaceRelativePath" TEXT NOT NULL,
            "SolutionPath" TEXT NOT NULL,
            "ProjectFilePath" TEXT NOT NULL,
            "FileName" TEXT NOT NULL,
            "Extension" TEXT NOT NULL,
            "ContentType" TEXT NOT NULL,
            "EncodingName" TEXT NOT NULL,
            "FileRole" TEXT NOT NULL,
            "StructureRegex" TEXT NOT NULL,
            "ContentFormatRegex" TEXT NOT NULL,
            "ContentHash" TEXT NOT NULL,
            "SizeBytes" INTEGER NOT NULL,
            "LastWriteTimeUtc" TEXT NULL,
            "LastSeenAtUtc" TEXT NOT NULL,
            "Exists" INTEGER NOT NULL,
            "IsGenerated" INTEGER NOT NULL,
            "IsUserApproved" INTEGER NOT NULL,
            CONSTRAINT "FK_LocalGptProjectTrackedFiles_LocalGptProjects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "LocalGptProjects" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_LocalGptProjectTrackedFiles_LocalGptProjectRevisions_RevisionId" FOREIGN KEY ("RevisionId") REFERENCES "LocalGptProjectRevisions" ("Id") ON DELETE RESTRICT
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_LocalGptProjectTrackedFiles_ProjectId_ProjectRelativePath" ON "LocalGptProjectTrackedFiles" ("ProjectId", "ProjectRelativePath");
        CREATE INDEX IF NOT EXISTS "IX_LocalGptProjectTrackedFiles_ProjectId_RevisionId_Exists" ON "LocalGptProjectTrackedFiles" ("ProjectId", "RevisionId", "Exists");

        CREATE TABLE IF NOT EXISTS "ProjectBuildVerifications" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_ProjectBuildVerifications" PRIMARY KEY,
            "ProjectId" TEXT NOT NULL,
            "RevisionId" TEXT NOT NULL,
            "CompilerInstallationId" TEXT NULL,
            "StartedAtUtc" TEXT NOT NULL,
            "CompletedAtUtc" TEXT NULL,
            "Configuration" TEXT NOT NULL,
            "TargetFramework" TEXT NOT NULL,
            "RuntimeIdentifier" TEXT NOT NULL,
            "ExecutablePath" TEXT NOT NULL,
            "Arguments" TEXT NOT NULL,
            "WorkingDirectory" TEXT NOT NULL,
            "ExitCode" INTEGER NULL,
            "BuildSucceeded" INTEGER NOT NULL,
            "TestsExecuted" INTEGER NOT NULL,
            "TestsSucceeded" INTEGER NOT NULL,
            "SourceChangedDuringVerification" INTEGER NOT NULL,
            "CouncilReviewSucceeded" INTEGER NOT NULL,
            "UserApprovedReadyForTest" INTEGER NOT NULL,
            "OutputLogPath" TEXT NOT NULL,
            "EvidenceManifestPath" TEXT NOT NULL,
            "OutputHash" TEXT NOT NULL,
            "SourceSnapshotHash" TEXT NOT NULL,
            "SnapshotArchivePath" TEXT NOT NULL,
            "CouncilReviewSummary" TEXT NOT NULL,
            "Summary" TEXT NOT NULL,
            CONSTRAINT "FK_ProjectBuildVerifications_LocalGptProjects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "LocalGptProjects" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_ProjectBuildVerifications_LocalGptProjectRevisions_RevisionId" FOREIGN KEY ("RevisionId") REFERENCES "LocalGptProjectRevisions" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_ProjectBuildVerifications_ProjectCompilerInstallations_CompilerInstallationId" FOREIGN KEY ("CompilerInstallationId") REFERENCES "ProjectCompilerInstallations" ("Id") ON DELETE RESTRICT
        );
        CREATE INDEX IF NOT EXISTS "IX_ProjectBuildVerifications_ProjectId_RevisionId_CompletedAtUtc" ON "ProjectBuildVerifications" ("ProjectId", "RevisionId", "CompletedAtUtc");
        CREATE INDEX IF NOT EXISTS "IX_ProjectBuildVerifications_CompilerInstallationId" ON "ProjectBuildVerifications" ("CompilerInstallationId");
        """);
    }

    /// <summary>
    /// Runs the down operation.
    /// </summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProjectBuildVerifications");
        migrationBuilder.DropTable(name: "LocalGptProjectTrackedFiles");
        migrationBuilder.DropTable(name: "ProjectCompilerInstallations");
        migrationBuilder.DropTable(name: "ProjectWorkspaceRoots");
    }
}
