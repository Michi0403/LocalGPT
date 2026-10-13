"""Source-only maintenance contract for 5.6.3 terminal, fullscreen and per-model memory repairs.

No dotnet, NuGet restore, external service, or shell command execution is required.
"""

from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


def source(name: str) -> str:
    return (ROOT / name).read_text(encoding="utf-8")


def require(condition: bool, reason: str) -> None:
    if not condition:
        raise SystemExit(f"LocalGPT 5.6.3 console/memory acceptance failed: {reason}")


def section(contents: str, beginning: str, ending: str) -> str:
    require(beginning in contents and ending in contents, f"missing maintained source section {beginning}")
    return contents.split(beginning, 1)[1].split(ending, 1)[0]


require("<Version>5.6.3</Version>" in source("src/LocalGPT/LocalGPT.csproj"), "version not advanced")
console = source("src/LocalGPT/Services/ConsoleCommandService.cs")
pump = section(console, "private async Task PumpOutputAsync(", "private void PublishOutputFrame(")
require("pendingCarriageReturn" in pump and "character != '\\n'" in pump, "CRLF still discarded as terminal rewrites")
require("if (character == '\\r')" in pump and "if (character == '\\n')" in pump, "stream-boundary newline handling missing")
display = section(console, "private string BuildDisplayCommand(", "private string DescribeShell(")
require('string.Equals(label, "ASCII operator shell", StringComparison.Ordinal)' in display, "human command echo not narrowly scoped")
require("request.UserConfirmed" in display and 'request.CommandText.Trim()' in display, "confirmed typed command not echoed")
require("request.IsReadOnly" in display, "programmatic command privacy boundary removed")

razor = source("src/LocalGPT/Components/Shared/ChatGameConsole.razor")
require("showRevisionPicker" in razor and "if (playableRevisions.Count > 0)" in razor, "empty Game Studio list still reserves header height")
require("<DxListBox" in razor and "ToggleRevisionPicker" in razor, "DevExpress revision control lost")
require('ValueFieldName="@nameof(LocalConsoleShellDescriptor.Shell)"' in razor, "shell selector still uses object identity")
require('Value="@operatorShell"' in razor and "ChangeOperatorShell(LocalConsoleShellKind shell)" in razor, "shell selection does not bind to stable enum")
require("chat-game-fullscreen-toggle" in razor and "chat-game-guide-toggle" in razor, "fullscreen and guide controls not retained")

console_css = source("src/LocalGPT/Components/Shared/ChatGameConsole.razor.css")
require("flex: 0 0 auto" in console_css and ".chat-game-revision-picker.is-open" in console_css, "compact closed picker styles absent")
require(".chat-game-console.localgpt-game-fullscreen .chat-game-console-header" in console_css, "immersive fullscreen header style missing")
site_css = source("src/LocalGPT/wwwroot/css/site.css")
require(":has(.chat-game-popup-body)" in site_css and "scrollbar-gutter: auto !important" in site_css, "normal DevExpress popup fill regression")
require("grid-template-rows: minmax(0, 1fr) auto !important" in site_css, "fullscreen grid still reserves a header row")

models = source("src/LocalGPT/BusinessObjects/InitialSetupAssistantModels.cs")
for field in ("EstimatedGpuVramGiB", "EstimatedSystemRamGiB", "EstimatedUnifiedMemoryGiB", "MemoryPlanSummary"):
    require(field in models, f"source-backed recommendation missing {field}")
memory = source("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs")
require("PopulateModelMemoryPlan(model, device)" in memory, "model memory split not populated by host-specific service")
require("model.RequiredVramGiB is not > 0" in memory, "missing source requirements must stay unknown")
require("device.DedicatedVramGiB == 0" in memory, "shared/CPU-only host handling missing")
setup = source("src/LocalGPT/Services/InitialSetupAssistantService.cs")
require("MemoryPlanSummary = item.MemoryPlanSummary" in setup, "multi-host recommendation copy loses memory estimate")
panel = source("src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor")
for label in ("Per-model memory budget", "PC RAM:", "Dedicated GPU VRAM:", "Unified/shared memory:"):
    require(label in panel, f"Install does not display {label}")
require("CanIRunModelRecommendation.MemoryPlanSummary" in panel, "model results grid lacks estimated memory")
require((ROOT / "RELEASE-CHECKLIST-v5.6.0.md").is_file(), "per-file release checklist missing")
print("LocalGPT 5.6.3 ASCII console, CRLF output, typed shell selection, compact revision list, fullscreen popup and attributed per-model memory source audit passed.")
