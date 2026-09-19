#!/usr/bin/env python3
from pathlib import Path
import re, sys
ROOT=Path(__file__).resolve().parents[1]
SERVICES=ROOT/'src/LocalGPT/Services'

def main():
    files=list(SERVICES.glob('OrganicCouncilBlueprintSeedDataService*.cs'))
    texts={p.name:p.read_text(encoding='utf-8') for p in files}
    joined='\n'.join(texts.values())
    expected={
      'kernel-creature-tournament','ascii-doom-council-adventure','green-dragon-runtime-story',
      'game-director-runtime','ascii-hot-seat-showcase','adaptive-model-benchmark','initial-setup-assistant'
    }
    found=set()
    for key in expected:
      located=False
      for text in texts.values():
        marker=f'Key = "{key}"'
        pos=text.find(marker)
        if pos < 0: continue
        located=True
        workflow=text.find('WorkflowSteps',pos)
        return_end=text.find('return new OrganicCouncilTeamDefinition',pos+1)
        candidates=[x for x in (workflow,return_end,len(text)) if x>=0]
        stop=min(candidates) if candidates else len(text)
        # PreferredCapabilities always belongs to the team header and precedes workflow in these maintained templates.
        preferred=text.find('PreferredCapabilities',pos,stop if stop>pos else len(text))
        if preferred < 0:
          # Some main seed teams place capabilities after WorkflowSteps; inspect through the next top-level team boundary instead.
          next_team=text.find('\n        new()\n        {\n            Key = ',pos+1)
          stop=next_team if next_team>=0 else len(text)
        else:
          stop=max(stop, preferred+1200)
        block=text[pos:stop]
        if 'CouncilContextCapabilities.RoleIsolated' in block: found.add(key)
        break
      if not located: raise SystemExit(f'Council role-context isolation audit failed: missing maintained preset {key}')
    if found != expected:
      raise SystemExit(f'Council role-context isolation audit failed: expected {sorted(expected)}, found {sorted(found)}')
    context= (SERVICES/'MultiModelCouncilService.ContextIsolation.cs').read_text(encoding='utf-8')
    orchestration=(SERVICES/'MultiModelCouncilService.RunOrchestration.cs').read_text(encoding='utf-8')
    prompting=(SERVICES/'MultiModelCouncilService.WorkflowPrompting.cs').read_text(encoding='utf-8')
    capability=(ROOT/'src/LocalGPT/BusinessObjects/CouncilContextCapabilities.cs').read_text(encoding='utf-8')
    checks=[
      'localgpt.council.context.role-isolated' in capability,
      'public sealed class CouncilContextCapabilities' in capability,
      'BuildRoleIsolatedBootstrap' in context,
      'Minecraft' in context and 'Markdown' in context,
      'UsesRoleIsolatedContext(organicTeam)' in orchestration,
      'if (!roleIsolatedContext)' in orchestration,
      'projectBriefing' in orchestration,
      'request.ExternalProjectContextJson' in prompting,
      'UsesRoleIsolatedContext(team) ? string.Empty' in prompting,
    ]
    if not all(checks): raise SystemExit('Council role-context isolation audit failed: boundary wiring incomplete')
    broad_keys={
      'general','general-project','embedded-firmware-wiring','openscad-team','spreadsheet-team','learning-round',
      'game-project-discovery','game-project-development','game-engine-extension-development','game-project-playtest'
    }
    broad_count=sum(1 for key in broad_keys if f'Key = "{key}"' in joined)
    print(f'Council role-context isolation audit passed: {len(found)} isolated maintained presets; broad-context presets remain available ({broad_count} detected).')
    return 0
if __name__=='__main__': sys.exit(main())
