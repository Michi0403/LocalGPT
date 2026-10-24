"""Static contract tests for 6.3.2 default seeding. No .NET runtime required."""
import json
import pathlib
import re
import unittest
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[2]
SERVICES = ROOT / "src/LocalGPT/Services/Persistence"


class DefaultSeedIntegrityTests(unittest.TestCase):
    def test_18_required_seed_documents_are_embedded(self):
        policy = (SERVICES / "LocalGptRuntimePolicySeedDataService.cs").read_text()
        m = re.search(r"new\(LocalGptRuntimeCollection.KnowledgeFiles.*?\[(.*?)\]\)", policy, re.S)
        self.assertIsNotNone(m)
        required = re.findall(r'"([^"]+)"', m.group(1))
        self.assertEqual(len(required), 18)
        resources = ET.parse(ROOT / "src/LocalGPT/LocalGPT.csproj").getroot().findall('.//EmbeddedResource')
        names = {entry.attrib.get('LogicalName') for entry in resources}
        for path in required:
            self.assertTrue((ROOT / path).is_file(), path)
            self.assertIn('LocalGPT.SeedDocument.' + path.replace('/', '.'), names)
        self.assertIn('LocalGPT.Knowledge.core-defaults.json', names)

    def test_core_manifest_is_curated_nonpersonal_and_unique(self):
        rows = json.loads((ROOT / 'src/LocalGPT/Resources/Knowledge/core-defaults.json').read_text())
        self.assertEqual(len(rows), 42)
        self.assertEqual(len({row['Id'].lower() for row in rows}), 42)
        for row in rows:
            self.assertTrue(row['Content'].strip())
            self.assertTrue(row['Topic'].strip())
            self.assertEqual(set(row), {'Id','Topic','Scope','Content','HelpfulSources','Tags','Confidence','VerificationStatus','ReviewStatus'})
            self.assertNotRegex(row['Content'], r'C:\\Users|JAYJAY|Michi0403|@gmail\.com')

    def test_seed_missing_is_fatal_and_durable_records_are_verified(self):
        loader = (SERVICES / 'InitialDataFeedService.cs').read_text()
        seeder = (SERVICES / 'DatabaseInitializationService.cs').read_text()
        self.assertIn('Required knowledge seed document', loader)
        self.assertIn('entries.Count != configuredFiles.Count', loader)
        self.assertIn('await VerifySeedStageAsync(db, stageName, cancellationToken)', seeder)
        self.assertIn('Mandatory seed stage', seeder)
        for seed_type in ['regex definitions','prompt definitions','system variables','knowledge entries']:
            self.assertIn('stageName == "'+seed_type+'"',seeder)

    def test_does_not_overwrite_user_edited_knowledge(self):
        s = (SERVICES / 'DatabaseInitializationService.SeedKnowledgeAndProjects.cs').read_text()
        self.assertIn('persistedContentHash', s)
        self.assertIn('if (!seedOwned || !string.Equals', s)
        self.assertIn('builtin:core-knowledge:', s)

    def test_generic_model_fallbacks(self):
        s=(ROOT / 'src/LocalGPT/Services/AiProviderBootstrapService.cs').read_text()
        for name in ['deepscaler:1.5b','deepseek-ocr:3b','dolphin3:8b','openthinker:7b','openthinker:32b','gemma3:27b','gemma2:27b']:
            self.assertIn('"'+name+'"', s)


if __name__ == '__main__':
    unittest.main()
