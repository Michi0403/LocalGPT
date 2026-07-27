import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
APP = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT"


class LocalGptFrontendCancellationContracts(unittest.TestCase):
    def test_runtime_components_do_not_prerender_interactive_server_islands(self):
        for path in (APP / "Components").rglob("*.razor"):
            text = path.read_text(encoding="utf-8")
            self.assertNotIn("@rendermode InteractiveServer", text, path)
            self.assertNotIn("prerender: true", text, path)

    def test_chat_autosave_starts_after_interactive_attach_without_cancelled_delay(self):
        chat = (APP / "Components" / "Pages" / "Chat.razor").read_text(encoding="utf-8")
        self.assertIn("interactiveAttached = true;", chat)
        self.assertIn("WaitForAutoSaveIntervalAsync", chat)
        self.assertNotIn("Task.Delay(TimeSpan.FromSeconds(12), cancellationToken)", chat)
        initialized = chat.split("void ChatInitialized()", 1)[1].split("private async Task ClearHistoryAsync", 1)[0]
        self.assertNotIn("StartAutoSaveLoop();", initialized)

    def test_development_html_is_not_response_compressed(self):
        program = (APP / "Program.cs").read_text(encoding="utf-8")
        self.assertIn("if (!app.Environment.IsDevelopment())", program)
        self.assertIn("app.UseResponseCompression()", program)

    def test_ollama_openai_compatible_defaults_precede_lm_studio_fallbacks(self):
        import json
        options = (APP / "BusinessObjects" / "AICoreOptions.cs").read_text(encoding="utf-8")
        factory = (APP / "Services" / "ChatClientFactory.cs").read_text(encoding="utf-8")
        settings = json.loads((APP / "appsettings.json").read_text(encoding="utf-8"))
        self.assertIn('"http://localhost:11434/v1"', options)
        self.assertEqual("http://localhost:11434/v1", settings["AICore"]["ChatGPTLocalCore"]["Endpoint"])
        self.assertLess(factory.index("configuredOllamaEndpoints"), factory.index('"http://localhost:1234/v1"'))


if __name__ == "__main__":
    unittest.main()
