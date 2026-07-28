import hashlib
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
APP = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT"


def normalized_hash(path: pathlib.Path) -> str:
    text = path.read_text(encoding="utf-8-sig").replace("\r\n", "\n").replace("\r", "\n")
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def load_manifest(path: pathlib.Path) -> dict[str, str]:
    result = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        digest, relative = line.split("  ", 1)
        result[relative.replace("\\", "/")] = digest.lower()
    return result


class Final23JavaScriptRuntimeFix(unittest.TestCase):
    def test_runtime_loads_before_framework_and_mirrors_errors(self):
        app = (APP / "Components/App.razor").read_text(encoding="utf-8")
        diagnostics = app.index('<script src="js/javascript-diagnostics.js"></script>')
        self.assertLess(diagnostics, app.index("@DxResourceManager.RegisterScripts()"))
        self.assertLess(diagnostics, app.index('<script src="_framework/blazor.web.js"'))
        runtime = (APP / "wwwroot/js/javascript-diagnostics.js").read_text(encoding="utf-8")
        for token in ("console.error", 'window.addEventListener("error"', "unhandledrejection", "ReportJavaScriptErrorAsync", "pendingReports", "guardObject", "guardClass"):
            self.assertIn(token, runtime)
        bridge = (APP / "Components/InteractiveStartupMarker.razor").read_text(encoding="utf-8")
        for token in ("InteractiveServerRenderMode(prerender: false)", "localGptJavaScriptDiagnostics.bindDotNet", "[JSInvokable]", "Logger.LogError"):
            self.assertIn(token, bridge)

    def test_theme_collaboration_and_chat_layout_regressions_are_fixed(self):
        module = (APP / "wwwroot/switcher-resources/theme-controller.js").read_text(encoding="utf-8")
        dispatcher = (APP / "Components/Layout/ThemeJsChangeDispatcher.cs").read_text(encoding="utf-8")
        self.assertIn("export async function applyThemeState", module)
        self.assertIn('"applyThemeState"', dispatcher)
        self.assertNotIn('"ThemeController.applyThemeState"', dispatcher)
        for relative in ("Components/Layout/HumanCollaborationInbox.razor", "Components/Layout/CouncilSpoolerPanel.razor"):
            text = (APP / relative).read_text(encoding="utf-8")
            self.assertTrue(text.startswith("@rendermode @(new InteractiveServerRenderMode(prerender: false))"))
            self.assertIn("@onclick", text)
        css = (APP / "wwwroot/css/localgpt-theme-contract.css").read_text(encoding="utf-8")
        for token in ('[data-testid="dxaichat-host"] > *', ".localgpt-chat-root", "flex: 1 1 100% !important", "max-width: none !important"):
            self.assertIn(token, css)

    def test_maintained_javascript_inventory_is_guarded_and_current(self):
        manifest = load_manifest(ROOT / "build/javascript-diagnostics-files.sha256")
        files = [p for p in (APP / "wwwroot/js").glob("*.js") if p.name != "devextreme-license.example.js"]
        files.append(APP / "wwwroot/switcher-resources/theme-controller.js")
        expected = {p.relative_to(ROOT).as_posix() for p in files}
        self.assertEqual(expected, set(manifest))
        for path in files:
            relative = path.relative_to(ROOT).as_posix()
            text = path.read_text(encoding="utf-8-sig")
            self.assertEqual(normalized_hash(path), manifest[relative], relative)
            self.assertRegex(text, r"javascript-diagnostics:\s*guarded")
            self.assertRegex(text, r"\btry\s*\{")
            self.assertRegex(text, r"\bcatch\s*(?:\([^)]*\))?\s*\{")
            self.assertNotRegex(text, r"catch\s*(?:\([^)]*\))?\s*\{\s*\}")
        guard = (ROOT / "build/Assert-JavaScriptDiagnostics.ps1").read_text(encoding="utf-8")
        targets = (ROOT / "Directory.Build.targets").read_text(encoding="utf-8")
        self.assertIn("javascript-diagnostics-files.sha256", guard)
        self.assertIn("Assert-JavaScriptDiagnostics.ps1", targets)
        self.assertIn("AssertLocalGptJavaScriptDiagnostics", targets)


if __name__ == "__main__":
    unittest.main()
