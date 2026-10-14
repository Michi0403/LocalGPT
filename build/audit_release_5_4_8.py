#!/usr/bin/env python3
"""Source-only release regression checks; does not require .NET or PowerShell."""
from __future__ import annotations

import json
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
application = root / "src/LocalGPT"
components = application / "Components"


def read(relative: str) -> str:
    return (root / relative).read_text(encoding="utf-8-sig")


def markup(relative: str) -> str:
    return read(relative).split("@code", 1)[0]


card = markup("src/LocalGPT/Components/Shared/HumanCollaborationRequestCard.razor")
chat = markup("src/LocalGPT/Components/Pages/Chat.razor")
inbox = markup("src/LocalGPT/Components/Layout/HumanCollaborationInbox.razor")
guard = read("build/Assert-DevExpressComponentRetention.ps1")
service = read("src/LocalGPT/Services/HardwarePerformancePresetService.cs")
doc_block = service.split("private ProviderModelBenchmarkProfileResult? SelectMeasuredTierProfile(", 1)[0][-700:]
contract = json.loads(read("build/devexpress-component-retention.json"))
checks = {
    "release version incremented": "<Version>5.4.8</Version>" in read("src/LocalGPT/LocalGPT.csproj"),
    "canonical request card remains on both surfaces": all(
        '<HumanCollaborationRequestCard @key="request.Id" Request="@request"' in page
        for page in (chat, inbox)
    ),
    "card retains top-level safety services": all(
        card.count("@inject " + directive) == 1
        for directive in (
            "ILogger<HumanCollaborationRequestCard> Logger",
            "INotificationService Notifier",
            "IComponentActivityService ComponentActivity",
        )
    ),
    "card retains approval/decline, feedback and suggestion decisions": all(
        token in card for token in ("SubmitAsync(true)", "SubmitAsync(false)", "SubmitAsync(null)", "ChooseSuggestion(suggestion)")
    ),
    "card retains DevExpress FormLayout editor hierarchy": all(
        token in card for token in ("<DxFormLayout CssClass=\"human-unified-editor\">", "<DxMemo", "<DxComboBox", "<DxCheckBox")
    ),
    "card preserves request audit metadata": all(
        token in card for token in ("Request.QuestionScope", "Request.GateMode", "Request.RequestedCouncilRound", "FingerprintPreview")
    ),
    "Chat decision history is a DevExpress accordion": "<DxAccordion" in chat and "<DxAccordionItem" in chat
    and 'data-testid="chat-shared-approval-history"' in chat,
    "Chat retains the original native disclosure maximum": len(re.findall(r"<\s*(?:details|summary)\b", chat, re.I)) <= 32,
    "composition retention is wired without lowering parent minima": all(
        "ComposedDevExpressComponents" in contract["Files"][relative]
        for relative in ("src/LocalGPT/Components/Pages/Chat.razor", "src/LocalGPT/Components/Layout/HumanCollaborationInbox.razor")
    ) and all(token in guard for token in ("PSDX0007", "$composedDxCount", "$baseline.ContainsKey($componentRelative)")),
    "no obsolete measured-tier XML parameters": all(
        token not in doc_block for token in ('<param name="target">', '<param name="successfulProfiles">', '<param name="profileIndex">')
    ) and doc_block.count('<param name="tierName">') == 1 and '<param name="profiles">' in doc_block,
}

# Recheck all existing retention minima and disclosure/native maxima with the new explicit
# component composition credit. This is intentionally not an updated or lowered baseline.
for relative, minimum in contract["Files"].items():
    text = markup(relative)
    direct = len(re.findall(r"<\s*Dx[A-Za-z0-9_]+", text, re.I))
    composed = 0
    for child in minimum.get("ComposedDevExpressComponents", []):
        child_relative = f"src/LocalGPT/Components/Shared/{child}.razor"
        checks[f"{relative}: composed {child} is present and independently protected"] = (
            re.search(r"<\s*" + re.escape(child) + r"\b", text, re.I) is not None
            and child_relative in contract["Files"]
            and (root / child_relative).is_file()
        )
        if (root / child_relative).is_file():
            composed += len(re.findall(r"<\s*Dx[A-Za-z0-9_]+", markup(child_relative), re.I))
    checks[f"{relative}: protected DevExpress count"] = direct + composed >= minimum["MinimumDevExpressTags"]
    checks[f"{relative}: native controls did not increase"] = (
        len(re.findall(r"<\s*(?:button|input|select|option|textarea|datalist|InputText|InputTextArea|InputCheckbox|InputNumber|InputDate|InputSelect)\b", text, re.I))
        <= minimum["MaximumNativeInteractiveTags"]
    )
    checks[f"{relative}: native disclosures did not increase"] = (
        len(re.findall(r"<\s*(?:details|summary)\b", text, re.I))
        <= minimum["MaximumNativeDisclosureTags"]
    )

failures = [name for name, passed in checks.items() if not passed]
if failures:
    raise SystemExit("LocalGPT 5.4.8 source audit FAILED:\n- " + "\n- ".join(failures))
print(f"LocalGPT 5.4.8 source audit passed: {len(checks)} checks, including every retained DevExpress component contract.")
