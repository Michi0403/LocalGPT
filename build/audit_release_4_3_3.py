from pathlib import Path
import json, re, sys, xml.etree.ElementTree as ET, zipfile
root=Path(__file__).resolve().parents[1]; failures=[]; version='4.3.3'
def text(rel):
 p=root/rel
 if not p.is_file(): failures.append(f'missing {rel}'); return ''
 return p.read_text(encoding='utf-8-sig')
def need(rel,token):
 if token not in text(rel): failures.append(f'{rel}: missing {token!r}')
for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj']:
 t=text(rel); need(rel,f'<Version>{version}</Version>'); m=re.search(r'<Version>(\d+)\.(\d+)\.(\d+)</Version>',t)
 if not m or int(m.group(2))>9 or int(m.group(3))>9: failures.append(f'{rel}: invalid one-digit release version')
for rel,tok in [('docs/index.md','**Version 4.3.3**'),('docs/pdf/toc.yml','LocalGPT-4.3.3.pdf'),('docs/docfx.json','"localgptVersion": "4.3.3"'),('RELEASE.md','# LocalGPT 4.3.3'),('CHANGELOG-v4.3.3-DOCUMENTATION-ASCII-GAME-SESSION-RECOVERY.md','LocalGPT 4.3.3'),('VALIDATION-v4.3.3-source.md','# LocalGPT 4.3.3 source validation')]: need(rel,tok)
targets=text('Directory.Build.targets')
if '<RequireLocalGptDocumentationPdf Condition="\'$(RequireLocalGptDocumentationPdf)\' == \'\'">true</RequireLocalGptDocumentationPdf>' not in targets: failures.append('PDF is not required by default')
js=text('docs/templates/localgpt/public/main.js'); css=text('docs/templates/localgpt/public/main.css')
for tok in ['const starCount = compact ? 48 : 112;','const nebulaCount = compact ? 3 : 5;','mermaid.render(renderId, item.source)','scheduleMermaidRecovery();','mermaid.core-PFJTYFYY.min.js']:
 if tok not in js: failures.append(f'JS missing {tok}')
if 'offsetParent' in js: failures.append('Mermaid recovery still depends on offsetParent')
for tok in ['4.3.3: starfield stacking, non-tiled fallback sky, glass and Mermaid recovery.','html.localgpt-kawaii-docs body::before','backdrop-filter:blur(22px) saturate(1.18)','--kawaii-docs-viewport-gutter:clamp(1.75rem,2.8vw,3.75rem);','.localgpt-mermaid-diagram']:
 if tok not in css: failures.append(f'CSS missing {tok}')
for rel in ['docs/index.md','docs/architecture/index.md']:
 if '```mermaid' not in text(rel): failures.append(f'{rel}: Mermaid fence missing')
cssb=(root/'docs/templates/localgpt/public/main.css').read_bytes(); jsb=(root/'docs/templates/localgpt/public/main.js').read_bytes()
for rel,expected in [('src/LocalGPT/wwwroot/help-docs/public/main.css',cssb),('src/LocalGPT/wwwroot/help-docs/public/main.js',jsb),('src/LocalGPT/wwwroot/help-docs/styles/localgpt-kawaii.css',cssb),('src/LocalGPT/wwwroot/help-docs/styles/localgpt-kawaii.js',jsb)]:
 if not (root/rel).is_file() or (root/rel).read_bytes()!=expected: failures.append(f'asset parity failed {rel}')
with zipfile.ZipFile(root/'.github/pages/localgpt-kawaii-docs.zip') as z:
 if z.read('public/main.css')!=cssb or z.read('styles/localgpt-kawaii.css')!=cssb: failures.append('Pages CSS parity failed')
 if z.read('public/main.js')!=jsb or z.read('styles/localgpt-kawaii.js')!=jsb: failures.append('Pages JS parity failed')
expected_modes={'src/LocalGPT/Components/InteractiveStartupMarker.razor','src/LocalGPT/Components/Pages/TestLab.razor','src/LocalGPT/Components/Pages/RemoteControl.razor','src/LocalGPT/Components/Pages/Index.razor','src/LocalGPT/Components/Pages/Database.razor','src/LocalGPT/Components/Pages/Help.razor','src/LocalGPT/Components/Pages/OneWireSecurity.razor','src/LocalGPT/Components/Pages/CouncilTeams.razor','src/LocalGPT/Components/Pages/ModelCouncil.razor','src/LocalGPT/Components/Pages/ProjectMaintenance.razor','src/LocalGPT/Components/Pages/MinecraftModBuilder.razor','src/LocalGPT/Components/Pages/Projects.razor','src/LocalGPT/Components/Pages/DxFunctionCatalog.razor','src/LocalGPT/Components/Pages/Chat.razor','src/LocalGPT/Components/Pages/Install.razor','src/LocalGPT/Components/Layout/CouncilSpoolerPanel.razor','src/LocalGPT/Components/Layout/ToastWrapper.razor','src/LocalGPT/Components/Layout/HumanCollaborationInbox.razor','src/LocalGPT/Components/Layout/NavMenu.razor','src/LocalGPT/Components/Layout/MenuIsland.razor'}
actual={p.relative_to(root).as_posix() for p in (root/'src').rglob('*.razor') if '@rendermode' in p.read_text(encoding='utf-8-sig')}
if actual!=expected_modes: failures.append(f'render-mode boundary set changed: {sorted(actual^expected_modes)}')
if '@rendermode' in text('src/LocalGPT/Components/Pages/Error.razor'): failures.append('Error page is no longer static')
game=text('src/LocalGPT/Services/CouncilGameDxAiFunctions.cs'); console=text('src/LocalGPT/Components/Shared/ChatGameConsole.razor'); autoplay=text('src/LocalGPT/Services/CouncilGameSessionService.Autoplay.cs')
for tok in ['localgpt.game.session.close','AutoplayEnabled = mode == CouncilGameControlMode.Ai','GetActiveAsync(request.ConversationId']:
 if tok not in game: failures.append(f'game functions missing {tok}')
for tok in ['ControlMode = CouncilGameControlMode.Shared','await InvokeAsync(() => CloseRequested.InvokeAsync())','await Games.EndAsync(snapshot.Id, "Human User")']:
 if tok not in console: failures.append(f'game console missing {tok}')
if 'ControlMode != CouncilGameControlMode.Ai' not in autoplay: failures.append('autoplay is not restricted to AI mode')
for rel in ['Directory.Build.targets','src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj']:
 try: ET.parse(root/rel)
 except Exception as e: failures.append(f'{rel}: XML parse failed: {e}')
try: json.loads(text('docs/docfx.json'))
except Exception as e: failures.append(f'docfx JSON failed: {e}')
if failures:
 print('LocalGPT 4.3.3 source audit failed:'); [print('-',f) for f in failures]; sys.exit(1)
print('LocalGPT 4.3.3 source audit passed.')
