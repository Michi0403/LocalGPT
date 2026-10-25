"""Protect realistic paper-lip geometry without changing the DocFX renderer."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]

class PaperContour638Tests(unittest.TestCase):
    def test_project_version(self):
        self.assertRegex((ROOT/'src/LocalGPT/LocalGPT.csproj').read_text(), r'<Version>6\.4\.1</Version>')

    def test_masks_real_content_but_not_dom(self):
        js = (ROOT/'docs/templates/localgpt/public/main.js').read_text()
        self.assertIn('function kawaiiTearPaperLips(geometry)',js)
        self.assertIn('function kawaiiTearFlaps(geometry)',js)
        self.assertIn('localgpt-tear-fiber',js)
        self.assertIn('localgpt-tear-paper-lip',js)
        self.assertIn('geometries.map(kawaiiTearPaperLips)',js)
        self.assertIn('const shapes = geometries.map(kawaiiTearOutline);',js)
        self.assertIn('session.target.style.maskImage = url;',js)
        self.assertIn('session.target.style.maskImage = session.initialMask;',js)
        self.assertIn('window.addEventListener(\'hashchange\', clearKawaiiTearSession)',js)
        self.assertNotIn('session.target.innerHTML =',js)

    def test_light_mode_and_paw_preserved(self):
        css=(ROOT/'docs/templates/localgpt/public/main.css').read_text()
        for name in ['tear-paper-lip','tear-flap','tear-flap-shadow','tear-fold','tear-fiber','cat-scratch','cursor-paw']:
            self.assertIn('localgpt-'+name,css)
        self.assertIn('not([data-bs-theme="dark"]) .localgpt-tear-paper-lip',css)

if __name__=='__main__':unittest.main()
