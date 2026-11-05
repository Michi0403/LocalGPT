# LocalGPT 4.7.6

## Local AI runtime foundation

- Adds a first-class **Local AI runtimes & Hugging Face models** workbench to the Install page for specialized models that are not conversational LLMs.
- Reuses LocalGPT's existing toolchain discovery to find Python installations and records the reviewed Python executable, shared runtime library, and resolved Python home explicitly.
- Adds a LocalGPT-managed virtual environment, model root, runtime scratch, input scratch and media-artifact boundary instead of requiring a machine-wide `PATH`, `PYTHONHOME`, or globally polluted Python installation.
- Adds visible package profiles for Python.NET/Hugging Face core, an explicit generic PyTorch option, Diffusers image/media, Transformers/Whisper speech, and Diffusers video. Media profiles do not silently replace an already-optimized CUDA/ROCm/vendor PyTorch installation.
- Adds platform-oriented Python installation hints and documents `HF_TOKEN` as the default optional authenticated Hugging Face environment variable without storing the token value in model manifests.

## Serialized Python.NET execution

- Adds `PythonNetRuntimeCoordinator`, a process-owned singleton with a bounded asynchronous queue and one serialized interpreter/GIL lane, following the stable worker pattern from the older Jezzifa/Whisper integration without copying its Telegram-specific architecture.
- Callers submit fixed operation keys plus structured parameters. Model/AI output cannot submit arbitrary Python source through the runtime job DTO.
- Python.NET is loaded from the managed environment at runtime, avoiding a hard compile-time Python.NET package dependency in LocalGPT.
- Interpreter binding is initialized lazily. If Python runtime/home/environment binding changes after initialization, LocalGPT marks restart required instead of attempting unsafe in-process rebinding.
- Per-job control files and private input copies live under LocalGPT runtime scratch and are removed on `finally` cleanup paths. Startup also scavenges abandoned runtime/input/install scratch and incomplete incoming snapshots from a previous crashed/stopped process.
- Diffusers jobs use a cooperative cancellation marker when the selected pipeline exposes a step callback. Queue accounting remains single-reader owned so cancelled waiters cannot drive the visible queue count negative.
- The fixed Python bridge is loaded once as a stable interpreter module, so model caching actually persists across queued jobs instead of reloading the pipeline for every invocation. Cache keys include the selected device, and explicit cache eviction/clear releases Python garbage and available CUDA/ROCm/MPS allocator caches.

## Hugging Face catalog and model installations

- Adds metadata-only Hugging Face search with capability classification for image, video, speech, audio, vision, and embedding model families.
- Adds an explicit capability-binding selector so new model families can be installed even when upstream metadata has not yet learned their pipeline classification.
- Adds explicit snapshot installation through `huggingface_hub.snapshot_download` into a temporary managed incoming directory and promotes the completed snapshot into the model root rather than cloning and executing repository setup scripts. Large downloads use the managed virtual-environment Python process instead of occupying the serialized embedded Python.NET/GIL inference lane.
- Optional Hugging Face credentials remain environment-owned; LocalGPT persists only the selected token environment-variable name.
- Gated/private model installation checks for the configured token variable before download.
- Installed-model manifests record repository/revision identity, local path, capability bindings, adapter, install time, and explicit custom-code policy.
- `trust_remote_code` remains off by default and is a per-install opt-in.
- Diffusers model loading requests safetensors-backed local files and does not fall back to arbitrary repository setup commands.
- Model removal is confirmation-gated and constrained to the managed model root. It attempts cache eviction first but still permits managed snapshot removal if Python.NET is unavailable, so a broken runtime cannot strand model data permanently.

## Specialized AI capabilities and Council integration

- Adds generic local adapters for Diffusers text-to-image, image editing, text-to-video, image-to-video, and Transformers automatic speech recognition/Whisper-family models.
- Adds DXFunctions for runtime status/probe/cache-unload, Hugging Face search, installed-model inventory, model install/removal, image generation, workspace image editing, text-to-video, workspace image-to-video, and workspace-audio transcription.
- Model installation/removal and generated-media operations use the existing Human Collaboration/deferred-approval flow; Council work can continue while an automatic request waits for review.
- Specialized models are exposed as capabilities/functions instead of being forced to impersonate conversational Council members. Existing LLM/VLM Council members can plan, invoke, review, and integrate their outputs through normal DXFunction orchestration.
- Generated image/video output is published through bounded LocalGPT artifact IDs, served as private/no-store inline media with range support, and returned with both media URLs and ready-to-use Markdown for Chat/Council handling.
- Pipeline invocation filters optional generation parameters against the selected pipeline signature and maps `guidance_scale` to `true_cfg_scale` when a compatible pipeline exposes that naming instead.
- Capability adapters reject incompatible pipelines that cannot consume their required prompt/image input instead of silently dropping that input, and ASR device selection keeps Apple MPS available while AMD ROCm continues through PyTorch's CUDA-compatible device surface.

## Media privacy and input admission

- Speech, image-edit and image-to-video inputs must already belong to a bounded LocalGPT upload workspace. Image inputs are checked for compressed byte size and decoded pixel count before expensive inference.
- Input media is copied into per-job private scratch, used by the fixed Python operation, and the copied media is deleted in a `finally` path after completion/failure/cancellation.
- Raw media, prompts, transcripts, tokens, generated bytes, and local model/runtime paths are omitted from the new runtime's structured log payloads.
- The speech bridge generalizes the older Jezzifa anti-empty/fraudulent-audio idea with empty/size/duration/payload-density admission checks before ASR inference.
- Scratch cleanup is retention minimization through filesystem deletion; the code does not make a false forensic secure-erasure claim for SSD/filesystem layers.

## Documentation and release wiring

- Adds `docs/reference/local-ai-runtime.md` to packaged and embedded LocalGPT knowledge.
- Updates LocalGPT, installer-console, WebView wrapper, static asset cache versions, documentation version metadata, and outbound LocalGPT user-agent identifiers to 4.7.6.
- PublisherStudio is unchanged.

## Preserved behavior

- The 4.7.5 benchmark JSON first-chance-exception and durable file-logging repairs remain intact.
- The 4.7.4 provider cancellation boundary remains intact.
- Existing Council, ASCII/Pixel game, Creature Tournament, popup interaction, provider, deployment, and protected `@rendermode InteractiveServer` behavior is not intentionally changed by this feature.
