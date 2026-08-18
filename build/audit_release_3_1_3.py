#!/usr/bin/env python3
"""Static source audit for LocalGPT 3.1.3 Council round recovery and UI/cancellation stability."""
from __future__ import annotations
from pathlib import Path
import hashlib, sys

root = Path(__file__).resolve().parents[1]
checks = 0

def read(rel: str) -> str:
    p = root / rel
    if not p.is_file():
        raise AssertionError(f"missing {rel}")
    return p.read_text(encoding="utf-8-sig", errors="strict")

def require(rel: str, *tokens: str) -> None:
    global checks
    data = read(rel)
    for token in tokens:
        checks += 1
        if token not in data:
            raise AssertionError(f"{rel} missing {token!r}")

def forbid(rel: str, *tokens: str) -> None:
    global checks
    data = read(rel)
    for token in tokens:
        checks += 1
        if token in data:
            raise AssertionError(f"{rel} unexpectedly contains {token!r}")

def tree_digest(path: Path) -> str:
    h = hashlib.sha256()
    for p in sorted(path.rglob('*')):
        if not p.is_file():
            continue
        rel = p.relative_to(root).as_posix().encode('utf-8')
        h.update(rel + b'\0' + p.read_bytes() + b'\0')
    return h.hexdigest()

try:
    for rel in (
        'src/LocalGPT/LocalGPT.csproj',
        'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
        'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
    ):
        require(rel, '<Version>3.1.3</Version>')
    require('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj', '<Version>2.1.1</Version>')
    require('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs', 'LocalGPT/3.1.3')
    require('global.json', '"version": "10.0.400"')
    require('src/LocalGPT/LocalGPT.csproj',
            '<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.11" />',
            '<PackageReference Include="DevExpress.Blazor" Version="25.2.*" />')

    checks += 1
    got = tree_digest(root / 'src/LocalGPT/Migrations')
    expected = '27c5b6d71b8f9527b64f18ff66ac102ae0558e4ed01317ff02e34f6b77f99c4f'
    if got != expected:
        raise AssertionError(f'migration source changed: {got} != {expected}')
    checks += 1
    db = root / 'src/LocalGPT/Services/Persistence/DatabaseMigrationCompatibilityService.cs'
    got_db = hashlib.sha256(db.read_bytes()).hexdigest()
    expected_db = '50bb2f62df4b6cfe5846063d5e4f20c2ab930a57cb95efa580ad6617f3a748ba'
    if got_db != expected_db:
        raise AssertionError(f'database migration compatibility source changed: {got_db} != {expected_db}')

    # Earlier benchmark evidence and truth guard stay intact.
    require('src/LocalGPT/Services/ProviderModelBenchmarkService.EvidencePersistence.cs',
            'BenchmarkEvidence',
            'private const int BenchmarkEvidenceSchemaVersion = 1;')
    require('src/LocalGPT/BusinessObjects/ProviderModelBenchmarkCoverageSnapshot.cs',
            'AttemptedTargetCount - SuccessfulTargetCount == UnresolvedTargetCount',
            'UnresolvedSelectionKeys')
    require('src/LocalGPT/Services/CouncilBenchmarkCalibrationService.cs',
            '### Machine-derived coverage invariant',
            '**Truth guard:**')

    # Recovery policy is serializable/user-owned and explicitly exposed in Council Teams.
    require('src/LocalGPT/BusinessObjects/OrganicCouncilModels.cs',
            'public enum CouncilMemberFailureRecoveryMode',
            'RetrySameMember',
            'RetrySameThenEligibleRolePool',
            'public CouncilMemberFailureRecoveryMode MemberFailureRecoveryMode',
            'public int MemberFailureRecoveryAttempts { get; set; } = 3;')
    require('src/LocalGPT/Components/Pages/CouncilTeams.razor',
            'Provider/member failure recovery',
            'Round-level recovery turns after built-in safe fallback',
            'MemberFailureRecoveryModes')
    require('src/LocalGPT/Services/CouncilTeamConfigurationService.Validation.cs',
            'CouncilMemberFailureRecoveryMode.RetrySameThenEligibleRolePool',
            'Math.Clamp(step.MemberFailureRecoveryAttempts, 0, 8)')

    # Round repair preserves evidence and derives alternates from existing role selection.
    require('src/LocalGPT/Services/MultiModelCouncilService.RoundRecovery.cs',
            'RecoverConfiguredRoundMemberFailuresAsync',
            'The failed attempt remains preserved in Council evidence',
            'roleDefinition.AssignedModelKeys',
            'DistinctAiAssignmentGroup',
            'GetCouncilExecutionHostKey(failedModel)',
            'OrderParticipantsByObservedHealth(result, candidates)',
            'automatic member recovery',
            'AssignedModelSingle',
            'LocalGPT did not silently drop or fabricate this member result')
    require('src/LocalGPT/Services/MultiModelCouncilService.WorkflowDefinitionExecution.cs',
            'RecoverConfiguredRoundMemberFailuresAsync(',
            'IsConfiguredRoundPrimaryOrRecoveryPhase(step.Phase, phase)',
            '.Concat(recoveredModels)')
    require('src/LocalGPT/Services/MultiModelCouncilService.PhaseExecution.cs',
            'The failure is converted into explicit step evidence so the host queue and configured round recovery can continue.',
            'The exception is rethrown so configured round recovery or the run-level failure boundary can preserve the failure instead of silently dropping the round.',
            'throw;')

    # Caller cancellation remains true cancellation and does not enter generic failure accounting.
    require('src/LocalGPT/Services/OllamaThinkingChatClient.Transport.cs',
            'catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)',
            'aborting the underlying transport is expected during Council stop/round cancellation')
    require('src/LocalGPT/Services/MultiModelCouncilService.RunOrchestration.cs',
            'was stopped by caller cancellation',
            'is not classified as a Council failure',
            'The Council run was stopped by an explicit user action.',
            'councilSpooler.Complete(result);')
    require('src/LocalGPT/Services/ComponentActivityService.cs',
            'ended because its caller cancellation token was signaled.')

    # Live user-message UI uses component-owned state, not heartbeat-recreated JS message rows.
    require('src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs',
            'The old JavaScript',
            'LoadSelectedSessionMessages();')
    require('src/LocalGPT/wwwroot/js/localgpt-chat-ui.js',
            'The .NET callback has already inserted the accepted message into the authoritative DxAIChat',
            'Accepted live user messages are rendered by DxAIChat from the authoritative .NET session.')
    forbid('src/LocalGPT/wwwroot/js/localgpt-chat-ui.js',
           "renderLiveUserMessages(host, scrollRegion);")

    require('CHANGELOG-v3.1.3-COUNCIL-ROUND-RECOVERY-CANCELLATION-UI-STABILITY.md',
            'No existing 3.1.2 feature was reverted',
            'No EF Core migration or SQLite schema change is introduced',
            'debugger configured to break on first-chance',
            '3.1.3')
    require('VALIDATION-v3.1.3-source.md', 'No `dotnet`', '3.1.3')
    require('RELEASE.md', '# LocalGPT 3.1.3', 'configured Social Team rounds recover required member work')

    print(f'LocalGPT 3.1.3 Council resilience source audit passed: {checks} checks.')
except (AssertionError, ValueError) as exc:
    print(f'LocalGPT 3.1.3 source audit failed: {exc}', file=sys.stderr)
    raise SystemExit(1)
