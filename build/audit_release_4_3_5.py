from pathlib import Path
import json, re, sys, xml.etree.ElementTree as ET, zipfile

root = Path(__file__).resolve().parents[1]
failures = []
version = '4.3.5'

def text(rel):
    path = root / rel
    if not path.is_file():
        failures.append(f'missing {rel}')
        return ''
    return path.read_text(encoding='utf-8-sig')

def need(rel, token):
    if token not in text(rel):
        failures.append(f'{rel}: missing {token!r}')

for rel in [
    'src/LocalGPT/LocalGPT.csproj',
    'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
    'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
]:
    content = text(rel)
    need(rel, f'<Version>{version}</Version>')
    match = re.search(r'<Version>(\d+)\.(\d+)\.(\d+)</Version>', content)
    if not match or int(match.group(2)) > 9 or int(match.group(3)) > 9:
        failures.append(f'{rel}: invalid one-digit release version')

for rel, token in [
    ('docs/index.md', '**Version 4.3.5**'),
    ('docs/pdf/toc.yml', 'LocalGPT-4.3.5.pdf'),
    ('docs/docfx.json', '"localgptVersion": "4.3.5"'),
    ('RELEASE.md', '# LocalGPT 4.3.5'),
    ('CHANGELOG-v4.3.5-BUILD-STREAMING-STARFIELD-REPAIR.md', 'LocalGPT 4.3.5'),
    ('VALIDATION-v4.3.5-source.md', '# LocalGPT 4.3.5 source validation'),
    ('src/LocalGPT/Components/App.razor', 'localgpt-game-console.js?v=4.3.5'),
    ('src/LocalGPT/Components/App.razor', 'localgpt-chat-ui.js?v=4.3.5'),
]:
    need(rel, token)

if '<RequireLocalGptDocumentationPdf Condition="\'$(RequireLocalGptDocumentationPdf)\' == \'\'">true</RequireLocalGptDocumentationPdf>' not in text('Directory.Build.targets'):
    failures.append('PDF is not required by default')

js = text('docs/templates/localgpt/public/main.js')
css = text('docs/templates/localgpt/public/main.css')
for token in [
    'const starCount = compact ? 48 : 112;',
    'const satelliteCount = compact ? 1 : 2;',
    'document.documentElement.dataset.localgptDynamicSky = "ready";',
    'await mermaid.run({ nodes: pendingBlocks, suppressErrors: false });',
    'flowchart: { htmlLabels: false }',
    'const retryDelays = [0, 300, 900, 1800];',
    'mermaid.core-PFJTYFYY.min.js',
]:
    if token not in js:
        failures.append(f'JS missing {token}')
for forbidden in ['mermaid.render(', 'offsetParent', 'const nebulaCount =']:
    if forbidden in js:
        failures.append(f'JS still contains superseded path {forbidden}')
for token in [
    '4.3.5: artifact-free star sky. Keep glass through alpha surfaces, not GPU backdrop tiles.',
    '.localgpt-kawaii-sky {',
    'contain: strict !important;',
    '.localgpt-kawaii-nebula { display: none !important; filter: none !important; }',
    'data-localgpt-dynamic-sky="ready"',
    'backdrop-filter: none !important;',
    '--kawaii-docs-viewport-gutter:clamp(1.75rem,2.8vw,3.75rem);',
]:
    if token not in css:
        failures.append(f'CSS missing {token}')

for rel in ['docs/index.md', 'docs/architecture/index.md']:
    content = text(rel)
    if '<div class="mermaid localgpt-mermaid-diagram">' not in content or 'flowchart ' not in content:
        failures.append(f'{rel}: attached Mermaid source block missing')
    if '```mermaid' in content:
        failures.append(f'{rel}: legacy Mermaid fence remains')

# Documentation theme parity across authored, shipped and Pages copies.
css_bytes = (root / 'docs/templates/localgpt/public/main.css').read_bytes()
js_bytes = (root / 'docs/templates/localgpt/public/main.js').read_bytes()
for rel, expected in [
    ('src/LocalGPT/wwwroot/help-docs/public/main.css', css_bytes),
    ('src/LocalGPT/wwwroot/help-docs/public/main.js', js_bytes),
    ('src/LocalGPT/wwwroot/help-docs/styles/localgpt-kawaii.css', css_bytes),
    ('src/LocalGPT/wwwroot/help-docs/styles/localgpt-kawaii.js', js_bytes),
]:
    path = root / rel
    if not path.is_file() or path.read_bytes() != expected:
        failures.append(f'asset parity failed {rel}')
with zipfile.ZipFile(root / '.github/pages/localgpt-kawaii-docs.zip') as archive:
    for name, expected in [
        ('public/main.css', css_bytes),
        ('styles/localgpt-kawaii.css', css_bytes),
        ('public/main.js', js_bytes),
        ('styles/localgpt-kawaii.js', js_bytes),
    ]:
        if name not in archive.namelist() or archive.read(name) != expected:
            failures.append(f'Pages asset parity failed {name}')
    if archive.testzip() is not None:
        failures.append('Pages archive integrity failed')

# Established InteractiveServer owner set is deliberate; child components inherit it.
expected_modes = {
    'src/LocalGPT/Components/InteractiveStartupMarker.razor',
    'src/LocalGPT/Components/Pages/TestLab.razor',
    'src/LocalGPT/Components/Pages/RemoteControl.razor',
    'src/LocalGPT/Components/Pages/Index.razor',
    'src/LocalGPT/Components/Pages/Database.razor',
    'src/LocalGPT/Components/Pages/Help.razor',
    'src/LocalGPT/Components/Pages/OneWireSecurity.razor',
    'src/LocalGPT/Components/Pages/CouncilTeams.razor',
    'src/LocalGPT/Components/Pages/ModelCouncil.razor',
    'src/LocalGPT/Components/Pages/ProjectMaintenance.razor',
    'src/LocalGPT/Components/Pages/MinecraftModBuilder.razor',
    'src/LocalGPT/Components/Pages/Projects.razor',
    'src/LocalGPT/Components/Pages/DxFunctionCatalog.razor',
    'src/LocalGPT/Components/Pages/Chat.razor',
    'src/LocalGPT/Components/Pages/Install.razor',
    'src/LocalGPT/Components/Layout/CouncilSpoolerPanel.razor',
    'src/LocalGPT/Components/Layout/ToastWrapper.razor',
    'src/LocalGPT/Components/Layout/HumanCollaborationInbox.razor',
    'src/LocalGPT/Components/Layout/NavMenu.razor',
    'src/LocalGPT/Components/Layout/MenuIsland.razor',
}
actual_modes = {
    path.relative_to(root).as_posix()
    for path in (root / 'src').rglob('*.razor')
    if '@rendermode' in path.read_text(encoding='utf-8-sig')
}
if actual_modes != expected_modes:
    failures.append(f'render-mode boundary set changed: {sorted(actual_modes ^ expected_modes)}')
if '@rendermode' in text('src/LocalGPT/Components/Pages/Error.razor'):
    failures.append('Error page is no longer static')

# Live-test regressions retained from 4.3.4 and maintenance repairs added in 4.3.5.
checks = {
    'src/LocalGPT/Components/Pages/Chat.razor.cs': [
        'readonly List<BlazorChatMessage> canonicalConversationMessages = [];',
        'Toggles the shared ASCII terminal while preserving the canonical DXAiChat conversation as the single source of truth.',
    ],
    'src/LocalGPT/Components/Pages/Chat.PersistenceAndMemory.razor.cs': [
        'ReuseContextWhenSwitching && canonicalConversationMessages.Count > 0',
    ],
    'src/LocalGPT/Components/Pages/Chat.razor': [
        'Contextual ASCII fun: ON',
        'Contextual ASCII fun: OFF',
        'ApplyReactiveAsciiGameplayPresetAsync',
        'Use Reactive ASCII Gameplay preset',
    ],
    'src/LocalGPT/Services/AiProviderConfigurationRegistryService.cs': [
        'explicitPrimary',
        'draftPrimaryEndpoint',
        'persistedPrimaryEndpoint',
        'registry.ContainsKey(draftPrimaryEndpoint)',
    ],
    'src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.AsciiHotSeatTemplate.cs': [
        'DistinctAiAssignmentGroup = "ascii-hot-seat-runtime"',
        'AllowDistinctAiAssignmentFallback = true',
    ],
    'src/LocalGPT/Services/MultiModelCouncilService.RoleSelection.cs': [
        'exhausted preferred distinct group',
    ],
    'src/LocalGPT/BusinessObjects/CouncilGameModels.cs': [
        'MapSeed',
        'ScenarioPrompt',
    ],
    'src/LocalGPT/Services/CouncilGameDxAiFunctions.cs': [
        'mapSeed',
        'scenarioPrompt',
        'localgpt.game.session.close',
    ],
    'src/LocalGPT/Components/Shared/ChatGameConsole.razor': [
        'Restart same map',
        'New map',
        'Start scenario',
        'Player 1 Hot Seat',
        'Player 2 Hot Seat',
        'HumanCollaboration',
    ],
    'src/LocalGPT/Components/Shared/ChatGameConsole.razor.css': [
        '.chat-game-screen-viewport',
    ],
}
for rel, tokens in checks.items():
    content = text(rel)
    for token in tokens:
        if token not in content:
            failures.append(f'{rel}: missing {token!r}')

# 4.3.5 compile/ownership/streaming maintenance assertions.
chat_razor = text('src/LocalGPT/Components/Pages/Chat.razor')
chat_code = text('src/LocalGPT/Components/Pages/Chat.razor.cs')
game = text('src/LocalGPT/Components/Shared/ChatGameConsole.razor')
ollama = text('src/LocalGPT/Services/OllamaThinkingChatClient.cs')
if 'CssClass="@AsciiFunButtonCssClass"' not in chat_razor or 'localgpt-rounded-action @(AsciiFunModeEnabled ?' in chat_razor:
    failures.append('Contextual ASCII fun button still uses a mixed Razor CssClass expression')
if 'private string AsciiFunButtonCssClass => AsciiFunModeEnabled' not in chat_code:
    failures.append('Contextual ASCII fun CssClass owner property is missing')
for forbidden in [
    "string.Join(' ', scenarioPrompt.Split",
    "string.Join(' ', hotSeatText.Split",
]:
    if forbidden in game:
        failures.append(f'ChatGameConsole still violates text-service ownership: {forbidden}')
for required in [
    'CouncilText.TrimForPrompt(scenarioPrompt, 240, Logger, collapseWhitespace: true)',
    'CouncilText.TrimForPrompt(hotSeatText, 80, Logger, collapseWhitespace: true)',
]:
    if required not in game:
        failures.append(f'ChatGameConsole missing service-owned text normalization: {required}')
for required in [
    'StreamingPresentationBatchCharacters = 96',
    'StreamingPresentationMaxLatencyMilliseconds = 45',
    'pendingVisiblePresentation',
    'pendingThinkingPresentation',
    'presentationFlushClock.ElapsedMilliseconds >= StreamingPresentationMaxLatencyMilliseconds',
]:
    if required not in ollama:
        failures.append(f'Ollama presentation batching missing {required!r}')
for required in [
    'content: none !important;',
    'backdrop-filter: none !important;',
    '.localgpt-kawaii-star { filter: none !important; }',
    'satelliteDx = index % 2 === 0 ? randomBetween(120, 250) : randomBetween(-250, -120);',
]:
    if required not in (css if 'satelliteDx' not in required else js):
        failures.append(f'artifact-free sky assertion missing {required!r}')

# Primary precedence must be explicit edit -> detached draft -> persisted fallback.
primary = text('src/LocalGPT/Services/AiProviderConfigurationRegistryService.cs')
pos_explicit = primary.find('if (!string.IsNullOrWhiteSpace(explicitPrimary)')
pos_draft = primary.find('else if (!string.IsNullOrWhiteSpace(draftPrimaryEndpoint)')
pos_persisted = primary.find('else if (!string.IsNullOrWhiteSpace(persistedPrimaryEndpoint)')
if min(pos_explicit, pos_draft, pos_persisted) < 0 or not (pos_explicit < pos_draft < pos_persisted):
    failures.append('Ollama primary precedence is not explicit edit -> draft -> persisted fallback')

for rel in [
    'Directory.Build.targets',
    'src/LocalGPT/LocalGPT.csproj',
    'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
    'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
]:
    try:
        ET.parse(root / rel)
    except Exception as exc:
        failures.append(f'{rel}: XML parse failed: {exc}')
try:
    json.loads(text('docs/docfx.json'))
except Exception as exc:
    failures.append(f'docs/docfx.json: JSON parse failed: {exc}')

if failures:
    print('LocalGPT 4.3.5 source audit failed:')
    for failure in failures:
        print('-', failure)
    sys.exit(1)
print('LocalGPT 4.3.5 source audit passed.')
