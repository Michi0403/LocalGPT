#!/usr/bin/env python3
"""Source-only integrity contract for 5.4.4 Council evidence/continuation recovery.

Does not claim Windows/macOS/Linux runtime coverage or .NET compilation.
"""
from pathlib import Path
root = Path(__file__).resolve().parents[1] / 'src' / 'LocalGPT'
def read(path): return (root/path).read_text(encoding='utf-8')
zip_source = read('Services/ChatUploadWorkspaceService.cs')
context = read('Services/CouncilRuntimeService.PromptAndTextRuntime.cs')
role = read('Services/OrganicCouncilBlueprintSeedDataService.cs')
workflow = read('Services/MultiModelCouncilService.WorkflowDefinitionExecution.cs')
state = read('BusinessObjects/CouncilServiceStateModels.cs')
gates = read('Services/MultiModelCouncilService.WorkspaceAndResearchGates.cs')
heartbeat = read('Services/MultiModelCouncilService.RoleSynthesis.cs')
chat = read('Components/Pages/Chat.LiveCouncil.razor.cs')
razor = read('Components/Pages/Chat.razor')
checks={
 'archive index not re-reading eager payload': 'Indexed read-only ZIP entry' in zip_source and 'analyzedFiles.Add(summary)' in zip_source and 'councilRuntime.AnalyzeBytes(relativePath, bytes, logger)' not in zip_source[zip_source.index('private async Task<bool> ExtractZipAsync'):],
 'prompt manifest is bounded by database policy': 'indexedFileLimit' in context and 'DefaultWorkspaceListCount' in context and '.Take(indexedFileLimit)' in context,
 'curator parses exact .csproj XML': 'System.Xml.Linq.XDocument.Load(path)' in zip_source and 'TargetFrameworks' in zip_source,
 'curator groups projects per ZIP root': 'projects.GroupBy(' in zip_source and '#### `{archiveGroup.Key}`' in zip_source,
 'source facts kept across roles': 'AuthoritativeCurationEvidence' in state and 'sourceAuthority = state.AuthoritativeCurationEvidence' in workflow and 'sourceAuthority);' in workflow,
 'model prompts keep separate provenance': 'A net8.0 sample' in zip_source and 'net10.0' in role,
 'new archive curation before message drain': heartbeat.index('CurateContinuationWorkspaceEvidenceAsync(') < heartbeat.index('humanCollaboration.DrainContributionsAsync('),
 'new archive gate checks complete': 'if (!report.IsComplete)' in gates[gates.index('CurateContinuationWorkspaceEvidenceAsync('):],
 'new archive gate preserves complete report': 'WriteLogAsync(result, CancellationToken.None, logger)' in gates[gates.index('CurateContinuationWorkspaceEvidenceAsync('):],
 'new upload source inherited next step': 'continuation workspace curator gate' in workflow,
 'research judge supports approved Github imports': 'localgpt.knowledge.remote.import' in role and 'human.collaboration.request' in role,
 'learning downstream phase waits for questions': all(('requiresHumanCheckpoint: true' in role[role.index(f'Step("{step}"'):role.index('Step("',role.index(f'Step("{step}"')+10)] if role.find('Step("',role.index(f'Step("{step}"')+10)>0 else True) for step in ('learning-study','learning-verify')),
 'chat refresh survives disposed scope': 'catch (ObjectDisposedException exception)' in chat and 'service scope was disposed' in chat,
 'in-chat Council progress': 'data-testid="live-council-progress"' in razor,
 'continuation messages persisted immediately': 'await PersistMessagesAsync(ChatClientProvider.SelectedSession.Messages.ToList()' in chat,
 'rejoin recovers matching run and not latest arbitrary': 'markerToRecover' in chat and 'ChatMemory.LoadConversationAsync(summary.Id' in chat,
 'active project version bumped': '<Version>5.4.4</Version>' in read('LocalGPT.csproj'),
}
for key,valid in checks.items():
 if not valid: print('FAILED',key)
if not all(checks.values()): raise SystemExit(1)
print(f'Council 5.4.4 source-grounding/continuation regression audit passed: {len(checks)} checks.')
