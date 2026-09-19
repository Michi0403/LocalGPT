#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]

def read(rel):
    p=ROOT/rel
    if not p.is_file(): raise SystemExit(f"Kernel Creature Tournament audit failed: missing {rel}")
    return p.read_text(encoding='utf-8')

def main():
    seed=read('src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs')
    start=seed.find('Key = "kernel-creature-tournament"'); end=seed.find('Key = "ascii-doom-council-adventure"',start)
    if start<0 or end<0: raise SystemExit('Kernel Creature Tournament audit failed: seed boundary')
    tournament=seed[start:end]
    models=read('src/LocalGPT/BusinessObjects/CouncilGameModels.cs')
    interface=read('src/LocalGPT/Interfaces/ICouncilGameSessionService.cs')
    service=read('src/LocalGPT/Services/CouncilGameSessionService.KernelTournament.cs')
    rules=read('src/LocalGPT/Services/CouncilGameSessionService.KernelTournamentRules.cs')
    bootstrap=read('src/LocalGPT/Services/CouncilGameWorkflowBootstrapService.cs')
    runtime=read('src/LocalGPT/Services/CouncilRuntimeClassService.cs')
    orchestration=read('src/LocalGPT/Services/MultiModelCouncilService.WorkflowDefinitionExecution.cs')
    bridge=read('src/LocalGPT/Services/MultiModelCouncilService.KernelTournament.cs')
    console=read('src/LocalGPT/Components/Shared/ChatGameConsole.razor')
    console_css=read('src/LocalGPT/Components/Shared/ChatGameConsole.razor.css')
    js=read('src/LocalGPT/wwwroot/js/localgpt-game-console.js')
    checks=[
      ('runtime class seed', 'BuildDefinition("games.ascii.kernel-tournament.rules"' in runtime),
      ('starting health field', '"startingHealth"' in runtime),
      ('damage minimum field', '"minimumDamage"' in runtime),
      ('damage maximum field', '"maximumDamage"' in runtime),
      ('guard field', '"guardReduction"' in runtime),
      ('recovery field', '"recoveryAmount"' in runtime),
      ('exchange cap field', '"maximumExchangesPerMatch"' in runtime),
      ('frame delay field', '"animationFrameDelayMilliseconds"' in runtime and '320' in runtime),
      ('subtitle hold field', '"subtitleHoldMilliseconds"' in runtime and '1500' in runtime),
      ('bootstrap family', 'games.ascii.kernel-tournament.' in bootstrap),
      ('game key', 'kernel-creature-tournament' in bootstrap),
      ('assigned runtime class', 'RuntimeClassKeys = ["games.ascii.kernel-tournament.rules"]' in tournament),
      ('role isolated', 'CouncilContextCapabilities.RoleIsolated' in tournament),
      ('lineup engine step', 'Key = "arena-lineup-engine"' in tournament),
      ('exchange engine step', 'Key = "arena-round-engine"' in tournament),
      ('system execution mode', tournament.count('ExecutionMode = "SystemKernelTournamentResolution"') == 2),
      ('trainer micro-turn', 'COMMAND: ATTACK|GUARD|RECOVER|WAIT' in tournament),
      ('creature micro-turn', 'MOVE: ATTACK|GUARD|RECOVER|WAIT' in tournament),
      ('flavor line', 'FLAVOR: <one short terminal-safe visual action description>' in tournament),
      ('voice line', 'VOICE: <one short in-character line>' in tournament),
      ('AI cannot own result', 'LocalGPT—not you—resolves HP' in tournament),
      ('completion marker', '[[TOURNAMENT_COMPLETE]]' in tournament),
      ('loop group', tournament.count('LoopGroup = "kernel-creature-battle"') >= 3),
      ('tool-free maintained steps', tournament.count('CanUseOrganicFunctions = false') == 7),
      ('rules model', 'class CouncilKernelTournamentRules' in models),
      ('fighter model', 'class CouncilKernelTournamentFighterState' in models),
      ('advance contract', 'AdvanceKernelTournamentAsync' in interface),
      ('deterministic hash', 'SHA256.HashData' in service),
      ('engine HP mutation', 'activeRight.Health = Math.Max' in service and 'activeLeft.Health = Math.Max' in service),
      ('engine guard', 'GuardReduction' in service),
      ('engine recovery', 'RecoveryAmount' in service),
      ('engine bracket advancement', 'TournamentNextRoundFighterIds.Add' in service),
      ('engine champion', 'TournamentChampionFighterId' in service),
      ('engine frames', 'AnimationFrames = frames.Select' in service),
      ('engine subtitle', 'AnimationSubtitleHoldMilliseconds = session.TournamentRules.SubtitleHoldMilliseconds' in service),
      ('rules copied precedence', 'OrderBy(' in rules and 'IsSystemSeed ? 1 : 0' in rules),
      ('workflow bridge', 'RunKernelTournamentSystemStepAsync' in orchestration and 'AdvanceKernelTournamentAsync' in bridge),
      ('one-shot game sequence', "state.sequenceOneShot = target === 'game'" in js and 'state.sequenceFrames.length - 1' in js),
      ('subtitle surface', 'data-game-animation-subtitle' in console and 'sequenceSubtitleHold' in console),
      ('screen styling preserved', '.chat-game-screen,\n.chat-game-console-placeholder pre {' in console_css),
      ('subtitle overlay isolated', '.chat-game-animation-subtitle {' in console_css and 'position: relative;' in console_css),
      ('one-shot focus guard', 'state.sequenceOneShot && state.sequenceIndex >= state.sequenceFrames.length' in js),
      ('subtitle timer detached', 'cancelSequenceSubtitleTimer(state);' in js[js.find('detach(id)'):js.find('refreshLayout(id)')]),
    ]
    failed=[name for name,ok in checks if not ok]
    if failed: raise SystemExit(f"Kernel Creature Tournament audit failed: {len(failed)}/{len(checks)} checks failed: {', '.join(failed)}")
    print(f"Kernel Creature Tournament audit passed: {len(checks)} checks.")
    return 0
if __name__=='__main__': sys.exit(main())
