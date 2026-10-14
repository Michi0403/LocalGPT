#!/usr/bin/env python3
"""Source-only regression tests for XML documentation of C# and Razor contracts."""
from __future__ import annotations

import tempfile
from pathlib import Path

import xml_documentation as xd
import razor_xml_documentation as rz

ROOT = Path(__file__).resolve().parents[1]
props = (ROOT / 'Directory.Build.props').read_text(encoding='utf-8')
targets = (ROOT / 'Directory.Build.targets').read_text(encoding='utf-8')
checks = 0


def require(value: bool, message: str) -> None:
    global checks
    checks += 1
    if not value:
        raise AssertionError(message)


def verify_csharp(source: str) -> list[str]:
    with tempfile.TemporaryDirectory() as directory:
        p = Path(directory) / 'Documented.cs'
        p.write_text(source, encoding='utf-8')
        content_before = p.read_bytes()
        issues, _ = xd.validate_file(p)
        require(p.read_bytes() == content_before, 'Validation must never change source files')
        return issues


CLEAN_CSHARP = '''/// <summary>Owns version-aware project analysis and identifies the selected compiler capability.</summary>
public sealed class Documented
{
    /// <summary>Returns the selected compiler version for the requested project.</summary>
    /// <param name="projectId">Tracked project identifier.</param>
    /// <returns>Selected compiler version.</returns>
    public string Version(string projectId) => projectId;

    /// <summary>Indicates whether the current revision is compatible with the selected compiler.</summary>
    /// <value>A compatibility determination produced from the stored project version.</value>
    public bool Compatible { get; set; }
}
'''


def verify_razor(code: str, has_companion: bool = True) -> list[str]:
    with tempfile.TemporaryDirectory() as directory:
        p = Path(directory) / 'Src' / 'LocalGPT' / 'Components' / 'Pages' / 'Probe.razor'
        p.parent.mkdir(parents=True)
        p.write_text('@code {\n' + code + '\n}\n', encoding='utf-8')
        if has_companion:
            p.with_suffix('.razor.cs').write_text('''namespace LocalGPT.Components.Pages;
/// <summary>Razor component that allows operators to inspect project compiler compatibility.</summary>
public partial class Probe { }
''', encoding='utf-8')
        original = p.read_bytes()
        issues, _ = rz.validate_razor_file(p)
        require(p.read_bytes() == original, 'Razor validation must not rewrite code blocks')
        return issues


require('AssertLocalGptXmlDocumentationCoverage' in targets and
        'BeforeTargets="BeforeBuild"' in targets and
        "'$(MSBuildProjectName)' == 'LocalGPT'" in targets,
        'Build-wired XML validation must be required on normal LocalGPT builds')
require('Assert-XmlDocumentationCoverage.ps1' in targets,
        'Build must invoke maintained XML guard through the PowerShell wrapper')
for code in ('CS1572', 'CS1573', 'CS1591'):
    require(code in props, f'Compiler warning {code} must be treated as an error')
require('def mismatched_contract_tags(' in (ROOT/'build/xml_documentation.py').read_text(encoding='utf-8'),
        'C# validator must enforce parameter/signature parity')
require('mismatched_contract_tags(' in (ROOT/'build/razor_xml_documentation.py').read_text(encoding='utf-8'),
        'Razor validator must enforce parameter/signature parity')
require(not verify_csharp(CLEAN_CSHARP), 'Fully documented C# contract should pass')
require(any('missing param' in x for x in verify_csharp(CLEAN_CSHARP.replace('    /// <param name="projectId">Tracked project identifier.</param>\n', ''))),
        'Missing C# parameters must be rejected')
require(any('unexpected param' in x for x in verify_csharp(CLEAN_CSHARP.replace('name="projectId"', 'name="obsoleteProject"'))),
        'Stale XML parameter tags must be rejected')
require(any('duplicate param' in x for x in verify_csharp(CLEAN_CSHARP.replace('    /// <returns>', '    /// <param name="projectId">Duplicate documentation.</param>\n    /// <returns>'))),
        'Duplicate XML parameter tags must be rejected')
require(any('empty XML summary' in x for x in verify_csharp(CLEAN_CSHARP.replace('    /// <summary>Returns the selected compiler version for the requested project.</summary>\n', ''))),
        'Undocumented C# methods must be rejected')
require(any('missing value tag' in x for x in verify_csharp(CLEAN_CSHARP.replace('    /// <value>A compatibility determination produced from the stored project version.</value>\n', ''))),
        'Undocumented property value semantics must be rejected')

RAZOR_METHOD = '''    /// <summary>Applies the selected revision to the current inspection request, preserving the active editing context.</summary>
    /// <param name="revisionId">Revision selected by the interactive component.</param>
    /// <returns>An awaited operation that completes after the state transition.</returns>
    private Task ApplyRevisionAsync(Guid revisionId) => Task.CompletedTask;'''
require(not verify_razor(RAZOR_METHOD), 'Fully documented Razor @code should pass')
require(any('empty XML summary' in x for x in verify_razor(RAZOR_METHOD.replace('    /// <summary>Applies the selected revision to the current inspection request, preserving the active editing context.</summary>\n', ''))),
        'Undocumented Razor @code methods must be rejected')
require(any('unexpected param' in x for x in verify_razor(RAZOR_METHOD.replace('name="revisionId"', 'name="wrongName"'))),
        'Stale Razor @code signature documentation must be rejected')
require(any('missing .razor.cs partial' in x for x in verify_razor(RAZOR_METHOD, has_companion=False)),
        'Razor component type without a documented partial class must be rejected')

print(f'XML documentation architecture regression passed: {checks} validations for build wiring, compiler parity, C# and Razor, negative tests and read-only auditing.')
