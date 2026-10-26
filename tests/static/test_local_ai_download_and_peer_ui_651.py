"""Release contract for stream lifetime, peer rendering and filtered model inventory."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

class LocalAiDownloadAndPeerUiTests(unittest.TestCase):
    def test_archive_published_only_after_writer_disposed(self):
        source = (ROOT / 'src/LocalGPT/Services/LocalAiAcquisitionService.cs').read_text()
        start = source.index('await using (var output = new FileStream(')
        publish = source.index('File.Move(temporaryPath, archivePath)', start)
        self.assertIn('await output.FlushAsync(', source[start:publish])
        self.assertIn('sha256-', source[start:publish])
        self.assertIn('if (File.Exists(archivePath))', source[start:publish])
        self.assertLess(source.index('await output.FlushAsync(', start), publish)
        self.assertLess(source.index('var digest =', start), publish)
        self.assertIn('finally', source[publish:publish+1100])

    def test_razor_deduplicates_peer_keys(self):
        source = (ROOT / 'src/LocalGPT/Components/Pages/OneWireSecurity.razor').read_text()
        self.assertIn('GroupBy(item => item.PeerId, StringComparer.OrdinalIgnoreCase)', source)
        self.assertIn('GroupBy(peer => peer.PeerId, StringComparer.OrdinalIgnoreCase)', source)
        self.assertIn('@key="peer.PeerId"', source)
        # Distinct lists share a render sibling scope; per-list grouping alone is insufficient.
        self.assertIn('@key="(peer.PeerId, true)"', source)
        self.assertIn('@key="(peer.PeerId, false)"', source)

    def test_model_grid_and_open_probes_are_guarded(self):
        view = (ROOT / 'src/LocalGPT/Components/Pages/Install.razor').read_text()
        code = (ROOT / 'src/LocalGPT/Components/Pages/Install.razor.cs').read_text()
        self.assertIn('ShowSearchBox="true"', view)
        self.assertIn('ShowFilterRow="true"', view)
        self.assertIn('InstallHuggingFaceModelAsync(model)', view)
        self.assertIn('ProbeLocalAiRuntimeOnOpen', code)
        self.assertIn('RefreshToolchainsOnOpen', code)

    def test_gate_is_build_blocking(self):
        targets = (ROOT / 'Directory.Build.targets').read_text()
        self.assertIn('AssertLocalAiDownloadAndPeerUI', targets)
        self.assertIn('Assert-LocalAiDownloadAndPeerUI.ps1', targets)

if __name__ == '__main__': unittest.main()
