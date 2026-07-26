import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');
const exists = (...parts) => fs.existsSync(path.join(root, ...parts));

const appProject = read('LocalGPTWebviewWrapper','LocalGPT','LocalGPT.csproj');
const installerProject = read('LocalGPTWebviewWrapper','LocalGPTInstallerConsole','LocalGPTInstallerConsole.csproj');
const wrapperProject = read('LocalGPTWebviewWrapper','LocalGPTWebviewWrapper','LocalGPTWebviewWrapper.csproj');
const wireProject = read('LocalGPTWebviewWrapper','LocalGPT.WireProtocolVersion','LocalGPT.WireProtocolVersion.csproj');
const wire = read('LocalGPTWebviewWrapper','LocalGPT.WireProtocolVersion','OneWireProtocolContracts.cs');
const program = read('LocalGPTWebviewWrapper','LocalGPT','Program.cs');
const councilText = read('LocalGPTWebviewWrapper','LocalGPT','Services','CouncilTextService.cs');
const preflight = read('LocalGPTWebviewWrapper','LocalGPT','Services','Council','CouncilPreflightService.cs');
const runtimeDirectory = read('LocalGPTWebviewWrapper','LocalGPT','Services','Council','RuntimeCapabilityDirectoryService.cs');
const initialization = read('LocalGPTWebviewWrapper','LocalGPT','Services','Persistence','DatabaseInitializationService.cs');
const council = read('LocalGPTWebviewWrapper','LocalGPT','Services','MultiModelCouncilService.cs');
const planner = read('LocalGPTWebviewWrapper','LocalGPT','Services','Council','Scheduling','CouncilHardwareRoadPlanner.cs');
const roadConfig = read('LocalGPTWebviewWrapper','LocalGPT','Services','Council','Scheduling','CouncilHardwareRoadConfigurationService.cs');
const roadEditor = read('LocalGPTWebviewWrapper','LocalGPT','Components','Shared','CouncilHardwareRoadEditor.razor');
const chat = read('LocalGPTWebviewWrapper','LocalGPT','Components','Pages','Chat.razor');
const modelCouncil = read('LocalGPTWebviewWrapper','LocalGPT','Components','Pages','ModelCouncil.razor');
const spooler = read('LocalGPTWebviewWrapper','LocalGPT','Services','Council','CouncilSpoolerService.cs');
const spoolerPanel = read('LocalGPTWebviewWrapper','LocalGPT','Components','Layout','CouncilSpoolerPanel.razor');
const mainLayout = read('LocalGPTWebviewWrapper','LocalGPT','Components','Layout','MainLayout.razor');
const debugInspector = read('LocalGPTWebviewWrapper','LocalGPT','Services','DebugArtifactInspectionService.cs');
const organicInterface = read('LocalGPTWebviewWrapper','LocalGPT','Interfaces','IOrganicSkillRegistryService.cs');
const organicService = read('LocalGPTWebviewWrapper','LocalGPT','Services','OrganicSkillRegistryService.cs');

for (const project of [appProject,installerProject,wrapperProject]) assert.match(project, /<Version>2\.0\.0<\/Version>/);
assert.match(appProject, /ProjectReference Include="\.\.\\LocalGPT\.WireProtocolVersion\\LocalGPT\.WireProtocolVersion\.csproj"/);
assert.ok(exists('LocalGPTWebviewWrapper','LocalGPT.WireProtocolVersion','OneWireProtocolContracts.cs'));
assert.match(wireProject, /<Version>2\.0\.0<\/Version>/);
assert.match(wire, /public sealed class OneWireModelSelfAssessment/);
for (const field of ['ModelName','MemberKey','DxFunctions','ControllerMethods','OrganicCapabilities','Skills','Confidence','Evidence'])
  assert.ok(wire.includes(field), `${field} missing from OneWireModelSelfAssessment.`);
assert.match(organicInterface, /LocalGPT\.WireProtocol\.OneWireModelSelfAssessment/);
assert.match(organicService, /LocalGPT\.WireProtocol\.OneWireModelSelfAssessment/);

assert.match(program, /public static System\.Int32 Port => System\.Threading\.Volatile\.Read/);
assert.doesNotMatch(program, /(?<!System\.Threading\.)Volatile\.(Read|Write)/);
assert.match(program, /AddScoped<IOrganicSkillRegistryService, OrganicSkillRegistryService>/);
assert.match(program, /AddScoped<IRuntimeCapabilityDirectoryService, LocalGPT\.Services\.Council\.RuntimeCapabilityDirectoryService>/);
assert.match(program, /AddHostedService<LocalGPT\.Services\.Council\.RuntimeCapabilityDirectoryHostedService>/);
assert.match(program, /AddSingleton<ICouncilSpoolerService/);

const promptMethod = councilText.slice(councilText.indexOf('MultiModelCouncilServiceCreateCouncilSystemPrompt'), councilText.indexOf('MultiModelCouncilServiceCreateCouncilSystemPrompt') + 12000);
assert.match(promptMethod, /var prompt = """/);
assert.doesNotMatch(promptMethod, /var prompt = \$+"""/);
assert.match(promptMethod, /localgpt-self-assessment>\{"modelName"/);
assert.match(promptMethod, /CPU\/GPU\/accelerator road, its model-specific minimum\/maximum token range/);

for (const projectName of ['LocalGPT Core','Humanitarian Collaboration Workspace']) assert.ok(initialization.includes(projectName));
for (const id of ['7f4d7b4a-b622-4d15-8e44-9dfae2aa6101','7f4d7b4a-b622-4d15-8e44-9dfae2aa6102']) assert.ok(initialization.includes(id));
assert.match(initialization, /Adaptive Mixed Hardware Council/);
assert.match(initialization, /Learning Round/);
assert.match(initialization, /SeedRegexAsync/);
assert.match(initialization, /SeedCoreProjectsAsync/);
assert.match(initialization, /SeedCouncilModelPresetsAsync/);
assert.ok(initialization.includes('pdb|dll|exe'));

assert.match(preflight, /capabilityDirectory\.SynchronizeAsync/);
assert.match(preflight, /CouncilMemberOrganicSkillLinks/);
assert.match(preflight, /AssignedDxFunctions/);
assert.match(preflight, /AssignedOrganicSkills/);
assert.match(preflight, /matching debug symbols/);
assert.match(runtimeDirectory, /Runtime DXFunction directory/);
assert.match(runtimeDirectory, /Runtime organic skill directory/);
assert.match(runtimeDirectory, /Startup continues/);

assert.match(roadConfig, /NormalizeLoadPercent/);
assert.match(roadConfig, /Math\.Round\(value \/ 5d\) \* 5/);
assert.match(planner, /Interpolate/);
assert.match(planner, /MinOutputTokens/);
assert.match(planner, /MaxContextTokens/);
for (const ui of [roadEditor,chat,modelCouncil]) assert.match(ui, /type="range"[^>]*min="0"[^>]*max="100"[^>]*step="5"/);
assert.match(roadEditor, /LoadPercentOverride/);
assert.match(council, /AllowParallelHardwareRoads/);
assert.match(council, /SemaphoreSlim/);
assert.match(council, /BuildMemberReadinessPrompt/);
assert.match(council, /Readiness and introductions/);
assert.match(council, /Expert preparation/);
assert.match(council, /Leader synthesis/);

assert.match(spooler, /ConcurrentDictionary<Guid, CouncilSpoolerSnapshot>/);
assert.match(spooler, /recent-runs\.json/);
assert.match(spooler, /browser circuits can disconnect/i);
assert.match(spoolerPanel, /Rejoin a running or recent AI Council session/);
assert.match(spoolerPanel, /Refresh/);
assert.match(mainLayout, /<CouncilSpoolerPanel \/>/);
assert.match(mainLayout, /<HumanCollaborationInbox \/>/);
assert.match(chat, /Join and add information to a running AI Council/);
assert.match(chat, /RefreshHumanCollaborationAsync/);

assert.match(debugInspector, /MetadataReaderProvider\.FromPortablePdbStream/);
assert.match(debugInspector, /MaximumInspectionBytes/);
assert.match(debugInspector, /Windows\/native or unknown PDB/);
assert.match(program, /DefaultPort = 5000/);

console.log('LocalGPT v2.0.0 Council spooler, shared protocol and source-closure contracts passed.');
