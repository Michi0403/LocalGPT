#!/usr/bin/env python3
"""Static release gate for the removable /chat ASCII game console."""
from __future__ import annotations
import argparse
from pathlib import Path


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True)
    root = parser.parse_args().root.resolve()
    checks: list[tuple[str, bool]] = []

    def read(relative: str) -> str:
        path = root / relative
        if relative.endswith('.razor'):
            stem = path.with_suffix('')
            parts = ([path] if path.is_file() else []) + sorted(stem.parent.glob(stem.name + '*.razor.cs'))
            checks.append((f"logical component exists: {relative}", bool(parts)))
            return '\n'.join(part.read_text(encoding='utf-8') for part in parts)
        checks.append((f"file exists: {relative}", path.is_file()))
        return path.read_text(encoding="utf-8") if path.is_file() else ""

    chat = read("src/LocalGPT/Components/Pages/Chat.razor")
    console = read("src/LocalGPT/Components/Shared/ChatGameConsole.razor")
    css = read("src/LocalGPT/Components/Shared/ChatGameConsole.razor.css")
    chat_css = read("src/LocalGPT/Components/Pages/Chat.razor.css")
    js = read("src/LocalGPT/wwwroot/js/localgpt-game-console.js")
    project = read("src/LocalGPT/LocalGPT.csproj")
    english = read("src/LocalGPT/Localization/en-US.json")
    german = read("src/LocalGPT/Localization/de-DE.json")

    checks.extend([
        ("Chat supplies the close callback", 'CloseRequested="CloseGameConsole"' in chat),
        ("Chat closes only the ASCII surface and republishes presentation state", "private void CloseGameConsole()" in chat and "showGameConsole = false;" in chat and "UpdateAsciiExperienceState();" in chat),
        ("console exposes close event callback", "[Parameter] public EventCallback CloseRequested" in console),
        ("close button is outside snapshot-only action branch", '            }\n            <DxButton Text="× Close"\n                      CssClass="ascii-console-button chat-game-console-close"' in console),
        ("close exits fullscreen before callback", console.index("localGptGameConsole.exitFullscreen") < console.index("CloseRequested.InvokeAsync")),
        ("fullscreen exit targets the same popup-aware host", "async exitFullscreen(id)" in js and "const host = fullscreenHost(element);" in js and "document.fullscreenElement === host" in js),
        ("close action has responsive/fullscreen styling", ".chat-game-console-close" in css and ":fullscreen .chat-game-console-close" in css),
        ("DevExpress mode-button CssClass values are pure Razor expressions",
         'CssClass="ascii-console-button ascii-operator-toggle @(' not in console
         and console.count('CssClass="@("ascii-console-button ascii-operator-toggle" +') == 3),
        ("renderer-affine control-mode update keeps an explicit true continuation",
         "ChangeControlModeAsync" in console and "snapshot.AutoplayDelayMilliseconds).ConfigureAwait(true);" in console),
        ("renderer-affine fullscreen JS keeps an explicit true continuation",
         'localGptGameConsole.fullscreen", elementId).ConfigureAwait(true)' in console),
        ("renderer-affine close callback keeps an explicit true continuation",
         'CloseRequested.InvokeAsync()).ConfigureAwait(true)' in console),
        ("popup game stage stretches inside the popup grid instead of forcing 100 percent overflow",
         ".chat-game-popup-body ::deep .chat-game-stage" in chat_css and "height: auto;" in chat_css),
        ("popup viewport does not reserve an empty scrollbar gutter",
         ".chat-game-popup-body ::deep .chat-game-screen-viewport" in chat_css and "scrollbar-gutter: auto;" in chat_css),
        ("accessible close label is localized in English", '"Text.Close␠ASCII␠game␠console": "Close ASCII game console"' in english),
        ("accessible close label is localized in German", '"Text.Close␠ASCII␠game␠console": "ASCII-Spielkonsole schließen"' in german),
        ("application version advanced", (lambda match: bool(match) and tuple(map(int, match.groups())) >= (2, 3, 8))(
            __import__("re").search(r"<Version>(\d+)\.(\d+)\.(\d+)</Version>", project))),
    ])

    failed = [name for name, ok in checks if not ok]
    if failed:
        for name in failed:
            print(f"FAIL: {name}")
        print(f"Chat ASCII-console audit failed: {len(failed)}/{len(checks)} checks failed.")
        return 1
    print(f"Chat ASCII-console audit passed: {len(checks)} checks.")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
