#!/usr/bin/env python3
from pathlib import Path
import hashlib, sys
ROOT=Path(__file__).resolve().parents[1]; VERSION='4.5.1'
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
 req('RELEASE.md','# LocalGPT 4.5.1'); req('VALIDATION.md','# LocalGPT 4.5.1 source validation')
 req('CHANGELOG-v4.5.1-ASCII-COLOR-BUILD-COMPILE-REPAIR.md','# LocalGPT 4.5.1')
 req('VALIDATION-v4.5.1-source.md','# LocalGPT 4.5.1 source validation')
 req('src/LocalGPT/Components/App.razor','localgpt-game-console.js?v=4.5.1')
 req('src/LocalGPT/Components/App.razor','localgpt-chat-ui.js?v=4.5.1')
 req('src/LocalGPT/Services/InitialSetupAssistantService.cs','LocalGPT/4.5.1')
 req('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs','LocalGPT/4.5.1')
 req('docs/pdf-cover.html','LocalGPT 4.5.1 Documentation')
 req('docs/reference/ai-provider-installation.md','LocalGPT/4.5.1')
 req('src/LocalGPT/Services/CouncilTeamConfigurationService.cs','private const int CurrentSeedVersion = 33;')
 req('src/LocalGPT/Services/CouncilRuntimeClassService.cs','private const int CurrentSeedVersion = 7;')
 req('src/LocalGPT/Migrations/20260919133000_AddGameProjectAsciiColor.cs','Migration("20260919133000_AddGameProjectAsciiColor")')
 missing=[]; routed=0; rendered=0
 all_rendered=[]
 for p in (ROOT/'src/LocalGPT/Components').rglob('*.razor'):
  t=p.read_text(encoding='utf-8',errors='ignore')
  if '@rendermode InteractiveServer' in t:
   all_rendered.append(str(p.relative_to(ROOT)))
 for p in (ROOT/'src/LocalGPT/Components/Pages').rglob('*.razor'):
  t=p.read_text(encoding='utf-8',errors='ignore')
  if '@page ' in t:
   routed += 1
   if '@rendermode InteractiveServer' in t: rendered += 1
   elif p.name!='Error.razor': missing.append(str(p.relative_to(ROOT)))
 if missing: raise SystemExit(f'LocalGPT {VERSION} audit failed: InteractiveServer missing: {missing[0]}')
 if rendered != 14: raise SystemExit(f'LocalGPT {VERSION} audit failed: expected 14 InteractiveServer routed pages, found {rendered}')
 if len(all_rendered) != 15: raise SystemExit(f'LocalGPT {VERSION} audit failed: expected 15 total InteractiveServer declarations, found {len(all_rendered)}')
 js=read('src/LocalGPT/wwwroot/js/localgpt-game-console.js').replace('\r\n','\n').replace('\r','\n')
 digest=hashlib.sha256(js.encode()).hexdigest()
 if f'{digest}  src/LocalGPT/wwwroot/js/localgpt-game-console.js' not in read('build/javascript-diagnostics-files.sha256'):
  raise SystemExit(f'LocalGPT {VERSION} audit failed: JS diagnostics digest stale')
 print(f'LocalGPT {VERSION} release audit passed: version policy, release metadata, {rendered} routed/{len(all_rendered)} total InteractiveServer declarations, seed versions, migration and JavaScript diagnostics manifest are consistent.')
 return 0
if __name__=='__main__': sys.exit(main())
