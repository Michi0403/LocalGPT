#!/usr/bin/env python3
"""Static source audit for LocalGPT 3.1.0 .NET/DevExpress upgrade integration."""
from __future__ import annotations
from pathlib import Path
import hashlib, sys

root = Path(__file__).resolve().parents[1]
checks = 0

def read(rel: str) -> str:
    p = root / rel
    if not p.is_file():
        raise AssertionError(f"missing {rel}")
    return p.read_text(encoding="utf-8-sig", errors="strict")

def require(rel: str, *tokens: str) -> None:
    global checks
    data = read(rel)
    for token in tokens:
        checks += 1
        if token not in data:
            raise AssertionError(f"{rel} missing {token!r}")

def tree_digest(path: Path) -> str:
    h = hashlib.sha256()
    for p in sorted(path.rglob('*')):
        if not p.is_file():
            continue
        rel = p.relative_to(root).as_posix().encode('utf-8')
        h.update(rel + b'\0' + p.read_bytes() + b'\0')
    return h.hexdigest()

try:
    for rel in (
        'src/LocalGPT/LocalGPT.csproj',
        'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
        'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
    ):
        require(rel, '<Version>3.1.0</Version>')
    require('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj', '<Version>2.1.1</Version>')
    require('Directory.Build.props', '<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>')
    require('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs', 'LocalGPT/3.1.0')

    require('global.json', '"version": "10.0.400"')
    require('src/LocalGPT/LocalGPT.csproj',
            '<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="10.0.11" />',
            '<PackageReference Include="Microsoft.AspNetCore.SignalR.Protocols.MessagePack" Version="10.0.11" />',
            '<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.11">',
            '<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.11" />',
            '<PackageReference Include="System.CodeDom" Version="10.0.11" />',
            '<PackageReference Include="DevExpress.Blazor" Version="25.2.*" />',
            '<PackageReference Include="DevExpress.AIIntegration.Blazor.Chat" Version="25.2.*" />')
    require('src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
            '<PackageReference Include="System.Security.Cryptography.Xml" Version="10.0.11" />')

    # Preserve the exact migration/database source supplied by the user. This release
    # intentionally does not manufacture a migration from the dependency upgrade.
    checks += 1
    got = tree_digest(root / 'src/LocalGPT/Migrations')
    expected = '27c5b6d71b8f9527b64f18ff66ac102ae0558e4ed01317ff02e34f6b77f99c4f'
    if got != expected:
        raise AssertionError(f'migration source changed: {got} != {expected}')
    checks += 1
    db = root / 'src/LocalGPT/Services/Persistence/DatabaseMigrationCompatibilityService.cs'
    got_db = hashlib.sha256(db.read_bytes()).hexdigest()
    expected_db = '50bb2f62df4b6cfe5846063d5e4f20c2ab930a57cb95efa580ad6617f3a748ba'
    if got_db != expected_db:
        raise AssertionError(f'database migration compatibility source changed: {got_db} != {expected_db}')

    # Benchmark and render architecture remain the 3.0.9 behavior.
    require('src/LocalGPT/BusinessObjects/CouncilBenchmarkCalibrationModels.cs',
            'public int ProfileCount { get; set; } = 5;', 'Low, Normal, High, Expert and Max')
    require('src/LocalGPT/Services/CouncilBenchmarkCalibrationService.cs',
            'return ["Low", "Normal", "High", "Expert", "Max"]',
            'Actual provider calls observed', 'advisory website values are not benchmark scores')
    render_files = [p for p in (root / 'src/LocalGPT').rglob('*.razor')
                    if '@rendermode' in p.read_text(encoding='utf-8', errors='ignore')]
    checks += 1
    if len(render_files) != 20:
        raise AssertionError(f'expected 20 explicit @rendermode files, found {len(render_files)}')

    require('CHANGELOG-v3.1.0-DOTNET-DEVEXPRESS-UPGRADE.md',
            '10.0.400', '10.0.11', '25.2.*', 'byte-for-byte', '2.1.1')
    require('VALIDATION-v3.1.0-source.md', 'No `dotnet`', 'byte-identical')

    print(f'LocalGPT 3.1.0 .NET/DevExpress upgrade source audit passed: {checks} checks.')
except (AssertionError, ValueError) as exc:
    print(f'LocalGPT 3.1.0 source audit failed: {exc}', file=sys.stderr)
    raise SystemExit(1)
