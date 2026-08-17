#!/usr/bin/env python3
from pathlib import Path
import re, sys
ROOT=Path(__file__).resolve().parents[1]
fail=[]; checks=0

def require(cond,msg):
    global checks
    checks += 1
    if not cond: fail.append(msg)

def text(rel):
    return (ROOT/rel).read_text(encoding='utf-8',errors='replace')

# Version / preserved transport.
for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj']:
    require('<Version>3.0.7</Version>' in text(rel), f'{rel}: version is not 3.0.5')
require('2.1.1' in text('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj'),'Wire protocol changed unexpectedly')
require('private const int CurrentSeedVersion = 26;' in text('src/LocalGPT/Services/CouncilTeamConfigurationService.cs'),'Council setup team seed version is not 26')
require(not any('3_0_5' in p.name.lower() for p in (ROOT/'src/LocalGPT/Migrations').glob('*.cs')),'3.0.5 unexpectedly introduced an EF migration')

# Unified cross-platform console command engine.
models=text('src/LocalGPT/BusinessObjects/InitialSetupAssistantModels.cs')
console=text('src/LocalGPT/Services/ConsoleCommandService.cs')
console_dx=text('src/LocalGPT/Services/ConsoleCommandDxAiFunctions.cs')
console_controller=text('src/LocalGPT/Controller/ConsoleCommandController.cs')
for token in ['Direct','PowerShell','Bash','Cmd']:
    require(token in models and token in console, f'console shell adapter missing {token}')
for token in ['TimeoutSeconds','Max','RedirectStandardOutput','RedirectStandardError','UseShellExecute = false']:
    require(token in console or token in models, f'bounded console execution contract missing {token}')
require('UserConfirmed' in console and 'IsReadOnly' in console,'console mutating/read-only approval split missing')
require('CommandText' not in ''.join(re.findall(r'Log(?:Information|Warning|Error|Debug)\([^;]+;', console, re.S)),'console command text is written to normal logger')
require('localgpt.console.history' in console_dx and 'localgpt.console.execute' in console_dx,'console DXFunctions incomplete')
execute_block=console_dx.split('public sealed class ConsoleHistoryFunction',1)[0] if 'public sealed class ConsoleHistoryFunction' in console_dx else console_dx
require('RequiresHumanConfirmation: true' in console_dx and 'SupportsAutomaticInvocation: false' in console_dx,'generic console execute is not human-confirmation gated')
require('[Route("api/console")' in console_controller,'console controller missing')
chat_console=text('src/LocalGPT/Components/Shared/ChatGameConsole.razor')
require('IConsoleCommandService' in chat_console and 'ASCII command console' in chat_console,'ASCII console does not consume shared command output')
require('Game' in chat_console or 'game' in chat_console,'ASCII console game precedence is not represented')

# Hardware as multi-host/multi-GPU evidence.
hardware_models=text('src/LocalGPT/BusinessObjects/ConfiguredAiHostHardwareModels.cs')
hardware_service=text('src/LocalGPT/Services/ConfiguredAiHostHardwareService.cs')
setup_service=text('src/LocalGPT/Services/InitialSetupAssistantService.cs')
setup_iface=text('src/LocalGPT/Interfaces/IInitialSetupAssistantService.cs')
for token in ['List<ConfiguredAiHostGpu> Gpus','GpusJson']:
    require(token in hardware_models or token in hardware_service, f'multi-GPU hardware contract missing {token}')
require('Gpus' in hardware_service and 'ImportHwInfo' in hardware_service and 'DetectLocal' in hardware_service,'multi-GPU probe/HWiNFO path missing')
for token in ['Endpoint','HostKey','CanIRunSlug']:
    require(token in models, f'initial setup hardware row missing {token}')
require('SaveHardwareListAsync' in setup_iface and 'GroupBy' in setup_service,'hardware list is not grouped/persisted by host')
require('DeviceEndpoint' in models and 'HostKey' in models,'CanIRun recommendation loses physical host identity')
require('GetHardwareRecommendationsAsync' in setup_iface and 'DeviceEndpoint' in setup_service,'multi-host CanIRun recommendation orchestration missing')

# CanIRun.ai is explicit, attributed, bounded and service-owned.
canirun=text('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs')
initial_data=text('src/LocalGPT/Services/Persistence/InitialDataCatalog.cs')
panel=text('src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor')
for token in ['canirun.ai','www.canirun.ai','https','userConfirmed']:
    require(token.lower() in canirun.lower(), f'CanIRun opt-in/network boundary missing {token}')
require('AllowAutoRedirect = false' in text('src/LocalGPT/Program.ServiceRegistration.cs'),'CanIRun client allows automatic redirects')
require('device/{Uri.EscapeDataString(slug)}/' in canirun,'CanIRun lookup does not use canonical trailing-slash device URL while redirects are disabled')
for token in ['builtin.canirun-model-card-pattern','builtin.html-data-attribute-pattern']:
    require(token in initial_data and token in canirun, f'CanIRun parser is not regex-policy owned: {token}')
require('CanIRun.ai by midudev' in panel,'CanIRun attribution is not visible in setup UI')
require('rx-7900-xtx' not in canirun and 'rtx-3060' not in canirun,'specific user GPU examples were hardcoded into CanIRun service')

# Knowledge-owned provider bootstrap, model listing and endpoint registration.
provider_doc=text('docs/reference/ai-provider-installation.md')
provider_service=text('src/LocalGPT/Services/AiProviderBootstrapService.cs')
seed=text('src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs')
seedcore=text('src/LocalGPT/Services/Persistence/DatabaseInitializationService.SeedCore.cs')
setup_controller=text('src/LocalGPT/Controller/InitialSetupAssistantController.cs')
setup_dx=text('src/LocalGPT/Services/InitialSetupDxAiFunctions.cs')+text('src/LocalGPT/Services/InitialSetupProviderConfigurationDxAiFunction.cs')+text('src/LocalGPT/Services/InitialSetupHardwareDxAiFunctions.cs')
for platform in ['windows','linux','macos']:
    require(platform in provider_doc.lower(), f'provider bootstrap knowledge missing {platform}')
for provider in ['ollama','lm studio']:
    require(provider in provider_doc.lower(), f'provider bootstrap knowledge missing {provider}')
require('openai-compatible' in provider_doc and '127.0.0.1:1234/v1' in provider_doc,'LM Studio profile does not use canonical openai-compatible /v1 identity')
require('ai-provider-installation.md' in seed,'provider bootstrap knowledge document is not in KnowledgeFiles seed')
require('IsPreviousKnowledgeFilesDefault' in seedcore,'existing untouched KnowledgeFiles default is not losslessly upgraded')
for dep in ['IConsoleCommandService','IRegexPatternService','ICouncilKnowledgeService','IJsonTextService']:
    require(dep in provider_service, f'provider bootstrap bypasses maintained service dependency {dep}')
for token in ['DetectAsync','ListModelsAsync','InstallAsync','StartAsync','InstallModelAsync','ConfigureEndpointAsync']:
    require(token in provider_service, f'provider bootstrap service missing {token}')
for route in ['providers/{profileKey}/detect','providers/{profileKey}/models/list','providers/{profileKey}/install','providers/{profileKey}/start','providers/{profileKey}/configure','providers/{profileKey}/models/{modelId}/install']:
    require(route in setup_controller, f'initial setup controller missing route {route}')
for name in ['initial.setup.provider.detect','initial.setup.provider.models.list','initial.setup.provider.install','initial.setup.provider.start','initial.setup.provider.configure','initial.setup.provider.model.install']:
    require(name in setup_dx, f'initial setup provider DXFunction missing {name}')
require('SupportsAutomaticInvocation: false' in setup_dx,'consequential setup actions are not protected from automatic invocation')

# Setup orchestration and hardware-aware benchmark Council.
for name in ['initial.setup.status','initial.setup.hardware.detect','initial.setup.hardware.hwinfo.import','initial.setup.hardware.save','initial.setup.canirun.recommendations','initial.setup.benchmark.team.create']:
    require(name in setup_dx, f'initial setup DXFunction missing {name}')
require('ProviderModelReviewerPolicyService' in setup_service or 'IProviderModelReviewerPolicyService' in setup_service,'setup curator selection does not reuse reviewer policy')
require('ProviderModelIdentity' in setup_service and 'Endpoint' in setup_service and 'ProviderKind' in setup_service,'installed-model matching is not provider+endpoint qualified')
require('adaptive-model-benchmark' in setup_service and 'ICouncilTeamConfigurationService' in setup_service,'hardware benchmark team does not reuse maintained Council team persistence')
require('PreferredCuratorModelKeys' in setup_service and re.search(r'curator|director|reviewer|auditor|analyst|synthesizer', setup_service, re.I),'strong-model role curation missing')

# AI-guided team and existing Human Collaboration choices.
team=text('src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.InitialSetupTemplate.cs')
blueprints=text('src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs')
prompts=text('src/LocalGPT/Services/LocalGptCatalogService.PromptCatalog.cs')
require('Key = "initial-setup-assistant"' in team and 'CreateInitialSetupAssistantTeam' in blueprints,'AI-guided initial setup Council blueprint is not seeded')
for token in ['human.collaboration.request','initial.setup.status','initial.setup.provider.models.list','initial.setup.benchmark.team.create']:
    require(token in team, f'initial setup Council capability missing {token}')
require('CanIRun.ai' in team and 'explicitly opts in' in team,'setup Council does not preserve CanIRun opt-in policy')
require('initial-setup-council-start' in prompts and 'initial-setup-assistant' in prompts,'prompt starter is not wired to setup Council')
require('CanStartAiGuidedSetup' in models and 'AiGuidedSetupRoute' in models and 'installedModels.Count > 0' in setup_service,'AI-guided setup availability does not depend on an available local model')

# Reopenable Install-guide UI, no extra render-mode island.
install=text('src/LocalGPT/Components/Pages/Install.razor')+text('src/LocalGPT/Components/Pages/Install.razor.cs')
require('InitialSetupAssistantPanel' in install,'initial setup assistant is not embedded in Install guide')
require('InstallSectionUserSelected' in install,'first-run guide does not respect explicit user section selection')
require('HWiNFO' in panel and 'List local models' in panel and 'Create / refresh hardware benchmark team' in panel,'setup UI is missing hardware/provider/benchmark workflow')
razor='\n'.join(p.read_text(encoding='utf-8',errors='replace') for p in (ROOT/'src/LocalGPT/Components').rglob('*.razor'))
require(razor.count('@rendermode') == 20,'3.0.5 unexpectedly changed explicit InteractiveServer island count')

# DI/controller/DX discovery architecture.
program=text('src/LocalGPT/Program.ServiceRegistration.cs')
for token in ['IConsoleCommandService','ICanIRunHardwareRecommendationService','IAiProviderBootstrapService','IInitialSetupAssistantService','LocalGPTCanIRun']:
    require(token in program, f'DI registration missing {token}')
require('IDxAiFunctionHandler' in setup_dx and 'IDxAiFunctionHandler' in console_dx,'new setup/console capabilities are not normal DXFunction handlers')

if fail:
    print('LocalGPT 3.0.5 AI-guided setup source audit failed:')
    for item in fail: print(' -',item)
    sys.exit(1)
print(f'LocalGPT 3.0.5 AI-guided setup source audit passed: {checks} checks.')
