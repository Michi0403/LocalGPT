# LocalGPT 2.6.0 — Council role/provider binding and workflow isolation

## Why 2.6.0

The maintained version policy rolls `2.5.9` to `2.6.0` rather than creating a two-digit patch slot.

## Council team model ownership

- Council roles can now use `AssignedModels` and persist exact provider-qualified model identities.
- `/council-teams` discovers the current provider catalog and groups selectable models by provider and endpoint, so `model + provider + host` is configured as one identity.
- Saved role bindings are authoritative for a run. Bound models are added to the run even when the Chat model picker did not separately select them.
- Legacy bare model assignments are upgraded in-memory only when exactly one current provider candidate matches. If the same model exists on more than one host, the run stops and asks the user to choose the exact provider/host instead of guessing.
- `AssignedModelSingle` no longer falls back to another healthy role member or another host. The exact saved provider-qualified member must belong to the role.
- Workflow roles that are absent from a team with explicit role definitions now fail closed instead of silently inheriting all selected models.
- Assigned-model workflow steps are rejected when their role is random or human-only, because those policies cannot guarantee the exact member belongs to the round.
- Runtime prompts explicitly state the executing provider-qualified model and that model-to-role ownership is authoritative for the workflow step.

## Workflow round and information boundaries

- Workflow step order is separated from logical Council round numbering with `LogicalRoundNumber` (`0` keeps automatic numbering).
- Multiple planning steps can therefore belong to the same logical simulation/work round before a later judge/resolution step.
- Each step now owns a transcript visibility policy: `FullCouncil`, `SameRole`, `CurrentRound`, `SameRoleCurrentRound`, or `None`.
- The visibility policy applies both to `{{Transcript}}` and to `{{PreviousStep}}`, preventing private output from leaking through the alternate previous-step placeholder.
- The team editor exposes both controls directly beside the workflow/model settings.

## General Council vs project Council

- The maintained `general` seed is now a neutral General Council for everyday questions, research, comparison, creativity and simulations. It no longer assumes that every request is a software project.
- The previous Organic Project Team remains available separately as `general-project` for compiler/project/revision work.
- The project-work quick starter is scoped to `general-project`.
- Seed version advances to 17; user-modified team rows remain user-owned and are not overwritten by seed evolution.

## Human collaboration repair

- Enabling the Human Council Participant profile from Chat now pushes the same trusted local-human interaction context already used by live contribution actions before calling `SaveProfileAsync`.
- This fixes the observed `Only the trusted local human UI may update the human council profile` failure without weakening the trust boundary.

## Decision-poll classifier repair

- Frustration detection now evaluates the latest user portion of the reconstructed Council prompt rather than blindly scanning the complete accumulated workflow text.
- Single-word markers use Unicode-aware token boundaries, so text such as `made by factions` can no longer trigger the `mad` marker.
- Phrase markers such as `does not work` remain supported.

## Ollama native-tool compatibility learning

- A provider-qualified Ollama model that returns HTTP 400/501 for native tool metadata is remembered for the lifetime of the LocalGPT process.
- Later requests to that exact Ollama host/model skip the already-known incompatible native tool metadata instead of paying the same failed probe again.
- The existing retry-without-tools behavior remains the first-observation recovery path.

## UI

- Provider model selection in Council Teams is grouped by host with collapsible groups instead of adding another nested scrolling surface.
- Stale saved bindings remain visible and removable even when the provider/model catalog is temporarily unavailable.
- Assigned-model workflow selection is a provider/host/model selector rather than a free-text model-name field.

## Versions

- LocalGPT: `2.6.0`
- LocalGPTInstallerConsole: `2.6.0`
- LocalGPTWebviewWrapper: `2.6.0`
- LocalGPT Wire Protocol: unchanged `2.1.1`
