import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
APP = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT"


class LocalGptCircuitFreezeRegressions(unittest.TestCase):
    def test_enhanced_navigation_is_disabled_for_per_page_interactive_roots(self):
        app = (APP / "Components" / "App.razor").read_text(encoding="utf-8")
        menu = (APP / "wwwroot" / "js" / "localgpt-context-menu.js").read_text(encoding="utf-8")
        self.assertIn('<body data-enhance-nav="false">', app)
        self.assertIn('ssr: { disableDomPreservation: true }', app)
        self.assertIn('anchor.dataset.enhanceNav = "false"', menu)

        index = (APP / "Components" / "Pages" / "Index.razor").read_text(encoding="utf-8")
        layout = (APP / "Components" / "Layout" / "MainLayout.razor").read_text(encoding="utf-8")
        chat = (APP / "Components" / "Pages" / "Chat.razor").read_text(encoding="utf-8")
        self.assertIn('data-enhance-nav="false"', index)
        self.assertIn('data-enhance-nav="false"', layout)
        self.assertIn('NavigateTo("/onewire-security", forceLoad: true)', chat)

    def test_chat_background_work_waits_for_dxaichat_initialization(self):
        chat = (APP / "Components" / "Pages" / "Chat.razor").read_text(encoding="utf-8")
        after_render = chat.split("protected override Task OnAfterRenderAsync", 1)[1].split("void ChatInitialized()", 1)[0]
        initialized = chat.split("void ChatInitialized()", 1)[1].split("private async Task ClearHistoryAsync", 1)[0]
        self.assertNotIn("StartAutoSaveLoop();", after_render)
        self.assertNotIn("StartInitialModelRefresh();", after_render)
        self.assertIn("chatControlInitialized = true;", initialized)
        self.assertIn("StartAutoSaveLoop();", initialized)
        self.assertIn("StartInitialModelRefresh();", initialized)
        self.assertIn("!chatControlInitialized", chat)

    def test_onewire_initialization_is_bounded_and_renderer_affine(self):
        page = (APP / "Components" / "Pages" / "OneWireSecurity.razor").read_text(encoding="utf-8")
        self.assertTrue(page.startswith("@rendermode @(new InteractiveServerRenderMode(prerender: false))"))
        self.assertIn("CancelAfter(TimeSpan.FromSeconds(8))", page)
        self.assertIn('"InitialSecurityRefresh"', page)
        self.assertIn("InitializeAfterRenderAsync", page)
        self.assertNotIn("protected override async Task OnInitializedAsync()", page)
        self.assertIn("ConfigureAwait(true)", page)
        self.assertNotIn("ConfigureAwait(false)", page)
        self.assertIn("JSDisconnectedException", page)

    def test_circuit_transitions_are_logged(self):
        handler = (APP / "Diagnostics" / "LocalGptCircuitDiagnosticsHandler.cs").read_text(encoding="utf-8")
        program = (APP / "Program.cs").read_text(encoding="utf-8")
        self.assertIn("OnConnectionDownAsync", handler)
        self.assertIn("OnCircuitClosedAsync", handler)
        self.assertIn("AddSingleton<CircuitHandler, LocalGptCircuitDiagnosticsHandler>()", program)


if __name__ == "__main__":
    unittest.main()
