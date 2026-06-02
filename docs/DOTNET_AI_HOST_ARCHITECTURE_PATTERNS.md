# .NET AI Host Architecture Patterns

Use this guide when LocalGPT, DXAiChat, the AI Council, or an agent generates a
local AI host, provider-compatible control plane, model runner, plugin host, or
application that needs external/native execution.

The important rule: do not confuse a UI dashboard with an AI host. A useful
generated milestone must show the architecture that could later run or delegate
model inference.

## Source Baseline

This guide is grounded in:

- .NET dependency injection:
  https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection
- ASP.NET Core dependency injection:
  https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection
- .NET options pattern:
  https://learn.microsoft.com/en-us/dotnet/core/extensions/options
- .NET worker services and ASP.NET Core hosted services:
  https://learn.microsoft.com/en-us/dotnet/core/extensions/workers
  and https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
- .NET plugin support with `AssemblyLoadContext` and
  `AssemblyDependencyResolver`:
  https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support
- Common web application architectures and .NET microservice guidance:
  https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures
  and https://learn.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/microservice-application-design
- Windows ML and ONNX Runtime local inference guidance:
  https://learn.microsoft.com/en-us/windows/ai/new-windows-ml/run-onnx-models
- ML.NET overview and pretrained model consumption:
  https://learn.microsoft.com/en-us/dotnet/machine-learning/overview
- PowerShell host/runspace guidance:
  https://learn.microsoft.com/en-us/powershell/scripting/developer/hosting/creating-runspaces
- Python.NET embedding documentation:
  https://pythonnet.github.io/pythonnet/dotnet.html

## Product Boundary

A local AI host can be split into layers:

- UI/control plane: navigation, chat, model catalog, downloads, settings, logs,
  hardware budget, templates, API console, and artifact links.
- HTTP compatibility layer: `/api/version`, `/api/tags`, `/api/ps`,
  `/api/show`, `/api/pull`, `/api/generate`, `/api/chat`, `/api/embed`, and
  optional `/v1/*` routes.
- Application services: model catalog, settings, logs, downloads, provider
  adapters, chat templates, session state, and hardware policy.
- Runner orchestration: queueing, load/unload, keep-alive, cancellation,
  streaming, health, and process/plugin lifecycle.
- Inference implementation: external provider, native process, ONNX/ML.NET,
  Python.NET bridge, PowerShell/native script, or future plugin.
- Storage: SQLite for settings, model records, downloads, chats, logs, jobs,
  and approvals. Appsettings stays for bootstrap and logging.

The first milestone can delegate to an existing provider. A later milestone can
attach a native or Python-backed runner. Never claim native inference exists
until a real backend is wired and tested.

## Interfaces To Generate

When the user asks for an AI host or provider-compatible app, generate these
interfaces before UI pages:

- `IModelCatalogService`: installed models, downloadable candidates, details,
  aliases, approval state, tags, local file metadata, and running models.
- `IModelTransferService`: pull/push/download plan, progress, retry, checksum,
  license/size/target-path approval, and cancel.
- `IInferenceProvider`: chat, generate, embed, stream tokens, report supported
  formats, cancel, and health.
- `IInferenceRunner`: native/process/plugin/Python runner contract for load,
  unload, infer, embed, health, and shutdown.
- `IRuntimeSessionService`: active sessions, keep-alive, queueing, resource
  budgets, unload idle, and cancellation.
- `IHardwareBudgetService`: CPU/GPU layers, VRAM target, concurrency, context,
  driver-safety policy, and model placement.
- `IChatTemplateService`: ChatML, Harmony, plain prompt, stop sequences, tools,
  role mapping, thinking/final parsing, and structured output.
- `IPluginCatalogService`: discover runner/provider plugins, validate manifests,
  load approved plugins, and expose trust/version status.
- `IScriptExecutionService`: approved PowerShell/Python/native scripts with
  bounded working directories, input/output capture, logs, and cancellation.

Prefer constructor injection and typed options. Do not use service locator
patterns except in carefully scoped plugin/host boundaries.

## IoC And Options Pattern

Use .NET DI as the composition root:

- Register interfaces in `Program.cs`.
- Use extension methods such as `AddAiHostCore()` only when the generated
  solution is large enough to justify grouping.
- Use `IOptions<T>`, `IOptionsMonitor<T>`, and validation for provider/runtime
  settings.
- Use `IServiceScopeFactory` inside hosted services when scoped services such as
  EF DbContexts are needed.
- Avoid resolving scoped services from singleton constructors.
- Use typed `HttpClient` or named clients for provider adapters.

Settings shape:

- appsettings: database path, safe storage root, first listen URL, logging,
  bootstrap provider URI.
- SQLite: user-approved model sources, provider profiles, hardware budgets,
  downloads, runner plugins, chat defaults, and generated-artifact state.

## Plugin System Pattern

Use plugins when runner/provider implementations need separate dependencies or
can be swapped independently.

Recommended structure:

- `PluginBase` project or folder with shared interfaces and DTOs.
- Host app loads plugin assemblies with a custom `AssemblyLoadContext`.
- Use `AssemblyDependencyResolver` so plugin dependencies and native libraries
  resolve from the plugin output folder.
- Set plugin class libraries to target the runtime framework, such as .NET 10,
  and enable dynamic loading where needed.
- The host must reference frameworks needed by plugins. A plugin cannot add a
  framework such as `Microsoft.AspNetCore.App` to a host that lacks it.
- Treat plugins as trusted code only. .NET plugin loading is not a security
  sandbox. For untrusted code, use OS/process/container isolation.

Generated plugin manifests should include:

- id, display name, version, target runtime, entry type, supported model formats,
  required files, native dependencies, environment variables, permissions,
  hardware support, and approval state.

## Runner Adapter Pattern

Use adapter layers so the control plane can work before native inference exists:

- `ExternalProviderInferenceProvider`: delegates to Ollama, LM Studio,
  OpenAI-compatible, or another HTTP provider.
- `ProcessInferenceRunner`: starts a local executable with safe arguments,
  streams stdout/stderr, handles cancellation, and records logs.
- `PythonNetInferenceRunner`: embeds Python only after user approval and runtime
  configuration.
- `PowerShellRunner`: uses a constrained runspace or explicit script file path
  for approved automation.
- `OnnxRuntimeRunner`: loads ONNX models with ONNX Runtime/Windows ML when the
  model format is compatible.
- `MlNetRunner`: uses ML.NET for supported machine-learning models and simple
  prediction tasks.

Each adapter must report:

- supported model formats
- whether it is local, external, native, script-based, or simulated
- whether it supports streaming, embeddings, tools, and cancellation
- hardware backend and expected memory budget
- health and last error

## Python.NET Pattern

Python.NET can be useful when Python libraries or model tooling are the most
practical backend.

Generation rules:

- Use Python.NET only behind an explicit adapter such as
  `IPythonInferenceRunner` or `IScriptExecutionService`.
- Require user approval for Python runtime path, `PYTHONNET_PYDLL`, packages,
  model directories, and script execution.
- Call `PythonEngine.Initialize()` during controlled startup, then acquire the
  GIL with `using (Py.GIL())` for Python object/API calls.
- Prefer importing a Python module and delegating work to it instead of writing
  large Python strings inside C#.
- Treat Python code as trusted only. It is not a CLR security sandbox.
- Log inputs, outputs, stderr, exceptions, package/runtime version, and artifact
  paths.
- Keep Blazor responsive: long Python work belongs in a background job with
  progress and cancellation.

## PowerShell And Script Pattern

PowerShell can orchestrate setup, downloads, diagnostics, and external tools.

Generation rules:

- Prefer explicit `.ps1` files with typed parameters over command strings.
- Use safe working directories and allowlisted script paths.
- Use constrained runspaces or limited InitialSessionState when embedding
  PowerShell in-process.
- Capture stdout/stderr, exit code, duration, and cancellation.
- Never run generated scripts automatically without user permission.
- Keep scripts out of the UI thread; expose progress/logs through services.

## Native Inference Runner Milestones

A generated AI host can honestly progress through milestones:

1. Control-plane shell: UI pages, API routes, settings, logs, model catalog,
   downloads, and deterministic non-inference responses.
2. External provider adapter: delegate `/api/chat` and `/api/generate` to
   Ollama/LM Studio/OpenAI-compatible endpoint selected by the user.
3. Managed inference adapter: ONNX Runtime/ML.NET for compatible models.
4. Script/Python adapter: Python.NET or approved process scripts for model
   tooling and inference.
5. Native runner plugin: executable or library backend with load/unload,
   streaming, cancellation, hardware policy, and model storage.
6. Benchmark and LocalGPT compatibility: point LocalGPT DXAiChat at the new
   provider URL and verify tags, chat, generate, downloads, settings, and logs.

Each milestone should build and expose downloadable source. If the runner is
missing, the app must show this as a visible capability gap and still provide
the next buildable step.

## Generated Solution Acceptance

An AI-host solution is not acceptable if it only has:

- a dashboard,
- fake grid rows,
- no provider/plugin interfaces,
- no service boundaries,
- no route compatibility,
- no settings/storage plan,
- no download/progress shape,
- no runner capability-gap report.

An acceptable milestone includes:

- real ASP.NET Core routes,
- DevExpress/Bootstrap pages for the core workflows,
- interfaces and concrete stub/provider classes,
- typed options and `Program.cs` registrations,
- README/architecture/build docs,
- `.localgpt-generation.json` with validation status,
- clear `NativeInference = NotImplemented` until a real backend exists,
- a next-step plan to attach external provider, Python.NET, PowerShell, ONNX,
  ML.NET, or native plugin runner.

