#!/usr/bin/env python3
"""Static source audit for LocalGPT 3.1.10 Chat attachment MIME/draft recovery."""
from __future__ import annotations
from pathlib import Path
import hashlib
import subprocess
import sys

root = Path(__file__).resolve().parents[1]
checks = 0


def read(rel: str) -> str:
    path = root / rel
    if not path.is_file():
        raise AssertionError(f"missing {rel}")
    return path.read_text(encoding="utf-8-sig", errors="strict")


def require(rel: str, *tokens: str) -> None:
    global checks
    data = read(rel)
    for token in tokens:
        checks += 1
        if token not in data:
            raise AssertionError(f"{rel} missing {token!r}")


def tree_digest(path: Path) -> str:
    digest = hashlib.sha256()
    for item in sorted(path.rglob("*")):
        if not item.is_file():
            continue
        rel = item.relative_to(root).as_posix().encode("utf-8")
        digest.update(rel + b"\0" + item.read_bytes() + b"\0")
    return digest.hexdigest()


try:
    for rel in (
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
    ):
        require(rel, "<Version>3.1.10</Version>")
    require("src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj", "<Version>2.1.1</Version>")
    require("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs", "LocalGPT/3.1.10")
    require("global.json", '"version": "10.0.400"')

    checks += 1
    migration_digest = tree_digest(root / "src/LocalGPT/Migrations")
    if migration_digest != "27c5b6d71b8f9527b64f18ff66ac102ae0558e4ed01317ff02e34f6b77f99c4f":
        raise AssertionError(f"migration source changed: {migration_digest}")

    checks += 1
    compatibility = root / "src/LocalGPT/Services/Persistence/DatabaseMigrationCompatibilityService.cs"
    if hashlib.sha256(compatibility.read_bytes()).hexdigest() != "50bb2f62df4b6cfe5846063d5e4f20c2ab930a57cb95efa580ad6617f3a748ba":
        raise AssertionError("database migration compatibility source changed")

    require("src/LocalGPT/Services/ProviderModelBenchmarkService.EvidencePersistence.cs",
            "private const int BenchmarkEvidenceSchemaVersion = 1;")
    require("src/LocalGPT/Services/ProviderStreamRepetitionWatchdog.cs",
            "MaximumBufferedCharacters = 12_288",
            "MinimumPeriodicAgreement = 0.97d",
            "RequiredSuspiciousSamples = 4")
    require("src/LocalGPT/Services/MultiModelCouncilService.RoundRecovery.cs",
            "RecoverConfiguredRoundMemberFailuresAsync",
            "automatic member recovery")

    require("CHANGELOG-v3.1.10-CHAT-ATTACHMENT-MIME-DRAFT-RECOVERY.md",
            "Browser MIME normalization", "application/octet-stream", "Failed native-send draft recovery", "byte-identical")
    require("VALIDATION-v3.1.10-source.md",
            "3.1.10", "No `dotnet`", "capture phase", "Chat.razor.css")
    require("RELEASE.md", "# LocalGPT 3.1.10", "Chat Attachment MIME & Draft Recovery")

    subprocess.run([sys.executable, str(root / "build/audit_chat_attachment_delivery_3_1_10.py")], check=True, cwd=root)
    checks += 1
    subprocess.run([sys.executable, str(root / "build/audit_chat_quick_configuration_3_1_9.py")], check=True, cwd=root)
    checks += 1

    print(f"LocalGPT 3.1.10 Chat attachment repair source audit passed: {checks} checks.")
except (AssertionError, ValueError, subprocess.CalledProcessError) as exc:
    print(f"LocalGPT 3.1.10 source audit failed: {exc}", file=sys.stderr)
    raise SystemExit(1)
