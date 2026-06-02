# Ollama Replacement Architecture Notes

These notes teach the AI Council how to reason about an Ollama-inspired .NET replacement without treating it as black magic or pretending the hard parts are already solved.

## Split the Problem

An Ollama-like system has at least four separable layers:

- API/control plane: HTTP routes, streaming responses, model catalog, running-model list, settings, logs, downloads, health, and request validation.
- Model storage and lifecycle: local model registry, manifests, download/pull state, file integrity, disk layout, loading/unloading, keep-alive, and concurrency limits.
- Inference runtime: tokenizer, prompt template/chat formatting, tensor execution, KV cache, sampling, embeddings, batching, and cancellation.
- Hardware backend: CPU kernels, GPU kernels, VRAM planning, AMD/NVIDIA/Intel backends, driver stability, and memory pressure controls.

LocalGPT can generate and test the API/control-plane layer well in .NET/ASP.NET Core/DevExpress Blazor.
It must not claim native GGML/GGUF/GPU inference exists unless a real approved backend is attached.

## .NET-Friendly Design

A serious .NET version should start as an API-compatible control plane with provider interfaces:

- `IModelCatalogService`: lists installed, downloadable, and running models.
- `IModelDownloadService`: plans and performs model pulls with progress and checksums.
- `IInferenceProvider`: chat/generate/embed entry point.
- `IModelRuntimeSession`: loaded model lifetime, keep-alive, unload, and cancellation.
- `IHardwareBudgetService`: CPU/GPU/VRAM policy and safe defaults.
- `IChatTemplateService`: model-specific prompt templates, harmony/thinking parsing, and role formatting.

The first backend can call existing Ollama/LM Studio/OpenAI-compatible providers.
A later native backend can wrap an approved inference engine or library.
Do not start by reimplementing tensor kernels inside Razor pages.

## API Routes to Emulate

An Ollama-inspired .NET lab should include representative route families:

- `GET /api/version`
- `GET /api/tags`
- `GET /api/ps`
- `POST /api/show`
- `POST /api/pull`
- `POST /api/push`
- `POST /api/create`
- `POST /api/copy`
- `DELETE /api/delete`
- `POST /api/generate`
- `POST /api/chat`
- `POST /api/embed`

Routes may be stubs in a lab artifact, but each stub must say what is implemented, what is simulated, and what needs a real inference provider.

## Generation Rules

When the user asks for an Ollama replacement:

- Ask whether they want API compatibility, a UI/control plane, a provider facade, or native inference.
- Generate an ASP.NET Core API plus DevExpress Blazor UI when the request is LocalGPT-style.
- Include model downloads/settings/logs/API console pages, not only a generic dashboard.
- Use safe download/progress endpoints instead of dumping binary data into chat.
- Mark native GPU inference as `Needs real backend` unless explicitly implemented.
- Keep architecture choices in a user-confirmed poll when the scope is ambiguous.
