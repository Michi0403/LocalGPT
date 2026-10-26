import hashlib
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]


class LocalGptBuildRepairs654(unittest.TestCase):
    def test_upload_council_dispatch_preserves_submitted_boolean(self):
        source = (ROOT / 'src/LocalGPT/Components/Pages/Chat.UploadAdvisor.razor.cs').read_text(encoding='utf-8-sig')
        self.assertIn('bool submitted = false;', source)
        self.assertIn('Func<Task> submitCouncil = async () =>', source)
        self.assertIn('await InvokeAsync(submitCouncil).ConfigureAwait(false);', source)
        self.assertIn('submitted = await StartCouncilPromptAsync(starter, startFresh: false).ConfigureAwait(true);', source)
        self.assertIn('if (!submitted)', source)
        self.assertNotIn('var submitted = await InvokeAsync(() => StartCouncilPromptAsync', source)

    def test_all_javascript_diagnostics_manifest_hashes(self):
        manifest = (ROOT / 'build/javascript-diagnostics-files.sha256').read_text(encoding='utf-8')
        reviewed = {}
        for line in manifest.splitlines():
            if not line.strip() or line.startswith('#'):
                continue
            sha256, name = line.split('  ', 1)
            self.assertNotIn(name, reviewed)
            reviewed[name] = sha256
            normalized = (ROOT / name).read_text(encoding='utf-8').replace('\r\n', '\n').replace('\r', '\n')
            self.assertEqual(hashlib.sha256(normalized.encode('utf-8')).hexdigest(), sha256, name)
        self.assertIn('src/LocalGPT/wwwroot/js/javascript-diagnostics.js', reviewed)
        self.assertIn('src/LocalGPT/wwwroot/js/localgpt-reconnect.js', reviewed)

    def test_project_version(self):
        self.assertIn('<Version>6.5.4</Version>', (ROOT / 'src/LocalGPT/LocalGPT.csproj').read_text(encoding='utf-8'))


if __name__ == '__main__':
    unittest.main()
