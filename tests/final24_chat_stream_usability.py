import hashlib
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
APP = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT"


def normalized_hash(path: pathlib.Path) -> str:
    text = path.read_text(encoding="utf-8-sig").replace("\r\n", "\n").replace("\r", "\n")
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


class Final24ChatStreamUsability(unittest.TestCase):
    def test_streamed_output_and_former_thoughts_are_copyable(self):
        chat = (APP / "Components/Pages/Chat.razor").read_text(encoding="utf-8")
        css = (APP / "Components/Pages/Chat.razor.css").read_text(encoding="utf-8")
        self.assertGreaterEqual(chat.count('data-localgpt-copyable="true"'), 2)
        self.assertIn('user-select: text !important', css)
        self.assertIn('.demo-chat ::deep .demo-chat-content *', css)

    def test_context_menu_preserves_native_copy_actions(self):
        script = (APP / "wwwroot/js/localgpt-context-menu.js").read_text(encoding="utf-8")
        self.assertIn("const copyableSelector", script)
        self.assertIn("function shouldUseNativeContextMenu", script)
        self.assertIn("if (shouldUseNativeContextMenu(target)) { close(); return; }", script)
        self.assertLess(script.index("shouldUseNativeContextMenu(target)"), script.index("event.preventDefault();"))

    def test_chat_grows_into_released_viewport_space_and_guard_is_enforced(self):
        css = (APP / "Components/Pages/Chat.razor.css").read_text(encoding="utf-8")
        guard = (ROOT / "build/Assert-JavaScriptDiagnostics.ps1").read_text(encoding="utf-8")
        for token in ["min-height: calc(100dvh - 5.25rem)", "flex: 1 1 32rem", "max-height: none"]:
            self.assertIn(token, css)
            self.assertIn(token, guard)
        manifest = {}
        manifest_path = ROOT / "build/javascript-diagnostics-files.sha256"
        for line in manifest_path.read_text(encoding="utf-8").splitlines():
            if line and not line.startswith("#"):
                digest, relative = line.split("  ", 1)
                manifest[relative] = digest
        relative = "LocalGPTWebviewWrapper/LocalGPT/wwwroot/js/localgpt-context-menu.js"
        self.assertEqual(normalized_hash(ROOT / relative), manifest[relative])


if __name__ == "__main__":
    unittest.main()
