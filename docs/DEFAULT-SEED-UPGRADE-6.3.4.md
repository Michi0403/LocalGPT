# Existing user default repair — 6.3.4

## Why upgrades matter

There is one local database per configured user-data profile, normally per Windows OS user (`%LOCALAPPDATA%\LocalGPT`). Knowledge entries and Council templates are persisted and survive an application update. Shipping new source manifests alone does not update a tester's old installed binary; the user must run a newer published application/installer that includes the seed resources.

## What 6.3.4 does at startup

1. Reconcile EF schema, then add/verify missing package-owned RegexPatterns, Prompts, SystemVariables, the 42 core Knowledge entries plus 18 mandatory source documents and the runtime path reference, core projects, and Council model presets.
2. Reload saved runtime policy (with existing built-in policy fallback).
3. Reconcile supplied Council team/workflow templates, including coding/C#/Blazor teams, and game/runtime class definitions. Existing intentionally deleted Council templates are not revived; user-authored changes are not discarded.
4. Read back stable seed keys across eight categories, log the counts and attempt to write `runtime/seed-reconciliation.json`.

This repairs missing **default content and definitions**, not historical user-created knowledge or the actual downloadable/installed Ollama model weights. C# know-how includes the embedded reference documents and general Knowledge manifest; the live Council still needs access to its registered functions and a compatible model.

## Tester acceptance

1. Back up the existing `localgpt-memory.db` plus its `-wal`/`-shm` companions as appropriate **with LocalGPT stopped**, or use the app's supported backup process. Do not replace it with another user's exported database.
2. Install/publish 6.3.4 normally under the **same Windows account and user data folder**, without deleting or resetting user data.
3. Launch once, then inspect `%LOCALAPPDATA%\LocalGPT\runtime\seed-reconciliation.json`. Require `Status: "Complete"` and each category `Present == Required`. On other platforms follow the path in `runtime/path-layout.json`.
4. Verify code-generation references, the Council team catalog, available game/runtime class templates, `/Install` model aliases and an actual model inventory request. Missing model weights should be installed through Ollama normally; no default-data repair should automatically download them.
5. If the report says `Failed`, or is absent, provide the LocalGPT startup log and build/runtime version. The problem is not considered fixed until a live update run has succeeded.

## Concurrency and user control

Reconciliation is idempotent and run once per process startup; the database initializer is singleton-serialized and Council data is reconciled through its service-owned source logic. Existing saved Council definitions, approvals and tombstones remain authoritative. Failed core database stages prevent normal startup. Supplemental Council/game reconciliation failures are reported as `Failed` and leave unrelated features available in degraded mode; repair is retried on the next restart. No destructive database reset is used as a repair strategy.

## Scope of verification

A complete report proves the required **stored keys** were present at audit time; seed-owned content hashes are separately validated by the existing Knowledge seed stage. It does not validate all user knowledge, network connectivity, Ollama registry accuracy, game execution, C# compilation or whether a model has learned a subject.
