"""Source-only regression assertions for model-assisted goal planning and crash-safe checkpoints."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / 'src/LocalGPT'


class GoalRecoveryReleaseTests(unittest.TestCase):
    def test_live_semantic_inference_is_read_only_and_validated(self):
        planner = (APP / 'Services/GoalExecutionService.SemanticPlanning.cs').read_text()
        runner = (APP / 'Services/GoalExecutionService.cs').read_text()
        policy = (APP / 'Services/Persistence/LocalGptRuntimePolicySeedDataService.cs').read_text()
        self.assertIn('providerModels.CreateChatClient(', planner)
        self.assertIn('enableAutomaticTools: false', planner)
        self.assertIn('GoalSemanticPlannerSystemPrompt', planner)
        self.assertIn('JsonSerializer.Deserialize<GoalSemanticProposal>', planner)
        self.assertIn('configuredTeams.FirstOrDefault(', runner)
        self.assertIn('validKeys.Contains(step.TeamKey)', runner)
        self.assertIn('request.PreserveSelectedTeam', runner)
        self.assertIn('GoalSemanticPlannerSystemPrompt', policy)
        self.assertIn('GoalMaximumReplans', policy)

    def test_checkpoint_is_write_ahead_and_preserves_prior_successes(self):
        ledger = (APP / 'Services/GoalExecutionLedgerService.cs').read_text()
        runner = (APP / 'Services/GoalExecutionService.DurableExecution.cs').read_text()
        self.assertIn('File.Move(temporaryPath, path, overwrite: true)', ledger)
        self.assertIn('FileOptions.WriteThrough', ledger)
        self.assertIn('SemaphoreSlim', ledger)
        self.assertIn('GoalExecutionState.Interrupted', ledger)
        self.assertIn('liveProcessGoals.ContainsKey(goalId)', ledger)
        self.assertIn('liveProcessGoals.TryAdd(checkpoint.GoalId, 0)', ledger)
        self.assertLess(runner.index('receipt.State = GoalExecutionState.Running;'),
                        runner.index('council.RunAsync('))
        self.assertIn('if (saved?.State == GoalExecutionState.Completed) continue;', runner)
        self.assertIn('await ledger.SaveAsync(checkpoint, cancellationToken)', runner)
        self.assertIn('GoalRecoveryRequiresExplicitApproval', runner)
        self.assertIn('TryReplanRemainingAsync(', runner)
        self.assertIn('GoalMaximumReplans', runner)

    def test_recovery_exposes_approval_gated_endpoint_and_preserves_original_approvals(self):
        controller = (APP / 'Controller/GoalExecutionController.cs').read_text()
        service = (APP / 'Services/GoalExecutionService.DurableExecution.cs').read_text()
        self.assertIn('HumanApprovalRequired("goal.execution.resume"', controller)
        self.assertIn('GetCheckpointAsync(', controller)
        self.assertIn('confirmInterruptedRetry: true', controller)
        self.assertNotIn('UserConfirmedArtifactBuild = true', service)
        self.assertNotIn('UserConfirmedProjectLink = true', service)
        self.assertIn('ModelSelectionKeys', service)
        self.assertIn('providerModels.ResolveAsync(', service)

    def test_services_registered_once(self):
        registration = (APP / 'Program.ServiceRegistration.cs').read_text()
        self.assertEqual(registration.count('AddScoped<IGoalExecutionService, GoalExecutionService>()'), 1)
        self.assertEqual(registration.count('AddSingleton<IGoalExecutionLedgerService, GoalExecutionLedgerService>()'), 1)


if __name__ == '__main__':
    unittest.main()
