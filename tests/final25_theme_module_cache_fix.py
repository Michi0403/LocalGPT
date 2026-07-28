import hashlib
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
APP = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT"


def normalized_hash(path: pathlib.Path) -> str:
    text = path.read_text(encoding="utf-8-sig").replace("\r\n", "\n").replace("\r", "\n")
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def load_manifest(path: pathlib.Path) -> dict[str, str]:
    result: dict[str, str] = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        digest, relative = line.split("  ", 1)
        result[relative.replace("\\", "/")] = digest.lower()
    return result


class Final25ThemeModuleCacheFix(unittest.TestCase):
    def test_theme_module_import_uses_static_asset_fingerprint(self):
        dispatcher = (APP / "Components/Layout/ThemeJsChangeDispatcher.cs").read_text(encoding="utf-8")
        for token in (
            "IFileVersionProvider",
            "AddFileVersionToPath(",
            '"switcher-resources/theme-controller.js"',
            'InvokeAsync<IJSObjectReference>("import", themeModulePath)',
            '"applyThemeState"',
        ):
            self.assertIn(token, dispatcher)
        self.assertNotIn('InvokeAsync<IJSObjectReference>("import", "./switcher-resources/theme-controller.js")', dispatcher)

    def test_app_uses_same_fingerprinted_theme_module_url(self):
        app = (APP / "Components/App.razor").read_text(encoding="utf-8")
        self.assertIn('var themeControllerModulePath = AppendVersion("switcher-resources/theme-controller.js");', app)
        self.assertIn('<script type="module" src="@themeControllerModulePath"></script>', app)
        self.assertNotIn('<script type="module" src="switcher-resources/theme-controller.js"></script>', app)

    def test_build_guard_and_protected_hashes_cover_the_fix(self):
        guard = (ROOT / "build/Assert-JavaScriptDiagnostics.ps1").read_text(encoding="utf-8")
        self.assertIn("Theme dispatcher cache-safe import contract", guard)
        self.assertIn("App.razor cache-safe theme module contract", guard)
        manifest = load_manifest(ROOT / "build/protected-files.sha256")
        for relative in (
            "build/Assert-JavaScriptDiagnostics.ps1",
            "LocalGPTWebviewWrapper/LocalGPT/Components/App.razor",
            "LocalGPTWebviewWrapper/LocalGPT/Components/Layout/ThemeJsChangeDispatcher.cs",
        ):
            self.assertEqual(normalized_hash(ROOT / relative), manifest[relative], relative)


if __name__ == "__main__":
    unittest.main()
