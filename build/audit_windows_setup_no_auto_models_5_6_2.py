#!/usr/bin/env python3
"""Static acceptance of opt-in model acquisition in Windows setup; no dotnet/network/process execution."""
from pathlib import Path
import re
ROOT = Path(__file__).resolve().parents[1]

def text(path):
    return (ROOT / path).read_text(encoding='utf-8-sig')

def check(ok, detail):
    if not ok:
        raise SystemExit('LocalGPT 5.6.3 setup model opt-in contract failed: '+detail)

installer = text('src/LocalGPTInstallerConsole/Program.cs')
parse = installer.split('public static CliOptions Parse(string[] args)', 1)[1].split('public override string ToString()', 1)[0]
implicit = parse.split('if (argsList.Count == 0)',1)[1].split('for (var i = 0;',1)[0]
all_case = parse.split('case "--all":',1)[1].split('case "--range":',1)[0]
explicit = parse.split('case "--pull-models":',1)[1].split('case "--install-localgpt":',1)[0]
check('--install-ollama' in implicit and '--install-localgpt' in implicit and '--start-localgpt' in implicit, 'default setup actions lost')
check('--pull-models' not in implicit and 'PullOllamaModels' not in implicit, 'default setup still pulls models')
check('PullOllamaModels' not in all_case, '--all still implicitly pulls models')
check('options.PullOllamaModels = true;' in explicit, 'manual --pull-models switch was disabled')
check(re.search(r'if \(options.PullOllamaModels\)\s*\{\s*var ollamaExe', installer), 'model download no longer guarded by explicit flag')
check('PullModelsAsync(ollamaExe, GetModelSet(options.Range), logger)' in installer, 'manual model pull removed')
check('InstallOllamaAsync(options, logger)' in installer, 'official Ollama install removed')
check('SetupOperatorConsole.ThrowIfCancellationRequested()' in installer, 'cancellation boundary lost')
check('in LocalGPT /install (Ollama or LM Studio)' in installer, 'user-directed provider guidance missing')

onboarding = text('src/LocalGPT/Services/FirstRunOnboardingService.cs')
profiles = onboarding.split('private IReadOnlyList<OnboardingInstallerProfile> CreateInstallerProfiles()',1)[1].split('private IReadOnlyList<CouncilQuickStart> CreateQuickStarts()',1)[0]
check('--pull-models' not in profiles, 'first-run UI advertises an automatic model pull command')
check('--install-ollama' in profiles and '/install' in profiles, 'first-run Ollama install/model handoff missing')
check('qwen3.5:0.8b' in profiles, 'low-B model recommendations were removed instead of remaining suggestions')
check('CreateQuickStarts()' in onboarding and 'GetPresetsAsync(' in onboarding, 'configured AI team onboarding was regressed')
for rel in ('src/LocalGPTInstallerConsole/Pull-Models-Slim.cmd', 'src/LocalGPTInstallerConsole/Pull-Models-RTX306012GSet.cmd', 'src/LocalGPTInstallerConsole/Pull-Models-RX7900XTXSet.cmd'):
    check('--pull-models' in text(rel), f'voluntary model pull utility changed: {rel}')
for rel in ('src/LocalGPTInstallerConsole/LocalGPT-Install-Start.cmd', 'src/LocalGPTInstallerConsole/Install-Force-Delete-Start.cmd'):
    check('--pull-models' not in text(rel), f'default launcher downloads models: {rel}')
check('<Version>5.6.3</Version>' in text('src/LocalGPT/LocalGPT.csproj'), 'LocalGPT release version missing')
check('<Version>4.9.5</Version>' in text('src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj'), 'installer release version missing')
check('game-studio.js?v=5.6.3' in text('src/LocalGPT/Components/App.razor'), 'browser cache version not refreshed')
check('without downloading any AI models' in text('src/LocalGPTInstallerConsole/README.md'), 'installer docs do not describe model-free default')
print('LocalGPT 5.6.3 Windows default/--all install is model-free; deliberate manual pulling, Ollama installer, first-run /install and prior features remain intact.')
