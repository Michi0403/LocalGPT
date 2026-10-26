"""Source contract checks for 6.3.1. Full ASP.NET/.NET runtime tests run on the owner's Windows host."""
from pathlib import Path
import unittest
ROOT = Path(__file__).resolve().parents[2] / 'src' / 'LocalGPT'

class Regression631(unittest.TestCase):
    def test_uncapped_db_profiles(self):
        s = (ROOT / 'Services/AiProviderBootstrapService.cs').read_text()
        self.assertNotIn('GetEntriesAsync(includeArchived: false, take: 500', s)
        self.assertIn('db.CouncilKnowledgeEntries.AsNoTracking()', s)
        self.assertIn('!entry.IsArchived', s)
        self.assertIn('entry.ReviewStatus != "Deprecated"', s)
        self.assertIn('entry.ExpiresAtUtc == null || entry.ExpiresAtUtc > now', s)
        self.assertIn('await databaseInitializer.InitializeAsync', s)
        self.assertIn('NormalizeProfile(profile)', s)
        self.assertIn('MaintainedOllamaModelAliases', s)

    def test_ai_can_request_approval_but_cannot_approve_itself(self):
        tools = (ROOT / 'Services/OllamaThinkingChatClient.Tools.cs').read_text()
        resolver = (ROOT / 'Services/OllamaThinkingChatClient.ToolExecution.cs').read_text()
        registry = (ROOT / 'Services/DxAiFunctionRegistry.cs').read_text()
        self.assertIn('function.RequiresHumanConfirmation ||', tools)
        self.assertIn('function.SupportsDeferredApprovalRequest', resolver)
        self.assertIn('humanCollaboration.AuthorizeOrEnqueueAsync(', registry)
        self.assertIn('await deferredInvocations.QueueAsync(', registry)
        self.assertIn('if (!gate.IsAuthorized)', registry)

    def test_two_gpu_rows_have_one_host_editor_and_coherent_endpoint(self):
        setup = (ROOT / 'Components/Shared/InitialSetupAssistantPanel.razor').read_text()
        install = (ROOT / 'Components/Pages/Install.razor').read_text()
        hardware_form = (ROOT / 'Components/Shared/InitialSetupHardwareForm.razor').read_text()
        self.assertIn('<InitialSetupHardwareForm Hardware="@hardware"', setup)
        self.assertIn('Hardware.GroupBy(device =>', hardware_form)
        self.assertIn('UpdateHardwareGroupEndpoint(hostGroup.Key, endpoint)', hardware_form)
        self.assertIn('foreach (var device in Hardware.Where', (ROOT / 'Components/Shared/InitialSetupHardwareForm.razor.cs').read_text())
        self.assertIn('renderedHardwareHostKeys.Add(', install)

    def test_console_tail_and_user_scroll_intent(self):
        feed = (ROOT / 'Services/OperationalActivityFeedService.cs').read_text()
        view = (ROOT / 'Components/Shared/OperationalActivityFeed.razor').read_text()
        js = (ROOT / 'wwwroot/js/localgpt-operational-feed.js').read_text()
        self.assertLess(feed.index('Application activity (separate'), feed.index('Shared operational / ASCII console'))
        self.assertIn('preserveTailScroll', view)
        self.assertIn('intent.follow', js)

    def test_ascii_sizes_and_existing_mfa_stop_contract(self):
        chat = (ROOT / 'Components/Pages/Chat.razor').read_text()
        self.assertIn('Text="Compact"', chat)
        self.assertIn('Text="Full window"', chat)
        self.assertIn('CouncilStopRequested="StopSelectedCouncilSessionAsync"', chat)
        self.assertIn('Click="VerifyMfaEnrollmentAsync"', (ROOT / 'Components/Pages/OneWireSecurity.razor').read_text())

if __name__ == '__main__': unittest.main()
