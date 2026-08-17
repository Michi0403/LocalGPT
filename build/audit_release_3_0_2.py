#!/usr/bin/env python3
from pathlib import Path
import json,re,sys
ROOT=Path(__file__).resolve().parents[1]
fail=[]; checks=0

def require(cond,msg):
    global checks; checks+=1
    if not cond: fail.append(msg)

def text(rel): return (ROOT/rel).read_text(encoding='utf-8',errors='replace')

for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj']:
    require('<Version>3.0.3</Version>' in text(rel),f'{rel}: version is not 3.0.2')

# Windows-build compile repair.
structured=text('src/LocalGPT/Controller/StructuredTextController.cs')
require('using LocalGPT.Services;' in structured,'StructuredTextController does not import LocalGPT.Services')
require('CouncilTextService councilText' in structured,'StructuredTextController lost CouncilTextService dependency')

program='\n'.join(text(p.relative_to(ROOT).as_posix()) for p in sorted((ROOT/'src/LocalGPT').glob('Program*.cs')))
for pat in ['AddScoped<ControllerRequestLoggingFilter>','Filters.AddService<ControllerRequestLoggingFilter>','AddScoped<INotificationService','AddHostedService<DatabaseInitializationHostedService>','AddSingleton<CircuitHandler, LocalGptCircuitDiagnosticsHandler>','AddInteractiveServerComponents()','AddInteractiveServerRenderMode()']:
    require(pat in program,f'Program partial set missing {pat}')

chat_files=sorted((ROOT/'src/LocalGPT/Components/Pages').glob('Chat*'))
chat='\n'.join(p.read_text(encoding='utf-8',errors='replace') for p in chat_files if p.suffix in {'.cs','.razor'} or p.name.endswith('.razor.cs'))
for pat in [r'interactiveAttached\s*=\s*true',r'StartInitialModelRefresh\(\)',r'StartAutoSaveLoop\(\)',r'catch\s*\(Exception\s+ex\)',r'Logger\.Log',r'Notifier\.Show']:
    require(re.search(pat,chat) is not None,f'Chat partial set missing diagnostics pattern {pat}')

op=text('build/Assert-OperationalDiagnostics.ps1')
require('Require-TextAcrossFiles' in op and '$programDiagnosticsFiles' in op and '$chatDiagnosticsFiles' in op,'Operational diagnostics guard is not partial-aware')
render=text('build/Assert-InteractiveServerRenderModes.ps1')
require("-Filter 'Program*.cs'" in render and '$programPaths' in render,'InteractiveServer guard is not partial-aware for Program')
iterator=text('build/Assert-IteratorExceptionPolicy.ps1')
sysvar=text('build/Assert-SystemVariableInitialization.ps1')
require('Get-BaselineRelativePath' in iterator and '${baselineRelative}' in iterator,'Iterator guard does not normalize partial filenames to baseline owner')
require('Get-BaselineRelativePath' in sysvar and '${baselineRelative}' in sysvar,'System-variable guard does not normalize partial filenames to baseline owner')

# Confirm every Windows-build-reported baseline item maps back to the maintained pre-split baseline.
iter_base=set(json.loads(text('build/iterator-exception-baseline.json')))
for item in [
'src/LocalGPT/Services/CouncilRuntimeService.cs|public IEnumerable<FileInfo> EnumerateUsefulFiles(string directory, ILogger logger)|iterator contains catch',
'src/LocalGPT/Services/DxAiFunctionCatalogService.cs|private IEnumerable<DxAiFunctionCatalogEntry> DiscoverPublicServiceMethods()|iterator requires logging',
'src/LocalGPT/Services/DxAiFunctionCatalogService.cs|private IEnumerable<DxAiFunctionCatalogEntry> DiscoverPublicServiceMethods()|iterator requires try/finally',
'src/LocalGPT/Services/ProjectMaintenanceService.cs|private IEnumerable<string> EnumerateFilesSafe(string root, ICollection<string> warnings)|iterator contains catch',
'src/LocalGPT/Services/ProjectMaintenanceService.cs|private IEnumerable<string> EnumerateFilesSafe(string root, ICollection<string> warnings)|iterator requires logging',
'src/LocalGPT/Services/ProjectMaintenanceService.cs|private IEnumerable<string> EnumerateFilesSafe(string root, ICollection<string> warnings)|iterator requires try/finally']:
    require(item in iter_base,f'iterator baseline lost expected historical item: {item}')
sv_base=set(json.loads(text('build/system-variable-initialization-baseline.json')))
for prefix in ['src/LocalGPT/Services/CouncilTextService.cs|details = new GeneratedAiHostModelDetails("gguf", "generated", "0B", "none"),','src/LocalGPT/Services/CouncilTextService.cs|var type = new CodeTypeDeclaration("CouncilFeatureRequestExample")','src/LocalGPT/Services/MultiModelCouncilService.cs|messages.Add(new ChatMessage(ChatRole.User, $"""','src/LocalGPT/Services/OllamaThinkingChatClient.cs|using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat")']:
    require(prefix in sv_base,f'system-variable baseline lost expected historical item: {prefix}')

# New extracted service wiring and exposure boundaries.
reg=text('src/LocalGPT/Program.ServiceRegistration.cs')
for token in ['AddSingleton<CouncilKnowledgeContentService>()','AddSingleton<IJsonTextService, JsonTextService>()','AddSingleton<MinecraftDatapackService>()','AddSingleton<MinecraftProjectService>()','AddSingleton<IRegexCompilationService, RegexCompilationService>()','AddSingleton<IProviderModelReviewerPolicyService, ProviderModelReviewerPolicyService>()']:
    require(token in reg,f'missing DI registration: {token}')
mc_controller=text('src/LocalGPT/Controller/MinecraftDiagnosticController.cs')
for token in ['MinecraftProjectService projectService','MinecraftDatapackService datapackService','ResolveMinecraftDependencyVersionInfo','MinecraftDatapackVersionInfoResolve']:
    require(token in mc_controller,f'Minecraft controller wiring missing: {token}')
regex_dx=text('src/LocalGPT/Services/RegexDxAiFunctions.cs')
require('localgpt.regex.list' in regex_dx and 'localgpt.regex.upsert' in regex_dx,'Regex owning service lost seeded DXFunctions')
bench_dx=text('src/LocalGPT/Services/ProviderModelBenchmarkDxAiFunction.cs')
require('IProviderModelBenchmarkService benchmarks' in bench_dx,'Benchmark owning service lost DXFunction')
knowledge_dx=text('src/LocalGPT/Services/DxAiFunctionRegistry.cs')
require('Council knowledge' in knowledge_dx or 'council knowledge' in knowledge_dx.lower(),'Council knowledge owning service lost DXFunction coverage')

mc_dx=text('src/LocalGPT/Services/MinecraftDxAiFunctions.cs')
for token in ['minecraft.datapack.version.resolve','minecraft.dependency.version.resolve','IDxAiFunctionHandler','SupportsAutomaticInvocation: true','MinecraftDatapackService datapacks','MinecraftProjectService projects']:
    require(token in mc_dx,f'Minecraft DXFunction wiring missing: {token}')
require("typeof(IDxAiFunctionHandler).IsAssignableFrom" in reg,'DI handler discovery missing; Minecraft DXFunctions would not be registered')
cat=text('src/LocalGPT/Services/DxAiFunctionCatalogService.QueriesAndDiscovery.cs')
require('registry.GetFunctions().Select(CreateDxEntry)' in cat and 'IsSystemSeed = true' in cat,'DXFunction catalog no longer discovers/persists DI handler descriptors as system seeds')

# Internal extracted services must stay behind owners rather than gain duplicate direct controllers.
controllers='\n'.join(p.read_text(encoding='utf-8',errors='replace') for p in (ROOT/'src/LocalGPT/Controller').glob('*.cs'))
for internal in ['JsonTextService','RegexCompilationService','ProviderModelReviewerPolicyService','CouncilKnowledgeContentService']:
    require(internal not in controllers,f'internal subservice leaked directly into controller layer: {internal}')

# Namespace cleanliness.
prod=[]
for p in (ROOT/'src/LocalGPT').rglob('*'):
    if p.is_file() and p.suffix in {'.cs','.razor'} and 'wwwroot' not in p.parts:
        prod.append(p.read_text(encoding='utf-8',errors='replace'))
prod='\n'.join(prod)
require('namespace TacosPortal' not in prod,'TacosPortal namespace remains in productive source')
require('namespace LocalGPT.Endpoints' not in prod,'LocalGPT.Endpoints namespace remains in productive source')

if fail:
    print('LocalGPT 3.0.2 source audit failed:')
    for f in fail: print(' -',f)
    sys.exit(1)
print(f'LocalGPT 3.0.2 Windows-build/wiring source audit passed: {checks} checks.')
