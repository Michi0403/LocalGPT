# LocalGPT 2.1.16 — Workspace policy LINQ correction

## Corrected

- Replaced the ambiguous `entries.Where(regex.IsMatch)` method-group conversion in `ProjectMaintenanceService.EvaluateAccessPolicyRule` with the explicit predicate `entries.Where(entry => regex.IsMatch(entry))`.
- This selects `Enumerable.Where<string>(IEnumerable<string>, Func<string, bool>)` deterministically on .NET 10 and avoids competition with the indexed `Where` overload and the available `Regex.IsMatch` overloads.
- Matching remains bounded to the first 100 workspace entries after regex evaluation; permission findings and policy semantics are unchanged.

## Version alignment

- Advanced the LocalGPT project/runtime version to `2.1.16`.
- Advanced the seeded LocalGPT Core release revision to `seed-v2.1.16`.
- Advanced the organic-wire application advertisement to `2.1.16-organic-wire`.

## Validation boundary

The correction is based directly on the Windows/.NET 10 compiler diagnostic `CS0121` at `ProjectMaintenanceService.cs:1018`. Repository static checks are rerun in the packaging environment; the user's Windows build remains the authoritative semantic compilation.
