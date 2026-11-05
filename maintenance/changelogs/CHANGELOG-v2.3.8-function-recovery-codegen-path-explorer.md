# LocalGPT 2.3.8 — dynamic code-generation wiring repair

This source-only repair keeps the 2.3.7 Kawaii documentation/Pages snapshot intact while reconnecting the newer database/service/controller architecture to the code-generation and local-path workflows.

## Function-call recovery and routing

- Added `IDxAiFunctionCallRecoveryService` and a registry-backed implementation that can recover provider-emitted textual function carriers without treating arbitrary text as authority.
- Native provider tool calls remain first choice. Text recovery only promotes a call when its name resolves to a live AI-visible DX function and the normal automatic/deferred policy permits it.
- HTML-escaped JSON such as `&quot;name&quot;: &quot;codegen_review_create&quot;` is decoded, parsed with `JsonDocument`, mapped back to the registered `codegen.review.create` name, and routed through the same schema, security, review and approval path as a native tool call.
- Added controller endpoints for function-text recovery and exact approved deferred execution. The existing DX function registry remains the authority for actual invocation.
- Direct Chat approvals can continue immediately from the Human Collaboration Inbox instead of requiring a running Council heartbeat.

## Code generation

- Reused the existing generic `CodeGenerationWorkflowService`, CodeDOM model, review store, artifact workspace and build policy rather than reintroducing old hardcoded per-line generation.
- `codegen.review.create` is now explicitly coordination-only and may create immutable review metadata automatically; it still does not write/build/execute generated code.
- `codegen.review.execute` remains exact-review/hash-bound and human-approval gated. Building remains a separate current confirmation.
- Added database-seeded, user-maintainable code-generation intent patterns for ConsoleApplication, ClassLibrary, Solution and LocalGPT addon output, plus quoted-literal extraction.
- A goal-only request such as `Create a .NET C# "Hello World" console application.` can now be resolved to the existing generic ConsoleApplication scaffold when a provider failed to supply the richer output object.
- Prompt policy tells Chat/Council to use registered code-generation, regex, path, project, log and knowledge functions instead of printing transport JSON or inventing machine paths.

## Local path and Knowledge Import UI

- Added a reusable local path explorer service, controller, DX functions and DevExpress-based popup/grid component.
- Knowledge Import now offers path browsing beside manual entry and separates source/import controls from file-selection/regex policy controls.
- Project Maintenance can browse revision roots, solution files, workspace roots and environment roots without hand-typing every path.
- Removed the active `C:\\learnbaseforlocalgpt` default from maintained runtime/source defaults. Fresh presets are portable and require an explicit local selection.
- Existing stored user values are deliberately not destructively overwritten by seeding.

## Preserved behavior

- No generated Kawaii documentation assets, tracked Pages bundle or Kawaii theme files were changed from the validated 2.3.7 repair snapshot.
- Existing project/knowledge/regex persistence and CRUD services remain the source of truth; new wiring consumes those services instead of creating a second hardcoded subsystem.
- This package contains source only. It does not claim a .NET compile in the repair environment.
