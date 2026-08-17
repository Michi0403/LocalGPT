#!/usr/bin/env python3
from pathlib import Path
import re, sys
ROOT=Path(__file__).resolve().parents[1]
fail=[]; checks=0

def require(cond,msg):
    global checks; checks+=1
    if not cond: fail.append(msg)

def text(rel): return (ROOT/rel).read_text(encoding='utf-8',errors='replace')

for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj']:
    require('<Version>3.0.7</Version>' in text(rel),f'{rel}: version is not 3.0.3')

# Exact Windows compiler regressions reported after 3.0.2.
prompt=text('src/LocalGPT/Services/LocalGptCatalogService.PromptCatalog.cs')
require('_logger.LogDebug(__serviceMethodException' in prompt,'GetSuggestion cancellation path does not use the LocalGptCatalogService logger field')
require('_logger.LogError(__serviceMethodException' in prompt,'GetSuggestion failure path does not use the LocalGptCatalogService logger field')
require(re.search(r'(?<!_)\blogger\.Log(?:Debug|Error)\(__serviceMethodException',prompt) is None,'GetSuggestion still references an out-of-scope logger identifier')
require(prompt.count('/// <summary>') == prompt.count('/// </summary>'),'PromptCatalog XML summary tags are unbalanced')

mc_base=text('src/LocalGPT/Services/MinecraftDatapackService.cs')
mc_art=text('src/LocalGPT/Services/MinecraftDatapackService.VersionsValidationAndArtifacts.cs')
require('private readonly CouncilTextService _text;' in mc_base,'MinecraftDatapackService text collaborator is not stored under a non-shadowable field name')
require('_text = text;' in mc_base,'MinecraftDatapackService constructor does not assign the text collaborator')
require('_text.ToPascalIdentifier(displayName, logger)' in mc_art,'Datapack artifact identity does not route Pascal identifier formatting through CouncilTextService')
require('text.ToPascalIdentifier(displayName, logger)' not in mc_art.replace('_text.ToPascalIdentifier(displayName, logger)',''),'Datapack artifact identity still binds ToPascalIdentifier to the string parameter')

# Preserve the 3.0.2 Windows build-guard and DXFunction wiring repairs.
require('using LocalGPT.Services;' in text('src/LocalGPT/Controller/StructuredTextController.cs'),'StructuredTextController lost LocalGPT.Services import')
for rel, token in [
    ('build/Assert-OperationalDiagnostics.ps1','Require-TextAcrossFiles'),
    ('build/Assert-InteractiveServerRenderModes.ps1',"-Filter 'Program*.cs'"),
    ('build/Assert-IteratorExceptionPolicy.ps1','Get-BaselineRelativePath'),
    ('build/Assert-SystemVariableInitialization.ps1','Get-BaselineRelativePath')]:
    require(token in text(rel),f'{rel}: 3.0.2 partial-aware Windows guard repair was lost')
mc_dx=text('src/LocalGPT/Services/MinecraftDxAiFunctions.cs')
for token in ['minecraft.datapack.version.resolve','minecraft.dependency.version.resolve','IDxAiFunctionHandler']:
    require(token in mc_dx,f'Minecraft DXFunction wiring lost: {token}')

# No accidental regression in critical rendering/transport/schema lines.
require('2.1.1' in text('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj'),'Wire protocol version changed unexpectedly')
razor='\n'.join(p.read_text(encoding='utf-8',errors='replace') for p in (ROOT/'src/LocalGPT/Components').rglob('*.razor'))
require(razor.count('@rendermode') == 20,'explicit InteractiveServer island/page count is no longer 20 after adding Remote Control')
require(text('src/LocalGPT/Components/Pages/RemoteControl.razor').lstrip().startswith('@rendermode InteractiveServer'), 'Remote Control did not preserve the InteractiveServer render contract')

if fail:
    print('LocalGPT 3.0.3 source audit failed:')
    for f in fail: print(' -',f)
    sys.exit(1)
print(f'LocalGPT 3.0.3 partial compile/shadowing source audit passed: {checks} checks.')
