#!/usr/bin/env python3
from pathlib import Path
import hashlib, sys
ROOT=Path(__file__).resolve().parents[1]; VERSION='4.4.9'
def read(rel):
 p=ROOT/rel
 if not p.is_file(): raise SystemExit(f'LocalGPT {VERSION} audit failed: missing {rel}')
 return p.read_text(encoding='utf-8')
def req(rel,marker):
 if marker not in read(rel): raise SystemExit(f'LocalGPT {VERSION} audit failed: {rel} missing {marker!r}')
def main():
 parts=[int(x) for x in VERSION.split('.')]
 if len(parts)!=3 or parts[1]>=10 or parts[2]>=10: raise SystemExit('version-slot policy failed')
 for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj']:
  req(rel,f'<Version>{VERSION}</Version>')
 req('RELEASE.md','# LocalGPT 4.4.9'); req('VALIDATION.md','# LocalGPT 4.4.9 source validation')
 req('CHANGELOG-v4.4.9-KERNEL-TOURNAMENT-ENGINE-CONTEXT-ISOLATION.md','# LocalGPT 4.4.9')
 req('VALIDATION-v4.4.9-source.md','# LocalGPT 4.4.9 source validation')
 req('src/LocalGPT/Components/App.razor','localgpt-game-console.js?v=4.4.9')
 req('src/LocalGPT/Components/App.razor','localgpt-chat-ui.js?v=4.4.9')
 req('src/LocalGPT/Services/InitialSetupAssistantService.cs','LocalGPT/4.4.9')
 req('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs','LocalGPT/4.4.9')
 req('docs/pdf-cover.html','LocalGPT 4.4.9 Documentation')
 req('docs/reference/ai-provider-installation.md','LocalGPT/4.4.9')
 req('src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs','localgpt.runtime-class.get')
 req('src/LocalGPT/Services/CouncilTeamConfigurationService.cs','private const int CurrentSeedVersion = 32;')
 req('src/LocalGPT/Services/CouncilRuntimeClassService.cs','private const int CurrentSeedVersion = 6;')
 missing=[]
 for p in (ROOT/'src/LocalGPT/Components/Pages').rglob('*.razor'):
  t=p.read_text(encoding='utf-8',errors='ignore')
  if '@page ' in t and p.name!='Error.razor' and '@rendermode InteractiveServer' not in t: missing.append(str(p.relative_to(ROOT)))
 if missing: raise SystemExit(f'LocalGPT {VERSION} audit failed: InteractiveServer missing: {missing[0]}')
 js=read('src/LocalGPT/wwwroot/js/localgpt-game-console.js').replace('\r\n','\n').replace('\r','\n')
 digest=hashlib.sha256(js.encode()).hexdigest()
 if f'{digest}  src/LocalGPT/wwwroot/js/localgpt-game-console.js' not in read('build/javascript-diagnostics-files.sha256'):
  raise SystemExit(f'LocalGPT {VERSION} audit failed: JS diagnostics digest stale')
 print('LocalGPT 4.4.9 release audit passed: version policy, release metadata, InteractiveServer routes, seed versions and JavaScript diagnostics manifest are consistent.')
 return 0
if __name__=='__main__': sys.exit(main())
