"""Source-level regression for generic, DB-configured Learning Round repair.

These tests deliberately do not mock approval or replace the existing .NET build gates.
"""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
SERVICES = ROOT / "src" / "LocalGPT" / "Services"


class LearningRoundSourceRecoveryTests(unittest.TestCase):
    def test_initial_empty_ledger_permits_first_configured_retry(self):
        source = (SERVICES / "MultiModelCouncilService.ConfiguredWorkflowExecution.cs").read_text()
        self.assertIn("var stalled = false;", source)
        self.assertIn("progress.CompletedFiles <= previousProgress.CompletedFiles", source)
        self.assertIn("GetVerifiedWorkspaceReadProgress", source)
        self.assertIn("if (!goalMet)", source)
        self.assertNotIn("progress.CompletedFiles == 0 && progress.ReviewedCharacters == 0", source)

    def test_recovery_uses_workspaces_and_kind_not_magic_extension_list(self):
        source = (SERVICES / "MultiModelCouncilService.SourceReviewRecovery.cs").read_text()
        self.assertIn('file.IncludedInPrompt || string.Equals(file.Kind, "zip"', source)
        self.assertIn("page?.ArchiveRoots", source)
        self.assertIn("representatives.Concat(rankedPaths)", source)
        self.assertNotIn("IsSourceReviewTextPath", source)
        self.assertIn("ReadFileAsync", source)

    def test_nested_document_pages_are_read_as_semantic_text(self):
        source = (SERVICES / "ChatUploadWorkspaceService.cs").read_text()
        self.assertIn("packages.IsZipContainer(file)", source)
        self.assertIn("packages.ReadSemanticPageAsync(file, effectiveOffset, effectiveMaximum", source)
        self.assertIn("CharactersReturned = page.Text.Length", source)
        self.assertIn("NextOffsetCharacters = page.HasMore ? effectiveOffset + page.Text.Length", source)
        self.assertIn("packages.IsUnsafeEntry(entry.ExternalAttributes)", source)

    def test_verified_read_ledger_rejects_binary_or_zero_character_receipts(self):
        recorder = (SERVICES / "CouncilDxFunctionOrchestrator.cs").read_text()
        verifier = (SERVICES / "MultiModelCouncilService.WorkspaceReadProgress.cs").read_text()
        self.assertIn('read.TryGetProperty("kind", out var readKind)', recorder)
        self.assertIn('kind = readKind.GetString()', recorder)
        self.assertIn('!source.TryGetProperty("kind", out var kind)', verifier)
        self.assertIn('!string.Equals(kind.GetString(), "text", StringComparison.OrdinalIgnoreCase)', verifier)
        self.assertIn('characters <= 0', verifier)
        self.assertIn('truncated.ValueKind == JsonValueKind.True', verifier)
        transport = (SERVICES / "OllamaThinkingChatClient.ToolExecution.cs").read_text()
        self.assertIn('Kind = kind.GetString()', transport)

    def test_db_first_seeds_refresh_without_clobbering_user_team(self):
        seed = (SERVICES / "OrganicCouncilBlueprintSeedDataService.cs").read_text()
        store = (SERVICES / "CouncilTeamConfigurationService.cs").read_text()
        merge = (SERVICES / "CouncilTeamConfigurationService.Seeding.cs").read_text()
        self.assertRegex(seed, r"missingWorkspaceFileReadRecoveryAttempts: 8,[^\n]*includeWorkspaceSourceExcerptsOnRecovery: true")
        self.assertIn("CurrentSeedVersion = 48", store)
        self.assertIn("!row.IsUserModified", merge)
        self.assertIn('Key = "learning-round"', seed)

    def test_missing_named_workspace_never_substitutes_unrelated_latest(self):
        sync = (SERVICES / "LearningProjectWorkspaceSyncService.cs").read_text()
        start = sync.index("if (requested is null)")
        branch = sync[start:sync.index("if (!parameters.IncludeNewerWorkspaces", start)]
        self.assertIn("return [];", branch)
        self.assertNotIn("return [candidates[0]];", branch)

    def test_no_fixed_build_volume_name_or_changed_release_gates(self):
        for path in SERVICES.glob("*.cs"):
            self.assertNotIn("compilestick", path.read_text(errors="replace").lower(), str(path))
        project = (ROOT / "src" / "LocalGPT" / "LocalGPT.csproj").read_text()
        self.assertIn("<Version>6.2.6</Version>", project)


if __name__ == "__main__":
    unittest.main()
