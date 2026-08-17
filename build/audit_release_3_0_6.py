#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
failures = []
checks = 0

def require(condition, message):
    global checks
    checks += 1
    if not condition:
        failures.append(message)

def text(rel):
    return (ROOT / rel).read_text(encoding='utf-8', errors='replace')

# Active source version and preserved transport/schema boundaries.
for rel in [
    'src/LocalGPT/LocalGPT.csproj',
    'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
    'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
]:
    require('<Version>3.0.7</Version>' in text(rel), f'{rel}: version is not 3.0.7')
require('2.1.1' in text('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj'), '1-Wire protocol changed unexpectedly')
require(not any('3_0_6' in p.name.lower() for p in (ROOT / 'src/LocalGPT/Migrations').glob('*.cs')), '3.0.7 unexpectedly introduced an EF migration')

# User-reported parser error.
console_dx = text('src/LocalGPT/Services/ConsoleCommandDxAiFunctions.cs')
require('&& request.Parameters.TryGetProperty("take", out var takeValue))' in console_dx,
        'ConsoleHistoryFunction TryGetProperty condition is not closed')
require('take = takeValue.TryGetInt32(out var parsed) ? parsed : 120;' in console_dx,
        'ConsoleHistoryFunction bounded take parsing is missing')

# User-reported missing DXFunction business-object types.
provider_dx = text('src/LocalGPT/Services/InitialSetupProviderConfigurationDxAiFunction.cs')
require('using LocalGPT.BusinessObjects;' in provider_dx,
        'InitialSetupProviderConfigurationDxAiFunction is missing LocalGPT.BusinessObjects import')
for token in ['DxaichatFunctionInfo', 'DxAiFunctionInvocationRequest', 'DxAiFunctionInvocationResult', 'IDxAiFunctionHandler']:
    require(token in provider_dx, f'Provider configuration DXFunction contract missing {token}')

# User-reported ConfigurationRoot ambiguity.
provider_service = text('src/LocalGPT/Services/AiProviderBootstrapService.cs')
qualified = 'global::LocalGPT.BusinessObjects.ConfigurationRoot'
require(f'IOptionsMonitor<{qualified}> options' in provider_service,
        'AiProviderBootstrapService options root is not explicitly LocalGPT.BusinessObjects.ConfigurationRoot')
require(f'new {qualified}' in provider_service,
        'AiProviderBootstrapService persisted root is not explicitly LocalGPT.BusinessObjects.ConfigurationRoot')
require('IOptionsMonitor<ConfigurationRoot>' not in provider_service,
        'AiProviderBootstrapService still contains ambiguous bare ConfigurationRoot options usage')
require('new ConfigurationRoot' not in provider_service,
        'AiProviderBootstrapService still contains ambiguous bare ConfigurationRoot construction')

# All setup DXFunction files using the LocalGPT invocation DTOs must import the business-object namespace.
services = ROOT / 'src/LocalGPT/Services'
for p in services.glob('*DxAiFunction*.cs'):
    source = p.read_text(encoding='utf-8', errors='replace')
    if any(token in source for token in ['DxaichatFunctionInfo', 'DxAiFunctionInvocationRequest', 'DxAiFunctionInvocationResult']):
        require('using LocalGPT.BusinessObjects;' in source,
                f'{p.relative_to(ROOT)} uses DXFunction business objects without importing LocalGPT.BusinessObjects')

# Preserve the explicit render-mode island count validated by 3.0.5.
razor = '\n'.join(p.read_text(encoding='utf-8', errors='replace') for p in (ROOT / 'src/LocalGPT/Components').rglob('*.razor'))
require(razor.count('@rendermode') == 20, '3.0.7 unexpectedly changed explicit InteractiveServer island count')

# Runtime identification follows the active release while retaining explicit opt-in/source credit.
canirun = text('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs')
require('LocalGPT/3.0.7' in canirun and 'source-credit-canirun.ai' in canirun,
        'CanIRun runtime identification/credit is not aligned with 3.0.7')

if failures:
    print('LocalGPT 3.0.7 compile-repair source audit failed:')
    for failure in failures:
        print(' -', failure)
    sys.exit(1)
print(f'LocalGPT 3.0.7 compile-repair source audit passed: {checks} checks.')
