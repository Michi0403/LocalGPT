# AI Host control plane

The AI Host control plane is a first-class part of LocalGPT's architecture. It defines how providers, model catalogs, runners, downloads, settings, logs, chat, and API-compatible routes fit behind explicit .NET service boundaries.

## Purpose

The AI Host boundary gives LocalGPT a provider-neutral way to discover, configure, run, and observe compatible model runtimes while keeping native inference engines replaceable.

It separates four concerns:

1. **Control plane** — model catalog, running-model state, settings, downloads, health, logs, and request validation.
2. **Provider adapter** — Ollama, OpenAI-compatible, OpenAI, Azure OpenAI, or another bounded route.
3. **Native/model runtime** — the process or library that owns inference, model loading, and hardware use.
4. **LocalGPT orchestration** — Chat/Council selection, benchmarks, presets, policy, and UI.

LocalGPT should not pretend that rebuilding a high-performance inference engine in managed code is a small UI task. A .NET host can provide a strong control plane, provider-compatible API, native runner boundary, and plugin model while delegating optimized inference to a suitable runtime.

## Provider-qualified identity

A model route contains:

- provider kind;
- configured provider name;
- endpoint;
- provider-native model name;
- optional credentials held by the configured provider;
- capabilities and provider-specific options.

The route is the identity. Model-name strings alone are insufficient.

## Core services

A practical .NET architecture uses contracts similar to:

| Service | Responsibility |
|---|---|
| `IProviderModelRuntimeService` | Discover and invoke provider-qualified routes |
| `IChatClientFactory` | Create the correct client for a route immediately before use |
| `IOllamaProcessService` | Inspect or control a bounded local Ollama process when explicitly configured |
| `IAiConnectivityProbe` | Test provider health without starting unrelated processes |
| `IModelPresetService` | Store LocalGPT routing presets |
| `IProviderModelBenchmarkService` | Run bounded benchmark tasks and recommendations |
| `INativeCommandRunner` | Execute approved native commands without shell-string composition |

A host-specific adapter owns route translation, streaming payloads, error normalization, and capability discovery. The Council layer consumes the common route model rather than branching on provider details in Razor components.

## API families

A compatible control plane commonly needs route families for:

- health and version;
- model catalog and model details;
- installed/running models;
- chat/generate/embedding requests;
- streaming responses and cancellation;
- pull/download progress;
- load/unload/keep-alive;
- runtime settings and resource limits;
- logs and diagnostics.

Compatibility must be explicit. A route should not be advertised until its request, response, streaming, cancellation, and error behavior are implemented and tested.

## Native runner boundary

The managed application owns configuration and policy; a native runner owns the executable process. The boundary should provide:

- executable and argument arrays rather than shell command strings;
- working-directory and environment allowlists;
- cancellation and timeout;
- bounded stdout/stderr capture;
- process identity and exit code;
- no automatic elevation;
- no execution of uploaded binaries.

Plugins or runners should be registered from reviewed application configuration, not discovered and executed from arbitrary archives.

## Model lifecycle

Model acquisition is a separate workflow from model use. A safe lifecycle includes:

1. user selects a provider and model source;
2. LocalGPT shows size, destination, compatibility, and checksum/signature information when available;
3. the user approves the download;
4. progress is recorded without leaking credentials;
5. the adapter verifies the result;
6. the model enters the catalog;
7. load/unload remains explicit and observable.

A benchmark may recommend context/output bounds or an Ollama GPU profile. It does not pull a model or rewrite provider-global settings automatically. The benchmark service owns a live-session transcript and cancellation token, so the run remains observable and stoppable from Chat even when its initiating panel is no longer in view.

## Blazor and DevExpress surfaces

A useful host UI can include:

- provider dashboard and health cards;
- model catalog and installed-model grid;
- active model/process view;
- chat playground;
- benchmark panel;
- download queue;
- settings and diagnostics;
- API compatibility status.

DevExpress grids, forms, dialogs, tabs, and charts should be used where they add concrete behavior. Bootstrap owns the responsive shell. Pages call application services; they do not become provider adapters.

## Multi-model operation

LocalGPT may run several compatible routes at once when hardware, provider behavior, and policy allow it. Resource planning should remain visible: model size, estimated memory, context, output bounds, concurrency, and provider limits are inputs to the plan.

The architecture must degrade honestly. If a requested model cannot be loaded beside another one, the host reports the constraint and offers a reviewable unload/retry plan instead of silently replacing the active model.

## Acceptance target

A LocalGPT AI Host milestone is complete only when:

- provider-qualified routes remain distinct;
- health/discovery and invocation use the same configured endpoint;
- streaming and cancellation work;
- native execution is bounded;
- credentials stay scoped;
- model lifecycle actions are explicit;
- the UI shows real state;
- benchmark recommendations require user application;
- compatibility claims are backed by tests.
