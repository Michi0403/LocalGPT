# AI Host .NET DevExpress Blazor Rebuild Guide

This guide teaches the LocalGPT AI Council how to generate a local AI host application in .NET, ASP.NET Core, Blazor, and DevExpress style.

The goal is not to copy names from source folders or provider brands. The goal is to learn functionality, protocols, host shapes, service boundaries, route families, component wiring, and user workflows.

## Product Shape

A local AI host tool needs a recognizable application shell:

- Left navigation with model catalog, chats, API console, downloads, running models, hardware, templates, logs, settings, and diagnostics.
- Main workspace that starts with useful state, not a marketing page.
- Status bar or header indicators for provider connectivity, selected model, active downloads, GPU/CPU policy, and runtime health.
- Download/progress surfaces that use HTTP routes and progress state instead of printing binaries or zips into chat text.
- Chat surfaces that expose streaming status, model selection, model-thinking visibility when available, and artifact links.

For a LocalGPT-style generation, use DevExpress Blazor components for application controls and Bootstrap v5 for macro layout.

## Better-For-LocalGPT Acceptance Target

The generated host should not be a thinner copy of a constrained provider. It
should be better for LocalGPT's use case:

- Run more than one model session when the selected runner, hardware budget,
  and user policy allow it.
- Queue honestly when concurrency is not possible, showing which model is
  waiting, why it is waiting, and how long the current step has been running.
- Let a council assign cooperative model roles such as implementer, reviewer,
  architect, safety checker, setup helper, and UX checker without toxic ranking.
- Support cancellation per request, per model session, and per download.
- Expose keep-alive, unload, context, output, CPU/GPU placement, and
  hardware-risk controls in the UI.
- Read local model files through approved runner contracts rather than routing
  chat/generate calls to an upstream host.
- Keep model download plans user-approved, source-backed, checksum-aware, and
  visible in SQLite-backed job history.
- Provide LocalGPT-compatible HTTP routes and a clear way to point DXAiChat at
  the generated host URL for acceptance testing.

If a generated solution cannot yet perform native tensor inference, it must
still build and expose the runner contract, model-file resolution path,
capability-gap report, and safe next setup step. It must not claim success by
silently proxying to another host.

## Source Lessons From Local AI Hosts

The local `ollama-main` source is architecture evidence for a .NET control plane. Treat Ollama as one provider/source example, not as the generated application name:

- API route surface: version, model tags/list, running models, show, pull, push, create, copy, delete, generate, chat, and embed.
- Compatibility adapters: OpenAI-compatible and Anthropic-compatible request/response surfaces.
- Model lifecycle: manifests, layers, digests, local model paths, copy/delete/show metadata, downloads, uploads, and progress.
- Runtime orchestration: loaded model sessions, keep-alive, runner processes, cancellation, status, and hardware-specific launch choices.
- Concurrent model orchestration: multiple model sessions where hardware and the
  selected backend allow it, plus queueing/sequential fallback where they do not.
- Formatting: chat templates, tokenizer behavior, harmony/thinking content, structured output, tools, and streaming chunks.
- Platform shell: WebView/tray concepts and OS-specific runtime helpers.

Translate those into .NET patterns:

- `IModelCatalogService` for installed, downloadable, and running models.
- `IModelTransferService` for pull/push/download progress and checksums.
- `IInferenceProvider` for chat, generate, embeddings, cancellation, and streaming.
- `IInferenceRunner` for native/process/plugin/Python execution with load,
  unload, infer, embed, health, cancellation, and shutdown.
- `IRuntimeSessionService` for load/unload, keep-alive, and current model state.
- `IModelScheduler` for per-model queues, concurrent-session limits, fairness,
  cancellation, and backpressure.
- `IHardwareBudgetService` for CPU/GPU/VRAM/concurrency settings.
- `IChatTemplateService` for role formatting, harmony parsing, thinking extraction, and tool calls.
- `IPluginCatalogService` for runner/provider plugin discovery, manifest
  validation, trust status, and approved loading.
- `IScriptExecutionService` for approved PowerShell/Python/native scripts with
  safe working directories, logs, and cancellation.
- ASP.NET Core endpoints for provider-compatible route families.
- SignalR or server-side streaming for progress and token output.
- EF/SQLite records for model metadata, settings, chat history, logs, downloads, and knowledge entries.

Native tensor execution is a backend the generated host must own through a
runner boundary. A generated first accepted milestone cannot be only an
Ollama/LM Studio/OpenAI-compatible proxy. It must include a native/local-file
runner contract, model-file resolution, and explicit settings for a trusted
runner executable or compatible managed/native inference library.

## .NET Architecture Patterns To Apply

An AI host generated in .NET should use these Microsoft-supported patterns:

- Dependency injection as the composition root. Register native local-file
  runner services, catalog/download/session services, hardware budget services,
  template parsers, plugin catalog services, and optional provider/catalog
  adapters in `Program.cs` or an
  `AddAiHostCore()` extension.
- Options pattern for provider profiles, safe storage roots, hardware budgets,
  context/output defaults, plugin roots, Python paths, script policy, and first
  bootstrap URL.
- Hosted services or worker services for long-running downloads, model unload
  timers, session cleanup, model-work queues, background indexing, and
  nonblocking diagnostics.
- `AssemblyLoadContext` plus `AssemblyDependencyResolver` when runner/provider
  plugins need isolated dependencies or native libraries. This is not a security
  sandbox; untrusted code needs process/OS/container isolation.
- Interface adapter layers for native processes, ONNX/ML.NET, Python.NET,
  PowerShell scripts, and optional external catalog/provider integrations.
- EF/SQLite for user-approved settings, model sources, downloads, jobs, logs,
  plugin manifests, and chat history. Appsettings should keep bootstrap/logging.
- Typed `HttpClient` adapters for HTTP providers and OpenAI-compatible surfaces.
- Capability gap reporting when a native runner, tokenizer, model format, GPU
  backend, or plugin implementation is missing.

## Native Runner And Plugin Milestone

The generated app should make native inference achievable instead of hiding it:

- `NativeModelFileInferenceProvider`: resolves `.gguf`, ONNX, or
  Ollama-managed local blob candidates and calls the approved runner directly.
- `ModelSchedulerHostedService`: coordinates active model workers, sequential
  fallback, cancellation, queue depth, and per-model session leases.
- `ProcessInferenceRunner`: starts an approved executable with safe arguments,
  streams output, handles cancellation, and records logs.
- `PythonNetInferenceRunner`: embeds Python after user approval, configured
  `PYTHONNET_PYDLL`, and package/runtime checks. Python calls must use `Py.GIL()`
  and belong behind a backend service.
- `PowerShellRunner`: runs explicit approved scripts or constrained runspaces
  for diagnostics/setup, never arbitrary command strings from UI.
- `OnnxRuntimeRunner` or `MlNetRunner`: represents compatible managed inference
  paths, not universal LLM replacement.
- `PluginLoadContext`: loads trusted runner/provider plugins and reports
  manifest, version, supported formats, hardware support, and approval state.

Milestones:

1. Buildable local model-file host with routes, UI, settings, logs, catalog,
   direct model-file resolution, and native-runner configuration.
2. Native process runner that executes an approved executable with safe
   arguments, streaming output, cancellation, and hardware policy.
3. Download/catalog service with user-approved HuggingFace/GitHub/provider
   model plans and checksums.
4. Python.NET, PowerShell, ONNX/ML.NET, or native-process runner adapter.
5. LocalGPT compatibility test by pointing DXAiChat at the generated host URL.
6. Multi-model worker test with two small/light models or a simulated runner:
   prove concurrent execution when supported, or prove honest queued execution
   when the selected runner or hardware cannot run more than one model at a time.

Do not present a provider proxy as a complete or accepted local AI host. Do
present a local-file runner host as the foundation, and state exactly which
native executable/library and model formats it needs for real inference.

## Provider-Compatible Local Model Host Milestone

When the user asks for a .NET local AI host that LocalGPT can test as a replacement host URL, generate an easy-testable compatibility milestone:

- ASP.NET Core routes for `/api/version`, `/api/tags`, `/api/ps`, `/api/chat`, `/api/generate`, `/api/show`, `/api/pull`, `/api/delete`, and `/v1/chat/completions` where selected.
- A model catalog backed by SQLite for installed models, downloadable model plans, local file paths, hashes, friendly names, context defaults, runner compatibility, and approval state.
- Appsettings only for bootstrap values: database path, default listen URL, logging, safe storage root, first model search roots, and native runner executable path.
- A native runner interface that reads local model files directly. Ollama manifests may be parsed as local metadata, but `/api/chat` and `/api/generate` must not forward to the Ollama service.
- DevExpress Blazor pages for chat, model catalog, downloads, running sessions, settings, logs, API console, templates, and hardware budget.
- Download plans from Hugging Face or GitHub must be user-approved. Do not download model binaries just because a catalog row exists.
- Keep native inference honest: if GGUF/GPU execution is not configured, return clear runner/setup metadata and a user-visible next step. Do not delegate to an upstream provider as a fallback.

## DevExpress Blazor Demo Lessons

The local DevExpress 25.2 Blazor demo teaches component usage and service wiring:

- Server-side and WebAssembly hosted project shapes.
- Central package version management through `Directory.Packages.props`.
- DevExpress service registration through `AddDevExpressBlazor` and reporting-specific registration where needed.
- Demo metadata and page organization for searchable component examples.
- `DxAIChat` examples for overview, templates, attachments, prompt suggestions, message handling, function calling, and grid function calling.
- `DxGrid` examples for CRUD, editing modes, validation, filtering, search, layout persistence, master-detail, selection, export, and large data.
- `DxFormLayout` for aligned settings and editor forms.
- Upload/file input examples for chunk upload, validation, multi-file selection, and upload modes.
- RichEdit, reports, PDF, charts, pivot, scheduler, and document workflows where the request requires them.

Generation rule: when the user asks for a component, produce the Razor page plus service/state wiring, registration notes, CSS if needed, and backend endpoints for data or downloads.

## Video Cutter Source Lessons

The local `videocutter` source is useful for optional Python/media tool integration:

- Python scripts can own media-specific pipelines while .NET owns UI, API, settings, logs, and permissions.
- Generated .NET apps should wrap external media actions behind user-approved backend services.
- Store command plans, inputs, outputs, and logs in SQLite or a bounded artifact folder.
- Use progress UI for long media tasks, and never block the Blazor UI thread.

For .NET generation, translate this into `IMediaJobService`, queued jobs, safe working directories, log capture, and DevExpress grids/forms for job history and settings.

## Whisper, Harmony, And Agents Source Lessons

The selected learn-base can include Whisper, Harmony, and OpenAI agents source folders. Teach the council these as architecture patterns, not as direct dependencies that must be copied:

- Whisper-style speech pipelines: audio input, model selection, transcription jobs, timestamps, language options, batching, progress, cancellation, and artifacts. In .NET, wrap them behind approved backend services and store job state/logs in SQLite.
- Harmony/chat-template handling: parse channels and final-answer boundaries defensively. UI must display user-visible final text, show permitted model-thinking summaries in a separate block, and never let channel markers leak into Markdown.
- Agent frameworks: model clients, tool/function registration, typed tool schemas, handoffs, guardrails, tracing, memory/state, and streaming events. In C#, translate these into interfaces such as `IAgentRunner`, `IAgentTool`, `IAgentTraceStore`, and `IAgentMemoryService`.
- Tool calls are not permission to self-expand. Generated agents must ask the user before running native commands, downloading model files, editing the real project, or integrating generated code.

## Jezzifa-Style Source Lessons

The local Jezzifa-style source is sanitized architecture evidence:

- Multi-project or microservice-style solution boundaries can separate business objects, core services, API hosts, frontend hosts, bot services, and optional Python integration.
- DevExpress Web API/XAF/OData-style business objects need explicit keys, scalar foreign keys, navigation properties, inverse relationships, and security/display metadata.
- Python.NET integration should use typed options for Python paths and runtime settings, explicit user permission gates, and service isolation.
- Telegram/bot integrations belong behind backend services with safe secrets handling and logs.
- Legacy names and obscene names are not relevant and must not be copied into generated code or guidance.

Poll before choosing monolith, modular monolith, microservice, DevExpress Web API security, plain EF backend, Python interop, bot integration, and deployment shape.

## Required Pages for an AI Host Rebuild

A serious generated solution should include at least these pages:

- Dashboard: provider status, installed models, running sessions, downloads, recent errors.
- Chat: model selector, streaming output, thinking visibility, tool/function status, artifact links.
- Model Catalog: installed/downloadable models, details, pull/delete/copy actions.
- Downloads: progress grid, logs, retries, checksum/state.
- Running Models: sessions, keep-alive, unload, memory budget, cancellation.
- API Console: selectable route templates and request/response viewer.
- Templates: chat templates, harmony/thinking parsing rules, model-specific settings.
- Hardware: CPU/GPU policy, VRAM limit, parallelism, safe defaults.
- Logs: EF-backed application logs with filters.
- Settings: provider endpoints, default model, token/context defaults, database paths, startup behavior.

Use a left navigation shell. Use DevExpress controls for real work surfaces and Bootstrap utilities for spacing and responsive layout.

## Generation Contract

When asked to build or rebuild a local AI host in .NET Blazor DevExpress style:

- Generate a real solution structure, not only a dashboard.
- Include ASP.NET Core API routes and DevExpress Blazor pages.
- Provide downloadable source zip artifacts through HTTP links.
- Include service interfaces, concrete stub/provider implementations, EF models, and startup wiring.
- Use the provider-compatible route families and UI workflows above as the baseline.
- Ask a poll only for missing architecture choices, then stop until the user answers.
- Do not refuse by claiming the task is too much. Produce a buildable milestone and list staged follow-up work.
- If the generated result would miss a route family, runtime/provider behavior, DevExpress component pattern, model download workflow, or validation step, add a `Capability gap report` and `<localgpt-capability-gap>` block. Name the missing language/framework/version/domain knowledge, local LocalGPT sources to inspect, external official sources needed, missing LocalGPT functions, and the next downloadable artifact plan.
