# LocalGPT 4.7.6 source validation

LocalGPT 4.7.6 was reviewed as a **source-only** change set. Per the handoff constraint, validation does not invoke `dotnet`, MSBuild, NuGet restore, build, publish, or GitHub access. Runtime compilation and GPU/model smoke testing remain for the owner's normal .NET/Python machines.

## Focus

The release review covers Python discovery/linking, managed environments, serialized Python.NET execution, Hugging Face metadata/snapshot boundaries, specialized image/video/speech adapters, DXFunction exposure, media artifact/input handling, Install integration, and preservation of the 4.7.4/4.7.5 regression fixes.

## Source checks

- `python build/audit_release_4_7_6.py`
- `python build/audit_devexpress_blazor_controls.py`
- `python build/audit_application_architecture.py --root . --product localgpt --mode static`
- `python -m py_compile src/LocalGPT/Runtime/Python/localgpt_runtime_bridge.py build/audit_release_4_7_6.py`
- `node --check src/LocalGPT/wwwroot/js/localgpt-game-console.js`
- `node --check src/LocalGPT/wwwroot/js/localgpt-chat-ui.js`
- XML parsing of the three versioned `.csproj` files.
- JSON parsing of `appsettings.json`, `appsettings.Development.json`, and `docs/docfx.json`.

The architecture static audit retains exactly the five known 4.7.5 findings and adds no new application-static finding: two `FileLogger` helpers plus the three non-throwing JSON-carrier helpers added in 4.7.5. The DevExpress/Razor audit passes with the existing App reconnect-control exceptions only.

## Python bridge review

The bridge accepts only registered operation names and structured JSON. It has no caller-supplied Python-source field and no `eval`, shell, subprocess, or `os.system` execution path. Model install uses Hugging Face `snapshot_download`; generated-model execution uses local model paths. Image/video operations request Diffusers safetensors loading with `local_files_only=True`; speech uses a local Transformers ASR model.
Capability-specific calls additionally verify that the selected pipeline accepts required modality inputs before optional argument filtering, so an image-edit/image-to-video request cannot silently degrade into an unrelated text-only generation path.

A standalone `runtime_probe` bridge envelope is exercised with the available Python interpreter to verify the fixed job/result contract without downloading a model or requiring GPU inference. Python bytecode caches created by syntax checking are removed before packaging.

## Privacy and containment review

Workspace media is path-bounded before private copying. Private speech/image inputs are removed from runtime scratch on `finally` paths. New logs avoid prompt/media/transcript/token/path payloads. Generated output is staged separately and then published into a bounded artifact root.

The embedded interpreter is process-global but execution is serialized behind a bounded asynchronous channel and Python GIL scope. Changing the CPython binding after initialization requires restart. Caller cancellation does not corrupt queue accounting; cooperative inference cancellation uses a marker/step callback where supported.

## InteractiveServer guard

The 4.7.5 and 4.7.6 source trees retain the exact same 15 `@rendermode InteractiveServer` components. No protected render-mode removal is part of this release.

## Known runtime test points

- This environment cannot compile the .NET source by design, so owner-side compilation remains required.
- GPU-specific PyTorch packages are intentionally not guessed. CUDA/ROCm/MPS/vendor-specific setup should be selected for the actual host; the generic PyPI PyTorch profile is explicit and visibly marked.
- Diffusers model families vary in accepted parameters. The bridge filters optional kwargs to the selected pipeline signature and includes a `true_cfg_scale` alias, but individual model repositories may still require an adapter extension.
- Transformers ASR cancellation cannot necessarily interrupt a native model call mid-kernel; cancellation is checked around the call while the LocalGPT caller stays asynchronous.
- Capability names such as speech synthesis/audio generation/understanding are represented for catalog classification, but 4.7.6 executable adapters are image generation/editing, text/image-to-video, and speech recognition.
- `trust_remote_code` remains an explicit trust boundary and must only be enabled for a repository the local owner has reviewed.

## Packaging

The final ZIP is validated for CRC integrity, path traversal/absolute entries, absence of Python cache files, and byte-for-byte clean-extraction equivalence with the packaged worktree.
