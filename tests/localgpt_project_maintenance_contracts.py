import json
import re
import unittest
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT"


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


class ProjectMaintenanceContracts(unittest.TestCase):
    def test_localization_catalogs_are_case_insensitively_unique_and_equal(self):
        catalogs = []
        for name in ("en-US.json", "de-DE.json"):
            path = APP / "Localization" / name
            pairs = json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=list)
            folded = defaultdict(list)
            for key, _ in pairs:
                folded[key.casefold()].append(key)
            duplicates = [values for values in folded.values() if len(values) > 1]
            self.assertEqual([], duplicates, f"case-insensitive localization duplicates in {name}: {duplicates}")
            catalogs.append(dict(pairs))
        self.assertEqual(set(catalogs[0]), set(catalogs[1]))
        self.assertGreaterEqual(len(catalogs[0]), 1200)
        guard = read("build/Assert-LocalizationIntegrity.ps1")
        self.assertIn("case-insensitive duplicate keys", guard)
        self.assertLess(guard.index("case-insensitive duplicate keys"), guard.index("try { $catalog = ConvertFrom-Json -InputObject $raw }"))

    def test_database_stores_revision_aware_file_paths_and_regex_metadata(self):
        models = read("LocalGPTWebviewWrapper/LocalGPT/BusinessObjects/ProjectMaintenanceModels.cs")
        context = read("LocalGPTWebviewWrapper/LocalGPT/BusinessObjects/EFCore/LocalGptMemoryDbContext.cs")
        migration = read("LocalGPTWebviewWrapper/LocalGPT/Migrations/20260727193000_FixProjectTrackedFileRevisionIdentity.cs")
        snapshot = read("LocalGPTWebviewWrapper/LocalGPT/Migrations/LocalGptMemoryDbContextModelSnapshot.cs")
        for token in (
            "StableFileKey", "AbsolutePath", "ProjectRelativePath", "WorkspaceRelativePath",
            "SolutionPath", "ProjectFilePath", "StructureRegex", "ContentFormatRegex", "ContentHash"
        ):
            self.assertIn(token, models)
        self.assertRegex(context, r"HasIndex\(item => new \{ item\.ProjectId, item\.RevisionId, item\.ProjectRelativePath \}\)\.IsUnique")
        self.assertIn("IX_LocalGptProjectTrackedFiles_ProjectId_RevisionId_ProjectRelativePath", migration)
        self.assertRegex(snapshot, r'HasIndex\("ProjectId", "RevisionId", "ProjectRelativePath"\)\s*\.IsUnique\(\)')

    def test_workspace_scope_and_cross_platform_toolchain_inventory_are_present(self):
        service = read("LocalGPTWebviewWrapper/LocalGPT/Services/ProjectMaintenanceService.cs")
        page = read("LocalGPTWebviewWrapper/LocalGPT/Components/Pages/ProjectMaintenance.razor")
        for token in ('ScopeKind == "Project"', 'ScopeKind == "ProjectType"', 'ScopeKind == "Global"', "ProjectTypePattern"):
            self.assertIn(token, service)
        for token in ("dotnet.exe", "javac.exe", "python.exe", "pwsh.exe", "powershell.exe", "cl.exe", "g++.exe", "clang++.exe"):
            self.assertIn(token, service)
        for token in ("/usr/share/dotnet", 'Path.Combine(home, ".dotnet")', "/usr/lib/jvm", "EnvironmentVariablesJson"):
            self.assertIn(token, service)
        for token in ("Additional compiler search roots", "Compiler environment JSON", "Revision source root", "Solution regex"):
            self.assertIn(token, page)

    def test_exact_source_state_is_required_before_and_after_build_and_snapshot(self):
        service = read("LocalGPTWebviewWrapper/LocalGPT/Services/ProjectMaintenanceService.cs")
        codegen = read("LocalGPTWebviewWrapper/LocalGPT/Services/CodeGenerationWorkflowService.cs")
        for token in (
            "CaptureTrackedSourceStateAsync", "requireStoredHashMatch: true", "SourceHashBefore",
            "SourceHashAfter", "SourceChangedDuringVerification", "verification.SourceSnapshotHash",
            "CreateEntryFromFile", ".localgpt-manifest.json"
        ):
            self.assertIn(token, service)
        for token in (
            "CopyTrackedProjectIntoWorkspaceAsync", "ComputeFileHashAsync", "file.ContentHash",
            "did not preserve the approved file bytes", "RegisterRevisionWorkspaceAsync", "ScanProjectFilesAsync"
        ):
            self.assertIn(token, codegen)
        self.assertRegex(service, r"item\.RevisionId == request\.RevisionId")
        self.assertIn('request.RevisionId?.ToString("N") ?? "base"', service)

    def test_council_and_controller_expose_only_approved_revision_workflow(self):
        functions = read("LocalGPTWebviewWrapper/LocalGPT/Services/ProjectMaintenanceDxAiFunctions.cs")
        controller = read("LocalGPTWebviewWrapper/LocalGPT/Controller/ProjectMaintenanceController.cs")
        expected_functions = (
            "project.maintenance.get", "project.revision.workspace.register", "project.files.scan",
            "project.file.patterns.save", "project.revision.build.verify",
            "project.revision.council-review", "project.revision.ready.approve"
        )
        for name in expected_functions:
            self.assertIn(name, functions)
        self.assertIn("RequiresHumanConfirmation: true", functions)
        self.assertIn("ApprovalRequiredBeforeCompletion: true", functions)
        for route in (
            "revisions/{revisionId:guid}/workspace", "projects/{projectId:guid}/scan",
            "files/{trackedFileId:guid}/patterns", "projects/{projectId:guid}/verify",
            "council-review", "approve-ready"
        ):
            self.assertIn(route, controller)
        self.assertGreaterEqual(controller.count("HumanApprovalRequired"), 8)

    def test_snapshot_guard_is_multiline_safe_and_startup_migrations_remain_automatic(self):
        guard = read("build/Assert-ProjectMaintenanceArchitecture.ps1")
        snapshot = read("LocalGPTWebviewWrapper/LocalGPT/Migrations/LocalGptMemoryDbContextModelSnapshot.cs")
        initialization = read("LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/DatabaseInitializationService.cs")
        program = read("LocalGPTWebviewWrapper/LocalGPT/Program.cs")

        self.assertIn("$snapshotWithoutWhitespace = [regex]::Replace($content.Snapshot, '\\s+', '')", guard)
        compact_snapshot = re.sub(r"\s+", "", snapshot)
        self.assertIn(
            'HasIndex("ProjectId","RevisionId","ProjectRelativePath").IsUnique()',
            compact_snapshot,
        )
        self.assertIn("await db.Database.MigrateAsync(cancellationToken)", initialization)
        self.assertIn("AddHostedService<DatabaseInitializationHostedService>()", program)

    def test_build_scripts_run_all_five_guards(self):
        for script_name in ("Build-LocalDevelopment.ps1", "Build-Release.ps1"):
            script = read(script_name)
            for guard in (
                "Assert-LoggingIntegrity.ps1", "Assert-OneWireArchitecture.ps1",
                "Assert-LocalizationIntegrity.ps1", "Assert-GitSourceVisibility.ps1", "Assert-ProjectMaintenanceArchitecture.ps1"
            ):
                self.assertIn(guard, script, f"{guard} missing from {script_name}")
        targets = read("Directory.Build.targets")
        self.assertIn("AssertLocalGptProjectMaintenanceArchitecture", targets)
        self.assertIn("AssertLocalGptGitSourceVisibility", targets)


if __name__ == "__main__":
    unittest.main()
