#!/usr/bin/env python3
from pathlib import Path
import hashlib
import sys

ROOT = Path(__file__).resolve().parents[1]
checks = []

def text(path):
    return (ROOT / path).read_text(encoding="utf-8")

def require(path, needle, label=None):
    data = text(path)
    if needle not in data:
        raise AssertionError(f"{path}: missing {label or needle!r}")
    checks.append(label or f"{path}:{needle[:48]}")

def forbid(path, needle, label=None):
    data = text(path)
    if needle in data:
        raise AssertionError(f"{path}: forbidden {label or needle!r}")
    checks.append(label or f"{path}:forbid:{needle[:48]}")

def hash_require(path, expected):
    actual = hashlib.sha256((ROOT / path).read_bytes()).hexdigest()
    if actual != expected:
        raise AssertionError(f"{path}: sha256 {actual} != protected {expected}")
    checks.append(f"hash:{path}")

try:
    # Protected Chat markup/layout from 3.1.10.
    hash_require("src/LocalGPT/Components/Pages/Chat.razor", "0d9ab6ed72f41eebbbf8839c54b5fda9a409d424a1fa11c87d2994352c837569")
    hash_require("src/LocalGPT/Components/Pages/Chat.razor.css", "2a620187aa41712f53dddab92ee2ab834c4f46fe512925dce94efb387f28b0e4")
    hash_require("src/LocalGPT/wwwroot/js/localgpt-chat-ui.js", "26a7609b73a450ae3643e922050bf8a821001be400b2ff93eb5ac07f3d1e817d")

    for p in ["src/LocalGPT/LocalGPT.csproj", "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj", "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj"]:
        require(p, "<Version>3.1.11</Version>", "3.1.11 package version")
    require("src/LocalGPT/Components/App.razor", "js/localgpt-chat-ui.js?v=3.1.11", "3.1.11 chat JS cache key")

    # Formatting boundary: normalize known envelopes before Markdown, never globally decode arbitrary prose.
    require("src/LocalGPT/Services/Formatting/ChatContentRenderer.cs", "TranslateSelfAssessmentBlocksToMarkdown", "self-assessment normalization before Markdown")
    require("src/LocalGPT/Services/Formatting/StructuredTextTranslationService.cs", "SelfAssessmentBlockPatternName", "database-backed self-assessment recognition")
    require("src/LocalGPT/Services/Formatting/StructuredTextTranslationService.cs", "JavaScriptEncoder.UnsafeRelaxedJsonEscaping", "readable Unicode JSON")
    require("src/LocalGPT/Services/Formatting/StructuredTextTranslationService.cs", "WebUtility.HtmlDecode(match.Groups[\"json\"].Value)", "single envelope decode")
    require("src/LocalGPT/Services/Formatting/StructuredTextTranslationService.cs", "mix the two names between opening and closing tags", "mismatched assessment envelope repair")
    require("src/LocalGPT/Services/Formatting/StructuredTextTranslationService.cs", "jsonText[^1] == '\\\\'", "trailing wrapper escape repair")
    require("src/LocalGPT/Services/Formatting/StructuredTextTranslationService.cs", "<pre><code class=\\\"language-json\\\">", "structured JSON code surface")
    forbid("src/LocalGPT/Services/Formatting/StructuredTextTranslationService.cs", "selfAssessmentBlockRegex = new Regex", "direct regex compilation outside regex service")
    require("src/LocalGPT/Services/CouncilRuntimeService.cs", "FormatJsonForUserVisibleCode", "provider/tool JSON display formatter")
    require("src/LocalGPT/Services/CouncilRuntimeService.cs", "WebUtility.HtmlDecode(element.GetString()", "display-only HTML entity decode in JSON strings")
    require("src/LocalGPT/Services/CouncilRuntimeService.cs", "WebUtility.HtmlEncode(formatted)", "final inert HTML encoding boundary")

    # Topic-neutral, persisted Learning team.
    require("src/LocalGPT/Services/CouncilTeamConfigurationService.cs", "private const int CurrentSeedVersion = 27;", "Learning seed evolution version")
    learning = text("src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs")
    for needle in [
        'Key = "learning-round"',
        "topic-neutral, evidence-driven council preset",
        "AllMembersReadinessPreflightMode = CouncilAllMembersReadinessPreflightMode.Disabled",
        'Step("learning-inventory"',
        'Step("learning-study"',
        'Step("learning-verify"',
        'Step("learning-maintain"',
        "chat.upload_workspace_files",
        "chat.upload_workspace_file",
        "localgpt.learning.maintain",
        "Knowledge is the primary outcome",
        "do not impose a coding/project frame",
    ]:
        if needle not in learning:
            raise AssertionError(f"Learning seed missing {needle!r}")
        checks.append(f"learning:{needle}")
    require("src/LocalGPT/Services/CouncilTeamConfigurationService.Seeding.cs", "IsUserModified", "user-modified team preservation path")
    require("src/LocalGPT/Services/CouncilTeamConfigurationService.Seeding.cs", "preserved", "seed evolution preservation diagnostics")

    # Session-owned settings captured and restored on live rejoin.
    for needle in ["CouncilTeamKey", "ModelPresetId", "HardwarePerformancePresetId", "CritiqueRounds", "IncludeMemory", "CreateProjectPerRun"]:
        require("src/LocalGPT/BusinessObjects/CouncilRunConfigurationModels.cs", needle, f"snapshot:{needle}")
        require("src/LocalGPT/BusinessObjects/CouncilServiceStateModels.cs", needle, f"state:{needle}")
    require("src/LocalGPT/BusinessObjects/MultiModelCouncilModels.cs", "ModelPresetId", "request model preset identity")
    require("src/LocalGPT/BusinessObjects/MultiModelCouncilModels.cs", "HardwarePerformancePresetId", "request performance preset identity")
    require("src/LocalGPT/Components/Pages/Chat.ProviderRuntime.razor.cs", "ModelPresetId = SelectedModelPreset?.Id", "capture selected model preset")
    require("src/LocalGPT/Components/Pages/Chat.ProviderRuntime.razor.cs", "HardwarePerformancePresetId = SelectedHardwarePerformancePreset?.Id", "capture selected performance preset")
    require("src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs", "ApplyRejoinedCouncilPreparationSnapshot(runConfigurationSnapshot)", "renderer-side rejoin restoration")
    require("src/LocalGPT/Components/Pages/Chat.PresetsAndCouncilConfiguration.razor.cs", "CouncilRunConfigurations.GetPreparation()?.HardwarePerformancePresetId", "hardware preset load-order recovery")
    require("src/LocalGPT/Components/Pages/Chat.PresetsAndCouncilConfiguration.razor.cs", "private void ApplyRejoinedCouncilPreparationSnapshot", "rejoin configuration mapper")
    require("src/LocalGPT/Services/CouncilRunConfigurationService.cs", "UpdateHardwarePerformancePresetIdentity", "running performance preset identity update")
    require("src/LocalGPT/Services/HardwarePerformancePresetService.cs", "UpdateHardwarePerformancePresetIdentity(runId, preset.Id)", "running preset service identity persistence")

    print(f"LocalGPT 3.1.11 Council formatting/Learning/session restore audit passed: {len(checks)} checks.")
except AssertionError as exc:
    print(f"LocalGPT 3.1.11 Council formatting/Learning/session restore audit failed: {exc}", file=sys.stderr)
    raise SystemExit(1)
