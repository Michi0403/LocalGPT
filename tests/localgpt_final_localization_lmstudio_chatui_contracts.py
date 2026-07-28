from __future__ import annotations
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT"

def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)

en = json.loads((APP / "Localization/en-US.json").read_text(encoding="utf-8"))
de = json.loads((APP / "Localization/de-DE.json").read_text(encoding="utf-8"))
require(set(en) == set(de), "LocalGPT English and German catalogs must have identical keys.")
require(len(en) >= 640, "LocalGPT localization catalog unexpectedly lost coverage.")
for source, expected in {
    "Start new chat": "Neuen Chat starten",
    "Send message": "Nachricht senden",
    "Former model thoughts": "Frühere Modellgedanken",
    "Install - Configure AI Connectivity": "Installieren – KI-Verbindung konfigurieren",
}.items():
    key = "Text." + source.replace(" ", "␠")
    require(de.get(key) == expected, f"Missing verified German translation for {source!r}.")

chat = (APP / "Components/Pages/Chat.razor").read_text(encoding="utf-8")
require("<DxToolbar" in chat and "<DxToolbarItem" in chat, "Chat commands must use the adaptive DevExpress toolbar.")
require("OnChatToolbarItemClick" in chat, "Chat toolbar must have a single explicit command dispatcher.")
require("<DxRibbon" not in chat, "A page-local Ribbon must not overlap LocalGPT's existing application menu.")
require("HtmlDecode(System.Net.WebUtility.HtmlDecode" in chat, "Former thoughts must decode historical encoded wrappers.")
require("(?:pre|code)" in chat and "former-thought-content" in chat, "Former thoughts must remove pre/code wrappers and preserve text formatting.")

css = (APP / "wwwroot/css/localgpt-theme-contract.css").read_text(encoding="utf-8")
require("font-size: clamp(16px" in css, "LocalGPT must retain its accessible adaptive base font size.")
require(".localgpt-send-button" in css and "4.75rem" in css, "The AI Chat send control must remain a large primary touch target.")
require(".localgpt-upload-button" in css and "4rem" in css, "The AI Chat upload control must remain a large touch target.")

ui = (APP / "wwwroot/js/localgpt-chat-ui.js").read_text(encoding="utf-8")
for marker in ["localgpt-chat-composer", "localgpt-send-button", "localgpt-upload-button", "localgpt-prompt-suggestion", "Message to AI assistant"]:
    require(marker in ui, f"Production AI Chat enhancer is missing {marker}.")

localization = (APP / "wwwroot/js/localgpt-localization.js").read_text(encoding="utf-8")
require("return complete ? translated : value" in localization, "Missing translations must not produce mixed-language labels.")

localization_service = (APP / "Services/Localization/LocalGptLocalizationService.cs").read_text(encoding="utf-8")
require("ILogger<LocalGptLocalizationService>" in localization_service, "LocalGPT localization service must participate in the structured logging policy.")
require("logger.Log" in localization_service and "catch (" in localization_service, "LocalGPT localization service must retain structured diagnostics and catch/log boundaries.")

factory = (APP / "Services/ChatClientFactory.cs").read_text(encoding="utf-8")
require('endpoint.TrimEnd(\'/\') + "/models"' in factory, "Local OpenAI-compatible providers must discover their actual model ids.")
require("LM Studio / OpenAI-compatible" in factory, "LM Studio must remain a first-class local OpenAI-compatible chat provider.")
require("ResolveOpenAiCompatibleModel" in factory, "LM Studio model discovery guard is missing.")
settings = json.loads((APP / "appsettings.json").read_text(encoding="utf-8"))
require(settings["AICore"]["ChatGPTLocalCore"]["Endpoint"] == "http://localhost:11434/v1", "Fresh local OpenAI-compatible setup must use Ollama's working endpoint by default.")
require('"http://localhost:1234/v1"' in factory, "LM Studio port 1234 must remain available as an automatic fallback.")


# Every maintained Razor label/tooltip that can be identified statically is registered in
# the external catalog. This prevents newly hardcoded UI from silently reintroducing mixed language.
import html as _html
import re as _re
_attr = _re.compile(r'\b(?:Text|Title|Tooltip|title|placeholder|aria-label|DropDownCaption)="([^"@{}]+)"')
_node = _re.compile(r'>\s*([^<@{}][^<{}]{1,240}?)\s*<')
for razor in (APP / "Components").rglob("*.razor"):
    markup = razor.read_text(encoding="utf-8-sig", errors="ignore").split("@code", 1)[0]
    candidates = [m.group(1) for m in _attr.finditer(markup)] + [m.group(1) for m in _node.finditer(markup)]
    for raw in candidates:
        source = _re.sub(r'\s+', ' ', _html.unescape(raw)).strip()
        if len(source) < 2 or len(source) > 240 or not _re.search('[A-Za-z]', source):
            continue
        if any(token in source for token in ('@', '{', '}', '=>', '="', ');', '?.', '??')) or '=' in source or ';' in source:
            continue
        key = "Text." + source.replace(" ", "␠")
        require(key in en, f"Uncatalogued LocalGPT UI text in {razor.relative_to(APP)}: {source!r}")

print("PASS LocalGPT adaptive toolbar, localization, former-thought, LM Studio and large composer contracts.")
