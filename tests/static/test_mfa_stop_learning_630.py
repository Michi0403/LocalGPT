"""Regression checks for the tested customer report (MFA, Stop, multi-ZIP Learning Round).

These checks are static contract tests, not a substitute for compiled Razor or live model execution.
"""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
S = ROOT / 'src/LocalGPT/Services'
C = ROOT / 'src/LocalGPT/Components/Pages'
I = ROOT / 'src/LocalGPT/Interfaces'

class CustomerRuntimeRegressionTests(unittest.TestCase):
    def test_mfa_popup_is_independent_from_peer_trust(self):
        contract = (I/'IOneWireServices.cs').read_text()
        service = (S/'OneWire/OneWireRuntimeSecurityService.cs').read_text()
        ui = (C/'OneWireSecurity.razor').read_text()
        self.assertIn('Task<bool> VerifyCurrentMfaCodeAsync(', contract)
        verify = service[service.index('public async Task<bool> VerifyCurrentMfaCodeAsync('):service.index('public async Task<bool> EstablishTrustAsync(')]
        self.assertIn('createWhenMissing: false', verify)
        self.assertIn('VerifyTotp(file.MfaSeed, code)', verify)
        self.assertNotIn('TrustedPeers.Add(', verify)
        self.assertIn('<DxPopup @bind-Visible="_showMfaVerification"', ui)
        self.assertIn('Click="VerifyMfaEnrollmentAsync"', ui)
        self.assertIn('Click="OpenMfaVerificationAsync"', ui)
        self.assertIn('_showMfaVerification = true;', ui)

    def test_stop_covers_native_chat_and_explicit_selected_run(self):
        ui = (C/'Chat.LiveCouncil.razor.cs').read_text()
        service = (S/'CouncilChatClient.cs').read_text()
        self.assertIn('[SelectedCouncilRunId, AttachedLiveCouncilRunId, RejoinCouncilRunId]', ui)
        self.assertIn('CouncilLiveSessions.Cancel(runId)', ui)
        self.assertIn('liveSessions.Cancel(request.RunId)', service)
        self.assertIn('cancellationToken.IsCancellationRequested', service)
        live = (S/'CouncilLiveSessionService.cs').read_text()
        self.assertIn('? "Council stopped by user."', live)

    def test_recovery_pages_are_source_backed_and_not_hash_receipts(self):
        selection = (S/'MultiModelCouncilService.SourceReviewRecovery.cs').read_text()
        execution = (S/'MultiModelCouncilService.ConfiguredWorkflowExecution.cs').read_text()
        progress = (S/'MultiModelCouncilService.WorkspaceReadProgress.cs').read_text()
        self.assertIn('uploadWorkspaces.ReadFileAsync(workspaceName, path, pageBudget', selection)
        self.assertIn('deliveredPages.Add(slice)', selection)
        self.assertIn('if (!slice.HasMore) break', selection)
        self.assertIn('result.Steps.Add(new MultiModelCouncilStep', execution)
        self.assertIn('source recovery gateway', execution)
        self.assertIn('item.WorkflowRevision == nextRevision', execution)
        self.assertIn('!string.IsNullOrWhiteSpace(item.VisibleContent)', execution)
        self.assertIn('!string.Equals(kind.GetString(), "text"', progress)
        self.assertIn('characters <= 0', progress)
        self.assertIn('if (segment.Final', progress)

    def test_preserve_review_and_approval_gates(self):
        seed = (S/'OrganicCouncilBlueprintSeedDataService.cs').read_text()
        self.assertIn('localgpt.learning.maintain', seed)
        self.assertIn('localgpt.learning.file.review', seed)
        self.assertIn('localgpt.regex.test', seed)
        self.assertIn('localgpt.regex.upsert', seed)
        self.assertIn('requiresHumanCheckpoint: true', seed)

if __name__ == '__main__': unittest.main()
