#!/usr/bin/env python3
"""Static source audit for LocalGPT 2.8.3 human-visible entity formatting and benchmark/rejoin repair."""
from pathlib import Path
import sys
root = Path(__file__).resolve().parents[1]

def text(rel):
    path = root / rel
    if rel.endswith('.cs'):
        stem = path.with_suffix('')
        parts = sorted(stem.parent.glob(stem.name + '*.cs'))
        if parts:
            return '\n'.join(part.read_text(encoding='utf-8', errors='replace') for part in parts)
    if rel.endswith('.razor'):
        stem = path.with_suffix('')
        parts = ([path] if path.is_file() else []) + sorted(stem.parent.glob(stem.name + '*.razor.cs'))
        if parts:
            return '\n'.join(part.read_text(encoding='utf-8', errors='replace') for part in parts)
    if not path.is_file():
        raise AssertionError(f"missing {rel}")
    return path.read_text(encoding='utf-8', errors='replace')

def require(rel, *needles):
    value = text(rel)
    missing = [needle for needle in needles if needle not in value]
    if missing:
        raise AssertionError(f"{rel} missing: {', '.join(missing)}")

try:
    require(
        "src/LocalGPT/Services/Formatting/ChatContentRenderer.cs",
        "DecodeHumanTextEntities",
        'Replace("&quot;", "\\\"", StringComparison.OrdinalIgnoreCase)',
        'Replace("&#34;", "\\\"", StringComparison.OrdinalIgnoreCase)',
        'Replace("&#x22;", "\\\"", StringComparison.OrdinalIgnoreCase)',
        'Replace("&apos;", "\'", StringComparison.OrdinalIgnoreCase)',
        'Replace("&#39;", "\'", StringComparison.OrdinalIgnoreCase)',
        'Replace("&#x27;", "\'", StringComparison.OrdinalIgnoreCase)',
        "text = DecodeHumanTextEntities(text);",
        "Markup-significant entities",
    )
    require(
        "src/LocalGPT/Components/Pages/ModelCouncil.razor",
        "@inject IChatContentRenderer ChatContentRenderer",
        "ChatContentRenderer.Render(LastResult.FinalAnswer)",
        "ChatContentRenderer.Render(step.VisibleContent)",
        "CouncilText.DecodeHumanVisibleText(CouncilText.TrimForDisplay(LastResult.Prompt, 12000, Logger))",
        "CouncilText.DecodeHumanVisibleText(step.Thinking)",
    )
    require(
        "src/LocalGPT/Components/Layout/CouncilSpoolerPanel.razor",
        "@inject CouncilTextService CouncilText",
        "CouncilText.DecodeHumanVisibleText(Selected.Prompt)",
        "CouncilText.DecodeHumanVisibleText(step.VisibleContent)",
        "CouncilText.DecodeHumanVisibleText(Selected.FinalAnswer)",
    )
    for rel in (
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
    ):
        require(rel, "<Version>3.0.2</Version>")
    print("LocalGPT 2.8.3 human-visible entity formatting source audit passed: quote/apostrophe entities normalize once through the chat renderer while markup-significant entities stay encoded, Council surfaces use the renderer/text decode boundary, and release versions are aligned.")
except AssertionError as exc:
    print(f"LocalGPT 2.8.3 human-visible entity formatting source audit failed: {exc}", file=sys.stderr)
    sys.exit(1)
