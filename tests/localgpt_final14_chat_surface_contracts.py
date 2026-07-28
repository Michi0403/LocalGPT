from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8-sig")

chat_css = read("LocalGPTWebviewWrapper/LocalGPT/Components/Pages/Chat.razor.css")
chat_js = read("LocalGPTWebviewWrapper/LocalGPT/wwwroot/js/localgpt-chat-ui.js")
inbox_css = read("LocalGPTWebviewWrapper/LocalGPT/Components/Layout/HumanCollaborationInbox.razor.css")

assert "localgpt-prompt-suggestion" in chat_js
assert "localgpt-prompt-suggestion" in chat_css
assert "-webkit-text-fill-color: currentColor" in chat_css
assert ".localgpt-chat-composer" in chat_css and "z-index: 1092" in chat_css
assert "bottom: 6.5rem" in inbox_css
assert "pointer-events: none" in inbox_css
assert ".human-approval-bar-actions" in inbox_css and "pointer-events: auto" in inbox_css
print("PASS LocalGPT final14 readable prompt suggestions and non-blocking approval surfaces.")
