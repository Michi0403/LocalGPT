from __future__ import annotations

from pathlib import Path
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
failures: list[str] = []


def fail(message: str) -> None:
    failures.append(message)


def run(args: list[str]) -> None:
    result = subprocess.run(args, cwd=ROOT)
    if result.returncode:
        fail("command failed: " + " ".join(args))


project = ROOT / "src/LocalGPT/LocalGPT.csproj"
if not project.is_file():
    fail("missing src/LocalGPT/LocalGPT.csproj")
elif (ET.parse(project).findtext(".//Version") or "").strip() != "5.2.9":
    fail("LocalGPT.csproj version mismatch")

for rel in (
    "CHANGELOG-v5.2.9-DEVEXPRESS-UI-RETENTION-HARDENING.md",
    "VALIDATION-v5.2.9-source.md",
    "RELEASE.md",
):
    if not (ROOT / rel).is_file():
        fail(f"missing {rel}")

razor_root = ROOT / "src/LocalGPT"
razor_files = {p.relative_to(ROOT).as_posix() for p in razor_root.rglob("*.razor")}
for path in razor_root.rglob("*.razor"):
    source = path.read_text(encoding="utf-8-sig").split("@code", 1)[0]
    rel = path.relative_to(ROOT).as_posix()
    if re.search(r"<datalist\b", source, re.IGNORECASE):
        fail(f"{rel} contains native <datalist>; use a searchable DevExpress editor")
    if re.search(r"<input\b[^>]*\btype\s*=\s*['\"]color['\"]", source, re.IGNORECASE | re.DOTALL):
        fail(f"{rel} contains native input type=color; use DevExpress color UI")

manifest_path = ROOT / "build/devexpress-component-retention.json"
manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig")) if manifest_path.is_file() else {}
entries = manifest.get("Files", {})
if set(entries) != razor_files:
    missing = sorted(razor_files - set(entries))
    extra = sorted(set(entries) - razor_files)
    fail(f"DevExpress retention manifest coverage mismatch; missing={missing[:5]} extra={extra[:5]}")

# The only maintained raw action controls are pre-circuit reconnect/reload buttons in App.razor.
native_button_files: dict[str, int] = {}
for path in razor_root.rglob("*.razor"):
    source = path.read_text(encoding="utf-8-sig").split("@code", 1)[0]
    count = len(re.findall(r"<button\b", source, re.IGNORECASE))
    if count:
        native_button_files[path.relative_to(ROOT).as_posix()] = count
expected = {"src/LocalGPT/Components/App.razor": 2}
if native_button_files != expected:
    fail(f"LocalGPT native-button exception changed: expected {expected}, found {native_button_files}")

run([sys.executable, str(ROOT / "build/audit_devexpress_blazor_controls.py"), "--root", str(ROOT)])
run([sys.executable, str(ROOT / "build/audit_razor_maintenance_contract.py"), "--root", str(ROOT), "--product", "localgpt"])
run([sys.executable, str(ROOT / "build/audit_transient_ui_state_ownership.py"), "--root", str(ROOT), "--product", "localgpt"])

if failures:
    print("\n".join("FAIL: " + x for x in failures), file=sys.stderr)
    raise SystemExit(1)
print("LocalGPT 5.2.9 DevExpress UI retention release audit passed.")
