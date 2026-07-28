from __future__ import annotations

import hashlib
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8-sig").replace("\r\n", "\n").replace("\r", "\n")


def normalized_hash(relative: str) -> str:
    return hashlib.sha256(read(relative).encode("utf-8")).hexdigest()


# The diagnostic and security policies are intentionally unchanged from final20.
EXPECTED_GUARD_HASHES = {
    "build/Assert-MethodDiagnostics.ps1": "16df7111279969696108046da5cfc53df892cbfd62e7b1036fbae7914f760983",
    "build/method-diagnostics-baseline.json": "96fe39684f3c25c299383c7588616bae3f1de7f6534ffd73cc8dd2bc23b2ec73",
    "build/Assert-SecurityRulePreservation.ps1": "7761ced2be171534fba99e71afe2e5ecda004ae7ba961bde5f9c4d5313dc7d19",
    "build/security-rules-final19.sha256": "fd7abb66ad763afce98bf57e784db681a7663e5989f89fd3255a3213bb699a1d",
}
for relative, expected in EXPECTED_GUARD_HASHES.items():
    actual = normalized_hash(relative)
    assert actual == expected, f"guard changed: {relative}: {actual} != {expected}"

pattern_service = read("LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/CouncilTextPatternDataService.cs")
for method in ("ExtractStructuredField", "ReadTimeoutMilliseconds", "ParseFlags"):
    match = re.search(
        rf"(?:public|private)\s+[^\n]+\s+{method}\s*\([^)]*\)\s*\{{(?P<body>[\s\S]*?)\n    \}}",
        pattern_service,
    )
    assert match, f"method not found: {method}"
    body = match.group("body")
    assert "try" in body and "catch (Exception exception)" in body, f"missing boundary: {method}"
    assert "logger.LogError(" in body, f"missing log: {method}"
    assert '$"' in body, f"non-interpolated diagnostic: {method}"

get_required = re.search(
    r"private Regex GetRequired\(string name\)\s*\{(?P<body>[\s\S]*?)\n    \}",
    pattern_service,
)
assert get_required, "GetRequired not found"
assert 'logger.LogDebug($"' in get_required.group("body")
assert 'logger.LogError(exception, $"' in get_required.group("body")

council_service = read("LocalGPTWebviewWrapper/LocalGPT/Services/CouncilTextService.cs")
for method in ("ExtractTargetFrameworks", "ExtractPackageReferences"):
    assert f'logger.LogError(ex, $"{{nameof({method})}}' in council_service, method

# Protected files and the final19 security-rule manifest still validate after the reviewed source hash refresh.
for manifest in ("build/protected-files.sha256", "build/security-rules-final19.sha256"):
    for raw in read(manifest).splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        expected, relative = line.split("  ", 1)
        assert normalized_hash(relative) == expected, f"manifest mismatch: {relative}"

print("LocalGPT final21 build-guard regression checks passed.")
