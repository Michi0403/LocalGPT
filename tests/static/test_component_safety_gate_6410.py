"""Regression checks for the existing build-blocking Razor safety guard."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]

class HardwareSafetyContract(unittest.TestCase):
    def test_component_preserves_all_three_top_level_safety_services(self):
        lines = (ROOT / 'src/LocalGPT/Components/Shared/InitialSetupHardwareForm.razor').read_text(encoding='utf8').splitlines()
        start = lines.index('<div class="razor-component-boundary">')
        for directive in (
            '@inject ILogger<InitialSetupHardwareForm> Logger',
            '@inject INotificationService Notifier',
            '@inject IComponentActivityService ComponentActivity',
        ):
            self.assertEqual(lines.count(directive), 1, directive)
            self.assertLess(lines.index(directive), start, directive)
        self.assertIn('OnDetectHardware.InvokeAsync()', '\n'.join(lines))
        self.assertIn('OnSaveHardware.InvokeAsync()', '\n'.join(lines))

    def test_build_gate_remains_enforced(self):
        targets = (ROOT / 'Directory.Build.targets').read_text(encoding='utf8')
        gate = (ROOT / 'build/Assert-ComponentSafety.ps1').read_text(encoding='utf8')
        self.assertIn('Assert-ComponentSafety.ps1', targets)
        for symbol in ('INotificationService Notifier','IComponentActivityService ComponentActivity', 'RAZORLOG0001'):
            self.assertIn(symbol, gate)
        self.assertIn('<Version>6.5.2</Version>', (ROOT/'src/LocalGPT/LocalGPT.csproj').read_text(encoding='utf8'))

if __name__=='__main__': unittest.main()
