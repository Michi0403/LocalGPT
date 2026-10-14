#!/usr/bin/env python3
"""Source-only regression contracts for the shared Chat/Human Collaboration decision lane."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1] / "src/LocalGPT"
def source(path):
    return (root / path).read_text(encoding="utf-8")

chat = source("Components/Pages/Chat.razor")
chat_code = source("Components/Pages/Chat.LiveCouncil.razor.cs")
inbox = source("Components/Layout/HumanCollaborationInbox.razor")
card = source("Components/Shared/HumanCollaborationRequestCard.razor")
card_logic = source("Components/Shared/HumanCollaborationRequestCard.razor.cs")
service = source("Services/HumanCollaborationService.cs")
models = source("BusinessObjects/HumanCollaborationModels.cs")
checks = {
    "single shared request card in Chat": '<HumanCollaborationRequestCard @key="request.Id" Request="@request"' in chat,
    "single shared request card in Human Collaboration": '<HumanCollaborationRequestCard @key="request.Id" Request="@request"' in inbox,
    "Chat no longer has a parallel inline submit implementation": "ResolveInlineChatRequestAsync(" not in chat_code,
    "Chat shows the same unfiltered pending database queue": '.Where(request => request.DecidedAtUtc is null)' in chat_code and '!AsciiText.IsCouncilRoleResponseRequest(request)' not in chat_code,
    "Human Collaboration shows its complete pending queue": 'item.Status == Vocabulary.Get().HumanStatusPending)' in inbox,
    "Chat shows persistent saved decisions": 'RecentChatReviewDecisions' in chat and 'RecentChatReviewDecisions' in chat_code,
    "Chat reads canonical pending and resolved requests": 'GetSnapshotAsync(includeResolved: true' in chat_code,
    "Human Collaboration reads persisted requests": 'GetSnapshotAsync(includeResolved: true' in inbox,
    "shared card supports explicit approvals": 'SubmitAsync(true)' in card and 'SubmitAsync(false)' in card,
    "shared card supports guidance": 'SubmitAsync(null)' in card,
    "shared card supports suggestions": 'Request.SuggestedResponses' in card and 'ChooseSuggestion(' in card,
    "shared card supports text input": '<DxMemo @bind-Text="response"' in card,
    "shared card supports reason input": '<DxMemo @bind-Text="reason"' in card,
    "shared card supports reuse scope": 'DxComboBox Data="@ReuseScopes"' in card,
    "shared card supports one-use approval": 'DxCheckBox @bind-Checked="consumeApproval"' in card,
    "shared card uses canonical request IDs": 'Request.Id' in card_logic and 'Collaboration.ResolveRequestAsync(' in card_logic,
    "shared card uses trusted local human context": 'HumanAmbientContext.PushHumanInteraction(' in card_logic,
    "shared card preserves human profile display identity": 'Collaboration.GetProfileAsync()' in card_logic,
    "shared card never approves on suggestion selection": 'private void ChooseSuggestion(' in card_logic and 'response = suggestion;' in card_logic,
    "shared card has explicit concurrency token": 'ExpectedDecisionVersion: editorDecisionVersion, RequirePending: true' in card_logic,
    "canonical contract accepts expected version": 'int? ExpectedDecisionVersion = null' in models,
    "canonical contract accepts pending-only guard": 'bool RequirePending = false' in models,
    "repository blocks stale duplicate decision writes": 'request.DecisionVersion != expectedVersion' in service,
    "repository blocks already handled pending decisions": 'submission.RequirePending && request.Status != vocabulary.Get().HumanStatusPending' in service,
    "saved edits are also version guarded": 'ExpectedDecisionVersion: request.DecisionVersion' in inbox,
    "approved deferred calls still execute once": 'ExecuteApprovedForApprovalRequestAsync(' in card_logic,
    "surfaces refresh after common service changes": 'HumanCollaboration.Changed += OnHumanCollaborationChanged' in source('Components/Pages/Chat.razor.cs') and 'Collaboration.Changed += OnCollaborationChanged' in inbox,
    "shared approval release remains active": (
        (match := re.search(r'<Version>(\d+)\.(\d+)\.(\d+)</Version>', source('LocalGPT.csproj'))) is not None
        and tuple(int(part) for part in match.groups()) >= (5, 4, 6)
    ),
}
for name, ok in checks.items():
    if not ok:
        print(f"FAIL: {name}")
if not all(checks.values()):
    raise SystemExit(1)
print(f"Unified human collaboration source audit passed: {len(checks)} checks.")
