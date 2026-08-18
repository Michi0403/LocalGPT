#!/usr/bin/env python3
"""Static source audit for LocalGPT 3.1.1 durable benchmark audit evidence."""
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

def forbid(rel: str, *tokens: str) -> None:
    global checks
    data = read(rel)
    for token in tokens:
        checks += 1
        if token in data:
            raise AssertionError(f"{rel} unexpectedly contains {token!r}")

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
        require(rel, '<Version>3.1.1</Version>')
    require('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj', '<Version>2.1.1</Version>')
    require('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs', 'LocalGPT/3.1.1')

    # Carry the supplied .NET 10 / DevExpress lane forward.
    require('global.json', '"version": "10.0.400"')
    require('src/LocalGPT/LocalGPT.csproj',
            '<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.11" />',
            '<PackageReference Include="DevExpress.Blazor" Version="25.2.*" />')

    # The database/migration boundary remains untouched.
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

    # The prior visible benchmark evidence fix remains present.
    require('src/LocalGPT/BusinessObjects/ProviderModelBenchmarkModels.cs',
            'public string TaskPrompt { get; set; } = string.Empty;',
            'public string ProviderTrace { get; set; } = string.Empty;',
            'public string ResponseText { get; set; } = string.Empty;',
            'public string EvidenceArtifactId { get; set; } = string.Empty;')
    require('src/LocalGPT/Services/ProviderModelBenchmarkService.ProfileExecution.cs',
            'GetStreamingResponseAsync(',
            'TryPersistFullTaskEvidenceAsync(',
            'LimitBenchmarkEvidence(fullProviderTrace, 64_000',
            'LimitBenchmarkEvidence(text, 48_000')
    require('src/LocalGPT/Components/Shared/ProviderModelBenchmarkTaskEvidence.razor',
            'inspect evidence',
            'Actual provider stream / exposed model thinking',
            'Measured final task result',
            'Load complete archived evidence')

    # Full task streams and report history are durable but not eagerly rendered.
    require('src/LocalGPT/Services/ProviderModelBenchmarkService.EvidencePersistence.cs',
            'BenchmarkEvidence',
            'WriteJsonAtomicallyAsync',
            'ProviderModelBenchmarkTaskEvidenceArchive',
            'ProviderModelBenchmarkEvidenceArchive',
            'File.Move(tempPath, finalPath, overwrite: true)',
            'Evidence persistence must never convert a completed benchmark into a failed benchmark')
    require('src/LocalGPT/Interfaces/IProviderModelBenchmarkService.cs',
            'GetStoredEvidenceAsync(',
            'LoadStoredEvidenceAsync(',
            'LoadTaskEvidenceAsync(')
    require('src/LocalGPT/Components/Shared/ProviderModelBenchmarkEvidenceHistory.razor',
            'Saved benchmark audit evidence',
            'Full task streams remain disk-backed until explicitly requested',
            'ProviderModelBenchmarkTaskEvidence')
    require('src/LocalGPT/Components/Shared/ProviderModelBenchmarkReportAuditSummary.razor',
            'Deterministic measurement evidence',
            'Council reviewer prose is secondary interpretation')

    # Service code must not switch back to synchronization-context capture for file/provider work.
    forbid('src/LocalGPT/Services/ProviderModelBenchmarkService.EvidencePersistence.cs', '.ConfigureAwait(true)')

    require('CHANGELOG-v3.1.1-BENCHMARK-AUDIT-EVIDENCE.md',
            'Durable full-fidelity benchmark evidence',
            'No database migration is introduced',
            '3.1.1')
    require('VALIDATION-v3.1.1-source.md', 'No `dotnet`', '3.1.1')
    require('RELEASE.md', '# LocalGPT 3.1.1', 'BenchmarkEvidence')

    print(f'LocalGPT 3.1.1 durable benchmark evidence source audit passed: {checks} checks.')
except (AssertionError, ValueError) as exc:
    print(f'LocalGPT 3.1.1 source audit failed: {exc}', file=sys.stderr)
    raise SystemExit(1)
