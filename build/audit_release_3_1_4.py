#!/usr/bin/env python3
"""Static source audit for LocalGPT 3.1.4 XML documentation completeness."""
from __future__ import annotations
from pathlib import Path
import hashlib, sys

root = Path(__file__).resolve().parents[1]
checks = 0

def read(rel: str) -> str:
    path = root / rel
    if not path.is_file():
        raise AssertionError(f'missing {rel}')
    return path.read_text(encoding='utf-8-sig', errors='strict')

def require(rel: str, *tokens: str) -> None:
    global checks
    data = read(rel)
    for token in tokens:
        checks += 1
        if token not in data:
            raise AssertionError(f'{rel} missing {token!r}')

def tree_digest(path: Path) -> str:
    digest = hashlib.sha256()
    for item in sorted(path.rglob('*')):
        if not item.is_file():
            continue
        rel = item.relative_to(root).as_posix().encode('utf-8')
        digest.update(rel + b'\0' + item.read_bytes() + b'\0')
    return digest.hexdigest()

try:
    for rel in (
        'src/LocalGPT/LocalGPT.csproj',
        'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
        'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
    ):
        require(rel, '<Version>3.1.4</Version>')
    require('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj', '<Version>2.1.1</Version>')
    require('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs', 'LocalGPT/3.1.4')
    require('global.json', '"version": "10.0.400"')

    checks += 1
    migration_digest = tree_digest(root / 'src/LocalGPT/Migrations')
    expected_migration_digest = '27c5b6d71b8f9527b64f18ff66ac102ae0558e4ed01317ff02e34f6b77f99c4f'
    if migration_digest != expected_migration_digest:
        raise AssertionError(f'migration source changed: {migration_digest} != {expected_migration_digest}')

    checks += 1
    compatibility = root / 'src/LocalGPT/Services/Persistence/DatabaseMigrationCompatibilityService.cs'
    compatibility_digest = hashlib.sha256(compatibility.read_bytes()).hexdigest()
    expected_compatibility_digest = '50bb2f62df4b6cfe5846063d5e4f20c2ab930a57cb95efa580ad6617f3a748ba'
    if compatibility_digest != expected_compatibility_digest:
        raise AssertionError(f'database migration compatibility source changed: {compatibility_digest} != {expected_compatibility_digest}')

    # Earlier benchmark/council resilience remains intact.
    require('src/LocalGPT/Services/ProviderModelBenchmarkService.EvidencePersistence.cs',
            'private const int BenchmarkEvidenceSchemaVersion = 1;', 'BenchmarkEvidence')
    require('src/LocalGPT/BusinessObjects/ProviderModelBenchmarkCoverageSnapshot.cs',
            'AttemptedTargetCount - SuccessfulTargetCount == UnresolvedTargetCount', 'UnresolvedSelectionKeys')
    require('src/LocalGPT/Services/MultiModelCouncilService.RoundRecovery.cs',
            'RecoverConfiguredRoundMemberFailuresAsync', 'automatic member recovery',
            'LocalGPT did not silently drop or fabricate this member result')
    require('src/LocalGPT/Services/MultiModelCouncilService.RunOrchestration.cs',
            'was stopped by caller cancellation', 'is not classified as a Council failure')

    # C# scanner covers Razor code-behind and richer contract completeness.
    require('build/xml_documentation.py',
            "yield p",
            "d=Decl('enum_member'",
            'def enum_member_summary',
            'def tag_text',
            'def ensure_tag',
            "path.name.lower().endswith('.razor.cs')")
    checks += 1
    if "endswith('.razor.cs'): continue" in read('build/xml_documentation.py'):
        raise AssertionError('Razor code-behind is still excluded from the C# XML documentation scanner')

    require('build/razor_xml_documentation.py',
            'def ensure_component_companion',
            'def scan_razor_members',
            'direct @code member declaration',
            'missing .razor.cs partial declaration carrying XML documentation',
            "xd.tag_text(block, 'param'",
            "xd.tag_text(block, 'returns'",
            "xd.tag_text(block, 'value'")
    require('build/Add-XmlDocumentation.py', 'run_razor', "run_razor(args.root, 'enhance')", "run_csharp(args.root, 'enhance')")
    require('build/Assert-XmlDocumentationCoverage.py', 'run_razor', "run_csharp(args.root, 'validate')", "run_razor(args.root, 'validate')")

    # Every maintained Razor component has a code-behind partial declaration carrying type docs.
    components = [p for p in (root / 'src').rglob('*.razor') if p.name != '_Imports.razor' and 'bin' not in p.parts and 'obj' not in p.parts]
    checks += 1
    if len(components) != 45:
        raise AssertionError(f'expected 45 maintained Razor components, found {len(components)}')
    missing_companions = [p for p in components if not p.with_suffix('.razor.cs').is_file()]
    checks += 1
    if missing_companions:
        raise AssertionError('Razor component documentation partial missing: ' + ', '.join(str(p.relative_to(root)) for p in missing_companions))

    require('src/LocalGPT/Components/Shared/BoundedNumberEditor.razor.cs',
            'Renders the reusable bounded number editor Razor component')
    require('src/LocalGPT/Components/Shared/BoundedNumberEditor.razor',
            '/// <value>', 'void OpenSlider()', '/// <summary>')
    require('src/LocalGPT/Components/Pages/Chat.razor.cs',
            'Renders the chat Razor component', '/// <value>')

    require('CHANGELOG-v3.1.4-XML-DOCUMENTATION-COMPLETENESS.md',
            'no existing 3.1.3 Council recovery behavior was removed',
            '752 direct Razor `@code` declarations',
            '9,865 direct maintained declarations',
            'no EF Core migration or SQLite compatibility source was changed')
    require('VALIDATION-v3.1.4-source.md', 'No `dotnet`', '9,865', '752', '3.1.4')
    require('RELEASE.md', '# LocalGPT 3.1.4', 'XML documentation completeness successor')

    print(f'LocalGPT 3.1.4 XML documentation completeness source audit passed: {checks} checks.')
except (AssertionError, ValueError) as exc:
    print(f'LocalGPT 3.1.4 source audit failed: {exc}', file=sys.stderr)
    raise SystemExit(1)
