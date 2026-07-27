import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
APP = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT"


class LocalGptFrontendCancellationContracts(unittest.TestCase):
    def test_routed_ui_restores_reviewed_interactive_server_islands(self):
        app = (APP / "Components" / "App.razor").read_text(encoding="utf-8")
        self.assertIn("<HeadOutlet />", app)
        self.assertIn("<Routes></Routes>", app)
        self.assertIn('<ToastWrapper Name="ComponentSafetyToasts" />', app)
        self.assertIn("<InteractiveStartupMarker />", app)
        self.assertNotIn("<Routes @rendermode", app)
        self.assertNotIn("<HeadOutlet @rendermode", app)

        expected = {
            "InteractiveStartupMarker.razor": "@rendermode @(new InteractiveServerRenderMode(prerender: false))",
            "Layout/MenuIsland.razor": "@rendermode @(new InteractiveServerRenderMode(prerender: false))",
            "Layout/NavMenu.razor": "@rendermode InteractiveServer",
            "Layout/ThemeSwitcher.razor": "@rendermode @(new InteractiveServerRenderMode(prerender: true))",
            "Layout/ThemeSwitcherContainer.razor": "@rendermode @(new InteractiveServerRenderMode(prerender: true))",
            "Layout/ThemeSwitcherItem.razor": "@rendermode @(new InteractiveServerRenderMode(prerender: true))",
            "Layout/ToastWrapper.razor": "@rendermode @(new InteractiveServerRenderMode(prerender: false))",
            "Pages/Chat.razor": "@rendermode InteractiveServer",
            "Pages/CouncilTeams.razor": "@rendermode InteractiveServer",
            "Pages/Database.razor": "@rendermode InteractiveServer",
            "Pages/DxFunctionCatalog.razor": "@rendermode InteractiveServer",
            "Pages/Install.razor": "@rendermode InteractiveServer",
            "Pages/MinecraftModBuilder.razor": "@rendermode InteractiveServer",
            "Pages/ModelCouncil.razor": "@rendermode InteractiveServer",
            "Pages/OneWireSecurity.razor": "@rendermode InteractiveServer",
            "Pages/ProjectMaintenance.razor": "@rendermode InteractiveServer",
            "Pages/Projects.razor": "@rendermode InteractiveServer",
            "Pages/TestLab.razor": "@rendermode InteractiveServer",
        }
        components = APP / "Components"
        for relative, directive in expected.items():
            first = next(line.strip() for line in (components / relative).read_text(encoding="utf-8").splitlines() if line.strip())
            self.assertEqual(directive, first, relative)

        layout = (APP / "Components" / "Layout" / "MainLayout.razor").read_text(encoding="utf-8")
        self.assertNotIn("<InteractiveStartupMarker />", layout)
        self.assertNotIn('<ToastWrapper Name="ComponentSafetyToasts" />', layout)

    def test_route_changes_replace_the_body_without_error_boundary_recovery_races(self):
        routes = (APP / "Components" / "Routes.razor").read_text(encoding="utf-8")
        self.assertIn('<SafeErrorBoundary @key="NavigationManager.Uri"', routes)
        self.assertIn("LocationChanged += HandleLocationChanged", routes)
        self.assertNotIn("routeBoundary?.Recover()", routes)
        self.assertNotIn("RecoverAfterNavigationAsync", routes)

    def test_controller_diagnostics_filter_is_implemented_and_registered(self):
        program = (APP / "Program.cs").read_text(encoding="utf-8")
        filter_source = (APP / "Diagnostics" / "ControllerRequestLoggingFilter.cs").read_text(encoding="utf-8")
        self.assertIn("AddScoped<ControllerRequestLoggingFilter>()", program)
        self.assertIn("Filters.AddService<ControllerRequestLoggingFilter>()", program)
        self.assertIn("IAsyncActionFilter", filter_source)
        self.assertIn("RecordFailure", filter_source)

    def test_chat_autosave_starts_after_interactive_attach_without_cancelled_delay(self):
        chat = (APP / "Components" / "Pages" / "Chat.razor").read_text(encoding="utf-8")
        self.assertIn("interactiveAttached = true;", chat)
        self.assertIn("StartInitialModelRefresh();", chat)
        self.assertNotIn('JS.InvokeVoidAsync("localGptReady.markInteractive")', chat)
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


class LocalGptDiagnosticsContracts(unittest.TestCase):
    def test_operational_diagnostics_gate_is_wired(self):
        root = ROOT
        targets = (root / "Directory.Build.targets").read_text(encoding="utf-8")
        gate = (root / "build" / "Assert-OperationalDiagnostics.ps1").read_text(encoding="utf-8")
        self.assertIn("Assert-OperationalDiagnostics.ps1", targets)
        self.assertIn("Dispose methods are exempt", gate)
        self.assertIn("Operational diagnostics validation passed", gate)
        imports = (APP / "Components" / "_Imports.razor").read_text(encoding="utf-8")
        self.assertIn("OperationalLoggerFactory", imports)
        self.assertIn("OperationalNotifier", imports)
        self.assertIn("MigrateAsync", gate)
