"""Static source contract for transient DocFX paper tear and the original paw UX."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
PUBLIC = ROOT / 'docs/templates/localgpt/public'

class KawaiiPaperTearSourceTests(unittest.TestCase):
    def test_docfx_pipeline_is_unchanged(self):
        script = (ROOT / 'build/Build-Documentation.ps1').read_text()
        self.assertIn('localgpt-kawaii.js', script)
        self.assertIn('localgpt-kawaii.css', script)

    def test_ephemeral_damage_masks_only_docs_content(self):
        text = (PUBLIC / 'main.js').read_text()
        self.assertIn("document.querySelector('main.container-xxl > .content')", text)
        self.assertIn('session.target.style.maskImage = url;', text)
        self.assertIn("session.target.style.webkitMaskImage = url;", text)
        self.assertIn("session.target.style.maskImage = session.initialMask;", text)
        self.assertIn('addEventListener(\'pagehide\', clearKawaiiTearSession)', text)
        self.assertIn('addEventListener(\'hashchange\', clearKawaiiTearSession)', text)
        self.assertIn("event.shiftKey", text)
        self.assertIn('addKawaiiClick', text)
        self.assertIn('installKawaiiTearInteraction();', text)
        self.assertIn('makeKawaiiGalaxyPaw()', text)
        self.assertIn('playKawaiiScratchSound', text)
        self.assertNotIn('localStorage.setItem(\'scratch', text)

    def test_theme_support_preserves_scratch_overlay(self):
        css = (PUBLIC / 'main.css').read_text()
        self.assertIn('.localgpt-cat-scratch', css)
        self.assertIn('.localgpt-tear-void', css)
        self.assertIn('.localgpt-tear-paper', css)
        self.assertIn(':not([data-bs-theme="dark"]) .localgpt-tear-paper', css)
        self.assertIn('pointer-events: none;', css)

if __name__ == '__main__':
    unittest.main()
