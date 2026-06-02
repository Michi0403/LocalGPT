# Ollama .NET/Blazor Experiment Notes

This note is source-backed by the user-provided local archive:

```text
C:/Users/micha/Downloads/ollama-main.zip
```

## Source Shape

The inspected archive contains about 1,214 source entries. The largest groups are:

- `x/`
- `app/`
- `docs/`
- `template/`
- `model/`
- `server/`
- `cmd/`
- `convert/`
- `discover/`
- `llm/`
- `api/`
- `llama/`

The dominant implementation language is Go, with roughly 722 `.go` files. The archive also contains
TypeScript/React app files, Markdown/MDX docs, JSON/templates, C/C++/header files, CMake files, shell scripts,
PowerShell scripts, and native/runtime build metadata.

## Architecture Observations

Ollama is not only a web UI. The source archive points to several separable concerns:

- REST API types and endpoints, including generate, chat, model creation, tags/list, model show/copy/delete,
  pull/push, embeddings, running model list, and version.
- Server routing and middleware.
- Model naming, manifests, templates, renderers, parser/model metadata, and storage concerns.
- LLM runner abstraction with methods for load, ping, completion, chat, embeddings, tokenize/detokenize, memory,
  VRAM by GPU, process id, device info, context length, and lifecycle.
- Native build model with CMake, C/C++ runtime payload, GGML/GPU backend selection, CUDA/ROCm/Vulkan/Metal-style
  hardware paths, and platform-specific install/runtime packaging.
- CLI and desktop/app integration surfaces.

## Feasibility Guidance For LocalGPT Council

A pure .NET/Blazor/DevExpress replacement is not a realistic single-step generation target. The feasible experiment is:

- Build a .NET 10 ASP.NET Core control plane that mimics selected Ollama REST routes.
- Build a DevExpress Blazor admin UI for models, endpoint compatibility, runner health, logs, and request testing.
- Persist model metadata and compatibility notes in EF/SQLite.
- Keep inference as a stub, adapter, or externally hosted runner unless a real .NET/native inference backend is supplied.
- Generate the work as a sandbox solution zip, not as automatic LocalGPT self-expansion.

Do not claim the generated prototype replaces Ollama. Call it an API-compatible .NET lab, shim, or control-plane prototype.

Generated project constraint: keep the generated stack in .NET, C#, ASP.NET Core, Razor, EF/SQLite, and DevExpress
Blazor. Do not propose generated Go or Python projects for this lab. If inference is discussed, describe it as a
generic external/native backend contract, an existing service adapter, or a future approved .NET/native integration,
not as part of the generated all-.NET solution.

## Recommended Prototype Scope

Generate a downloadable .NET 10 Blazor/DevExpress solution zip with:

- `.sln`
- `.csproj`
- `Program.cs`
- routable `.razor` pages
- CSS
- model/service classes
- README
- manifest
- minimal API stubs for `/api/version`, `/api/tags`, and a non-inference `/api/generate` placeholder
- DevExpress grids/forms for endpoint compatibility and model catalog status

The generated project must build before it is presented as successful.
