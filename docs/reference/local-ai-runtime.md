# Local AI runtime: Python.NET, direct upstream models and optional hubs

LocalGPT 4.8.0 introduced a specialized local-AI runtime beside the existing conversational-provider system. It is intentionally not a second Council orchestrator. Ollama, LM Studio and OpenAI-compatible hosts keep owning normal LLM/VLM chat routes; image, video and speech models can instead be installed as capability-bound local models and invoked through `localai.*` DXFunctions from Chat or Council workflows.

## Runtime ownership

The embedded Python runtime follows the serialized-worker pattern proven by the older Jezzifa/Whisper integration, but exposes it through an asynchronous bounded queue. `PythonNetRuntimeCoordinator` is a process singleton because Python.NET binds one CPython runtime in-process. Callers remain asynchronous: jobs enter a bounded channel, one worker acquires the Python GIL, executes a fixed bridge operation, and completes the caller task. A changed CPython binding after initialization is not rebound live; LocalGPT marks restart required instead.

The bridge does not accept Python source. .NET sends one registered operation key plus structured JSON to `Runtime/Python/localgpt_runtime_bridge.py`. The bridge currently owns fixed operations for runtime probing, direct OpenAI Whisper weight acquisition, optional Hugging Face snapshot download, image generation/editing, text/image-to-video generation, speech recognition, model eviction and cache clearing. The bridge module is loaded once inside the embedded interpreter so its model cache survives between queued jobs; the selected device is part of the cache key.

## Installation workflow

The **Install > Toolchains** workbench owns Python discovery/linking beside .NET, Node.js and the other compiler/runtime profiles. It also shows reviewed manufacturer/GitHub download entries and redacted Effective, Process, LocalGPT Application, User and Machine environment scopes. Downloads require explicit approval and are never executed automatically.

The **Install > Local AI runtimes** workbench uses direct upstream projects as the primary model path. OpenAI Whisper is fully executable in this release: LocalGPT downloads the upstream GitHub source ZIP over HTTPS, installs it into the managed virtual environment without Git/GitHub CLI, and lets the installed Whisper package download/load the selected real model weights. Additional upstream projects can be downloaded as source until a bounded adapter exists.

The same workbench exposes the saved Python/Whisper execution policy: default device, Whisper variant, transcribe/translate task, language, initial prompt, queue capacity, input/media limits and model caching. The local user can edit it directly; AI teams can read/propose the same policy through `localai.runtime.configuration` and the approval-gated `localai.runtime.configuration.update` DXFunction. Queue-capacity changes after Python.NET initialization are marked restart-required rather than rebound unsafely.

PyTorch remains separate from capability profiles because CUDA, ROCm, MPS and CPU installations have platform-specific requirements. The generic PyPI PyTorch profile is an explicit user action rather than an automatic replacement for a vendor-specific backend.

## Optional Hugging Face boundary

Hugging Face is an optional provider-bound integration, not the primary acquisition route. Search uses `https://huggingface.co/api/models` metadata only. Snapshot installation uses `huggingface_hub.snapshot_download` only after the optional `hub-huggingface` package profile is explicitly installed. The download runs as a bounded helper process so a multi-gigabyte snapshot does not monopolize the serialized embedded Python.NET inference lane. It first lands in a managed incoming directory and is promoted only after a successful result. LocalGPT does not run `git clone`, `setup.py`, shell scripts or repository commands as part of the model-install action.

`trust_remote_code` remains false by default and is recorded per installed model when explicitly enabled. Models are stored with a manifest containing their repository identity, revision, adapter and declared LocalGPT capability bindings. Model data remains separate from the conversational-provider registry.

## Implemented capability adapters

The first runtime adapters are intentionally bounded and useful:

- `ImageGeneration` -> Diffusers image pipelines such as compatible Qwen-Image, FLUX or Z-Image repositories;
- `ImageEditing` -> compatible Diffusers image-edit pipelines over bounded workspace images;
- `TextToVideo` -> compatible Diffusers video pipelines such as supported Wan-family repositories;
- `ImageToVideo` -> compatible Diffusers image-to-video pipelines over bounded workspace images;
- `SpeechRecognition` -> the direct OpenAI Whisper adapter or optional Transformers ASR pipelines.

The capability model already names additional media/understanding categories so catalog metadata can describe them, but a capability is executable only when a LocalGPT runtime function/adapter exists for it. Adding a new backend should extend the service/bridge operation and DXFunction surface rather than teaching Chat or Council model-specific Python.

## Council and DXFunction integration

Specialized models are tools, not fake conversational members. Current DXFunctions include:

- `localai.runtime.status`
- `localai.runtime.probe`
- `localai.runtime.configuration`
- `localai.runtime.configuration.update`
- `localai.sources.known`
- `localai.source.download`
- `localai.known.install`
- `localai.runtime.cache.clear`
- `localai.models.installed`
- `localai.huggingface.search`
- `localai.model.install`
- `localai.model.remove`
- `localai.image.generate`
- `localai.image.edit.workspace`
- `localai.video.generate`
- `localai.video.from-image.workspace`
- `localai.audio.transcribe.workspace`

Model installation/removal and media generation are human-approval operations. Automatic Council requests can enter the existing deferred Human Collaboration flow so other Council work does not need to block while approval is pending. Generated image/video files are published as bounded LocalGPT artifacts and returned by URL plus a ready-to-use Markdown representation for Chat/Council review. Media routes are private/no-store, support byte ranges for video, and are served inline rather than forced as downloads.

This keeps the Council as the reasoning/orchestration layer: an LLM can request an image, a specialized model creates it, and another LLM/VLM can interpret or incorporate the artifact without forcing the diffusion/video model into a text-turn contract.

## Media privacy and admission

Raw user audio or workspace images are not handed to the Python runtime in-place. Workspace input is copied into a per-job private scratch directory, bounded by size, passed to a fixed operation, and deleted in a `finally` path. Images also have a decoded-pixel ceiling before conversion/inference. Audio is admitted by decodable duration and payload density as well as byte size. The bridge can additionally reject undecodable/empty audio, excessive duration and implausibly low encoded bytes per decoded second, preserving the anti-empty/abuse idea from the older Whisper worker without persisting user media.

Generated output is staged separately and then copied into the LocalGPT artifact root. Logs omit prompts, transcripts, model filesystem paths, credentials and generated bytes. Filesystem deletion is used for scratch cleanup; this is retention minimization, not a claim of forensic secure erasure on SSD/filesystem layers.

## Failure containment

The Python queue is bounded and serialized around the process-global interpreter/GIL. Active jobs link caller cancellation with runtime shutdown, and startup scavenges abandoned ephemeral scratch from a previous process. Cancellation is cooperative: .NET creates a per-job cancellation marker and compatible diffusion pipelines receive a step callback. A Python job failure is returned as a failed job result and does not become a Council-wide exception. Changing Python runtime identity after the interpreter has started requires a LocalGPT restart rather than unsafe in-process rebinding.

If a future model family proves unsafe or dependency-incompatible in the shared interpreter, the public runtime service and DXFunction contracts are intentionally independent of transport so an execution lane can later move to an isolated worker process without changing Council or Chat orchestration.
