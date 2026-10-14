# LocalGPT 2.9.7 source changes

Source-only compile-repair release following the authoritative Windows `Build-LocalDevelopment.ps1` result for 2.9.6.

## Compile repair

- `HumanCollaborationInbox.razor` no longer reads a nonexistent `DeferredDxAiExecutionOutcome.Succeeded` member. Deferred execution success/failure is derived from the outcome's persisted `Status` contract (`DeferredCompleted` versus failure), matching `DeferredDxAiInvocationService`.
- `MultiModelCouncilService` no longer reads nonexistent `OrganicCouncilTeamDefinition.KnowledgeReferences`. Team-level knowledge relevance continues to use configured preferred capabilities, while request/project-specific grounding is detected from the supplied `ExternalProjectContextJson`.
- The role briefing still advertises authoritative local project evidence when external project context is supplied; no knowledge-grounding behavior from 2.9.6 is removed.

## Compatibility

- LocalGPT application / installer / WebView wrapper version: **2.9.7**.
- LocalGPT Wire Protocol remains **2.1.1**.
- No benchmark seed-version bump is required because this release repairs compile contracts without changing the maintained seed definition.
