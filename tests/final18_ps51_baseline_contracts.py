from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BUILD = ROOT / "build"

GUARDS = (
    "Assert-MethodDiagnostics.ps1",
    "Assert-IteratorExceptionPolicy.ps1",
    "Assert-ApplicationStaticPolicy.ps1",
    "Assert-TextServiceOwnership.ps1",
    "Assert-SystemVariableInitialization.ps1",
)

for name in GUARDS:
    text = (BUILD / name).read_text(encoding="utf-8-sig")
    assert "$parsedBaseline =" in text, name
    assert "foreach ($item in $parsedBaseline)" in text, name
    assert "$baseline = @([System.IO.File]::ReadAllText" not in text, name

method_baseline = (BUILD / "method-diagnostics-baseline.json").read_text(encoding="utf-8")
assert "AmbientLocalGptContextSnapshot" in method_baseline

visibility = (BUILD / "Assert-GitSourceVisibility.ps1").read_text(encoding="utf-8-sig")
assert "tests/final18_ps51_baseline_contracts.py" in visibility

print("PASS final18 Windows PowerShell 5.1 JSON-array baseline enumeration contracts.")
