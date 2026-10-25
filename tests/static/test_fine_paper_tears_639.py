"""The alpha mask and paper edges share fine, deterministic claw geometry."""
from pathlib import Path
import unittest
ROOT = Path(__file__).resolve().parents[2]
class FinePaperTear639Tests(unittest.TestCase):
    def test_fine_geometry_preserves_workflow(self):
        js = (ROOT/'docs/templates/localgpt/public/main.js').read_text()
        css = (ROOT/'docs/templates/localgpt/public/main.css').read_text()
        self.assertIn('const KAWAII_CLAW_SPACING = 16;', js)
        self.assertIn('kawaiiTearGeometry(scratch, claw)',js)
        self.assertIn('const shapes = geometries.map(kawaiiTearOutline);',js)
        self.assertIn('geometries.map(kawaiiTearPaperLips)',js)
        self.assertIn('geometries.map(kawaiiTearFlaps)',js)
        self.assertIn('session.target.style.maskImage = url;',js)
        self.assertIn('event.shiftKey', js)
        self.assertIn('playKawaiiScratchSound', js)
        self.assertIn("addEventListener('hashchange', clearKawaiiTearSession)", js)
        self.assertNotIn('stroke-width: 9;',css)
        self.assertIn('stroke-width: 2.4;', css)
        self.assertIn('.localgpt-cat-scratch',css)
        self.assertIn('.localgpt-cursor-paw',css)
