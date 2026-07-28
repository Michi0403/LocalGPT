from pathlib import Path
import json
import re
import sys

ROOT = Path(__file__).resolve().parents[1]

def read(path):
    target = ROOT / path
    assert target.exists(), f"missing {path}"
    return target.read_text(encoding="utf-8-sig")

build = read("Build-LocalDevelopment.ps1")
release = read("Build-Release.ps1")
wrapper = read("LocalGPTWebviewWrapper/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj")
architecture_guard = read("build/Assert-OneWireArchitecture.ps1")
app_project = read("LocalGPTWebviewWrapper/LocalGPT/LocalGPT.csproj")
installer = read("LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Program.cs")
installer_project = read("LocalGPTWebviewWrapper/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj")
start_cmd = read("LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Start.cmd")
install_cmd = read("LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Install.cmd")
update_cmd = read("LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Update.cmd")
uninstall_cmd = read("LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Uninstall.cmd")
program = read("LocalGPTWebviewWrapper/LocalGPT/Program.cs")
chat = read("LocalGPTWebviewWrapper/LocalGPT/Components/Pages/Chat.razor")
model_council = read("LocalGPTWebviewWrapper/LocalGPT/Components/Pages/ModelCouncil.razor")
chat_css = read("LocalGPTWebviewWrapper/LocalGPT/Components/Pages/Chat.razor.css")
layout = read("LocalGPTWebviewWrapper/LocalGPT/Components/Layout/MainLayout.razor")
human = read("LocalGPTWebviewWrapper/LocalGPT/Components/Layout/HumanCollaborationInbox.razor")
council = read("LocalGPTWebviewWrapper/LocalGPT/Services/MultiModelCouncilService.cs")
composite = read("LocalGPTWebviewWrapper/LocalGPT/Services/CompositeChatClient.cs")
controller = read("LocalGPTWebviewWrapper/LocalGPT/Controller/OneWireHttpController.cs")
settings = json.loads(read("LocalGPTWebviewWrapper/LocalGPT/appsettings.json"))

assert "Packing the RID-neutral protocol for WinUI/package graph isolation" in build
assert "Rebinding LocalGPT to the RID-neutral package graph for WinUI metadata resolution" in build
assert "-t:Rebuild" in build
assert re.search(r'wrapperProperties[\s\S]*UseLocalWireProtocolProject=false', build)
assert re.search(r'wrapperProperties[\s\S]*BuildProjectReferences=false', build)
assert "UseLocalWireProtocolProject=true" not in release
assert re.search(r'Building the optional WinUI wrapper[\s\S]*UseLocalWireProtocolProject=false', release)
assert "LocalGPT.WireProtocolVersion" not in wrapper
assert "<Nullable>enable</Nullable>" in wrapper
assert "GlobalPropertiesToRemove=\"Platform;PlatformTarget;RuntimeIdentifier;RuntimeIdentifiers" in wrapper
assert "SelectNodes(\"/Project/PropertyGroup/RuntimeIdentifier | /Project/PropertyGroup/RuntimeIdentifiers\")" in architecture_guard
assert "if ($protocolProject -match 'RuntimeIdentifier')" not in architecture_guard
assert "WaitForRuntimeEndpoint" in installer
assert "TryGetRunningEndpoint" in installer
assert "LocalGPT is already running: {existingUrl}" in installer
assert "LocalGPT is ready: {url}" in installer
assert '<None Update="*.cmd" CopyToOutputDirectory="Always" CopyToPublishDirectory="Always" />' in installer_project
assert "--start-localgpt --port 5000" in start_cmd
assert "--install-localgpt --install-ollama --pull-models --range Slim --shortcuts --start-localgpt --port 5000" in install_cmd
assert "--force-delete" not in install_cmd
assert "--force-delete" not in update_cmd
assert "--uninstall --force-delete" in uninstall_cmd
assert 'Thread.Sleep(TimeSpan.FromSeconds(2))' not in installer
assert "LocalGPT listening on {BaseUrl}" in program
assert "using LocalGPT.Services.OneWire;" in controller
assert "council-command-center" in chat
assert "chat-memory-project-center" in chat
assert "CouncilResourceLoadPercent { get; set; } = 100" in chat
assert re.search(r'MultiModelCouncilRequest Request[\s\S]*?MaxOutputTokens = 262144,[\s\S]*?ResourceLoadPercent = 100,[\s\S]*?MaxContextTokens = 262144', model_council)
assert "Request.OllamaNumGpu = null;" in model_council
assert "CouncilCritiqueRounds { get; set; } = 1" in chat
assert "MaxRounds = Math.Clamp(CouncilCritiqueRounds, 0, 3)" in chat
assert "IsUnavailableConfiguredOllamaSession" in chat
assert "SelectReachableFallbackSession" in chat
assert "configured but not reachable" in chat
assert "prompt-suggestion" in chat_css and "send-button" in chat_css
assert "<Drawer" in layout and "main-command-ribbon" not in layout
assert "<DxToolbar" in chat and "<DxRibbon" not in chat
assert "Approvals & team" in human
assert "RetryParticipantWithSafeLimitsAsync" in council
assert "ApplyApprovedOneRunModelExclusionsAsync" in council
assert "QueueModelHealthExclusionReviewAsync" in council
assert "AppendRuntimeBenchmarkSummary" in council
assert "OrderParticipantsByObservedHealth" in council
assert "does not silently rewrite user-approved hardware roads" in council
assert 'streamUpdate?.Invoke($"\n' not in council
assert 'new CouncilHardwareRoadPlan(modelName, OneWireHardwareKind.Auto, -1, "Automatic", $"auto:{modelName}", 100' in council
assert "Retrying streaming chat once" in composite
assert "var retryUpdates = session.Client" in composite
assert "retryUpdates.MoveNextAsync" in composite
assert "await foreach (var update in session.Client" not in composite
assert settings["LocalGPT"]["Port"] == 5000
assert settings["OneWire"]["ServicePort"] == 51140
assert settings["OneWire"]["DiscoveryPort"] == 51141
assert settings["OneWire"]["BroadcastAddress"] == "255.255.255.255"
print("PASS LocalGPT final script-build, startup URL, Council resilience, UI and 1-Wire safeguards.")
