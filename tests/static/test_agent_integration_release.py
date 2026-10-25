"""Source-level integration checks; not a substitute for mandatory PowerShell/.NET gates."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SERVICES = ROOT / 'src' / 'LocalGPT' / 'Services'


class AgentIntegrationReleaseTests(unittest.TestCase):
    def test_version(self):
        self.assertIn('<Version>6.4.1</Version>', (ROOT / 'src/LocalGPT/LocalGPT.csproj').read_text())

    def test_council_team_functions_include_approval_gate(self):
        s = (SERVICES / 'CouncilTeamDxAiFunctions.cs').read_text()
        for function in ('council.teams.list', 'council.teams.inspect',
                         'council.teams.validate', 'council.teams.match', 'council.teams.save'):
            self.assertIn('"' + function + '"', s)
        save = s[s.index('public sealed class CouncilTeamSaveFunction'):]
        self.assertIn('RequiresHumanConfirmation: true', save)
        self.assertIn('SupportsDeferredApprovalRequest: true', save)
        self.assertIn('if (!request.UserConfirmed)', save)
        self.assertIn('teams.SaveAsync(', save)

    def test_model_install_probe_precedes_manifest(self):
        s = (SERVICES / 'LocalAiRuntimeService.cs').read_text()
        segment = s[s.index('public async Task<LocalAiModelInstallation> InstallModelAsync('):]
        self.assertLess(segment.index('foreach (var capability in capabilities)'),
                        segment.index('SaveManifest(modelRoot, installation)'))
        self.assertIn('"model_load_probe"', segment)
        self.assertIn('TryDeleteDirectory(localPath)', segment)
        bridge = (ROOT / 'src/LocalGPT/Runtime/Python/localgpt_runtime_bridge.py').read_text()
        self.assertIn('"model_load_probe": _model_load_probe', bridge)

    def test_canirun_does_not_invent_install_identifiers(self):
        s = (SERVICES / 'CanIRunInstallPlanDxAiFunctions.cs').read_text()
        self.assertIn('providers.GetProfilesAsync(', s)
        self.assertIn('providers.IsOllamaProfile', s)
        self.assertIn('recommendation.OllamaModelId', s)
        self.assertIn('HasVerifiedRuntimeCompatibility = false', s)

    def test_new_function_schemas_are_valid_json(self):
        for name in ('CouncilTeamDxAiFunctions.cs', 'CanIRunInstallPlanDxAiFunctions.cs'):
            s = (SERVICES / name).read_text()
            schemas = re.findall(r'ParameterSchemaJson: """(.+?)"""', s, re.S)
            self.assertTrue(schemas, name)
            for schema in schemas:
                self.assertEqual('object', json.loads(schema)['type'])


if __name__ == '__main__':
    unittest.main()
