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
        checks.append((f"file exists: {relative}", path.is_file()))
        return path.read_text(encoding="utf-8") if path.is_file() else ""

    chat = read("src/LocalGPT/Components/Pages/Chat.razor")
    console = read("src/LocalGPT/Components/Shared/ChatGameConsole.razor")
    css = read("src/LocalGPT/Components/Shared/ChatGameConsole.razor.css")
    js = read("src/LocalGPT/wwwroot/js/localgpt-game-console.js")
    project = read("src/LocalGPT/LocalGPT.csproj")
    english = read("src/LocalGPT/Localization/en-US.json")
    german = read("src/LocalGPT/Localization/de-DE.json")

    checks.extend([
        ("Chat supplies the close callback", 'CloseRequested="CloseGameConsole"' in chat),
        ("Chat removes only the game surface", "private void CloseGameConsole() => showGameConsole = false;" in chat),
        ("console exposes close event callback", "[Parameter] public EventCallback CloseRequested" in console),
        ("close button is outside snapshot-only action branch", '                <button type="button" @onclick="FullscreenAsync">Fullscreen</button>\n            }\n            <button type="button"\n                    class="chat-game-console-close"' in console),
        ("close exits fullscreen before callback", console.index("localGptGameConsole.exitFullscreen") < console.index("CloseRequested.InvokeAsync")),
        ("fullscreen exit is one-way", "async exitFullscreen(id)" in js and "document.fullscreenElement === element" in js),
        ("close action has responsive/fullscreen styling", ".chat-game-console-close" in css and ":fullscreen .chat-game-console-close" in css),
        ("accessible close label is localized in English", '"Text.Close␠ASCII␠game␠console": "Close ASCII game console"' in english),
        ("accessible close label is localized in German", '"Text.Close␠ASCII␠game␠console": "ASCII-Spielkonsole schließen"' in german),
        ("application version advanced", any(f"<Version>2.3.{minor}</Version>" in project for minor in range(8, 100))),
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
