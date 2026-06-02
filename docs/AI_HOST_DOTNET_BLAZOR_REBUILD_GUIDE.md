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

## Source Lessons From Local AI Hosts

The local `ollama-main` source is architecture evidence for a .NET control plane. Treat Ollama as one provider/source example, not as the generated application name:

- API route surface: version, model tags/list, running models, show, pull, push, create, copy, delete, generate, chat, and embed.
- Compatibility adapters: OpenAI-compatible and Anthropic-compatible request/response surfaces.
- Model lifecycle: manifests, layers, digests, local model paths, copy/delete/show metadata, downloads, uploads, and progress.
- Runtime orchestration: loaded model sessions, keep-alive, runner processes, cancellation, status, and hardware-specific launch choices.
- Formatting: chat templates, tokenizer behavior, harmony/thinking content, structured output, tools, and streaming chunks.
- Platform shell: WebView/tray concepts and OS-specific runtime helpers.

Translate those into .NET patterns:

- `IModelCatalogService` for installed, downloadable, and running models.
- `IModelTransferService` for pull/push/download progress and checksums.
- `IInferenceProvider` for chat, generate, embeddings, cancellation, and streaming.
- `IRuntimeSessionService` for load/unload, keep-alive, and current model state.
- `IHardwareBudgetService` for CPU/GPU/VRAM/concurrency settings.
- `IChatTemplateService` for role formatting, harmony parsing, thinking extraction, and tool calls.
- ASP.NET Core endpoints for provider-compatible route families.
- SignalR or server-side streaming for progress and token output.
- EF/SQLite records for model metadata, settings, chat history, logs, downloads, and knowledge entries.

Native tensor execution is a separate backend. A generated first milestone can be a real control plane with an Ollama, LM Studio, OpenAI-compatible, or custom provider adapter, plus explicit extension points for native inference.

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
