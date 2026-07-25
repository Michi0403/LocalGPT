# DXAIFunctions and change-review generation

## Purpose

LocalGPT exposes selected application capabilities through dependency-injected `IDxAiFunctionHandler` implementations. Function discovery is dynamic: the composition root registers all concrete handlers and `DxAiFunctionRegistry` publishes their descriptors, safety classification, invocation support, and JSON parameter schema.

This does **not** expose every public service method. Only bounded operations with an intentional AI-facing contract become DXAIFunctions.

## Invocation classes

- **Automatic read-only:** Local Ollama chat may call these while answering. They return bounded project, review, log, knowledge, or conversation metadata.
- **Direct user invocation:** The frontend or controller may invoke a registered handler explicitly.
- **Confirmation-gated mutation:** Review creation, generation, rejection, and build operations require a fresh current user decision. They are never available to automatic model tool calling.
- **Discovery-only route:** Older diagnostic routes can remain visible in the briefing without being dispatched by the generic registry.

The registry rejects automatic calls unless the descriptor is read-only, direct-invocation capable, explicitly automatic-safe, and does not require confirmation.

## Council change-review heartbeat

When the user explicitly asks the Council to generate code, the consensus may include an exact structured proposal:

```text
<localgpt-change-review>
{"files":[],"codeDomTypes":[],"outputs":[]}
</localgpt-change-review>
```

The proposal can contain:

- exact relative source files,
- CodeDOM type specifications,
- source-only output,
- class-library/DLL project,
- console/EXE project,
- whole solution,
- LocalGPT addon project and disabled manifest,
- C# script source,
- JavaScript module source.

LocalGPT parses the last valid proposal block. If no valid block exists, it creates a small bounded fallback proposal and reports that fact.

The proposal is persisted in `CodeGenerationChangeReviews` with its project/topic/council links, summaries, payload, status, and SHA-256 review hash. At this point no generated workspace exists.

## User decision and generation

The frontend displays the heartbeat with paths, purposes, sizes, hashes, CodeDOM types, output targets, and safety summary. The user can reject it or approve that exact review hash once.

After source approval:

1. LocalGPT writes only inside `CouncilArtifacts/CodeGeneration/review-{id}`.
2. Explicit source and CodeDOM output are written.
3. Reviewed C# files are copied into generated .NET project outputs so DLL/EXE/solution builds evaluate the reviewed source rather than an unrelated placeholder.
4. Script/module outputs reuse reviewed `.csx` or `.js` files when supplied.
5. A ZIP is produced for review.
6. A bounded `dotnet build` runs only after a separate current build confirmation.

Generated scripts, executables, DLLs, and addons are never run or loaded automatically. Project paths stored in the database are descriptive context and provide no filesystem authority.

## Logging and operational memory

Every registry invocation and generation operation uses a structured operation ID. Logs include safe identifiers, counts, status, and bounded metadata. Prompts, generated source, request bodies, secrets, private reasoning, and full database rows are omitted from external or summary logs.
