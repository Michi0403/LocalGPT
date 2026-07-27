import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MARKER = "\u2420"


class LocalizationEncodingAndGitVisibilityContracts(unittest.TestCase):
    def test_catalogs_and_required_german_key_are_intact(self):
        base = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT" / "Localization"
        english = json.loads((base / "en-US.json").read_text(encoding="utf-8"))
        german = json.loads((base / "de-DE.json").read_text(encoding="utf-8"))
        self.assertEqual(set(english), set(german))
        self.assertEqual("Neuen Chat starten", german[f"Text.Start{MARKER}new{MARKER}chat"])

    def test_powershell_gates_are_windows_powershell_51_safe(self):
        for relative in ("build/Assert-LocalizationIntegrity.ps1", "build/Assert-GitSourceVisibility.ps1"):
            data = (ROOT / relative).read_bytes()
            self.assertTrue(all(byte < 128 for byte in data), f"{relative} must remain ASCII-only")
        guard = (ROOT / "build/Assert-LocalizationIntegrity.ps1").read_text(encoding="ascii")
        self.assertIn("[char]0x2420", guard)
        self.assertIn("System.Text.UTF8Encoding", guard)
        self.assertIn("ReadAllText", guard)
        self.assertNotIn(MARKER, guard)
        self.assertNotIn("â", guard)

    def test_scratch_clone_rule_is_root_anchored_and_cannot_hide_the_product_tree(self):
        ignore = (ROOT / ".gitignore").read_text(encoding="utf-8")
        self.assertIn("/localgpt/", ignore)
        self.assertNotIn("\nlocalgpt/", ignore)
        self.assertIn("!LocalGPTWebviewWrapper/LocalGPT/Localization/", ignore)

    def test_build_and_git_visibility_wiring_is_present(self):
        target = (ROOT / "Directory.Build.targets").read_text(encoding="utf-8")
        self.assertIn("AssertLocalGptGitSourceVisibility", target)
        self.assertIn("SkipGitSourceVisibilityGuard", target)
        for relative in ("Build-LocalDevelopment.ps1", "Build-Release.ps1"):
            content = (ROOT / relative).read_text(encoding="utf-8")
            self.assertIn("Assert-GitSourceVisibility.ps1", content)
            self.assertIn("SkipGitSourceVisibilityGuard=true", content)
        ignore = (ROOT / ".gitignore").read_text(encoding="utf-8")
        for rule in ("!build/*.ps1", "!tests/*.py", "!LocalGPTWebviewWrapper/LocalGPT/Localization/*.json"):
            self.assertIn(rule, ignore)


if __name__ == "__main__":
    unittest.main()
