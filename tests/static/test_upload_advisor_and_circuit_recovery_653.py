import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
def read(path):
    return (ROOT / path).read_text(encoding='utf-8-sig')

class UploadAdvisorAndCircuitRecovery653(unittest.TestCase):
    def test_fallback_prioritizes_workspace_curation(self):
        text = read('src/LocalGPT/Services/UploadProcessingRecommendationService.cs')
        self.assertIn('"chat.upload_workspace_curate"', text)
        self.assertIn('var team = workspaceCurationTeam ?? repositoryTeam', text)
        self.assertIn('string.IsNullOrWhiteSpace(request.UserGoal)', text)
        self.assertIn('ai.SuggestedTeamKey = fallback.SuggestedTeamKey', text)
    def test_disconnected_circuit_is_not_reported_as_successful_council_submission(self):
        text = read('src/LocalGPT/Components/Pages/Chat.Lifecycle.razor.cs')
        self.assertIn('catch (JSDisconnectedException exception)', text)
        self.assertIn('catch (TaskCanceledException exception)', text)
        advisor = read('src/LocalGPT/Components/Pages/Chat.UploadAdvisor.razor.cs')
        self.assertIn('submitted = await StartCouncilPromptAsync(starter, startFresh: false)', advisor)
        self.assertIn('if (!submitted)', advisor)
    def test_browser_can_recover_without_forwarding_error_storm(self):
        diagnostics = read('src/LocalGPT/wwwroot/js/javascript-diagnostics.js')
        reconnect = read('src/LocalGPT/wwwroot/js/localgpt-reconnect.js')
        self.assertIn('transportUnavailable = true;', diagnostics)
        self.assertIn('if (pendingReports.size >= 100)', diagnostics)
        self.assertIn('resumeTransport', diagnostics)
        self.assertIn('showDisconnected()', reconnect)
        self.assertIn('globalThis.localGptJavaScriptDiagnostics?.resumeTransport?.()', reconnect)
        self.assertIn('DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(10)',
                      read('src/LocalGPT/Program.ServiceRegistration.cs'))
    def test_release_version(self):
        self.assertIn('<Version>6.5.4</Version>', read('src/LocalGPT/LocalGPT.csproj'))

if __name__ == '__main__':
    unittest.main()
