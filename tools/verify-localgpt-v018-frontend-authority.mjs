import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');
const exists = (...parts) => fs.existsSync(path.join(root, ...parts));

const wire = read('LocalGPTWebviewWrapper','LocalGPT.WireProtocolVersion','OneWireProtocolContracts.cs');
const appProject = read('LocalGPTWebviewWrapper','LocalGPT','LocalGPT.csproj');
const wireProject = read('LocalGPTWebviewWrapper','LocalGPT.WireProtocolVersion','LocalGPT.WireProtocolVersion.csproj');
const program = read('LocalGPTWebviewWrapper','LocalGPT','Program.cs');
const catalogModel = read('LocalGPTWebviewWrapper','LocalGPT','BusinessObjects','DxAiFunctionCatalogModels.cs');
const catalogInterface = read('LocalGPTWebviewWrapper','LocalGPT','Interfaces','IDxAiFunctionCatalogService.cs');
const catalogService = read('LocalGPTWebviewWrapper','LocalGPT','Services','DxAiFunctionCatalogService.cs');
const publicInvoker = read('LocalGPTWebviewWrapper','LocalGPT','Services','PublicServiceMethodInvoker.cs');
const capabilityCatalog = read('LocalGPTWebviewWrapper','LocalGPT','Services','OneWire','OneWireCapabilityCatalog.cs');
const execution = read('LocalGPTWebviewWrapper','LocalGPT','Services','OneWire','OneWireExecutionServices.cs');
const transport = read('LocalGPTWebviewWrapper','LocalGPT','Services','OneWire','OneWireTransportHostedServices.cs');
const humanModels = read('LocalGPTWebviewWrapper','LocalGPT','BusinessObjects','HumanCollaborationModels.cs');
const humanService = read('LocalGPTWebviewWrapper','LocalGPT','Services','HumanCollaborationService.cs');
const page = read('LocalGPTWebviewWrapper','LocalGPT','Components','Pages','DxFunctionCatalog.razor');
const inbox = read('LocalGPTWebviewWrapper','LocalGPT','Components','Layout','HumanCollaborationInbox.razor');
const nav = read('LocalGPTWebviewWrapper','LocalGPT','Components','Layout','NavMenu.razor');
const runtimeDirectory = read('LocalGPTWebviewWrapper','LocalGPT','Services','Council','RuntimeCapabilityDirectoryService.cs');
const selfAssessmentInterface = read('LocalGPTWebviewWrapper','LocalGPT','Interfaces','IOrganicSkillRegistryService.cs');
const selfAssessmentService = read('LocalGPTWebviewWrapper','LocalGPT','Services','OrganicSkillRegistryService.cs');

assert.match(appProject, /<Version>0\.1\.8<\/Version>/);
assert.match(wireProject, /<Version>1\.6\.0<\/Version>/);
assert.match(wire, /public const string Version = "1\.6";/);
for (const token of [
  'OneWireInteractionEditor', 'IsExposedToPeer', 'AllowPeerInvocation',
  'RequiresFrontendUserConfirmation', 'ConfigurationKey', 'RequiresFrontendConfirmation',
  'RequireLinkedPeer', 'InteractionValueJson'
]) assert.ok(wire.includes(token), `${token} is missing from the shared protocol.`);
assert.match(wire, /public sealed class OneWireModelSelfAssessment/);
assert.match(selfAssessmentInterface, /LocalGPT\.WireProtocol\.OneWireModelSelfAssessment/);
assert.match(selfAssessmentService, /LocalGPT\.WireProtocol\.OneWireModelSelfAssessment/);

for (const file of [
  ['BusinessObjects','DxAiFunctionCatalogModels.cs'],
  ['Interfaces','IDxAiFunctionCatalogService.cs'],
  ['Services','DxAiFunctionCatalogService.cs'],
  ['Services','PublicServiceMethodInvoker.cs'],
  ['Controller','DxAiFunctionCatalogController.cs'],
  ['Components','Pages','DxFunctionCatalog.razor']
]) assert.ok(exists('LocalGPTWebviewWrapper','LocalGPT',...file), `${file.join('/')} missing.`);

for (const token of ['ExposeToAiChat','ExposeToOneWire','AllowRemoteInvocation','RequiresFrontendConfirmation','InteractionEditor','AllowedPeerIdsJson'])
  assert.ok(catalogModel.includes(token), `${token} missing from database-backed catalog policy.`);
assert.match(catalogInterface, /GetExposedToPeerAsync/);
assert.match(catalogService, /DataType = "DxAiFunctionCatalogEntry"/);
assert.match(catalogService, /PreservePolicyAndRefreshDescriptor/);
assert.match(catalogService, /DiscoverPublicServiceMethods/);
assert.match(catalogService, /implementation\.DeclaredMethods\.Where\(IsSupportedPublicMethod\)/);
assert.match(catalogService, /ExposeToOneWire = false/);
assert.match(catalogService, /RequiresFrontendConfirmation = true/);
assert.match(publicInvoker, /GetEntryAsync\(request\.CatalogKey/);
assert.match(publicInvoker, /serviceProvider\.GetService\(contractType\)/);
assert.match(publicInvoker, /BindArguments/);
assert.match(publicInvoker, /localgpt\.public_service\.invoke/);

assert.match(program, /AddScoped<IDxAiFunctionCatalogService, DxAiFunctionCatalogService>/);
assert.match(program, /AddScoped<IPublicServiceMethodInvoker, PublicServiceMethodInvoker>/);
assert.match(program, /AddHostedService<DxAiFunctionCatalogHostedService>/);
assert.match(capabilityCatalog, /GetLocalCapabilitiesForPeerAsync/);
assert.match(capabilityCatalog, /GetExposedToPeerAsync/);
assert.match(capabilityCatalog, /CreateAsyncScope/);
assert.doesNotMatch(capabilityCatalog.split('public sealed class OneWireCapabilityCatalog(',2)[1].split(')',1)[0], /IDxAiFunctionCatalogService/, 'A singleton capability catalog must not directly capture a scoped catalog service.');
assert.match(execution, /The requested capability is not exposed to this linked peer/);
assert.match(execution, /The requested capability is discovery-only/);
assert.match(execution, /OneWireTargetApprovalPolicy\.Create/);
assert.match(execution, /ApplyHumanResponse/);
assert.match(execution, /Waiting for the LocalGPT frontend user to approve this organic link/);
assert.match(execution, /This transport is not an approved 1-Wire link/);
assert.match(execution, /OneWireMessageType\.HelloAck/);
assert.match(execution, /LinkedByLocalFrontend/);
assert.match(transport, /ApplicationVersion = "0\.1\.8-organic-wire"/);
assert.doesNotMatch(transport, /peers\.SetConnected\(peerId, true\)/, 'A TCP connection must not become a trusted link before LocalGPT frontend approval.');
assert.match(execution, /ResponsePrompt:/);
assert.match(execution, /PrefillText: envelope\.InteractionValueJson/);
assert.match(execution, /AllowFreeText: needsValue/);
assert.match(humanModels, /string UserResponse = ""/);
assert.match(humanService, /existing\.UserResponse/);
assert.match(inbox, /request\.AllowFreeText/);
assert.match(humanService, /MaxTextLength = 1_000_000/);
assert.match(inbox, /maxlength="1000000"/);

assert.match(page, /@page "\/dx-functions"/);
for (const label of ['Reveal to AI chat','Reveal to linked app','Allow linked invocation','Require this frontend','Frontend editor','Allowed peer IDs JSON'])
  assert.ok(page.includes(label), `${label} missing from LocalGPT policy UI.`);
assert.match(nav, /NavigateUrl="\/dx-functions"/);
assert.match(runtimeDirectory, /Runtime user-controlled DXFunction exposure catalog/);
assert.match(runtimeDirectory, /functionCatalog\.SynchronizeAsync/);

assert.match(program, /public static System\.Int32 Port => System\.Threading\.Volatile\.Read/);
assert.doesNotMatch(program, /(?<!System\.Threading\.)Volatile\.(Read|Write)/);

console.log('LocalGPT v0.1.8 user-controlled DX/public-service exposure and receiving-frontend authority contracts passed.');
