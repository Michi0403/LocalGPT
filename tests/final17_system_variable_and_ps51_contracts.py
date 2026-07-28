from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[1]
BUILD = ROOT / "build"
SOURCE = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT"

for name in ("Assert-MethodDiagnostics.ps1", "Assert-IteratorExceptionPolicy.ps1"):
    text = (BUILD / name).read_text(encoding="utf-8-sig")
    assert "New-Object System.Collections.Generic.List[object]" not in text, name
    assert "return @($records)" not in text, name
    assert "$records = @()" in text, name
    assert "return $records" in text, name

policy = (BUILD / "Assert-SystemVariableInitialization.ps1").read_text(encoding="utf-8-sig")
assert "direct-system-variable-name" in policy
assert "SystemVariableDefinitionService.cs" in policy
assert (BUILD / "system-variable-initialization-baseline.json").is_file()
json.loads((BUILD / "system-variable-initialization-baseline.json").read_text(encoding="utf-8"))

targets = (ROOT / "Directory.Build.targets").read_text(encoding="utf-8-sig")
assert "AssertLocalGptSystemVariableInitialization" in targets
assert "Assert-SystemVariableInitialization.ps1" in targets

visibility = (BUILD / "Assert-GitSourceVisibility.ps1").read_text(encoding="utf-8-sig")
assert "build/Assert-SystemVariableInitialization.ps1" in visibility
assert "build/system-variable-initialization-baseline.json" in visibility

service = (SOURCE / "Services" / "Persistence" / "SystemVariableDefinitionService.cs").read_text(encoding="utf-8-sig")
interface = (SOURCE / "Interfaces" / "ISystemVariableDefinitionService.cs").read_text(encoding="utf-8-sig")
catalog = (SOURCE / "Services" / "Persistence" / "InitialDataCatalog.cs").read_text(encoding="utf-8-sig")
chat = (SOURCE / "Components" / "Pages" / "Chat.razor").read_text(encoding="utf-8-sig")
composite = (SOURCE / "Services" / "CompositeChatClient.cs").read_text(encoding="utf-8-sig")
program = (SOURCE / "Program.cs").read_text(encoding="utf-8-sig")

for key in (
    "DefaultMaxOutputTokens", "DefaultContextTokens", "DefaultCouncilResourceLoadPercent",
    "DefaultOllamaEndpoint", "CouncilDefaultsVersion"
):
    assert key in service
assert "IReadOnlyList<InitialVariable> InitialValues" in interface
assert "Variables => systemVariables.InitialValues" in catalog
assert 'VariableStore.GetAsync<int>("' not in chat
assert 'VariableStore.SetAsync("' not in chat
assert '.GetAsync<int>("DefaultMaxOutputTokens"' not in composite
assert "_systemVariables.DefaultMaxOutputTokens" in composite
assert "AddSingleton<ISystemVariableDefinitionService, SystemVariableDefinitionService>" in program

print("PASS final17 PowerShell 5.1 method-record handling and LocalGPT system-variable ownership contracts.")
