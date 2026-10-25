"""LocalGPT 6.3.5 source-only contract tests; compilation and AI behavior still require Windows/.NET."""
import unittest
from pathlib import Path

APP = Path(__file__).resolve().parents[2] / 'src/LocalGPT'


def source(rel):
    return (APP / rel).read_text(encoding='utf-8')


class KeyaccountManagerReleaseTests(unittest.TestCase):
    def test_applies_to_existing_goal_execution_for_all_teams(self):
        chat = source('Services/CouncilChatClient.cs')
        wire = source('Services/OneWire/OneWireExecutionServices.cs')
        runner = source('Services/GoalExecutionService.DurableExecution.cs')
        self.assertIn('goalService.ExecuteAsync(', chat)
        self.assertIn('goalService.ExecuteAsync(', wire)
        self.assertIn('ReviewKeyaccountCompletionAsync(', runner)
        self.assertIn('council.RunAsync(request.CouncilRequest', runner)
        self.assertLess(runner.index('council.RunAsync('), runner.index('ReviewKeyaccountCompletionAsync('))

    def test_review_is_read_only_and_preserves_human_approval(self):
        review = source('Services/GoalExecutionService.KeyaccountManager.cs')
        self.assertIn('enableAutomaticTools: false', review)
        self.assertIn('needsHumanReview', review)
        self.assertIn('GoalKeyaccountManagerSystemPrompt', review)
        self.assertNotIn('UserConfirmedArtifactBuild = true', review)
        self.assertNotIn('UserConfirmedProjectLink = true', review)

    def test_reviewer_conclusion_not_a_technical_receipt(self):
        review = source('Services/GoalExecutionService.KeyaccountManager.cs')
        self.assertIn('receipt.Evidence', review)
        self.assertIn('review.Complete = false', review)
        self.assertIn('Keyaccount reviewer unavailable', review)
        self.assertIn('CouncilArtifact', review)
        receipt = source('BusinessObjects/GoalExecutionModels.cs')
        self.assertIn('KeyaccountReviews', receipt)
        self.assertIn('ReviewerResponded', receipt)
        self.assertIn('KeyaccountPendingObjective', receipt)

    def test_plans_new_objectives_no_replay_and_uses_durable_journal(self):
        durable = source('Services/GoalExecutionService.DurableExecution.cs')
        self.assertIn('checkpoint.Steps.Add(new GoalExecutionPlanStep', durable)
        self.assertIn('KeyaccountContinuationsScheduled < checkpoint.KeyaccountContinuationLimit', durable)
        self.assertIn('await ledger.SaveAsync(checkpoint, cancellationToken)', durable)
        self.assertIn('keyaccount-" +', durable)
        self.assertIn('checkpoint.Steps.Any(item => string.Equals(item.Objective.Trim()', durable)
        self.assertIn('GoalExecutionState.NeedsVerification', durable)

    def test_regular_chat_exposes_iteration_choice(self):
        view = source('Components/Pages/Chat.razor')
        request = source('Components/Pages/Chat.ProviderRuntime.razor.cs')
        self.assertIn('Keyaccount extra rounds', view)
        self.assertIn('KeyaccountExpectedArtifacts', view)
        self.assertIn('<DxSpinEdit @bind-Value="KeyaccountContinuationRounds"', view)
        self.assertIn('KeyaccountContinuationLimit = Math.Clamp(KeyaccountContinuationRounds', request)

    def test_policy_is_db_owned_and_existing_ids_unchanged(self):
        policies = source('BusinessObjects/LocalGptRuntimePolicyModels.cs')
        seed = source('Services/Persistence/LocalGptRuntimePolicySeedDataService.cs')
        self.assertIn('GoalKeyaccountManagerSystemPrompt', policies)
        self.assertIn('GoalKeyaccountDefaultContinuations', policies)
        self.assertIn('new(LocalGptRuntimeValue.GoalKeyaccountManagerSystemPrompt', seed)
        self.assertIn('new(LocalGptRuntimeValue.GoalKeyaccountDefaultContinuations', seed)
        self.assertLess(policies.index('ProjectMarkerPatternTemplate,'), policies.index('GoalKeyaccountManagerSystemPrompt,'))

    def test_model_council_and_minecraft_ui_no_longer_bypass_goal(self):
        model = source('Components/Pages/ModelCouncil.razor.cs')
        minecraft = source('Components/Pages/MinecraftModBuilder.razor')
        self.assertIn('Goals.ExecuteAsync(new GoalExecutionRequest', model)
        self.assertIn('Goals.ExecuteAsync(new GoalExecutionRequest', minecraft)
        self.assertNotIn('CouncilService.RunAsync(', model)
        self.assertNotIn('CouncilService.RunAsync(', minecraft)

    def test_user_required_artifact_floor_enforced_on_observed_files(self):
        review = source('Services/GoalExecutionService.KeyaccountManager.cs')
        runner = source('Services/GoalExecutionService.DurableExecution.cs')
        self.assertIn('checkpoint.KeyaccountExpectedArtifacts > verifiedFiles', review)
        self.assertIn('KeyaccountExpectedArtifacts = Math.Clamp(councilRequest.KeyaccountExpectedArtifacts', runner)
        self.assertIn('File.Exists(artifact.FilePath)', runner)
        self.assertIn('Distinct(StringComparer.OrdinalIgnoreCase)', review)

    def test_human_approved_continuation_for_existing_goals(self):
        controller = source('Controller/GoalExecutionController.cs')
        service = source('Services/GoalExecutionService.cs')
        self.assertIn('HumanApprovalRequired("goal.execution.continue"', controller)
        self.assertIn('ContinueWithNewObjectiveAsync', service)
        self.assertIn('goal.execution.resume', controller)
        self.assertIn('checkpoint.KeyaccountContinuationApproved = true', service)
        self.assertIn('checkpoint.State = GoalExecutionState.Running', service)
        self.assertIn('if (checkpoint.State is GoalExecutionState.Running or GoalExecutionState.Interrupted', service)

    def test_no_domain_specific_hardcoded_agent(self):
        review = source('Services/GoalExecutionService.KeyaccountManager.cs')
        self.assertNotIn('if (goal.Contains("texture"', review)
        self.assertNotIn('if (goal.Contains("openscad"', review)
        self.assertIn('currentObjective', review)


if __name__ == '__main__':
    unittest.main()
