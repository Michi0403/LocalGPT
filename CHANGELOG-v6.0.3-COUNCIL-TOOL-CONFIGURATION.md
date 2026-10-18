# LocalGPT 6.0.3 — Council tool exposure and workflow configuration

Based exclusively on the supplied LocalGPT 6.0.2 complete source archive. PublisherStudio remains untouched.

## User-visible configuration

- Council team editor persists project provisioning (never, explicit request, ordinary project, game project) and required completed DXFunction receipts. Council runs no longer decide these outcomes from the literal game development team key.
- Workflow-step editor persists source-workspace curation, a required actual `chat.upload_workspace_file` result, one bounded same-step read retry, and automatic/native function exposure mode. Source integrity curation does not count as model source inspection.
- Standard enabled automatic-function modes expose all registered functions through the normal catalog broker, with team/step names as direct-tool preferences. Read-only upload workspace tools are included in the preferred native set regardless of team name or `games.*` runtime class.
- Optional **Strict team function list** and **Strict step function list** are opt-in settings for **one selected workflow step only**. Both strictly control tools offered through the role-step's provider-native path **and** the textual DXFunction gateway (including bounded continuation); they do not mutate registered DXFunction or organic skill permission settings or restrict future steps or ordinary chat.
- A required model source-file read cannot be combined with an explicit strict list that omits `chat.upload_workspace_file`; configuration validation reports a clear error instead of permitting unprovable review.

## Elimination of hidden team-type behavior

- Removed hardcoded `game-project-development` branches from generic Council project creation, source-read retry, and completion evidence.
- Removed implicit runtime-class-based strictness (`games.*`). Critically **there is no automatic conversion of legacy saved game teams into strict lists**: historical listed function names are treated as preferences unless an operator explicitly chooses Strict in the team editor.
- Seeded game development now declares its project creation, actual file-read requirement and verified build/playtest receipts as data. Learning seed opts out of automatic project provisioning.
- Existing system teams evolve through the normal seed-version mechanism; modified seed content is preserved using the existing user-owned clone/recovery behavior. Migration adds persisted project/evidence fields without replacing user-defined function permissions.

## Scope and limits

- Central DXFunction/organic skill eligibility, authorization and approval gates are unchanged. A strict role-step list is not an authorization grant or revocation.
- The built-in Game Project build/playtest path still produces LocalGPT game definitions; it is **not** a general source-code compilation/test pipeline for arbitrary uploaded open-source games. This release does not claim autonomous recreation of an external repository.
- Windows/macOS .NET, PowerShell, EF migrations, Ollama tool execution, and end-to-end Council source review require runtime testing on a configured host. Static validation alone is not proof of those outcomes.
