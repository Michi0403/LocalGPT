from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT"

def read(path): return (APP / path).read_text(encoding="utf-8-sig")

layout = read("Components/Layout/MainLayout.razor")
layout_css = read("Components/Layout/MainLayout.razor.css")
chat = read("Components/Pages/Chat.razor")
chat_css = read("Components/Pages/Chat.razor.css")
chat_js = read("wwwroot/js/localgpt-chat-ui.js")
context = read("wwwroot/js/localgpt-context-menu.js")
service = read("Services/CouncilTextService.cs")
assert "localgpt-assistant-rail" in layout and "pointer-events: none" in layout_css
assert "findComposer" in chat_js and "rect.height <=" in chat_js
assert "z-index: 3 !important" in chat_css
assert 'anchor.dataset.enhanceNav = "false"' in context
method_start = chat.index("private async Task OnChatToolbarItemClick")
assert "try" in chat[method_start:method_start+1400]
assert "BuildArchitecturePollMessage" in service and "ParseModelNames" in service
for guard in ["Assert-MethodDiagnostics.ps1","Assert-ApplicationStaticPolicy.ps1","Assert-TextServiceOwnership.ps1","Assert-IteratorExceptionPolicy.ps1"]:
    assert (ROOT / "build" / guard).exists(), guard
print("PASS LocalGPT final15 overlay, chat wiring, text-service ownership, and safeguards.")
