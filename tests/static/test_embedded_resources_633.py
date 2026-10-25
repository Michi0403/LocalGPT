"""Guard against duplicate EmbeddedResource includes causing NETSDK1022. Offline XML/source check."""
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / "src/LocalGPT/LocalGPT.csproj"
SOURCE = ROOT / "src/LocalGPT/Services/Persistence/InitialDataFeedService.cs"

class EmbeddedResources633Tests(unittest.TestCase):
    def test_unique_physical_resource_path(self):
        resources = ET.parse(PROJECT).getroot().findall('.//EmbeddedResource')
        physical = [x.attrib['Include'].replace('\\','/').casefold() for x in resources]
        self.assertEqual(len(physical), len(set(physical)),
                         "Duplicate EmbeddedResource paths cause NETSDK1022 even with different LogicalNames")

    def test_required_seed_names_remain_available_and_expected_by_reader(self):
        resources = ET.parse(PROJECT).getroot().findall('.//EmbeddedResource')
        logical_names = {x.attrib.get('LogicalName') for x in resources}
        reader = SOURCE.read_text()
        self.assertIn("\"LocalGPT.SeedDocument.\" + canonicalPath.Replace('/', '.')", reader)
        for doc in ('toolchain-discovery', 'ai-provider-installation', 'ascii-game-authoring'):
            expected = 'LocalGPT.SeedDocument.docs.reference.' + doc + '.md'
            self.assertIn(expected, logical_names)
            self.assertNotIn('LocalGPT.Knowledge.' + doc + '.md', logical_names)
            self.assertTrue((ROOT / ('docs/reference/' + doc + '.md')).is_file())
        self.assertIn('LocalGPT.Knowledge.core-defaults.json', logical_names)

    def test_version(self):
        self.assertIn('<Version>6.3.7</Version>', PROJECT.read_text())

if __name__ == '__main__':
    unittest.main()
