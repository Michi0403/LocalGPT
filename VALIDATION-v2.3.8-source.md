# LocalGPT 2.3.8 source validation

Validation was performed in an environment without `dotnet`, `csc`, `mcs`, or `pwsh`, so no compilation or runtime execution claim is made here.

Passed maintained source audits:

- `audit_application_architecture.py --product localgpt --mode all`
- `audit_async_continuations.py`
- `audit_chat_ascii_console.py`
- `audit_documentation_onewire_contracts.py`
- `audit_kawaii_documentation_layout.py`
- `audit_provider_qualified_council.py`

Additional repair checks:

- No maintained source reference to the old `C:\\learnbaseforlocalgpt` default remains outside the intentionally preserved generated documentation snapshot.
- No `.dll`, `.exe`, `.pdb`, `.nupkg`, `.snupkg`, `bin`, or `obj` artifact is included in the source worktree/package.
- The tracked Kawaii documentation trees are byte-for-byte identical to the 2.3.7 repair baseline.
- The tracked Pages package still validates as the last real generated documentation snapshot: version 2.3.7, 884 HTML files, 855 API HTML files, complete API reference, tagged PDF, valid local links, accessibility, theme persistence and cat/paw favicon checks.
- The 2.3.8 source version is intentionally newer than that generated documentation snapshot. A real owner-side .NET/DocFX release build should regenerate the versioned 2.3.8 HTML/PDF rather than relabeling an older artifact.

The repair specifically preserves the existing generic CodeDOM/output/build pipeline and adds routing/recovery around it; it does not restore an older hardcoded generator as a parallel implementation.
