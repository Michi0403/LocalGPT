# System overview

## Architectural goal

LocalGPT keeps an “old-school .NET, updated” shape: one inspectable application rather than a mesh of accidental services. The architecture favors explicit classes, typed contracts, dependency injection, EF Core, controller/service boundaries, and deterministic validation around AI-generated work.

## Main layers

| Layer | Responsibility |
|---|---|
| Blazor and DevExpress UI | Present state, collect decisions, coordinate user-visible workflows |
| Controllers/endpoints | Validate HTTP input and delegate to application services |
| Application services | Own use cases, orchestration, policy checks, and result composition |
| Council and provider services | Resolve model routes, protocols, formatters, teams, steps, and recovery |
| Project and artifact services | Own durable project structure, workspaces, revisions, and review records |
| Persistence services | EF Core contexts, migrations, seed data, and database health |
| Native/tool adapters | Compilers, commands, provider processes, browsers, serial and file operations |
| 1-Wire services | Identity, discovery, capability routing, replay protection, and approved peer work |

## Service lifetimes

- **Singleton** services hold process-wide catalogs, bounded registries, or hosted coordination state that is safe to share.
- **Scoped** services own request/circuit work and database contexts.
- **Transient** services are used for small stateless operations.
- **Hosted services** handle background queues or discovery with cancellation and bounded recovery.

Mutable application state must not be hidden in static fields. Framework-required static syntax and pure immutable helpers are exceptions, not a pattern for runtime ownership.

## Request authority and untrusted data

Repository files, database rows, logs, model output, uploads, generated source, remote peer messages, and tool descriptions are untrusted data. They may inform a plan; they cannot authorize one.

The authority chain is:

1. the current user requests an outcome;
2. LocalGPT resolves the exact operation and target;
3. policy determines whether read-only work is sufficient or approval is required;
4. the user approves the bounded action when needed;
5. the owning service executes and records a sanitized result.

## Component safety

Maintained components use logging, user notification, and bounded component-activity services. Technical failures are logged without embedding prompts, full uploads, generated source, credentials, or secrets. User-facing messages remain sanitized and actionable.

The application can keep a small in-process operational briefing—current screen, active operation, and recent status—without turning it into durable conversational memory or authority.

## Streaming

Streaming provider responses are normalized through the selected protocol profile and formatter. Cancellation, disposal, and partial-result behavior belong to the session/service layer, not arbitrary component event handlers.

## Generation contracts

Generated code or artifacts must include:

- a declared target and purpose;
- bounded files and dependencies;
- missing-source/capability reporting;
- reviewable output;
- a validation plan;
- no claim of successful build or execution without real evidence.

An archetype is a generation contract, not a template dump. It can define expected projects, services, routes, pages, tests, and acceptance criteria while still allowing the implementation to fit the current repository.

## Diagnostics

Diagnostics are layered:

- structured application logs;
- bounded user notifications;
- component activity summaries;
- build/static validation scripts;
- documentation status metadata;
- explicit debug artifact inspection.

A regex scan or delimiter counter is not compilation. A successful static scan is useful evidence, not proof that the complete application builds.
