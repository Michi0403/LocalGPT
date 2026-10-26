"""Regression contracts for Mermaid label bounds and mobile cat scratches."""
from pathlib import Path
import unittest
R=Path(__file__).resolve().parents[2]
JS=(R/'docs/templates/localgpt/public/main.js').read_text()
CSS=(R/'docs/templates/localgpt/public/main.css').read_text()
class ResponsiveMermaidTouchTests(unittest.TestCase):
    def test_desktop_svg_node_bounds_and_fallback(self):
        for token in ('function clippedMermaidNodeLabels(block)',
                      'getBoundingClientRect()',
                      "const renderedBlocks = [...document.querySelectorAll('.mermaid')]",
                      "window.matchMedia('(min-width: 768px)').matches",
                      'htmlLabels: true, useMaxWidth: false',
                      'fitAlreadyRenderedMermaidLabels(block)',
                      "block.setAttribute('data-processed', 'true');"):
            self.assertIn(token, JS)
        self.assertIn("const source = block.getAttribute('data-localgpt-mermaid-source')",JS)
    def test_mobile_touch_mode_not_forced_scroll_regression(self):
        for token in ('installKawaiiTouchScratchToggle()',
                      "event.pointerType === 'touch'",
                      'moveKawaiiTouchPaw(event.clientX, event.clientY)',
                      "if (event.pointerType !== 'touch' && (event.buttons & 1) === 0) return;",
                      "window.addEventListener('hashchange', clearKawaiiTearSession)",
                      'session.target.style.maskImage = url;',
                      'session.target.style.maskImage = session.initialMask;'):
            self.assertIn(token,JS)
        self.assertIn('@media (any-pointer: coarse)',CSS)
        self.assertIn('touch-action: none;',CSS)
        self.assertIn('aria-pressed',JS)
        self.assertNotIn('html.localgpt-kawaii-docs .localgpt-tear-overlay { display: none !important; }',CSS)
if __name__ == '__main__': unittest.main()
