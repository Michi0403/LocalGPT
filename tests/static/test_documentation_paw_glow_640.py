"""Source-only checks for the 6.4.2 theme-aware documentation paw enhancement."""
from pathlib import Path
import unittest
ROOT = Path(__file__).resolve().parents[2]
PUBLIC = ROOT / 'docs/templates/localgpt/public'

class DocumentationPawGlow640Tests(unittest.TestCase):
    def test_maintained_version_and_isolated_paw(self):
        self.assertIn('<Version>6.5.4</Version>', (ROOT/'src/LocalGPT/LocalGPT.csproj').read_text())
        js = (PUBLIC/'main.js').read_text()
        self.assertIn('function makeKawaiiGalaxyPaw()', js)
        self.assertIn('class="localgpt-paw-fur"', js)
        self.assertIn('class="localgpt-paw-fur-detail"', js)
        self.assertIn('url(#localgpt-paw-gradient)', js)
        self.assertIn('session.target.style.maskImage = url;', js)
        self.assertIn('KAWAII_CLAW_SPACING = 16', js)
        self.assertIn('playKawaiiScratchSound', js)

    def test_theme_specific_rim_and_shadow_without_layout_changes(self):
        css = (PUBLIC/'main.css').read_text()
        section = css[css.index('/* Theme-aware rim lighting'):]
        self.assertIn('drop-shadow(0 0 3px rgba(75, 225, 255, .90))', section)
        self.assertIn(':not([data-bs-theme="dark"]) .localgpt-cursor-paw', section)
        self.assertIn('drop-shadow(0 0 5px rgba(59, 44, 82, .82))', section)
        self.assertIn('transform: translate(-.35rem, -.55rem)', section)
        self.assertIn('pointer-events: none;', css)
        self.assertIn('url(#localgpt-tear-space-light)', css)
        self.assertIn('main.container-xxl > .content', (PUBLIC/'main.js').read_text())

if __name__ == '__main__':
    unittest.main()
