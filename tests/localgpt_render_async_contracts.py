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
        self.assertEqual(2, manifest["schemaVersion"])
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
            limits = baseline.get(relative, {
                "maxUnconfiguredAwaitCount": 0,
                "maxConfigureAwaitTrueCount": 0,
                "minConfigureAwaitFalseCount": 0,
            })
            self.assertLessEqual(unconfigured, limits["maxUnconfiguredAwaitCount"], relative)
            self.assertLessEqual(true_count, limits["maxConfigureAwaitTrueCount"], relative)
            if relative.startswith("Components/"):
                self.assertEqual(0, false_count, relative)
            else:
                self.assertGreaterEqual(false_count, limits["minConfigureAwaitFalseCount"], relative)
            checked += 1
        self.assertGreater(checked, 100)

    def test_renderer_sources_keep_the_renderer_context_and_services_keep_false_continuations(self):
        component_false = []
        component_true = 0
        for path in (APP / "Components").rglob("*"):
            if path.suffix not in {".cs", ".razor"}:
                continue
            text = path.read_text(encoding="utf-8")
            if ".ConfigureAwait(false)" in text:
                component_false.append(path.relative_to(APP).as_posix())
            component_true += text.count(".ConfigureAwait(true)")
        self.assertEqual([], component_false)
        self.assertGreater(component_true, 250)

        service_false = 0
        for folder in ("Services", "Controller"):
            for path in (APP / folder).rglob("*.cs"):
                service_false += path.read_text(encoding="utf-8").count(".ConfigureAwait(false)")
        self.assertGreater(service_false, 1000)

    def test_async_guard_is_windows_powershell_51_parser_safe(self):
        gate = (BUILD / "Assert-AsyncContinuationPolicy.ps1").read_text(encoding="utf-8")
        self.assertIn('"${relative}: continuation count exceeds await count', gate)
        self.assertNotRegex(gate, r'"[^"\r\n]*\$[A-Za-z_][A-Za-z0-9_]*:')

    def test_repository_validation_runs_both_guards(self):
        validation = (BUILD / "Invoke-RepositoryValidation.ps1").read_text(encoding="utf-8")
        self.assertIn("Assert-InteractiveServerRenderModes.ps1", validation)
        self.assertIn("Assert-AsyncContinuationPolicy.ps1", validation)


if __name__ == "__main__":
    unittest.main()
