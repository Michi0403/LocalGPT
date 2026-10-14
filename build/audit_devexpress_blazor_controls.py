#!/usr/bin/env python3
from __future__ import annotations
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMPONENTS = ROOT / "src" / "LocalGPT" / "Components"
ALLOWED = {
    "src/LocalGPT/Components/App.razor": {"data-localgpt-reconnect", "data-localgpt-reload"},
}
PATTERNS = [
    (re.compile(r"<\s*button\b", re.I), "DxButton"),
    (re.compile(r"<\s*textarea\b", re.I), "DxMemo"),
    (re.compile(r"<\s*select\b", re.I), "DxComboBox/DxListBox/DxTreeView"),
    (re.compile(r"<\s*datalist\b", re.I), "DxComboBox/DxListBox"),
    (re.compile(r"<\s*option\b", re.I), "DevExpress data item"),
    (re.compile(r"<\s*input\b", re.I), "DevExpress editor"),
    (re.compile(r"<\s*Input(?:Text|TextArea|Checkbox|Number|Date|Select)\b", re.I), "DevExpress editor"),
]

def main() -> int:
    violations: list[str] = []
    for path in sorted(COMPONENTS.rglob("*.razor")):
        rel = path.relative_to(ROOT).as_posix()
        for line_no, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            allowed_markers = ALLOWED.get(rel, set())
            if any(marker in line for marker in allowed_markers):
                continue
            for pattern, replacement in PATTERNS:
                match = pattern.search(line)
                if match:
                    violations.append(
                        f"{rel}:{line_no}:{match.start()+1}: native/legacy control; use {replacement}: {line.strip()}"
                    )

        content = path.read_text(encoding="utf-8")
        for match in re.finditer(r"<DxGridLayoutItem\b[^>]*>(.*?)</DxGridLayoutItem\s*>", content, re.I | re.S):
            body = match.group(1)
            if re.match(r"\s*<Template(?:\s|>)", body, re.I):
                continue
            line_no = content.count("\n", 0, match.start()) + 1
            violations.append(
                f"{rel}:{line_no}:1: DxGridLayoutItem must wrap content in <Template>; implicit ChildContent fails at render time"
            )
    if violations:
        print("DevExpress Blazor control audit failed:")
        print("\n".join(f"  - {item}" for item in violations))
        return 1
    print("DevExpress Blazor control audit passed: only the two circuit-independent App.razor reconnect controls remain native and all DxGridLayoutItem content uses explicit Template children.")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
