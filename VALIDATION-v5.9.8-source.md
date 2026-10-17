# LocalGPT 5.9.8 — static source verification

**Baseline:** complete LocalGPT 5.9.7 source ZIP provided earlier in this conversation.

**Identified cause:** `Assert-PowerShellCompatibility.ps1` inspected any
quoted backslash-containing text on a source line also containing `Join-Path`.
The regex source string at `Assert-RepositoryPowerShellBoundary.ps1:56`
triggered that text-level heuristic even though no `Join-Path` command
received a backslash-delimited path. Release failed before building.

**Change:** inspect literal `StringConstantExpressionAst` and
`ExpandableStringExpressionAst` arguments from actual `Join-Path` command ASTs
that the validator already obtains via PowerShell's parser. Retain all other
compatibility policies and checks.

**Offline validation performed:**

- Confirmed the 5.9.7 line 56 false-positive source matches the original regex heuristic.
- Confirmed that the obsolete `sourceLine.IndexOf('Join-Path'...)` heuristic is removed, and the actual `CommandAst` argument check is present.
- Confirmed both `StringConstantExpressionAst` and `ExpandableStringExpressionAst` literal arguments are inspected, along with the original Windows PowerShell 5.1 positional-argument limit.
- Confirmed all original ZIP entries remain unchanged except for the modified guard and application version; new changelog and this validation report are additive.
- Confirmed project XML version `5.9.8`, required release prerequisites, and ZIP integrity.

**Limits:** no installed PowerShell runtime in this environment; did not execute
the guard, parser, publish script, .NET, or a macOS release. The next definitive
test is the user's macOS release preflight.
