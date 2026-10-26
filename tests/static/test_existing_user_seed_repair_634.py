"""Offline, source-level acceptance for existing-user default reconciliation (no .NET build)."""
from pathlib import Path
import json
import re
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
DB = (ROOT/'src/LocalGPT/Services/Persistence/DatabaseInitializationService.cs').read_text()
PRESETS = (ROOT/'src/LocalGPT/Services/Persistence/DatabaseInitializationService.SeedPresets.cs').read_text()
TEAM = (ROOT/'src/LocalGPT/Services/CouncilTeamConfigurationService.Seeding.cs').read_text()
RUNTIME = (ROOT/'src/LocalGPT/Services/CouncilRuntimeClassService.cs').read_text()
TEAM_INTERFACE = (ROOT/'src/LocalGPT/Interfaces/ICouncilTeamConfigurationService.cs').read_text()
RUNTIME_INTERFACE = (ROOT/'src/LocalGPT/Interfaces/ICouncilRuntimeClassService.cs').read_text()

class ExistingUserSeedRepair634Tests(unittest.TestCase):
    def test_upgrade_runs_core_then_team_and_game_reconciliation_without_ui_visits(self):
        init = DB.index('await initializer.InitializeAsync(stoppingToken)')
        team = DB.index('await teams.ReconcileDefaultsAsync(stoppingToken)')
        runtime = DB.index('await runtimeClasses.ReconcileDefaultsAsync(stoppingToken)')
        audit = DB.index('await AuditRequiredDefaultsAsync(teams, runtimeClasses, stoppingToken)')
        self.assertLess(init, team)
        self.assertLess(team, runtime)
        self.assertLess(runtime, audit)
        self.assertIn('CreateAsyncScope()', DB)
        self.assertIn('AddHostedService<DatabaseInitializationHostedService>()', (ROOT/'src/LocalGPT/Program.ServiceRegistration.cs').read_text())

    def test_all_default_categories_are_audited(self):
        for name in ('Knowledge (C#, DevExpress, Ollama', 'Regex patterns', 'Prompts',
                     'System variables', 'Council team templates', 'Council game/runtime classes',
                     'Council model presets', 'Core project templates'):
            self.assertIn(name, DB)
        self.assertIn('expectedKnowledge.Count(knowledge.Contains)', DB)
        self.assertIn('expectedRuntime.Count(runtimeKeys.Contains)', DB)
        self.assertIn('expectedCoreProjects.Count(projectIds.Contains)', DB)
        self.assertIn('Where(item => item.Present < item.Required)', DB)

    def test_host_reports_failed_state_and_never_approves_missing_seeds(self):
        self.assertIn('WriteStatusReportAsync("Complete"', DB)
        self.assertIn('WriteStatusReportAsync("Failed"', DB)
        self.assertIn('logger.LogCritical(exception', DB)
        self.assertIn('throw;', DB)
        self.assertIn('seed-reconciliation.json', DB)
        self.assertIn('LocalGptApplicationPathService', (ROOT/'src/LocalGPT/Program.ServiceRegistration.cs').read_text())

    def test_council_reconcile_service_contracts_and_durable_completeness_checks(self):
        self.assertIn('Task ReconcileDefaultsAsync(',TEAM_INTERFACE)
        self.assertIn('Task ReconcileDefaultsAsync(',RUNTIME_INTERFACE)
        self.assertIn('GetSuppliedSeedKeys()',RUNTIME_INTERFACE)
        for src in (TEAM,RUNTIME):
            self.assertIn('requiredKeys.IsSubsetOf(persistedKeys)', src)
            self.assertIn('ToListAsync(cancellationToken)', src)
            self.assertIn('requiredKeys.Count == 0', src)
            self.assertIn('SaveChangesAsync(cancellationToken)', src)
        self.assertIn('if (row.IsDeleted)',TEAM)
        self.assertIn('if (row.IsSystemSeed && row.IsUserModified)',TEAM)
        self.assertIn('if (row.IsSystemSeed && !row.IsUserModified',RUNTIME)

    def test_default_model_presets_repaired_and_verification_matches_builder(self):
        # Shared builder must be used by both seed and post-commit verification.
        self.assertGreaterEqual(DB.count('CreateDefaultCouncilModelPresets()'),1)
        self.assertIn('var presets = CreateDefaultCouncilModelPresets()',PRESETS)
        self.assertIn('CreateDefaultCouncilModelPresets()',PRESETS)
        builder_names = set(re.findall(r'BuildPreset\(\s*"([^"]+)"', PRESETS))
        audit_block = DB.split('var expectedPresets = new[]',1)[1].split('}.ToHashSet',1)[0]
        audited_names = set(re.findall(r'"([^"]+)"', audit_block))
        self.assertEqual(builder_names,audited_names)
        self.assertEqual(len(builder_names),6)

    def test_knowledge_seed_is_release_owned_not_imported_from_tester_db(self):
        manifest = ROOT/'src/LocalGPT/Resources/Knowledge/core-defaults.json'
        data = json.loads(manifest.read_text())
        self.assertEqual(len(data),42)
        project = ET.parse(ROOT/'src/LocalGPT/LocalGPT.csproj')
        resources=project.findall('.//EmbeddedResource')
        names=[x.attrib['Include'].replace('\\','/').casefold() for x in resources]
        self.assertEqual(len(names),len(set(names)))
        assert any('core-defaults.json' in n for n in names)
        self.assertIn('await RunSeedStageAsync("knowledge entries", SeedKnowledgeAsync',DB)

    def test_expected_versions_and_changelog(self):
        self.assertIn('<Version>6.5.2</Version>',(ROOT/'src/LocalGPT/LocalGPT.csproj').read_text())
        self.assertTrue((ROOT/'CHANGELOG-6.3.4-EXISTING-USER-DEFAULT-RECONCILIATION.md').exists())
        self.assertTrue((ROOT/'docs/DEFAULT-SEED-UPGRADE-6.3.4.md').exists())

if __name__ == '__main__': unittest.main()
