from __future__ import annotations

import hashlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8-sig").replace("\r\n", "\n").replace("\r", "\n")


def normalized_hash(relative: str) -> str:
    return hashlib.sha256(read(relative).encode("utf-8")).hexdigest()


pattern_service = read("LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/CouncilTextPatternDataService.cs")
assert ".Select(item => item.ValueString)" in pattern_service
assert ".Select(item => item.Value)" not in pattern_service
assert "db.SystemVariables.AsNoTracking()" in pattern_service
assert "systemVariables.RegexMatchTimeoutMilliseconds" in pattern_service

council_service = read("LocalGPTWebviewWrapper/LocalGPT/Services/CouncilTextService.cs")
assert (
    "foreach (System.Text.RegularExpressions.Match match in "
    "_patterns.KnowledgeBlockPattern.Matches(responseText))"
) in council_service
assert (
    "foreach (System.Text.RegularExpressions.Match match in "
    "_patterns.CapabilityGapBlockPattern.Matches(responseText))"
) in council_service
assert "var body = match.Groups[\"body\"].Value.Trim();" in council_service

# Architecture/security policies and monotonic baselines remain unchanged from final21.
EXPECTED_GUARD_HASHES = {
    "build/Assert-MethodDiagnostics.ps1": "16df7111279969696108046da5cfc53df892cbfd62e7b1036fbae7914f760983",
    "build/method-diagnostics-baseline.json": "96fe39684f3c25c299383c7588616bae3f1de7f6534ffd73cc8dd2bc23b2ec73",
    "build/Assert-SecurityRulePreservation.ps1": "7761ced2be171534fba99e71afe2e5ecda004ae7ba961bde5f9c4d5313dc7d19",
    "build/security-rules-final19.sha256": "fd7abb66ad763afce98bf57e784db681a7663e5989f89fd3255a3213bb699a1d",
    "build/Assert-RuntimeValueOwnership.ps1": "49730fee894f2f0ff1d49834461ad69e8b60e83f34876ca852e0d24ddbf2f610",
    "build/runtime-value-ownership-baseline.json": "36ada2054b7e61579283f99cf975b026b30492e20bc588b43c24e7e2a6778b6b",
}
for relative, expected in EXPECTED_GUARD_HASHES.items():
    assert normalized_hash(relative) == expected, f"guard changed: {relative}"

for manifest in ("build/protected-files.sha256", "build/security-rules-final19.sha256"):
    for raw in read(manifest).splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        expected, relative = line.split("  ", 1)
        assert normalized_hash(relative) == expected, f"manifest mismatch: {relative}"

print("LocalGPT final22 compiler regression checks passed.")
