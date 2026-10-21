"""Source-only goal routing regression checks; compiler/runtime gates remain mandatory."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / 'src' / 'LocalGPT'


class GoalExecutionReleaseTests(unittest.TestCase):
    def test_goal_service_exists_and_is_registered(self):
        service = (APP / 'Services/GoalExecutionService.cs').read_text()
        iface = (APP / 'Interfaces/IGoalExecutionService.cs').read_text()
        self.assertIn('class GoalExecutionService(', service)
        self.assertIn('interface IGoalExecutionService', iface)
        self.assertIn('PlanAsync(', iface)
        self.assertIn('ExecuteAsync(', iface)
        self.assertIn('AddScoped<IGoalExecutionService, GoalExecutionService>()',
                      (APP / 'Program.ServiceRegistration.cs').read_text())

    def test_goal_dispatch_uses_actual_catalog_and_saved_teams(self):
        service = '\n'.join(path.read_text() for path in (APP / 'Services').glob('GoalExecutionService*.cs'))
        for expected in ('directory.SynchronizeAsync(', 'teams.GetTeamsAsync(',
                         'function.AvailableToAi', 'function.SupportsAutomaticInvocation',
                         'function.SupportsDeferredApprovalRequest',
                         'skill.IsEnabled && skill.IsOnline', 'council.GetCandidatesAsync(',
                         'candidate.IsInstalled && candidate.IsConfigured',
                         'request.PreserveSelectedTeam', 'council.RunAsync(',
                         'gameBootstrap.EnsureSessionAsync(',
                         'request.CouncilRequest.CouncilTeamKey = step.TeamKey'):
            self.assertIn(expected, service)
        self.assertNotIn('teams.SaveAsync(', service)
        self.assertNotIn('UserConfirmed = true', service)
        self.assertNotIn('switch (goal', service)

    def test_both_chat_paths_and_onewire_route_through_goal(self):
        chat = (APP / 'Services/CouncilChatClient.cs').read_text()
        self.assertEqual(chat.count('goalService.ExecuteAsync('), 2)
        self.assertEqual(chat.count('GetRequiredService<IGoalExecutionService>()'), 3)
        self.assertNotIn('councilService.RunAsync(', chat)
        self.assertIn('request.GoalText = history.LastOrDefault(', chat)
        wire = (APP / 'Services/OneWire/OneWireExecutionServices.cs').read_text()
        section = wire.split('if (string.Equals(item.CapabilityKey, "localgpt.screenreader.help"', 1)[0]
        self.assertIn('goalService.ExecuteAsync(', section)
        self.assertIn('RequiredCapabilities = request.RequestedOrganicCapabilities.ToList()', section)
        self.assertIn('JsonSerializer.Serialize(result, codec.JsonOptions)', section)

    def test_plan_does_not_self_authorize_or_claim_model_prose_as_evidence(self):
        source = '\n'.join(path.read_text() for path in (APP / 'Services').glob('GoalExecutionService*.cs'))
        self.assertIn('step.VerifiedWorkspaceReadMetadataJson', source)
        self.assertIn('step.Error', source)
        self.assertIn('result.Artifacts', source)
        self.assertIn('outcome.CouncilResult.GoalExecution = new GoalExecutionSummary', source)
        self.assertIn('GoalExecutionState.NeedsVerification', source)
        self.assertNotIn('UserConfirmedArtifactBuild = true', source)


if __name__ == '__main__':
    unittest.main()
