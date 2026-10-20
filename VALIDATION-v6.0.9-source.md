# LocalGPT 6.0.9 — source verification report

## Verified from checked-in source

- Compared the full tree against the original 6.0.8 source ZIP: no original paths removed; changes confined to council models, team editor, runtime execution, seed templates, runtime policy, EF migration and documentation.
- Original executor no longer contains the hard-coded game-team key, benchmark-team key, `project.game.build`, or `project.game.start` special cases.
- Recovery rules and follow-on target are serializable BusinessObjects; database model properties, EF model snapshot, migration, and save/read round-trip references are present.
- Team editor contains DevExpress selectors/editors for per-rule retries and dependencies, next team, chain depth, approval and evidence requirements; no Python source was newly introduced.
- Follow-on reuses the child-Council and owner approval paths, propagates workspace/project context and team ancestry, and respects a chain-wide depth cap.
- Recovery tool permissions remain constrained by the existing strict-step/strict-team policies. Recovery functions require verified function receipts, not model claims.
- Game and benchmark behaviors now live in editable seed templates rather than runtime team-key conditions. Seed version incremented to 46 and application version to 6.0.9.
- The Council execution runtime policy JSON seed parses as valid JSON and includes both new bounded retry limits.
- The complete source ZIP was tested with the standard ZIP CRC integrity check.

## Not verified in this environment

- Windows PowerShell architecture/assertion scripts: PowerShell unavailable.
- C# / Razor compile, .NET 10 build, EF Core migration application, XML documentation compiler diagnostics: .NET SDK unavailable.
- Integrated Council model/function execution, owner approval UI flows, and chained-team runtime behavior: requires LocalGPT, configured models, database and application host.

No validation script was weakened or bypassed. No source files were reverted to conceal the defect. No GitHub access was used.
