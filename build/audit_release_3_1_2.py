#!/usr/bin/env python3
"""Static source audit for LocalGPT 3.1.2 benchmark coverage truth guard."""
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
        require(rel, '<Version>3.1.2</Version>')
    require('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj', '<Version>2.1.1</Version>')
    require('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs', 'LocalGPT/3.1.2')

    require('global.json', '"version": "10.0.400"')
    require('src/LocalGPT/LocalGPT.csproj',
            '<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.11" />',
            '<PackageReference Include="DevExpress.Blazor" Version="25.2.*" />')

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

    # 3.1.1 durable evidence remains intact.
    require('src/LocalGPT/Services/ProviderModelBenchmarkService.EvidencePersistence.cs',
            'BenchmarkEvidence',
            'ProviderModelBenchmarkTaskEvidenceArchive',
            'ProviderModelBenchmarkEvidenceArchive',
            'File.Move(tempPath, finalPath, overwrite: true)')
    require('src/LocalGPT/Components/Shared/ProviderModelBenchmarkTaskEvidence.razor',
            'inspect evidence',
            'Actual provider stream / exposed model thinking',
            'Measured final task result')

    # Coverage truth is machine-derived from one shared rule.
    require('src/LocalGPT/BusinessObjects/ProviderModelBenchmarkCoverageSnapshot.cs',
            'public sealed class ProviderModelBenchmarkCoverageSnapshot',
            'AttemptedTargetCount - SuccessfulTargetCount == UnresolvedTargetCount',
            'public ProviderModelBenchmarkCoverageSnapshot(ProviderModelBenchmarkReport report)',
            'HasSuccessfulMeasuredRecommendation',
            '!string.IsNullOrWhiteSpace(target.Recommendation.ProfileName)',
            'UnresolvedSelectionKeys')
    require('src/LocalGPT/BusinessObjects/CouncilBenchmarkCalibrationModels.cs',
            'public List<string> UnresolvedTargetSelectionKeys { get; set; } = [];')
    require('src/LocalGPT/Services/CouncilBenchmarkCalibrationService.cs',
            'new ProviderModelBenchmarkCoverageSnapshot(report)',
            'The deterministic benchmark coverage invariant failed',
            'UnresolvedTargetSelectionKeys = coverage.UnresolvedSelectionKeys.ToList()',
            '### Machine-derived coverage invariant',
            'Arithmetic check:',
            '#### Authoritative unresolved provider-qualified identities',
            '**Truth guard:**',
            'reports a different unresolved count or a different identity set',
            'Exactly **{coverage.UnresolvedTargetCount}** attempted provider-qualified identity/identities remain unresolved')

    require('src/LocalGPT/Components/Shared/ProviderModelBenchmarkReportAuditSummary.razor',
            'Coverage invariant:',
            'Show exact unresolved identities',
            'new ProviderModelBenchmarkCoverageSnapshot(Report)',
            'Council reviewer prose is secondary interpretation and cannot override these counts or identities')

    require('src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.BenchmarkTemplates.cs',
            'Never claim that all benchmark subjects succeeded or were high quality unless the deterministic machine-derived coverage invariant',
            'Copy those machine-derived counts and identities exactly',
            'Before answering verify `attempted - successful = unresolved`',
            'never replace them with counts or subsets from reviewer prose',
            'correct the prose rather than the benchmark evidence')

    # File-backed evidence schema remains v1, so existing 3.1.1 archives are compatible.
    require('src/LocalGPT/Services/ProviderModelBenchmarkService.EvidencePersistence.cs',
            'private const int BenchmarkEvidenceSchemaVersion = 1;')
    forbid('src/LocalGPT/Services/ProviderModelBenchmarkService.EvidencePersistence.cs', '.ConfigureAwait(true)')

    require('CHANGELOG-v3.1.2-BENCHMARK-COVERAGE-TRUTH-GUARD.md',
            '94 - 84 requires 10 unresolved attempted identities',
            'No database schema or EF migration is changed',
            '3.1.2')
    require('VALIDATION-v3.1.2-source.md', 'No `dotnet`', '3.1.2')
    require('RELEASE.md', '# LocalGPT 3.1.2', 'machine-derived coverage')

    print(f'LocalGPT 3.1.2 benchmark coverage truth-guard source audit passed: {checks} checks.')
except (AssertionError, ValueError) as exc:
    print(f'LocalGPT 3.1.2 source audit failed: {exc}', file=sys.stderr)
    raise SystemExit(1)
