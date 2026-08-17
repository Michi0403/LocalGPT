#!/usr/bin/env python3
from pathlib import Path
import re
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

# Active release version and preserved transport/schema boundaries.
for rel in [
    'src/LocalGPT/LocalGPT.csproj',
    'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
    'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
]:
    require('<Version>3.0.7</Version>' in text(rel), f'{rel}: version is not 3.0.7')
require('<Version>2.1.1</Version>' in text('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj'),
        '1-Wire protocol changed unexpectedly')
require(not any('3_0_7' in p.name.lower() for p in (ROOT / 'src/LocalGPT/Migrations').glob('*.cs')),
        '3.0.7 unexpectedly introduced an EF migration')

# User-reported CS0173: nested timeout/process/null conditional needs a nullable target type.
console = text('src/LocalGPT/Services/ConsoleCommandService.cs')
expected = 'int? exitCode = timedOut ? -2 : process.HasExited ? process.ExitCode : null;'
require(expected in console, 'ConsoleCommandService exitCode conditional is not explicitly nullable')
require('var exitCode = timedOut ? -2 : process.HasExited ? process.ExitCode : null;' not in console,
        'CS0173-producing inferred exitCode expression is still present')
require('public int? ExitCode { get; set; }' in text('src/LocalGPT/BusinessObjects/InitialSetupAssistantModels.cs'),
        'LocalConsoleCommandResult.ExitCode no longer matches nullable console exit semantics')
for token in ['exitCode == 0', 'ExitCode = exitCode', 'exitCode?.ToString() ?? "n/a"']:
    require(token in console, f'console result/status flow lost expected exit-code use: {token}')

# Search for the same compact nested-null inference hazard in current C# sources.
pattern = re.compile(r'^\s*var\s+\w+\s*=.*\?.*:\s*.*\?.*:\s*null\s*;', re.MULTILINE)
for p in (ROOT / 'src/LocalGPT').rglob('*.cs'):
    source = p.read_text(encoding='utf-8', errors='replace')
    require(not pattern.search(source), f'{p.relative_to(ROOT)} still contains a nested var/null conditional inference hazard')

# Retain the earlier 3.0.6 compiler fixes.
console_dx = text('src/LocalGPT/Services/ConsoleCommandDxAiFunctions.cs')
require('&& request.Parameters.TryGetProperty("take", out var takeValue))' in console_dx,
        'ConsoleHistoryFunction TryGetProperty condition regressed')
provider_dx = text('src/LocalGPT/Services/InitialSetupProviderConfigurationDxAiFunction.cs')
require('using LocalGPT.BusinessObjects;' in provider_dx,
        'provider configuration DXFunction business-object import regressed')
provider_service = text('src/LocalGPT/Services/AiProviderBootstrapService.cs')
qualified = 'global::LocalGPT.BusinessObjects.ConfigurationRoot'
require(f'IOptionsMonitor<{qualified}> options' in provider_service,
        'AiProviderBootstrapService options root qualification regressed')
require(f'new {qualified}' in provider_service,
        'AiProviderBootstrapService persisted root qualification regressed')

# Preserve setup integration and render-mode boundaries.
setup_service = text('src/LocalGPT/Services/InitialSetupAssistantService.cs')
for token in ['SaveHardwareListAsync', 'GetHardwareRecommendationsAsync', 'CreateBenchmarkTeamAsync']:
    require(token in setup_service, f'initial setup orchestration missing {token}')
razor = '\n'.join(p.read_text(encoding='utf-8', errors='replace') for p in (ROOT / 'src/LocalGPT/Components').rglob('*.razor'))
require(razor.count('@rendermode') == 20, '3.0.7 unexpectedly changed explicit InteractiveServer island count')

canirun = text('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs')
require('LocalGPT/3.0.7' in canirun and 'source-credit-canirun.ai' in canirun,
        'CanIRun runtime identification/credit is not aligned with 3.0.7')

if failures:
    print('LocalGPT 3.0.7 nullable-exitcode source audit failed:')
    for failure in failures:
        print(' -', failure)
    sys.exit(1)
print(f'LocalGPT 3.0.7 nullable-exitcode source audit passed: {checks} checks.')
