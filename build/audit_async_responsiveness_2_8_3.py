#!/usr/bin/env python3
from pathlib import Path
import subprocess, sys
root=Path(__file__).resolve().parents[1]
def require(path, needle):
    text=(root/path).read_text(encoding='utf-8-sig',errors='replace')
    if needle not in text: raise RuntimeError(f'{path} missing: {needle}')
if (root/'build/async-continuation-baseline.json').exists():
    raise SystemExit('Legacy async-continuation-baseline.json must not exist; raw-await grandfathering is forbidden.')
require('src/LocalGPT/LocalGPT.csproj','<Version>2.9.3</Version>')
require('src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj','<Version>2.9.3</Version>')
require('src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','<Version>2.9.3</Version>')
require('build/async-continuation-policy.json','"maxUnconfiguredAwaitExpressionCount": 0')
require('src/LocalGPT/Components/Pages/CouncilTeams.razor','_ = RefreshProviderModelsAsync();')
require('src/LocalGPT/Components/Pages/CouncilTeams.razor','ToggleDxFunctionPickerAsync')
require('src/LocalGPT/Components/Pages/CouncilTeams.razor','Provider discovery continues in the background.')
require('src/LocalGPT/Services/ProviderModelRuntimeService.cs','var ollamaProbeTasks = EnumerateOllamaProbeEndpoints(options)')
require('src/LocalGPT/Services/ProviderModelRuntimeService.cs','var openAiProbeTasks = EnumerateOpenAiCompatible(options)')
result=subprocess.run([sys.executable,str(root/'build/audit_async_continuations.py'),'--source-root',str(root/'src/LocalGPT')],text=True,capture_output=True)
print(result.stdout,end='')
if result.returncode:
    print(result.stderr,end='',file=sys.stderr); raise SystemExit(result.returncode)
print('LocalGPT 2.8.3 strict async/Council Teams responsiveness regression audit passed.')
