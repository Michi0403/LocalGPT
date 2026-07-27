import json
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
APP = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT"
BUILD = ROOT / "build"


class LocalGptRenderAndAsyncContracts(unittest.TestCase):
    def test_interactive_server_guard_is_wired_into_direct_builds(self):
        targets = (ROOT / "Directory.Build.targets").read_text(encoding="utf-8")
        self.assertIn("Assert-InteractiveServerRenderModes.ps1", targets)
        self.assertIn("AssertLocalGptInteractiveServerRenderModes", targets)
        self.assertIn("SkipInteractiveServerRenderModeGuard", targets)
        gate = (BUILD / "Assert-InteractiveServerRenderModes.ps1").read_text(encoding="utf-8")
        self.assertEqual(18, len(re.findall(r"^\s+'Components/[^']+'\s*=", gate, re.MULTILINE)))
        self.assertIn("must not replace the reviewed page/island render modes", gate)

    def test_async_continuation_guard_is_wired_and_baselined(self):
        targets = (ROOT / "Directory.Build.targets").read_text(encoding="utf-8")
        self.assertIn("Assert-AsyncContinuationPolicy.ps1", targets)
        self.assertIn("AssertLocalGptAsyncContinuationPolicy", targets)
        self.assertIn("SkipAsyncContinuationGuard", targets)
        manifest = json.loads((BUILD / "async-continuation-baseline.json").read_text(encoding="utf-8"))
        self.assertEqual(1, manifest["schemaVersion"])
        self.assertIn("Components/Pages/Chat.razor", manifest["files"])
        self.assertIn("Services/MultiModelCouncilService.cs", manifest["files"])

    def test_current_source_does_not_exceed_reviewed_async_exceptions(self):
        manifest = json.loads((BUILD / "async-continuation-baseline.json").read_text(encoding="utf-8"))
        baseline = manifest["files"]
        checked = 0
        for path in APP.rglob("*"):
            if path.suffix not in {".cs", ".razor"} or {"bin", "obj"}.intersection(path.parts):
                continue
            text = path.read_text(encoding="utf-8-sig")
            awaits = len(re.findall(r"\bawait\b", text))
            if not awaits:
                continue
            false_count = len(re.findall(r"\.ConfigureAwait\s*\(\s*false\s*\)", text))
            true_count = len(re.findall(r"\.ConfigureAwait\s*\(\s*true\s*\)", text))
            unconfigured = awaits - false_count - true_count
            relative = path.relative_to(APP).as_posix()
            limits = baseline.get(relative, {"maxUnconfiguredAwaitCount": 0, "maxConfigureAwaitTrueCount": 0})
            self.assertLessEqual(unconfigured, limits["maxUnconfiguredAwaitCount"], relative)
            self.assertLessEqual(true_count, limits["maxConfigureAwaitTrueCount"], relative)
            checked += 1
        self.assertGreater(checked, 100)

    def test_historically_configured_ui_files_keep_their_continuations(self):
        expected_false_counts = {
            "Components/Pages/Chat.razor": 58,
            "Components/Pages/ProjectMaintenance.razor": 43,
            "Components/Pages/Install.razor": 33,
            "Components/Pages/Projects.razor": 29,
            "Components/Pages/Database.razor": 29,
            "Components/Pages/TestLab.razor": 20,
            "Components/Pages/ModelCouncil.razor": 16,
            "Components/Pages/MinecraftModBuilder.razor": 14,
            "Components/Layout/ThemeSwitcherItem.razor": 1,
            "Components/Layout/Drawer.razor": 1,
        }
        for relative, minimum in expected_false_counts.items():
            text = (APP / relative).read_text(encoding="utf-8")
            actual = len(re.findall(r"\.ConfigureAwait\s*\(\s*false\s*\)", text))
            self.assertGreaterEqual(actual, minimum, relative)

    def test_repository_validation_runs_both_guards(self):
        validation = (BUILD / "Invoke-RepositoryValidation.ps1").read_text(encoding="utf-8")
        self.assertIn("Assert-InteractiveServerRenderModes.ps1", validation)
        self.assertIn("Assert-AsyncContinuationPolicy.ps1", validation)


if __name__ == "__main__":
    unittest.main()
