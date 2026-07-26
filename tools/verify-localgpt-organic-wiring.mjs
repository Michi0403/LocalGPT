import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';

const root = path.resolve(import.meta.dirname, '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');
const program = read('LocalGPTWebviewWrapper', 'LocalGPT', 'Program.cs');
const wrapper = read('LocalGPTWebviewWrapper', 'LocalGPTWebviewWrapper', 'App.xaml.cs');
const installer = read('LocalGPTWebviewWrapper', 'LocalGPTInstallerConsole', 'Program.cs');
const wrapperProject = read('LocalGPTWebviewWrapper', 'LocalGPTWebviewWrapper', 'LocalGPTWebviewWrapper.csproj');
const settings = JSON.parse(read('LocalGPTWebviewWrapper', 'LocalGPT', 'appsettings.json'));
const organicModels = read('LocalGPTWebviewWrapper', 'LocalGPT', 'BusinessObjects', 'OrganicCouncilModels.cs');
const oneWireTransport = read('LocalGPTWebviewWrapper', 'LocalGPT', 'Services', 'OneWire', 'OneWireTransportHostedServices.cs');
const oneWireController = read('LocalGPTWebviewWrapper', 'LocalGPT', 'Controller', 'OneWireController.cs');
const councilText = read('LocalGPTWebviewWrapper', 'LocalGPT', 'Services', 'CouncilTextService.cs');
const chat = read('LocalGPTWebviewWrapper', 'LocalGPT', 'Components', 'Pages', 'Chat.razor');
const initialData = read('LocalGPTWebviewWrapper', 'LocalGPT', 'Services', 'Persistence', 'InitialDataCatalog.cs');
const learningBlueprint = read('LocalGPTWebviewWrapper', 'LocalGPT', 'Services', 'OrganicCouncilBlueprintService.cs');
const organicDx = read('LocalGPTWebviewWrapper', 'LocalGPT', 'Services', 'OneWire', 'OrganicPluginDxAiFunctions.cs');

assert.match(program, /public const int DefaultPort = 5000;/, 'Program.DefaultPort must remain 5000 for installer compatibility.');
assert.match(program, /public static System\.Int32 Port => System\.Threading\.Volatile\.Read\(ref runtimePort\);/, 'Program.Port must remain a public read-only wrapper/installer contract.');
assert.doesNotMatch(program, /(?<!System\.Threading\.)\bVolatile\.(?:Read|Write)/, 'All Volatile access must be explicitly qualified because DevExpress.CodeParser also exposes Volatile.');
assert.match(program, /int\.TryParse\(args\[0\]/, 'The installer positional port argument must remain supported.');
assert.match(program, /ValidatePortContracts\(logger\);/, 'Startup must validate application/organic port separation.');
assert.match(program, /installer-selected web port is authoritative/, 'Installer-port precedence safeguard is missing.');
assert.match(program, /Reassigned conflicting optional organic TCP port/, 'Organic port collision repair is missing.');
assert.doesNotMatch(program, /installer\/bootstrap port contract cannot share a listener/, 'An optional organic port conflict must not terminate the installer/bootstrap path.');
assert.doesNotMatch(program, /public\s+static\s+int\s+Port\s*\{\s*get;\s*set;/, 'Do not restore a publicly mutable int port property; keep the compatibility surface read-only.');
assert.match(wrapper, /_baseUrl = LocalGPT\.Program\.BaseUrl;/, 'The WinUI wrapper must use the authoritative Program endpoint.');
assert.match(installer, /LocalGptPort \{ get; private set; \} = 5000;/, 'Installer default port changed unexpectedly.');
assert.match(installer, /ArgumentList = \{ port\.ToString\(\) \}/, 'Installer must pass the selected LocalGPT port to the executable.');
assert.match(wrapperProject, /ProjectReference Include="\.\.\\LocalGPT\\LocalGPT\.csproj"/, 'Wrapper project reference to LocalGPT is missing.');
assert.equal(settings.LocalGPT.Port, 5000);
assert.equal(settings.OneWire.ServicePort, 51140);
assert.equal(settings.OneWire.DiscoveryPort, 51141);
assert.match(organicModels, /public class ProjectOrganicContext/, 'ProjectOrganicContext must remain inheritable by the save request DTO.');
assert.doesNotMatch(organicModels, /public sealed class ProjectOrganicContext/, 'A sealed ProjectOrganicContext breaks SaveProjectOrganicContextRequest compilation.');
assert.match(oneWireTransport, /ISupervisedTaskRunner taskRunner/, 'Concurrent 1-Wire client handlers must use the supervised task runner.');
assert.match(oneWireTransport, /taskRunner\.Run\(/, 'Concurrent 1-Wire client handlers must be supervised.');
assert.match(oneWireController, /HumanApprovalRequired\([\s\S]*onewire\.peer\.invoke/, 'HTTP organic peer invocation must use the persistent human approval gate.');
assert.match(oneWireController, /HumanApprovalRequired\([\s\S]*project\.organic-context\.save/, 'Project organic-context mutation must use the persistent human approval gate.');
const oneWireExecution = read('LocalGPTWebviewWrapper', 'LocalGPT', 'Services', 'OneWire', 'OneWireExecutionServices.cs');
assert.match(oneWireExecution, /OneWireCouncilApprovalProcessorHostedService/, 'Approved external council requests must resume automatically.');
assert.match(oneWireExecution, /Resumed approved organic request/, 'Council approval resume path is missing.');
assert.match(oneWireExecution, /SendResultAsync\(item, OneWireWorkStatus\.Completed/, 'Completed council work must return an explicit status to the organic peer.');
assert.match(oneWireExecution, /SendResultAsync\(item, OneWireWorkStatus\.Failed/, 'Failed council work must be returned to the organic peer.');


assert.match(councilText, /var prompt = """/);
assert.doesNotMatch(councilText, /var prompt = \$+"""/);
assert.match(councilText, /<localgpt-self-assessment>\{"modelName"/, 'Self-assessment JSON must be literal content in a non-interpolated raw string.');
assert.match(chat, /data-testid="council-team-picker"/);
assert.match(chat, /Running Council session/);
assert.match(chat, /Enable human participation/);
assert.match(chat, /Share without stopping generation/);
assert.match(chat, /MaxFileCount="@LocalGptCatalogService\.MaxUploadFiles"/);
assert.match(chat, /MaxFileSize="@LocalGptCatalogService\.MaxUploadBytes"/);
assert.doesNotMatch(chat, /<DxAIChatFileUploadSettings[\s\S]{0,300}FileTypeFilter=/, 'Chat upload must not reject unknown offline media types.');
assert.match(initialData, /new\("LearningRoundPolicy"/);
assert.match(initialData, /builtin\.solution-project-reference/);
assert.match(initialData, /builtin\.installer-port-contract/);
assert.match(initialData, /builtin\.onewire-capability-key/);
assert.match(learningBlueprint, /Key = "learning-round"/);
assert.match(organicDx, /class ProposePublisherTextFunction/);
assert.match(organicDx, /Name: "publisher\.text\.proposal\.request"/);

const requiredFiles = [
  ['BusinessObjects', 'OneWireProtocolModels.cs'],
  ['Interfaces', 'IOneWireServices.cs'],
  ['Services', 'OneWire', 'OneWireEnvelopeCodec.cs'],
  ['Services', 'OneWire', 'OneWireTransportHostedServices.cs'],
  ['Services', 'OrganicCouncilBlueprintService.cs'],
  ['Services', 'ProjectOrganicContextService.cs'],
  ['Controller', 'OneWireController.cs'],
  ['BusinessObjects', 'LearningRoundModels.cs'],
  ['Interfaces', 'ILearningRoundService.cs'],
  ['Services', 'LearningRoundService.cs'],
  ['Services', 'LearningRoundDxAiFunctions.cs'],
  ['Services', 'RegexDxAiFunctions.cs']
];
const blueprint = read('LocalGPTWebviewWrapper', 'LocalGPT', 'Services', 'OrganicCouncilBlueprintService.cs');
assert.match(blueprint, /Key = "openscad-team"/);
assert.match(blueprint, /Key = "spreadsheet-team"/);
assert.match(blueprint, /Key = "learning-round"/);
assert.match(blueprint, /sequential 1-Wire spools/);

for (const parts of requiredFiles) assert.ok(fs.existsSync(path.join(root, 'LocalGPTWebviewWrapper', 'LocalGPT', ...parts)), `Missing ${parts.join('/')}`);

console.log('LocalGPT installer/bootstrap and organic 1-Wire source contracts passed.');
