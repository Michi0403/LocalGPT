from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CHAT = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT" / "Components" / "Pages" / "Chat.razor"
BUILD = ROOT / "build"

text = CHAT.read_text(encoding="utf-8-sig")
for signature in (
    "private void OnFeedbackTargetChanged(ChangeEventArgs args)",
    "private void LoadFeedbackEditor()",
    "private async Task ClearSelectedFeedbackAsync()",
):
    assert signature in text, signature
    start = text.index(signature)
    body = text[start : start + 2200]
    assert "try" in body, signature
    assert "catch (Exception" in body, signature
    assert "Logger.Log" in body, signature
clear_start = text.index("private async Task ClearSelectedFeedbackAsync()")
assert "ConfigureAwait(true)" in text[clear_start : clear_start + 1200]

logging_guard = (BUILD / "Assert-LoggingIntegrity.ps1").read_text(encoding="utf-8-sig")
assert "bin|obj|artifacts|node_modules|build" in logging_guard

for name in (
    "Assert-MethodDiagnostics.ps1",
    "Assert-ApplicationStaticPolicy.ps1",
    "Assert-TextServiceOwnership.ps1",
    "Assert-IteratorExceptionPolicy.ps1",
):
    guard = (BUILD / name).read_text(encoding="utf-8-sig")
    assert "TrimStart('\\\\','/')" not in guard, name
    assert "TrimStart([char[]]@([char]'\\', [char]'/'))" in guard, name
    assert "Replace([char]'\\', [char]'/')" in guard, name

print("PASS final16 feedback handlers, build-workspace exclusion, and PowerShell 5.1 guard path handling.")
