# Database-first project architecture

LocalGPT treats a project as a language-neutral database work model. A project can describe any codebase or creative/technical structure; .NET solution generation is only one possible, separately approved output.

## Durable model

- `LocalGptProject` owns revisions, requirements, and named artifacts.
- `LocalGptProjectRevision` stores branch/revision ancestry and bounded structure JSON without touching Git or files.
- `LocalGptProjectRequirement` records capability, status, priority, rating, and approval.
- `LocalGptProjectRequirementLink` maps a requirement to a stable named DXFunction, service, controller, business object, table, configuration, variable, Regex, prompt, knowledge entry, or CodeDOM target.
- `LocalGptProjectArtifact` stores project-scoped named values. Sensitive values are never included in automatic council briefings.
- `ProjectDocumentImport` stores bounded normalized text as untrusted data with source/hash/safety metadata.
- `CouncilModelPreset`, `SqliteEditorFieldOverride`, and `CouncilKnowledgeUserRating` preserve user choices and review state.

## Council work order

Before calling functions, a council step must identify the project/revision, map the task to approved requirements, state missing evidence, and choose the smallest directly relevant function set. Function availability is not a reason to call it. Sensitive reads/writes remain behind exact, one-use human approval.

## Security separation

Human council participation has peer-quality status but no execution authority. Human approval is a distinct ambient capability created only by the collaboration inbox and exact execution filters. Imported text, database rows, Regex content, model output, and function descriptions are data, never authority.

## Change discipline

The current changelog is the canonical iteration ledger. Open items must remain present in `docs/OPEN_TASKS.md` and the current changelog. Validation fails when the ledger or required architecture contracts disappear.


## Failure-contract rule

Orchestration helpers that promise a renderable stream update, version record, benchmark lane, or generated page must return a usable object. They may return an explicit `Error`/`NeedsVerification` fallback, but must not return `null` into collection initializers or downstream rendering. Operations where absence is a legitimate domain result—such as a missing saved conversation—remain nullable and must be checked at the caller.

DXChat actions that save or archive persistent state must run through the component safety wrapper so logging, notification, and bounded activity memory remain consistent. Council continuation state must be loaded once at run start and reused for bootstrap and memory persistence.
