#!/usr/bin/env python3
from pathlib import Path
import json, re, sys
ROOT=Path(__file__).resolve().parents[1]
fail=[]; checks=0

def require(cond,msg):
    global checks; checks += 1
    if not cond: fail.append(msg)

def text(rel):
    return (ROOT/rel).read_text(encoding='utf-8',errors='replace')

# Release versions.
for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj']:
    require('<Version>3.0.7</Version>' in text(rel), f'{rel}: version is not 3.0.4')
require('2.1.1' in text('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj'),'Wire protocol unexpectedly changed')

# Additive persistence: remote-control fabric, user functions, and knowledge-backed toolchain metadata.
migration=text('src/LocalGPT/Migrations/20260817135000_AddRemoteControlIntegrationFabric.cs')
snapshot=text('src/LocalGPT/Migrations/LocalGptMemoryDbContextModelSnapshot.cs')
dbctx=text('src/LocalGPT/BusinessObjects/EFCore/LocalGptMemoryDbContext.cs')
require('[Migration("20260817135000_AddRemoteControlIntegrationFabric")]' in migration,'3.0.4 integration migration id missing')
for table in ['RemoteControlConnectorDefinitions','RemoteControlPipelineDefinitions','RemoteControlExecutionRecords','UserDxAiFunctionDefinitions']:
    require(f'name: "{table}"' in migration, f'migration does not create {table}')
    require(table in snapshot, f'snapshot does not contain {table}')
    require(table in dbctx, f'DbContext does not map {table}')
for col in ['KnowledgeProfileKey','KnowledgeEntryId','VersionKnowledgeEntryId','ToolchainKind','DetectedPlatform']:
    require(f'name: "{col}"' in migration, f'migration missing ProjectCompilerInstallations.{col}')
    require(f'b.Property<string>("{col}")' in snapshot if col in ['KnowledgeProfileKey','ToolchainKind','DetectedPlatform'] else f'b.Property<Guid?>("{col}")' in snapshot, f'snapshot missing ProjectCompilerInstallations.{col}')
require('IX_ProjectCompilerInstallations_KnowledgeProfileKey_Version' in migration,'toolchain knowledge/version index missing')
require('DropIndex(name: "IX_ProjectCompilerInstallations_KnowledgeProfileKey_Version"' in migration,'migration Down does not remove toolchain knowledge/version index first')

# Remote Control: offline-by-default and bounded network policy.
models=text('src/LocalGPT/BusinessObjects/RemoteControlIntegrationModels.cs')
transport=text('src/LocalGPT/Services/RemoteControlTransportService.cs')
connectors=text('src/LocalGPT/Services/RemoteControlConnectorService.cs')
pipelines=text('src/LocalGPT/Services/RemoteControlPipelineService.cs')
pipeline_dx=text('src/LocalGPT/Services/RemoteControlPipelineDxAiFunctions.cs')
connector_dx=text('src/LocalGPT/Services/RemoteControlConnectorDxAiFunctions.cs')
require('public bool IsEnabled { get; set; }' in models and 'public bool IsEnabled { get; set; } = true' not in models,'Remote Control connector is not disabled by default')
require('public bool NetworkEnabled { get; set; }' in models and 'NetworkEnabled { get; set; } = true' not in models,'Remote Control network is not disabled by default')
require('[JsonIgnore]' in models and 'WebhookToken' in models,'webhook token is not excluded from JSON')
for token in ['AllowedHostsJson','AllowInsecureHttp','MaxPayloadBytes','TimeoutSeconds']:
    require(token in models and token in transport, f'bounded transport contract missing {token}')
require('https' in transport.lower() and 'allowed host' in transport.lower(),'HTTPS/host allowlist validation missing')
require('NetworkEnabled' in transport and 'IsEnabled' in transport,'transport does not gate outbound I/O on connector enablement')
require('IDxAiFunctionRegistry' in pipelines,'pipeline execution bypasses the shared DXFunction registry')
require('localgpt.public_service.invoke' in pipelines,'public-service pipeline bridge missing')
require('Source, "UserDxFunction"' in pipelines or '"UserDxFunction"' in pipelines,'pipeline does not guard user-wrapper recursion')
require('System.Reflection' not in pipelines,'pipeline introduced a raw reflection execution path')
for token in ['localgpt.remote_control.connector.list','localgpt.remote_control.connector.save','localgpt.remote_control.connector.delete','localgpt.remote_control.connector.pull','localgpt.remote_control.pipeline.list','localgpt.remote_control.target.list','localgpt.remote_control.pipeline.save','localgpt.remote_control.pipeline.delete','localgpt.remote_control.pipeline.execute']:
    require(token in connector_dx or token in pipeline_dx, f'Remote Control DXFunction missing: {token}')
# AI-facing list handlers must not expose secret-bearing connector templates.
list_block=connector_dx.split('public sealed class SaveRemoteControlConnectorFunction',1)[0]
for secret in ['HeadersJson','RequestBodyTemplate']:
    require(secret not in list_block, f'connector list DXFunction exposes {secret}')
require('WebhookToken = item.WebhookToken' not in list_block and 'item.WebhookToken,' not in list_block, 'connector list DXFunction exposes the webhook token value')
require('WebhookTokenConfigured' in list_block, 'connector list DXFunction does not expose safe webhook-token configured state')

# Remote Control UI/controller/DI and render mode.
remote_razor=text('src/LocalGPT/Components/Pages/RemoteControl.razor')
require(remote_razor.lstrip().startswith('@rendermode InteractiveServer\n@page "/remote-control"'),'Remote Control page does not preserve InteractiveServer as first directive')
program=text('src/LocalGPT/Program.ServiceRegistration.cs')
for token in ['IRemoteControlConnectorService','IRemoteControlPipelineService','IRemoteControlTemplateService','IRemoteControlExecutionStoreService','RemoteControlPollingHostedService','IUserDxAiFunctionService','IToolchainKnowledgeService','IToolchainDiscoveryService']:
    require(token in program, f'DI registration missing {token}')
controller=text('src/LocalGPT/Controller/RemoteControlController.cs')
for token in ['[Route("api/remote-control")]', 'webhook/{key}', 'connectors', 'pipelines']:
    require(token in controller, f'Remote Control controller wiring missing {token}')
razor='\n'.join(p.read_text(encoding='utf-8',errors='replace') for p in (ROOT/'src/LocalGPT/Components').rglob('*.razor'))
require(razor.count('@rendermode') == 20,'explicit InteractiveServer island/page count is not 20')
render_guard=text('build/Assert-InteractiveServerRenderModes.ps1')
require("'Components/Pages/RemoteControl.razor' = '@rendermode InteractiveServer'" in render_guard,'InteractiveServer Windows guard does not protect Remote Control page')

# User-created DXFunctions: persisted, editable, and routed through the SAME registry policy.
user_models=text('src/LocalGPT/BusinessObjects/UserDxAiFunctionModels.cs')
user_service=text('src/LocalGPT/Services/UserDxAiFunctionService.cs')
registry=text('src/LocalGPT/Services/DxAiFunctionRegistry.cs')
catalog=text('src/LocalGPT/Services/DxAiFunctionCatalogService.cs')+text('src/LocalGPT/Services/DxAiFunctionCatalogService.QueriesAndDiscovery.cs')
user_controller=text('src/LocalGPT/Controller/UserDxAiFunctionController.cs')
user_editor=text('src/LocalGPT/Components/Shared/UserDxFunctionEditor.razor')
dx_page=text('src/LocalGPT/Components/Pages/DxFunctionCatalog.razor')
require('class UserDxAiFunctionDefinition' in user_models and 'PipelineKey' in user_models,'user DXFunction persisted definition missing')
require('^user\\.' in user_service or '^user\\\\.' in user_service,'user DXFunction namespace restriction missing')
for token in ['SaveAsync','DeleteAsync','GetDescriptors','TryGetDescriptor','InvokeAsync']:
    require(token in user_service, f'user DXFunction service missing {token}')
require('IUserDxAiFunctionService' in registry,'shared registry is not wired to user DXFunctions')
require('userFunctions.InvokeAsync' in registry,'user DXFunctions do not execute through shared registry')
for token in ['RequiresHumanConfirmation','SupportsAutomaticInvocation','ValidateInvocationParameters']:
    require(token in registry, f'shared DXFunction policy path missing {token}')
require('UserDxFunction' in catalog,'catalog does not preserve user DXFunction source/policy')
require('[Route("api/dxai/user-functions")]' in user_controller and '[HttpPost]' in user_controller and '[HttpDelete' in user_controller,'user DXFunction CRUD controller incomplete')
for token in ['Save user function','Delete user function','PipelineKey','ParameterSchemaJson']:
    require(token in user_editor, f'user DXFunction editor incomplete: {token}')
require('New user DXFunction' in dx_page and 'UserDxFunctionEditor' in dx_page,'DX Functions page cannot create/edit user functions')
# No source handler may reserve the dynamic user.* namespace.
handler_source='\n'.join(p.read_text(encoding='utf-8',errors='replace') for p in (ROOT/'src/LocalGPT/Services').glob('*DxAiFunctions.cs'))
require(re.search(r'DxaichatFunctionInfo[^\n]*\n(?:.|\n){0,250}?"user\.',handler_source) is None,'source-controlled DXFunction handler reserves user.* namespace')

# Knowledge-backed, cross-platform toolchain discovery.
tool_models=text('src/LocalGPT/BusinessObjects/ToolchainIntegrationModels.cs')
tool_discovery=text('src/LocalGPT/Services/ToolchainDiscoveryService.cs')
tool_knowledge=text('src/LocalGPT/Services/ToolchainKnowledgeService.cs')
tool_dx=text('src/LocalGPT/Services/ToolchainDxAiFunctions.cs')
project_service=text('src/LocalGPT/Services/ProjectMaintenanceService.cs')+text('src/LocalGPT/Services/ProjectMaintenanceService.BuildReview.cs')
install=text('src/LocalGPT/Components/Pages/Install.razor')+text('src/LocalGPT/Components/Pages/Install.razor.cs')
env_editor=text('src/LocalGPT/Components/Shared/ToolchainEnvironmentEditor.razor')
tool_doc=text('docs/reference/toolchain-discovery.md')
for token in ['Windows','Linux','MacOS']:
    require(token in tool_models, f'toolchain platform enum missing {token}')
require('Path.PathSeparator' in tool_discovery,'PATH discovery is not cross-platform list based')
require('SplitPathDirectories' in tool_discovery and 'PATH' in tool_discovery,'PATH-first discovery missing')
for token in ['EnvironmentRootVariables','WindowsSearchRoots','LinuxSearchRoots','MacOsSearchRoots','CommonSearchRoots']:
    require(token in tool_discovery and token in tool_models, f'knowledge-backed discovery missing {token}')
require('OperatingSystem.IsWindows()' in tool_discovery and 'OperatingSystem.IsLinux()' in tool_discovery and 'OperatingSystem.IsMacOS()' in tool_discovery,'platform selection is not Windows/Linux/macOS aware')
require('File.Exists(root)' in tool_discovery,'environment variable executable-file discovery missing')
for obsolete in ['DiscoverCompilerCandidates(', 'EnumerateCompilerFiles(', 'GetCompilerFiles(', 'GetCompilerDirectories(']:
    require(obsolete not in project_service, f'old hardcoded ProjectMaintenance discovery helper remains: {obsolete}')
for key in ['dotnet-sdk','java-jdk','java-runtime','gradle','maven','python','node','powershell','gcc','clang','cmake','rust-cargo','go']:
    require(f'"key":"{key}"' in tool_doc, f'knowledge seed is missing toolchain profile {key}')
require('http://' not in tool_doc and 'https://' not in tool_doc,'toolchain discovery knowledge seed contains an online endpoint')
for token in ['builtin.toolchain-knowledge-block','builtin.toolchain-version-token','builtin.toolchain-environment-token']:
    require(token in text('src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs'), f'runtime regex seed missing {token}')
    require(token in tool_knowledge or token in tool_discovery, f'toolchain services do not consume {token}')
require('toolchain-discovery.md' in text('src/LocalGPT/Services/Persistence/InitialDataCatalog.cs'),'toolchain discovery knowledge document not seeded into Knowledge DB')
for token in ['Provide Markdown file','Knowledge Database article','Paste text blob','Skip for now']:
    require(token in tool_knowledge, f'missing Human Collaboration knowledge choice: {token}')
require('no automatic online lookup' in tool_knowledge.lower() or 'no online lookup' in tool_knowledge.lower(),'missing explicit offline toolchain-knowledge request policy')
for token in ['toolchain.knowledge.list','toolchain.installation.list','toolchain.discover','toolchain.installation.save','toolchain.installation.validate','toolchain.installation.delete','toolchain.knowledge.request']:
    require(token in tool_dx, f'toolchain DXFunction missing {token}')
require('[Route("api/toolchains")' in text('src/LocalGPT/Controller/ToolchainKnowledgeController.cs'),'toolchain knowledge controller missing')
require('[HttpGet("compilers")' in text('src/LocalGPT/Controller/ProjectMaintenanceController.cs') and '[HttpPost("compilers/discover")' in text('src/LocalGPT/Controller/ProjectMaintenanceController.cs'),'project toolchain CRUD/discovery controller missing')
require('ToolchainEnvironmentEditor' in install and 'ToolchainKind' in install and 'DetectedPlatform' in install,'Installer does not show structured toolchain metadata/editor')
for token in ['Environment variables','PATH itself is discovered','EnvironmentVariables = _variables']:
    require(token in env_editor, f'structured toolchain environment editor missing {token}')
require('ToolchainKind' in text('src/LocalGPT/BusinessObjects/ProjectMaintenanceModels.cs') and 'DetectedPlatform' in text('src/LocalGPT/BusinessObjects/ProjectMaintenanceModels.cs'),'persisted toolchain kind/platform fields missing')
require('item.ToolchainKind' in text('src/LocalGPT/Services/ProjectMaintenanceDxAiFunctions.cs'),'Project Maintenance AI metadata omits toolchain kind')

# Preserve existing critical architecture lines.
require('CurrentSeedVersion = 26' in text('src/LocalGPT/Services/CouncilTeamConfigurationService.cs'),'Council team seed version changed unexpectedly')
for old in ['TacosPortal.Services','namespace LocalGPT.Endpoints']:
    productive='\n'.join(p.read_text(encoding='utf-8',errors='replace') for p in (ROOT/'src/LocalGPT').rglob('*.cs'))
    require(old not in productive, f'legacy namespace returned: {old}')

if fail:
    print('LocalGPT 3.0.4 integration fabric/toolchain source audit failed:')
    for item in fail: print(' -',item)
    sys.exit(1)
print(f'LocalGPT 3.0.4 integration fabric/toolchain source audit passed: {checks} checks.')
