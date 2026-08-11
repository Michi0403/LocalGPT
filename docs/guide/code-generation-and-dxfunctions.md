# Code generation and DXFunctions

LocalGPT code generation is a review-first workflow. The AI can prepare an exact change review, but writing files and building them are separate actions with human approval. The workflow is local and does not require GitHub or another remote source host.

## The five DXFunctions

The code-generation surface is exposed through these exact registry names:

- `codegen.review.list` lists recent change reviews. `projectId` is optional and `take` is any positive integer the caller chooses; the workflow no longer imposes an arbitrary 100-item ceiling.
- `codegen.review.get` reads one review by `reviewId` so the exact review hash and proposed content can be inspected.
- `codegen.review.create` creates the database-backed review. It does **not** write, build, execute, load, or integrate generated code.
- `codegen.review.execute` writes the exact approved review to an isolated LocalGPT workspace. It requires the exact `reviewId`, the current `expectedReviewHash`, and fresh human confirmation. A .NET build is optional and has its own confirmation gate.
- `codegen.review.reject` rejects a pending review without writing or building it.

The DI container discovers all `IDxAiFunctionHandler` implementations and registers them as scoped handlers. The code-generation handlers therefore use the same registry, policy, deferred-approval, logging, and activity display path as other DXFunctions.

## Minimum data a user or AI must provide

`codegen.review.create` always requires a concrete `goal`. Everything else depends on the requested output.

### Exact source tree of any size

Provide `files`, where every item has:

- `relativePath`: path inside the isolated generated workspace.
- `content`: the exact file content to review and write.
- `purpose`: optional human-readable reason for the file.

Use an output with `kind: "SourceFiles"` when the supplied file tree itself is authoritative. This is the most exact route for mixed repositories and multi-project solutions because LocalGPT preserves supplied `.sln`, `.slnx`, `.csproj`, `.cs`, `.razor`, `.js`, `.json`, configuration, documentation, and other reviewed files rather than inventing project structure around them.

There is no fixed 512-file review ceiling and no fixed four-million-character payload ceiling in `CodeGenerationWorkflowService`. The practical limits are the machine's memory, disk, SQLite storage, request transport, and whatever context the selected AI model can actually process. LocalGPT does not silently truncate reviewed file content, goal/context summaries, or the written-file result list in this workflow.

After an approved generated or maintained project is registered, its tracking rescan also no longer imposes the former 100,000-file source clamp or a hard-coded file-size literal from `CodeGenerationWorkflowService`. `ScanProjectFilesRequest.MaximumFiles` and `MaximumFileBytes` default to the database-backed `MaxFiles` and `MaxSingleFileBytes` runtime policies; a caller may still deliberately request a smaller positive bound for a specific scan. This keeps repositories with hundreds of thousands of files representable without baking small repository/file ceilings into `CodeGenerationWorkflowService` or `ProjectMaintenanceService`.

### C# source, class library, or console application

Supply reviewed `.cs` files in `files`. Choose one of:

- `SourceFiles` for exact files only.
- `ClassLibrary` for a generated SDK-style library project around the reviewed C# sources.
- `ConsoleApplication` for a generated executable project around the reviewed C# sources.

An output can additionally provide `name`, `relativeDirectory`, `targetFramework`, `rootNamespace`, and `description`. Defaults are used only when these optional values are omitted.

### C# script

Supply one or more reviewed `.csx` files and choose `CSharpScript`. If no `.csx` source was supplied, LocalGPT scaffolds the reviewed script output rather than executing it. Scripts are never run automatically.

### JavaScript module

Supply reviewed `.js` files and choose `JavaScriptModule`. If no `.js` source was supplied, LocalGPT scaffolds a module file. JavaScript is written only; it is not automatically executed or loaded.

### LocalGPT add-on

Choose `LocalGptAddon` and supply the reviewed C# sources. LocalGPT creates the add-on project/manifest structure in the isolated workspace. Integration/loading remains outside the review creation step.

### Simple generated .NET solution

Choose `Solution` when LocalGPT should scaffold a solution and project around reviewed C# sources. For an **exact existing or multi-project solution layout**, prefer `SourceFiles` and supply all `.sln`/`.slnx`, project, source, web, asset, and configuration files explicitly. That prevents the scaffold layer from guessing a project topology that the user already knows.

## Existing LocalGPT project maintenance

To generate changes against an already tracked project, provide:

- `projectId`: the LocalGPT project identifier.
- `projectRevisionId`: the approved/scanned revision whose user-approved, non-generated tracked files are the baseline.
- `goal`: the requested change.

`projectTopicId` and `councilRunId` are optional links to the project/council context. `currentProjectState`, `councilSummary`, `changeSummary`, and `safetySummary` are optional descriptive context and are stored without arbitrary workflow truncation.

At execution time LocalGPT re-reads every approved tracked source file, verifies its content hash against the approved scan, and copies it byte-for-byte into the isolated workspace before applying reviewed changes. The complete approved tree is copied and the complete written-file path list is retained; the previous 5,000-path reporting cut-off is removed.

## CodeDOM input

`codeDomTypes` is optional. A CodeDOM item can provide `relativePath`, `namespace`, `typeName`, `methodName`, `methodResult`, and `summary`. Use it when structured C# generation is more convenient than supplying exact text. For exact source reproduction, `files` is the preferred representation.

## Approval and build sequence

A safe generation sequence is:

1. Call `codegen.review.create` with the exact goal and proposed files/output targets.
2. Present or call `codegen.review.get` and let the user inspect the review plus `reviewHash`.
3. Call `codegen.review.execute` only after the user approves that exact hash. The DXFunction is marked as requiring human confirmation and supports deferred approval.
4. Set `buildAfterGeneration: true` only when a .NET build is wanted. The workflow requires the separate current build confirmation before invoking the bounded build executor.
5. Generated programs and scripts are not executed or loaded automatically.

The review hash, one-use approval flag, isolated path validation, tracked-file hashes, and separate build confirmation are intentional safety boundaries; they are not repository-size limits.

## Ollama models without native tool metadata

Some exact Ollama model variants reject native tool metadata. LocalGPT then attaches a textual DXFunction directory and instructs the model to emit one standalone object in this form when a real function is needed:

```json
{"functionName":"exact.registry.name","arguments":{}}
```

LocalGPT still resolves the exact registry name, applies its normal automatic/deferred approval policy, shows tool activity, validates arguments, executes only when permitted, returns the tool result, and continues the response. A model is explicitly told not to invent function names that are absent from the request-specific directory.

## What is not required

Creating and reviewing code does not require GitHub access, a remote repository, or a compiler. A compiler/toolchain is required only for the optional build step the user explicitly requests and confirms. The generated ZIP/workspace can always be inspected or built elsewhere.
